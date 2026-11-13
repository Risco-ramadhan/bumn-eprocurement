using Eproc.CatalogAndClm;
using Eproc.Configuration;
using Eproc.Enums;
using Eproc.ExecutionAndFinance;
using Eproc.Planning;
using Eproc.Services;
using Eproc.Sourcing;
using Eproc.Vms;
using Eproc.Workflows;

var builder = WebApplication.CreateBuilder(args);

// 1. Load Environment Variables (dengan prefix EPROC_) & appsettings.{Environment}.json
builder.Configuration.AddEnvironmentVariables(prefix: "EPROC_");

var clientOptions = builder.Configuration
    .GetSection(EprocClientOptions.SectionName)
    .Get<EprocClientOptions>() ?? new EprocClientOptions();

var featureOptions = builder.Configuration
    .GetSection(EprocFeatureOptions.SectionName)
    .Get<EprocFeatureOptions>() ?? new EprocFeatureOptions();

var integrationOptions = builder.Configuration
    .GetSection(EprocIntegrationOptions.SectionName)
    .Get<EprocIntegrationOptions>() ?? new EprocIntegrationOptions();

builder.Services.AddSingleton(clientOptions);
builder.Services.AddSingleton(featureOptions);
builder.Services.AddSingleton(integrationOptions);

// 2. Register Core Domain & Application Services (100% Zero-ABP Naming)
builder.Services.AddSingleton<TkdnHeaCalculatorService>();
builder.Services.AddSingleton<VendorFinancialScoringService>();
builder.Services.AddSingleton<EnvelopeCryptoService>();
builder.Services.AddSingleton<ThreeWayMatchingService>();
builder.Services.AddSingleton<AuditHashChainService>();
builder.Services.AddSingleton<DoaMatrixApprovalEngine>();
builder.Services.AddSingleton<EprocApplicationConfigurationAppService>();
builder.Services.AddSingleton<VmsCsmsAppService>();
builder.Services.AddSingleton<PlanningAndDoaAppService>();
builder.Services.AddSingleton<ESourcingTenderAppService>();
builder.Services.AddSingleton<ECatalogAndClmAppService>();
builder.Services.AddSingleton<ProjectExecutionAndFinanceAppService>();

// 3. CORS untuk komunikasi Angular Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("EprocCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 4. Konfigurasi Anti-Forgery / CSRF menggunakan penamaan kustom EPROC
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "EPROC-XSRF-TOKEN";
    options.HeaderName = "X-Eproc-Xsrf-Token";
});

var app = builder.Build();

app.UseCors("EprocCorsPolicy");

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Eproc-Client-Code"] = clientOptions.ClientCode;
    context.Response.Headers["X-Eproc-Regulation"] = clientOptions.RegulationReference;
    await next();
});

// =============================================================================
// ENDPOINTS: EPROC APPLICATION CONFIGURATION & ROLE SWITCHER (FASE 1)
// =============================================================================

app.MapGet("/api/eproc/application-configuration", (
    EprocApplicationConfigurationAppService configService,
    VmsCsmsAppService vmsService) =>
{
    var activePersona = vmsService.GetActivePersona();
    var currentUser = new EprocCurrentUserDto
    {
        IsAuthenticated = true,
        UserId = Guid.NewGuid(),
        UserName = activePersona.RoleCode.ToLowerInvariant(),
        FullName = activePersona.RoleName,
        Email = $"{activePersona.RoleCode.ToLowerInvariant()}@bumn.co.id",
        PositionGrade = activePersona.PositionGrade,
        Roles = [activePersona.RoleCode]
    };

    var dto = configService.GetConfiguration(currentUser);
    // Filter GrantedPolicies sesuai activePersona
    var grantedSet = new HashSet<string>(activePersona.GrantedPermissions, StringComparer.OrdinalIgnoreCase);
    foreach (var key in dto.Auth.GrantedPolicies.Keys.ToList())
    {
        if (!key.StartsWith("eproc.public.", StringComparison.OrdinalIgnoreCase))
        {
            dto.Auth.GrantedPolicies[key] = grantedSet.Contains(key);
        }
    }

    return Results.Ok(dto);
});

