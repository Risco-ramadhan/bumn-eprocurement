using Eproc.Configuration;
using Eproc.Enums;

namespace Eproc.Services;

public sealed record BidEvaluationCandidate(
    Guid ParticipantId,
    string VendorName,
    decimal OfferedPriceHp,
    decimal VerifiedTkdnPct,
    decimal VerifiedBmpPct,
    decimal TechnicalScore
);

public sealed record HeaEvaluationResult(
    Guid ParticipantId,
    string VendorName,
    decimal OfferedPriceHp,
    decimal PercentageToHps,
    bool IsAboveHps,
    bool RequiresPriceFairnessClarification,
    decimal RequiredPerformanceBondAmount,
    decimal VerifiedTkdnPct,
    decimal VerifiedBmpPct,
    bool IsPreferenceEligible,
    decimal KoefisienPreferensiKp,
    decimal CalculatedHea,
    decimal CommercialScore,
    decimal FinalCombinedScore,
    int FinalRank
);

/// <summary>
/// Kalkulator Resmi Evaluasi Harga, Kewajaran Harga (<80% HPS), Preferensi TKDN/BMP, dan Harga Evaluasi Akhir (HEA) BUMN.
/// </summary>
public sealed class TkdnHeaCalculatorService
{
    private readonly EprocFeatureOptions _featureOptions;

    public TkdnHeaCalculatorService(EprocFeatureOptions? featureOptions = null)
    {
        _featureOptions = featureOptions ?? new EprocFeatureOptions();
    }

    /// <summary>
    /// Menghitung Koefisien Preferensi (KP) dan Harga Evaluasi Akhir (HEA):
    /// HEA = (1 / (1 + KP)) * HP
    /// Syarat: TKDN >= 25% (MinTkdnPreferenceEligiblePct).
    /// Maksimal Preferensi: 25% untuk Barang, 7.5% untuk Konstruksi.
    /// </summary>
    public (bool IsEligible, decimal Kp, decimal Hea) CalculateHea(
        decimal offeredPriceHp,
        decimal verifiedTkdnPct,
        ProcurementCategory category)
    {
        if (offeredPriceHp <= 0)
            throw new ArgumentOutOfRangeException(nameof(offeredPriceHp), "Harga penawaran (HP) harus lebih besar dari 0.");

        if (verifiedTkdnPct < _featureOptions.MinTkdnPreferenceEligiblePct)
        {
            return (false, 0m, Math.Round(offeredPriceHp, 2, MidpointRounding.AwayFromZero));
        }

        decimal maxPrefPct = category switch
        {
            ProcurementCategory.Goods => _featureOptions.GoodsTkdnMaxPreferencePct,
            ProcurementCategory.Construction => _featureOptions.ConstructionTkdnMaxPreferencePct,
            _ => 0m
        };

        if (maxPrefPct <= 0m)
        {
            return (false, 0m, Math.Round(offeredPriceHp, 2, MidpointRounding.AwayFromZero));
        }

        decimal tkdnRatio = Math.Clamp(verifiedTkdnPct, 0m, 100m) / 100m;
        decimal maxPrefRatio = maxPrefPct / 100m;
        decimal kp = Math.Round(tkdnRatio * maxPrefRatio, 6, MidpointRounding.AwayFromZero);

        decimal hea = Math.Round((1m / (1m + kp)) * offeredPriceHp, 2, MidpointRounding.AwayFromZero);
        return (true, kp, hea);
    }

