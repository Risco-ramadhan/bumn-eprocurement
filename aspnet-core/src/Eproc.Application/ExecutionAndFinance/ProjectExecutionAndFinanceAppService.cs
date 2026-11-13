using Eproc.Configuration;
using Eproc.Enums;
using Eproc.Services;

namespace Eproc.ExecutionAndFinance;

/// <summary>
/// Application Service Fase 5 (Penutup Siklus Source-to-Pay / S2P):
/// 1. Expediting Pengiriman Barang (ASN) & Pemeriksaan Fisik Gudang (Goods Receipt / GR & QC Rejection).
/// 2. Pemantauan Kurva S Konstruksi/EPC & Eskalasi Otomatis Show Cause Meeting (SCM Tingkat I, II, III).
/// 3. Penerbitan BAST / PHO / FHO & Post-Audit Verifikasi Realisasi TKDN (beserta Sanksi Finansial TKDN).
/// 4. 3-Way Matching e-Invoicing (Kontrak vs BAST/GR vs e-Invoice), Potongan DP, Retensi 5%, Denda 1‰ (Maks 5%),
///    Pemungutan Pajak WAPU BUMN (PPN 11% Faktur 030.xxx & PPh), serta Penerbitan Berita Acara Pembayaran (BAP).
/// 5. Rapor Kinerja Penyedia Akhir Kontrak (Vendor Performance Evaluation / VPE 5 Dimensi).
/// </summary>
public sealed class ProjectExecutionAndFinanceAppService
{
    private readonly ThreeWayMatchingService _threeWayMatching;
    private readonly TkdnHeaCalculatorService _tkdnCalculator;
    private readonly AuditHashChainService _auditAndScmService;

    private readonly List<AsnShipmentAndGrDto> _asnGrRecords = [];
    private readonly List<SCurveProgressAndScmDto> _sCurveRecords = [];
    private readonly List<BastHandoverDocumentDto> _bastDocuments = [];
    private readonly List<VendorEInvoiceSettlementDto> _eInvoices = [];
    private readonly List<VendorPerformanceScorecardDto> _vpeScorecards = [];

    public ProjectExecutionAndFinanceAppService(
        ThreeWayMatchingService? threeWayMatching = null,
        TkdnHeaCalculatorService? tkdnCalculator = null,
        AuditHashChainService? auditAndScmService = null)
    {
        _threeWayMatching = threeWayMatching ?? new ThreeWayMatchingService();
        _tkdnCalculator = tkdnCalculator ?? new TkdnHeaCalculatorService();
        _auditAndScmService = auditAndScmService ?? new AuditHashChainService();

        SeedInitialExecutionAndFinanceData();
    }

    public IReadOnlyList<AsnShipmentAndGrDto> GetAsnAndGrRecords() => _asnGrRecords;

    public IReadOnlyList<SCurveProgressAndScmDto> GetSCurveRecords() => _sCurveRecords;

    public IReadOnlyList<BastHandoverDocumentDto> GetBastDocuments() => _bastDocuments;

    public IReadOnlyList<VendorEInvoiceSettlementDto> GetEInvoiceSettlements() => _eInvoices;

    public IReadOnlyList<VendorPerformanceScorecardDto> GetVpeScorecards() => _vpeScorecards;

    /// <summary>
    /// Mencatat pengiriman barang (ASN) & penerimaan fisik gudang (Goods Receipt / GR):
    /// - Menghitung otomatis barang yang ditolak QC (RejectedDefectiveQty = ShippedQty - AcceptedQty).
    /// - Hanya barang yang lulus QC (AcceptedQty) yang diakui dalam ApprovedGrGrossAmount.
    /// </summary>
    public AsnShipmentAndGrDto RecordAsnAndGoodsReceipt(CreateAsnAndGrRequestDto input)
    {
        if (input.AcceptedQty < 0 || input.AcceptedQty > input.ShippedQty)
            throw new ArgumentOutOfRangeException(nameof(input.AcceptedQty), "Kuantitas diterima QC tidak valid.");

        int rejectedQty = input.ShippedQty - input.AcceptedQty;
        decimal approvedGross = Math.Round(input.AcceptedQty * input.UnitPrice, 2, MidpointRounding.AwayFromZero);

        var record = new AsnShipmentAndGrDto(
            Id: Guid.NewGuid(),
            ContractId: input.ContractId,
            ContractNumber: input.ContractNumber,
            AsnNumber: input.AsnNumber,
            GrNumber: $"GR-WH/2026/{_asnGrRecords.Count + 1:D4}",
            VendorName: input.VendorName,
            ItemName: input.ItemName,
            ShippedQty: input.ShippedQty,
            AcceptedQty: input.AcceptedQty,
            RejectedDefectiveQty: rejectedQty,
            Uom: input.Uom,
            UnitPrice: input.UnitPrice,
            ApprovedGrGrossAmount: approvedGross,
            QcInspectorNotes: input.QcInspectorNotes,
            InspectedAtUtc: DateTimeOffset.UtcNow
        );

        _asnGrRecords.Add(record);
        return record;
    }

