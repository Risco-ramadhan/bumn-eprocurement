using System.Security.Cryptography;
using System.Text.Json;
using Eproc.Enums;
using Eproc.Services;
using Eproc.Vms;
using Eproc.Workflows;

namespace Eproc.Planning;

/// <summary>
/// Application Service untuk Fase 2:
/// - Manajemen Pagu Anggaran RKAP (Capex/Opex) & Real-Time Budget Commitment Lock
/// - Pembuatan Purchase Requisition (PR) & Konsolidasi PR Lintas Cabang/Divisi
/// - Penyusunan BoQ / Analisa Harga Satuan Pekerjaan (AHSP)
/// - Penyegelan HPS / Owner Estimate dengan Hybrid Envelope Encryption (AES-256-GCM + RSA)
/// - Eksekusi Workflow Persetujuan Berjenjang (DOA Inbox)
/// </summary>
public sealed class PlanningAndDoaAppService
{
    private readonly EnvelopeCryptoService _cryptoService;
    private readonly DoaMatrixApprovalEngine _doaEngine;
    private readonly RSA _committeeRsaKey;
    private readonly List<RkapBudgetDto> _rkapBudgets = new();
    private readonly List<PurchaseRequisitionDto> _purchaseRequisitions = new();
    private readonly List<ProcurementPackageDto> _packages = new();
    private readonly List<DoaApprovalInstanceDto> _doaInstances = new();
    private readonly Dictionary<Guid, SealedEnvelopePackage> _sealedHpsVault = new();

    public PlanningAndDoaAppService(
        EnvelopeCryptoService cryptoService,
        DoaMatrixApprovalEngine doaEngine)
    {
        _cryptoService = cryptoService;
        _doaEngine = doaEngine;
        _committeeRsaKey = RSA.Create(2048);
        SeedInitialPlanningData();
    }

    public IReadOnlyList<RkapBudgetDto> GetRkapBudgets() => _rkapBudgets.ToList();

    public IReadOnlyList<PurchaseRequisitionDto> GetPurchaseRequisitions() => _purchaseRequisitions.ToList();

    public IReadOnlyList<ProcurementPackageDto> GetPackages() => _packages.ToList();

    public IReadOnlyList<DoaApprovalInstanceDto> GetDoaInbox() => _doaInstances.ToList();

    /// <summary>
    /// Membuat Purchase Requisition (PR) baru dan otomatis memvalidasi serta mengunci pagu RKAP (CommittedAmount).
    /// </summary>
    public PurchaseRequisitionDto CreatePurchaseRequisition(CreatePurchaseRequisitionRequestDto input)
    {
        int budgetIdx = _rkapBudgets.FindIndex(b => b.Id == input.RkapBudgetId);
        if (budgetIdx < 0)
            throw new KeyNotFoundException("Pagu anggaran RKAP tidak ditemukan.");

        var budget = _rkapBudgets[budgetIdx];
        decimal totalPrValue = input.Items.Sum(i => i.Quantity * i.EstimatedUnitPrice);

        if (totalPrValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(input), "Total nilai PR harus lebih besar dari 0.");

        if (totalPrValue > budget.AvailableAmount)
        {
            throw new InvalidOperationException(
                $"Eproc:Budget:InsufficientRkapBalance - Nilai PR (Rp {totalPrValue:N2}) melebihi sisa pagu RKAP '{budget.WbsElementGlAccount}' yang tersedia (Rp {budget.AvailableAmount:N2}).");
        }

        // Kunci pagu anggaran (CommittedAmount bertambah, AvailableAmount berkurang)
        decimal newCommitted = budget.CommittedAmount + totalPrValue;
        decimal newAvailable = budget.RevisedAmount - newCommitted - budget.ActualSpentAmount;
        _rkapBudgets[budgetIdx] = budget with
        {
            CommittedAmount = newCommitted,
            AvailableAmount = newAvailable
        };

        var prId = Guid.NewGuid();
        string prNumber = $"PR-{DateTime.UtcNow:yyyy}-{_purchaseRequisitions.Count + 201:D4}";

        var normalizedItems = input.Items.Select((item, idx) => item with
        {
            Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id,
            LineNumber = idx + 1,
            TotalEstimatedPrice = Math.Round(item.Quantity * item.EstimatedUnitPrice, 2, MidpointRounding.AwayFromZero)
        }).ToList();

        var createdPr = new PurchaseRequisitionDto(
            Id: prId,
            PrNumber: prNumber,
            PrTitle: input.PrTitle,
            RkapBudgetId: budget.Id,
            WbsElementGlAccount: budget.WbsElementGlAccount,
            RequesterUnitName: input.RequesterUnitName,
            Category: input.Category,
            TotalEstimatedValue: totalPrValue,
            RequiredDeliveryDate: input.RequiredDeliveryDate,
            RequiredCsmsLevel: input.RequiredCsmsLevel,
            Status: "APPROVED_READY_FOR_SOURCING",
            ConsolidatedPackageId: null,
            Items: normalizedItems
        );

        _purchaseRequisitions.Insert(0, createdPr);
        return createdPr;
    }

