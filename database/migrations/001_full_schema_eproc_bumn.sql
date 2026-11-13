-- =============================================================================
-- ENTERPRISE BUMN e-PROCUREMENT (SOURCE-TO-PAY) DATABASE SCHEMA
-- Engine      : PostgreSQL 16+
-- Architecture: Modular DDD (Opsi B - Zero-ABP Prefix, Environment-Driven Client)
-- Regulation  : Permen BUMN No. PER-2/MBU/03/2023
-- Total Tables: 83 Tables across 9 Core Domains
-- =============================================================================

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================================================
-- DOMAIN 0: IAM, INTERNAL BUMN ORGANIZATION, DOA MATRIX & IMMUTABLE AUDIT TRAIL
-- =============================================================================

-- 1. Entitas Internal dalam Instalasi Server BUMN (Kantor Pusat / Anak Usaha / Regional)
CREATE TABLE company_entities (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    parent_entity_id UUID REFERENCES company_entities(id),
    entity_code VARCHAR(30) UNIQUE NOT NULL,
    entity_name VARCHAR(255) NOT NULL,
    entity_level VARCHAR(30) NOT NULL CHECK (entity_level IN ('HEADQUARTERS', 'SUB_HOLDING', 'SUBSIDIARY', 'AFFILIATE')),
    npwp VARCHAR(30) NOT NULL,
    is_wapu BOOLEAN NOT NULL DEFAULT TRUE,
    erp_company_code VARCHAR(20),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 2. Struktur Unit Kerja (Direktorat, Divisi, UKPBJ/Procurement Unit, Cabang/Site, Gudang)
CREATE TABLE org_units (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_entity_id UUID NOT NULL REFERENCES company_entities(id),
    parent_unit_id UUID REFERENCES org_units(id),
    unit_code VARCHAR(50) UNIQUE NOT NULL,
    unit_name VARCHAR(255) NOT NULL,
    unit_type VARCHAR(50) NOT NULL CHECK (unit_type IN ('DIRECTORATE', 'DIVISION', 'PROCUREMENT_UNIT', 'BRANCH', 'SITE', 'WAREHOUSE')),
    region_code VARCHAR(30),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 3. Cost Center & Profit Center (Sinkronisasi ERP SAP/Oracle)
CREATE TABLE cost_centers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_entity_id UUID NOT NULL REFERENCES company_entities(id),
    org_unit_id UUID NOT NULL REFERENCES org_units(id),
    cost_center_code VARCHAR(50) UNIQUE NOT NULL,
    cost_center_name VARCHAR(255) NOT NULL,
    erp_controlling_area VARCHAR(30),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- 4. Master Role (Segregation of Duties / SoD)
CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_code VARCHAR(50) UNIQUE NOT NULL,
    role_name VARCHAR(100) NOT NULL,
    role_category VARCHAR(30) NOT NULL CHECK (role_category IN ('INTERNAL', 'COMMITTEE', 'EXTERNAL_VENDOR', 'AUDITOR')),
    sod_conflict_roles VARCHAR(50)[] NOT NULL DEFAULT '{}',
    description TEXT
);

-- 5. Permissions Granular (eproc.*)
CREATE TABLE permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    perm_code VARCHAR(100) UNIQUE NOT NULL,
    module_name VARCHAR(50) NOT NULL,
    description TEXT
);

-- 6. Mapping Role Permission
CREATE TABLE role_permissions (
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

-- 7. Users (Pegawai Internal BUMN & Akun Eksternal Vendor)
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_entity_id UUID REFERENCES company_entities(id),
    vendor_id UUID,
    employee_nip VARCHAR(50),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    phone_number VARCHAR(30),
    job_title VARCHAR(150),
    position_grade VARCHAR(30),
    mfa_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    mfa_secret_encrypted TEXT,
    digital_cert_nik VARCHAR(20),
    public_key_pem TEXT,
    status VARCHAR(30) NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE', 'SUSPENDED', 'INACTIVE')),
    last_login_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 8. Penugasan Role User per Unit Kerja (Dilengkapi Nomor SK Penunjukan)
CREATE TABLE user_role_assignments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id),
    company_entity_id UUID REFERENCES company_entities(id),
    org_unit_id UUID REFERENCES org_units(id),
    sk_decree_number VARCHAR(100),
    valid_from DATE NOT NULL,
    valid_until DATE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 9. Delegation of Authority (DOA) Matrix Header
CREATE TABLE doa_matrices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_entity_id UUID NOT NULL REFERENCES company_entities(id),
    document_type VARCHAR(50) NOT NULL,
    procurement_category VARCHAR(30) NOT NULL DEFAULT 'ALL',
    budget_type VARCHAR(20) NOT NULL DEFAULT 'ALL',
    effective_from DATE NOT NULL,
    effective_until DATE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- 10. DOA Approval Tiers (Jenjang Persetujuan Berdasarkan Nilai Transaksi)
CREATE TABLE doa_approval_tiers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    doa_matrix_id UUID NOT NULL REFERENCES doa_matrices(id) ON DELETE CASCADE,
    min_amount NUMERIC(20, 2) NOT NULL,
    max_amount NUMERIC(20, 2),
    step_sequence INT NOT NULL,
    approver_role_code VARCHAR(50) NOT NULL,
    approver_position_grade VARCHAR(30),
    approval_mode VARCHAR(20) NOT NULL DEFAULT 'SINGLE' CHECK (approval_mode IN ('SINGLE', 'PARALLEL_ALL', 'QUORUM')),
    sla_hours INT NOT NULL DEFAULT 48
);

-- 11. Instance Workflow Persetujuan Aktif
CREATE TABLE approval_instances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    doa_matrix_id UUID NOT NULL REFERENCES doa_matrices(id),
    reference_table VARCHAR(100) NOT NULL,
    reference_id UUID NOT NULL,
    transaction_amount NUMERIC(20, 2) NOT NULL,
    current_step_sequence INT NOT NULL DEFAULT 1,
    overall_status VARCHAR(30) NOT NULL DEFAULT 'IN_PROGRESS' CHECK (overall_status IN ('IN_PROGRESS', 'APPROVED', 'REJECTED', 'RETURNED_FOR_REVISION')),
    requested_by UUID NOT NULL REFERENCES users(id),
    requested_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ
);

-- 12. Detail Log Langkah Persetujuan
CREATE TABLE approval_steps_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    approval_instance_id UUID NOT NULL REFERENCES approval_instances(id) ON DELETE CASCADE,
    step_sequence INT NOT NULL,
    assigned_user_id UUID REFERENCES users(id),
    acted_by_user_id UUID REFERENCES users(id),
    action VARCHAR(30) NOT NULL CHECK (action IN ('PENDING', 'APPROVED', 'REJECTED', 'REVISED', 'DELEGATED')),
    comments TEXT,
    digital_signature_hash TEXT,
    acted_at TIMESTAMPTZ,
    ip_address INET,
    user_agent TEXT
);

-- 13. Immutable Audit Trail (Hash-Chained untuk Forensik SPI / BPK / KPK)
CREATE TABLE audit_logs_hash_chain (
    id BIGSERIAL PRIMARY KEY,
    event_uuid UUID UNIQUE NOT NULL DEFAULT gen_random_uuid(),
    actor_user_id UUID,
    actor_role VARCHAR(50),
    event_module VARCHAR(50) NOT NULL,
    event_action VARCHAR(100) NOT NULL,
    entity_table VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    old_values JSONB,
    new_values JSONB,
    reason_notes TEXT,
    client_ip INET NOT NULL,
    user_agent TEXT,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    prev_record_hash CHAR(64) NOT NULL,
    current_record_hash CHAR(64) NOT NULL
);

-- =============================================================================
-- DOMAIN 1: VENDOR MANAGEMENT SYSTEM (VMS), FINANCIAL SCORING, CSMS & BLACKLIST
-- =============================================================================

