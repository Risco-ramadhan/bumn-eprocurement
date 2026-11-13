using System.Security.Cryptography;
using System.Text.Json;
using Eproc.Enums;
using Eproc.Services;

namespace Eproc.Sourcing;

/// <summary>
/// Application Service untuk Fase 3 (e-Sourcing & Tender Engine):
/// - Forum Aanwijzing Teranonimisasi ("Peserta #01", "Peserta #02") & BAP Penjelasan
/// - Encrypted Bid Vault (Penyegelan Sampul Penawaran Vendor dengan AES-256-GCM + RSA)
/// - Bid Opening Ceremony (Dekripsi HPS & Sampul 2 Harga hanya bagi peserta yang lulus Sampul 1 Admin/Teknis)
/// - Perankingan Otomatis Harga Evaluasi Akhir (HEA) berbasis Preferensi TKDN & Deteksi < 80% HPS
/// - Ruang e-Reverse Auction Real-Time dilengkapi Anti-Sniping Auto-Extend (+5 Menit jika bid masuk di 3 menit terakhir)
/// - Negosiasi BAFO, Perhitungan Cost Saving terhadap HPS/Pagu, Penetapan Pemenang (BAHP), & Masa Sanggah
/// </summary>
public sealed class ESourcingTenderAppService
{
    private readonly EnvelopeCryptoService _cryptoService;
    private readonly TkdnHeaCalculatorService _heaCalculator;
    private readonly RSA _committeeRsaKey;
    private readonly List<TenderDetailDto> _tenders = new();
    private readonly Dictionary<Guid, SealedEnvelopePackage> _sealedBidVault = new();
    private readonly Dictionary<Guid, decimal> _hiddenHpsVault = new();

    private sealed record SecretBidPayload(
        decimal OfferedPriceHp,
        decimal DeclaredTkdnPct,
        decimal DeclaredBmpPct
    );

    public ESourcingTenderAppService(
        EnvelopeCryptoService cryptoService,
        TkdnHeaCalculatorService heaCalculator)
    {
        _cryptoService = cryptoService;
        _heaCalculator = heaCalculator;
        _committeeRsaKey = RSA.Create(2048);
        SeedInitialActiveTender();
    }

    public IReadOnlyList<TenderDetailDto> GetAllTenders() => _tenders.ToList();

    public TenderDetailDto? GetTenderById(Guid id) => _tenders.FirstOrDefault(t => t.Id == id);

    /// <summary>
    /// Menambahkan pertanyaan/jawaban di Forum Aanwijzing dengan identitas peserta disamarkan ("Peserta #XX").
    /// </summary>
    public TenderDetailDto AddAanwijzingQa(Guid tenderId, AskAanwijzingQuestionRequestDto input)
    {
        int idx = _tenders.FindIndex(t => t.Id == tenderId);
        if (idx < 0)
            throw new KeyNotFoundException("Tender tidak ditemukan.");

        var tender = _tenders[idx];
        var qa = new AanwijzingQaDto(
            Id: Guid.NewGuid(),
            MaskedParticipantAlias: input.MaskedParticipantAlias,
            DocumentSectionRef: input.DocumentSectionRef,
            QuestionText: input.QuestionText,
            AnswerText: input.CommitteeAnswerText,
            AnsweredByCommitteeName: string.IsNullOrWhiteSpace(input.CommitteeAnswerText) ? null : "Ketua Pokja Pemilihan UKPBJ",
            AskedAtUtc: DateTimeOffset.UtcNow,
            AnsweredAtUtc: string.IsNullOrWhiteSpace(input.CommitteeAnswerText) ? null : DateTimeOffset.UtcNow
        );

        var updatedForum = new List<AanwijzingQaDto>(tender.AanwijzingForum) { qa };
        var updated = tender with { AanwijzingForum = updatedForum };
        _tenders[idx] = updated;
        return updated;
    }

