using System.Security.Cryptography;
using System.Text;
using Eproc.Configuration;
using Eproc.Enums;
using Eproc.Services;

namespace Eproc.CatalogAndClm;

/// <summary>
/// Application Service Fase 4:
/// 1. E-Katalog Internal BUMN, Kontrak Payung (Umbrella Agreement), Tiered Volume Pricing & Call-Off Order.
/// 2. Electronic Contract Lifecycle Management (e-CLM):
///    - Penerbitan SPPBJ & Negosiasi Klausul Kontrak (Contract Redlining)
///    - Verifikasi Jaminan Pelaksanaan (Performance Bond 5% Nilai Kontrak atau 5% HPS bila penawaran < 80% HPS)
///    - Pembubuhan e-Meterai Peruri & Tanda Tangan Elektronik (TTE BSrE/PSrE)
///    - Guard Penerbitan SPMK (Wajib Jaminan Pelaksanaan Verified + Kontrak TTE)
///    - Addendum / Contract Change Order (CCO) dengan Guard Plafon Kumulatif Maksimal 10%
/// </summary>
public sealed class ECatalogAndClmAppService
{
    private readonly List<UmbrellaAgreementDto> _umbrellaAgreements = [];
    private readonly List<CatalogItemDto> _catalogItems = [];
    private readonly List<CallOffOrderDto> _callOffOrders = [];
    private readonly List<ProcurementContractDto> _contracts = [];

    public ECatalogAndClmAppService()
    {
        SeedInitialCatalogAndContracts();
    }

    // ========================================================================
    // 1. E-KATALOG INTERNAL, KONTRAK PAYUNG & CALL-OFF ORDER
    // ========================================================================

    public IReadOnlyList<UmbrellaAgreementDto> GetUmbrellaAgreements() => _umbrellaAgreements;

    public IReadOnlyList<CatalogItemDto> GetCatalogItems() => _catalogItems;

    public IReadOnlyList<CallOffOrderDto> GetCallOffOrders() => _callOffOrders;

    /// <summary>
    /// Membuat pesanan Call-Off Order (E-Purchasing) terhadap Kontrak Payung aktif:
    /// - Menentukan harga satuan otomatis sesuai Tiered Volume Pricing.
    /// - Memvalidasi sisa plafon kuota Kontrak Payung (RemainingQuotaAmount).
    /// - Memotong kuota Kontrak Payung secara otomatis.
    /// </summary>
    public CallOffOrderDto CreateCallOffOrder(CreateCallOffOrderRequestDto input)
    {
        if (input.OrderedQty <= 0)
            throw new ArgumentOutOfRangeException(nameof(input.OrderedQty), "Kuantitas pesanan Call-Off harus lebih dari 0.");

        var item = _catalogItems.FirstOrDefault(c => c.Id == input.CatalogItemId)
            ?? throw new KeyNotFoundException("Item E-Katalog tidak ditemukan.");

        int uaIdx = _umbrellaAgreements.FindIndex(u => u.Id == item.UmbrellaAgreementId);
        if (uaIdx < 0)
            throw new KeyNotFoundException("Kontrak Payung (Umbrella Agreement) tidak ditemukan.");

        var umbrella = _umbrellaAgreements[uaIdx];
        if (umbrella.Status != "ACTIVE")
            throw new InvalidOperationException($"Eproc:Catalog:InactiveUmbrella - Kontrak Payung {umbrella.AgreementNumber} berstatus {umbrella.Status}.");

        // Pilih tier harga berdasarkan kuantitas pesanan
        var matchedTier = item.TierPrices
            .Where(t => input.OrderedQty >= t.MinQty && (!t.MaxQty.HasValue || input.OrderedQty <= t.MaxQty.Value))
            .OrderByDescending(t => t.MinQty)
            .FirstOrDefault()
            ?? item.TierPrices.OrderByDescending(t => t.MinQty).First();

        decimal totalOrderAmount = Math.Round(input.OrderedQty * matchedTier.UnitPrice, 2, MidpointRounding.AwayFromZero);

        if (totalOrderAmount > umbrella.RemainingQuotaAmount)
        {
            throw new InvalidOperationException(
                $"Eproc:Catalog:QuotaExhausted - Nilai pesanan Call-Off (Rp {totalOrderAmount:N2}) melebihi sisa plafon kuota Kontrak Payung {umbrella.AgreementNumber} (Sisa: Rp {umbrella.RemainingQuotaAmount:N2}).");
        }

        decimal newConsumed = umbrella.ConsumedQuotaAmount + totalOrderAmount;
        decimal newRemaining = umbrella.TotalQuotaCeiling - newConsumed;

        _umbrellaAgreements[uaIdx] = umbrella with
        {
            ConsumedQuotaAmount = newConsumed,
            RemainingQuotaAmount = newRemaining,
            Status = newRemaining == 0m ? "QUOTA_EXHAUSTED" : "ACTIVE"
        };

        var order = new CallOffOrderDto(
            Id: Guid.NewGuid(),
            CallOffPoNumber: $"PO-CAT/{input.OrderingBranchCode}/2026/{_callOffOrders.Count + 1:D4}",
            UmbrellaAgreementId: umbrella.Id,
            CatalogItemId: item.Id,
            KimapCode: item.KimapCode,
            ItemName: item.ItemName,
            VendorName: item.VendorName,
            OrderingBranchCode: input.OrderingBranchCode,
            OrderedQty: input.OrderedQty,
            Uom: item.Uom,
            AppliedTierUnitPrice: matchedTier.UnitPrice,
            AppliedDiscountPct: matchedTier.DiscountPct,
            TotalOrderAmount: totalOrderAmount,
            Status: "ISSUED_AND_QUOTA_DEDUCTED",
            OrderedAtUtc: DateTimeOffset.UtcNow
        );

        _callOffOrders.Add(order);
        return order;
    }

