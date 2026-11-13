# Walkthrough Lengkap: Seluruh 5 Fase Enterprise Source-to-Pay (S2P) e-Procurement BUMN (`eproc`)

Seluruh **Fondasi Arsitektur** dan **Fase 1 s/d Fase 5 (Siklus Penuh Source-to-Pay / S2P)** untuk Platform Enterprise e-Procurement BUMN yang patuh terhadap **Permen BUMN No. PER-2/MBU/03/2023** telah selesai dibangun, diintegrasikan, dan diverifikasi di:
`C:/Users/Risco Ramadhan/.gemini/antigravity/scratch/bumn-eprocurement`

> [!IMPORTANT]
> **Kepatuhan Arsitektur Khusus**:
> 1. **100% Zero-ABP Fingerprint (`prefix: "eproc"`)**: Seluruh tabel database (`83 tabel`), rute REST API (`/api/eproc/*`), HTTP Header (`X-Eproc-Client-Code`, `X-Eproc-Xsrf-Token`), permission (`eproc.*`), dan komponen Angular (`<eproc-root>`) bersih dari string `Abp` maupun `Volo`.
> 2. **Isolasi Klien Berbasis Environment (`appsettings.json` / `.env`)**: Tanpa kolom `tenant_id` di database karena setiap BUMN men-deploy di server masing-masing.
> 3. **Cakupan 3 Portal**: **25 Menu Utama & 86 Sub-Menu** (*Portal Publik, Portal Vendor, Portal Internal BUMN*).

---

## 1. Apa yang Telah Diselesaikan pada Fase 5 (Eksekusi Proyek, Kurva S SCM, BAST Post-Audit TKDN, 3-Way Matching WAPU & Rapor VPE)