    /// <summary>
    /// Mengevaluasi progres mingguan Kurva S Konstruksi/EPC dan mendeteksi Kontrak Kritis:
    /// - Periode Rencana 0% - 70%: Kritis bila deviasi <= -10%
    /// - Periode Rencana 70% - 100%: Kritis bila deviasi <= -5%
    /// - Otomatis menaikkan level Show Cause Meeting (None -> ScmLevel1 -> ScmLevel2 -> ScmLevel3).
    /// </summary>
    public SCurveProgressAndScmDto EvaluateAndRecordSCurveProgress(EvaluateSCurveProgressRequestDto input)
    {
        var (deviation, isCritical, recommendedScm) = _auditAndScmService.EvaluateScurveDeviation(
            input.PlannedCumulativePct,
            input.ActualCumulativePct,
            input.CurrentActiveScmLevel);

        var record = new SCurveProgressAndScmDto(
            Id: Guid.NewGuid(),
            ContractId: input.ContractId,
            ContractNumber: input.ContractNumber,
            WeekNumber: input.WeekNumber,
            PlannedCumulativePct: input.PlannedCumulativePct,
            ActualCumulativePct: input.ActualCumulativePct,
            DeviationPct: deviation,
            IsCriticalContract: isCritical,
            TriggeredScmLevel: recommendedScm,
            ActionPlanNotes: input.ActionPlanNotes,
            EvaluatedAtUtc: DateTimeOffset.UtcNow
        );

        _sCurveRecords.Add(record);
        return record;
    }

    /// <summary>
    /// Mengesahkan Berita Acara Serah Terima (BAST / PHO / FHO) sekaligus memverifikasi hasil Post-Audit TKDN:
    /// - Apabila realisasi TKDN hasil audit surveyor independen lebih rendah dari komitmen kontrak,
    ///   sistem menghitung otomatis Sanksi Finansial Kekurangan TKDN (FinancialSanctionAmount).
    /// </summary>
    public BastHandoverDocumentDto ApproveBastWithPostAuditTkdn(ApproveBastWithPostAuditTkdnRequestDto input)
    {
        decimal shortfall = Math.Max(0m, input.CommittedTkdnPct - input.ActualVerifiedTkdnPct);
        decimal tkdnSanction = _tkdnCalculator.CalculatePostAuditTkdnSanction(
            input.ContractTotalValue,
            input.CommittedTkdnPct,
            input.ActualVerifiedTkdnPct,
            input.Category);

        var postAudit = new PostAuditTkdnVerificationDto(
            SurveyorInstitution: input.SurveyorInstitution,
            VerificationReportNo: input.VerificationReportNo,
            CommittedTkdnPct: input.CommittedTkdnPct,
            ActualVerifiedTkdnPct: input.ActualVerifiedTkdnPct,
            ShortfallPct: shortfall,
            FinancialSanctionAmount: tkdnSanction
        );

        var bast = new BastHandoverDocumentDto(
            Id: Guid.NewGuid(),
            ContractId: input.ContractId,
            ContractNumber: input.ContractNumber,
            BastNumber: input.BastNumber,
            HandoverType: input.HandoverType,
            VendorName: input.VendorName,
            Category: input.Category,
            ContractTotalValue: input.ContractTotalValue,
            ApprovedGrossValue: input.ApprovedGrossValue,
            DelayCalendarDays: input.DelayCalendarDays,
            PostAuditTkdn: postAudit,
            Status: "APPROVED_READY_FOR_INVOICE",
            ApprovedAtUtc: DateTimeOffset.UtcNow
        );

        _bastDocuments.Add(bast);
        return bast;
    }

