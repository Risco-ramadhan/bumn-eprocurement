# Enterprise Source-to-Pay (S2P) e-Procurement Platform BUMN (`eproc`)

Platform **Enterprise Source-to-Pay (S2P) e-Procurement BUMN** yang dirancang patuh penuh terhadap **Permen BUMN No. PER-2/MBU/03/2023** (*Pedoman Pengadaan Barang dan Jasa Badan Usaha Milik Negara*), mencakup 4 kategori pengadaan (*Barang, Konstruksi/EPC, Jasa Konsultansi, Jasa Lainnya*), 7 metode pemilihan, dan 3 metode penyampaian sampul (*1 Sampul, 2 Sampul, 2 Tahap*).

---

## Arsitektur Utama
1. **Modular DDD (.NET 9 + Angular 19) dengan Custom Prefix `eproc` (100% Zero-ABP Fingerprint)**:
   - Seluruh tabel database (`83 tabel`), rute REST API (`/api/eproc/*`), HTTP Header (`X-Eproc-Client-Code`, `X-Eproc-Xsrf-Token`), permission (`eproc.*`), dan komponen Angular (`<eproc-root>`) bersih dari string `Abp` maupun `Volo`.
2. **Environment-Driven Client Isolation (Tanpa `tenant_id` di Database)**:
   - Setiap klien BUMN di-deploy pada server dan database terdedikasi masing-masing. Identitas instansi, tema warna, kebijakan WAPU PPN 11%, dan batas preferensi TKDN dikonfigurasi melalui `appsettings.json` / `.env` (`EPROC_*`).
3. **3 Portal Terpadu (25 Menu Utama & 86 Sub-Menu)**:
   - **Portal Publik**: 5 Menu Utama
   - **Portal Eksternal Vendor / Penyedia**: 9 Menu Utama & 34 Sub-Menu
   - **Portal Internal BUMN**: 11 Menu Utama & 52 Sub-Menu

---

## Struktur Proyek
- `database/migrations/001_full_schema_eproc_bumn.sql` — Skema PostgreSQL lengkap (**83 Tabel** lintas 9 domain S2P + indeks performa).
- `database/seeds/002_seed_roles_permissions_doa.sql` — Seed 12 Role BUMN beserta aturan *Segregation of Duties (SoD)*.
- `aspnet-core/` — Backend `.NET 9` Modular DDD (`Eproc.Domain.Shared`, `Eproc.Domain`, `Eproc.Application.Contracts`, `Eproc.Application`, `Eproc.HttpApi.Host`, `Eproc.Domain.Tests`).
- `angular/` — Frontend Angular Standalone (`prefix: "eproc"`) dengan `APP_INITIALIZER` bootstrap dan 15 Tab Kerja Interaktif (Fase 1 s/d Fase 5).
- `docs/` — Dokumen Blueprint Arsitektur, Skema 83 Tabel, Rumus Matematis, dan Walkthrough Pengujian.

---

## Cara Menjalankan & Menguji

### 1. Menjalankan Unit Test & Build Backend (.NET 9)
```bash
cd aspnet-core
dotnet test
dotnet build
```
*(Seluruh **13/13 Unit Test** mencakup Fase 1 s/d Fase 5 lulus 100% dengan 0 Warning & 0 Error).*

### 2. Menjalankan REST API Host (.NET 9)
```bash
cd aspnet-core/src/Eproc.HttpApi.Host
dotnet run
```

### 3. Menjalankan Frontend Angular (`eproc`)
```bash
cd angular
npm install
npm start
```
