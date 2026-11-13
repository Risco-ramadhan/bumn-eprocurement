using Eproc.Configuration;
using Eproc.Enums;

namespace Eproc.Services;

public sealed record ThreeWayMatchingInput(
    decimal ContractTotalValue,
    decimal ApprovedBastOrGrGrossValue,
    decimal VendorInvoiceClaimedDpp,
    int DelayCalendarDays,
    decimal PenaltyBasisAmount,
    decimal SlaBreachPenaltyAmount,
    decimal TkdnShortfallPenaltyAmount,
    decimal DownPaymentRecoupmentPct,
    decimal RetentionDeductionPct,
    bool IsClientWapuPpn,
    decimal PpnRatePct,
    PphTaxType PphType,
    decimal PphRatePct
);

public sealed record ThreeWayMatchingResult(
    bool IsThreeWayMatched,
    string MatchDiscrepancyNotes,
    decimal DppGrossAmount,
    decimal DpRecoupmentDeduction,
    decimal RetentionDeductionAmount,
    decimal RawLatePenaltyAmount,
    decimal CappedLatePenaltyAmount,
    decimal TotalPenaltyDeduction,
    decimal DppNetAfterDeductions,
    decimal PpnAmount,
    bool IsPpnWithheldByWapu,
    decimal PphWithheldAmount,
    decimal NetPayableToVendor
);

/// <summary>
/// Engine 3-Way Matching (Kontrak/PO vs BAST/GR/SES vs e-Invoice), Kalkulator Denda 1‰, Retensi 5%, dan Pajak WAPU BUMN.
/// </summary>
public sealed class ThreeWayMatchingService
{
    private readonly EprocFeatureOptions _featureOptions;

    public ThreeWayMatchingService(EprocFeatureOptions? featureOptions = null)
    {
        _featureOptions = featureOptions ?? new EprocFeatureOptions();
    }

    public ThreeWayMatchingResult CalculateInvoiceSettlement(ThreeWayMatchingInput input)
    {
        // 1. Validasi 3-Way Matching: Tagihan vendor tidak boleh melebihi nilai BAST/GR/SES yang disetujui maupun plafon kontrak
        bool matchesBast = Math.Abs(input.VendorInvoiceClaimedDpp - input.ApprovedBastOrGrGrossValue) <= 1.00m;
        bool withinContract = input.ApprovedBastOrGrGrossValue <= input.ContractTotalValue;
        bool isMatched = matchesBast && withinContract;

        string discrepancyNotes = isMatched
            ? "MATCHED_OK"
            : $"DISCREPANCY: Invoice DPP ({input.VendorInvoiceClaimedDpp:N2}) != Approved BAST/GR ({input.ApprovedBastOrGrGrossValue:N2}) atau melebihi Kontrak ({input.ContractTotalValue:N2}).";

        decimal dppGross = input.ApprovedBastOrGrGrossValue;

        // 2. Potongan Pengembalian Uang Muka (DP Recoupment) & Retensi Pemeliharaan (umumnya 5%)
        decimal dpRecoupment = Math.Round(dppGross * (input.DownPaymentRecoupmentPct / 100m), 2, MidpointRounding.AwayFromZero);
        decimal retentionAmount = Math.Round(dppGross * (input.RetentionDeductionPct / 100m), 2, MidpointRounding.AwayFromZero);

        // 3. Kalkulasi Denda Keterlambatan (1 permil = 1/1000 per hari, maksimal 5% dari nilai kontrak)
        decimal perMilleRatio = _featureOptions.DefaultLatePenaltyPerMille / 1000m;
        decimal maxPenaltyCapAmount = Math.Round(
            input.ContractTotalValue * (_featureOptions.MaxPenaltyCapPct / 100m),
            2,
            MidpointRounding.AwayFromZero);

        decimal rawLatePenalty = input.DelayCalendarDays > 0
            ? Math.Round(input.DelayCalendarDays * perMilleRatio * input.PenaltyBasisAmount, 2, MidpointRounding.AwayFromZero)
            : 0m;

        decimal cappedLatePenalty = Math.Min(rawLatePenalty, maxPenaltyCapAmount);
        decimal totalPenalty = Math.Round(
            cappedLatePenalty + input.SlaBreachPenaltyAmount + input.TkdnShortfallPenaltyAmount,
            2,
            MidpointRounding.AwayFromZero);

        // 4. DPP Bersih setelah potongan DP & Retensi
        decimal dppAfterDpAndRetention = Math.Max(0m, dppGross - dpRecoupment - retentionAmount);
        decimal dppNetAfterAllDeductions = Math.Max(0m, dppAfterDpAndRetention - totalPenalty);

        // 5. Perhitungan PPN (11%) & PPh (WAPU BUMN)
        decimal ppnAmount = Math.Round(dppAfterDpAndRetention * (input.PpnRatePct / 100m), 2, MidpointRounding.AwayFromZero);
        decimal pphWithheld = Math.Round(dppAfterDpAndRetention * (input.PphRatePct / 100m), 2, MidpointRounding.AwayFromZero);

        // Jika BUMN adalah Wajib Pungut (WAPU), PPN dipungut dan disetor langsung oleh BUMN ke Kas Negara (tidak ditransfer ke rekening vendor)
        decimal ppnPayableToVendor = input.IsClientWapuPpn ? 0m : ppnAmount;

        decimal netPayableToVendor = Math.Round(
            dppNetAfterAllDeductions + ppnPayableToVendor - pphWithheld,
            2,
            MidpointRounding.AwayFromZero);

        return new ThreeWayMatchingResult(
            IsThreeWayMatched: isMatched,
            MatchDiscrepancyNotes: discrepancyNotes,
            DppGrossAmount: dppGross,
            DpRecoupmentDeduction: dpRecoupment,
            RetentionDeductionAmount: retentionAmount,
            RawLatePenaltyAmount: rawLatePenalty,
            CappedLatePenaltyAmount: cappedLatePenalty,
            TotalPenaltyDeduction: totalPenalty,
            DppNetAfterDeductions: dppNetAfterAllDeductions,
            PpnAmount: ppnAmount,
            IsPpnWithheldByWapu: input.IsClientWapuPpn,
            PphWithheldAmount: pphWithheld,
            NetPayableToVendor: netPayableToVendor
        );
    }
}
