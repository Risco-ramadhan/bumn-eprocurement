using Eproc.Enums;

namespace Eproc.ExecutionAndFinance;

// ============================================================================
// BAGIAN 1: EKSEKUSI PROYEK, EXPEDITING (ASN/GR), KURVA S (SCM I/II/III), BAST & POST-AUDIT TKDN
// ============================================================================

public sealed record AsnShipmentAndGrDto(
    Guid Id,
    Guid ContractId,
    string ContractNumber,
    string AsnNumber,
    string GrNumber,
    string VendorName,
    string ItemName,
    int ShippedQty,
    int AcceptedQty,
    int RejectedDefectiveQty,
    string Uom,
    decimal UnitPrice,
    decimal ApprovedGrGrossAmount,
    string QcInspectorNotes,
    DateTimeOffset InspectedAtUtc
);

public sealed record SCurveProgressAndScmDto(
    Guid Id,
    Guid ContractId,
    string ContractNumber,
    int WeekNumber,
    decimal PlannedCumulativePct,
    decimal ActualCumulativePct,
    decimal DeviationPct,
    bool IsCriticalContract,
    ScmLevel TriggeredScmLevel,
    string ActionPlanNotes,
    DateTimeOffset EvaluatedAtUtc
);

public sealed record PostAuditTkdnVerificationDto(
    string SurveyorInstitution, // "PT Surveyor Indonesia" | "PT Sucofindo"
    string VerificationReportNo,
    decimal CommittedTkdnPct,
    decimal ActualVerifiedTkdnPct,
    decimal ShortfallPct,
    decimal FinancialSanctionAmount
);

public sealed record BastHandoverDocumentDto(
    Guid Id,
    Guid ContractId,
    string ContractNumber,
    string BastNumber,
    string HandoverType, // "BAST_GOODS_100" | "PHO_CONSTRUCTION_95" | "FHO_RETENTION_5"
    string VendorName,
    ProcurementCategory Category,
    decimal ContractTotalValue,
    decimal ApprovedGrossValue,
    int DelayCalendarDays,
    PostAuditTkdnVerificationDto PostAuditTkdn,
    string Status, // "APPROVED_READY_FOR_INVOICE"
    DateTimeOffset ApprovedAtUtc
);

// ============================================================================
// BAGIAN 2: 3-WAY MATCHING E-INVOICING, PAJAK WAPU BUMN, BAP & SP2D
// ============================================================================

public sealed record VendorEInvoiceSettlementDto(
    Guid Id,
    string InvoiceNumber,
    string TaxInvoiceFakturNo, // Wajib kode "030." untuk transaksi WAPU BUMN
    string BapDocumentNumber,
    Guid ContractId,
    string ContractNumber,
    Guid BastId,
    string BastNumber,
    string VendorName,
    decimal ContractTotalValue,
    decimal ApprovedBastGrossValue,
    decimal VendorInvoiceClaimedDpp,
    bool IsThreeWayMatched,
    string MatchDiscrepancyNotes,
    decimal DpRecoupmentDeduction,
    decimal RetentionDeductionAmount,
    int DelayCalendarDays,
    decimal CappedLatePenaltyAmount,
    decimal TkdnShortfallPenaltyAmount,
    decimal TotalPenaltyDeduction,
    decimal DppNetAfterDeductions,
    decimal PpnAmount,
    bool IsPpnWithheldByWapu,
    PphTaxType PphType,
    decimal PphWithheldAmount,
    decimal NetPayableToVendor,
    string PaymentStatus, // "THREE_WAY_MATCHED_BAP_ISSUED" | "BLOCKED_MISMATCH"
    DateTimeOffset ProcessedAtUtc
);

// ============================================================================
// BAGIAN 3: VENDOR PERFORMANCE EVALUATION (VPE / RAPOR KINERJA PENYEDIA)
// ============================================================================

public sealed record VendorPerformanceScorecardDto(
    Guid Id,
    Guid ContractId,
    string ContractNumber,
    Guid VendorId,
    string VendorName,
    decimal QualityScore,            // Bobot 30%
    decimal DeliveryTimelinessScore, // Bobot 25%
    decimal HseCsmsComplianceScore,  // Bobot 20%
    decimal TkdnComplianceScore,     // Bobot 15%
    decimal ResponsivenessScore,     // Bobot 10%
    decimal FinalWeightedVpeScore,
    string VpeGrade,                 // "PLATINUM" | "GOLD" | "SILVER" | "RED_WARNING" | "BLACKLIST_CANDIDATE"
    string AvlActionRecommendation,  // "MAINTAIN_AVL_PRIORITY" | "COACHING_REQUIRED" | "SUSPEND_FROM_AVL"
    DateTimeOffset EvaluatedAtUtc
);

// ============================================================================
// REQUEST DTOs
// ============================================================================

public sealed record CreateAsnAndGrRequestDto(
    Guid ContractId,
    string ContractNumber,
    string AsnNumber,
    string VendorName,
    string ItemName,
    int ShippedQty,
    int AcceptedQty,
    string Uom,
    decimal UnitPrice,
    string QcInspectorNotes
);

public sealed record EvaluateSCurveProgressRequestDto(
    Guid ContractId,
    string ContractNumber,
    int WeekNumber,
    decimal PlannedCumulativePct,
    decimal ActualCumulativePct,
    ScmLevel CurrentActiveScmLevel,
    string ActionPlanNotes
);

public sealed record ApproveBastWithPostAuditTkdnRequestDto(
    Guid ContractId,
    string ContractNumber,
    string BastNumber,
    string HandoverType,
    string VendorName,
    ProcurementCategory Category,
    decimal ContractTotalValue,
    decimal ApprovedGrossValue,
    int DelayCalendarDays,
    string SurveyorInstitution,
    string VerificationReportNo,
    decimal CommittedTkdnPct,
    decimal ActualVerifiedTkdnPct
);

public sealed record SubmitVendorEInvoiceRequestDto(
    Guid BastId,
    string InvoiceNumber,
    string TaxInvoiceFakturNo,
    decimal VendorInvoiceClaimedDpp,
    decimal DownPaymentRecoupmentPct,
    decimal RetentionDeductionPct,
    bool IsClientWapuPpn,
    decimal PpnRatePct,
    PphTaxType PphType,
    decimal PphRatePct
);

public sealed record EvaluateVendorVpeRequestDto(
    Guid ContractId,
    string ContractNumber,
    Guid VendorId,
    string VendorName,
    decimal QualityScore,
    decimal DeliveryTimelinessScore,
    decimal HseCsmsComplianceScore,
    decimal TkdnComplianceScore,
    decimal ResponsivenessScore
);