    /// <summary>
    /// Mengevaluasi dan memeringkat seluruh peserta tender berdasarkan HEA dan Bobot Gabungan Teknis & Biaya.
    /// </summary>
    public IReadOnlyList<HeaEvaluationResult> RankCandidates(
        IEnumerable<BidEvaluationCandidate> candidates,
        decimal hpsAmount,
        ProcurementCategory category,
        decimal technicalWeightPct = 0m,
        decimal commercialWeightPct = 100m)
    {
        if (hpsAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(hpsAmount), "Nilai HPS harus lebih besar dari 0.");

        if (Math.Round(technicalWeightPct + commercialWeightPct, 2) != 100m)
            throw new ArgumentException("Total bobot teknis dan komersial harus tepat 100%.");

        var preliminary = candidates.Select(c =>
        {
            var (isEligible, kp, hea) = CalculateHea(c.OfferedPriceHp, c.VerifiedTkdnPct, category);
            decimal pctToHps = Math.Round((c.OfferedPriceHp / hpsAmount) * 100m, 2, MidpointRounding.AwayFromZero);
            bool isAboveHps = c.OfferedPriceHp > hpsAmount;
            bool isBelow80Pct = pctToHps < 80.00m;

            // Jika penawaran < 80% HPS, Jaminan Pelaksanaan wajib 5% dari nilai HPS (bukan 5% dari nilai kontrak)
            decimal perfBondAmount = isBelow80Pct
                ? Math.Round(hpsAmount * 0.05m, 2, MidpointRounding.AwayFromZero)
                : Math.Round(c.OfferedPriceHp * 0.05m, 2, MidpointRounding.AwayFromZero);

            return new
            {
                Candidate = c,
                PctToHps = pctToHps,
                IsAboveHps = isAboveHps,
                IsBelow80Pct = isBelow80Pct,
                PerfBondAmount = perfBondAmount,
                IsEligible = isEligible,
                Kp = kp,
                Hea = hea
            };
        }).ToList();

        var validHeas = preliminary.Where(x => !x.IsAboveHps).Select(x => x.Hea).ToList();
        decimal lowestValidHea = validHeas.Count > 0 ? validHeas.Min() : preliminary.Min(x => x.Hea);

        var scored = preliminary.Select(x =>
        {
            decimal commercialScore = x.IsAboveHps
                ? 0m
                : Math.Round((lowestValidHea / x.Hea) * 100m, 2, MidpointRounding.AwayFromZero);

            decimal combinedScore = Math.Round(
                (x.Candidate.TechnicalScore * (technicalWeightPct / 100m)) +
                (commercialScore * (commercialWeightPct / 100m)),
                2,
                MidpointRounding.AwayFromZero);

            return (Item: x, CommercialScore: commercialScore, CombinedScore: combinedScore);
        })
        .OrderBy(x => x.Item.IsAboveHps) // Peserta di atas HPS ditaruh paling bawah
        .ThenByDescending(x => technicalWeightPct > 0m ? x.CombinedScore : 0m)
        .ThenBy(x => x.Item.Hea)
        .ToList();

        var results = new List<HeaEvaluationResult>(scored.Count);
        for (int i = 0; i < scored.Count; i++)
        {
            var s = scored[i];
            results.Add(new HeaEvaluationResult(
                ParticipantId: s.Item.Candidate.ParticipantId,
                VendorName: s.Item.Candidate.VendorName,
                OfferedPriceHp: s.Item.Candidate.OfferedPriceHp,
                PercentageToHps: s.Item.PctToHps,
                IsAboveHps: s.Item.IsAboveHps,
                RequiresPriceFairnessClarification: s.Item.IsBelow80Pct,
                RequiredPerformanceBondAmount: s.Item.PerfBondAmount,
                VerifiedTkdnPct: s.Item.Candidate.VerifiedTkdnPct,
                VerifiedBmpPct: s.Item.Candidate.VerifiedBmpPct,
                IsPreferenceEligible: s.Item.IsEligible,
                KoefisienPreferensiKp: s.Item.Kp,
                CalculatedHea: s.Item.Hea,
                CommercialScore: s.CommercialScore,
                FinalCombinedScore: s.CombinedScore,
                FinalRank: i + 1
            ));
        }

        return results;
    }

    /// <summary>
    /// Menghitung sanksi finansial apabila realisasi TKDN akhir (Post-Audit) lebih rendah dari komitmen saat tender.
    /// </summary>
    public decimal CalculatePostAuditTkdnSanction(
        decimal contractValue,
        decimal committedTkdnPct,
        decimal actualVerifiedTkdnPct,
        ProcurementCategory category)
    {
        if (actualVerifiedTkdnPct >= committedTkdnPct)
            return 0m;

        decimal shortfallPct = committedTkdnPct - actualVerifiedTkdnPct;
        decimal maxPrefPct = category == ProcurementCategory.Construction
            ? _featureOptions.ConstructionTkdnMaxPreferencePct
            : _featureOptions.GoodsTkdnMaxPreferencePct;

        // Sanksi dihitung proporsional dari selisih preferensi harga yang seharusnya tidak didapat, maksimal sebesar persentase preferensi
        decimal penaltyRatio = Math.Min(shortfallPct / 100m, maxPrefPct / 100m);
        return Math.Round(contractValue * penaltyRatio, 2, MidpointRounding.AwayFromZero);
    }
}