    /// <summary>
    /// Vendor mengunggah Sampul Penawaran ke Encrypted Bid Vault.
    /// Harga penawaran (HP) dan deklarasi TKDN disegel dengan AES-256-GCM + RSA sehingga bernilai NULL sebelum Bid Opening Ceremony.
    /// </summary>
    public TenderDetailDto SubmitEncryptedVendorBid(Guid tenderId, SubmitEncryptedVendorBidRequestDto input)
    {
        int idx = _tenders.FindIndex(t => t.Id == tenderId);
        if (idx < 0)
            throw new KeyNotFoundException("Tender tidak ditemukan.");

        var tender = _tenders[idx];
        if (input.OfferedPriceHp <= 0)
            throw new ArgumentOutOfRangeException(nameof(input), "Harga penawaran harus lebih besar dari 0.");

        var submissionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        string payloadJson = JsonSerializer.Serialize(new SecretBidPayload(
            input.OfferedPriceHp,
            input.DeclaredTkdnPct,
            input.DeclaredBmpPct));

        // Jadwal pembukaan diset siap dibuka saat seremoni Pokja
        var sealedPkg = _cryptoService.SealEnvelope(
            payloadJson,
            _committeeRsaKey,
            scheduledOpeningUtc: now.AddSeconds(-1),
            currentUtcNow: now);

        _sealedBidVault[submissionId] = sealedPkg;

        string cipherPreview = Convert.ToHexStringLower(sealedPkg.CiphertextWithTag)[..Math.Min(40, sealedPkg.CiphertextWithTag.Length * 2)] + "...";

        var submissionDto = new EncryptedBidSubmissionDto(
            Id: submissionId,
            ParticipantId: participantId,
            VendorId: input.VendorId,
            VendorName: input.VendorName,
            BusinessScale: input.BusinessScale,
            SubmissionReceiptNo: $"BVAULT-{DateTime.UtcNow:yyyy}-{tender.BidSubmissions.Count + 1:D3}",
            Envelope1AdminTechStatus: input.TechnicalSelfScore >= 70m ? "OPENED_PASSED" : "OPENED_FAILED",
            TechnicalScore: input.TechnicalSelfScore,
            Envelope2CommercialCipherPreview: cipherPreview,
            PayloadSha256Hash: sealedPkg.PayloadSha256Hash,
            IsEnvelope2Opened: false,
            DecryptedOfferedPriceHp: null, // Terkunci di dalam Encrypted Bid Vault!
            DeclaredTkdnPct: null,
            DeclaredBmpPct: null,
            BidBondNumber: input.BidBondNumber,
            BidBondAmount: input.BidBondAmount,
            SubmittedAtUtc: now
        );

        var updatedSubs = new List<EncryptedBidSubmissionDto>(tender.BidSubmissions) { submissionDto };
        var updated = tender with { BidSubmissions = updatedSubs };
        _tenders[idx] = updated;
        return updated;
    }

