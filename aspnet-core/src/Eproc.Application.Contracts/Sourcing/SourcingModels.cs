using Eproc.Enums;

namespace Eproc.Sourcing;

public sealed record HeaRankingItemDto(
    Guid ParticipantId,
    string VendorName,
    decimal OfferedPriceHp,
    decimal VerifiedTkdnPct,
    decimal VerifiedBmpPct,
    decimal PreferenceCoefficientKp,
    decimal CalculatedHea,
    decimal PriceToHpsRatioPct,
    bool RequiresAbnormallyLowClarification,
    decimal RequiredPerformanceBondAmount,
    bool IsAboveHpsCeiling,
    int CommercialRank,
    decimal FinalCombinedScore,
    int FinalRank
);

public sealed record AanwijzingQaDto(
    Guid Id,
    string MaskedParticipantAlias, // Misal "Peserta #01" (Disamarkan agar bebas kolusi)
    string DocumentSectionRef,
    string QuestionText,
    string? AnswerText,
    string? AnsweredByCommitteeName,
    DateTimeOffset AskedAtUtc,
    DateTimeOffset? AnsweredAtUtc
);

public sealed record EncryptedBidSubmissionDto(
    Guid Id,
    Guid ParticipantId,
    Guid VendorId,
    string VendorName,
    string BusinessScale,
    string SubmissionReceiptNo,
    string Envelope1AdminTechStatus, // "SEALED" | "OPENED_PASSED" | "OPENED_FAILED"
    decimal? TechnicalScore,
    string Envelope2CommercialCipherPreview,
    string PayloadSha256Hash,
    bool IsEnvelope2Opened,
    decimal? DecryptedOfferedPriceHp, // NULL sebelum Bid Opening Ceremony Sampul 2!
    decimal? DeclaredTkdnPct,
    decimal? DeclaredBmpPct,
    string BidBondNumber,
    decimal BidBondAmount,
    DateTimeOffset SubmittedAtUtc
);

public sealed record EAuctionBidLogDto(
    int BidSequence,
    Guid ParticipantId,
    string MaskedBidderAlias,
    string VendorName,
    decimal SubmittedPriceHp,
    decimal RecalculatedHea,
    bool TriggeredAntiSnipingAutoExtend,
    DateTimeOffset BidTimestampUtc
);

public sealed record EAuctionSessionDto(
    Guid Id,
    decimal MinDecrementAmount,
    DateTimeOffset ScheduledStartUtc,
    DateTimeOffset ScheduledEndUtc,
    DateTimeOffset CurrentEffectiveEndUtc,
    int AutoExtendWindowSeconds,
    int AutoExtendDurationSeconds,
    int CurrentExtensionCount,
    int MaxExtensionCount,
    string Status, // "LIVE_BIDDING" | "CLOSED"
    List<EAuctionBidLogDto> BidHistory
);

public sealed record TenderObjectionSanggahDto(
    Guid Id,
    Guid ParticipantId,
    string VendorName,
    string ObjectionLevel, // "SANGGAH_AWAL" | "SANGGAH_BANDING"
    string ObjectionCategory,
    string ObjectionLetterNo,
    string ObjectionDetails,
    string RulingDecision, // "PENDING" | "REJECTED_TENDER_CONTINUES" | "ACCEPTED_REEVALUATION"
    string? ResponseLetterNo,
    string? ResponseDetails,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? RespondedAtUtc
);

public sealed record WinnerDeterminationSummaryDto(
    Guid ParticipantId,
    string WinnerVendorName,
    string BahpDocumentNumber,
    decimal InitialBidPriceHp,
    decimal PostAuctionPriceHp,
    decimal FinalBafoContractPrice,
    decimal FinalCalculatedHea,
    decimal CommittedTkdnPct,
    decimal CostSavingToHpsAmount,
    decimal CostSavingToHpsPct,
    decimal CostSavingToPaguAmount,
    decimal CostSavingToPaguPct,
    decimal RequiredPerformanceBondAmount,
    DateTimeOffset AnnouncedAtUtc
);

public sealed record TenderDetailDto(
    Guid Id,
    Guid PackageId,
    string TenderNumber,
    string TenderTitle,
    ProcurementCategory Category,
    SourcingMethod SourcingMethod,
    SubmissionEnvelopeMethod EnvelopeMethod,
    EvaluationMethod EvaluationMethod,
    decimal TechnicalWeightPct,
    decimal CommercialWeightPct,
    decimal TotalPaguBudget,
    bool IsHpsDecrypted,
    decimal? DecryptedHpsAmount, // NULL sebelum Bid Opening Ceremony Sampul 2!
    string CurrentStage,
    // "AANWIJZING_AND_BID_SUBMISSION" | "ENVELOPE_1_TECH_EVALUATION" | "ENVELOPE_2_OPENED_HEA_RANKED"
    // | "E_REVERSE_AUCTION_LIVE" | "BAFO_AND_WINNER_ANNOUNCED" | "OBJECTION_SANGGAH_PERIOD" | "READY_FOR_SPPBJ"
    List<AanwijzingQaDto> AanwijzingForum,
    List<EncryptedBidSubmissionDto> BidSubmissions,
    List<HeaRankingItemDto> HeaRankings,
    EAuctionSessionDto? EAuctionSession,
    WinnerDeterminationSummaryDto? WinnerDetermination,
    List<TenderObjectionSanggahDto> ObjectionsSanggah
);

public sealed record AskAanwijzingQuestionRequestDto(
    string MaskedParticipantAlias,
    string DocumentSectionRef,
    string QuestionText,
    string? CommitteeAnswerText
);

public sealed record SubmitEncryptedVendorBidRequestDto(
    Guid VendorId,
    string VendorName,
    string BusinessScale,
    decimal OfferedPriceHp,
    decimal DeclaredTkdnPct,
    decimal DeclaredBmpPct,
    decimal TechnicalSelfScore,
    string BidBondNumber,
    decimal BidBondAmount
);

public sealed record SubmitEAuctionBidRequestDto(
    Guid ParticipantId,
    decimal NewLoweredBidPriceHp,
    int SecondsBeforeEndSimulated
);

public sealed record FinalizeBafoAndWinnerRequestDto(
    Guid WinningParticipantId,
    decimal FinalBafoAgreedPrice,
    string BahpNumber
);

public sealed record SubmitSanggahRequestDto(
    Guid ParticipantId,
    string VendorName,
    string ObjectionLevel,
    string ObjectionCategory,
    string ObjectionLetterNo,
    string ObjectionDetails
);

public sealed record ResolveSanggahRequestDto(
    Guid ObjectionId,
    bool AcceptObjection,
    string ResponseLetterNo,
    string ResponseDetails
);