-- 14. Master Profil Vendor / Penyedia
CREATE TABLE vendors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_code VARCHAR(30) UNIQUE NOT NULL,
    legal_entity_type VARCHAR(30) NOT NULL,
    company_name VARCHAR(255) NOT NULL,
    business_scale VARCHAR(30) NOT NULL CHECK (business_scale IN ('MIKRO', 'KECIL', 'MENENGAH', 'BESAR')),
    vendor_role_type VARCHAR(50)[] NOT NULL,
    is_bumn_affiliated BOOLEAN NOT NULL DEFAULT FALSE,
    is_padi_umkm BOOLEAN NOT NULL DEFAULT FALSE,
    nib_number VARCHAR(50) UNIQUE,
    npwp_number VARCHAR(30) UNIQUE NOT NULL,
    is_pkp BOOLEAN NOT NULL DEFAULT TRUE,
    pkp_sppkp_number VARCHAR(100),
    country_code CHAR(2) NOT NULL DEFAULT 'ID',
    province_name VARCHAR(100),
    city_name VARCHAR(100),
    head_office_address TEXT NOT NULL,
    postal_code VARCHAR(15),
    company_email VARCHAR(255) NOT NULL,
    company_phone VARCHAR(50),
    website_url VARCHAR(255),
    verification_status VARCHAR(30) NOT NULL DEFAULT 'DRAFT'
        CHECK (verification_status IN ('DRAFT', 'SUBMITTED', 'UNDER_REVIEW', 'SITE_VISIT_REQUIRED', 'VERIFIED_AVL', 'SUSPENDED', 'BLACKLISTED')),
    avl_approved_at TIMESTAMPTZ,
    avl_expired_at TIMESTAMPTZ,
    current_vpe_score NUMERIC(5, 2) NOT NULL DEFAULT 0,
    current_vpe_grade VARCHAR(20) NOT NULL DEFAULT 'UNRATED',
    erp_vendor_bp_code VARCHAR(30),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 15. Pemegang Saham & Beneficial Ownership (Deteksi Otomatis Afiliasi / Konflik Kepentingan)
CREATE TABLE vendor_shareholders_bo (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id) ON DELETE CASCADE,
    shareholder_type VARCHAR(20) NOT NULL CHECK (shareholder_type IN ('INDIVIDUAL', 'CORPORATE')),
    full_name VARCHAR(255) NOT NULL,
    identity_number_nik_npwp VARCHAR(50) NOT NULL,
    ownership_percentage NUMERIC(5, 2) NOT NULL,
    is_ultimate_beneficial_owner BOOLEAN NOT NULL DEFAULT FALSE,
    is_pep BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 16. Susunan Pengurus (Direksi & Komisaris)
CREATE TABLE vendor_board_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id) ON DELETE CASCADE,
    position_type VARCHAR(30) NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    nik_passport VARCHAR(50) NOT NULL,
    npwp_personal VARCHAR(30),
    ktp_file_url TEXT,
    is_authorized_signatory BOOLEAN NOT NULL DEFAULT FALSE,
    term_start_date DATE,
    term_end_date DATE
);

-- 17. Dokumen Legalitas & Perizinan Vendor
CREATE TABLE vendor_legal_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id) ON DELETE CASCADE,
    doc_type VARCHAR(50) NOT NULL,
    doc_number VARCHAR(150) NOT NULL,
    issued_by VARCHAR(150),
    issued_date DATE NOT NULL,
    expired_date DATE,
    file_storage_url TEXT NOT NULL,
    file_sha256_hash CHAR(64) NOT NULL,
    verification_status VARCHAR(30) NOT NULL DEFAULT 'PENDING' CHECK (verification_status IN ('PENDING', 'VALID', 'REJECTED', 'EXPIRED')),
    verified_by UUID REFERENCES users(id),
    verified_at TIMESTAMPTZ,
    rejection_notes TEXT
);

-- 18. Master Pohon Komoditas & KBLI
CREATE TABLE commodity_classifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    parent_id UUID REFERENCES commodity_classifications(id),
    kbli_code VARCHAR(15),
    unspsc_code VARCHAR(20),
    commodity_code VARCHAR(30) UNIQUE NOT NULL,
    commodity_name VARCHAR(255) NOT NULL,
    procurement_category VARCHAR(30) NOT NULL CHECK (procurement_category IN ('GOODS', 'CONSTRUCTION', 'CONSULTING', 'OTHER_SERVICES')),
    default_csms_risk_level VARCHAR(20) NOT NULL DEFAULT 'LOW' CHECK (default_csms_risk_level IN ('LOW', 'MEDIUM', 'HIGH', 'EXTREME'))
);

-- 19. Pemetaan Kualifikasi Komoditas / KBLI Vendor & Kemampuan Dasar (KD)
CREATE TABLE vendor_commodity_mappings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id) ON DELETE CASCADE,
    commodity_id UUID NOT NULL REFERENCES commodity_classifications(id),
    sub_qualification VARCHAR(30),
    kemampuan_dasar_kd NUMERIC(20, 2) NOT NULL DEFAULT 0,
    supporting_doc_id UUID REFERENCES vendor_legal_documents(id),
    is_verified BOOLEAN NOT NULL DEFAULT FALSE,
    UNIQUE(vendor_id, commodity_id)
);

-- 20. Laporan Keuangan Audited & Perhitungan Kapasitas Finansial (SKN & Altman Z-Score)
CREATE TABLE vendor_financial_statements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id) ON DELETE CASCADE,
    fiscal_year INT NOT NULL,
    audit_status VARCHAR(30) NOT NULL CHECK (audit_status IN ('AUDITED_KAP', 'UNAUDITED_INTERNAL')),
    kap_auditor_name VARCHAR(255),
    audit_opinion VARCHAR(50),
    currency CHAR(3) NOT NULL DEFAULT 'IDR',
    current_assets NUMERIC(20, 2) NOT NULL,
    total_assets NUMERIC(20, 2) NOT NULL,
    current_liabilities NUMERIC(20, 2) NOT NULL,
    total_liabilities NUMERIC(20, 2) NOT NULL,
    net_worth_equity NUMERIC(20, 2) NOT NULL,
    retained_earnings NUMERIC(20, 2) NOT NULL,
    ebit NUMERIC(20, 2) NOT NULL,
    annual_revenue NUMERIC(20, 2) NOT NULL,
    current_ratio NUMERIC(10, 4) GENERATED ALWAYS AS (
        CASE WHEN current_liabilities > 0 THEN current_assets / current_liabilities ELSE 0 END
    ) STORED,
    debt_to_equity_ratio NUMERIC(10, 4) GENERATED ALWAYS AS (
        CASE WHEN net_worth_equity > 0 THEN total_liabilities / net_worth_equity ELSE 0 END
    ) STORED,
    altman_z_score NUMERIC(10, 4),
    modal_kerja_mk NUMERIC(20, 2),
    kemampuan_nyata_kn NUMERIC(20, 2),
    verified_status VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    file_url TEXT NOT NULL,
    UNIQUE(vendor_id, fiscal_year)
);

-- 21. Daftar Pengalaman Kerja Vendor (Untuk NPt, KD, dan SKP/SKN)
CREATE TABLE vendor_experiences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id) ON DELETE CASCADE,
    commodity_id UUID REFERENCES commodity_classifications(id),
    project_name VARCHAR(255) NOT NULL,
    client_name VARCHAR(255) NOT NULL,
    client_category VARCHAR(30) NOT NULL CHECK (client_category IN ('GOVERNMENT', 'BUMN', 'PRIVATE')),
    contract_number VARCHAR(100) NOT NULL,
    contract_date DATE NOT NULL,
    contract_value NUMERIC(20, 2) NOT NULL,
    nk_sharing_percentage NUMERIC(5, 2) NOT NULL DEFAULT 100.00,
    effective_experience_value NUMERIC(20, 2) NOT NULL,
    start_date DATE NOT NULL,
    completion_date_bast DATE,
    is_ongoing BOOLEAN NOT NULL DEFAULT FALSE,
    bast_number VARCHAR(100),
    contract_file_url TEXT NOT NULL,
    bast_file_url TEXT,
    verification_status VARCHAR(30) NOT NULL DEFAULT 'PENDING'
);

-- 22. Daftar Tenaga Ahli / Personel Inti Vendor
CREATE TABLE vendor_experts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id) ON DELETE CASCADE,
    full_name VARCHAR(255) NOT NULL,
    nik VARCHAR(30) NOT NULL,
    npwp VARCHAR(30),
    employment_status VARCHAR(30) NOT NULL CHECK (employment_status IN ('PERMANENT', 'CONTRACT_PROJECT')),
    education_degree VARCHAR(30) NOT NULL,
    major_field VARCHAR(150),
    years_of_experience INT NOT NULL,
    certification_type VARCHAR(50),
    certificate_number VARCHAR(100),
    certificate_valid_until DATE,
    cv_file_url TEXT,
    certificate_file_url TEXT,
    bpjs_tk_number VARCHAR(50)
);

