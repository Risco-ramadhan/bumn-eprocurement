using System.Security.Cryptography;
using Eproc.CatalogAndClm;
using Eproc.Configuration;
using Eproc.Enums;
using Eproc.ExecutionAndFinance;
using Eproc.Permissions;
using Eproc.Planning;
using Eproc.Services;
using Eproc.Sourcing;
using Eproc.Vms;
using Eproc.Workflows;
using Xunit;

namespace Eproc.Tests;

public class BumnEprocDomainTests
{
    [Fact]
    public void TkdnHeaCalculator_LocalProductWithHighTkdn_WinsAgainstCheaperImportProductViaHea()
    {
        var calculator = new TkdnHeaCalculatorService();
        decimal hps = 10_000_000_000m; // Rp 10 Miliar

        var candidates = new List<BidEvaluationCandidate>
        {
            // Vendor A (Barang Impor, TKDN 10% < 25%, Harga Rp 9.0 Miliar)
            new(Guid.NewGuid(), "PT Impor Global", OfferedPriceHp: 9_000_000_000m, VerifiedTkdnPct: 10m, VerifiedBmpPct: 0m, TechnicalScore: 85m),
            // Vendor B (Produk Dalam Negeri, TKDN 60% >= 25%, Harga Rp 9.5 Miliar)
            // KP = 60% * 25% = 0.15 -> HEA = (1 / 1.15) * 9.5 Miliar = Rp 8,260,869,565.22
            new(Guid.NewGuid(), "PT Karya Lokal Nusantara", OfferedPriceHp: 9_500_000_000m, VerifiedTkdnPct: 60m, VerifiedBmpPct: 10m, TechnicalScore: 85m)
        };

        var ranked = calculator.RankCandidates(candidates, hps, ProcurementCategory.Goods);

        Assert.Equal(2, ranked.Count);
        Assert.Equal("PT Karya Lokal Nusantara", ranked[0].VendorName);
        Assert.Equal(1, ranked[0].FinalRank);
        Assert.True(ranked[0].IsPreferenceEligible);
        Assert.Equal(0.15m, ranked[0].KoefisienPreferensiKp);
        Assert.Equal(8_260_869_565.22m, ranked[0].CalculatedHea);
        // Nilai kontrak tetap harga penawaran asli (Rp 9.5 Miliar), namun menang peringkat lewat HEA
        Assert.Equal(9_500_000_000m, ranked[0].OfferedPriceHp);
    }

    [Fact]
    public void TkdnHeaCalculator_BidBelow80PercentHps_FlagsPriceFairnessAndRaisesPerfBondTo5PercentHps()
    {
        var calculator = new TkdnHeaCalculatorService();
        decimal hps = 1_000_000_000m; // Rp 1 Miliar

        var candidates = new List<BidEvaluationCandidate>
        {
            new(Guid.NewGuid(), "PT Banting Harga", OfferedPriceHp: 750_000_000m, VerifiedTkdnPct: 30m, VerifiedBmpPct: 5m, TechnicalScore: 80m)
        };

        var ranked = calculator.RankCandidates(candidates, hps, ProcurementCategory.Construction);
        var result = ranked.Single();

        Assert.True(result.RequiresPriceFairnessClarification);
        // Karena < 80% HPS, Jaminan Pelaksanaan wajib 5% dari HPS (Rp 50 Juta), bukan 5% dari HP (Rp 37.5 Juta)
        Assert.Equal(50_000_000m, result.RequiredPerformanceBondAmount);
    }

    [Fact]
    public void VendorFinancialScoring_CalculatesKdSknAltmanAndEnforcesCsmsRiskGuard()
    {
        var service = new VendorFinancialScoringService();

        var profile = new VendorFinancialProfileInput(
            BusinessScale: "BESAR",
            CurrentAssets: 50_000_000_000m,
            TotalAssets: 100_000_000_000m,
            CurrentLiabilities: 20_000_000_000m,
            TotalLiabilities: 40_000_000_000m,
            NetWorthEquityKb: 60_000_000_000m,
            RetainedEarnings: 25_000_000_000m,
            Ebit: 15_000_000_000m,
            HighestExperienceValueNpt: 20_000_000_000m,
            TotalOngoingContractsValue: 50_000_000_000m,
            ActiveOngoingPackagesCount: 2
        );

        // Paket High Risk dengan HPS Rp 40 Miliar, tetapi Vendor hanya punya sertifikat CSMS Medium
        var failedCsmsResult = service.AssessVendorForPackage(
            profile,
            packageHpsAmount: 40_000_000_000m,
            requiredPackageCsmsLevel: CsmsRiskLevel.High,
            vendorCertifiedCsmsLevel: CsmsRiskLevel.Medium);

        // KD = 3 * 20 Miliar = 60 Miliar (>= 40 Miliar -> Lulus KD)
        Assert.Equal(60_000_000_000m, failedCsmsResult.KemampuanDasarKd);
        Assert.True(failedCsmsResult.PassesKdRequirement);
        Assert.True(failedCsmsResult.PassesSknRequirement);
        Assert.Equal("SAFE_ZONE", failedCsmsResult.FinancialHealthZone);
        // Gagal CSMS karena High > Medium
        Assert.False(failedCsmsResult.PassesCsmsRequirement);
        Assert.False(failedCsmsResult.IsOverallQualified);

        // Jika Vendor memiliki sertifikat CSMS High, maka Lulus Keseluruhan
        var passedResult = service.AssessVendorForPackage(
            profile,
            packageHpsAmount: 40_000_000_000m,
            requiredPackageCsmsLevel: CsmsRiskLevel.High,
            vendorCertifiedCsmsLevel: CsmsRiskLevel.High);

        Assert.True(passedResult.IsOverallQualified);
    }