app.MapGet("/api/eproc/roles/personas", (VmsCsmsAppService vmsService) =>
{
    return Results.Ok(new
    {
        ActivePersona = vmsService.GetActivePersona(),
        Personas = vmsService.GetPersonas()
    });
});

app.MapPost("/api/eproc/roles/switch", (SwitchRoleRequestDto request, VmsCsmsAppService vmsService) =>
{
    var switched = vmsService.SwitchPersona(request.RoleCode);
    return Results.Ok(switched);
});

// =============================================================================
// ENDPOINTS: FASE 1 - VENDOR MANAGEMENT SYSTEM (VMS), CSMS & BO DETECTOR
// =============================================================================

app.MapGet("/api/eproc/vms/vendors", (VmsCsmsAppService vmsService) =>
{
    return Results.Ok(vmsService.GetAllVendors());
});

app.MapGet("/api/eproc/vms/vendors/{id:guid}", (Guid id, VmsCsmsAppService vmsService) =>
{
    var vendor = vmsService.GetVendorById(id);
    return vendor is not null ? Results.Ok(vendor) : Results.NotFound();
});

app.MapPost("/api/eproc/vms/vendors", (RegisterVendorRequestDto request, VmsCsmsAppService vmsService) =>
{
    var created = vmsService.RegisterVendor(request);
    return Results.Ok(created);
});

app.MapPost("/api/eproc/vms/vendors/{id:guid}/verify-avl", (
    Guid id,
    VerifyAvlRequestDto request,
    VmsCsmsAppService vmsService) =>
{
    var updated = vmsService.VerifyAndApproveVendorAvl(id, request.Approve, request.VerifierNotes);
    return Results.Ok(updated);
});

app.MapPost("/api/eproc/vms/vendors/{id:guid}/csms-submit", (
    Guid id,
    SubmitCsmsAssessmentRequestDto request,
    VmsCsmsAppService vmsService) =>
{
    var updated = vmsService.SubmitAndCertifyCsms(id, request);
    return Results.Ok(updated);
});

app.MapGet("/api/eproc/vms/bo-affiliation-conflicts", (VmsCsmsAppService vmsService) =>
{
    return Results.Ok(vmsService.DetectBeneficialOwnershipConflicts());
});

app.MapGet("/api/eproc/audit/hash-chain", (VmsCsmsAppService vmsService) =>
{
    return Results.Ok(new
    {
        IsChainIntegrityVerified = vmsService.IsAuditChainValid(),
        TotalRecords = vmsService.GetAuditChain().Count,
        Records = vmsService.GetAuditChain()
    });
});

// =============================================================================
// ENDPOINTS: FASE 2 - DEMAND PLANNING (RKAP, PR CONSOLIDATION, ENCRYPTED HPS, DOA INBOX)
// =============================================================================

app.MapGet("/api/eproc/planning/rkap-budgets", (PlanningAndDoaAppService planningService) =>
{
    return Results.Ok(planningService.GetRkapBudgets());
});

app.MapGet("/api/eproc/planning/purchase-requisitions", (PlanningAndDoaAppService planningService) =>
{
    return Results.Ok(planningService.GetPurchaseRequisitions());
});

app.MapPost("/api/eproc/planning/purchase-requisitions", (
    CreatePurchaseRequisitionRequestDto request,
    PlanningAndDoaAppService planningService) =>
{
    var created = planningService.CreatePurchaseRequisition(request);
    return Results.Ok(created);
});

app.MapGet("/api/eproc/planning/packages", (PlanningAndDoaAppService planningService) =>
{
    return Results.Ok(planningService.GetPackages());
});

app.MapPost("/api/eproc/planning/packages/consolidate", (
    ConsolidatePrToPackageRequestDto request,
    PlanningAndDoaAppService planningService) =>
{
    var pkg = planningService.ConsolidatePrsToPackage(request);
    return Results.Ok(pkg);
});

