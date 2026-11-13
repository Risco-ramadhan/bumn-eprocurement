using System.Text.Json;
using Eproc.Configuration;
using Eproc.Enums;
using Eproc.Permissions;
using Eproc.Services;

namespace Eproc.Vms;

/// <summary>
/// Application Service untuk Fase 1:
/// - Manajemen Vendor (VMS), Verifikasi Legalitas & Persetujuan AVL
/// - Kalkulasi Otomatis Kemampuan Finansial (KD, MK, KN, SKN, SKP, Altman Z-Score)
/// - Deteksi Konflik Kepentingan & Beneficial Ownership (Anti-Persekongkolan Tender)
/// - Evaluasi 8 Elemen CSMS (Contractor Safety Management System) & Penerbitan Sertifikat Risiko HSE
/// - Pencatatan Otomatis ke Immutable Audit Trail (SHA-256 Hash-Chain)
/// </summary>
public sealed class VmsCsmsAppService
{
    private readonly VendorFinancialScoringService _financialService;
    private readonly AuditHashChainService _auditService;
    private readonly List<VendorDetailDto> _vendors = new();
    private readonly List<AuditChainRecord> _auditChain = new();
    private RolePersonaDto _activePersona;

    public VmsCsmsAppService(
        VendorFinancialScoringService financialService,
        AuditHashChainService auditService)
    {
        _financialService = financialService;
        _auditService = auditService;
        _activePersona = GetPersonas().First(p => p.RoleCode == "VMS_VERIFIER");
        SeedInitialVendorsAndAuditLogs();
    }

    public IReadOnlyList<VendorDetailDto> GetAllVendors() => _vendors.ToList();

    public VendorDetailDto? GetVendorById(Guid id) => _vendors.FirstOrDefault(v => v.Id == id);

    public IReadOnlyList<AuditChainRecord> GetAuditChain() => _auditChain.ToList();

    public bool IsAuditChainValid() => _auditService.VerifyChainIntegrity(_auditChain);

    public RolePersonaDto GetActivePersona() => _activePersona;

    public RolePersonaDto SwitchPersona(string roleCode)
    {
        var found = GetPersonas().FirstOrDefault(p =>
            string.Equals(p.RoleCode, roleCode, StringComparison.OrdinalIgnoreCase));

        if (found is null)
            throw new ArgumentException($"Role '{roleCode}' tidak ditemukan dalam matriks Role BUMN.");

        _activePersona = found;
        return _activePersona;
    }