    [Fact]
    public void EnvelopeCryptoService_BlocksDecryptionBeforeScheduleAndDetectsTampering()
    {
        var crypto = new EnvelopeCryptoService();
        using var committeeRsa = RSA.Create(2048);

        var now = new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);
        var scheduledOpening = now.AddDays(3);
        string secretBidPayload = """{"vendor":"PT Nusantara","totalBidPrice":14500000000,"tkdn":45.5}""";

        var sealedEnvelope = crypto.SealEnvelope(secretBidPayload, committeeRsa, scheduledOpening, now);

        // 1. Coba buka sebelum jadwal pembukaan -> Wajib ditolak!
        var prematureAttempt = now.AddDays(1);
        var lockEx = Assert.Throws<InvalidOperationException>(() =>
            crypto.OpenSealedEnvelope(sealedEnvelope, committeeRsa, prematureAttempt));
        Assert.Contains("Eproc:BidOpening:ScheduleLocked", lockEx.Message);

        // 2. Coba buka Sampul 2 untuk peserta yang tidak lulus teknis -> Wajib ditolak!
        var techEx = Assert.Throws<InvalidOperationException>(() =>
            crypto.OpenSealedEnvelope(sealedEnvelope, committeeRsa, scheduledOpening.AddMinutes(5), isTechnicalStagePassed: false));
        Assert.Contains("Eproc:BidOpening:TechnicalNotPassed", techEx.Message);

        // 3. Buka saat jadwal tiba & lulus teknis -> Berhasil!
        string decrypted = crypto.OpenSealedEnvelope(sealedEnvelope, committeeRsa, scheduledOpening.AddMinutes(5), isTechnicalStagePassed: true);
        Assert.Equal(secretBidPayload, decrypted);