    // ========================================================================
    // 2. MANAJEMEN KONTRAK SIKLUS PENUH (e-CLM)
    // ========================================================================

    public IReadOnlyList<ProcurementContractDto> GetContracts() => _contracts;

    public ProcurementContractDto GetContractById(Guid id) =>
        _contracts.FirstOrDefault(c => c.Id == id)
        ?? throw new KeyNotFoundException($"Kontrak dengan ID {id} tidak ditemukan.");

    /// <summary>
    /// Menambahkan versi revisi klausul kontrak (Contract Redlining) antara Biro Hukum BUMN & Legal Vendor.
    /// </summary>
    public ProcurementContractDto SubmitContractRedline(Guid contractId, SubmitContractRedlineRequestDto input)
    {
        int idx = _contracts.FindIndex(c => c.Id == contractId);
        if (idx < 0)
            throw new KeyNotFoundException("Kontrak tidak ditemukan.");

        var contract = _contracts[idx];
        int nextVer = contract.RedlineHistory.Count + 1;

        var redline = new ContractRedlineVersionDto(
            VersionNumber: nextVer,
            VersionLabel: $"v1.{nextVer - 1} ({input.AuthorRole})",
            AuthorRole: input.AuthorRole,
            AuthorName: input.AuthorName,
            ClauseReference: input.ClauseReference,
            ChangeSummaryNotes: input.ChangeSummaryNotes,
            CreatedAtUtc: DateTimeOffset.UtcNow
        );

        var updatedHistory = new List<ContractRedlineVersionDto>(contract.RedlineHistory) { redline };
        var updated = contract with
        {
            CurrentLifecycleStatus = contract.CurrentLifecycleStatus == "SPPBJ_ISSUED"
                ? "REDLINING_NEGOTIATION"
                : contract.CurrentLifecycleStatus,
            RedlineHistory = updatedHistory
        };

        _contracts[idx] = updated;
        return updated;
    }

