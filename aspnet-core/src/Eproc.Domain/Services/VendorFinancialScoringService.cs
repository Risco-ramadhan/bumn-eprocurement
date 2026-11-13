using Eproc.Enums;

namespace Eproc.Services;

public sealed record VendorFinancialProfileInput(
    string BusinessScale, // "KECIL", "MENENGAH", "BESAR"
    decimal CurrentAssets,
    decimal TotalAssets,
    decimal CurrentLiabilities,
    decimal TotalLiabilities,
    decimal NetWorthEquityKb,
    decimal RetainedEarnings,
    decimal Ebit,
    decimal HighestExperienceValueNpt,
    decimal TotalOngoingContractsValue,
    int ActiveOngoingPackagesCount
);

public sealed record VendorQualificationAssessmentResult(
    decimal KemampuanDasarKd,
    decimal ModalKerjaMk,
    decimal KemampuanNyataKn,
    decimal SisaKemampuanNyataSkn,
    int SisaKemampuanPaketSkp,
    decimal AltmanZScore,
    string FinancialHealthZone, // "SAFE_ZONE", "GREY_ZONE", "DISTRESS_ZONE"
    bool PassesKdRequirement,
    bool PassesSknRequirement,
    bool PassesSkpRequirement,
    bool PassesCsmsRequirement,
    bool IsOverallQualified
);

/// <summary>
/// Domain Service untuk menghitung kualifikasi finansial & teknis vendor BUMN:
/// KD (Kemampuan Dasar), SKN (Sisa Kemampuan Nyata), SKP (Sisa Kemampuan Paket), Altman Z-Score, dan CSMS Guard.
/// </summary>
public sealed class VendorFinancialScoringService
{
    /// <summary>
    /// Menghitung Kemampuan Dasar (KD) = 3 * NPt (Nilai Pengalaman Tertinggi).
    /// </summary>
    public decimal CalculateKemampuanDasar(decimal highestExperienceValueNpt)
    {
        if (highestExperienceValueNpt < 0)
            throw new ArgumentOutOfRangeException(nameof(highestExperienceValueNpt));

        return Math.Round(3m * highestExperienceValueNpt, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Menghitung MK, KN, dan SKN berdasarkan skala usaha:
    /// - Kecil    : fl = 0.3, fp = 5, KP = 5
    /// - Menengah : fl = 0.6, fp = 7, KP = 6 (atau max(6, 1.2*N))
    /// - Besar    : fl = 0.8, fp = 8, KP = 8 (atau max(6, 1.2*N))
    /// </summary>
    public (decimal Mk, decimal Kn, decimal Skn, int Skp) CalculateSknAndSkp(
        string businessScale,
        decimal netWorthEquityKb,
        decimal totalOngoingContractsValue,
        int activeOngoingPackagesCount)
    {
        var (fl, fp, maxPackagesKp) = businessScale.ToUpperInvariant() switch
        {
            "MIKRO" or "KECIL" => (0.3m, 5m, 5),
            "MENENGAH" => (0.6m, 7m, 6),
            _ => (0.8m, 8m, 8) // BESAR
        };

        decimal mk = Math.Round(fl * netWorthEquityKb, 2, MidpointRounding.AwayFromZero);
        decimal kn = Math.Round(fp * mk, 2, MidpointRounding.AwayFromZero);
        decimal skn = Math.Round(kn - totalOngoingContractsValue, 2, MidpointRounding.AwayFromZero);
        int skp = Math.Max(0, maxPackagesKp - activeOngoingPackagesCount);

        return (mk, kn, skn, skp);
    }

    /// <summary>
    /// Menghitung Altman Z''-Score (Emerging Markets / Non-Manufaktur & Konstruksi):
    /// Z'' = 6.56*X1 + 3.26*X2 + 6.72*X3 + 1.05*X4
    /// X1 = (Current Assets - Current Liabilities) / Total Assets
    /// X2 = Retained Earnings / Total Assets
    /// X3 = EBIT / Total Assets
    /// X4 = Book Value of Equity / Total Liabilities
    /// </summary>
    public (decimal ZScore, string HealthZone) CalculateAltmanZScore(
        decimal currentAssets,
        decimal totalAssets,
        decimal currentLiabilities,
        decimal totalLiabilities,
        decimal netWorthEquity,
        decimal retainedEarnings,
        decimal ebit)
    {
        if (totalAssets <= 0)
            return (0m, "DISTRESS_ZONE");

        decimal workingCapital = currentAssets - currentLiabilities;
        decimal x1 = workingCapital / totalAssets;
        decimal x2 = retainedEarnings / totalAssets;
        decimal x3 = ebit / totalAssets;
        decimal x4 = totalLiabilities > 0 ? netWorthEquity / totalLiabilities : 5.0m;

        decimal zScore = Math.Round(
            (6.56m * x1) + (3.26m * x2) + (6.72m * x3) + (1.05m * x4),
            4,
            MidpointRounding.AwayFromZero);

        string zone = zScore switch
        {
            > 2.60m => "SAFE_ZONE",
            >= 1.10m => "GREY_ZONE",
            _ => "DISTRESS_ZONE"
        };

        return (zScore, zone);
    }

    /// <summary>
    /// Evaluasi menyeluruh kelayakan kualifikasi vendor terhadap paket tender tertentu (termasuk CSMS Risk Guard).
    /// </summary>
    public VendorQualificationAssessmentResult AssessVendorForPackage(
        VendorFinancialProfileInput input,
        decimal packageHpsAmount,
        CsmsRiskLevel requiredPackageCsmsLevel,
        CsmsRiskLevel? vendorCertifiedCsmsLevel,
        bool requireKdCheck = true)
    {
        decimal kd = CalculateKemampuanDasar(input.HighestExperienceValueNpt);
        var (mk, kn, skn, skp) = CalculateSknAndSkp(
            input.BusinessScale,
            input.NetWorthEquityKb,
            input.TotalOngoingContractsValue,
            input.ActiveOngoingPackagesCount);

        var (zScore, healthZone) = CalculateAltmanZScore(
            input.CurrentAssets,
            input.TotalAssets,
            input.CurrentLiabilities,
            input.TotalLiabilities,
            input.NetWorthEquityKb,
            input.RetainedEarnings,
            input.Ebit);

        bool passesKd = !requireKdCheck || kd >= packageHpsAmount;
        // Syarat SKN minimal 10% dari nilai HPS paket yang diikuti
        bool passesSkn = skn >= Math.Round(0.10m * packageHpsAmount, 2, MidpointRounding.AwayFromZero);
        bool passesSkp = skp >= 1;
        bool passesCsms = vendorCertifiedCsmsLevel.HasValue && vendorCertifiedCsmsLevel.Value >= requiredPackageCsmsLevel;

        return new VendorQualificationAssessmentResult(
            KemampuanDasarKd: kd,
            ModalKerjaMk: mk,
            KemampuanNyataKn: kn,
            SisaKemampuanNyataSkn: skn,
            SisaKemampuanPaketSkp: skp,
            AltmanZScore: zScore,
            FinancialHealthZone: healthZone,
            PassesKdRequirement: passesKd,
            PassesSknRequirement: passesSkn,
            PassesSkpRequirement: passesSkp,
            PassesCsmsRequirement: passesCsms,
            IsOverallQualified: passesKd && passesSkn && passesSkp && passesCsms
        );
    }
}