    /// <summary>
    /// Menggabungkan (Konsolidasi) satu atau beberapa PR dari berbagai divisi/cabang menjadi 1 Paket Pengadaan
    /// untuk mendapatkan efisiensi harga volume (Consolidated Sourcing).
    /// </summary>
    public ProcurementPackageDto ConsolidatePrsToPackage(ConsolidatePrToPackageRequestDto input)
    {
        if (input.PurchaseRequisitionIds is null || input.PurchaseRequisitionIds.Count == 0)
            throw new ArgumentException("Pilih minimal 1 Purchase Requisition untuk dikonsolidasikan.");

        var selectedPrs = _purchaseRequisitions
            .Where(p => input.PurchaseRequisitionIds.Contains(p.Id))
            .ToList();

        if (selectedPrs.Count != input.PurchaseRequisitionIds.Count)
            throw new KeyNotFoundException("Beberapa Purchase Requisition tidak ditemukan.");

        if (selectedPrs.Any(p => p.Status == "CONSOLIDATED_TO_PACKAGE"))
            throw new InvalidOperationException("Eproc:Consolidation:AlreadyConsolidated - Salah satu PR sudah pernah dikonsolidasikan ke paket lain.");

        var packageId = Guid.NewGuid();
        string packageCode = $"PKG-{DateTime.UtcNow:yyyy}-{_packages.Count + 301:D4}";
        decimal totalPagu = selectedPrs.Sum(p => p.TotalEstimatedValue);
        var highestCsms = selectedPrs.Max(p => p.RequiredCsmsLevel);
        var primaryCategory = selectedPrs[0].Category;

        // Konsolidasikan item dengan MaterialServiceCode yang sama menjadi baris BoQ gabungan
        var groupedBoq = selectedPrs
            .SelectMany(p => p.Items)
            .GroupBy(i => i.MaterialServiceCode, StringComparer.OrdinalIgnoreCase)
            .Select((grp, idx) =>
            {
                var first = grp.First();
                decimal totalQty = grp.Sum(x => x.Quantity);
                decimal avgUnitEst = first.EstimatedUnitPrice;

                var ahsp = new List<AhspComponentDto>
                {
                    new("BAHAN_MATERIAL", $"Material Utama {first.ItemDescription}", 1.00m, first.Uom, Math.Round(avgUnitEst * 0.75m, 2)),
                    new("TENAGA_KERJA", "Tenaga Instalasi & Komisioning", 1.00m, "OH", Math.Round(avgUnitEst * 0.15m, 2)),
                    new("PERALATAN", "Alat Bantu & Uji Fungsi", 1.00m, "LOT", Math.Round(avgUnitEst * 0.10m, 2))
                };

                return new PackageBoqItemDto(
                    Id: Guid.NewGuid(),
                    WbsCode: $"1.{idx + 1}",
                    ItemName: $"{first.ItemDescription} (Konsolidasi {grp.Count()} Unit/Cabang)",
                    Quantity: totalQty,
                    Uom: first.Uom,
                    AhspBreakdowns: ahsp
                );
            })
            .ToList();

        // Tandai PR asal sebagai CONSOLIDATED_TO_PACKAGE
        for (int i = 0; i < _purchaseRequisitions.Count; i++)
        {
            if (input.PurchaseRequisitionIds.Contains(_purchaseRequisitions[i].Id))
            {
                _purchaseRequisitions[i] = _purchaseRequisitions[i] with
                {
                    Status = "CONSOLIDATED_TO_PACKAGE",
                    ConsolidatedPackageId = packageId
                };
            }
        }

        var package = new ProcurementPackageDto(
            Id: packageId,
            PackageCode: packageCode,
            PackageName: input.PackageName,
            Category: primaryCategory,
            SourcingMethod: input.SourcingMethod,
            EnvelopeMethod: input.EnvelopeMethod,
            EvaluationMethod: input.EvaluationMethod,
            ContractType: input.ContractType,
            TotalPaguBudget: totalPagu,
            RequiredCsmsLevel: highestCsms,
            MinTkdnRequirementPct: input.MinTkdnRequirementPct,
            EnableEReverseAuction: input.EnableEReverseAuction,
            Status: "DRAFT_CONSOLIDATED",
            SourcePrNumbers: selectedPrs.Select(p => p.PrNumber).ToList(),
            BoqItems: groupedBoq,
            SealedOwnerEstimate: null,
            DoaApprovalInstanceId: null
        );

        _packages.Insert(0, package);
        return package;
    }