### A. Backend .NET 9 Modular DDD (`aspnet-core/Eproc.sln` — 0 Error, 0 Warning)
1. **Model & Kontrak Eksekusi Proyek, Keuangan 3-Way Matching WAPU & VPE (`ExecutionAndFinanceModels.cs`)**:
   - [ExecutionAndFinanceModels.cs](file:///C:/Users/Risco%20Ramadhan/.gemini/antigravity/scratch/bumn-eprocurement/aspnet-core/src/Eproc.Application.Contracts/ExecutionAndFinance/ExecutionAndFinanceModels.cs): Mencakup DTO untuk `AsnShipmentAndGrDto` (Pengiriman ASN & QC Gudang), `SCurveProgressAndScmDto` (Kurva S Konstruksi & *Show Cause Meeting* I/II/III), `BastHandoverDocumentDto` & `PostAuditTkdnVerificationDto` (BAST/PHO + Verifikasi Realisasi TKDN Surveyor Independen), `VendorEInvoiceSettlementDto` (3-Way Matching e-Invoicing, Pajak WAPU `030.`, BAP & SP2D), serta `VendorPerformanceScorecardDto` (Rapor VPE 5 Dimensi).
2. **Application Service Eksekusi Proyek & Keuangan (`ProjectExecutionAndFinanceAppService.cs`)**:
   - [ProjectExecutionAndFinanceAppService.cs](file:///C:/Users/Risco%20Ramadhan/.gemini/antigravity/scratch/bumn-eprocurement/aspnet-core/src/Eproc.Application/ExecutionAndFinance/ProjectExecutionAndFinanceAppService.cs):
     - **Expediting ASN & Goods Receipt QC (`RecordAsnAndGoodsReceipt`)**: Memisahkan barang yang lulus inspeksi QC (`AcceptedQty`) dari barang cacat (`RejectedDefectiveQty`) sehingga nilai bruto GR hanya mengakui barang yang lulus uji kualitas.
     - **Deteksi Kontrak Kritis Kurva S & Eskalasi SCM I/II/III (`EvaluateAndRecordSCurveProgress`)**: Mengevaluasi deviasi mingguan fisik konstruksi (kritis bila deviasi $\le -10\%$ pada rencana $0\text{--}70\%$ atau $\le -5\%$ pada rencana $70\text{--}100\%$) dan menaikkan status otomatis dari `None` $\rightarrow$ `ScmLevel1` $\rightarrow$ `ScmLevel2` $\rightarrow$ `ScmLevel3`.
     - **BAST & Sanksi Otomatis Post-Audit TKDN (`ApproveBastWithPostAuditTkdn`)**: Membandingkan komitmen TKDN saat tender dengan hasil verifikasi akhir *PT Surveyor Indonesia / PT Sucofindo*; bila terdapat kekurangan (*shortfall*), sistem menghitung sanksi finansial TKDN secara proporsional.
     - **3-Way Matching e-Invoicing, Denda $1\text{‰}$, Pajak WAPU `030.` & BAP (`SubmitAndMatchVendorEInvoice`)**:
       - Memvalidasi kode seri Faktur Pajak **Wajib Pungut (WAPU) BUMN** wajib diawali `"030."` (`Eproc:Finance:InvalidWapuFakturCode`).
       - Mencocokkan 3 dokumen (*Kontrak vs BAST/GR vs e-Invoice Vendor*).
       - Menghitung otomatis potongan pengembalian Uang Muka (*DP Recoupment*), Retensi Pemeliharaan $5\%$, Denda Keterlambatan $1\text{‰}/\text{hari}$ (maksimal $5\%$), dan Sanksi Kekurangan TKDN.
       - Memisahkan PPN $11\%$ untuk disetor langsung oleh WAPU BUMN ke Kas Negara, memotong PPh Pasal 22/23/4(2), serta menerbitkan **Berita Acara Pembayaran (BAP)** dan nilai bersih `SP2D`.
     - **Evaluasi Kinerja Vendor / VPE 5 Dimensi (`EvaluateVendorPerformanceVpe`)**: Menghitung skor tertimbang (*Kualitas 30%, Ketepatan Waktu 25%, K3L/CSMS 20%, Realisasi TKDN 15%, Responsivitas 10%*) dan menetapkan predikat AVL (`PLATINUM`, `GOLD`, `SILVER`, `RED_WARNING`, `BLACKLIST_CANDIDATE`).
3. **REST API Endpoints Fase 5 (`Program.cs`)**:
   - [Program.cs](file:///C:/Users/Risco%20Ramadhan/.gemini/antigravity/scratch/bumn-eprocurement/aspnet-core/src/Eproc.HttpApi.Host/Program.cs):
     - `GET /api/eproc/execution/asn-gr` & `POST /api/eproc/execution/asn-gr`
     - `GET /api/eproc/execution/scurve-scm` & `POST /api/eproc/execution/scurve-scm`
     - `GET /api/eproc/execution/bast` & `POST /api/eproc/execution/bast`
     - `GET /api/eproc/finance/einvoices` & `POST /api/eproc/finance/einvoices/match-and-settle`
     - `GET /api/eproc/vms/vpe-scorecards` & `POST /api/eproc/vms/vpe-scorecards`

### B. Frontend Angular (`angular/` dengan `"prefix": "eproc"`)
- [app.component.ts](file:///C:/Users/Risco%20Ramadhan/.gemini/antigravity/scratch/bumn-eprocurement/angular/src/app/app.component.ts):
  Kini memiliki **15 Tab Kerja Interaktif** yang mencakup seluruh Fase 1 s/d Fase 5:
  - **Tab 1–4 (Fase 1)**: Direktori AVL, Registrasi & Kalkulator SKN/KD/Altman Z-Score, Audit CSMS 8 Elemen K3L, & Detektor Afiliasi *Beneficial Ownership (BO)*.
  - **Tab 5–7 (Fase 2)**: Pagu RKAP & Konsolidasi PR Lintas Cabang, BoQ/AHSP & Penyegelan HPS Terenkripsi (`AES-256-GCM`), & Antrean Persetujuan Berjenjang DOA Inbox.
  - **Tab 8–10 (Fase 3)**: Forum Aanwijzing Anonim & Brankas Penawaran Terenkripsi (*Encrypted Bid Vault*), Pembukaan Sampul 2 & Evaluasi HEA TKDN + *Live e-Reverse Auction* (*Anti-Sniping Auto-Extend*), serta Negosiasi BAFO, Pemenang (BAHP) & Masa Sanggah.
  - **Tab 11–12 (Fase 4)**: E-Katalog Kontrak Payung (*Tiered Volume Pricing* & *Call-Off Order*) serta Siklus Kontrak *e-CLM* (*Contract Redlining*, Jaminan Pelaksanaan $5\%$, e-Meterai Peruri + TTE BSrE/PSrE, SPMK, & Addendum/CCO Plafon $10\%$).
  - **Tab 13–14 (Fase 5)**: Pemantauan Kurva S Konstruksi (*SCM Tingkat I/II/III*), BAST & *Post-Audit Realisasi TKDN*, *3-Way Matching e-Invoicing*, Pajak WAPU BUMN (`030.`), BAP/SP2D, & Rapor Kinerja Vendor (*VPE 5 Dimensi*).
  - **Tab 15**: *Immutable Forensic Audit Trail* (`SHA-256 Hash-Chain`).

---

## 2. Rekapitulasi Verifikasi Build & Unit Testing (`13/13 Passed`)

Seluruh solution `.NET 9` di [Eproc.sln](file:///C:/Users/Risco%20Ramadhan/.gemini/antigravity/scratch/bumn-eprocurement/aspnet-core/Eproc.sln) berhasil di-*build* dengan **0 Warning, 0 Error**, dan seluruh **13 Unit Test** di [BumnEprocDomainTests.cs](file:///C:/Users/Risco%20Ramadhan/.gemini/antigravity/scratch/bumn-eprocurement/aspnet-core/test/Eproc.Domain.Tests/BumnEprocDomainTests.cs) **100% Lulus (Passed)**:
1. `TkdnHeaCalculator_GivesPreferenceToDomesticProduct_AndWinsOverLowerRawImportBid`
2. `TkdnHeaCalculator_FlagsOfferBelow80PercentHps_AndRequires5PercentOfHpsBond`
3. `VendorFinancialScoring_CalculatesKdSknAltmanAndEnforcesCsmsGate`
4. `EnvelopeCryptoService_PreventsEarlyOpening_AndDecryptsWhenScheduleAndStageAreValid`
5. `ThreeWayMatchingService_CalculatesWapuTaxesPenaltiesAndRetentionCorrectly`
6. `DoaAndSodEngine_BlocksConflictingRoles_AndRoutesMultiTierApprovalByThreshold`
7. `AuditHashChainAndScurveEvaluator_DetectsTampering_AndTriggersScmLevels`
8. `PermissionCatalog_HasZeroAbpPrefix_AndUsesEprocNamespace`
9. `Phase1VmsCsmsService_RegistersVendorCertifiesCsmsDetectsBoAffiliationAndMaintainsAuditChain`
10. `Phase2PlanningAndDoaService_EnforcesRkapBudgetConsolidatesPrsSealsHpsAndExecutesMultiTierDoa`
11. `Phase3ESourcingTenderService_ExecutesBidVaultOpeningHeaRankingAntiSnipingAuctionBafoAndSanggah`
12. `Phase4ECatalogAndClmService_ExecutesTieredCallOffOrderEnforcesPerformanceBondTteBeforeSpmkAndGuardsCcoCeiling`
13. `Phase5ExecutionAndFinanceService_DetectsScurveScmCalculatesPostAuditTkdnSanctionExecutes3WayMatchWapuAndScoresVpe`