    /// <summary>
    /// Menjalankan 3-Way Matching e-Invoicing (Kontrak vs BAST/GR vs e-Invoice Vendor),
    /// menghitung denda keterlambatan 1‰/hari (maks 5%), sanksi kekurangan TKDN, retensi 5%,
    /// memisahkan PPN 11% WAPU BUMN (Faktur 030.xxx), memotong PPh, dan menerbitkan BAP.
    /// </summary>
    public VendorEInvoiceSettlementDto SubmitAndMatchVendorEInvoice(SubmitVendorEInvoiceRequestDto input)
    {
        var bast = _bastDocuments.FirstOrDefault(b => b.Id == input.BastId)
            ?? throw new KeyNotFoundException("Dokumen BAST/PHO acuan tidak ditemukan.");

        // Validasi kode Faktur Pajak WAPU BUMN (Wajib diawali "030." bila IsClientWapuPpn = true)
        if (input.IsClientWapuPpn && !input.TaxInvoiceFakturNo.StartsWith("030.", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Eproc:Finance:InvalidWapuFakturCode - Untuk transaksi BUMN Wajib Pungut (WAPU), Nomor Seri Faktur Pajak ({input.TaxInvoiceFakturNo}) wajib menggunakan kode transaksi '030.'.");
        }

        var matchResult = _threeWayMatching.CalculateInvoiceSettlement(new ThreeWayMatchingInput(
            ContractTotalValue: bast.ContractTotalValue,
            ApprovedBastOrGrGrossValue: bast.ApprovedGrossValue,
            VendorInvoiceClaimedDpp: input.VendorInvoiceClaimedDpp,
            DelayCalendarDays: bast.DelayCalendarDays,
            PenaltyBasisAmount: bast.ApprovedGrossValue,
            SlaBreachPenaltyAmount: 0m,
            TkdnShortfallPenaltyAmount: bast.PostAuditTkdn.FinancialSanctionAmount,
            DownPaymentRecoupmentPct: input.DownPaymentRecoupmentPct,
            RetentionDeductionPct: input.RetentionDeductionPct,
            IsClientWapuPpn: input.IsClientWapuPpn,
            PpnRatePct: input.PpnRatePct,
            PphType: input.PphType,
            PphRatePct: input.PphRatePct
        ));

        var settlement = new VendorEInvoiceSettlementDto(
            Id: Guid.NewGuid(),
            InvoiceNumber: input.InvoiceNumber,
            TaxInvoiceFakturNo: input.TaxInvoiceFakturNo,
            BapDocumentNumber: matchResult.IsThreeWayMatched
                ? $"BAP/BUMN-FIN/2026/{_eInvoices.Count + 1:D4}"
                : "BAP-BLOCKED-MISMATCH",
            ContractId: bast.ContractId,
            ContractNumber: bast.ContractNumber,
            BastId: bast.Id,
            BastNumber: bast.BastNumber,
            VendorName: bast.VendorName,
            ContractTotalValue: bast.ContractTotalValue,
            ApprovedBastGrossValue: bast.ApprovedGrossValue,
            VendorInvoiceClaimedDpp: input.VendorInvoiceClaimedDpp,
            IsThreeWayMatched: matchResult.IsThreeWayMatched,
            MatchDiscrepancyNotes: matchResult.MatchDiscrepancyNotes,
            DpRecoupmentDeduction: matchResult.DpRecoupmentDeduction,
            RetentionDeductionAmount: matchResult.RetentionDeductionAmount,
            DelayCalendarDays: bast.DelayCalendarDays,
            CappedLatePenaltyAmount: matchResult.CappedLatePenaltyAmount,
            TkdnShortfallPenaltyAmount: bast.PostAuditTkdn.FinancialSanctionAmount,
            TotalPenaltyDeduction: matchResult.TotalPenaltyDeduction,
            DppNetAfterDeductions: matchResult.DppNetAfterDeductions,
            PpnAmount: matchResult.PpnAmount,
            IsPpnWithheldByWapu: matchResult.IsPpnWithheldByWapu,
            PphType: input.PphType,
            PphWithheldAmount: matchResult.PphWithheldAmount,
            NetPayableToVendor: matchResult.NetPayableToVendor,
            PaymentStatus: matchResult.IsThreeWayMatched
                ? "THREE_WAY_MATCHED_BAP_ISSUED"
                : "BLOCKED_MISMATCH",
            ProcessedAtUtc: DateTimeOffset.UtcNow
        );

        _eInvoices.Add(settlement);
        return settlement;
    }

    /// <summary>
    /// Mengevaluasi Rapor Kinerja Penyedia (Vendor Performance Evaluation / VPE) pasca-BAST:
    /// - Kualitas (30%) + Ketepatan Waktu (25%) + K3L/CSMS (20%) + Kepatuhan TKDN (15%) + Responsivitas (10%).
    /// </summary>
    public VendorPerformanceScorecardDto EvaluateVendorPerformanceVpe(EvaluateVendorVpeRequestDto input)
    {
        decimal weightedScore = Math.Round(
            (input.QualityScore * 0.30m) +
            (input.DeliveryTimelinessScore * 0.25m) +
            (input.HseCsmsComplianceScore * 0.20m) +
            (input.TkdnComplianceScore * 0.15m) +
            (input.ResponsivenessScore * 0.10m),
            2,
            MidpointRounding.AwayFromZero);

        var (grade, action) = weightedScore switch
        {
            >= 90.0m => ("PLATINUM", "MAINTAIN_AVL_PRIORITY"),
            >= 80.0m => ("GOLD", "MAINTAIN_AVL_PRIORITY"),
            >= 70.0m => ("SILVER", "COACHING_REQUIRED"),
            >= 55.0m => ("RED_WARNING", "SUSPEND_FROM_AVL"),
            _ => ("BLACKLIST_CANDIDATE", "SUSPEND_FROM_AVL")
        };

        var scorecard = new VendorPerformanceScorecardDto(
            Id: Guid.NewGuid(),
            ContractId: input.ContractId,
            ContractNumber: input.ContractNumber,
            VendorId: input.VendorId,
            VendorName: input.VendorName,
            QualityScore: input.QualityScore,
            DeliveryTimelinessScore: input.DeliveryTimelinessScore,
            HseCsmsComplianceScore: input.HseCsmsComplianceScore,
            TkdnComplianceScore: input.TkdnComplianceScore,
            ResponsivenessScore: input.ResponsivenessScore,
            FinalWeightedVpeScore: weightedScore,
            VpeGrade: grade,
            AvlActionRecommendation: action,
            EvaluatedAtUtc: DateTimeOffset.UtcNow
        );

        _vpeScorecards.Add(scorecard);
        return scorecard;
    }

    private void SeedInitialExecutionAndFinanceData()
    {
        var contractId = Guid.NewGuid();
        const string contractNo = "KTR/BUMN-ENG/EPC-VALVE/2026/001";
        const string vendorName = "PT Nusantara Valve & Piping Manufacturing";

        // 1. Seed ASN & Goods Receipt (17 Unit dikirim, 17 Unit lulus QC Hydrotest API 6D)
        RecordAsnAndGoodsReceipt(new CreateAsnAndGrRequestDto(
            ContractId: contractId,
            ContractNumber: contractNo,
            AsnNumber: "ASN-NVPM-2026-0081",
            VendorName: vendorName,
            ItemName: "High-Pressure Gate Valve 12 Inch Class 600 API 6D",
            ShippedQty: 17,
            AcceptedQty: 17,
            Uom: "UNIT",
            UnitPrice: 320_588_235.29m,
            QcInspectorNotes: "Seluruh 17 unit lulus uji tekanan hidrostatik API 6D dan pemeriksaan sertifikat mill-test."));

        // 2. Seed Pemantauan Kurva S Konstruksi (Minggu ke-8: Rencana 55%, Realisasi 43% -> Deviasi -12% -> Kritis SCM Tingkat I)
        EvaluateAndRecordSCurveProgress(new EvaluateSCurveProgressRequestDto(
            ContractId: contractId,
            ContractNumber: "KTR/BUMN-KARYA/EPC-GARDU/2026/004",
            WeekNumber: 8,
            PlannedCumulativePct: 55.0m,
            ActualCumulativePct: 43.0m,
            CurrentActiveScmLevel: ScmLevel.None,
            ActionPlanNotes: "Terjadi keterlambatan fabrikasi struktur baja; diterbitkan Surat Peringatan & Show Cause Meeting (SCM) Tingkat I dengan wajib Uji Coba 2 shift."));

        // 3. Seed BAST 100% dengan Post-Audit TKDN (Komitmen 60%, Realisasi Audit PT Surveyor Indonesia 58% -> Shortfall 2%, Keterlambatan 8 Hari)
        ApproveBastWithPostAuditTkdn(new ApproveBastWithPostAuditTkdnRequestDto(
            ContractId: contractId,
            ContractNumber: contractNo,
            BastNumber: "BAST/BUMN-ENG/2026/001",
            HandoverType: "BAST_GOODS_100",
            VendorName: vendorName,
            Category: ProcurementCategory.Goods,
            ContractTotalValue: 5_900_000_000m,
            ApprovedGrossValue: 5_450_000_000m,
            DelayCalendarDays: 8, // Terlambat 8 hari -> Denda 8 permil (0.8%)
            SurveyorInstitution: "PT Surveyor Indonesia",
            VerificationReportNo: "LHV-TKDN-SI-2026-9912",
            CommittedTkdnPct: 60.0m,
            ActualVerifiedTkdnPct: 58.0m)); // Shortfall 2% -> Sanksi finansial TKDN otomatis!
    }
}