    /// <summary>
    /// Pokja Pemilihan menjalankan Bid Opening Ceremony (Pembukaan Sampul 2 Harga):
    /// 1. Mendekripsi HPS / Owner Estimate PPK.
    /// 2. Mendekripsi Sampul 2 (Harga & TKDN) HANYA untuk peserta yang lulus evaluasi Administrasi & Teknis (Sampul 1).
    /// 3. Menghitung Koefisien Preferensi (KP), Harga Evaluasi Akhir (HEA), kewajaran harga (< 80% HPS), dan memeringkat peserta.
    /// 4. Membuka sesi Live e-Reverse Auction.
    /// </summary>
    public TenderDetailDto ExecuteBidOpeningAndEvaluateHea(Guid tenderId)
    {
        int idx = _tenders.FindIndex(t => t.Id == tenderId);
        if (idx < 0)
            throw new KeyNotFoundException("Tender tidak ditemukan.");

        var tender = _tenders[idx];
        decimal hpsAmount = _hiddenHpsVault[tenderId];
        var now = DateTimeOffset.UtcNow;

        var decryptedSubmissions = new List<EncryptedBidSubmissionDto>();
        var candidatesForHea = new List<BidEvaluationCandidate>();

        foreach (var sub in tender.BidSubmissions)
        {
            bool passedTech = sub.Envelope1AdminTechStatus == "OPENED_PASSED";
            if (!passedTech)
            {
                // Peserta yang gugur teknis Sampul 2-nya TIDAK BOLEH dibuka!
                decryptedSubmissions.Add(sub);
                continue;
            }

            var sealedPkg = _sealedBidVault[sub.Id];
            string plaintextJson = _cryptoService.OpenSealedEnvelope(
                sealedPkg,
                _committeeRsaKey,
                currentCeremonyUtc: now,
                isTechnicalStagePassed: true);

            var payload = JsonSerializer.Deserialize<SecretBidPayload>(plaintextJson)!;

            var openedSub = sub with
            {
                IsEnvelope2Opened = true,
                DecryptedOfferedPriceHp = payload.OfferedPriceHp,
                DeclaredTkdnPct = payload.DeclaredTkdnPct,
                DeclaredBmpPct = payload.DeclaredBmpPct
            };

            decryptedSubmissions.Add(openedSub);

            candidatesForHea.Add(new BidEvaluationCandidate(
                ParticipantId: openedSub.ParticipantId,
                VendorName: openedSub.VendorName,
                OfferedPriceHp: payload.OfferedPriceHp,
                VerifiedTkdnPct: payload.DeclaredTkdnPct,
                VerifiedBmpPct: payload.DeclaredBmpPct,
                TechnicalScore: openedSub.TechnicalScore ?? 80m
            ));
        }

        var heaRankings = _heaCalculator.RankCandidates(
            candidatesForHea,
            hpsAmount,
            tender.Category,
            tender.TechnicalWeightPct,
            tender.CommercialWeightPct)
            .Select(MapToDto)
            .ToList();

        // Inisialisasi sesi e-Reverse Auction untuk peserta yang lolos
        var auctionSession = new EAuctionSessionDto(
            Id: Guid.NewGuid(),
            MinDecrementAmount: 25_000_000m, // Minimal penurunan Rp 25 Juta per bid
            ScheduledStartUtc: now,
            ScheduledEndUtc: now.AddMinutes(60),
            CurrentEffectiveEndUtc: now.AddMinutes(60),
            AutoExtendWindowSeconds: 180, // 3 menit terakhir
            AutoExtendDurationSeconds: 300, // Tambah 5 menit
            CurrentExtensionCount: 0,
            MaxExtensionCount: 5,
            Status: "LIVE_BIDDING",
            BidHistory: []
        );

        var updated = tender with
        {
            IsHpsDecrypted = true,
            DecryptedHpsAmount = hpsAmount,
            CurrentStage = "E_REVERSE_AUCTION_LIVE",
            BidSubmissions = decryptedSubmissions,
            HeaRankings = heaRankings,
            EAuctionSession = auctionSession
        };

        _tenders[idx] = updated;
        return updated;
    }