-- 23. Daftar Peralatan Utama Vendor
CREATE TABLE vendor_equipments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id) ON DELETE CASCADE,
    equipment_name VARCHAR(255) NOT NULL,
    brand_type VARCHAR(150),
    capacity_spec VARCHAR(150),
    manufacturing_year INT,
    quantity INT NOT NULL DEFAULT 1,
    condition_status VARCHAR(30) NOT NULL,
    ownership_status VARCHAR(30) NOT NULL CHECK (ownership_status IN ('MILIK_SENDIRI', 'SEWA_BELI_LEASING', 'SEWA')),
    current_location VARCHAR(255),
    proof_of_ownership_url TEXT NOT NULL,
    silo_sia_license_until DATE
);

-- 24. Rekening Bank Vendor
CREATE TABLE vendor_bank_accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id) ON DELETE CASCADE,
    bank_code VARCHAR(20) NOT NULL,
    bank_name VARCHAR(150) NOT NULL,
    branch_name VARCHAR(150),
    account_number VARCHAR(50) NOT NULL,
    account_holder_name VARCHAR(255) NOT NULL,
    currency CHAR(3) NOT NULL DEFAULT 'IDR',
    reference_letter_url TEXT NOT NULL,
    is_primary BOOLEAN NOT NULL DEFAULT FALSE,
    verification_status VARCHAR(30) NOT NULL DEFAULT 'PENDING'
);

-- 25. CSMS (Contractor Safety Management System) Submissions & Field Audits
CREATE TABLE csms_submissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    submission_number VARCHAR(50) UNIQUE NOT NULL,
    target_risk_category VARCHAR(20) NOT NULL CHECK (target_risk_category IN ('LOW', 'MEDIUM', 'HIGH', 'EXTREME')),
    doc_evaluation_score NUMERIC(5, 2),
    field_verification_score NUMERIC(5, 2),
    final_csms_score NUMERIC(5, 2),
    qualified_risk_level VARCHAR(20) CHECK (qualified_risk_level IN ('LOW', 'MEDIUM', 'HIGH', 'EXTREME')),
    status VARCHAR(30) NOT NULL DEFAULT 'SUBMITTED',
    audited_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 26. Sertifikat CSMS Aktif
CREATE TABLE csms_certificates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    csms_submission_id UUID NOT NULL REFERENCES csms_submissions(id),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    certificate_number VARCHAR(100) UNIQUE NOT NULL,
    qualified_risk_level VARCHAR(20) NOT NULL CHECK (qualified_risk_level IN ('LOW', 'MEDIUM', 'HIGH', 'EXTREME')),
    final_score NUMERIC(5, 2) NOT NULL,
    issued_date DATE NOT NULL,
    expired_date DATE NOT NULL,
    certificate_file_url TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- 27. Manajemen Sanksi & Daftar Hitam (Blacklist)
CREATE TABLE vendor_sanctions_blacklists (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    sanction_level VARCHAR(30) NOT NULL CHECK (sanction_level IN ('SP1', 'SP2', 'SP3', 'SUSPEND', 'BLACKLIST')),
    violation_category VARCHAR(50) NOT NULL,
    reference_tender_id UUID,
    reference_contract_id UUID,
    sk_blacklist_number VARCHAR(100) NOT NULL,
    effective_from DATE NOT NULL,
    effective_until DATE NOT NULL,
    penalty_points INT NOT NULL,
    notes TEXT NOT NULL,
    appeal_status VARCHAR(30) NOT NULL DEFAULT 'NONE',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- DOMAIN 2: DEMAND PLANNING (RKAP/DRP), CONSOLIDATED PR & ENCRYPTED HPS/OE
-- =============================================================================

-- 28. Pagu Anggaran RKAP (Capex / Opex)
CREATE TABLE rkap_budgets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_entity_id UUID NOT NULL REFERENCES company_entities(id),
    cost_center_id UUID NOT NULL REFERENCES cost_centers(id),
    fiscal_year INT NOT NULL,
    budget_type VARCHAR(20) NOT NULL CHECK (budget_type IN ('CAPEX', 'OPEX')),
    wbs_element_gl_account VARCHAR(50) NOT NULL,
    budget_name VARCHAR(255) NOT NULL,
    initial_amount NUMERIC(20, 2) NOT NULL,
    revised_amount NUMERIC(20, 2) NOT NULL,
    committed_amount NUMERIC(20, 2) NOT NULL DEFAULT 0,
    actual_spent_amount NUMERIC(20, 2) NOT NULL DEFAULT 0,
    available_amount NUMERIC(20, 2) GENERATED ALWAYS AS (
        revised_amount - committed_amount - actual_spent_amount
    ) STORED,
    currency CHAR(3) NOT NULL DEFAULT 'IDR',
    UNIQUE(company_entity_id, fiscal_year, wbs_element_gl_account)
);

-- 29. DRP (Daftar Rencana Pengadaan Tahunan)
CREATE TABLE drp_plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_entity_id UUID NOT NULL REFERENCES company_entities(id),
    rkap_budget_id UUID NOT NULL REFERENCES rkap_budgets(id),
    drp_code VARCHAR(50) UNIQUE NOT NULL,
    package_title VARCHAR(255) NOT NULL,
    procurement_category VARCHAR(30) NOT NULL,
    planned_method VARCHAR(50) NOT NULL,
    pagu_amount NUMERIC(20, 2) NOT NULL,
    min_tkdn_target_pct NUMERIC(5, 2) NOT NULL DEFAULT 25.00,
    is_umkm_reserved BOOLEAN NOT NULL DEFAULT FALSE,
    planned_announce_quarter INT NOT NULL CHECK (planned_announce_quarter BETWEEN 1 AND 4),
    approval_status VARCHAR(30) NOT NULL DEFAULT 'DRAFT',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 30. Master Material & Service Catalog + Historical Price Intelligence
CREATE TABLE master_catalog_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    commodity_id UUID NOT NULL REFERENCES commodity_classifications(id),
    material_service_code VARCHAR(50) UNIQUE NOT NULL,
    item_name VARCHAR(255) NOT NULL,
    item_type VARCHAR(20) NOT NULL CHECK (item_type IN ('MATERIAL', 'SERVICE', 'ASSET')),
    technical_specification TEXT,
    uom VARCHAR(20) NOT NULL,
    last_po_unit_price NUMERIC(20, 2),
    moving_avg_price NUMERIC(20, 2),
    benchmark_market_price NUMERIC(20, 2),
    is_pdn_mandatory BOOLEAN NOT NULL DEFAULT FALSE
);

-- 31. Purchase Requisition (PR)
CREATE TABLE purchase_requisitions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_entity_id UUID NOT NULL REFERENCES company_entities(id),
    org_unit_id UUID NOT NULL REFERENCES org_units(id),
    rkap_budget_id UUID NOT NULL REFERENCES rkap_budgets(id),
    drp_plan_id UUID REFERENCES drp_plans(id),
    pr_number VARCHAR(50) UNIQUE NOT NULL,
    erp_pr_number VARCHAR(50),
    pr_title VARCHAR(255) NOT NULL,
    requester_user_id UUID NOT NULL REFERENCES users(id),
    ppk_user_id UUID NOT NULL REFERENCES users(id),
    procurement_category VARCHAR(30) NOT NULL,
    total_estimated_value NUMERIC(20, 2) NOT NULL,
    required_delivery_date DATE NOT NULL,
    kak_tor_file_url TEXT NOT NULL,
    risk_assessment_csms_level VARCHAR(20) NOT NULL DEFAULT 'LOW',
    status VARCHAR(30) NOT NULL DEFAULT 'DRAFT',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 32. Item Detail Purchase Requisition
CREATE TABLE pr_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pr_id UUID NOT NULL REFERENCES purchase_requisitions(id) ON DELETE CASCADE,
    catalog_item_id UUID REFERENCES master_catalog_items(id),
    line_number INT NOT NULL,
    item_description VARCHAR(500) NOT NULL,
    technical_spec JSONB,
    quantity NUMERIC(18, 4) NOT NULL,
    uom VARCHAR(20) NOT NULL,
    estimated_unit_price NUMERIC(20, 2) NOT NULL,
    total_estimated_price NUMERIC(20, 2) NOT NULL,
    delivery_plant_unit_id UUID REFERENCES org_units(id),
    consolidated_package_id UUID
);

