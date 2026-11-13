using Eproc.Enums;

namespace Eproc.Planning;

public sealed record RkapBudgetDto(
    Guid Id,
    string CostCenterCode,
    string CostCenterName,
    int FiscalYear,
    string BudgetType, // "CAPEX" | "OPEX"
    string WbsElementGlAccount,
    string BudgetName,
    decimal RevisedAmount,
    decimal CommittedAmount,
    decimal ActualSpentAmount,
    decimal AvailableAmount
);

public sealed record PrItemDto(
    Guid Id,
    int LineNumber,
    string MaterialServiceCode,
    string ItemDescription,
    decimal Quantity,
    string Uom,
    decimal EstimatedUnitPrice,
    decimal TotalEstimatedPrice,
    string DeliveryBranchName
);

public sealed record PurchaseRequisitionDto(
    Guid Id,
    string PrNumber,
    string PrTitle,
    Guid RkapBudgetId,
    string WbsElementGlAccount,
    string RequesterUnitName,
    ProcurementCategory Category,
    decimal TotalEstimatedValue,
    DateOnly RequiredDeliveryDate,
    CsmsRiskLevel RequiredCsmsLevel,
    string Status, // "APPROVED_READY_FOR_SOURCING" | "CONSOLIDATED_TO_PACKAGE"
    Guid? ConsolidatedPackageId,
    List<PrItemDto> Items
);

public sealed record AhspComponentDto(
    string ComponentCategory, // "TENAGA_KERJA" | "BAHAN_MATERIAL" | "PERALATAN"
    string ComponentName,
    decimal Coefficient,
    string Uom,
    decimal UnitPrice
);

public sealed record PackageBoqItemDto(
    Guid Id,
    string WbsCode,
    string ItemName,
    decimal Quantity,
    string Uom,
    List<AhspComponentDto> AhspBreakdowns
);

public sealed record SealedOwnerEstimateDto(
    Guid PackageId,
    string CiphertextHexPreview,
    string NonceHex,
    string PayloadSha256Hash,
    DateTimeOffset SealedAtUtc,
    DateTimeOffset ScheduledOpeningUtc,
    bool IsOpenedAndDecrypted,
    decimal? DecryptedTotalHps
);

public sealed record DoaApprovalLogItemDto(
    int StepSequence,
    string ApproverRoleCode,
    string ApproverPositionGrade,
    string Action, // "PENDING" | "APPROVED" | "REJECTED"
    string? ActedByName,
    string? Comments,
    DateTimeOffset? ActedAtUtc
);

public sealed record DoaApprovalInstanceDto(
    Guid Id,
    string DocumentType, // "PACKAGE_AND_HPS_INITIATION" | "WINNER_DETERMINATION" | "CCO_ADDENDUM"
    Guid ReferenceId,
    string ReferenceCode,
    string ReferenceTitle,
    decimal TransactionAmountIdr,
    int CurrentStepSequence,
    int TotalStepsCount,
    string OverallStatus, // "IN_PROGRESS" | "APPROVED" | "REJECTED"
    List<DoaApprovalLogItemDto> Steps
);

public sealed record ProcurementPackageDto(
    Guid Id,
    string PackageCode,
    string PackageName,
    ProcurementCategory Category,
    SourcingMethod SourcingMethod,
    SubmissionEnvelopeMethod EnvelopeMethod,
    EvaluationMethod EvaluationMethod,
    ContractType ContractType,
    decimal TotalPaguBudget,
    CsmsRiskLevel RequiredCsmsLevel,
    decimal MinTkdnRequirementPct,
    bool EnableEReverseAuction,
    string Status, // "DRAFT_CONSOLIDATED" | "HPS_SEALED_WAITING_DOA" | "DOA_APPROVED_READY_FOR_TENDER"
    List<string> SourcePrNumbers,
    List<PackageBoqItemDto> BoqItems,
    SealedOwnerEstimateDto? SealedOwnerEstimate,
    Guid? DoaApprovalInstanceId
);

public sealed record CreatePurchaseRequisitionRequestDto(
    Guid RkapBudgetId,
    string PrTitle,
    string RequesterUnitName,
    ProcurementCategory Category,
    DateOnly RequiredDeliveryDate,
    CsmsRiskLevel RequiredCsmsLevel,
    List<PrItemDto> Items
);

public sealed record ConsolidatePrToPackageRequestDto(
    string PackageName,
    List<Guid> PurchaseRequisitionIds,
    SourcingMethod SourcingMethod,
    SubmissionEnvelopeMethod EnvelopeMethod,
    EvaluationMethod EvaluationMethod,
    ContractType ContractType,
    decimal MinTkdnRequirementPct,
    bool EnableEReverseAuction
);

public sealed record SealPackageHpsRequestDto(
    decimal TotalHpsPlaintextAmount,
    string CostHistoryNotes,
    int DaysUntilBidOpening
);

public sealed record ActOnDoaTaskRequestDto(
    string ActorRoleCode,
    string ActorPositionGrade,
    string ActorFullName,
    bool Approve,
    string Comments
);