    /// <summary>
    /// Peserta memasukkan harga penawaran baru yang lebih rendah di ruang e-Reverse Auction:
    /// - Memvalidasi penurunan minimal (MinDecrementAmount).
    /// - Menghitung ulang HEA TKDN dan memperbarui peringkat seluruh peserta secara real-time.
    /// - Memicu Anti-Sniping Auto-Extend (+5 menit) apabila bid dimasukkan di dalam 180 detik (3 menit) terakhir!
    /// </summary>
    public TenderDetailDto SubmitEAuctionBid(Guid tenderId, SubmitEAuctionBidRequestDto input)
    {
        int idx = _tenders.FindIndex(t => t.Id == tenderId);
        if (idx < 0)
            throw new KeyNotFoundException("Tender tidak ditemukan.");

        var tender = _tenders[idx];
        if (tender.EAuctionSession is null || tender.DecryptedHpsAmount is null)
            throw new InvalidOperationException("Sesi e-Reverse Auction belum dibuka. Lakukan Bid Opening Ceremony terlebih dahulu.");

        var session = tender.EAuctionSession;
        var currentRankItem = tender.HeaRankings.FirstOrDefault(r => r.ParticipantId == input.ParticipantId);
        if (currentRankItem is null)
            throw new KeyNotFoundException("Peserta tidak terdaftar dalam sesi e-Reverse Auction.");

        decimal maxAllowedBid = currentRankItem.OfferedPriceHp - session.MinDecrementAmount;
        if (input.NewLoweredBidPriceHp > maxAllowedBid)
        {
            throw new InvalidOperationException(
                $"Eproc:EAuction:InsufficientDecrement - Harga bid baru (Rp {input.NewLoweredBidPriceHp:N2}) harus turun minimal Rp {session.MinDecrementAmount:N2} dari harga sebelumnya (Rp {currentRankItem.OfferedPriceHp:N2}).");
        }

        // Cek apakah masuk dalam jendela Anti-Sniping (<= 180 detik sebelum waktu habis)
        bool triggerExtend = input.SecondsBeforeEndSimulated <= session.AutoExtendWindowSeconds
                             && session.CurrentExtensionCount < session.MaxExtensionCount;

        int newExtCount = triggerExtend ? session.CurrentExtensionCount + 1 : session.CurrentExtensionCount;
        var newEffectiveEnd = triggerExtend
            ? session.CurrentEffectiveEndUtc.AddSeconds(session.AutoExtendDurationSeconds)
            : session.CurrentEffectiveEndUtc;

        var (_, _, newHea) = _heaCalculator.CalculateHea(
            input.NewLoweredBidPriceHp,
            currentRankItem.VerifiedTkdnPct,
            tender.Category);

        var bidLog = new EAuctionBidLogDto(
            BidSequence: session.BidHistory.Count + 1,
            ParticipantId: input.ParticipantId,
            MaskedBidderAlias: $"Bidder-{input.ParticipantId.ToString()[..4].ToUpperInvariant()}",
            VendorName: currentRankItem.VendorName,
            SubmittedPriceHp: input.NewLoweredBidPriceHp,
            RecalculatedHea: newHea,
            TriggeredAntiSnipingAutoExtend: triggerExtend,
            BidTimestampUtc: DateTimeOffset.UtcNow
        );

        var updatedHistory = new List<EAuctionBidLogDto>(session.BidHistory) { bidLog };
        var updatedSession = session with
        {
            CurrentEffectiveEndUtc = newEffectiveEnd,
            CurrentExtensionCount = newExtCount,
            BidHistory = updatedHistory
        };

        // Perbarui harga peserta dan hitung ulang peringkat HEA
        var updatedSubmissions = tender.BidSubmissions.Select(s =>
            s.ParticipantId == input.ParticipantId
                ? s with { DecryptedOfferedPriceHp = input.NewLoweredBidPriceHp }
                : s).ToList();

        var updatedCandidates = updatedSubmissions
            .Where(s => s.IsEnvelope2Opened && s.DecryptedOfferedPriceHp.HasValue)
            .Select(s => new BidEvaluationCandidate(
                ParticipantId: s.ParticipantId,
                VendorName: s.VendorName,
                OfferedPriceHp: s.DecryptedOfferedPriceHp!.Value,
                VerifiedTkdnPct: s.DeclaredTkdnPct ?? 0m,
                VerifiedBmpPct: s.DeclaredBmpPct ?? 0m,
                TechnicalScore: s.TechnicalScore ?? 80m))
            .ToList();

        var newRankings = _heaCalculator.RankCandidates(
            updatedCandidates,
            tender.DecryptedHpsAmount.Value,
            tender.Category,
            tender.TechnicalWeightPct,
            tender.CommercialWeightPct)
            .Select(MapToDto)
            .ToList();

        var updatedTender = tender with
        {
            BidSubmissions = updatedSubmissions,
            HeaRankings = newRankings,
            EAuctionSession = updatedSession
        };

        _tenders[idx] = updatedTender;
        return updatedTender;
    }