    /// <summary>
    /// Memverifikasi Jaminan Pelaksanaan (Performance Bond Bank Garansi):
    /// - Jika Nilai Kontrak >= 80% HPS -> Minimal 5% dari Nilai Kontrak.
    /// - Jika Nilai Kontrak < 80% HPS -> Minimal 5% dari Total HPS!
    /// </summary>
    public ProcurementContractDto VerifyPerformanceBond(Guid contractId, VerifyPerformanceBondRequestDto input)
    {
        int idx = _contracts.FindIndex(c => c.Id == contractId);
        if (idx < 0)
            throw new KeyNotFoundException("Kontrak tidak ditemukan.");

        var contract = _contracts[idx];
        if (input.BondAmount < contract.RequiredPerformanceBondAmount)
        {
            throw new InvalidOperationException(
                $"Eproc:Clm:InsufficientPerformanceBondAmount - Nominal Bank Garansi (Rp {input.BondAmount:N2}) kurang dari kewajiban Jaminan Pelaksanaan minimum (Rp {contract.RequiredPerformanceBondAmount:N2}).");
        }

        decimal bondPct = Math.Round((input.BondAmount / contract.InitialContractAmount) * 100m, 2, MidpointRounding.AwayFromZero);

        var bond = new PerformanceBondGuaranteeDto(
            GuaranteeNumber: input.GuaranteeNumber,
            IssuingBankName: input.IssuingBankName,
            BondAmount: input.BondAmount,
            RequiredMinimumBondAmount: contract.RequiredPerformanceBondAmount,
            BondToContractPct: bondPct,
            IssueDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ExpiryDate: input.ExpiryDate,
            VerificationStatus: "VERIFIED_ACTIVE",
            VerifiedAtUtc: DateTimeOffset.UtcNow
        );

        var updated = contract with
        {
            PerformanceBond = bond,
            CurrentLifecycleStatus = "PERFORMANCE_BOND_VERIFIED"
        };

        _contracts[idx] = updated;
        return updated;
    }

