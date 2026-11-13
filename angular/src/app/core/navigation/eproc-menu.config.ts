export type EprocPortalType = 'PUBLIC_PORTAL' | 'VENDOR_PORTAL' | 'INTERNAL_BUMN_PORTAL';

export interface EprocSubMenuItem {
  code: string;
  label: string;
  route: string;
  requiredPermission: string;
  allowedRoles: string[];
}

export interface EprocMainMenuItem {
  id: string;
  portal: EprocPortalType;
  order: number;
  title: string;
  icon: string;
  route: string;
  requiredPermission: string;
  subMenus: EprocSubMenuItem[];
}

/**
 * Konfigurasi Peta Navigasi Lengkap Enterprise e-Procurement BUMN:
 * - Portal Publik       : 5 Menu Utama
 * - Portal Vendor       : 9 Menu Utama (34 Sub-Menu)
 * - Portal Internal BUMN: 11 Menu Utama (52 Sub-Menu)
 * Total Keseluruhan     : 25 Menu Utama & 86 Sub-Menu Fungsional
 */
export const EPROC_NAVIGATION_MENUS: EprocMainMenuItem[] = [
  // ===========================================================================
  // A. PORTAL PUBLIK (5 MENU UTAMA)
  // ===========================================================================
  {
    id: 'PUB-01',
    portal: 'PUBLIC_PORTAL',
    order: 1,
    title: 'Beranda & Transparansi',
    icon: 'eproc-icon-globe',
    route: '/public/home',
    requiredPermission: 'eproc.public.dashboard',
    subMenus: [],
  },
  {
    id: 'PUB-02',
    portal: 'PUBLIC_PORTAL',
    order: 2,
    title: 'Pengumuman Pengadaan',
    icon: 'eproc-icon-megaphone',
    route: '/public/announcements',
    requiredPermission: 'eproc.public.announcements',
    subMenus: [],
  },
  {
    id: 'PUB-03',
    portal: 'PUBLIC_PORTAL',
    order: 3,
    title: 'Daftar Hitam (Blacklist)',
    icon: 'eproc-icon-shield-alert',
    route: '/public/blacklist',
    requiredPermission: 'eproc.public.blacklist',
    subMenus: [],
  },
  {
    id: 'PUB-04',
    portal: 'PUBLIC_PORTAL',
    order: 4,
    title: 'Verifikasi Dokumen QR',
    icon: 'eproc-icon-qrcode',
    route: '/public/qr-verifier',
    requiredPermission: 'eproc.public.qr_verifier',
    subMenus: [],
  },
  {
    id: 'PUB-05',
    portal: 'PUBLIC_PORTAL',
    order: 5,
    title: 'Pusat Regulasi & Bantuan',
    icon: 'eproc-icon-book-open',
    route: '/public/regulations',
    requiredPermission: 'eproc.public.regulations',
    subMenus: [],
  },

  // ===========================================================================
  // B. PORTAL EKSTERNAL VENDOR (9 MENU UTAMA / 34 SUB-MENU)
  // ===========================================================================
  {
    id: 'VND-01',
    portal: 'VENDOR_PORTAL',
    order: 1,
    title: 'Dashboard Vendor',
    icon: 'eproc-icon-layout-dashboard',
    route: '/vendor/dashboard',
    requiredPermission: 'eproc.vendor_portal.dashboard',
    subMenus: [
      { code: 'VND-01-01', label: 'Ringkasan Tender Diikuti & Jadwal Hari Ini', route: '/vendor/dashboard/overview', requiredPermission: 'eproc.vendor_portal.dashboard', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-01-02', label: 'Alert Masa Berlaku Dokumen & Jaminan', route: '/vendor/dashboard/expiry-alerts', requiredPermission: 'eproc.vendor_portal.dashboard', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-01-03', label: 'Rapor Skor Kinerja (VPE Grade) & Status AVL', route: '/vendor/dashboard/avl-status', requiredPermission: 'eproc.vendor_portal.dashboard', allowedRoles: ['VENDOR_ADMIN'] },
    ],
  },
  {
    id: 'VND-02',
    portal: 'VENDOR_PORTAL',
    order: 2,
    title: 'Profil & Kualifikasi (VMS)',
    icon: 'eproc-icon-building-2',
    route: '/vendor/vms',
    requiredPermission: 'eproc.vendor_portal.vms_profile',
    subMenus: [
      { code: 'VND-02-01', label: 'Data Perusahaan & Kantor Cabang', route: '/vendor/vms/company-profile', requiredPermission: 'eproc.vendor_portal.vms_profile', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-02-02', label: 'Akta, Pemegang Saham & Beneficial Ownership', route: '/vendor/vms/shareholders-bo', requiredPermission: 'eproc.vendor_portal.vms_profile', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-02-03', label: 'Susunan Pengurus (Direksi & Komisaris)', route: '/vendor/vms/board-members', requiredPermission: 'eproc.vendor_portal.vms_profile', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-02-04', label: 'Izin Usaha & Legalitas (NIB, SBU, SIUJK, ISO)', route: '/vendor/vms/legal-documents', requiredPermission: 'eproc.vendor_portal.vms_profile', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-02-05', label: 'KBLI & Klasifikasi Bidang Komoditas', route: '/vendor/vms/commodities-kbli', requiredPermission: 'eproc.vendor_portal.vms_profile', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-02-06', label: 'Keuangan, Neraca Audited & Pajak (NPWP, PKP, SPT)', route: '/vendor/vms/financial-tax', requiredPermission: 'eproc.vendor_portal.vms_profile', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-02-07', label: 'Daftar Pengalaman Kerja (Kontrak & BAST)', route: '/vendor/vms/experiences', requiredPermission: 'eproc.vendor_portal.vms_profile', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-02-08', label: 'Daftar Tenaga Ahli & Peralatan Utama', route: '/vendor/vms/experts-equipments', requiredPermission: 'eproc.vendor_portal.vms_profile', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-02-09', label: 'Rekening Bank Pencairan', route: '/vendor/vms/bank-accounts', requiredPermission: 'eproc.vendor_portal.vms_profile', allowedRoles: ['VENDOR_ADMIN'] },
    ],
  },
  {
    id: 'VND-03',
    portal: 'VENDOR_PORTAL',
    order: 3,
    title: 'K3L / HSE (CSMS)',
    icon: 'eproc-icon-hard-hat',
    route: '/vendor/csms',
    requiredPermission: 'eproc.vendor_portal.csms',
    subMenus: [
      { code: 'VND-03-01', label: 'Pengajuan & Kuesioner Prakualifikasi CSMS', route: '/vendor/csms/questionnaire', requiredPermission: 'eproc.vendor_portal.csms', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-03-02', label: 'Jadwal & Hasil Audit Lapangan HSE', route: '/vendor/csms/field-audit', requiredPermission: 'eproc.vendor_portal.csms', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-03-03', label: 'Sertifikat CSMS Aktif', route: '/vendor/csms/certificates', requiredPermission: 'eproc.vendor_portal.csms', allowedRoles: ['VENDOR_ADMIN'] },
    ],
  },
  {
    id: 'VND-04',
    portal: 'VENDOR_PORTAL',
    order: 4,
    title: 'Bursa & Proses Tender',
    icon: 'eproc-icon-gavel',
    route: '/vendor/tenders',
    requiredPermission: 'eproc.vendor_portal.bidding',
    subMenus: [
      { code: 'VND-04-01', label: 'Cari Paket Pengadaan (Tender Terbuka)', route: '/vendor/tenders/open-market', requiredPermission: 'eproc.vendor_portal.bidding', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-04-02', label: 'Undangan Pengadaan (Tender Terbatas / Penunjukan)', route: '/vendor/tenders/invitations', requiredPermission: 'eproc.vendor_portal.bidding', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-04-03', label: 'Paket Saya (Ruang Aanwijzing & Dokumen Pemilihan)', route: '/vendor/tenders/my-packages', requiredPermission: 'eproc.vendor_portal.bidding', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-04-04', label: 'Encrypted Bid Vault (Upload Sampul & TKDN)', route: '/vendor/tenders/bid-vault', requiredPermission: 'eproc.vendor_portal.bidding', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-04-05', label: 'Ruang e-Reverse Auction (Real-Time Bidding)', route: '/vendor/tenders/e-auction', requiredPermission: 'eproc.vendor_portal.eauction', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-04-06', label: 'Klarifikasi & Negosiasi Harga (BAFO)', route: '/vendor/tenders/bafo-negotiation', requiredPermission: 'eproc.vendor_portal.bidding', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-04-07', label: 'Pengajuan Sanggah & Sanggah Banding', route: '/vendor/tenders/objections-sanggah', requiredPermission: 'eproc.vendor_portal.bidding', allowedRoles: ['VENDOR_ADMIN'] },
    ],
  },
  {
    id: 'VND-05',
    portal: 'VENDOR_PORTAL',
    order: 5,
    title: 'E-Katalog & Toko Vendor',
    icon: 'eproc-icon-shopping-bag',
    route: '/vendor/ecatalog',
    requiredPermission: 'eproc.vendor_portal.ecatalog_store',
    subMenus: [
      { code: 'VND-05-01', label: 'Kelola Produk & Harga Berjenjang (Tier Pricing)', route: '/vendor/ecatalog/products', requiredPermission: 'eproc.vendor_portal.ecatalog_store', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-05-02', label: 'Pesanan Masuk (Call-Off Order & Mini-Competition)', route: '/vendor/ecatalog/call-off-orders', requiredPermission: 'eproc.vendor_portal.ecatalog_store', allowedRoles: ['VENDOR_ADMIN'] },
    ],
  },
  {
    id: 'VND-06',
    portal: 'VENDOR_PORTAL',
    order: 6,
    title: 'Kontrak & Jaminan (e-CLM)',
    icon: 'eproc-icon-file-signature',
    route: '/vendor/contracts',
    requiredPermission: 'eproc.vendor_portal.contracts',
    subMenus: [
      { code: 'VND-06-01', label: 'Penerimaan SPPBJ', route: '/vendor/contracts/sppbj', requiredPermission: 'eproc.vendor_portal.contracts', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-06-02', label: 'Review Draft Kontrak (Redlining) & TTE/e-Meterai', route: '/vendor/contracts/redlining-tte', requiredPermission: 'eproc.vendor_portal.contracts', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-06-03', label: 'Upload & Tracking Jaminan (Bank Garansi / Surety Bond)', route: '/vendor/contracts/guarantees-bonds', requiredPermission: 'eproc.vendor_portal.contracts', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-06-04', label: 'Pengajuan Addendum / CCO', route: '/vendor/contracts/addendum-cco', requiredPermission: 'eproc.vendor_portal.contracts', allowedRoles: ['VENDOR_ADMIN'] },
    ],
  },
  {
    id: 'VND-07',
    portal: 'VENDOR_PORTAL',
    order: 7,
    title: 'Eksekusi & Serah Terima',
    icon: 'eproc-icon-truck',
    route: '/vendor/execution',
    requiredPermission: 'eproc.vendor_portal.execution_bast',
    subMenus: [
      { code: 'VND-07-01', label: 'Pengiriman Barang (ASN / Surat Jalan)', route: '/vendor/execution/asn-shipping', requiredPermission: 'eproc.vendor_portal.execution_bast', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-07-02', label: 'Laporan Progres Mingguan, Kurva S & Tindak Lanjut SCM', route: '/vendor/execution/scurve-progress', requiredPermission: 'eproc.vendor_portal.execution_bast', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-07-03', label: 'Pengajuan Pemeriksaan (BAPP/GR/SES) & BAST (PHO/FHO)', route: '/vendor/execution/bast-handover', requiredPermission: 'eproc.vendor_portal.execution_bast', allowedRoles: ['VENDOR_ADMIN'] },
    ],
  },
  {
    id: 'VND-08',
    portal: 'VENDOR_PORTAL',
    order: 8,
    title: 'Tagihan & Pembayaran',
    icon: 'eproc-icon-receipt',
    route: '/vendor/invoicing',
    requiredPermission: 'eproc.vendor_portal.einvoicing',
    subMenus: [
      { code: 'VND-08-01', label: 'Buat e-Invoice & Upload Faktur Pajak / e-Bupot', route: '/vendor/invoicing/create-invoice', requiredPermission: 'eproc.vendor_portal.einvoicing', allowedRoles: ['VENDOR_ADMIN'] },
      { code: 'VND-08-02', label: 'Tracking Status Pembayaran (3-Way Matching s.d. Paid)', route: '/vendor/invoicing/payment-tracking', requiredPermission: 'eproc.vendor_portal.einvoicing', allowedRoles: ['VENDOR_ADMIN'] },
    ],
  },
  {
    id: 'VND-09',
    portal: 'VENDOR_PORTAL',
    order: 9,
    title: 'Rapor & Riwayat Sanksi',
    icon: 'eproc-icon-award',
    route: '/vendor/scorecard',
    requiredPermission: 'eproc.vendor_portal.vpe_scorecard',
    subMenus: [
      { code: 'VND-09-01', label: 'Kartu Skor Kinerja (VPE) & Riwayat Teguran/SP', route: '/vendor/scorecard/vpe-history', requiredPermission: 'eproc.vendor_portal.vpe_scorecard', allowedRoles: ['VENDOR_ADMIN'] },
    ],
  },

  // ===========================================================================
  // C. PORTAL INTERNAL BUMN (11 MENU UTAMA / 52 SUB-MENU)
  // ===========================================================================
  {
    id: 'INT-01',
    portal: 'INTERNAL_BUMN_PORTAL',
    order: 1,
    title: 'Executive Dashboard & Analytics',
    icon: 'eproc-icon-bar-chart-3',
    route: '/internal/executive',
    requiredPermission: 'eproc.internal.executive.rkap_saving',
    subMenus: [
      { code: 'INT-01-01', label: 'Executive Summary (Realisasi RKAP & Cost Saving HPS/Pagu)', route: '/internal/executive/rkap-cost-saving', requiredPermission: 'eproc.internal.executive.rkap_saving', allowedRoles: ['PA_KPA', 'PPK', 'INTERNAL_AUDITOR'] },
      { code: 'INT-01-02', label: 'Dashboard Capaian TKDN & Porsi UMKM / PaDi', route: '/internal/executive/tkdn-umkm', requiredPermission: 'eproc.internal.executive.tkdn_umkm', allowedRoles: ['PA_KPA', 'PPK', 'INTERNAL_AUDITOR'] },
      { code: 'INT-01-03', label: 'Bottleneck & SLA Monitoring Proses Tender', route: '/internal/executive/sla-monitoring', requiredPermission: 'eproc.internal.executive.sla_monitoring', allowedRoles: ['PA_KPA', 'POKJA_COMMITTEE', 'INTERNAL_AUDITOR'] },
      { code: 'INT-01-04', label: 'Early Warning Kontrak Kritis & Jaminan Expired', route: '/internal/executive/early-warning', requiredPermission: 'eproc.internal.executive.early_warning', allowedRoles: ['PA_KPA', 'PPK', 'LEGAL_OFFICER', 'INTERNAL_AUDITOR'] },
    ],
  },
  {
    id: 'INT-02',
    portal: 'INTERNAL_BUMN_PORTAL',
    order: 2,
    title: 'Kotak Persetujuan (DOA Inbox)',
    icon: 'eproc-icon-check-circle-2',
    route: '/internal/doa-inbox',
    requiredPermission: 'eproc.internal.doa_inbox.approve',
    subMenus: [
      { code: 'INT-02-01', label: 'My Approval Tasklist (PR, HPS, Pemenang, Kontrak, CCO, BAP)', route: '/internal/doa-inbox/tasks', requiredPermission: 'eproc.internal.doa_inbox.approve', allowedRoles: ['PA_KPA', 'PPK'] },
      { code: 'INT-02-02', label: 'Riwayat Persetujuan & Delegasi Wewenang Sementara', route: '/internal/doa-inbox/history-delegation', requiredPermission: 'eproc.internal.doa_inbox.delegate', allowedRoles: ['PA_KPA', 'PPK'] },
    ],
  },
  {
    id: 'INT-03',
    portal: 'INTERNAL_BUMN_PORTAL',
    order: 3,
    title: 'Manajemen Vendor & CSMS',
    icon: 'eproc-icon-users-check',
    route: '/internal/vms-admin',
    requiredPermission: 'eproc.internal.vms_admin.verify_legal',
    subMenus: [
      { code: 'INT-03-01', label: 'Antrean Verifikasi Registrasi & Perubahan Data Vendor', route: '/internal/vms-admin/verification-queue', requiredPermission: 'eproc.internal.vms_admin.verify_legal', allowedRoles: ['VMS_VERIFIER'] },
      { code: 'INT-03-02', label: 'Evaluasi Kemampuan Finansial (SKN, KD, Altman Z-Score)', route: '/internal/vms-admin/financial-scoring', requiredPermission: 'eproc.internal.vms_admin.evaluate_financial', allowedRoles: ['VMS_VERIFIER', 'POKJA_COMMITTEE'] },
      { code: 'INT-03-03', label: 'Database Approved Vendor List (AVL) & Pohon Komoditas', route: '/internal/vms-admin/avl-directory', requiredPermission: 'eproc.internal.vms_admin.manage_avl', allowedRoles: ['VMS_VERIFIER', 'POKJA_COMMITTEE', 'BUYER_PEJABAT_PENGADAAN'] },
      { code: 'INT-03-04', label: 'Evaluasi Kuesioner & Audit Lapangan CSMS (HSE)', route: '/internal/vms-admin/csms-audit', requiredPermission: 'eproc.internal.vms_admin.audit_csms', allowedRoles: ['HSE_CSMS_AUDITOR'] },
      { code: 'INT-03-05', label: 'Manajemen Sanksi (SP1/2/3, Suspend, Usulan Blacklist)', route: '/internal/vms-admin/sanctions-blacklist', requiredPermission: 'eproc.internal.vms_admin.sanctions_blacklist', allowedRoles: ['VMS_VERIFIER', 'PPK', 'LEGAL_OFFICER'] },
    ],
  },
  {
    id: 'INT-04',
    portal: 'INTERNAL_BUMN_PORTAL',
    order: 4,
    title: 'Perencanaan & Anggaran',
    icon: 'eproc-icon-wallet',
    route: '/internal/planning',
    requiredPermission: 'eproc.internal.planning.rkap',
    subMenus: [
      { code: 'INT-04-01', label: 'Master Pagu RKAP (Capex/Opex) & Cost Center', route: '/internal/planning/rkap-budgets', requiredPermission: 'eproc.internal.planning.rkap', allowedRoles: ['PA_KPA', 'PPK'] },
      { code: 'INT-04-02', label: 'Daftar Rencana Pengadaan (DRP Tahunan)', route: '/internal/planning/drp-annual', requiredPermission: 'eproc.internal.planning.drp', allowedRoles: ['PA_KPA', 'PPK'] },
      { code: 'INT-04-03', label: 'Buat & Kelola Purchase Requisition (PR) + Upload KAK', route: '/internal/planning/purchase-requisitions', requiredPermission: 'eproc.internal.planning.pr_create', allowedRoles: ['PPK'] },
      { code: 'INT-04-04', label: 'Konsolidasi PR Lintas Divisi / Unit Kerja', route: '/internal/planning/pr-consolidation', requiredPermission: 'eproc.internal.planning.pr_consolidate', allowedRoles: ['PPK', 'BUYER_PEJABAT_PENGADAAN'] },
      { code: 'INT-04-05', label: 'Penyusunan HPS / Owner Estimate (Terenkripsi) & AHSP', route: '/internal/planning/encrypted-hps-oe', requiredPermission: 'eproc.internal.planning.seal_hps', allowedRoles: ['PPK'] },
      { code: 'INT-04-06', label: 'Price Intelligence (Database Riwayat Harga PO & Pasar)', route: '/internal/planning/price-intelligence', requiredPermission: 'eproc.internal.planning.price_intelligence', allowedRoles: ['PPK', 'POKJA_COMMITTEE', 'BUYER_PEJABAT_PENGADAAN'] },
    ],
  },
  {
    id: 'INT-05',
    portal: 'INTERNAL_BUMN_PORTAL',
    order: 5,
    title: 'Manajemen Tender (e-Sourcing)',
    icon: 'eproc-icon-scale',
    route: '/internal/esourcing',
    requiredPermission: 'eproc.internal.esourcing.manage_rfp',
    subMenus: [
      { code: 'INT-05-01', label: 'Pembentukan Paket & Penugasan Pokja / Buyer', route: '/internal/esourcing/package-assignment', requiredPermission: 'eproc.internal.esourcing.assign_committee', allowedRoles: ['PA_KPA', 'POKJA_COMMITTEE'] },
      { code: 'INT-05-02', label: 'Penyusunan Dokumen Pemilihan, Jadwal, & Bobot Kriteria', route: '/internal/esourcing/rfp-schedules', requiredPermission: 'eproc.internal.esourcing.manage_rfp', allowedRoles: ['POKJA_COMMITTEE', 'BUYER_PEJABAT_PENGADAAN'] },
      { code: 'INT-05-03', label: 'Ruang Aanwijzing & Penerbitan BAP / Addendum', route: '/internal/esourcing/aanwijzing', requiredPermission: 'eproc.internal.esourcing.aanwijzing', allowedRoles: ['POKJA_COMMITTEE', 'PPK'] },
      { code: 'INT-05-04', label: 'Bid Opening Ceremony (Dekripsi HPS & Pembukaan Sampul)', route: '/internal/esourcing/bid-opening-ceremony', requiredPermission: 'eproc.internal.esourcing.open_envelopes', allowedRoles: ['POKJA_COMMITTEE'] },
      { code: 'INT-05-05', label: 'Kertas Kerja Evaluasi (Admin, Kualifikasi, Teknis, 2 Tahap)', route: '/internal/esourcing/technical-worksheets', requiredPermission: 'eproc.internal.esourcing.evaluate_tech', allowedRoles: ['POKJA_COMMITTEE'] },
      { code: 'INT-05-06', label: 'Kalkulator Evaluasi Harga, Kewajaran (<80% HPS), & HEA TKDN', route: '/internal/esourcing/commercial-hea-calculator', requiredPermission: 'eproc.internal.esourcing.calculate_hea', allowedRoles: ['POKJA_COMMITTEE'] },
      { code: 'INT-05-07', label: 'Kontrol Ruang e-Reverse Auction', route: '/internal/esourcing/eauction-control', requiredPermission: 'eproc.internal.esourcing.host_eauction', allowedRoles: ['POKJA_COMMITTEE'] },
      { code: 'INT-05-08', label: 'Ruang Klarifikasi & Negosiasi (BAFO)', route: '/internal/esourcing/bafo-negotiation', requiredPermission: 'eproc.internal.esourcing.negotiate_bafo', allowedRoles: ['POKJA_COMMITTEE', 'BUYER_PEJABAT_PENGADAAN'] },
      { code: 'INT-05-09', label: 'Usulan & Penetapan Pemenang (BAHP)', route: '/internal/esourcing/winner-determination', requiredPermission: 'eproc.internal.esourcing.winner_determination', allowedRoles: ['POKJA_COMMITTEE', 'PA_KPA'] },
      { code: 'INT-05-10', label: 'Penanganan Sanggah & Sanggah Banding', route: '/internal/esourcing/objections-sanggah', requiredPermission: 'eproc.internal.esourcing.resolve_sanggah', allowedRoles: ['POKJA_COMMITTEE', 'PA_KPA', 'LEGAL_OFFICER'] },
    ],
  },
  {
    id: 'INT-06',
    portal: 'INTERNAL_BUMN_PORTAL',
    order: 6,
    title: 'E-Catalog & Kontrak Payung',
    icon: 'eproc-icon-store',
    route: '/internal/ecatalog',
    requiredPermission: 'eproc.internal.ecatalog.umbrella',
    subMenus: [
      { code: 'INT-06-01', label: 'Manajemen Kontrak Payung (Umbrella Agreement)', route: '/internal/ecatalog/umbrella-agreements', requiredPermission: 'eproc.internal.ecatalog.umbrella', allowedRoles: ['BUYER_PEJABAT_PENGADAAN', 'PPK'] },
      { code: 'INT-06-02', label: 'Kurasi & Persetujuan Tayang Produk E-Katalog', route: '/internal/ecatalog/product-curation', requiredPermission: 'eproc.internal.ecatalog.curate_products', allowedRoles: ['BUYER_PEJABAT_PENGADAAN'] },
      { code: 'INT-06-03', label: 'Marketplace Internal (Call-Off Order & Mini-Competition)', route: '/internal/ecatalog/marketplace-calloff', requiredPermission: 'eproc.internal.ecatalog.call_off_order', allowedRoles: ['BUYER_PEJABAT_PENGADAAN', 'PPK'] },
    ],
  },
  {
    id: 'INT-07',
    portal: 'INTERNAL_BUMN_PORTAL',
    order: 7,
    title: 'Manajemen Kontrak (e-CLM)',
    icon: 'eproc-icon-file-check-2',
    route: '/internal/eclm',
    requiredPermission: 'eproc.internal.eclm.issue_sppbj',
    subMenus: [
      { code: 'INT-07-01', label: 'Penerbitan SPPBJ', route: '/internal/eclm/sppbj', requiredPermission: 'eproc.internal.eclm.issue_sppbj', allowedRoles: ['PPK'] },
      { code: 'INT-07-02', label: 'Contract Drafting, Template Clause, & Redlining Hukum', route: '/internal/eclm/contract-redlining', requiredPermission: 'eproc.internal.eclm.redline_contract', allowedRoles: ['PPK', 'LEGAL_OFFICER'] },
      { code: 'INT-07-03', label: 'Pembubuhan e-Meterai & Tanda Tangan Elektronik (TTE)', route: '/internal/eclm/digital-signature-tte', requiredPermission: 'eproc.internal.eclm.sign_tte', allowedRoles: ['PPK', 'PA_KPA'] },
      { code: 'INT-07-04', label: 'Verifikasi & Monitoring Bank Garansi / Surety Bond', route: '/internal/eclm/guarantee-bonds', requiredPermission: 'eproc.internal.eclm.verify_bonds', allowedRoles: ['PPK', 'FINANCE_AP'] },
      { code: 'INT-07-05', label: 'Penerbitan SPMK', route: '/internal/eclm/spmk', requiredPermission: 'eproc.internal.eclm.issue_spmk', allowedRoles: ['PPK'] },
      { code: 'INT-07-06', label: 'Pemrosesan Addendum / Contract Change Order (CCO)', route: '/internal/eclm/addendum-cco', requiredPermission: 'eproc.internal.eclm.process_cco', allowedRoles: ['PPK', 'LEGAL_OFFICER'] },
    ],
  },
  {
    id: 'INT-08',
    portal: 'INTERNAL_BUMN_PORTAL',
    order: 8,
    title: 'Monitoring Proyek & BAST',
    icon: 'eproc-icon-activity',
    route: '/internal/execution',
    requiredPermission: 'eproc.internal.execution.scurve_monitor',
    subMenus: [
      { code: 'INT-08-01', label: 'Monitoring Expediting Barang (ASN) & Inspeksi QC Gudang', route: '/internal/execution/asn-qc-inspection', requiredPermission: 'eproc.internal.execution.warehouse_gr', allowedRoles: ['WAREHOUSE_QC', 'PPK'] },
      { code: 'INT-08-02', label: 'Penerbitan Goods Receipt (GR) & BAPP', route: '/internal/execution/goods-receipt-gr', requiredPermission: 'eproc.internal.execution.warehouse_gr', allowedRoles: ['WAREHOUSE_QC', 'PPK'] },
      { code: 'INT-08-03', label: 'Monitoring Kurva S Konstruksi (Rencana vs Aktual Mingguan)', route: '/internal/execution/scurve-monitoring', requiredPermission: 'eproc.internal.execution.scurve_monitor', allowedRoles: ['PPK'] },
      { code: 'INT-08-04', label: 'Manajemen Kontrak Kritis & Show Cause Meeting (SCM I, II, III)', route: '/internal/execution/scm-critical', requiredPermission: 'eproc.internal.execution.issue_scm', allowedRoles: ['PPK', 'LEGAL_OFFICER'] },
      { code: 'INT-08-05', label: 'Verifikasi SLA Jasa & Penerbitan Service Entry Sheet (SES)', route: '/internal/execution/service-entry-ses', requiredPermission: 'eproc.internal.execution.approve_ses', allowedRoles: ['PPK'] },
      { code: 'INT-08-06', label: 'Penerbitan BAST (Barang 100%, PHO Konstruksi, Pemeliharaan, FHO)', route: '/internal/execution/bast-pho-fho', requiredPermission: 'eproc.internal.execution.issue_bast', allowedRoles: ['PPK'] },
      { code: 'INT-08-07', label: 'Verifikasi Post-Audit Realisasi TKDN & Kalkulasi Denda (1‰ / SLA)', route: '/internal/execution/post-audit-tkdn-penalties', requiredPermission: 'eproc.internal.execution.verify_post_tkdn', allowedRoles: ['PPK', 'INTERNAL_AUDITOR'] },
    ],
  },
  {
    id: 'INT-09',
    portal: 'INTERNAL_BUMN_PORTAL',
    order: 9,
    title: 'e-Invoicing, Pajak & Pembayaran',
    icon: 'eproc-icon-landmark',
    route: '/internal/finance',
    requiredPermission: 'eproc.internal.finance.three_way_match',
    subMenus: [
      { code: 'INT-09-01', label: 'Verifikasi 3-Way Matching (Kontrak vs GR/SES/BAST vs Invoice)', route: '/internal/finance/three-way-matching', requiredPermission: 'eproc.internal.finance.three_way_match', allowedRoles: ['FINANCE_AP', 'PPK'] },
      { code: 'INT-09-02', label: 'Verifikasi Faktur Pajak (WAPU BUMN) & e-Bupot PPh', route: '/internal/finance/tax-wapu-verification', requiredPermission: 'eproc.internal.finance.verify_tax_wapu', allowedRoles: ['TAX_OFFICER', 'FINANCE_AP'] },
      { code: 'INT-09-03', label: 'Penerbitan Berita Acara Pembayaran (BAP)', route: '/internal/finance/bap-minutes', requiredPermission: 'eproc.internal.finance.issue_bap', allowedRoles: ['PPK', 'FINANCE_AP'] },
      { code: 'INT-09-04', label: 'Integrasi Pencairan ERP (SAP MIRO / Payment Run)', route: '/internal/finance/erp-disbursement', requiredPermission: 'eproc.internal.finance.disburse_erp', allowedRoles: ['FINANCE_AP'] },
    ],
  },
  {
    id: 'INT-10',
    portal: 'INTERNAL_BUMN_PORTAL',
    order: 10,
    title: 'Evaluasi Kinerja Vendor (VPE)',
    icon: 'eproc-icon-star',
    route: '/internal/vpe',
    requiredPermission: 'eproc.internal.vpe.score_vendor',
    subMenus: [
      { code: 'INT-10-01', label: 'Penilaian Kinerja Kontrak Pasca-BAST (Mutu, Waktu, HSE, Layanan)', route: '/internal/vpe/score-evaluation', requiredPermission: 'eproc.internal.vpe.score_vendor', allowedRoles: ['PPK', 'HSE_CSMS_AUDITOR'] },
      { code: 'INT-10-02', label: 'Buku Besar (Ledger) Perubahan Skor & Grade Vendor', route: '/internal/vpe/rating-ledger', requiredPermission: 'eproc.internal.vpe.rating_ledger', allowedRoles: ['PPK', 'VMS_VERIFIER'] },
    ],
  },
  {
    id: 'INT-11',
    portal: 'INTERNAL_BUMN_PORTAL',
    order: 11,
    title: 'Master Data, Audit & Pengaturan',
    icon: 'eproc-icon-shield-check',
    route: '/internal/sysadmin',
    requiredPermission: 'eproc.internal.sysadmin.org_users_sod',
    subMenus: [
      { code: 'INT-11-01', label: 'Struktur Unit Organisasi, User, & Role (SoD Guard)', route: '/internal/sysadmin/org-users-roles', requiredPermission: 'eproc.internal.sysadmin.org_users_sod', allowedRoles: ['PA_KPA'] },
      { code: 'INT-11-02', label: 'Konfigurasi Matriks DOA (Delegation of Authority) & KIMAP', route: '/internal/sysadmin/doa-kimap-config', requiredPermission: 'eproc.internal.sysadmin.configure_doa', allowedRoles: ['PA_KPA'] },
      { code: 'INT-11-03', label: 'Immutable Audit Trail Viewer (Log Hash-Chain untuk SPI / BPK)', route: '/internal/sysadmin/immutable-audit-trail', requiredPermission: 'eproc.internal.sysadmin.audit_hash_chain', allowedRoles: ['INTERNAL_AUDITOR'] },
    ],
  },
];