app.MapPost("/api/eproc/planning/packages/{id:guid}/seal-hps", (
    Guid id,
    SealPackageHpsRequestDto request,
    PlanningAndDoaAppService planningService) =>
{
    var pkg = planningService.SealPackageOwnerEstimateHps(id, request);
    return Results.Ok(pkg);
});

app.MapGet("/api/eproc/doa/inbox", (PlanningAndDoaAppService planningService) =>
{
    return Results.Ok(planningService.GetDoaInbox());
});

app.MapPost("/api/eproc/doa/inbox/{instanceId:guid}/act", (
    Guid instanceId,
    ActOnDoaTaskRequestDto request,
    PlanningAndDoaAppService planningService) =>
{
    var updated = planningService.ActOnDoaApprovalTask(instanceId, request);
    return Results.Ok(updated);
});

// =============================================================================
// ENDPOINTS: FASE 3 - E-SOURCING TENDER ENGINE, BID VAULT, HEA, E-AUCTION, BAFO & SANGGAH
// =============================================================================

app.MapGet("/api/eproc/sourcing/tenders", (ESourcingTenderAppService tenderService) =>
{
    return Results.Ok(tenderService.GetAllTenders());
});

app.MapGet("/api/eproc/sourcing/tenders/{id:guid}", (Guid id, ESourcingTenderAppService tenderService) =>
{
    var tender = tenderService.GetTenderById(id);
    return tender is not null ? Results.Ok(tender) : Results.NotFound();
});

app.MapPost("/api/eproc/sourcing/tenders/{id:guid}/aanwijzing", (
    Guid id,
    AskAanwijzingQuestionRequestDto request,
    ESourcingTenderAppService tenderService) =>
{
    return Results.Ok(tenderService.AddAanwijzingQa(id, request));
});

app.MapPost("/api/eproc/sourcing/tenders/{id:guid}/submit-bid", (
    Guid id,
    SubmitEncryptedVendorBidRequestDto request,
    ESourcingTenderAppService tenderService) =>
{
    return Results.Ok(tenderService.SubmitEncryptedVendorBid(id, request));
});

app.MapPost("/api/eproc/sourcing/tenders/{id:guid}/open-evaluate-hea", (
    Guid id,
    ESourcingTenderAppService tenderService) =>
{
    return Results.Ok(tenderService.ExecuteBidOpeningAndEvaluateHea(id));
});

app.MapPost("/api/eproc/sourcing/tenders/{id:guid}/eauction-bid", (
    Guid id,
    SubmitEAuctionBidRequestDto request,
    ESourcingTenderAppService tenderService) =>
{
    return Results.Ok(tenderService.SubmitEAuctionBid(id, request));
});

app.MapPost("/api/eproc/sourcing/tenders/{id:guid}/bafo-winner", (
    Guid id,
    FinalizeBafoAndWinnerRequestDto request,
    ESourcingTenderAppService tenderService) =>
{
    return Results.Ok(tenderService.FinalizeBafoAndAnnounceWinner(id, request));
});

app.MapPost("/api/eproc/sourcing/tenders/{id:guid}/sanggah", (
    Guid id,
    SubmitSanggahRequestDto request,
    ESourcingTenderAppService tenderService) =>
{
    return Results.Ok(tenderService.SubmitObjectionSanggah(id, request));
});

app.MapPost("/api/eproc/sourcing/tenders/{id:guid}/sanggah/resolve", (
    Guid id,
    ResolveSanggahRequestDto request,
    ESourcingTenderAppService tenderService) =>
{
    return Results.Ok(tenderService.ResolveObjectionSanggah(id, request));
});

// =============================================================================
// ENDPOINTS: FASE 4 - E-KATALOG (KONTRAK PAYUNG, TIERED PRICING, CALL-OFF ORDER) & E-CLM
// =============================================================================