    /// <summary>
    /// Membubuhkan e-Meterai Peruri & Tanda Tangan Elektronik (TTE BSrE/PSrE) pada dokumen kontrak.
    /// Wajib telah melewati verifikasi Jaminan Pelaksanaan (Performance Bond).
    /// </summary>
    public ProcurementContractDto ExecuteDigitalSignatureAndEmeterai(Guid contractId, SignContractTteRequestDto input)
    {
        int idx = _contracts.FindIndex(c => c.Id == contractId);
        if (idx < 0)
            throw new KeyNotFoundException("Kontrak tidak ditemukan.");

        var contract = _contracts[idx];
        if (contract.PerformanceBond is null || contract.PerformanceBond.VerificationStatus != "VERIFIED_ACTIVE")
        {
            throw new InvalidOperationException(
                "Eproc:Clm:PerformanceBondRequiredBeforeSigning - Kontrak tidak dapat ditandatangani secara elektronik (TTE) sebelum Jaminan Pelaksanaan 5% diverifikasi aktif.");
        }

        string rawDocDescriptor = $"{contract.ContractNumber}|{contract.InitialContractAmount:F2}|{input.EmeteraiSerialNumber}|{input.BumnSignerName}|{input.VendorSignerName}|{DateTimeOffset.UtcNow:O}";
        string pdfHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawDocDescriptor))).ToLowerInvariant();

        var sig = new ContractDigitalSignatureDto(
            EmeteraiSerialNumber: input.EmeteraiSerialNumber,
            BumnSignerName: input.BumnSignerName,
            BumnSignerTitle: input.BumnSignerTitle,
            BumnTteCertificateSerial: $"BSRE-BUMN-2026-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
            VendorSignerName: input.VendorSignerName,
            VendorSignerTitle: input.VendorSignerTitle,
            VendorTteCertificateSerial: $"PSRE-PERURI-2026-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
            SignedPdfSha256Hash: pdfHash,
            SignedAtUtc: DateTimeOffset.UtcNow
        );

        var updated = contract with
        {
            DigitalSignature = sig,
            CurrentLifecycleStatus = "SIGNED_TTE_EMETERAI"
        };

        _contracts[idx] = updated;
        return updated;
    }

    /// <summary>
    /// Menerbitkan Surat Perintah Mulai Kerja (SPMK):
    /// DILARANG menerbitkan SPMK apabila Jaminan Pelaksanaan belum VERIFIED_ACTIVE atau Kontrak belum SIGNED_TTE_EMETERAI!
    /// </summary>
    public ProcurementContractDto IssueSpmk(Guid contractId, IssueSpmkRequestDto input)
    {
        int idx = _contracts.FindIndex(c => c.Id == contractId);
        if (idx < 0)
            throw new KeyNotFoundException("Kontrak tidak ditemukan.");

        var contract = _contracts[idx];
        if (contract.PerformanceBond?.VerificationStatus != "VERIFIED_ACTIVE" || contract.DigitalSignature is null)
        {
            throw new InvalidOperationException(
                "Eproc:Clm:CannotIssueSpmkWithoutBondAndTte - SPMK dilarang diterbitkan sebelum Jaminan Pelaksanaan diverifikasi aktif DAN Kontrak telah dibubuhi e-Meterai serta TTE BSrE/PSrE.");
        }

        var updated = contract with
        {
            SpmkNumber = input.SpmkNumber,
            SpmkEffectiveStartDate = input.EffectiveStartDate,
            TargetCompletionDate = input.EffectiveStartDate.AddDays(input.ExecutionDurationDays),
            CurrentLifecycleStatus = "SPMK_ISSUED_ACTIVE"
        };

        _contracts[idx] = updated;
        return updated;
    }

    /// <summary>
    /// Mengajukan Addendum / Contract Change Order (CCO - Tambah/Kurang Pekerjaan):
    /// - Memvalidasi batas plafon kenaikan kumulatif maksimal 10% dari Nilai Kontrak Awal sesuai regulasi pengadaan BUMN,
    ///   kecuali disertai otorisasi pengecualian Direksi Kolektif (HasBoardExceptionOverride = true).
    /// </summary>
    public ProcurementContractDto SubmitContractAddendumCco(Guid contractId, SubmitAddendumCcoRequestDto input)
    {
        int idx = _contracts.FindIndex(c => c.Id == contractId);
        if (idx < 0)
            throw new KeyNotFoundException("Kontrak tidak ditemukan.");

        var contract = _contracts[idx];
        if (contract.SpmkNumber is null)
        {
            throw new InvalidOperationException("Eproc:Clm:ContractNotActiveForCco - Addendum/CCO hanya dapat diajukan pada kontrak yang telah memiliki SPMK aktif.");
        }

        decimal newContractAmount = contract.CurrentActiveContractAmount + input.DeltaAmount;
        decimal cumulativeDelta = newContractAmount - contract.InitialContractAmount;
        decimal cumulativeIncreasePct = Math.Round((cumulativeDelta / contract.InitialContractAmount) * 100m, 2, MidpointRounding.AwayFromZero);

        if (cumulativeIncreasePct > 10.0m && !input.HasBoardExceptionOverride)
        {
            throw new InvalidOperationException(
                $"Eproc:Clm:CcoExceedsMaxTenPercentCeiling - Kenaikan kumulatif Addendum/CCO sebesar {cumulativeIncreasePct:N2}% ( Nilai Baru: Rp {newContractAmount:N2} ) melampaui batas plafon 10% dari Nilai Kontrak Awal (Rp {contract.InitialContractAmount:N2}) tanpa otorisasi khusus Direksi.");
        }

        var cco = new ContractChangeOrderCcoDto(
            Id: Guid.NewGuid(),
            AddendumSequence: contract.AddendumsCco.Count + 1,
            AddendumNumber: input.AddendumNumber,
            ChangeType: input.ChangeType,
            JustificationReason: input.JustificationReason,
            PreviousContractAmount: contract.CurrentActiveContractAmount,
            DeltaAmount: input.DeltaAmount,
            NewContractAmount: newContractAmount,
            CumulativeIncreasePct: cumulativeIncreasePct,
            AdditionalDurationDays: input.AdditionalDurationDays,
            ApprovedByBoardOverride: input.HasBoardExceptionOverride,
            ApprovedAtUtc: DateTimeOffset.UtcNow
        );

        var updatedAddendums = new List<ContractChangeOrderCcoDto>(contract.AddendumsCco) { cco };
        var newTargetDate = contract.TargetCompletionDate?.AddDays(input.AdditionalDurationDays);

        var updated = contract with
        {
            CurrentActiveContractAmount = newContractAmount,
            TargetCompletionDate = newTargetDate,
            CurrentLifecycleStatus = "ADDENDUM_AMENDED",
            AddendumsCco = updatedAddendums
        };

        _contracts[idx] = updated;
        return updated;
    }

    private void SeedInitialCatalogAndContracts()
    {
        // 1. Seed 2 Kontrak Payung (Umbrella Agreement)
        var uaPipeId = Guid.NewGuid();
        var uaItId = Guid.NewGuid();

        _umbrellaAgreements.AddRange([
            new UmbrellaAgreementDto(
                Id: uaPipeId,
                AgreementNumber: "KP-BUMN/ENG-PIPE/2026/001",
                Title: "Kontrak Payung Penyediaan Pipa Baja Seamless & Valve API 5L/6D Regional Nasional",
                VendorId: Guid.NewGuid(),
                VendorName: "PT Nusantara Valve & Piping Manufacturing",
                ValidFrom: new DateOnly(2026, 1, 1),
                ValidUntil: new DateOnly(2027, 12, 31),
                TotalQuotaCeiling: 25_000_000_000m,
                ConsumedQuotaAmount: 4_500_000_000m,
                RemainingQuotaAmount: 20_500_000_000m,
                Status: "ACTIVE"
            ),
            new UmbrellaAgreementDto(
                Id: uaItId,
                AgreementNumber: "KP-BUMN/IT-WORKSTATION/2026/002",
                Title: "Kontrak Payung Pengadaan Perangkat Laptop & Workstation Enterprise TKDN",
                VendorId: Guid.NewGuid(),
                VendorName: "PT Teknologi Komputasi Merah Putih",
                ValidFrom: new DateOnly(2026, 1, 1),
                ValidUntil: new DateOnly(2026, 12, 31),
                TotalQuotaCeiling: 8_000_000_000m,
                ConsumedQuotaAmount: 1_200_000_000m,
                RemainingQuotaAmount: 6_800_000_000m,
                Status: "ACTIVE"
            )
        ]);

        // 2. Seed Item E-Katalog dengan Tiered Volume Pricing
        var itemPipeId = Guid.NewGuid();
        var itemLaptopId = Guid.NewGuid();

        _catalogItems.AddRange([
            new CatalogItemDto(
                Id: itemPipeId,
                UmbrellaAgreementId: uaPipeId,
                AgreementNumber: "KP-BUMN/ENG-PIPE/2026/001",
                KimapCode: "KIMAP-PIPE-8IN-CS",
                ItemName: "Carbon Steel Seamless Line Pipe 8 Inch Sch 40 API 5L Gr.B (6 Meter/Batang)",
                Category: ProcurementCategory.Goods,
                VendorName: "PT Nusantara Valve & Piping Manufacturing",
                Uom: "BATANG",
                VerifiedTkdnPct: 62.50m,
                TkdnCertificateNo: "TKDN-KEMENPERIN-2026-8812",
                LeadTimeDays: 14,
                TierPrices:
                [
                    new CatalogItemTierPriceDto(MinQty: 1, MaxQty: 49, UnitPrice: 4_800_000m, DiscountPct: 0m),
                    new CatalogItemTierPriceDto(MinQty: 50, MaxQty: 199, UnitPrice: 4_560_000m, DiscountPct: 5.0m),
                    new CatalogItemTierPriceDto(MinQty: 200, MaxQty: null, UnitPrice: 4_320_000m, DiscountPct: 10.0m)
                ]
            ),
            new CatalogItemDto(
                Id: itemLaptopId,
                UmbrellaAgreementId: uaItId,
                AgreementNumber: "KP-BUMN/IT-WORKSTATION/2026/002",
                KimapCode: "KIMAP-IT-LPT-PRO14",
                ItemName: "Laptop Enterprise Core Ultra 7 / 32GB RAM / 1TB NVMe (Sertifikat TKDN + BMP)",
                Category: ProcurementCategory.Goods,
                VendorName: "PT Teknologi Komputasi Merah Putih",
                Uom: "UNIT",
                VerifiedTkdnPct: 41.20m,
                TkdnCertificateNo: "TKDN-KEMENPERIN-2026-4409",
                LeadTimeDays: 7,
                TierPrices:
                [
                    new CatalogItemTierPriceDto(MinQty: 1, MaxQty: 24, UnitPrice: 18_500_000m, DiscountPct: 0m),
                    new CatalogItemTierPriceDto(MinQty: 25, MaxQty: 99, UnitPrice: 17_575_000m, DiscountPct: 5.0m),
                    new CatalogItemTierPriceDto(MinQty: 100, MaxQty: null, UnitPrice: 16_650_000m, DiscountPct: 10.0m)
                ]
            )
        ]);

        // 3. Seed 1 Kontrak Pengadaan Hasil Tender Fase 3 (Siap Redlining, Verifikasi Jaminan 5%, TTE & SPMK)
        decimal hps = 6_000_000_000m;
        decimal initialContractVal = 5_450_000_000m; // Hasil BAFO Fase 3
        bool isBelow80Pct = (initialContractVal / hps) < 0.80m;
        decimal requiredPerfBond = isBelow80Pct
            ? Math.Round(hps * 0.05m, 2, MidpointRounding.AwayFromZero)
            : Math.Round(initialContractVal * 0.05m, 2, MidpointRounding.AwayFromZero);

        _contracts.Add(new ProcurementContractDto(
            Id: Guid.NewGuid(),
            SourceTenderId: Guid.NewGuid(),
            SppbjNumber: "SPPBJ/BUMN-ENG/2026/001",
            SppbjDate: new DateOnly(2026, 3, 20),
            ContractNumber: "KTR/BUMN-ENG/EPC-VALVE/2026/001",
            ContractTitle: "Kontrak Pengadaan Terkonsolidasi High-Pressure Gate Valve 12 Inch Class 600 API 6D (17 Unit)",
            VendorId: Guid.NewGuid(),
            VendorName: "PT Nusantara Valve & Piping Manufacturing",
            Category: ProcurementCategory.Goods,
            ReferenceHpsAmount: hps,
            InitialContractAmount: initialContractVal,
            CurrentActiveContractAmount: initialContractVal,
            CommittedTkdnPct: 60.0m,
            IsBelow80PercentHps: isBelow80Pct,
            RequiredPerformanceBondAmount: requiredPerfBond, // 5% dari Rp 5.45 Miliar = Rp 272.500.000
            CurrentLifecycleStatus: "REDLINING_NEGOTIATION",
            SpmkNumber: null,
            SpmkEffectiveStartDate: null,
            TargetCompletionDate: null,
            RedlineHistory:
            [
                new ContractRedlineVersionDto(
                    VersionNumber: 1,
                    VersionLabel: "v1.0 (LEGAL_BUMN)",
                    AuthorRole: "LEGAL_BUMN",
                    AuthorName: "Biro Hukum & Kepatuhan BUMN",
                    ClauseReference: "Pasal 12 & Pasal 18 (Jaminan TKDN 60% & Denda Keterlambatan 1 Permil/Hari)",
                    ChangeSummaryNotes: "Draft awal kontrak standar BUMN memasukkan klausul denda keterlambatan 1‰ per hari (maks 5%) dan sanksi selisih preferensi TKDN bila post-audit < 60%.",
                    CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-2)
                )
            ],
            PerformanceBond: null,
            DigitalSignature: null,
            AddendumsCco: []
        ));
    }
}