    /// <summary>
    /// Menyelesaikan Negosiasi BAFO (Best and Final Offer), menghitung penghematan (Cost Saving terhadap HPS & Pagu),
    /// menerbitkan BAHP (Penetapan Pemenang), dan membuka Masa Sanggah.
    /// </summary>
    public TenderDetailDto FinalizeBafoAndAnnounceWinner(Guid tenderId, FinalizeBafoAndWinnerRequestDto input)
    {
        int idx = _tenders.FindIndex(t => t.Id == tenderId);
        if (idx < 0)
            throw new KeyNotFoundException("Tender tidak ditemukan.");

        var tender = _tenders[idx];
        if (tender.DecryptedHpsAmount is null)
            throw new InvalidOperationException("HPS belum didekripsi.");

        var winnerRank = tender.HeaRankings.FirstOrDefault(r => r.ParticipantId == input.WinningParticipantId);
        if (winnerRank is null)
            throw new KeyNotFoundException("Peserta pemenang tidak ditemukan dalam daftar evaluasi HEA.");

        decimal hps = tender.DecryptedHpsAmount.Value;
        decimal pagu = tender.TotalPaguBudget;
        decimal bafoPrice = input.FinalBafoAgreedPrice;

        if (bafoPrice > winnerRank.OfferedPriceHp)
            throw new InvalidOperationException("Harga BAFO tidak boleh lebih tinggi dari harga penawaran/e-Auction terakhir.");

        var (_, _, finalHea) = _heaCalculator.CalculateHea(bafoPrice, winnerRank.VerifiedTkdnPct, tender.Category);

        decimal savingToHps = hps - bafoPrice;
        decimal savingToHpsPct = Math.Round((savingToHps / hps) * 100m, 2, MidpointRounding.AwayFromZero);
        decimal savingToPagu = pagu - bafoPrice;
        decimal savingToPaguPct = Math.Round((savingToPagu / pagu) * 100m, 2, MidpointRounding.AwayFromZero);

        bool isBelow80Pct = (bafoPrice / hps) < 0.80m;
        decimal perfBond = isBelow80Pct
            ? Math.Round(hps * 0.05m, 2, MidpointRounding.AwayFromZero)
            : Math.Round(bafoPrice * 0.05m, 2, MidpointRounding.AwayFromZero);

        var winnerSummary = new WinnerDeterminationSummaryDto(
            ParticipantId: winnerRank.ParticipantId,
            WinnerVendorName: winnerRank.VendorName,
            BahpDocumentNumber: input.BahpNumber,
            InitialBidPriceHp: winnerRank.OfferedPriceHp,
            PostAuctionPriceHp: winnerRank.OfferedPriceHp,
            FinalBafoContractPrice: bafoPrice,
            FinalCalculatedHea: finalHea,
            CommittedTkdnPct: winnerRank.VerifiedTkdnPct,
            CostSavingToHpsAmount: savingToHps,
            CostSavingToHpsPct: savingToHpsPct,
            CostSavingToPaguAmount: savingToPagu,
            CostSavingToPaguPct: savingToPaguPct,
            RequiredPerformanceBondAmount: perfBond,
            AnnouncedAtUtc: DateTimeOffset.UtcNow
        );

        var updated = tender with
        {
            CurrentStage = "OBJECTION_SANGGAH_PERIOD",
            WinnerDetermination = winnerSummary
        };

        _tenders[idx] = updated;
        return updated;
    }