    /// <summary>
    /// Menyegel HPS / Owner Estimate (OE) menggunakan Hybrid Envelope Encryption (AES-256-GCM + RSA)
    /// dan otomatis membangkitkan alur persetujuan berjenjang di DOA Inbox sesuai nilai Pagu/HPS.
    /// </summary>
    public ProcurementPackageDto SealPackageOwnerEstimateHps(Guid packageId, SealPackageHpsRequestDto input)
    {
        int pkgIdx = _packages.FindIndex(p => p.Id == packageId);
        if (pkgIdx < 0)
            throw new KeyNotFoundException("Paket pengadaan tidak ditemukan.");

        var pkg = _packages[pkgIdx];
        if (input.TotalHpsPlaintextAmount <= 0 || input.TotalHpsPlaintextAmount > pkg.TotalPaguBudget)
        {
            throw new InvalidOperationException(
                $"Eproc:Hps:ExceedsPagu - Nilai HPS (Rp {input.TotalHpsPlaintextAmount:N2}) tidak boleh melebihi Pagu Anggaran Paket (Rp {pkg.TotalPaguBudget:N2}).");
        }

        var now = DateTimeOffset.UtcNow;
        var scheduledOpening = now.AddDays(Math.Max(1, input.DaysUntilBidOpening));

        string secretHpsJson = JsonSerializer.Serialize(new
        {
            PackageCode = pkg.PackageCode,
            TotalHpsAmount = input.TotalHpsPlaintextAmount,
            CostHistoryNotes = input.CostHistoryNotes,
            SealedAt = now
        });

        var sealedEnvelope = _cryptoService.SealEnvelope(
            secretHpsJson,
            _committeeRsaKey,
            scheduledOpening,
            now);

        _sealedHpsVault[packageId] = sealedEnvelope;

        var sealedDto = new SealedOwnerEstimateDto(
            PackageId: packageId,
            CiphertextHexPreview: Convert.ToHexStringLower(sealedEnvelope.CiphertextWithTag)[..Math.Min(48, sealedEnvelope.CiphertextWithTag.Length * 2)] + "...",
            NonceHex: Convert.ToHexStringLower(sealedEnvelope.NonceIv),
            PayloadSha256Hash: sealedEnvelope.PayloadSha256Hash,
            SealedAtUtc: sealedEnvelope.SealedAtUtc,
            ScheduledOpeningUtc: sealedEnvelope.ScheduledOpeningUtc,
            IsOpenedAndDecrypted: false,
            DecryptedTotalHps: null // Terkunci rapat (Zero-Knowledge) sampai jadwal Bid Opening Ceremony!
        );

        // Bangkitkan rute persetujuan DOA (Delegation of Authority) berdasarkan nilai paket
        var doaRoute = _doaEngine.ResolveApprovalRoute(pkg.TotalPaguBudget, "PACKAGE_AND_HPS_INITIATION");
        var doaId = Guid.NewGuid();

        var doaSteps = doaRoute.Select(step => new DoaApprovalLogItemDto(
            StepSequence: step.StepSequence,
            ApproverRoleCode: step.ApproverRoleCode,
            ApproverPositionGrade: step.ApproverPositionGrade,
            Action: step.StepSequence == 1 ? "APPROVED" : "PENDING", // Step 1 (PPK) langsung otomatis submit/approve saat menyegel HPS
            ActedByName: step.StepSequence == 1 ? "Pejabat Pembuat Komitmen (PPK)" : null,
            Comments: step.StepSequence == 1 ? "HPS telah disusun dan disegel kriptografis (AES-256-GCM)." : null,
            ActedAtUtc: step.StepSequence == 1 ? now : null
        )).ToList();

        bool immediatelyApproved = doaSteps.Count == 1;
        var doaInstance = new DoaApprovalInstanceDto(
            Id: doaId,
            DocumentType: "PACKAGE_AND_HPS_INITIATION",
            ReferenceId: packageId,
            ReferenceCode: pkg.PackageCode,
            ReferenceTitle: pkg.PackageName,
            TransactionAmountIdr: pkg.TotalPaguBudget,
            CurrentStepSequence: immediatelyApproved ? 1 : 2,
            TotalStepsCount: doaSteps.Count,
            OverallStatus: immediatelyApproved ? "APPROVED" : "IN_PROGRESS",
            Steps: doaSteps
        );

        _doaInstances.RemoveAll(d => d.ReferenceId == packageId);
        _doaInstances.Insert(0, doaInstance);

        var updatedPkg = pkg with
        {
            SealedOwnerEstimate = sealedDto,
            DoaApprovalInstanceId = doaId,
            Status = immediatelyApproved ? "DOA_APPROVED_READY_FOR_TENDER" : "HPS_SEALED_WAITING_DOA"
        };

        _packages[pkgIdx] = updatedPkg;
        return updatedPkg;
    }