-- 33. Paket Pengadaan (Hasil Konsolidasi 1 atau Beberapa PR)
CREATE TABLE procurement_packages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_entity_id UUID NOT NULL REFERENCES company_entities(id),
    package_code VARCHAR(50) UNIQUE NOT NULL,
    package_name VARCHAR(255) NOT NULL,
    ppk_user_id UUID NOT NULL REFERENCES users(id),
    assigned_ukpbj_unit_id UUID REFERENCES org_units(id),
    procurement_category VARCHAR(30) NOT NULL CHECK (procurement_category IN ('GOODS', 'CONSTRUCTION', 'CONSULTING', 'OTHER_SERVICES')),
    sourcing_method VARCHAR(50) NOT NULL
        CHECK (sourcing_method IN ('TENDER_UMUM', 'TENDER_TERBATAS', 'SELEKSI_UMUM', 'BEAUTY_CONTEST', 'PENUNJUKAN_LANGSUNG', 'PENGADAAN_LANGSUNG', 'E_PURCHASING')),
    qualification_method VARCHAR(30) NOT NULL CHECK (qualification_method IN ('PASCAKUALIFIKASI', 'PRAKUALIFIKASI')),
    submission_envelope_method VARCHAR(30) NOT NULL CHECK (submission_envelope_method IN ('1_SAMPUL', '2_SAMPUL', '2_TAHAP')),
    evaluation_method VARCHAR(50) NOT NULL,
    contract_type VARCHAR(50) NOT NULL,
    total_pagu_budget NUMERIC(20, 2) NOT NULL,
    currency CHAR(3) NOT NULL DEFAULT 'IDR',
    required_csms_level VARCHAR(20) NOT NULL DEFAULT 'LOW',
    min_tkdn_requirement_pct NUMERIC(5, 2) NOT NULL DEFAULT 0.00,
    tkdn_category VARCHAR(30) NOT NULL DEFAULT 'NON_MANDATORY',
    allow_consortium_kso BOOLEAN NOT NULL DEFAULT FALSE,
    enable_e_reverse_auction BOOLEAN NOT NULL DEFAULT FALSE,
    bid_bond_required BOOLEAN NOT NULL DEFAULT FALSE,
    bid_bond_percentage NUMERIC(5, 2) NOT NULL DEFAULT 1.00,
    draft_contract_file_url TEXT,
    status VARCHAR(30) NOT NULL DEFAULT 'PREPARATION',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 34. Struktur BoQ Paket (Hierarkis)
CREATE TABLE package_boq_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    package_id UUID NOT NULL REFERENCES procurement_packages(id) ON DELETE CASCADE,
    parent_boq_id UUID REFERENCES package_boq_items(id),
    pr_item_id UUID REFERENCES pr_items(id),
    wbs_code VARCHAR(50) NOT NULL,
    item_type VARCHAR(30) NOT NULL,
    item_name VARCHAR(500) NOT NULL,
    specification_detail TEXT,
    quantity NUMERIC(18, 4),
    uom VARCHAR(20),
    is_lump_sum_item BOOLEAN NOT NULL DEFAULT FALSE,
    sort_order INT NOT NULL
);

-- 35. Analisa Harga Satuan Pekerjaan (AHSP Konstruksi)
CREATE TABLE package_ahsp_breakdowns (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    boq_item_id UUID NOT NULL REFERENCES package_boq_items(id) ON DELETE CASCADE,
    component_category VARCHAR(30) NOT NULL CHECK (component_category IN ('TENAGA_KERJA', 'BAHAN_MATERIAL', 'PERALATAN')),
    component_code VARCHAR(50),
    component_name VARCHAR(255) NOT NULL,
    coefficient NUMERIC(14, 6) NOT NULL,
    uom VARCHAR(20) NOT NULL,
    encrypted_unit_price BYTEA NOT NULL
);

-- 36. Owner Estimate / HPS Terenkripsi (Envelope Encryption)
CREATE TABLE owner_estimates_encrypted (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    package_id UUID UNIQUE NOT NULL REFERENCES procurement_packages(id) ON DELETE CASCADE,
    encrypted_total_hps BYTEA NOT NULL,
    encrypted_boq_prices_json BYTEA NOT NULL,
    encryption_iv BYTEA NOT NULL,
    wrapped_dek_ppk BYTEA NOT NULL,
    wrapped_dek_committee BYTEA NOT NULL,
    hps_ciphertext_sha256 CHAR(64) NOT NULL,
    plaintext_total_hps NUMERIC(20, 2),
    cost_history_basis_notes TEXT,
    approved_by_ppk_at TIMESTAMPTZ NOT NULL,
    decrypted_at TIMESTAMPTZ
);

-- 37. Kriteria Evaluasi Paket (Admin, Kualifikasi, Teknis, Harga)
CREATE TABLE package_evaluation_criteria (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    package_id UUID NOT NULL REFERENCES procurement_packages(id) ON DELETE CASCADE,
    parent_criteria_id UUID REFERENCES package_evaluation_criteria(id),
    envelope_stage VARCHAR(30) NOT NULL CHECK (envelope_stage IN ('QUALIFICATION', 'ADMINISTRATION', 'TECHNICAL', 'COMMERCIAL_PRICE')),
    criteria_name VARCHAR(255) NOT NULL,
    scoring_type VARCHAR(30) NOT NULL CHECK (scoring_type IN ('PASS_FAIL_GUGUR', 'NUMERIC_SCORE_0_100', 'AUTO_FORMULA')),
    weight_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0,
    passing_grade NUMERIC(5, 2) NOT NULL DEFAULT 70.00,
    evaluation_guideline TEXT
);

-- =============================================================================
-- DOMAIN 3: MULTI-METHOD E-SOURCING, TENDER EXECUTION & ENCRYPTED BID VAULT
-- =============================================================================

-- 38. Header Pelaksanaan Tender / RFx
CREATE TABLE tenders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    package_id UUID NOT NULL REFERENCES procurement_packages(id),
    company_entity_id UUID NOT NULL REFERENCES company_entities(id),
    tender_number VARCHAR(50) UNIQUE NOT NULL,
    retender_sequence INT NOT NULL DEFAULT 0,
    tender_title VARCHAR(255) NOT NULL,
    sourcing_method VARCHAR(50) NOT NULL,
    submission_envelope_method VARCHAR(30) NOT NULL,
    evaluation_method VARCHAR(50) NOT NULL,
    technical_weight_pct NUMERIC(5, 2) NOT NULL DEFAULT 0,
    commercial_weight_pct NUMERIC(5, 2) NOT NULL DEFAULT 100,
    rfp_dokumen_pemilihan_url TEXT NOT NULL,
    current_stage VARCHAR(50) NOT NULL DEFAULT 'ANNOUNCEMENT',
    status VARCHAR(30) NOT NULL DEFAULT 'ACTIVE',
    published_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 39. Susunan Anggota Pokja Pemilihan / Panitia Pengadaan
CREATE TABLE tender_committee_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tender_id UUID NOT NULL REFERENCES tenders(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id),
    committee_role VARCHAR(30) NOT NULL CHECK (committee_role IN ('CHAIRMAN', 'SECRETARY', 'MEMBER_TECHNICAL', 'MEMBER_COMMERCIAL')),
    has_conflict_free_pact_signed BOOLEAN NOT NULL DEFAULT FALSE,
    signed_pact_at TIMESTAMPTZ,
    UNIQUE(tender_id, user_id)
);

-- 40. Jadwal Tahapan Tender
CREATE TABLE tender_schedules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tender_id UUID NOT NULL REFERENCES tenders(id) ON DELETE CASCADE,
    stage_code VARCHAR(50) NOT NULL,
    stage_name VARCHAR(150) NOT NULL,
    sequence_order INT NOT NULL,
    start_at TIMESTAMPTZ NOT NULL,
    end_at TIMESTAMPTZ NOT NULL,
    revision_count INT NOT NULL DEFAULT 0
);

-- 41. Log Perubahan Jadwal Tender
CREATE TABLE tender_schedule_changes_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    schedule_id UUID NOT NULL REFERENCES tender_schedules(id) ON DELETE CASCADE,
    old_start_at TIMESTAMPTZ NOT NULL,
    old_end_at TIMESTAMPTZ NOT NULL,
    new_start_at TIMESTAMPTZ NOT NULL,
    new_end_at TIMESTAMPTZ NOT NULL,
    change_reason TEXT NOT NULL,
    changed_by UUID NOT NULL REFERENCES users(id),
    changed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 42. Kepesertaan Vendor pada Tender
CREATE TABLE tender_participants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tender_id UUID NOT NULL REFERENCES tenders(id) ON DELETE CASCADE,
    lead_vendor_id UUID NOT NULL REFERENCES vendors(id),
    is_invited_shortlist BOOLEAN NOT NULL DEFAULT FALSE,
    is_consortium_kso BOOLEAN NOT NULL DEFAULT FALSE,
    kso_agreement_file_url TEXT,
    registered_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    pakta_integritas_signed BOOLEAN NOT NULL DEFAULT TRUE,
    prequalification_status VARCHAR(30) NOT NULL DEFAULT 'PASSED',
    disqualification_stage VARCHAR(50),
    disqualification_reason TEXT,
    UNIQUE(tender_id, lead_vendor_id)
);