    /// <summary>
    /// Peserta yang kalah mengajukan Sanggahan pada Masa Sanggah.
    /// </summary>
    public TenderDetailDto SubmitObjectionSanggah(Guid tenderId, SubmitSanggahRequestDto input)
    {
        int idx = _tenders.FindIndex(t => t.Id == tenderId);
        if (idx < 0)
            throw new KeyNotFoundException("Tender tidak ditemukan.");

        var tender = _tenders[idx];
        var sanggah = new TenderObjectionSanggahDto(
            Id: Guid.NewGuid(),
            ParticipantId: input.ParticipantId,
            VendorName: input.VendorName,
            ObjectionLevel: input.ObjectionLevel,
            ObjectionCategory: input.ObjectionCategory,
            ObjectionLetterNo: input.ObjectionLetterNo,
            ObjectionDetails: input.ObjectionDetails,
            RulingDecision: "PENDING",
            ResponseLetterNo: null,
            ResponseDetails: null,
            SubmittedAtUtc: DateTimeOffset.UtcNow,
            RespondedAtUtc: null
        );

        var list = new List<TenderObjectionSanggahDto>(tender.ObjectionsSanggah) { sanggah };
        var updated = tender with { ObjectionsSanggah = list };
        _tenders[idx] = updated;
        return updated;
    }

    /// <summary>
    /// Pokja / PA-KPA menjawab dan memutuskan Sanggahan.
    /// Jika sanggahan ditolak (REJECTED_TENDER_CONTINUES), status tender beralih menjadi READY_FOR_SPPBJ (siap masuk Fase 4 Kontrak).
    /// </summary>
    public TenderDetailDto ResolveObjectionSanggah(Guid tenderId, ResolveSanggahRequestDto input)
    {
        int idx = _tenders.FindIndex(t => t.Id == tenderId);
        if (idx < 0)
            throw new KeyNotFoundException("Tender tidak ditemukan.");

        var tender = _tenders[idx];
        string ruling = input.AcceptObjection ? "ACCEPTED_REEVALUATION" : "REJECTED_TENDER_CONTINUES";

        var updatedList = tender.ObjectionsSanggah.Select(o =>
            o.Id == input.ObjectionId
                ? o with
                {
                    RulingDecision = ruling,
                    ResponseLetterNo = input.ResponseLetterNo,
                    ResponseDetails = input.ResponseDetails,
                    RespondedAtUtc = DateTimeOffset.UtcNow
                }
                : o).ToList();

        bool allResolvedRejected = updatedList.All(o => o.RulingDecision == "REJECTED_TENDER_CONTINUES");

        var updated = tender with
        {
            ObjectionsSanggah = updatedList,
            CurrentStage = allResolvedRejected ? "READY_FOR_SPPBJ" : "EVALUATION_REVIEW"
        };

        _tenders[idx] = updated;
        return updated;
    }

