using Eproc.Enums;

namespace Eproc.CatalogAndClm;

// ============================================================================
// BAGIAN 1: E-KATALOG INTERNAL, KONTRAK PAYUNG & CALL-OFF ORDER (E-PURCHASING)
// ============================================================================

public sealed record UmbrellaAgreementDto(
    Guid Id,
    string AgreementNumber,
    string Title,
    Guid VendorId,
    string VendorName,
    DateOnly ValidFrom,
    DateOnly ValidUntil,
    decimal TotalQuotaCeiling,
    decimal ConsumedQuotaAmount,
    decimal RemainingQuotaAmount,
    string Status // "ACTIVE" | "QUOTA_EXHAUSTED" | "EXPIRED"
);

public sealed record CatalogItemTierPriceDto(
    int MinQty,
    int? MaxQty,
    decimal UnitPrice,
    decimal DiscountPct
);

public sealed record CatalogItemDto(
    Guid Id,
    Guid UmbrellaAgreementId,
    string AgreementNumber,
    string KimapCode,
    string ItemName,
    ProcurementCategory Category,
    string VendorName,
    string Uom,
    decimal VerifiedTkdnPct,
    string TkdnCertificateNo,
    int LeadTimeDays,
    List<CatalogItemTierPriceDto> TierPrices
);

public sealed record CallOffOrderDto(
    Guid Id,
    string CallOffPoNumber,
    Guid UmbrellaAgreementId,
    Guid CatalogItemId,
    string KimapCode,
    string ItemName,
    string VendorName,
    string OrderingBranchCode,
    int OrderedQty,
    string Uom,
    decimal AppliedTierUnitPrice,
    decimal AppliedDiscountPct,
    decimal TotalOrderAmount,
    string Status, // "ISSUED_AND_QUOTA_DEDUCTED"
    DateTimeOffset OrderedAtUtc
);

public sealed record CreateCallOffOrderRequestDto(
    Guid CatalogItemId,
    string OrderingBranchCode,
    int OrderedQty
);

// ============================================================================
// BAGIAN 2: MANAJEMEN KONTRAK SIKLUS PENUH (e-CLM)
// ============================================================================

public sealed record ContractRedlineVersionDto(
    int VersionNumber,
    string VersionLabel, // "v1.0 Draft Awal" | "v1.1 Revisi Vendor" | "v2.0 Final Legal BUMN"
    string AuthorRole,   // "LEGAL_BUMN" | "VENDOR_LEGAL"
    string AuthorName,
    string ClauseReference,
    string ChangeSummaryNotes,
    DateTimeOffset CreatedAtUtc
);

public sealed record PerformanceBondGuaranteeDto(
    string GuaranteeNumber,
    string IssuingBankName,
    decimal BondAmount,
    decimal RequiredMinimumBondAmount,
    decimal BondToContractPct,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    string VerificationStatus, // "VERIFIED_ACTIVE" | "REJECTED_INSUFFICIENT"
    DateTimeOffset VerifiedAtUtc
);

public sealed record ContractDigitalSignatureDto(
    string EmeteraiSerialNumber,
    string BumnSignerName,
    string BumnSignerTitle,
    string BumnTteCertificateSerial,
    string VendorSignerName,
    string VendorSignerTitle,
    string VendorTteCertificateSerial,
    string SignedPdfSha256Hash,
    DateTimeOffset SignedAtUtc
);

public sealed record ContractChangeOrderCcoDto(
    Guid Id,
    int AddendumSequence,
    string AddendumNumber,
    string ChangeType, // "VOLUME_ADDITION_CCO" | "SCOPE_REDUCTION" | "TIME_EXTENSION"
    string JustificationReason,
    decimal PreviousContractAmount,
    decimal DeltaAmount,
    decimal NewContractAmount,
    decimal CumulativeIncreasePct,
    int AdditionalDurationDays,
    bool ApprovedByBoardOverride,
    DateTimeOffset ApprovedAtUtc
);

public sealed record ProcurementContractDto(
    Guid Id,
    Guid? SourceTenderId,
    string SppbjNumber,
    DateOnly SppbjDate,
    string ContractNumber,
    string ContractTitle,
    Guid VendorId,
    string VendorName,
    ProcurementCategory Category,
    decimal ReferenceHpsAmount,
    decimal InitialContractAmount,
    decimal CurrentActiveContractAmount,
    decimal CommittedTkdnPct,
    bool IsBelow80PercentHps,
    decimal RequiredPerformanceBondAmount,
    string CurrentLifecycleStatus,
    // "SPPBJ_ISSUED" | "REDLINING_NEGOTIATION" | "PERFORMANCE_BOND_VERIFIED"
    // | "SIGNED_TTE_EMETERAI" | "SPMK_ISSUED_ACTIVE" | "ADDENDUM_AMENDED"
    string? SpmkNumber,
    DateOnly? SpmkEffectiveStartDate,
    DateOnly? TargetCompletionDate,
    List<ContractRedlineVersionDto> RedlineHistory,
    PerformanceBondGuaranteeDto? PerformanceBond,
    ContractDigitalSignatureDto? DigitalSignature,
    List<ContractChangeOrderCcoDto> AddendumsCco
);

public sealed record SubmitContractRedlineRequestDto(
    string AuthorRole,
    string AuthorName,
    string ClauseReference,
    string ChangeSummaryNotes
);

public sealed record VerifyPerformanceBondRequestDto(
    string GuaranteeNumber,
    string IssuingBankName,
    decimal BondAmount,
    DateOnly ExpiryDate
);

public sealed record SignContractTteRequestDto(
    string EmeteraiSerialNumber,
    string BumnSignerName,
    string BumnSignerTitle,
    string VendorSignerName,
    string VendorSignerTitle
);

public sealed record IssueSpmkRequestDto(
    string SpmkNumber,
    DateOnly EffectiveStartDate,
    int ExecutionDurationDays
);

public sealed record SubmitAddendumCcoRequestDto(
    string AddendumNumber,
    string ChangeType,
    string JustificationReason,
    decimal DeltaAmount,
    int AdditionalDurationDays,
    bool HasBoardExceptionOverride = false
);
