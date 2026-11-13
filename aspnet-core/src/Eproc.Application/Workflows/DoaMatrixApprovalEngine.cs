namespace Eproc.Workflows;

public sealed record DoaApprovalTierStep(
    int StepSequence,
    string ApproverRoleCode,
    string ApproverPositionGrade,
    string ApprovalMode,
    int SlaHours
);

/// <summary>
/// Engine Delegation of Authority (DOA) & Pemeriksa Segregation of Duties (SoD) BUMN.
/// Menentukan rute persetujuan berjenjang berdasarkan nilai paket/transaksi dan mencegah konflik kepentingan role.
/// </summary>
public sealed class DoaMatrixApprovalEngine
{
    private static readonly Dictionary<string, HashSet<string>> SodConflicts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PPK"] = new(StringComparer.OrdinalIgnoreCase) { "POKJA_COMMITTEE", "BUYER_PEJABAT_PENGADAAN", "VMS_VERIFIER", "FINANCE_AP", "INTERNAL_AUDITOR", "VENDOR_ADMIN" },
        ["POKJA_COMMITTEE"] = new(StringComparer.OrdinalIgnoreCase) { "PA_KPA", "PPK", "VMS_VERIFIER", "LEGAL_OFFICER", "FINANCE_AP", "INTERNAL_AUDITOR", "VENDOR_ADMIN" },
        ["PA_KPA"] = new(StringComparer.OrdinalIgnoreCase) { "POKJA_COMMITTEE", "INTERNAL_AUDITOR", "VENDOR_ADMIN" },
        ["FINANCE_AP"] = new(StringComparer.OrdinalIgnoreCase) { "PPK", "POKJA_COMMITTEE", "BUYER_PEJABAT_PENGADAAN", "WAREHOUSE_QC", "INTERNAL_AUDITOR", "VENDOR_ADMIN" },
        ["INTERNAL_AUDITOR"] = new(StringComparer.OrdinalIgnoreCase) { "PA_KPA", "PPK", "POKJA_COMMITTEE", "FINANCE_AP", "VENDOR_ADMIN" },
        ["VENDOR_ADMIN"] = new(StringComparer.OrdinalIgnoreCase) { "PA_KPA", "PPK", "POKJA_COMMITTEE", "BUYER_PEJABAT_PENGADAAN", "VMS_VERIFIER", "FINANCE_AP", "INTERNAL_AUDITOR" }
    };

    /// <summary>
    /// Memvalidasi apakah kombinasi role yang diberikan kepada satu user melanggar Segregation of Duties (SoD) BUMN.
    /// </summary>
    public (bool IsValid, string? ConflictReason) ValidateSegregationOfDuties(IEnumerable<string> assignedRoleCodes)
    {
        var roleList = assignedRoleCodes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        for (int i = 0; i < roleList.Count; i++)
        {
            string roleA = roleList[i];
            if (!SodConflicts.TryGetValue(roleA, out var forbiddenRoles))
                continue;

            for (int j = i + 1; j < roleList.Count; j++)
            {
                string roleB = roleList[j];
                if (forbiddenRoles.Contains(roleB))
                {
                    return (false, $"Eproc:SoD:RoleConflict - Role '{roleA}' dilarang dipegang bersamaan dengan '{roleB}' oleh pejabat yang sama.");
                }
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Menyusun rute jenjang persetujuan DOA (Delegation of Authority) berdasarkan nilai rupiah transaksi:
    /// - <= Rp 100 Juta                 : 1 Step  (BOD-3 Manager / PPK)
    /// - > Rp 100 Juta s.d. Rp 1 Miliar : 2 Steps (BOD-3 Manager -> BOD-2 VP/GM)
    /// - > Rp 1 Miliar s.d. Rp 10 Miliar: 3 Steps (BOD-2 VP -> BOD-1 SVP -> Direktur Bidang)
    /// - > Rp 10 Miliar                 : 4 Steps (BOD-1 SVP -> Direktur Bidang -> Rapat Direksi Kolektif / Komisaris)
    /// </summary>
    public IReadOnlyList<DoaApprovalTierStep> ResolveApprovalRoute(decimal transactionAmountIdr, string documentType = "WINNER_DETERMINATION")
    {
        if (transactionAmountIdr <= 0)
            throw new ArgumentOutOfRangeException(nameof(transactionAmountIdr), "Nilai transaksi harus lebih besar dari 0.");

        var steps = new List<DoaApprovalTierStep>
        {
            new(1, "PPK", "BOD-3_MANAGER", "SINGLE", 24)
        };

        if (transactionAmountIdr > 100_000_000m)
        {
            steps.Add(new(2, "PA_KPA", "BOD-2_VP_GM", "SINGLE", 48));
        }

        if (transactionAmountIdr > 1_000_000_000m)
        {
            steps.Add(new(3, "PA_KPA", "BOD-1_SVP", "SINGLE", 48));
            steps.Add(new(4, "PA_KPA", "DIRECTOR_BIDANG", "SINGLE", 72));
        }

        if (transactionAmountIdr > 10_000_000_000m)
        {
            steps.Add(new(5, "PA_KPA", "BOARD_OF_DIRECTORS_COLLEGIATE", "QUORUM", 120));
        }

        return steps;
    }
}