    /// <summary>
    /// Memproses persetujuan atau penolakan berjenjang pada DOA Inbox.
    /// Jika seluruh jenjang selesai disetujui, status Paket Pengadaan otomatis menjadi DOA_APPROVED_READY_FOR_TENDER.
    /// </summary>
    public DoaApprovalInstanceDto ActOnDoaApprovalTask(Guid doaInstanceId, ActOnDoaTaskRequestDto input)
    {
        int idx = _doaInstances.FindIndex(d => d.Id == doaInstanceId);
        if (idx < 0)
            throw new KeyNotFoundException("Dokumen persetujuan DOA tidak ditemukan.");

        var instance = _doaInstances[idx];
        if (instance.OverallStatus != "IN_PROGRESS")
            throw new InvalidOperationException($"Dokumen DOA sudah berstatus '{instance.OverallStatus}'.");

        var currentStep = instance.Steps.First(s => s.StepSequence == instance.CurrentStepSequence);

        var updatedSteps = instance.Steps.Select(s =>
        {
            if (s.StepSequence != instance.CurrentStepSequence)
                return s;

            return s with
            {
                Action = input.Approve ? "APPROVED" : "REJECTED",
                ActedByName = input.ActorFullName,
                Comments = input.Comments,
                ActedAtUtc = DateTimeOffset.UtcNow
            };
        }).ToList();

        bool isLastStep = instance.CurrentStepSequence >= instance.TotalStepsCount;
        string newOverallStatus = !input.Approve
            ? "REJECTED"
            : isLastStep ? "APPROVED" : "IN_PROGRESS";

        int nextStepSeq = input.Approve && !isLastStep
            ? instance.CurrentStepSequence + 1
            : instance.CurrentStepSequence;

        var updatedInstance = instance with
        {
            CurrentStepSequence = nextStepSeq,
            OverallStatus = newOverallStatus,
            Steps = updatedSteps
        };

        _doaInstances[idx] = updatedInstance;

        // Sinkronkan status Paket Pengadaan jika DOA telah selesai
        int pkgIdx = _packages.FindIndex(p => p.Id == instance.ReferenceId);
        if (pkgIdx >= 0)
        {
            _packages[pkgIdx] = _packages[pkgIdx] with
            {
                Status = newOverallStatus == "APPROVED"
                    ? "DOA_APPROVED_READY_FOR_TENDER"
                    : newOverallStatus == "REJECTED"
                        ? "DOA_REJECTED_NEED_REVISION"
                        : "HPS_SEALED_WAITING_DOA"
            };
        }

        return updatedInstance;
    }