    /// <summary>
    /// Mendaftarkan vendor baru, menghitung rasio finansial (KD, SKN, Altman Z-Score), dan mencatat Hash-Chain Audit Log.
    /// </summary>
    public VendorDetailDto RegisterVendor(RegisterVendorRequestDto input)
    {
        decimal kd = _financialService.CalculateKemampuanDasar(input.HighestExperienceNpt);
        var (mk, kn, skn, skp) = _financialService.CalculateSknAndSkp(
            input.BusinessScale,
            input.NetWorthEquityKb,
            input.TotalOngoingContractsValue,
            input.ActiveOngoingPackagesCount);

        var (zScore, healthZone) = _financialService.CalculateAltmanZScore(
            input.CurrentAssets,
            input.TotalAssets,
            input.CurrentLiabilities,
            input.TotalLiabilities,
            input.NetWorthEquityKb,
            input.RetainedEarnings,
            input.Ebit);

        var vendorId = Guid.NewGuid();
        string vendorCode = $"DRT-{DateTime.UtcNow:yyyy}-{_vendors.Count + 101:D4}";

        var legalDocs = new List<VendorLegalDocDto>
        {
            new(Guid.NewGuid(), "NIB_OSS", input.NibNumber, "Kementerian Investasi / BKPM", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)), null, "PENDING"),
            new(Guid.NewGuid(), "SPT_TAHUNAN", $"SPT-{DateTime.UtcNow.Year - 1}-{input.NpwpNumber[..6]}", "Direktorat Jenderal Pajak", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-4)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(8)), "PENDING"),
            new(Guid.NewGuid(), "SBU_KONSTRUKSI", $"SBU-LPJK-{_vendors.Count + 500}", "LPJK Kementerian PUPR", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)), "PENDING")
        };

        var newVendor = new VendorDetailDto(
            Id: vendorId,
            VendorCode: vendorCode,
            LegalEntityType: input.LegalEntityType,
            CompanyName: input.CompanyName,
            BusinessScale: input.BusinessScale.ToUpperInvariant(),
            VendorRoleTypes: input.VendorRoleTypes,
            IsBumnAffiliated: input.IsBumnAffiliated,
            IsPadiUmkm: input.IsPadiUmkm,
            NibNumber: input.NibNumber,
            NpwpNumber: input.NpwpNumber,
            IsPkp: input.IsPkp,
            CityName: input.CityName,
            CompanyEmail: input.CompanyEmail,
            VerificationStatus: VendorVerificationStatus.Submitted,
            CurrentVpeScore: 0m,
            CurrentVpeGrade: "UNRATED",
            NetWorthEquityKb: input.NetWorthEquityKb,
            HighestExperienceNpt: input.HighestExperienceNpt,
            KemampuanDasarKd: kd,
            ModalKerjaMk: mk,
            KemampuanNyataKn: kn,
            SisaKemampuanNyataSkn: skn,
            SisaKemampuanPaketSkp: skp,
            AltmanZScore: zScore,
            FinancialHealthZone: healthZone,
            Shareholders: input.Shareholders ?? [],
            BoardMembers: input.BoardMembers ?? [],
            LegalDocuments: legalDocs,
            ActiveCsmsCertificate: null
        );

        _vendors.Insert(0, newVendor);

        AppendAuditRecord(
            actorRole: "VENDOR_ADMIN",
            eventModule: "VMS",
            eventAction: "VENDOR_REGISTERED",
            entityTable: "vendors",
            entityId: vendorId,
            payload: new { newVendor.VendorCode, newVendor.CompanyName, newVendor.SisaKemampuanNyataSkn, newVendor.AltmanZScore });

        return newVendor;
    }

    /// <summary>
    /// Verifikasi dokumen legalitas oleh Verifikator VMS dan penerbitan status Approved Vendor List (VERIFIED_AVL).
    /// </summary>
    public VendorDetailDto VerifyAndApproveVendorAvl(Guid vendorId, bool approve, string verifierNotes)
    {
        int index = _vendors.FindIndex(v => v.Id == vendorId);
        if (index < 0)
            throw new KeyNotFoundException($"Vendor dengan ID {vendorId} tidak ditemukan.");

        var current = _vendors[index];
        var updatedDocs = current.LegalDocuments
            .Select(d => d with { VerificationStatus = approve ? "VALID" : "REJECTED" })
            .ToList();

        var newStatus = approve ? VendorVerificationStatus.VerifiedAvl : VendorVerificationStatus.UnderReview;
        var updated = current with
        {
            VerificationStatus = newStatus,
            CurrentVpeScore = current.CurrentVpeScore == 0m && approve ? 80.0m : current.CurrentVpeScore,
            CurrentVpeGrade = current.CurrentVpeGrade == "UNRATED" && approve ? "GOLD" : current.CurrentVpeGrade,
            LegalDocuments = updatedDocs
        };

        _vendors[index] = updated;

        AppendAuditRecord(
            actorRole: "VMS_VERIFIER",
            eventModule: "VMS",
            eventAction: approve ? "AVL_APPROVED" : "AVL_RETURNED",
            entityTable: "vendors",
            entityId: vendorId,
            payload: new { updated.VendorCode, Status = newStatus.ToString(), verifierNotes });

        return updated;
    }

    /// <summary>
    /// Evaluasi 8 Elemen CSMS (Contractor Safety Management System) oleh Auditor HSE BUMN:
    /// - Skor Dokumen (40%) + Skor Verifikasi Lapangan (60%) = Final CSMS Score
    /// - >= 85.00 : EXTREME Risk Qualified
    /// - 70.00 - 84.99 : HIGH Risk Qualified
    /// - 50.00 - 69.99 : MEDIUM Risk Qualified
    /// - < 50.00 : LOW Risk Qualified
    /// </summary>
    public VendorDetailDto SubmitAndCertifyCsms(Guid vendorId, SubmitCsmsAssessmentRequestDto request)
    {
        int index = _vendors.FindIndex(v => v.Id == vendorId);
        if (index < 0)
            throw new KeyNotFoundException($"Vendor dengan ID {vendorId} tidak ditemukan.");

        var current = _vendors[index];

        decimal totalWeight = request.Elements.Sum(e => e.WeightPct);
        decimal docScore = totalWeight > 0
            ? Math.Round(request.Elements.Sum(e => e.AuditorVerifiedScore * (e.WeightPct / totalWeight)), 2, MidpointRounding.AwayFromZero)
            : 0m;

        decimal fieldScore = Math.Clamp(request.FieldVerificationScore, 0m, 100m);
        decimal finalScore = Math.Round((docScore * 0.40m) + (fieldScore * 0.60m), 2, MidpointRounding.AwayFromZero);

        CsmsRiskLevel qualifiedLevel = finalScore switch
        {
            >= 85.00m => CsmsRiskLevel.Extreme,
            >= 70.00m => CsmsRiskLevel.High,
            >= 50.00m => CsmsRiskLevel.Medium,
            _ => CsmsRiskLevel.Low
        };

        var cert = new CsmsCertificateSummaryDto(
            CertificateNumber: $"CSMS-BUMN-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(1000, 9999)}",
            QualifiedRiskLevel: qualifiedLevel,
            DocScore: docScore,
            FieldAuditScore: fieldScore,
            FinalCsmsScore: finalScore,
            IssuedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ExpiredDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
            IsActive: true
        );

        var updated = current with { ActiveCsmsCertificate = cert };
        _vendors[index] = updated;

        AppendAuditRecord(
            actorRole: "HSE_CSMS_AUDITOR",
            eventModule: "CSMS",
            eventAction: "CSMS_CERTIFICATE_ISSUED",
            entityTable: "csms_certificates",
            entityId: vendorId,
            payload: new { cert.CertificateNumber, QualifiedLevel = qualifiedLevel.ToString(), cert.FinalCsmsScore, request.AuditorName });

        return updated;
    }

    /// <summary>
    /// Mendeteksi afiliasi tersembunyi (Beneficial Ownership / Pemegang Saham / Pengurus dengan NIK/NPWP yang sama)
    /// di antara daftar vendor untuk mencegah persekongkolan tender (Bid Rigging).
    /// </summary>
    public IReadOnlyList<AffiliationConflictItemDto> DetectBeneficialOwnershipConflicts(IEnumerable<Guid>? filterVendorIds = null)
    {
        var targetVendors = filterVendorIds is not null && filterVendorIds.Any()
            ? _vendors.Where(v => filterVendorIds.Contains(v.Id)).ToList()
            : _vendors;

        var conflicts = new List<AffiliationConflictItemDto>();

        for (int i = 0; i < targetVendors.Count; i++)
        {
            for (int j = i + 1; j < targetVendors.Count; j++)
            {
                var vA = targetVendors[i];
                var vB = targetVendors[j];

                // 1. Cek kesamaan NIK/NPWP Pemegang Saham (Beneficial Owner)
                foreach (var shA in vA.Shareholders)
                {
                    foreach (var shB in vB.Shareholders)
                    {
                        if (string.Equals(shA.IdentityNumberNikNpwp, shB.IdentityNumberNikNpwp, StringComparison.OrdinalIgnoreCase))
                        {
                            conflicts.Add(new AffiliationConflictItemDto(
                                vA.Id, vA.CompanyName,
                                vB.Id, vB.CompanyName,
                                "SHARED_ULTIMATE_BENEFICIAL_OWNER",
                                shA.FullName,
                                shA.IdentityNumberNikNpwp));
                        }
                    }
                }

                // 2. Cek kesamaan NIK Direksi / Komisaris
                foreach (var bmA in vA.BoardMembers)
                {
                    foreach (var bmB in vB.BoardMembers)
                    {
                        if (string.Equals(bmA.NikPassport, bmB.NikPassport, StringComparison.OrdinalIgnoreCase))
                        {
                            conflicts.Add(new AffiliationConflictItemDto(
                                vA.Id, vA.CompanyName,
                                vB.Id, vB.CompanyName,
                                "SHARED_BOARD_DIRECTOR",
                                bmA.FullName,
                                bmA.NikPassport));
                        }
                    }
                }
            }
        }

        return conflicts;
    }

    public IReadOnlyList<RolePersonaDto> GetPersonas() =>
    [
        new(
            RoleCode: "VMS_VERIFIER",
            RoleName: "Verifikator Kualifikasi & Legalitas Vendor (VMS)",
            PortalTarget: "INTERNAL_BUMN_PORTAL",
            PositionGrade: "BOD-3_MANAGER",
            ConflictingSodRoles: ["PPK", "POKJA_COMMITTEE", "VENDOR_ADMIN"],
            GrantedPermissions:
            [
                EprocPermissions.InternalBumnPortal.VmsAndCsmsAdmin.VerifyVendorLegal,
                EprocPermissions.InternalBumnPortal.VmsAndCsmsAdmin.EvaluateFinancialSknKd,
                EprocPermissions.InternalBumnPortal.VmsAndCsmsAdmin.ManageAvlCommodity,
                EprocPermissions.InternalBumnPortal.VmsAndCsmsAdmin.ManageSanctionsBlacklist,
                EprocPermissions.InternalBumnPortal.VendorPerformanceVpe.ViewVendorRatingLedger
            ]),
        new(
            RoleCode: "HSE_CSMS_AUDITOR",
            RoleName: "Auditor K3L / HSE (CSMS Certification)",
            PortalTarget: "INTERNAL_BUMN_PORTAL",
            PositionGrade: "BOD-3_MANAGER",
            ConflictingSodRoles: ["VENDOR_ADMIN"],
            GrantedPermissions:
            [
                EprocPermissions.InternalBumnPortal.VmsAndCsmsAdmin.AuditCsmsHse,
                EprocPermissions.InternalBumnPortal.VmsAndCsmsAdmin.ManageAvlCommodity,
                EprocPermissions.InternalBumnPortal.VendorPerformanceVpe.ScoreVendorPerformance
            ]),
        new(
            RoleCode: "PPK",
            RoleName: "Pejabat Pembuat Komitmen (Project Owner / Estimator)",
            PortalTarget: "INTERNAL_BUMN_PORTAL",
            PositionGrade: "BOD-2_VP_GM",
            ConflictingSodRoles: ["POKJA_COMMITTEE", "BUYER_PEJABAT_PENGADAAN", "VMS_VERIFIER", "FINANCE_AP", "VENDOR_ADMIN"],
            GrantedPermissions:
            [
                EprocPermissions.InternalBumnPortal.ExecutiveDashboard.RkapAndCostSaving,
                EprocPermissions.InternalBumnPortal.ExecutiveDashboard.TkdnAndUmkmAnalytics,
                EprocPermissions.InternalBumnPortal.ExecutiveDashboard.CriticalContractEarlyWarning,
                EprocPermissions.InternalBumnPortal.DoaInbox.ApproveTransactions,
                EprocPermissions.InternalBumnPortal.PlanningAndBudgeting.ManageRkapCostCenter,
                EprocPermissions.InternalBumnPortal.PlanningAndBudgeting.ManageDrpAnnual,
                EprocPermissions.InternalBumnPortal.PlanningAndBudgeting.CreatePurchaseRequisition,
                EprocPermissions.InternalBumnPortal.PlanningAndBudgeting.ConsolidatePackages,
                EprocPermissions.InternalBumnPortal.PlanningAndBudgeting.SealEncryptedOwnerEstimateHps,
                EprocPermissions.InternalBumnPortal.PlanningAndBudgeting.ViewPriceIntelligence,
                EprocPermissions.InternalBumnPortal.ContractLifecycle.IssueSppbj,
                EprocPermissions.InternalBumnPortal.ContractLifecycle.DraftAndRedlineContract,
                EprocPermissions.InternalBumnPortal.ContractLifecycle.SignDigitalContractTte,
                EprocPermissions.InternalBumnPortal.ContractLifecycle.VerifyGuaranteesBankBonds,
                EprocPermissions.InternalBumnPortal.ContractLifecycle.IssueSpmk,
                EprocPermissions.InternalBumnPortal.ContractLifecycle.ProcessAddendumCco,
                EprocPermissions.InternalBumnPortal.ProjectExecutionAndBast.MonitorScurveProgress,
                EprocPermissions.InternalBumnPortal.ProjectExecutionAndBast.IssueShowCauseMeetingScm,
                EprocPermissions.InternalBumnPortal.ProjectExecutionAndBast.ApproveServiceEntrySheetSes,
                EprocPermissions.InternalBumnPortal.ProjectExecutionAndBast.IssueBastPhoFho,
                EprocPermissions.InternalBumnPortal.ProjectExecutionAndBast.VerifyPostAuditTkdnAndPenalties,
                EprocPermissions.InternalBumnPortal.VendorPerformanceVpe.ScoreVendorPerformance
            ]),
        new(
            RoleCode: "POKJA_COMMITTEE",
            RoleName: "Kelompok Kerja Pemilihan (Panitia Tender UKPBJ)",
            PortalTarget: "INTERNAL_BUMN_PORTAL",
            PositionGrade: "COMMITTEE_SPECIALIST",
            ConflictingSodRoles: ["PA_KPA", "PPK", "FINANCE_AP", "VENDOR_ADMIN"],
            GrantedPermissions:
            [
                EprocPermissions.InternalBumnPortal.ExecutiveDashboard.TenderSlaMonitoring,
                EprocPermissions.InternalBumnPortal.VmsAndCsmsAdmin.EvaluateFinancialSknKd,
                EprocPermissions.InternalBumnPortal.VmsAndCsmsAdmin.ManageAvlCommodity,
                EprocPermissions.InternalBumnPortal.ESourcingTender.AssignCommittee,
                EprocPermissions.InternalBumnPortal.ESourcingTender.ManageRfpAndSchedule,
                EprocPermissions.InternalBumnPortal.ESourcingTender.ConductAanwijzing,
                EprocPermissions.InternalBumnPortal.ESourcingTender.OpenAndDecryptBidEnvelopes,
                EprocPermissions.InternalBumnPortal.ESourcingTender.EvaluateAdminTechQualification,
                EprocPermissions.InternalBumnPortal.ESourcingTender.CalculateCommercialHeaTkdn,
                EprocPermissions.InternalBumnPortal.ESourcingTender.HostEReverseAuction,
                EprocPermissions.InternalBumnPortal.ESourcingTender.ConductBafoNegotiation,
                EprocPermissions.InternalBumnPortal.ESourcingTender.ProposeAndAnnounceWinner,
                EprocPermissions.InternalBumnPortal.ESourcingTender.ResolveObjectionsSanggah
            ]),
        new(
            RoleCode: "PA_KPA",
            RoleName: "Direksi / Pengguna Anggaran (DOA Approver)",
            PortalTarget: "INTERNAL_BUMN_PORTAL",
            PositionGrade: "BOARD_OF_DIRECTORS",
            ConflictingSodRoles: ["POKJA_COMMITTEE", "VENDOR_ADMIN"],
            GrantedPermissions: EprocPermissions.GetAllPermissions().ToArray()),
        new(
            RoleCode: "FINANCE_AP",
            RoleName: "Verifikator Keuangan & Pajak WAPU BUMN",
            PortalTarget: "INTERNAL_BUMN_PORTAL",
            PositionGrade: "BOD-3_MANAGER",
            ConflictingSodRoles: ["PPK", "POKJA_COMMITTEE", "WAREHOUSE_QC", "VENDOR_ADMIN"],
            GrantedPermissions:
            [
                EprocPermissions.InternalBumnPortal.FinanceWapuAndPayment.VerifyThreeWayMatching,
                EprocPermissions.InternalBumnPortal.FinanceWapuAndPayment.VerifyTaxWapuAndEbupot,
                EprocPermissions.InternalBumnPortal.FinanceWapuAndPayment.IssueBapPaymentMinutes,
                EprocPermissions.InternalBumnPortal.FinanceWapuAndPayment.DisburseErpPayment
            ]),
        new(
            RoleCode: "INTERNAL_AUDITOR",
            RoleName: "Satuan Pengawasan Intern (SPI) / Auditor BPK",
            PortalTarget: "INTERNAL_BUMN_PORTAL",
            PositionGrade: "AUDITOR_INSPECTOR",
            ConflictingSodRoles: ["PA_KPA", "PPK", "POKJA_COMMITTEE", "FINANCE_AP", "VENDOR_ADMIN"],
            GrantedPermissions:
            [
                EprocPermissions.InternalBumnPortal.ExecutiveDashboard.RkapAndCostSaving,
                EprocPermissions.InternalBumnPortal.ExecutiveDashboard.TkdnAndUmkmAnalytics,
                EprocPermissions.InternalBumnPortal.ExecutiveDashboard.TenderSlaMonitoring,
                EprocPermissions.InternalBumnPortal.ExecutiveDashboard.CriticalContractEarlyWarning,
                EprocPermissions.InternalBumnPortal.SystemAdminAndAudit.InspectImmutableAuditChain
            ]),
        new(
            RoleCode: "VENDOR_ADMIN",
            RoleName: "Direktur / Admin Perusahaan Penyedia (Portal Vendor)",
            PortalTarget: "VENDOR_PORTAL",
            PositionGrade: "EXTERNAL_PROVIDER",
            ConflictingSodRoles: ["PA_KPA", "PPK", "POKJA_COMMITTEE", "VMS_VERIFIER", "FINANCE_AP", "INTERNAL_AUDITOR"],
            GrantedPermissions:
            [
                EprocPermissions.VendorPortal.Dashboard,
                EprocPermissions.VendorPortal.VmsProfile,
                EprocPermissions.VendorPortal.CsmsQualification,
                EprocPermissions.VendorPortal.TenderBidding,
                EprocPermissions.VendorPortal.EReverseAuctionRoom,
                EprocPermissions.VendorPortal.ECatalogStore,
                EprocPermissions.VendorPortal.ContractAndBonds,
                EprocPermissions.VendorPortal.ExecutionAndBast,
                EprocPermissions.VendorPortal.EInvoicing,
                EprocPermissions.VendorPortal.VpeScorecard
            ])
    ];

    private void AppendAuditRecord(
        string actorRole,
        string eventModule,
        string eventAction,
        string entityTable,
        Guid entityId,
        object payload)
    {
        string? prevHash = _auditChain.Count > 0 ? _auditChain[^1].CurrentRecordHash : null;
        string json = JsonSerializer.Serialize(payload);
        var record = _auditService.CreateLinkedRecord(
            actorUserId: Guid.NewGuid(),
            actorRole: actorRole,
            eventModule: eventModule,
            eventAction: eventAction,
            entityTable: entityTable,
            entityId: entityId,
            payloadJson: json,
            previousHash: prevHash);

        _auditChain.Add(record);
    }

    private void SeedInitialVendorsAndAuditLogs()
    {
        // 1. Vendor BUMN Synergy / Kontraktor EPC Besar (High CSMS, Safe Financial Zone)
        var v1 = RegisterVendor(new RegisterVendorRequestDto(
            LegalEntityType: "PT",
            CompanyName: "PT Karya Infrastruktur Nusantara Tbk",
            BusinessScale: "BESAR",
            VendorRoleTypes: ["CONTRACTOR", "MANUFACTURER"],
            IsBumnAffiliated: true,
            IsPadiUmkm: false,
            NibNumber: "9120300451821",
            NpwpNumber: "01.302.441.8-093.000",
            IsPkp: true,
            CityName: "Jakarta Selatan",
            CompanyEmail: "tender@karyainfrastruktur.co.id",
            CurrentAssets: 85_000_000_000m,
            TotalAssets: 160_000_000_000m,
            CurrentLiabilities: 30_000_000_000m,
            TotalLiabilities: 55_000_000_000m,
            NetWorthEquityKb: 105_000_000_000m,
            RetainedEarnings: 42_000_000_000m,
            Ebit: 28_000_000_000m,
            HighestExperienceNpt: 45_000_000_000m,
            TotalOngoingContractsValue: 90_000_000_000m,
            ActiveOngoingPackagesCount: 2,
            Shareholders:
            [
                new("CORPORATE", "PT Holding Karya Persero", "01.000.111.2-093.000", 65.0m, true, false),
                new("INDIVIDUAL", "Ir. Hendra Wijaya, M.Eng", "3174011508750001", 35.0m, true, false)
            ],
            BoardMembers:
            [
                new("PRESIDENT_DIRECTOR", "Ir. Hendra Wijaya, M.Eng", "3174011508750001", true)
            ]));

        VerifyAndApproveVendorAvl(v1.Id, true, "Dokumen legalitas, SBU B2, dan Neraca KAP WTP lengkap.");
        SubmitAndCertifyCsms(v1.Id, new SubmitCsmsAssessmentRequestDto(
            TargetRiskCategory: CsmsRiskLevel.Extreme,
            Elements:
            [
                new(1, "Kepemimpinan & Komitmen Manajemen K3L", 15m, 92m, 90m),
                new(2, "Kebijakan & Sasaran Strategis HSE", 10m, 90m, 88m),
                new(3, "Organisasi, Kompetensi AK3 & Sumber Daya", 15m, 88m, 87m),
                new(4, "Manajemen Risiko (HIRADC & JSA)", 15m, 95m, 92m),
                new(5, "Prosedur Operasi & Izin Kerja Aman (PTW)", 15m, 90m, 89m),
                new(6, "Pemantauan Kinerja K3L & Zero Fatality", 10m, 94m, 91m),
                new(7, "Tanggap Darurat (ERP) & Investigasi Insiden", 10m, 88m, 86m),
                new(8, "Audit Internal SMK3 & ISO 45001", 10m, 92m, 90m)
            ],
            FieldVerificationScore: 91.5m,
            AuditorName: "Budi Santoso, S.T. (Lead Auditor HSE)"));

        // 2. Vendor PaDi UMKM Menengah (Memiliki Beneficial Owner yang sama dengan Vendor 3 untuk demo Detektor Konflik Kepentingan!)
        var v2 = RegisterVendor(new RegisterVendorRequestDto(
            LegalEntityType: "PT",
            CompanyName: "PT Mitra Teknik Manufaktur Lokal",
            BusinessScale: "MENENGAH",
            VendorRoleTypes: ["MANUFACTURER", "AUTHORIZED_DISTRIBUTOR"],
            IsBumnAffiliated: false,
            IsPadiUmkm: true,
            NibNumber: "8120019283741",
            NpwpNumber: "02.415.778.1-412.000",
            IsPkp: true,
            CityName: "Surabaya",
            CompanyEmail: "sales@mitratekniklokal.co.id",
            CurrentAssets: 14_000_000_000m,
            TotalAssets: 24_000_000_000m,
            CurrentLiabilities: 5_000_000_000m,
            TotalLiabilities: 9_000_000_000m,
            NetWorthEquityKb: 15_000_000_000m,
            RetainedEarnings: 6_500_000_000m,
            Ebit: 4_200_000_000m,
            HighestExperienceNpt: 8_000_000_000m,
            TotalOngoingContractsValue: 10_000_000_000m,
            ActiveOngoingPackagesCount: 1,
            Shareholders:
            [
                new("INDIVIDUAL", "Surya Darmawan, S.E.", "3578042209800003", 80.0m, true, false)
            ],
            BoardMembers:
            [
                new("PRESIDENT_DIRECTOR", "Surya Darmawan, S.E.", "3578042209800003", true)
            ]));

        VerifyAndApproveVendorAvl(v2.Id, true, "Terverifikasi PaDi UMKM & Sertifikat TKDN Kemenperin 62%.");
        SubmitAndCertifyCsms(v2.Id, new SubmitCsmsAssessmentRequestDto(
            TargetRiskCategory: CsmsRiskLevel.High,
            Elements:
            [
                new(1, "Kepemimpinan & Komitmen Manajemen K3L", 100m, 78m, 76m)
            ],
            FieldVerificationScore: 78.0m,
            AuditorName: "Budi Santoso, S.T. (Lead Auditor HSE)"));

        // 3. Vendor Baru (Menunggu Verifikasi VMS & Memiliki Afiliasi Beneficial Owner dengan PT Mitra Teknik Manufaktur Lokal)
        RegisterVendor(new RegisterVendorRequestDto(
            LegalEntityType: "CV",
            CompanyName: "CV Cipta Sarana Elektrikal",
            BusinessScale: "KECIL",
            VendorRoleTypes: ["CONTRACTOR", "AGENT"],
            IsBumnAffiliated: false,
            IsPadiUmkm: true,
            NibNumber: "0220109928312",
            NpwpNumber: "03.889.112.4-412.000",
            IsPkp: true,
            CityName: "Sidoarjo",
            CompanyEmail: "admin@ciptasarana.id",
            CurrentAssets: 3_200_000_000m,
            TotalAssets: 5_000_000_000m,
            CurrentLiabilities: 1_800_000_000m,
            TotalLiabilities: 2_800_000_000m,
            NetWorthEquityKb: 2_200_000_000m,
            RetainedEarnings: 700_000_000m,
            Ebit: 550_000_000m,
            HighestExperienceNpt: 1_500_000_000m,
            TotalOngoingContractsValue: 800_000_000m,
            ActiveOngoingPackagesCount: 1,
            Shareholders:
            [
                // NIK sama dengan pemilik PT Mitra Teknik Manufaktur Lokal -> Terdeteksi otomatis oleh Anti-Bid-Rigging Engine!
                new("INDIVIDUAL", "Surya Darmawan, S.E.", "3578042209800003", 55.0m, true, false)
            ],
            BoardMembers:
            [
                new("PRESIDENT_DIRECTOR", "Rian Pratama", "3515081103880002", true)
            ]));
    }
}