        // 4. Jika ciphertext di database dimanipulasi 1 byte oleh oknum DBA -> Terdeteksi SHA-256 Integrity Violation!
        sealedEnvelope.CiphertextWithTag[0] ^= 0xFF;
        Assert.Throws<CryptographicException>(() =>
            crypto.OpenSealedEnvelope(sealedEnvelope, committeeRsa, scheduledOpening.AddMinutes(5)));
    }

    [Fact]
    public void ThreeWayMatchingService_CalculatesLatePenaltyCapRetentionDpAndWapuTaxAccurately()
    {
        var matchingService = new ThreeWayMatchingService();

        // Kontrak Konstruksi Rp 10 Miliar, Tagihan Termin Rp 2 Miliar, Terlambat 60 hari
        // Denda 60 hari * 1/1000 * 10 Miliar = Rp 600 Juta -> Terkena Cap Maksimal 5% Kontrak = Rp 500 Juta!
        var input = new ThreeWayMatchingInput(
            ContractTotalValue: 10_000_000_000m,
            ApprovedBastOrGrGrossValue: 2_000_000_000m,
            VendorInvoiceClaimedDpp: 2_000_000_000m,
            DelayCalendarDays: 60,
            PenaltyBasisAmount: 10_000_000_000m,
            SlaBreachPenaltyAmount: 0m,
            TkdnShortfallPenaltyAmount: 0m,
            DownPaymentRecoupmentPct: 10m, // Potong DP 10% = Rp 200 Juta
            RetentionDeductionPct: 5m,     // Tahan Retensi 5% = Rp 100 Juta
            IsClientWapuPpn: true,         // BUMN WAPU (PPN 11% disetor langsung ke negara)
            PpnRatePct: 11m,
            PphType: PphTaxType.Pph4Ayat2Konstruksi,
            PphRatePct: 2.65m              // PPh Final Konstruksi 2.65%
        );

        var result = matchingService.CalculateInvoiceSettlement(input);

        Assert.True(result.IsThreeWayMatched);
        Assert.Equal(600_000_000m, result.RawLatePenaltyAmount);
        Assert.Equal(500_000_000m, result.CappedLatePenaltyAmount); // Ter-cap di 5% (Rp 500 Juta)
        Assert.Equal(200_000_000m, result.DpRecoupmentDeduction);
        Assert.Equal(100_000_000m, result.RetentionDeductionAmount);

        // DPP setelah DP & Retensi = 2 Miliar - 200 Juta - 100 Juta = Rp 1.7 Miliar
        // DPP setelah Denda = 1.7 Miliar - 500 Juta = Rp 1.2 Miliar
        Assert.Equal(1_200_000_000m, result.DppNetAfterDeductions);

        // PPh 2.65% dari 1.7 Miliar = Rp 45,050,000
        Assert.Equal(45_050_000m, result.PphWithheldAmount);
        // Karena BUMN WAPU, PPN dipungut langsung sehingga Net Payable = 1.2 Miliar - 45,050,000 = Rp 1,154,950,000
        Assert.Equal(1_154_950_000m, result.NetPayableToVendor);
    }

    [Fact]
    public void DoaAndSodEngine_BlocksConflictingRolesAndRoutesMultiBillionApprovals()
    {
        var engine = new DoaMatrixApprovalEngine();

        // PPK tidak boleh merangkap sebagai Anggota Pokja Pemilihan
        var (isValid, reason) = engine.ValidateSegregationOfDuties(["PPK", "POKJA_COMMITTEE"]);
        Assert.False(isValid);
        Assert.Contains("Eproc:SoD:RoleConflict", reason);

        // Nilai paket Rp 25 Miliar harus melewati 5 jenjang persetujuan hingga Direksi Kolektif
        var route = engine.ResolveApprovalRoute(25_000_000_000m);
        Assert.Equal(5, route.Count);
        Assert.Equal("BOARD_OF_DIRECTORS_COLLEGIATE", route.Last().ApproverPositionGrade);
    }

    [Fact]
    public void AuditHashChainAndScurveService_DetectsTamperingAndTriggersScmLevels()
    {
        var auditService = new AuditHashChainService();

        var rec1 = auditService.CreateLinkedRecord(
            Guid.NewGuid(), "PPK", "DEMAND", "SEAL_HPS", "owner_estimates_encrypted", Guid.NewGuid(), """{"hps":"sealed"}""");
        var rec2 = auditService.CreateLinkedRecord(
            Guid.NewGuid(), "POKJA_COMMITTEE", "TENDER", "PUBLISH_TENDER", "tenders", Guid.NewGuid(), """{"tender":"TND-001"}""", rec1.CurrentRecordHash);

        var chain = new List<AuditChainRecord> { rec1, rec2 };
        Assert.True(auditService.VerifyChainIntegrity(chain));

        // Evaluasi Kurva S: Rencana 50%, Aktual 35% (Deviasi -15% pada Periode I <= 70% -> Kontrak Kritis -> Trigger SCM Level 1)
        var (deviation, isCritical, scmLevel) = auditService.EvaluateScurveDeviation(50m, 35m, ScmLevel.None);
        Assert.Equal(-15m, deviation);
        Assert.True(isCritical);
        Assert.Equal(ScmLevel.ScmLevel1, scmLevel);
    }

    [Fact]
    public void ZeroFrameworkFingerprintAudit_AllPermissionsAndAssembliesUseEprocPrefixOnly()
    {
        var allPermissions = EprocPermissions.GetAllPermissions();
        Assert.NotEmpty(allPermissions);
        Assert.All(allPermissions, perm =>
        {
            Assert.StartsWith("eproc.", perm);
            Assert.DoesNotContain("abp", perm, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("volo", perm, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Phase1VmsCsmsService_RegistersVendorCertifiesCsmsDetectsBoAffiliationAndMaintainsAuditChain()
    {
        var vmsService = new VmsCsmsAppService(new VendorFinancialScoringService(), new AuditHashChainService());

        // 1. Cek vendor awal yang sudah di-seed (3 vendor)
        var initialVendors = vmsService.GetAllVendors();
        Assert.Equal(3, initialVendors.Count);

        // 2. Detektor Konflik Kepentingan (Beneficial Ownership) otomatis menemukan NIK yang sama
        // antara PT Mitra Teknik Manufaktur Lokal dan CV Cipta Sarana Elektrikal
        var conflicts = vmsService.DetectBeneficialOwnershipConflicts();
        Assert.Single(conflicts);
        Assert.Equal("SHARED_ULTIMATE_BENEFICIAL_OWNER", conflicts[0].ConflictRelationType);
        Assert.Equal("3578042209800003", conflicts[0].IdentityNumberNikNpwp);

        // 3. Verifikasi AVL & Sertifikasi CSMS untuk CV Cipta Sarana Elektrikal
        var unverifiedVendor = initialVendors.First(v => v.VerificationStatus == VendorVerificationStatus.Submitted);
        var verifiedVendor = vmsService.VerifyAndApproveVendorAvl(unverifiedVendor.Id, true, "Dokumen NIB & SPT Valid.");
        Assert.Equal(VendorVerificationStatus.VerifiedAvl, verifiedVendor.VerificationStatus);

        var csmsVendor = vmsService.SubmitAndCertifyCsms(unverifiedVendor.Id, new SubmitCsmsAssessmentRequestDto(
            TargetRiskCategory: CsmsRiskLevel.High,
            Elements:
            [
                new(1, "Kepemimpinan & Komitmen K3L", 50m, 80m, 75m),
                new(2, "HIRADC & JSA", 50m, 78m, 75m)
            ],
            FieldVerificationScore: 76m,
            AuditorName: "Ir. Dian (HSE Auditor)"));

        Assert.NotNull(csmsVendor.ActiveCsmsCertificate);
        Assert.Equal(CsmsRiskLevel.High, csmsVendor.ActiveCsmsCertificate!.QualifiedRiskLevel);

        // 4. Pastikan seluruh jejak audit di Immutable Hash-Chain valid 100%
        Assert.True(vmsService.IsAuditChainValid());
        Assert.True(vmsService.GetAuditChain().Count >= 6);
    }

    [Fact]
    public void Phase2PlanningAndDoaService_EnforcesRkapBudgetConsolidatesPrsSealsHpsAndExecutesMultiTierDoa()
    {
        var planningService = new PlanningAndDoaAppService(new EnvelopeCryptoService(), new DoaMatrixApprovalEngine());

        // 1. Cek Pagu RKAP awal & blokir PR jika nilai melebihi sisa pagu tersedia
        var opexBudget = planningService.GetRkapBudgets().First(b => b.BudgetType == "OPEX");
        Assert.Throws<InvalidOperationException>(() =>
            planningService.CreatePurchaseRequisition(new CreatePurchaseRequisitionRequestDto(
                RkapBudgetId: opexBudget.Id,
                PrTitle: "PR Melebihi Pagu RKAP",
                RequesterUnitName: "Cabang Medan",
                Category: ProcurementCategory.Goods,
                RequiredDeliveryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                RequiredCsmsLevel: CsmsRiskLevel.Low,
                Items:
                [
                    new(Guid.NewGuid(), 1, "KIMAP-OVER-99", "Barang Overbudget", 1m, "LOT", 999_000_000_000m, 999_000_000_000m, "Gudang Medan")
                ])));

        // 2. Konsolidasikan 2 PR Gate Valve dari Regional Jawa Barat (20 unit) & Jawa Timur (30 unit) menjadi 1 Paket Pengadaan (Total 50 unit, Rp 6.25 Miliar)
        var readyPrs = planningService.GetPurchaseRequisitions()
            .Where(p => p.Status == "APPROVED_READY_FOR_SOURCING")
            .ToList();
        Assert.Equal(2, readyPrs.Count);

        var consolidatedPkg = planningService.ConsolidatePrsToPackage(new ConsolidatePrToPackageRequestDto(
            PackageName: "Pengadaan Konsolidasi Gate Valve API 600 DN300 (Jabar & Jatim)",
            PurchaseRequisitionIds: readyPrs.Select(p => p.Id).ToList(),
            SourcingMethod: SourcingMethod.TenderUmum,
            EnvelopeMethod: SubmissionEnvelopeMethod.DuaSampul,
            EvaluationMethod: EvaluationMethod.HargaTerendahGugur,
            ContractType: ContractType.HargaSatuan,
            MinTkdnRequirementPct: 40.0m,
            EnableEReverseAuction: true));

        Assert.Equal(6_250_000_000m, consolidatedPkg.TotalPaguBudget);
        // Item dengan kode KIMAP yang sama otomatis digabung kuantitasnya (20 + 30 = 50 UNIT)
        Assert.Single(consolidatedPkg.BoqItems);
        Assert.Equal(50m, consolidatedPkg.BoqItems[0].Quantity);

        // 3. Segel HPS dengan Envelope Encryption (Rp 5.95 Miliar <= Pagu Rp 6.25 Miliar)
        var sealedPkg = planningService.SealPackageOwnerEstimateHps(consolidatedPkg.Id, new SealPackageHpsRequestDto(
            TotalHpsPlaintextAmount: 5_950_000_000m,
            CostHistoryNotes: "Benchmark Kontrak Payung Valve 2025",
            DaysUntilBidOpening: 5));

        Assert.NotNull(sealedPkg.SealedOwnerEstimate);
        // Pastikan nilai plaintext HPS disembunyikan (null) selama belum jadwal pembukaan penawaran!
        Assert.Null(sealedPkg.SealedOwnerEstimate!.DecryptedTotalHps);
        Assert.False(sealedPkg.SealedOwnerEstimate.IsOpenedAndDecrypted);
        Assert.Equal("HPS_SEALED_WAITING_DOA", sealedPkg.Status);

        // 4. Karena nilai paket Rp 6.25 Miliar (> 1 Miliar s.d. 10 Miliar), DOA memiliki 4 jenjang persetujuan
        // Step 1 (PPK) sudah otomatis APPROVED saat menyegel HPS, sekarang berada di Step 2 (BOD-2 VP/GM)
        var doaInstance = planningService.GetDoaInbox().First(d => d.Id == sealedPkg.DoaApprovalInstanceId);
        Assert.Equal(4, doaInstance.TotalStepsCount);
        Assert.Equal(2, doaInstance.CurrentStepSequence);

        // Setujui Step 2 (BOD-2 VP/GM), Step 3 (BOD-1 SVP), dan Step 4 (DIRECTOR_BIDANG)
        planningService.ActOnDoaApprovalTask(doaInstance.Id, new ActOnDoaTaskRequestDto("PA_KPA", "BOD-2_VP_GM", "VP Operasi", true, "Setuju lanjut SVP"));
        planningService.ActOnDoaApprovalTask(doaInstance.Id, new ActOnDoaTaskRequestDto("PA_KPA", "BOD-1_SVP", "SVP Supply Chain", true, "Setuju lanjut Direktur"));
        var finalDoa = planningService.ActOnDoaApprovalTask(doaInstance.Id, new ActOnDoaTaskRequestDto("PA_KPA", "DIRECTOR_BIDANG", "Direktur Operasi", true, "Disetujui tayang tender."));

        Assert.Equal("APPROVED", finalDoa.OverallStatus);
        var approvedPkg = planningService.GetPackages().First(p => p.Id == consolidatedPkg.Id);
        Assert.Equal("DOA_APPROVED_READY_FOR_TENDER", approvedPkg.Status);
    }

    [Fact]
    public void Phase3ESourcingTenderService_ExecutesBidVaultOpeningHeaRankingAntiSnipingAuctionBafoAndSanggah()
    {
        var tenderService = new ESourcingTenderAppService(new EnvelopeCryptoService(), new TkdnHeaCalculatorService());

        var tender = tenderService.GetAllTenders().First();

        // 1. Sebelum Bid Opening Ceremony: HPS dan seluruh Harga Penawaran di Encrypted Bid Vault harus NULL (Zero-Knowledge)!
        Assert.False(tender.IsHpsDecrypted);
        Assert.Null(tender.DecryptedHpsAmount);
        Assert.All(tender.BidSubmissions, sub => Assert.Null(sub.DecryptedOfferedPriceHp));

        // 2. Jalankan Bid Opening Ceremony & Evaluasi Otomatis HEA TKDN
        var openedTender = tenderService.ExecuteBidOpeningAndEvaluateHea(tender.Id);
        Assert.True(openedTender.IsHpsDecrypted);
        Assert.Equal(6_000_000_000m, openedTender.DecryptedHpsAmount);
        Assert.Equal(3, openedTender.HeaRankings.Count);

        // PT Mitra Teknik Manufaktur Lokal (TKDN 60%, HP Rp 5.70 Miliar) wajib Ranking 1 mengalahkan PT Global Valve Importindo (TKDN 10%, HP Rp 5.35 Miliar) berkat Preferensi HEA!
        Assert.Equal("PT Mitra Teknik Manufaktur Lokal", openedTender.HeaRankings[0].VendorName);
        Assert.Equal(1, openedTender.HeaRankings[0].FinalRank);

        // 3. Simulasi Live e-Reverse Auction pada 120 detik terakhir (<= 180 detik) -> Wajib memicu Anti-Sniping Auto-Extend (+5 menit / +1 extension)!
        var rank3Participant = openedTender.HeaRankings.First(r => r.VendorName == "PT Karya Infrastruktur Nusantara Tbk");
        var afterAuction = tenderService.SubmitEAuctionBid(openedTender.Id, new SubmitEAuctionBidRequestDto(
            ParticipantId: rank3Participant.ParticipantId,
            NewLoweredBidPriceHp: 5_400_000_000m,
            SecondsBeforeEndSimulated: 120));

        Assert.Equal(1, afterAuction.EAuctionSession!.CurrentExtensionCount);
        Assert.True(afterAuction.EAuctionSession.BidHistory.Single().TriggeredAntiSnipingAutoExtend);

        // 4. Finalisasi Negosiasi BAFO & Pengumuman Pemenang (BAHP)
        var topWinner = afterAuction.HeaRankings.First();
        var afterBafo = tenderService.FinalizeBafoAndAnnounceWinner(tender.Id, new FinalizeBafoAndWinnerRequestDto(
            WinningParticipantId: topWinner.ParticipantId,
            FinalBafoAgreedPrice: 5_250_000_000m,
            BahpNumber: "BAHP-UKPBJ-2026-088"));

        Assert.NotNull(afterBafo.WinnerDetermination);
        // Cost Saving terhadap HPS (Rp 6.00 Miliar - Rp 5.25 Miliar = Rp 750 Juta / 12.50%)
        Assert.Equal(750_000_000m, afterBafo.WinnerDetermination!.CostSavingToHpsAmount);
        Assert.Equal(12.50m, afterBafo.WinnerDetermination.CostSavingToHpsPct);
        Assert.Equal("OBJECTION_SANGGAH_PERIOD", afterBafo.CurrentStage);

        // 5. Pengajuan Sanggah oleh peserta yang kalah & Penolakan Sanggah oleh Pokja -> Status menjadi READY_FOR_SPPBJ
        var losingBidder = afterBafo.HeaRankings.Last();
        var withSanggah = tenderService.SubmitObjectionSanggah(tender.Id, new SubmitSanggahRequestDto(
            ParticipantId: losingBidder.ParticipantId,
            VendorName: losingBidder.VendorName,
            ObjectionLevel: "SANGGAH_AWAL",
            ObjectionCategory: "PREFERENSI_TKDN_CALCULATION",
            ObjectionLetterNo: "SRT-SGH-2026-019",
            ObjectionDetails: "Mempertanyakan mengapa harga nominal kami yang lebih rendah berada di peringkat bawah."));

        var sanggahId = withSanggah.ObjectionsSanggah.Single().Id;
        var resolvedTender = tenderService.ResolveObjectionSanggah(tender.Id, new ResolveSanggahRequestDto(
            ObjectionId: sanggahId,
            AcceptObjection: false,
            ResponseLetterNo: "BA-JWB-SGH-2026-019",
            ResponseDetails: "Sesuai Permen BUMN PER-2/MBU/03/2023, produk dengan TKDN >= 25% diberikan preferensi harga hingga 25% melalui rumus HEA. Sanggahan ditolak."));

        Assert.Equal("READY_FOR_SPPBJ", resolvedTender.CurrentStage);
    }

    [Fact]
    public void Phase4ECatalogAndClmService_ExecutesTieredCallOffOrderEnforcesPerformanceBondTteBeforeSpmkAndGuardsCcoCeiling()
    {
        var clmService = new ECatalogAndClmAppService();

        // 1. E-Katalog & Kontrak Payung: Pesan 250 Batang Pipa Seamless -> Otomatis masuk Tier 3 (>= 200 batang, diskon 10%, Rp 4.320.000/batang)
        var pipeItem = clmService.GetCatalogItems().First(c => c.KimapCode == "KIMAP-PIPE-8IN-CS");
        var callOffOrder = clmService.CreateCallOffOrder(new CreateCallOffOrderRequestDto(
            CatalogItemId: pipeItem.Id,
            OrderingBranchCode: "REG-KALTIM-04",
            OrderedQty: 250));

        Assert.Equal(4_320_000m, callOffOrder.AppliedTierUnitPrice);
        Assert.Equal(10.0m, callOffOrder.AppliedDiscountPct);
        Assert.Equal(1_080_000_000m, callOffOrder.TotalOrderAmount); // 250 * 4.320.000 = Rp 1.08 Miliar

        // Pastikan sisa plafon kuota Kontrak Payung berkurang dari Rp 20.50 Miliar menjadi Rp 19.42 Miliar
        var updatedUmbrella = clmService.GetUmbrellaAgreements().First(u => u.Id == pipeItem.UmbrellaAgreementId);
        Assert.Equal(19_420_000_000m, updatedUmbrella.RemainingQuotaAmount);

        // 2. Siklus e-CLM: Ambil Kontrak hasil Tender (Nilai Rp 5.45 Miliar, Kewajiban Jaminan Pelaksanaan 5% = Rp 272.500.000)
        var contract = clmService.GetContracts().First();
        Assert.Equal(272_500_000m, contract.RequiredPerformanceBondAmount);

        // 3. Guard SPMK: Percobaan menerbitkan SPMK sebelum Jaminan Pelaksanaan & TTE wajib DITOLAK!
        var spmkPrematureEx = Assert.Throws<InvalidOperationException>(() =>
            clmService.IssueSpmk(contract.Id, new IssueSpmkRequestDto(
                SpmkNumber: "SPMK/BUMN-ENG/2026/001",
                EffectiveStartDate: new DateOnly(2026, 4, 1),
                ExecutionDurationDays: 120)));
        Assert.Contains("Eproc:Clm:CannotIssueSpmkWithoutBondAndTte", spmkPrematureEx.Message);

        // 4. Guard Performance Bond: Bank Garansi di bawah 5% (misal Rp 200 Juta < Rp 272.5 Juta) wajib DITOLAK!
        var insufficientBondEx = Assert.Throws<InvalidOperationException>(() =>
            clmService.VerifyPerformanceBond(contract.Id, new VerifyPerformanceBondRequestDto(
                GuaranteeNumber: "BG-MANDIRI-2026-LOW",
                IssuingBankName: "PT Bank Mandiri (Persero) Tbk",
                BondAmount: 200_000_000m,
                ExpiryDate: new DateOnly(2026, 12, 31))));
        Assert.Contains("Eproc:Clm:InsufficientPerformanceBondAmount", insufficientBondEx.Message);

        // 5. Verifikasi Jaminan Pelaksanaan 5% yang sah (Rp 272.500.000), TTE + e-Meterai Peruri, dan Penerbitan SPMK
        clmService.SubmitContractRedline(contract.Id, new SubmitContractRedlineRequestDto(
            AuthorRole: "VENDOR_LEGAL",
            AuthorName: "Divisi Legal PT Nusantara Valve",
            ClauseReference: "Pasal 21 Force Majeure",
            ChangeSummaryNotes: "Penyesuaian definisi gangguan rantai pasok pelabuhan internasional disepakati kedua pihak."));

        var bondVerified = clmService.VerifyPerformanceBond(contract.Id, new VerifyPerformanceBondRequestDto(
            GuaranteeNumber: "BG-MANDIRI-2026-99881",
            IssuingBankName: "PT Bank Mandiri (Persero) Tbk",
            BondAmount: 272_500_000m,
            ExpiryDate: new DateOnly(2026, 12, 31)));
        Assert.Equal("PERFORMANCE_BOND_VERIFIED", bondVerified.CurrentLifecycleStatus);

        var tteSigned = clmService.ExecuteDigitalSignatureAndEmeterai(contract.Id, new SignContractTteRequestDto(
            EmeteraiSerialNumber: "EMTR-PERURI-2026-7741902",
            BumnSignerName: "Ir. Hendra Wijaya, M.T.",
            BumnSignerTitle: "Direktur Operasi & Teknik BUMN",
            VendorSignerName: "Budi Santoso, S.T.",
            VendorSignerTitle: "Direktur Utama PT Nusantara Valve"));
        Assert.Equal("SIGNED_TTE_EMETERAI", tteSigned.CurrentLifecycleStatus);
        Assert.NotNull(tteSigned.DigitalSignature);

        var spmkIssued = clmService.IssueSpmk(contract.Id, new IssueSpmkRequestDto(
            SpmkNumber: "SPMK/BUMN-ENG/2026/001",
            EffectiveStartDate: new DateOnly(2026, 4, 1),
            ExecutionDurationDays: 120));
        Assert.Equal("SPMK_ISSUED_ACTIVE", spmkIssued.CurrentLifecycleStatus);

        // 6. Guard Addendum / CCO: Kenaikan pekerjaan tambah > 10% (misal +Rp 700 Juta = +12.84%) tanpa otorisasi khusus Direksi wajib DITOLAK!
        var ccoExceedEx = Assert.Throws<InvalidOperationException>(() =>
            clmService.SubmitContractAddendumCco(contract.Id, new SubmitAddendumCcoRequestDto(
                AddendumNumber: "ADD-01/KTR/BUMN-ENG/2026",
                ChangeType: "VOLUME_ADDITION_CCO",
                JustificationReason: "Penambahan 3 unit valve cadangan melebihi plafon 10%",
                DeltaAmount: 700_000_000m,
                AdditionalDurationDays: 14,
                HasBoardExceptionOverride: false)));
        Assert.Contains("Eproc:Clm:CcoExceedsMaxTenPercentCeiling", ccoExceedEx.Message);

        // 7. Addendum / CCO yang patuh plafon <= 10% (+Rp 450 Juta = +8.26%) berhasil disahkan!
        var amendedContract = clmService.SubmitContractAddendumCco(contract.Id, new SubmitAddendumCcoRequestDto(
            AddendumNumber: "ADD-01/KTR/BUMN-ENG/2026",
            ChangeType: "VOLUME_ADDITION_CCO",
            JustificationReason: "Penambahan aksesoris flange & stud bolt lapangan (+8.26%)",
            DeltaAmount: 450_000_000m,
            AdditionalDurationDays: 14,
            HasBoardExceptionOverride: false));

        Assert.Equal("ADDENDUM_AMENDED", amendedContract.CurrentLifecycleStatus);
        Assert.Equal(5_900_000_000m, amendedContract.CurrentActiveContractAmount);
        Assert.Equal(8.26m, amendedContract.AddendumsCco.Single().CumulativeIncreasePct);
    }

    [Fact]
    public void Phase5ExecutionAndFinanceService_DetectsScurveScmCalculatesPostAuditTkdnSanctionExecutes3WayMatchWapuAndScoresVpe()
    {
        var execService = new ProjectExecutionAndFinanceAppService();

        // 1. Verifikasi Kurva S Konstruksi: Minggu ke-12 Rencana 75%, Realisasi 68% (Deviasi -7% pada periode >= 70% -> Kritis, naik dari ScmLevel1 ke ScmLevel2)
        var scurve = execService.EvaluateAndRecordSCurveProgress(new EvaluateSCurveProgressRequestDto(
            ContractId: Guid.NewGuid(),
            ContractNumber: "KTR/BUMN-KARYA/EPC-GARDU/2026/004",
            WeekNumber: 12,
            PlannedCumulativePct: 75.0m,
            ActualCumulativePct: 68.0m,
            CurrentActiveScmLevel: ScmLevel.ScmLevel1,
            ActionPlanNotes: "Uji coba SCM I belum menutup ketertinggalan; ditingkatkan ke Show Cause Meeting (SCM) Tingkat II di hadapan GM/VP."));

        Assert.True(scurve.IsCriticalContract);
        Assert.Equal(-7.0m, scurve.DeviationPct);
        Assert.Equal(ScmLevel.ScmLevel2, scurve.TriggeredScmLevel);

        // 2. Verifikasi BAST & Post-Audit TKDN yang sudah di-seed (Kontrak Rp 5.90 Miliar, BAST Bruto Rp 5.45 Miliar, Komitmen TKDN 60%, Realisasi 58% -> Shortfall 2% = Sanksi Rp 118 Juta)
        var bast = execService.GetBastDocuments().First();
        Assert.Equal(2.0m, bast.PostAuditTkdn.ShortfallPct);
        Assert.Equal(118_000_000m, bast.PostAuditTkdn.FinancialSanctionAmount); // 2% * Rp 5.900.000.000 = Rp 118.000.000

        // 3. Guard Faktur Pajak WAPU BUMN: Kode faktur selain "030." (misal "010.") wajib DITOLAK!
        var invalidFakturEx = Assert.Throws<InvalidOperationException>(() =>
            execService.SubmitAndMatchVendorEInvoice(new SubmitVendorEInvoiceRequestDto(
                BastId: bast.Id,
                InvoiceNumber: "INV-NVPM-2026-0091",
                TaxInvoiceFakturNo: "010.004-26.12345678",
                VendorInvoiceClaimedDpp: 5_450_000_000m,
                DownPaymentRecoupmentPct: 10.0m,
                RetentionDeductionPct: 5.0m,
                IsClientWapuPpn: true,
                PpnRatePct: 11.0m,
                PphType: PphTaxType.Pph22,
                PphRatePct: 1.5m)));
        Assert.Contains("Eproc:Finance:InvalidWapuFakturCode", invalidFakturEx.Message);

        // 4. Eksekusi 3-Way Matching e-Invoice Sah dengan Faktur WAPU "030."
        //    - DPP Gross = Rp 5.450.000.000
        //    - Potongan DP (10%) = Rp 545.000.000
        //    - Retensi Pemeliharaan (5%) = Rp 272.500.000
        //    - DPP Setelah DP & Retensi = Rp 4.632.500.000
        //    - Denda Keterlambatan 8 hari (8 permil * Rp 5.450.000.000) = Rp 43.600.000
        //    - Sanksi Kekurangan TKDN (2% * Rp 5.900.000.000) = Rp 118.000.000
        //    - Total Penalti = Rp 161.600.000
        //    - DPP Net Setelah Penalti = Rp 4.470.900.000
        //    - PPN 11% (Rp 509.575.000) -> Dipungut langsung oleh WAPU BUMN (tidak ditransfer ke vendor)
        //    - PPh 22 (1.5% * Rp 4.632.500.000) = Rp 69.487.500
        //    - Net Payable (SP2D ke Rekening Vendor) = Rp 4.470.900.000 - Rp 69.487.500 = Rp 4.401.412.500
        var settlement = execService.SubmitAndMatchVendorEInvoice(new SubmitVendorEInvoiceRequestDto(
            BastId: bast.Id,
            InvoiceNumber: "INV-NVPM-2026-0091",
            TaxInvoiceFakturNo: "030.004-26.88991122",
            VendorInvoiceClaimedDpp: 5_450_000_000m,
            DownPaymentRecoupmentPct: 10.0m,
            RetentionDeductionPct: 5.0m,
            IsClientWapuPpn: true,
            PpnRatePct: 11.0m,
            PphType: PphTaxType.Pph22,
            PphRatePct: 1.5m));

        Assert.True(settlement.IsThreeWayMatched);
        Assert.Equal("THREE_WAY_MATCHED_BAP_ISSUED", settlement.PaymentStatus);
        Assert.Equal(43_600_000m, settlement.CappedLatePenaltyAmount);
        Assert.Equal(118_000_000m, settlement.TkdnShortfallPenaltyAmount);
        Assert.Equal(161_600_000m, settlement.TotalPenaltyDeduction);
        Assert.Equal(509_575_000m, settlement.PpnAmount);
        Assert.True(settlement.IsPpnWithheldByWapu);
        Assert.Equal(69_487_500m, settlement.PphWithheldAmount);
        Assert.Equal(4_401_412_500m, settlement.NetPayableToVendor);

        // 5. Evaluasi Rapor Kinerja Penyedia (VPE 5 Dimensi)
        var vpe = execService.EvaluateVendorPerformanceVpe(new EvaluateVendorVpeRequestDto(
            ContractId: bast.ContractId,
            ContractNumber: bast.ContractNumber,
            VendorId: Guid.NewGuid(),
            VendorName: bast.VendorName,
            QualityScore: 92.0m,            // 30% -> 27.60
            DeliveryTimelinessScore: 80.0m, // 25% -> 20.00
            HseCsmsComplianceScore: 90.0m,  // 20% -> 18.00
            TkdnComplianceScore: 84.0m,     // 15% -> 12.60
            ResponsivenessScore: 88.0m));   // 10% ->  8.80 => Total = 87.00 (GOLD)

        Assert.Equal(87.00m, vpe.FinalWeightedVpeScore);
        Assert.Equal("GOLD", vpe.VpeGrade);
        Assert.Equal("MAINTAIN_AVL_PRIORITY", vpe.AvlActionRecommendation);
    }
}