    private void SeedInitialPlanningData()
    {
        // 1. Seed Master Pagu RKAP (Capex & Opex)
        var capexId = Guid.NewGuid();
        var opexId = Guid.NewGuid();

        _rkapBudgets.Add(new RkapBudgetDto(
            Id: capexId,
            CostCenterCode: "CC-INFRA-001",
            CostCenterName: "Divisi Infrastruktur & EPC Strategis",
            FiscalYear: 2026,
            BudgetType: "CAPEX",
            WbsElementGlAccount: "WBS-2026-CAPEX-EPC-01",
            BudgetName: "Pagu Investasi Pembangunan Gardu & Jaringan Pipa Distribusi",
            RevisedAmount: 150_000_000_000m,
            CommittedAmount: 0m,
            ActualSpentAmount: 12_000_000_000m,
            AvailableAmount: 138_000_000_000m
        ));

        _rkapBudgets.Add(new RkapBudgetDto(
            Id: opexId,
            CostCenterCode: "CC-OPS-002",
            CostCenterName: "Divisi Operasional & Pemeliharaan Regional",
            FiscalYear: 2026,
            BudgetType: "OPEX",
            WbsElementGlAccount: "GL-510200-OPEX-VALVE",
            BudgetName: "Pagu Pengadaan Suku Cadang Industrial Valve & Pompa",
            RevisedAmount: 45_000_000_000m,
            CommittedAmount: 0m,
            ActualSpentAmount: 5_000_000_000m,
            AvailableAmount: 40_000_000_000m
        ));

        // 2. Seed 2 PR dari Cabang/Regional berbeda dengan kode material sama (Siap Dikonsolidasikan!)
        CreatePurchaseRequisition(new CreatePurchaseRequisitionRequestDto(
            RkapBudgetId: opexId,
            PrTitle: "Kebutuhan Gate Valve API 600 DN300 — Regional Jawa Barat",
            RequesterUnitName: "Cabang Regional Jawa Barat (Bandung)",
            Category: ProcurementCategory.Goods,
            RequiredDeliveryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)),
            RequiredCsmsLevel: CsmsRiskLevel.Medium,
            Items:
            [
                new(Guid.NewGuid(), 1, "KIMAP-VLV-600-DN300", "Gate Valve Carbon Steel API 600 Class 300 DN300 (TKDN Wajib)", 20m, "UNIT", 125_000_000m, 2_500_000_000m, "Gudang Regional Jabar")
            ]));

        CreatePurchaseRequisition(new CreatePurchaseRequisitionRequestDto(
            RkapBudgetId: opexId,
            PrTitle: "Kebutuhan Gate Valve API 600 DN300 — Regional Jawa Timur",
            RequesterUnitName: "Cabang Regional Jawa Timur (Surabaya)",
            Category: ProcurementCategory.Goods,
            RequiredDeliveryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)),
            RequiredCsmsLevel: CsmsRiskLevel.Medium,
            Items:
            [
                new(Guid.NewGuid(), 1, "KIMAP-VLV-600-DN300", "Gate Valve Carbon Steel API 600 Class 300 DN300 (TKDN Wajib)", 30m, "UNIT", 125_000_000m, 3_750_000_000m, "Gudang Regional Jatim")
            ]));

        // 3. Seed 1 PR Konstruksi EPC Besar yang langsung dikonsolidasi & disegel HPS-nya untuk contoh di DOA Inbox
        var epcPr = CreatePurchaseRequisition(new CreatePurchaseRequisitionRequestDto(
            RkapBudgetId: capexId,
            PrTitle: "Pembangunan EPC Stasiun Kompresor & Metering System",
            RequesterUnitName: "Direktorat Proyek Infrastruktur Pusat",
            Category: ProcurementCategory.Construction,
            RequiredDeliveryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(180)),
            RequiredCsmsLevel: CsmsRiskLevel.High,
            Items:
            [
                new(Guid.NewGuid(), 1, "WBS-EPC-KOMP-01", "Pekerjaan Sipil, Mekanikal, Elektrikal & Instrumentasi Stasiun Kompresor", 1m, "LOT", 18_500_000_000m, 18_500_000_000m, "Site Proyek Utama")
            ]));

        var epcPkg = ConsolidatePrsToPackage(new ConsolidatePrToPackageRequestDto(
            PackageName: "Tender EPC Pembangunan Stasiun Kompresor & Metering System",
            PurchaseRequisitionIds: [epcPr.Id],
            SourcingMethod: SourcingMethod.TenderUmum,
            EnvelopeMethod: SubmissionEnvelopeMethod.DuaSampul,
            EvaluationMethod: EvaluationMethod.SistemNilaiScoring,
            ContractType: ContractType.GabunganLumsumHargaSatuan,
            MinTkdnRequirementPct: 35.0m,
            EnableEReverseAuction: true));

        SealPackageOwnerEstimateHps(epcPkg.Id, new SealPackageHpsRequestDto(
            TotalHpsPlaintextAmount: 17_850_000_000m,
            CostHistoryNotes: "Berdasarkan Kontrak Historis 2025 + Eskalasi Indeks BPS 3.2%",
            DaysUntilBidOpening: 7));
    }
}
