-- =============================================================================
-- SEED DATA: BUMN ROLES (SEGREGATION OF DUTIES) & DEFAULT DOA MATRIX TIERS
-- =============================================================================

INSERT INTO roles (role_code, role_name, role_category, sod_conflict_roles, description)
VALUES
    ('PA_KPA', 'Pengguna / Kuasa Pengguna Anggaran (Direksi / GM)', 'INTERNAL', '{"POKJA_COMMITTEE","VENDOR_ADMIN"}', 'Menetapkan RKAP, DRP, dan persetujuan DOA nilai tinggi'),
    ('PPK', 'Pejabat Pembuat Komitmen (Project Owner)', 'INTERNAL', '{"POKJA_COMMITTEE","BUYER_PEJABAT_PENGADAAN","VMS_VERIFIER","VENDOR_ADMIN"}', 'Menyusun KAK/Spek, HPS/OE terenkripsi, SPPBJ, dan mengendalikan kontrak'),
    ('POKJA_COMMITTEE', 'Kelompok Kerja Pemilihan (Panitia Tender UKPBJ)', 'COMMITTEE', '{"PA_KPA","PPK","FINANCE_AP","VENDOR_ADMIN"}', 'Menjalankan proses tender, membuka sampul penawaran, evaluasi HEA TKDN, dan menetapkan pemenang'),
    ('BUYER_PEJABAT_PENGADAAN', 'Pejabat Pengadaan / Buyer Operasional', 'COMMITTEE', '{"PPK","FINANCE_AP","VENDOR_ADMIN"}', 'Menjalankan Pengadaan Langsung (e-Quotation) dan Call-Off E-Katalog'),
    ('VMS_VERIFIER', 'Verifikator Kualifikasi & Legalitas Vendor', 'INTERNAL', '{"PPK","POKJA_COMMITTEE","VENDOR_ADMIN"}', 'Memverifikasi dokumen legalitas, akta, KBLI, dan laporan keuangan vendor'),
    ('HSE_CSMS_AUDITOR', 'Auditor K3L / HSE (CSMS)', 'INTERNAL', '{"VENDOR_ADMIN"}', 'Melakukan evaluasi dokumen dan audit lapangan CSMS'),
    ('LEGAL_OFFICER', 'Pejabat Hukum & Kontrak Korporat', 'INTERNAL', '{"POKJA_COMMITTEE","VENDOR_ADMIN"}', 'Review pasal kontrak (redlining) dan validasi keabsahan jaminan/perjanjian'),
    ('WAREHOUSE_QC', 'Petugas Gudang & Inspeksi Quality Control', 'INTERNAL', '{"BUYER_PEJABAT_PENGADAAN","FINANCE_AP","VENDOR_ADMIN"}', 'Memeriksa pengiriman barang (ASN) dan menerbitkan Goods Receipt (GR)'),
    ('FINANCE_AP', 'Verifikator Keuangan & Perbendaharaan (AP)', 'INTERNAL', '{"PPK","POKJA_COMMITTEE","WAREHOUSE_QC","VENDOR_ADMIN"}', 'Verifikasi 3-Way Matching, BAP, dan pencairan pembayaran ERP (SAP MIRO)'),
    ('TAX_OFFICER', 'Verifikator Pajak WAPU BUMN', 'INTERNAL', '{"VENDOR_ADMIN"}', 'Memvalidasi Faktur Pajak e-Faktur WAPU dan bukti potong PPh'),
    ('INTERNAL_AUDITOR', 'Satuan Pengawasan Intern (SPI) / Auditor BPK', 'AUDITOR', '{"PA_KPA","PPK","POKJA_COMMITTEE","FINANCE_AP","VENDOR_ADMIN"}', 'Akses read-only ke seluruh dokumen dan Immutable Hash-Chain Audit Trail'),
    ('VENDOR_ADMIN', 'Administrator Perusahaan Penyedia / Vendor', 'EXTERNAL_VENDOR', '{"PA_KPA","PPK","POKJA_COMMITTEE","BUYER_PEJABAT_PENGADAAN","VMS_VERIFIER","FINANCE_AP","INTERNAL_AUDITOR"}', 'Mengelola profil VMS, CSMS, penawaran tender, kontrak, BAST, dan e-Invoice')
ON CONFLICT (role_code) DO NOTHING;