app.MapGet("/api/eproc/catalog/umbrella-agreements", (ECatalogAndClmAppService clmService) =>
{
    return Results.Ok(clmService.GetUmbrellaAgreements());
});

app.MapGet("/api/eproc/catalog/items", (ECatalogAndClmAppService clmService) =>
{
    return Results.Ok(clmService.GetCatalogItems());
});

app.MapGet("/api/eproc/catalog/call-off-orders", (ECatalogAndClmAppService clmService) =>
{
    return Results.Ok(clmService.GetCallOffOrders());
});

app.MapPost("/api/eproc/catalog/call-off-orders", (
    CreateCallOffOrderRequestDto request,
    ECatalogAndClmAppService clmService) =>
{
    return Results.Ok(clmService.CreateCallOffOrder(request));
});

app.MapGet("/api/eproc/clm/contracts", (ECatalogAndClmAppService clmService) =>
{
    return Results.Ok(clmService.GetContracts());
});

app.MapGet("/api/eproc/clm/contracts/{id:guid}", (Guid id, ECatalogAndClmAppService clmService) =>
{
    return Results.Ok(clmService.GetContractById(id));
});

app.MapPost("/api/eproc/clm/contracts/{id:guid}/redlines", (
    Guid id,
    SubmitContractRedlineRequestDto request,
    ECatalogAndClmAppService clmService) =>
{
    return Results.Ok(clmService.SubmitContractRedline(id, request));
});

app.MapPost("/api/eproc/clm/contracts/{id:guid}/verify-performance-bond", (
    Guid id,
    VerifyPerformanceBondRequestDto request,
    ECatalogAndClmAppService clmService) =>
{
    return Results.Ok(clmService.VerifyPerformanceBond(id, request));
});

app.MapPost("/api/eproc/clm/contracts/{id:guid}/sign-tte-emeterai", (
    Guid id,
    SignContractTteRequestDto request,
    ECatalogAndClmAppService clmService) =>
{
    return Results.Ok(clmService.ExecuteDigitalSignatureAndEmeterai(id, request));
});

app.MapPost("/api/eproc/clm/contracts/{id:guid}/issue-spmk", (
    Guid id,
    IssueSpmkRequestDto request,
    ECatalogAndClmAppService clmService) =>
{
    return Results.Ok(clmService.IssueSpmk(id, request));
});

app.MapPost("/api/eproc/clm/contracts/{id:guid}/addendum-cco", (
    Guid id,
    SubmitAddendumCcoRequestDto request,
    ECatalogAndClmAppService clmService) =>
{
    return Results.Ok(clmService.SubmitContractAddendumCco(id, request));
});

// =============================================================================
// ENDPOINTS: FASE 5 - PROJECT EXECUTION (ASN/GR, KURVA S SCM, BAST POST-AUDIT TKDN)
//                     & FINANCE (3-WAY MATCHING E-INVOICE, PAJAK WAPU, BAP, VPE)
// =============================================================================

app.MapGet("/api/eproc/execution/asn-gr", (ProjectExecutionAndFinanceAppService execService) =>
{
    return Results.Ok(execService.GetAsnAndGrRecords());
});

app.MapPost("/api/eproc/execution/asn-gr", (
    CreateAsnAndGrRequestDto request,
    ProjectExecutionAndFinanceAppService execService) =>
{
    return Results.Ok(execService.RecordAsnAndGoodsReceipt(request));
});

app.MapGet("/api/eproc/execution/scurve-scm", (ProjectExecutionAndFinanceAppService execService) =>
{
    return Results.Ok(execService.GetSCurveRecords());
});

app.MapPost("/api/eproc/execution/scurve-scm", (
    EvaluateSCurveProgressRequestDto request,
    ProjectExecutionAndFinanceAppService execService) =>
{
    return Results.Ok(execService.EvaluateAndRecordSCurveProgress(request));
});

app.MapGet("/api/eproc/execution/bast", (ProjectExecutionAndFinanceAppService execService) =>
{
    return Results.Ok(execService.GetBastDocuments());
});