-- 43. Anggota Konsorsium / KSO (Joint Operation)
CREATE TABLE bid_consortium_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    participant_id UUID NOT NULL REFERENCES tender_participants(id) ON DELETE CASCADE,
    member_vendor_id UUID NOT NULL REFERENCES vendors(id),
    role_in_kso VARCHAR(20) NOT NULL CHECK (role_in_kso IN ('LEAD_FIRM', 'MEMBER_FIRM')),
    share_percentage NUMERIC(5, 2) NOT NULL
);

-- 44. Forum Aanwijzing
CREATE TABLE aanwijzing_questions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tender_id UUID NOT NULL REFERENCES tenders(id) ON DELETE CASCADE,
    participant_id UUID NOT NULL REFERENCES tender_participants(id),
    document_section_ref VARCHAR(150),
    question_text TEXT NOT NULL,
    attachment_url TEXT,
    asked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    answer_text TEXT,
    answer_attachment_url TEXT,
    answered_by UUID REFERENCES users(id),
    answered_at TIMESTAMPTZ
);

-- 45. Berita Acara Penjelasan (BAP Aanwijzing) & Addendum
CREATE TABLE aanwijzing_bap_addendums (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tender_id UUID NOT NULL REFERENCES tenders(id) ON DELETE CASCADE,
    bap_number VARCHAR(100) NOT NULL,
    addendum_version INT NOT NULL DEFAULT 1,
    summary_notes TEXT NOT NULL,
    addendum_rfp_file_url TEXT,
    published_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 46. Pemasukan Penawaran (Bid Submission Header)
CREATE TABLE bid_submissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tender_id UUID NOT NULL REFERENCES tenders(id),
    participant_id UUID UNIQUE NOT NULL REFERENCES tender_participants(id),
    submission_receipt_no VARCHAR(100) UNIQUE NOT NULL,
    bid_validity_days INT NOT NULL,
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_revised_at TIMESTAMPTZ,
    submission_ip INET NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'SUBMITTED_ENCRYPTED'
);

-- 47. Sampul Penawaran Terenkripsi (Sampul 1, Sampul 2, Tahap 2)
CREATE TABLE bid_envelopes_encrypted (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bid_submission_id UUID NOT NULL REFERENCES bid_submissions(id) ON DELETE CASCADE,
    envelope_type VARCHAR(30) NOT NULL CHECK (envelope_type IN ('ENVELOPE_1_ADMIN_TECH', 'ENVELOPE_2_COMMERCIAL', 'STAGE_2_EQUALIZED_PRICE')),
    encrypted_payload_blob BYTEA NOT NULL,
    encrypted_files_manifest JSONB NOT NULL,
    encryption_iv BYTEA NOT NULL,
    wrapped_dek_committee BYTEA NOT NULL,
    payload_sha256_hash CHAR(64) NOT NULL,
    is_opened BOOLEAN NOT NULL DEFAULT FALSE,
    opened_at TIMESTAMPTZ,
    opened_by_ceremony_id UUID,
    decrypted_bid_currency CHAR(3),
    decrypted_total_bid_price NUMERIC(20, 2),
    decrypted_corrected_price NUMERIC(20, 2),
    declared_combined_tkdn_pct NUMERIC(5, 2)
);

-- 48. Jaminan Penawaran (Bid Bond)
CREATE TABLE bid_bonds (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bid_submission_id UUID UNIQUE NOT NULL REFERENCES bid_submissions(id) ON DELETE CASCADE,
    issuer_type VARCHAR(30) NOT NULL CHECK (issuer_type IN ('BANK_GARANSI', 'SURETY_BOND_ASURANSI')),
    issuer_institution_name VARCHAR(255) NOT NULL,
    bond_number VARCHAR(100) NOT NULL,
    bond_amount NUMERIC(20, 2) NOT NULL,
    valid_from DATE NOT NULL,
    valid_until DATE NOT NULL,
    encrypted_scan_url TEXT NOT NULL,
    authenticity_verified VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    verified_by UUID REFERENCES users(id)
);

-- =============================================================================
-- DOMAIN 4: EVALUATION ENGINE, TKDN/HEA CALCULATOR, E-AUCTION, BAFO & SANGGAH
-- =============================================================================

-- 49. Kertas Kerja Evaluasi Peserta
CREATE TABLE evaluation_worksheets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tender_id UUID NOT NULL REFERENCES tenders(id),
    participant_id UUID NOT NULL REFERENCES tender_participants(id),
    admin_passed BOOLEAN,
    admin_notes TEXT,
    qualification_passed BOOLEAN,
    skn_calculated_value NUMERIC(20, 2),
    kd_calculated_value NUMERIC(20, 2),
    csms_verified_passed BOOLEAN,
    technical_passed BOOLEAN,
    technical_raw_score NUMERIC(6, 2),
    technical_weighted_score NUMERIC(6, 2),
    equalization_adjustments_json JSONB,
    evaluated_by UUID REFERENCES users(id),
    evaluated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(tender_id, participant_id)
);

-- 50. Detail Skor Evaluasi per Kriteria & per Anggota Pokja
CREATE TABLE evaluation_criteria_scores (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    worksheet_id UUID NOT NULL REFERENCES evaluation_worksheets(id) ON DELETE CASCADE,
    criteria_id UUID NOT NULL REFERENCES package_evaluation_criteria(id),
    committee_member_id UUID NOT NULL REFERENCES tender_committee_members(id),
    is_passed BOOLEAN,
    score_value NUMERIC(6, 2),
    evaluator_remarks TEXT,
    UNIQUE(worksheet_id, criteria_id, committee_member_id)
);

-- 51. Perhitungan Harga, Kewajaran (<80% HPS), Preferensi TKDN/BMP & HEA
CREATE TABLE evaluation_commercial_hea (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    worksheet_id UUID UNIQUE NOT NULL REFERENCES evaluation_worksheets(id) ON DELETE CASCADE,
    initial_bid_price_hp NUMERIC(20, 2) NOT NULL,
    arithmetic_corrected_hp NUMERIC(20, 2) NOT NULL,
    post_auction_or_bafo_hp NUMERIC(20, 2) NOT NULL,
    percentage_to_hps NUMERIC(6, 2) NOT NULL,
    is_above_hps BOOLEAN NOT NULL,
    is_below_80pct_hps BOOLEAN NOT NULL,
    price_fairness_passed BOOLEAN NOT NULL DEFAULT TRUE,
    verified_tkdn_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0,
    verified_bmp_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0,
    preference_rate_max NUMERIC(5, 4) NOT NULL DEFAULT 0.2500,
    koefisien_preferensi_kp NUMERIC(8, 6) NOT NULL DEFAULT 0,
    calculated_hea NUMERIC(20, 2) NOT NULL,
    commercial_score NUMERIC(6, 2),
    final_combined_score NUMERIC(6, 2),
    final_rank INT
);

-- 52. Rincian Penawaran Item BoQ & Sertifikat TKDN per Baris
CREATE TABLE bid_boq_item_details (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bid_submission_id UUID NOT NULL REFERENCES bid_submissions(id) ON DELETE CASCADE,
    package_boq_item_id UUID NOT NULL REFERENCES package_boq_items(id),
    offered_brand_model VARCHAR(255),
    country_of_origin CHAR(2) NOT NULL DEFAULT 'ID',
    offered_quantity NUMERIC(18, 4) NOT NULL,
    initial_unit_price NUMERIC(20, 2) NOT NULL,
    final_nego_unit_price NUMERIC(20, 2) NOT NULL,
    tkdn_certificate_number VARCHAR(100),
    tkdn_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0,
    is_pdn_local BOOLEAN NOT NULL DEFAULT TRUE
);

-- 53. Sesi e-Reverse Auction
CREATE TABLE eauction_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tender_id UUID NOT NULL REFERENCES tenders(id),
    session_number INT NOT NULL DEFAULT 1,
    visibility_mode VARCHAR(30) NOT NULL DEFAULT 'RANK_ONLY',
    min_decrement_amount NUMERIC(20, 2) NOT NULL,
    scheduled_start_at TIMESTAMPTZ NOT NULL,
    scheduled_end_at TIMESTAMPTZ NOT NULL,
    actual_end_at TIMESTAMPTZ NOT NULL,
    auto_extend_trigger_window_sec INT NOT NULL DEFAULT 180,
    auto_extend_duration_sec INT NOT NULL DEFAULT 300,
    max_extension_count INT NOT NULL DEFAULT 5,
    current_extension_count INT NOT NULL DEFAULT 0,
    status VARCHAR(30) NOT NULL DEFAULT 'SCHEDULED'
);