    private void SeedInitialActiveTender()
    {
        var tenderId = Guid.NewGuid();
        var packageId = Guid.NewGuid();

        // Simpan nilai HPS rahasia (Rp 6.000.000.000) di brankas terenkripsi
        _hiddenHpsVault[tenderId] = 6_000_000_000m;

        var initialTender = new TenderDetailDto(
            Id: tenderId,
            PackageId: packageId,
            TenderNumber: "TND-BUMN-2026-0401",
            TenderTitle: "Pengadaan Konsolidasi Gate Valve Carbon Steel API 600 DN300 (50 Unit Lintas Regional)",
            Category: ProcurementCategory.Goods,
            SourcingMethod: SourcingMethod.TenderUmum,
            EnvelopeMethod: SubmissionEnvelopeMethod.DuaSampul,
            EvaluationMethod: EvaluationMethod.SistemNilaiScoring,
            TechnicalWeightPct: 30m,
            CommercialWeightPct: 70m,
            TotalPaguBudget: 6_250_000_000m,
            IsHpsDecrypted: false,
            DecryptedHpsAmount: null, // Terkunci sebelum Bid Opening Ceremony!
            CurrentStage: "AANWIJZING_AND_BID_SUBMISSION",
            AanwijzingForum:
            [
                new(
                    Id: Guid.NewGuid(),
                    MaskedParticipantAlias: "Peserta #01 (Disamarkan)",
                    DocumentSectionRef: "BAB IV - Spesifikasi Teknis & Sertifikat TKDN",
                    QuestionText: "Apakah sertifikat TKDN Kemenperin minimal 40% wajib dilampirkan pada Sampul 1 atau Sampul 2?",
                    AnswerText: "Salinan sertifikat TKDN dilampirkan pada Sampul 1 (Teknis) dan nilai persentase capaian TKDN dideklarasikan pada Sampul 2 (Komersial) untuk perhitungan HEA.",
                    AnsweredByCommitteeName: "Ketua Pokja Pemilihan UKPBJ",
                    AskedAtUtc: DateTimeOffset.UtcNow.AddHours(-5),
                    AnsweredAtUtc: DateTimeOffset.UtcNow.AddHours(-4))
            ],
            BidSubmissions: [],
            HeaRankings: [],
            EAuctionSession: null,
            WinnerDetermination: null,
            ObjectionsSanggah: []
        );

        _tenders.Add(initialTender);

        // Seed 3 Sampul Penawaran Terenkripsi di Encrypted Bid Vault:
        // 1. PT Mitra Teknik Manufaktur Lokal (Produk Dalam Negeri TKDN 60%, Harga Rp 5.70 Miliar -> Menang via Preferensi HEA!)
        SubmitEncryptedVendorBid(tenderId, new SubmitEncryptedVendorBidRequestDto(
            VendorId: Guid.NewGuid(),
            VendorName: "PT Mitra Teknik Manufaktur Lokal",
            BusinessScale: "MENENGAH",
            OfferedPriceHp: 5_700_000_000m,
            DeclaredTkdnPct: 60.0m,
            DeclaredBmpPct: 12.0m,
            TechnicalSelfScore: 88.0m,
            BidBondNumber: "BG-MANDIRI-2026-9901",
            BidBondAmount: 120_000_000m));

        // 2. PT Global Valve Importindo (Barang Impor TKDN 10% < 25%, Harga lebih murah Rp 5.35 Miliar -> Kalah HEA terhadap produk lokal!)
        SubmitEncryptedVendorBid(tenderId, new SubmitEncryptedVendorBidRequestDto(
            VendorId: Guid.NewGuid(),
            VendorName: "PT Global Valve Importindo",
            BusinessScale: "BESAR",
            OfferedPriceHp: 5_350_000_000m,
            DeclaredTkdnPct: 10.0m,
            DeclaredBmpPct: 0.0m,
            TechnicalSelfScore: 84.0m,
            BidBondNumber: "BG-BNI-2026-7742",
            BidBondAmount: 120_000_000m));

        // 3. PT Karya Infrastruktur Nusantara Tbk (Produk Dalam Negeri TKDN 45%, Harga Rp 5.85 Miliar)
        SubmitEncryptedVendorBid(tenderId, new SubmitEncryptedVendorBidRequestDto(
            VendorId: Guid.NewGuid(),
            VendorName: "PT Karya Infrastruktur Nusantara Tbk",
            BusinessScale: "BESAR",
            OfferedPriceHp: 5_850_000_000m,
            DeclaredTkdnPct: 45.0m,
            DeclaredBmpPct: 15.0m,
            TechnicalSelfScore: 90.0m,
            BidBondNumber: "BG-BRI-2026-3318",
            BidBondAmount: 125_000_000m));
    }

    private static HeaRankingItemDto MapToDto(HeaEvaluationResult r) => new(
        ParticipantId: r.ParticipantId,
        VendorName: r.VendorName,
        OfferedPriceHp: r.OfferedPriceHp,
        VerifiedTkdnPct: r.VerifiedTkdnPct,
        VerifiedBmpPct: r.VerifiedBmpPct,
        PreferenceCoefficientKp: r.KoefisienPreferensiKp,
        CalculatedHea: r.CalculatedHea,
        PriceToHpsRatioPct: r.PercentageToHps,
        RequiresAbnormallyLowClarification: r.RequiresPriceFairnessClarification,
        RequiredPerformanceBondAmount: r.RequiredPerformanceBondAmount,
        IsAboveHpsCeiling: r.IsAboveHps,
        CommercialRank: r.FinalRank,
        FinalCombinedScore: r.FinalCombinedScore,
        FinalRank: r.FinalRank
    );
}