app.MapPost("/api/eproc/execution/bast", (
    ApproveBastWithPostAuditTkdnRequestDto request,
    ProjectExecutionAndFinanceAppService execService) =>
{
    return Results.Ok(execService.ApproveBastWithPostAuditTkdn(request));
});

app.MapGet("/api/eproc/finance/einvoices", (ProjectExecutionAndFinanceAppService execService) =>
{
    return Results.Ok(execService.GetEInvoiceSettlements());
});

app.MapPost("/api/eproc/finance/einvoices/match-and-settle", (
    SubmitVendorEInvoiceRequestDto request,
    ProjectExecutionAndFinanceAppService execService) =>
{
    return Results.Ok(execService.SubmitAndMatchVendorEInvoice(request));
});

app.MapGet("/api/eproc/vms/vpe-scorecards", (ProjectExecutionAndFinanceAppService execService) =>
{
    return Results.Ok(execService.GetVpeScorecards());
});

app.MapPost("/api/eproc/vms/vpe-scorecards", (
    EvaluateVendorVpeRequestDto request,
    ProjectExecutionAndFinanceAppService execService) =>
{
    return Results.Ok(execService.EvaluateVendorPerformanceVpe(request));
});

// =============================================================================
// ENDPOINTS: CORE CALCULATORS (HEA TKDN, FINANCIAL SKN, 3-WAY MATCHING, DOA)
// =============================================================================

app.MapPost("/api/eproc/evaluations/rank-hea", (
    RankHeaRequestDto request,
    TkdnHeaCalculatorService calculator) =>
{
    var results = calculator.RankCandidates(
        request.Candidates,
        request.HpsAmount,
        request.Category,
        request.TechnicalWeightPct,
        request.CommercialWeightPct);

    return Results.Ok(results);
});

app.MapPost("/api/eproc/vms/assess-qualification", (
    AssessVendorQualificationRequestDto request,
    VendorFinancialScoringService scoringService) =>
{
    var result = scoringService.AssessVendorForPackage(
        request.FinancialProfile,
        request.PackageHpsAmount,
        request.RequiredPackageCsmsLevel,
        request.VendorCertifiedCsmsLevel,
        request.RequireKdCheck);

    return Results.Ok(result);
});

app.MapPost("/api/eproc/finance/three-way-matching", (
    ThreeWayMatchingInput input,
    ThreeWayMatchingService matchingService) =>
{
    return Results.Ok(matchingService.CalculateInvoiceSettlement(input));
});

app.MapPost("/api/eproc/workflows/resolve-doa", (
    ResolveDoaRequestDto request,
    DoaMatrixApprovalEngine doaEngine) =>
{
    var sodCheck = doaEngine.ValidateSegregationOfDuties(request.CandidateUserRoles);
    var approvalSteps = doaEngine.ResolveApprovalRoute(request.TransactionAmountIdr, request.DocumentType);

    return Results.Ok(new
    {
        SodValidation = new { sodCheck.IsValid, sodCheck.ConflictReason },
        ApprovalRoute = approvalSteps
    });
});

app.Run();

public sealed record SwitchRoleRequestDto(string RoleCode);
public sealed record VerifyAvlRequestDto(bool Approve, string VerifierNotes);

public sealed record RankHeaRequestDto(
    List<BidEvaluationCandidate> Candidates,
    decimal HpsAmount,
    ProcurementCategory Category,
    decimal TechnicalWeightPct = 0m,
    decimal CommercialWeightPct = 100m
);

public sealed record AssessVendorQualificationRequestDto(
    VendorFinancialProfileInput FinancialProfile,
    decimal PackageHpsAmount,
    CsmsRiskLevel RequiredPackageCsmsLevel,
    CsmsRiskLevel? VendorCertifiedCsmsLevel,
    bool RequireKdCheck = true
);

public sealed record ResolveDoaRequestDto(
    decimal TransactionAmountIdr,
    string DocumentType,
    List<string> CandidateUserRoles
);
