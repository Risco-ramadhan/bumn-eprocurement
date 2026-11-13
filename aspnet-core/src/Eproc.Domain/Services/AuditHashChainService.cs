using System.Security.Cryptography;
using System.Text;
using Eproc.Enums;

namespace Eproc.Services;

public sealed record AuditChainRecord(
    Guid EventUuid,
    Guid? ActorUserId,
    string ActorRole,
    string EventModule,
    string EventAction,
    string EntityTable,
    Guid EntityId,
    string PayloadJson,
    DateTimeOffset OccurredAtUtc,
    string PrevRecordHash,
    string CurrentRecordHash
);

/// <summary>
/// Layanan Immutable Audit Trail berbasis Hash-Chain (SHA-256) dan Deteksi Kontrak Kritis (Kurva S & SCM I/II/III).
/// </summary>
public sealed class AuditHashChainService
{
    public const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";

    public AuditChainRecord CreateLinkedRecord(
        Guid? actorUserId,
        string actorRole,
        string eventModule,
        string eventAction,
        string entityTable,
        Guid entityId,
        string payloadJson,
        string? previousHash = null,
        DateTimeOffset? occurredAtUtc = null)
    {
        var eventUuid = Guid.NewGuid();
        var timestamp = occurredAtUtc ?? DateTimeOffset.UtcNow;
        var prevHash = string.IsNullOrWhiteSpace(previousHash) ? GenesisHash : previousHash;

        string currentHash = ComputeRecordHash(
            eventUuid,
            actorUserId,
            actorRole,
            eventModule,
            eventAction,
            entityTable,
            entityId,
            payloadJson,
            timestamp,
            prevHash);

        return new AuditChainRecord(
            EventUuid: eventUuid,
            ActorUserId: actorUserId,
            ActorRole: actorRole,
            EventModule: eventModule,
            EventAction: eventAction,
            EntityTable: entityTable,
            EntityId: entityId,
            PayloadJson: payloadJson,
            OccurredAtUtc: timestamp,
            PrevRecordHash: prevHash,
            CurrentRecordHash: currentHash
        );
    }

    public bool VerifyChainIntegrity(IReadOnlyList<AuditChainRecord> chain)
    {
        string expectedPrevHash = GenesisHash;
        foreach (var record in chain)
        {
            if (!string.Equals(record.PrevRecordHash, expectedPrevHash, StringComparison.OrdinalIgnoreCase))
                return false;

            string recomputed = ComputeRecordHash(
                record.EventUuid,
                record.ActorUserId,
                record.ActorRole,
                record.EventModule,
                record.EventAction,
                record.EntityTable,
                record.EntityId,
                record.PayloadJson,
                record.OccurredAtUtc,
                record.PrevRecordHash);

            if (!string.Equals(recomputed, record.CurrentRecordHash, StringComparison.OrdinalIgnoreCase))
                return false;

            expectedPrevHash = record.CurrentRecordHash;
        }

        return true;
    }

    /// <summary>
    /// Evaluasi deviasi Kurva S konstruksi untuk mendeteksi Kontrak Kritis dan memicu Show Cause Meeting (SCM I/II/III):
    /// - Periode I (Rencana 0% - 70%)   : Kritis jika deviasi minus > 10%
    /// - Periode II (Rencana 70% - 100%): Kritis jika deviasi minus > 5%
    /// </summary>
    public (decimal DeviationPct, bool IsCriticalContract, ScmLevel RecommendedScmLevel) EvaluateScurveDeviation(
        decimal plannedCumulativePct,
        decimal actualCumulativePct,
        ScmLevel currentActiveScmLevel = ScmLevel.None)
    {
        decimal deviation = Math.Round(actualCumulativePct - plannedCumulativePct, 3, MidpointRounding.AwayFromZero);
        decimal criticalThreshold = plannedCumulativePct <= 70.0m ? -10.0m : -5.0m;

        bool isCritical = deviation <= criticalThreshold;
        if (!isCritical)
        {
            return (deviation, false, ScmLevel.None);
        }

        ScmLevel nextScm = currentActiveScmLevel switch
        {
            ScmLevel.None => ScmLevel.ScmLevel1,
            ScmLevel.ScmLevel1 => ScmLevel.ScmLevel2,
            _ => ScmLevel.ScmLevel3
        };

        return (deviation, true, nextScm);
    }

    private static string ComputeRecordHash(
        Guid eventUuid,
        Guid? actorUserId,
        string actorRole,
        string eventModule,
        string eventAction,
        string entityTable,
        Guid entityId,
        string payloadJson,
        DateTimeOffset occurredAtUtc,
        string prevHash)
    {
        string canonical = $"{prevHash}|{eventUuid:D}|{actorUserId?.ToString("D") ?? "ANONYMOUS"}|{actorRole}|{eventModule}|{eventAction}|{entityTable}|{entityId:D}|{occurredAtUtc.ToUnixTimeMilliseconds()}|{payloadJson}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