-- 54. Log Bid Real-Time dalam e-Reverse Auction
CREATE TABLE eauction_bids_realtime (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    eauction_session_id UUID NOT NULL REFERENCES eauction_sessions(id) ON DELETE CASCADE,
    participant_id UUID NOT NULL REFERENCES tender_participants(id),
    bid_sequence INT NOT NULL,
    submitted_total_price NUMERIC(20, 2) NOT NULL,
    recalculated_hea NUMERIC(20, 2) NOT NULL,
    triggered_auto_extend BOOLEAN NOT NULL DEFAULT FALSE,
    bid_timestamp TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    client_ip INET NOT NULL
);

-- 55. Riwayat Putaran Negosiasi & BAFO
CREATE TABLE negotiation_rounds_bafo (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tender_id UUID NOT NULL REFERENCES tenders(id),
    participant_id UUID NOT NULL REFERENCES tender_participants(id),
    round_number INT NOT NULL,
    is_bafo_final_round BOOLEAN NOT NULL DEFAULT FALSE,
    committee_target_price NUMERIC(20, 2),
    vendor_offered_price NUMERIC(20, 2) NOT NULL,
    negotiation_minutes_notes TEXT,
    ba_negosiasi_file_url TEXT,
    agreed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 56. Penetapan Pemenang & Cadangan (BAHP)
CREATE TABLE winner_determinations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tender_id UUID NOT NULL REFERENCES tenders(id),
    participant_id UUID NOT NULL REFERENCES tender_participants(id),
    winner_status VARCHAR(30) NOT NULL CHECK (winner_status IN ('WINNER_MAIN', 'RESERVE_1', 'RESERVE_2')),
    final_contract_price NUMERIC(20, 2) NOT NULL,
    final_hea_value NUMERIC(20, 2) NOT NULL,
    committed_tkdn_pct NUMERIC(5, 2) NOT NULL,
    cost_saving_to_hps NUMERIC(20, 2) NOT NULL,
    cost_saving_to_pagu NUMERIC(20, 2) NOT NULL,
    bahp_document_number VARCHAR(100) NOT NULL,
    bahp_file_url TEXT NOT NULL,
    doa_approval_instance_id UUID REFERENCES approval_instances(id),
    announced_at TIMESTAMPTZ
);

-- 57. Pengajuan Sanggah & Sanggah Banding
CREATE TABLE tender_objections_sanggah (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tender_id UUID NOT NULL REFERENCES tenders(id),
    participant_id UUID NOT NULL REFERENCES tender_participants(id),
    objection_level VARCHAR(30) NOT NULL CHECK (objection_level IN ('SANGGAH_AWAL', 'SANGGAH_BANDING')),
    objection_category VARCHAR(50) NOT NULL,
    objection_letter_no VARCHAR(100) NOT NULL,
    objection_details TEXT NOT NULL,
    evidence_file_url TEXT NOT NULL,
    sanggah_banding_bond_amount NUMERIC(20, 2),
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    response_letter_no VARCHAR(100),
    response_details TEXT,
    ruling_decision VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    responded_by UUID REFERENCES users(id),
    responded_at TIMESTAMPTZ
);

-- =============================================================================
-- DOMAIN 5: E-CATALOGUE, UMBRELLA AGREEMENT & CALL-OFF E-PURCHASING
-- =============================================================================

-- 58. Kontrak Payung / Outline Agreement
CREATE TABLE umbrella_agreements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_entity_id UUID NOT NULL REFERENCES company_entities(id),
    source_tender_id UUID REFERENCES tenders(id),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    agreement_number VARCHAR(100) UNIQUE NOT NULL,
    title VARCHAR(255) NOT NULL,
    valid_from DATE NOT NULL,
    valid_until DATE NOT NULL,
    ceiling_quota_amount NUMERIC(20, 2),
    consumed_amount NUMERIC(20, 2) NOT NULL DEFAULT 0,
    is_group_wide BOOLEAN NOT NULL DEFAULT TRUE,
    status VARCHAR(30) NOT NULL DEFAULT 'ACTIVE'
);

-- 59. Produk / Jasa di E-Katalog Internal
CREATE TABLE ecatalog_products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    umbrella_agreement_id UUID REFERENCES umbrella_agreements(id),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    catalog_item_id UUID REFERENCES master_catalog_items(id),
    sku_code VARCHAR(50) UNIQUE NOT NULL,
    product_name VARCHAR(255) NOT NULL,
    brand_name VARCHAR(150),
    specification_json JSONB NOT NULL,
    base_unit_price NUMERIC(20, 2) NOT NULL,
    currency CHAR(3) NOT NULL DEFAULT 'IDR',
    tkdn_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0,
    bmp_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0,
    lead_time_days INT NOT NULL,
    available_stock NUMERIC(18, 2),
    product_image_urls TEXT[],
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- 60. Tier Pricing (Harga Grosir Berjenjang)
CREATE TABLE ecatalog_tier_pricings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ecatalog_product_id UUID NOT NULL REFERENCES ecatalog_products(id) ON DELETE CASCADE,
    min_qty NUMERIC(18, 4) NOT NULL,
    max_qty NUMERIC(18, 4),
    tiered_unit_price NUMERIC(20, 2) NOT NULL
);

-- 61. Pemesanan E-Purchasing / Call-Off Order
CREATE TABLE call_off_orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_entity_id UUID NOT NULL REFERENCES company_entities(id),
    pr_id UUID NOT NULL REFERENCES purchase_requisitions(id),
    umbrella_agreement_id UUID REFERENCES umbrella_agreements(id),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    call_off_po_number VARCHAR(100) UNIQUE NOT NULL,
    purchasing_mode VARCHAR(30) NOT NULL CHECK (purchasing_mode IN ('DIRECT_CHECKOUT', 'NEGOTIATED_CHECKOUT', 'MINI_COMPETITION')),
    total_order_amount NUMERIC(20, 2) NOT NULL,
    shipping_cost NUMERIC(20, 2) NOT NULL DEFAULT 0,
    delivery_address TEXT NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'ORDERED',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- DOMAIN 6: CONTRACT LIFECYCLE MANAGEMENT (E-CLM), GUARANTEES & CCO/ADDENDUM
-- =============================================================================

-- 62. SPPBJ (Surat Penunjukan Penyedia Barang/Jasa)
CREATE TABLE sppbj_letters (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tender_id UUID NOT NULL REFERENCES tenders(id),
    winner_determination_id UUID NOT NULL REFERENCES winner_determinations(id),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    sppbj_number VARCHAR(100) UNIQUE NOT NULL,
    issued_date DATE NOT NULL,
    required_perf_bond_amount NUMERIC(20, 2) NOT NULL,
    perf_bond_submission_deadline DATE NOT NULL,
    sppbj_file_url TEXT NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'ISSUED'
);

-- 63. Master Kontrak / Surat Perjanjian / PO
CREATE TABLE contracts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_entity_id UUID NOT NULL REFERENCES company_entities(id),
    package_id UUID REFERENCES procurement_packages(id),
    sppbj_id UUID REFERENCES sppbj_letters(id),
    call_off_order_id UUID REFERENCES call_off_orders(id),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    ppk_user_id UUID NOT NULL REFERENCES users(id),
    contract_number VARCHAR(100) UNIQUE NOT NULL,
    erp_po_contract_number VARCHAR(50),
    contract_title VARCHAR(255) NOT NULL,
    contract_type VARCHAR(50) NOT NULL,
    initial_contract_value NUMERIC(20, 2) NOT NULL,
    current_contract_value NUMERIC(20, 2) NOT NULL,
    currency CHAR(3) NOT NULL DEFAULT 'IDR',
    committed_tkdn_pct NUMERIC(5, 2) NOT NULL DEFAULT 0,
    signed_date DATE,
    effective_start_date DATE NOT NULL,
    initial_end_date DATE NOT NULL,
    current_end_date DATE NOT NULL,
    maintenance_period_days INT NOT NULL DEFAULT 0,
    retention_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0,
    late_penalty_per_mille NUMERIC(5, 2) NOT NULL DEFAULT 1.00,
    late_penalty_basis VARCHAR(30) NOT NULL DEFAULT 'CONTRACT_TOTAL',
    max_penalty_percentage NUMERIC(5, 2) NOT NULL DEFAULT 5.00,
    emeterai_serial_number VARCHAR(100),
    signed_contract_file_url TEXT,
    status VARCHAR(30) NOT NULL DEFAULT 'DRAFTING',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 64. Riwayat Versi & Redlining Pasal Kontrak
CREATE TABLE contract_clauses_versions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id) ON DELETE CASCADE,
    version_number INT NOT NULL,
    clause_section VARCHAR(100) NOT NULL,
    content_html_diff TEXT NOT NULL,
    proposed_by_user_id UUID NOT NULL REFERENCES users(id),
    review_status VARCHAR(30) NOT NULL DEFAULT 'PROPOSED',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 65. Manajemen Jaminan Kontrak (Pelaksanaan, Uang Muka, Pemeliharaan)
CREATE TABLE contract_guarantees_bonds (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id) ON DELETE CASCADE,
    bond_type VARCHAR(40) NOT NULL CHECK (bond_type IN ('PERFORMANCE_BOND', 'ADVANCE_PAYMENT_BOND', 'MAINTENANCE_BOND')),
    issuer_institution VARCHAR(255) NOT NULL,
    bond_number VARCHAR(100) NOT NULL,
    bond_amount NUMERIC(20, 2) NOT NULL,
    valid_from DATE NOT NULL,
    valid_until DATE NOT NULL,
    bond_file_url TEXT NOT NULL,
    bank_confirmation_letter_url TEXT,
    status VARCHAR(30) NOT NULL DEFAULT 'ACTIVE'
);

-- 66. Skema Termin Pembayaran & Uang Muka
CREATE TABLE contract_payment_terms_termin (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id) ON DELETE CASCADE,
    termin_sequence INT NOT NULL,
    termin_type VARCHAR(30) NOT NULL,
    termin_name VARCHAR(150) NOT NULL,
    required_physical_progress_pct NUMERIC(5, 2) NOT NULL,
    payment_percentage NUMERIC(5, 2) NOT NULL,
    gross_amount NUMERIC(20, 2) NOT NULL,
    dp_recoupment_pct NUMERIC(5, 2) NOT NULL DEFAULT 0,
    retention_deduction_pct NUMERIC(5, 2) NOT NULL DEFAULT 0,
    status VARCHAR(30) NOT NULL DEFAULT 'UNBILLED'
);

-- 67. SPMK (Surat Perintah Mulai Kerja)
CREATE TABLE spmk_orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID UNIQUE NOT NULL REFERENCES contracts(id) ON DELETE CASCADE,
    spmk_number VARCHAR(100) UNIQUE NOT NULL,
    site_handover_ba_number VARCHAR(100),
    issued_date DATE NOT NULL,
    work_start_date DATE NOT NULL,
    spmk_file_url TEXT NOT NULL
);

-- 68. Contract Change Order (CCO) / Addendum Kontrak
CREATE TABLE contract_addendums_cco (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id) ON DELETE CASCADE,
    addendum_sequence INT NOT NULL,
    addendum_number VARCHAR(100) UNIQUE NOT NULL,
    cco_reason_category VARCHAR(50) NOT NULL,
    old_contract_value NUMERIC(20, 2) NOT NULL,
    new_contract_value NUMERIC(20, 2) NOT NULL,
    delta_percentage NUMERIC(6, 2) NOT NULL,
    old_end_date DATE NOT NULL,
    new_end_date DATE NOT NULL,
    technical_justification_ba_url TEXT NOT NULL,
    doa_approval_instance_id UUID REFERENCES approval_instances(id),
    signed_addendum_file_url TEXT,
    status VARCHAR(30) NOT NULL DEFAULT 'PROPOSED'
);

-- =============================================================================
-- DOMAIN 7: PROJECT EXECUTION, KURVA-S, SCM, BAST (GR/SES/PHO/FHO) & PENALTIES
-- =============================================================================

-- 69. Advance Shipping Notice (ASN)
CREATE TABLE asn_shipping_notices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id),
    asn_number VARCHAR(100) UNIQUE NOT NULL,
    surat_jalan_number VARCHAR(100) NOT NULL,
    expedition_carrier VARCHAR(150),
    tracking_awb_container_no VARCHAR(100),
    shipped_date DATE NOT NULL,
    estimated_arrival_date DATE NOT NULL,
    destination_warehouse_unit_id UUID REFERENCES org_units(id),
    packing_list_file_url TEXT,
    status VARCHAR(30) NOT NULL DEFAULT 'IN_TRANSIT'
);

-- 70. Pemeriksaan QC & Penerimaan Barang Gudang (Goods Receipt / GR)
CREATE TABLE goods_receipts_gr (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id),
    asn_id UUID REFERENCES asn_shipping_notices(id),
    gr_number VARCHAR(100) UNIQUE NOT NULL,
    erp_material_doc_gr VARCHAR(50),
    received_date DATE NOT NULL,
    inspected_by_user_id UUID NOT NULL REFERENCES users(id),
    qc_result_status VARCHAR(30) NOT NULL,
    accepted_items_json JSONB NOT NULL,
    total_gr_value NUMERIC(20, 2) NOT NULL,
    bapp_qc_file_url TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 71. Baseline Rencana Kurva S
CREATE TABLE project_scurve_baselines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id) ON DELETE CASCADE,
    baseline_version INT NOT NULL DEFAULT 1,
    week_number INT NOT NULL,
    week_start_date DATE NOT NULL,
    week_end_date DATE NOT NULL,
    planned_weekly_weight_pct NUMERIC(6, 3) NOT NULL,
    planned_cumulative_pct NUMERIC(6, 3) NOT NULL,
    UNIQUE(contract_id, baseline_version, week_number)
);

-- 72. Realisasi Progres Mingguan Kurva S & Deteksi Deviasi Kritis
CREATE TABLE project_scurve_actuals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id) ON DELETE CASCADE,
    week_number INT NOT NULL,
    report_date DATE NOT NULL,
    planned_cumulative_pct NUMERIC(6, 3) NOT NULL,
    actual_cumulative_pct NUMERIC(6, 3) NOT NULL,
    deviation_pct NUMERIC(6, 3) GENERATED ALWAYS AS (
        actual_cumulative_pct - planned_cumulative_pct
    ) STORED,
    is_critical_delay BOOLEAN NOT NULL DEFAULT FALSE,
    field_photos_geotag_urls TEXT[],
    consultant_supervision_verified BOOLEAN NOT NULL DEFAULT FALSE,
    verified_by_ppk_at TIMESTAMPTZ
);

-- 73. Show Cause Meeting (SCM Tingkat I, II, III)
CREATE TABLE show_cause_meetings_scm (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id),
    scurve_actual_id UUID REFERENCES project_scurve_actuals(id),
    scm_level VARCHAR(20) NOT NULL CHECK (scm_level IN ('SCM_LEVEL_1', 'SCM_LEVEL_2', 'SCM_LEVEL_3')),
    deviation_at_issuance_pct NUMERIC(6, 3) NOT NULL,
    meeting_date DATE NOT NULL,
    action_plan_test_days INT NOT NULL,
    target_catchup_pct NUMERIC(6, 3) NOT NULL,
    actual_catchup_pct NUMERIC(6, 3),
    outcome_status VARCHAR(30) NOT NULL DEFAULT 'ON_TRIAL',
    ba_scm_file_url TEXT NOT NULL
);

-- 74. Service Entry Sheet (SES) & Evaluasi Realisasi SLA
CREATE TABLE service_entry_sheets_ses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id),
    ses_number VARCHAR(100) UNIQUE NOT NULL,
    erp_ses_number VARCHAR(50),
    service_period_start DATE NOT NULL,
    service_period_end DATE NOT NULL,
    target_sla_percentage NUMERIC(5, 2) NOT NULL DEFAULT 99.00,
    achieved_sla_percentage NUMERIC(5, 2) NOT NULL,
    gross_service_value NUMERIC(20, 2) NOT NULL,
    sla_penalty_deduction NUMERIC(20, 2) NOT NULL DEFAULT 0,
    timesheet_deliverables_url TEXT NOT NULL,
    approved_by_user_id UUID REFERENCES users(id),
    approved_at TIMESTAMPTZ
);

-- 75. BAST (Parsial, 100%, PHO, dan FHO)
CREATE TABLE bast_handovers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id),
    termin_id UUID REFERENCES contract_payment_terms_termin(id),
    bast_type VARCHAR(30) NOT NULL
        CHECK (bast_type IN ('BAST_GOODS_PARTIAL', 'BAST_GOODS_100', 'BAST_SERVICE_TERMIN', 'BAST_CONSTRUCTION_PHO', 'BAST_CONSTRUCTION_FHO')),
    bast_number VARCHAR(100) UNIQUE NOT NULL,
    handover_date DATE NOT NULL,
    contract_due_date_ref DATE NOT NULL,
    delay_calendar_days INT GENERATED ALWAYS AS (
        GREATEST(0, handover_date - contract_due_date_ref)
    ) STORED,
    physical_progress_achieved_pct NUMERIC(5, 2) NOT NULL,
    handover_gross_value NUMERIC(20, 2) NOT NULL,
    maintenance_end_date_target DATE,
    signed_bast_file_url TEXT NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'SIGNED_VALID'
);

-- 76. Verifikasi Realisasi TKDN Akhir (Post-Audit TKDN)
CREATE TABLE tkdn_post_audit_verifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID UNIQUE NOT NULL REFERENCES contracts(id),
    committed_tkdn_pct NUMERIC(5, 2) NOT NULL,
    actual_verified_tkdn_pct NUMERIC(5, 2) NOT NULL,
    tkdn_shortfall_pct NUMERIC(5, 2) GENERATED ALWAYS AS (
        GREATEST(0, committed_tkdn_pct - actual_verified_tkdn_pct)
    ) STORED,
    verifying_agency VARCHAR(100),
    verification_report_no VARCHAR(100) NOT NULL,
    financial_sanction_amount NUMERIC(20, 2) NOT NULL DEFAULT 0,
    report_file_url TEXT NOT NULL
);

-- 77. Kalkulasi Denda Kontrak (1‰, Penalti SLA, & Sanksi TKDN)
CREATE TABLE penalty_calculations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id),
    bast_id UUID UNIQUE NOT NULL REFERENCES bast_handovers(id),
    delay_days INT NOT NULL DEFAULT 0,
    penalty_basis_amount NUMERIC(20, 2) NOT NULL,
    per_mille_rate NUMERIC(5, 2) NOT NULL DEFAULT 1.00,
    raw_delay_penalty NUMERIC(20, 2) NOT NULL,
    capped_delay_penalty NUMERIC(20, 2) NOT NULL,
    sla_breach_penalty NUMERIC(20, 2) NOT NULL DEFAULT 0,
    tkdn_shortfall_penalty NUMERIC(20, 2) NOT NULL DEFAULT 0,
    total_penalty_deduction NUMERIC(20, 2) NOT NULL
);

-- =============================================================================
-- DOMAIN 8: E-INVOICING, 3-WAY MATCHING, TAX WAPU & VENDOR PERFORMANCE (VPE)
-- =============================================================================

-- 78. Pengajuan e-Invoice & Hasil 3-Way Matching Otomatis
CREATE TABLE invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_entity_id UUID NOT NULL REFERENCES company_entities(id),
    contract_id UUID NOT NULL REFERENCES contracts(id),
    termin_id UUID REFERENCES contract_payment_terms_termin(id),
    bast_id UUID NOT NULL REFERENCES bast_handovers(id),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    vendor_invoice_number VARCHAR(100) NOT NULL,
    invoice_date DATE NOT NULL,
    dpp_gross_amount NUMERIC(20, 2) NOT NULL,
    dp_recoupment_deduction NUMERIC(20, 2) NOT NULL DEFAULT 0,
    retention_5pct_deduction NUMERIC(20, 2) NOT NULL DEFAULT 0,
    penalty_deduction_amount NUMERIC(20, 2) NOT NULL DEFAULT 0,
    dpp_net_after_deduction NUMERIC(20, 2) NOT NULL,
    ppn_rate_pct NUMERIC(5, 2) NOT NULL DEFAULT 11.00,
    ppn_amount NUMERIC(20, 2) NOT NULL,
    is_ppn_withheld_wapu BOOLEAN NOT NULL DEFAULT TRUE,
    pph_tax_type VARCHAR(30) NOT NULL,
    pph_rate_pct NUMERIC(5, 2) NOT NULL,
    pph_withheld_amount NUMERIC(20, 2) NOT NULL,
    net_payable_to_vendor NUMERIC(20, 2) NOT NULL,
    three_way_match_status VARCHAR(30) NOT NULL DEFAULT 'MATCHED',
    invoice_status VARCHAR(30) NOT NULL DEFAULT 'SUBMITTED',
    invoice_file_url TEXT NOT NULL,
    kwitansi_file_url TEXT NOT NULL,
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 79. Dokumen Pajak (Faktur Pajak e-Faktur & Bukti Potong e-Bupot)
CREATE TABLE tax_wapu_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID UNIQUE NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    faktur_pajak_nsfp VARCHAR(50) UNIQUE NOT NULL,
    faktur_pajak_date DATE NOT NULL,
    djp_qr_validation_url TEXT,
    ssp_ntpn_wapu_number VARCHAR(50),
    ebupot_pph_number VARCHAR(50),
    faktur_file_url TEXT NOT NULL,
    tax_verified_by UUID REFERENCES users(id),
    tax_verified_at TIMESTAMPTZ
);

-- 80. Berita Acara Pembayaran (BAP)
CREATE TABLE bap_payment_minutes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID UNIQUE NOT NULL REFERENCES invoices(id),
    bap_number VARCHAR(100) UNIQUE NOT NULL,
    bap_date DATE NOT NULL,
    doa_approval_instance_id UUID REFERENCES approval_instances(id),
    signed_bap_file_url TEXT NOT NULL
);

-- 81. Pencairan Pembayaran & Integrasi ERP
CREATE TABLE payment_disbursements_erp (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID UNIQUE NOT NULL REFERENCES invoices(id),
    bap_id UUID NOT NULL REFERENCES bap_payment_minutes(id),
    vendor_bank_account_id UUID NOT NULL REFERENCES vendor_bank_accounts(id),
    erp_miro_doc_number VARCHAR(50),
    erp_clearing_sp2d_number VARCHAR(50),
    disbursed_amount NUMERIC(20, 2) NOT NULL,
    bank_transfer_ref_no VARCHAR(100),
    paid_at TIMESTAMPTZ NOT NULL
);

-- 82. Evaluasi Kinerja Vendor (VPE) Pasca-BAST
CREATE TABLE vpe_evaluations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES contracts(id),
    bast_id UUID UNIQUE NOT NULL REFERENCES bast_handovers(id),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    quality_spec_score NUMERIC(5, 2) NOT NULL,
    delivery_timeliness_score NUMERIC(5, 2) NOT NULL,
    hse_csms_compliance_score NUMERIC(5, 2) NOT NULL,
    service_responsiveness_score NUMERIC(5, 2) NOT NULL,
    final_weighted_vpe_score NUMERIC(5, 2) NOT NULL,
    performance_category VARCHAR(20) NOT NULL,
    evaluator_notes TEXT,
    evaluated_by UUID NOT NULL REFERENCES users(id),
    evaluated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 83. Buku Besar Riwayat Perubahan Skor & Grade Vendor
CREATE TABLE vendor_rating_ledgers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id) ON DELETE CASCADE,
    vpe_evaluation_id UUID REFERENCES vpe_evaluations(id),
    sanction_id UUID REFERENCES vendor_sanctions_blacklists(id),
    previous_score NUMERIC(5, 2) NOT NULL,
    new_cumulative_score NUMERIC(5, 2) NOT NULL,
    previous_grade VARCHAR(20) NOT NULL,
    new_grade VARCHAR(20) NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- PERFORMANCE INDEXES
-- =============================================================================
CREATE INDEX idx_vendors_status_scale ON vendors(verification_status, business_scale);
CREATE INDEX idx_vendor_shareholders_nik ON vendor_shareholders_bo(identity_number_nik_npwp);
CREATE INDEX idx_vendor_legal_docs_expiry ON vendor_legal_documents(expired_date, verification_status);
CREATE INDEX idx_csms_certs_vendor_expiry ON csms_certificates(vendor_id, expired_date, is_active);
CREATE INDEX idx_tenders_stage_status ON tenders(current_stage, status);
CREATE INDEX idx_eauction_bids_session_hea ON eauction_bids_realtime(eauction_session_id, recalculated_hea ASC);
CREATE INDEX idx_contracts_status_dates ON contracts(status, current_end_date);
CREATE INDEX idx_guarantees_expiry ON contract_guarantees_bonds(valid_until, status);
CREATE INDEX idx_invoices_status_vendor ON invoices(invoice_status, vendor_id);
CREATE INDEX idx_audit_logs_entity ON audit_logs_hash_chain(entity_table, entity_id, occurred_at DESC);
