import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EprocConfigService } from './core/config/eproc-config.service';
import {
  EPROC_NAVIGATION_MENUS,
  EprocMainMenuItem,
  EprocPortalType,
} from './core/navigation/eproc-menu.config';

export interface VendorViewModel {
  id: string;
  vendorCode: string;
  legalEntityType: string;
  companyName: string;
  businessScale: 'KECIL' | 'MENENGAH' | 'BESAR';
  isBumnAffiliated: boolean;
  isPadiUmkm: boolean;
  nibNumber: string;
  npwpNumber: string;
  cityName: string;
  verificationStatus: 'SUBMITTED' | 'VERIFIED_AVL' | 'SUSPENDED' | 'BLACKLISTED';
  vpeGrade: string;
  netWorthEquityKb: number;
  highestExperienceNpt: number;
  kemampuanDasarKd: number;
  sisaKemampuanNyataSkn: number;
  sisaKemampuanPaketSkp: number;
  altmanZScore: number;
  financialHealthZone: 'SAFE_ZONE' | 'GREY_ZONE' | 'DISTRESS_ZONE';
  shareholderOwnerName: string;
  shareholderNik: string;
  csmsLevel: 'NONE' | 'LOW' | 'MEDIUM' | 'HIGH' | 'EXTREME';
  csmsScore: number;
  csmsCertNo?: string;
}

export interface RolePersonaItem {
  roleCode: string;
  roleName: string;
  portalTarget: EprocPortalType;
  positionGrade: string;
  sodConflicts: string[];
  grantedPermissions: string[];
}

export interface RkapBudgetViewModel {
  id: string;
  costCenterCode: string;
  costCenterName: string;
  budgetType: 'CAPEX' | 'OPEX';
  wbsElementGlAccount: string;
  budgetName: string;
  revisedAmount: number;
  committedAmount: number;
  actualSpentAmount: number;
  availableAmount: number;
}

export interface PrViewModel {
  id: string;
  prNumber: string;
  prTitle: string;
  rkapBudgetId: string;
  wbsCode: string;
  requesterUnitName: string;
  category: 'GOODS' | 'CONSTRUCTION' | 'CONSULTING' | 'OTHER_SERVICES';
  materialCode: string;
  itemName: string;
  quantity: number;
  uom: string;
  unitPrice: number;
  totalValue: number;
  csmsLevel: 'LOW' | 'MEDIUM' | 'HIGH' | 'EXTREME';
  status: 'APPROVED_READY_FOR_SOURCING' | 'CONSOLIDATED_TO_PACKAGE';
  selectedForConsolidation?: boolean;
}

export interface PackageViewModel {
  id: string;
  packageCode: string;
  packageName: string;
  category: string;
  sourcingMethod: string;
  envelopeMethod: string;
  evaluationMethod: string;
  totalPaguBudget: number;
  minTkdnPct: number;
  sourcePrNumbers: string[];
  consolidatedQty: number;
  uom: string;
  status: 'DRAFT_CONSOLIDATED' | 'HPS_SEALED_WAITING_DOA' | 'DOA_APPROVED_READY_FOR_TENDER';
  sealedHpsCipherPreview?: string;
  sealedHpsSha256?: string;
  doaInstanceId?: string;
}

export interface DoaStepViewModel {
  stepSequence: number;
  roleCode: string;
  positionGrade: string;
  action: 'PENDING' | 'APPROVED' | 'REJECTED';
  actedBy?: string;
  comments?: string;
}

export interface DoaInstanceViewModel {
  id: string;
  packageId: string;
  packageCode: string;
  packageTitle: string;
  transactionAmountIdr: number;
  currentStepSequence: number;
  totalStepsCount: number;
  overallStatus: 'IN_PROGRESS' | 'APPROVED' | 'REJECTED';
  steps: DoaStepViewModel[];
}

export interface AanwijzingItemViewModel {
  maskedAlias: string;
  sectionRef: string;
  question: string;
  answer: string;
}

export interface BidVaultItemViewModel {
  participantId: string;
  receiptNo: string;
  vendorName: string;
  businessScale: string;
  techScore: number;
  techPassed: boolean;
  cipherPreview: string;
  sha256Hash: string;
  hiddenPriceHp: number;
  hiddenTkdnPct: number;
  hiddenBmpPct: number;
  isOpened: boolean;
  decryptedPriceHp?: number;
  declaredTkdnPct?: number;
  kp?: number;
  calculatedHea?: number;
  combinedScore?: number;
  rank?: number;
}

export interface EAuctionBidLogViewModel {
  seq: number;
  vendorName: string;
  newPriceHp: number;
  newHea: number;
  triggeredAutoExtend: boolean;
  timestamp: string;
}

export interface SanggahItemViewModel {
  id: string;
  vendorName: string;
  letterNo: string;
  category: string;
  details: string;
  status: 'PENDING' | 'REJECTED_TENDER_CONTINUES' | 'ACCEPTED_REEVALUATION';
  responseNotes?: string;
}

export interface AuditHashItem {
  seq: number;
  timestamp: string;
  actorRole: string;
  module: string;
  action: string;
  entityRef: string;
  prevHash: string;
  currentHash: string;
}

export interface UmbrellaAgreementViewModel {
  id: string;
  agreementNumber: string;
  title: string;
  vendorName: string;
  validUntil: string;
  totalQuotaCeiling: number;
  consumedQuotaAmount: number;
  remainingQuotaAmount: number;
  status: 'ACTIVE' | 'QUOTA_EXHAUSTED';
}

export interface CatalogItemViewModel {
  id: string;
  umbrellaId: string;
  agreementNumber: string;
  kimapCode: string;
  itemName: string;
  vendorName: string;
  uom: string;
  verifiedTkdnPct: number;
  tkdnCertNo: string;
  leadTimeDays: number;
  tiers: { minQty: number; maxQty: number | null; unitPrice: number; discountPct: number }[];
}

export interface CallOffOrderViewModel {
  poNumber: string;
  branchCode: string;
  kimapCode: string;
  itemName: string;
  vendorName: string;
  orderedQty: number;
  uom: string;
  appliedUnitPrice: number;
  discountPct: number;
  totalOrderAmount: number;
  orderedAt: string;
}

export interface ContractRedlineViewModel {
  versionLabel: string;
  authorRole: string;
  authorName: string;
  clauseRef: string;
  notes: string;
  timestamp: string;
}

export interface ContractAddendumCcoViewModel {
  seq: number;
  addendumNumber: string;
  changeType: string;
  justification: string;
  previousAmount: number;
  deltaAmount: number;
  newAmount: number;
  cumulativeIncreasePct: number;
  additionalDays: number;
  boardOverride: boolean;
}

export type ActiveWorkspaceTab =
  | 'VMS_DIRECTORY'
  | 'VENDOR_ONBOARDING'
  | 'CSMS_HSE'
  | 'BO_CONFLICT'
  | 'RKAP_PR_CONSOLIDATION'
  | 'BOQ_ENCRYPTED_HPS'
  | 'DOA_INBOX'
  | 'TENDER_BID_VAULT'
  | 'HEA_EAUCTION_ROOM'
  | 'BAFO_WINNER_SANGGAH'
  | 'ECATALOG_CALL_OFF'
  | 'ECLM_LIFECYCLE'
  | 'EXECUTION_SCURVE_BAST'
  | 'FINANCE_3WAY_WAPU_VPE'
  | 'AUDIT_CHAIN';

@Component({
  selector: 'eproc-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="eproc-shell">
      <!-- HEADER UTAMA: IDENTITAS KLIEN BUMN DARI ENVIRONMENT / APPSETTINGS.JSON -->
      <header class="eproc-header">
        <div class="eproc-brand">
          <div>
            <div style="font-size: 1.05rem; font-weight: 700;">
              {{ activeClientName() }}
            </div>
            <div style="font-size: 0.76rem; opacity: 0.9;">
              Client Code: <code>{{ activeClientCode() }}</code> | NPWP: {{ activeClientNpwp() }} | Regulasi: {{ activeRegulation() }}
            </div>
          </div>
          <span class="eproc-badge">WAPU PPN 11%: AKTIF</span>
          <span class="eproc-badge">Prefix: eproc (Zero-ABP)</span>
        </div>

        <!-- PEMILIH PORTAL (3 PORTAL / 25 MENU UTAMA / 86 SUB-MENU) -->
        <div style="display: flex; gap: 0.5rem; flex-wrap: wrap;">
          <button
            type="button"
            class="eproc-btn"
            [style.background]="activePortal() === 'PUBLIC_PORTAL' ? '#ffffff' : 'rgba(255,255,255,0.16)'"
            [style.color]="activePortal() === 'PUBLIC_PORTAL' ? '#0f172a' : '#ffffff'"
            (click)="selectPortal('PUBLIC_PORTAL')">
            Portal Publik (5 Menu)
          </button>
          <button
            type="button"
            class="eproc-btn"
            [style.background]="activePortal() === 'VENDOR_PORTAL' ? '#ffffff' : 'rgba(255,255,255,0.16)'"
            [style.color]="activePortal() === 'VENDOR_PORTAL' ? '#0f172a' : '#ffffff'"
            (click)="selectPortal('VENDOR_PORTAL')">
            Portal Vendor (9 Menu / 34 Sub)
          </button>
          <button
            type="button"
            class="eproc-btn"
            [style.background]="activePortal() === 'INTERNAL_BUMN_PORTAL' ? '#ffffff' : 'rgba(255,255,255,0.16)'"
            [style.color]="activePortal() === 'INTERNAL_BUMN_PORTAL' ? '#0f172a' : '#ffffff'"
            (click)="selectPortal('INTERNAL_BUMN_PORTAL')">
            Portal Internal BUMN (11 Menu / 52 Sub)
          </button>
        </div>
      </header>

      <!-- BARIS KONTROL: SIMULASI ENVIRONMENT SERVER BUMN & ROLE PERSONA (SEGREGATION OF DUTIES) -->
      <section class="eproc-controls-bar">
        <div style="display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap;">
          <label style="font-size: 0.8rem; font-weight: 600;">Server Environment (appsettings.json):</label>
          <select class="eproc-input" style="width: auto;" [ngModel]="selectedClientEnv()" (ngModelChange)="switchClientEnvironment($event)">
            <option value="BUMN_KARYA">Server 1: PT BUMN Karya Persero Tbk (#0F4C81)</option>
            <option value="BUMN_ENERGI">Server 2: PT BUMN Energi Nusantara Persero (#006A4E)</option>
          </select>

          <label style="font-size: 0.8rem; font-weight: 600; margin-left: 0.5rem;">Simulasi Role Aktor (SoD):</label>
          <select class="eproc-input" style="width: auto;" [ngModel]="activeRoleCode()" (ngModelChange)="switchRolePersona($event)">
            <option *ngFor="let p of personas" [value]="p.roleCode">
              {{ p.roleCode }} — {{ p.roleName }}
            </option>
          </select>
        </div>

        <div style="font-size: 0.78rem; color: #475569;">
          <strong>SoD Conflict Guard:</strong> Dilarang merangkap
          <code>{{ currentPersona().sodConflicts.join(', ') }}</code>
        </div>
      </section>

      <!-- WORKSPACE UTAMA: SIDEBAR 25 MENU / 86 SUB-MENU & KONTEN INTERAKTIF FASE 1, 2 & 3 -->
      <div class="eproc-workspace">
        <!-- SIDEBAR NAVIGASI -->
        <aside class="eproc-sidebar">
          <div style="font-size: 0.78rem; font-weight: 700; text-transform: uppercase; color: #64748b; margin-bottom: 0.75rem;">
            Peta Menu: {{ activePortal() }} ({{ filteredMenus().length }} Menu Utama)
          </div>

          <div *ngFor="let menu of filteredMenus()" style="margin-bottom: 0.85rem; border-bottom: 1px solid #f1f5f9; padding-bottom: 0.65rem;">
            <div style="font-weight: 700; font-size: 0.86rem; color: #0f172a; margin-bottom: 0.35rem;">
              {{ menu.order }}. {{ menu.title }}
            </div>
            <div *ngIf="menu.subMenus.length === 0" style="font-size: 0.76rem; color: #64748b; padding-left: 0.75rem;">
              Route: <code>{{ menu.route }}</code>
            </div>
            <ul *ngIf="menu.subMenus.length > 0" style="list-style: none; padding-left: 0.6rem; margin: 0;">
              <li
                *ngFor="let sub of menu.subMenus"
                style="font-size: 0.77rem; padding: 0.22rem 0.35rem; border-radius: 4px; margin-bottom: 0.15rem;"
                [style.background]="isSubMenuAllowed(sub.allowedRoles) ? '#f0fdf4' : 'transparent'"
                [style.color]="isSubMenuAllowed(sub.allowedRoles) ? '#166534' : '#64748b'">
                <strong>{{ sub.code }}</strong>: {{ sub.label }}
              </li>
            </ul>
          </div>
        </aside>

        <!-- AREA KERJA UTAMA (FASE 1, FASE 2, & FASE 3) -->
        <main class="eproc-content">
          <!-- BARIS TAB MODUL -->
          <div style="display: flex; gap: 0.4rem; flex-wrap: wrap; margin-bottom: 1rem;">
            <button type="button" [class]="activeTab() === 'VMS_DIRECTORY' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('VMS_DIRECTORY')">
              1. Direktori AVL ({{ vendors().length }})
            </button>
            <button type="button" [class]="activeTab() === 'VENDOR_ONBOARDING' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('VENDOR_ONBOARDING')">
              2. Registrasi & SKN/KD
            </button>
            <button type="button" [class]="activeTab() === 'CSMS_HSE' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('CSMS_HSE')">
              3. Audit CSMS (K3L)
            </button>
            <button type="button" [class]="activeTab() === 'BO_CONFLICT' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('BO_CONFLICT')">
              4. Detektor Afiliasi BO ({{ detectedBoConflicts().length }})
            </button>
            <button type="button" [class]="activeTab() === 'RKAP_PR_CONSOLIDATION' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('RKAP_PR_CONSOLIDATION')">
              5. [Fase 2] RKAP & Konsolidasi PR
            </button>
            <button type="button" [class]="activeTab() === 'BOQ_ENCRYPTED_HPS' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('BOQ_ENCRYPTED_HPS')">
              6. [Fase 2] Segel HPS Terenkripsi
            </button>
            <button type="button" [class]="activeTab() === 'DOA_INBOX' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('DOA_INBOX')">
              7. [Fase 2] DOA Inbox ({{ doaInbox().length }})
            </button>
            <button type="button" [class]="activeTab() === 'TENDER_BID_VAULT' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('TENDER_BID_VAULT')">
              8. [Fase 3] Aanwijzing & Encrypted Bid Vault
            </button>
            <button type="button" [class]="activeTab() === 'HEA_EAUCTION_ROOM' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('HEA_EAUCTION_ROOM')">
              9. [Fase 3] Evaluasi HEA TKDN & e-Reverse Auction
            </button>
            <button type="button" [class]="activeTab() === 'BAFO_WINNER_SANGGAH' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('BAFO_WINNER_SANGGAH')">
              10. [Fase 3] BAFO, Pemenang (BAHP) & Sanggah
            </button>
            <button type="button" [class]="activeTab() === 'ECATALOG_CALL_OFF' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('ECATALOG_CALL_OFF')">
              11. [Fase 4] E-Katalog & Call-Off Order
            </button>
            <button type="button" [class]="activeTab() === 'ECLM_LIFECYCLE' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('ECLM_LIFECYCLE')">
              12. [Fase 4] e-CLM: Jaminan 5%, TTE, SPMK & CCO
            </button>
            <button type="button" [class]="activeTab() === 'EXECUTION_SCURVE_BAST' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('EXECUTION_SCURVE_BAST')">
              13. [Fase 5] Kurva S (SCM), BAST & Post-Audit TKDN
            </button>
            <button type="button" [class]="activeTab() === 'FINANCE_3WAY_WAPU_VPE' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('FINANCE_3WAY_WAPU_VPE')">
              14. [Fase 5] 3-Way Match e-Invoice, Pajak WAPU & VPE
            </button>
            <button type="button" [class]="activeTab() === 'AUDIT_CHAIN' ? 'eproc-btn' : 'eproc-btn-outline'" (click)="activeTab.set('AUDIT_CHAIN')">
              15. Audit Hash-Chain ({{ auditLogs().length }})
            </button>
          </div>

          <!-- TAB 1: DIREKTORI & VERIFIKASI AVL VENDOR -->
          <section *ngIf="activeTab() === 'VMS_DIRECTORY'" class="eproc-card">
            <h3 style="margin-top: 0;">Direktori Vendor Terdaftar (Approved Vendor List / AVL & Kapasitas Finansial)</h3>
            <table class="eproc-table">
              <thead>
                <tr>
                  <th>Kode DRT & Nama Vendor</th>
                  <th>Skala & Kategori</th>
                  <th>Kemampuan Dasar (KD)</th>
                  <th>Sisa Kemampuan Nyata (SKN)</th>
                  <th>Altman Z-Score</th>
                  <th>Sertifikat CSMS</th>
                  <th>Status AVL</th>
                  <th>Aksi Verifikator</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let v of vendors()">
                  <td>
                    <strong>{{ v.companyName }}</strong><br />
                    <small style="color: #64748b;">{{ v.vendorCode }} | NPWP: {{ v.npwpNumber }}</small>
                  </td>
                  <td>
                    <span class="eproc-pill eproc-pill-warn">{{ v.businessScale }}</span>
                    <span *ngIf="v.isBumnAffiliated" class="eproc-pill eproc-pill-success" style="margin-left: 4px;">Sinergi BUMN</span>
                    <span *ngIf="v.isPadiUmkm" class="eproc-pill eproc-pill-success" style="margin-left: 4px;">PaDi UMKM</span>
                  </td>
                  <td>Rp {{ v.kemampuanDasarKd | number:'1.0-0' }}</td>
                  <td>
                    <strong>Rp {{ v.sisaKemampuanNyataSkn | number:'1.0-0' }}</strong><br />
                    <small>SKP: {{ v.sisaKemampuanPaketSkp }} Paket</small>
                  </td>
                  <td>
                    <strong>{{ v.altmanZScore | number:'1.2-2' }}</strong><br />
                    <span class="eproc-pill eproc-pill-success">{{ v.financialHealthZone }}</span>
                  </td>
                  <td>
                    <span
                      class="eproc-pill"
                      [ngClass]="v.csmsLevel === 'HIGH' || v.csmsLevel === 'EXTREME' ? 'eproc-pill-success' : 'eproc-pill-warn'">
                      {{ v.csmsLevel }} ({{ v.csmsScore }}%)
                    </span>
                  </td>
                  <td>
                    <span
                      class="eproc-pill"
                      [ngClass]="v.verificationStatus === 'VERIFIED_AVL' ? 'eproc-pill-success' : 'eproc-pill-warn'">
                      {{ v.verificationStatus }}
                    </span>
                  </td>
                  <td>
                    <button
                      *ngIf="v.verificationStatus !== 'VERIFIED_AVL'"
                      type="button"
                      class="eproc-btn"
                      (click)="approveVendorAvl(v.id)">
                      Setujui AVL
                    </button>
                    <span *ngIf="v.verificationStatus === 'VERIFIED_AVL'" style="font-size: 0.78rem; color: #0f766e; font-weight: 600;">
                      ✓ Terverifikasi AVL
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </section>

          <!-- TAB 2: REGISTRASI VENDOR BARU & KALKULATOR SKN / KD / ALTMAN -->
          <section *ngIf="activeTab() === 'VENDOR_ONBOARDING'" class="eproc-card">
            <h3 style="margin-top: 0;">Registrasi Vendor Baru & Kalkulator Otomatis KD, SKN, dan Altman Z-Score</h3>
            <div class="eproc-grid-2">
              <div>
                <label style="font-size: 0.8rem; font-weight: 600;">Nama Perusahaan Penyedia</label>
                <input class="eproc-input" [(ngModel)]="newVendorForm.companyName" />

                <label style="font-size: 0.8rem; font-weight: 600; display: block; margin-top: 0.6rem;">Skala Usaha</label>
                <select class="eproc-input" [(ngModel)]="newVendorForm.businessScale">
                  <option value="KECIL">KECIL / UMKM (fl=0.3, fp=5, KP=5)</option>
                  <option value="MENENGAH">MENENGAH (fl=0.6, fp=7, KP=6)</option>
                  <option value="BESAR">BESAR (fl=0.8, fp=8, KP=8)</option>
                </select>

                <label style="font-size: 0.8rem; font-weight: 600; display: block; margin-top: 0.6rem;">Nomor NIB (OSS) & NPWP</label>
                <div style="display: flex; gap: 0.5rem;">
                  <input class="eproc-input" [(ngModel)]="newVendorForm.nibNumber" placeholder="NIB" />
                  <input class="eproc-input" [(ngModel)]="newVendorForm.npwpNumber" placeholder="NPWP" />
                </div>

                <label style="font-size: 0.8rem; font-weight: 600; display: block; margin-top: 0.6rem;">
                  Nama & NIK Beneficial Owner (Pemegang Saham Utama)
                </label>
                <div style="display: flex; gap: 0.5rem;">
                  <input class="eproc-input" [(ngModel)]="newVendorForm.shareholderOwnerName" placeholder="Nama Pemilik Manfaat" />
                  <input class="eproc-input" [(ngModel)]="newVendorForm.shareholderNik" placeholder="NIK 16 Digit" />
                </div>
              </div>

              <div>
                <label style="font-size: 0.8rem; font-weight: 600;">Kekayaan Bersih / Ekuitas Audited (KB) — Rp</label>
                <input type="number" class="eproc-input" [(ngModel)]="newVendorForm.netWorthEquityKb" />

                <label style="font-size: 0.8rem; font-weight: 600; display: block; margin-top: 0.6rem;">
                  Nilai Pengalaman Tertinggi 10 Tahun Terakhir (NPt) — Rp
                </label>
                <input type="number" class="eproc-input" [(ngModel)]="newVendorForm.highestExperienceNpt" />

                <label style="font-size: 0.8rem; font-weight: 600; display: block; margin-top: 0.6rem;">
                  Total Nilai Kontrak Sedang Berjalan (On-Going) — Rp
                </label>
                <input type="number" class="eproc-input" [(ngModel)]="newVendorForm.ongoingContractsValue" />

                <div style="background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 8px; padding: 0.75rem; margin-top: 0.85rem; font-size: 0.82rem;">
                  <div><strong>Preview Kemampuan Dasar (KD = 3 &times; NPt):</strong> Rp {{ previewKd() | number:'1.0-0' }}</div>
                  <div><strong>Preview Sisa Kemampuan Nyata (SKN):</strong> Rp {{ previewSkn() | number:'1.0-0' }}</div>
                </div>

                <button type="button" class="eproc-btn" style="margin-top: 0.75rem; width: 100%;" (click)="submitNewVendor()">
                  Daftarkan Vendor & Catat ke Hash-Chain Audit Trail
                </button>
              </div>
            </div>
          </section>

          <!-- TAB 3: EVALUASI 8 ELEMEN CSMS -->
          <section *ngIf="activeTab() === 'CSMS_HSE'" class="eproc-card">
            <h3 style="margin-top: 0;">Evaluasi K3L / HSE — 8 Elemen CSMS (Contractor Safety Management System)</h3>
            <div style="display: flex; gap: 1rem; align-items: center; margin-bottom: 1rem; flex-wrap: wrap;">
              <label style="font-size: 0.84rem; font-weight: 600;">Pilih Vendor:</label>
              <select class="eproc-input" style="width: 340px;" [(ngModel)]="selectedCsmsVendorId">
                <option *ngFor="let v of vendors()" [value]="v.id">{{ v.companyName }} ({{ v.vendorCode }})</option>
              </select>

              <label style="font-size: 0.84rem; font-weight: 600;">Skor Verifikasi Lapangan HSE (60%):</label>
              <input type="number" class="eproc-input" style="width: 110px;" [(ngModel)]="csmsFieldScore" min="0" max="100" />
            </div>

            <table class="eproc-table">
              <thead>
                <tr>
                  <th>No</th>
                  <th>Elemen Standar CSMS BUMN</th>
                  <th>Bobot (%)</th>
                  <th>Skor Verifikasi Dokumen Auditor (0-100)</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let el of csmsElements">
                  <td>{{ el.no }}</td>
                  <td>{{ el.name }}</td>
                  <td>{{ el.weight }}%</td>
                  <td>
                    <input type="number" class="eproc-input" style="width: 100px;" [(ngModel)]="el.score" min="0" max="100" />
                  </td>
                </tr>
              </tbody>
            </table>

            <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 1rem; background: #f0fdf4; padding: 0.85rem; border-radius: 8px;">
              <div>
                <strong>Skor Akhir CSMS (40% Dokumen + 60% Lapangan): {{ calculatedCsmsFinalScore() | number:'1.2-2' }}%</strong>
                &rarr; Kualifikasi Risiko:
                <span class="eproc-pill eproc-pill-success">{{ calculatedCsmsRiskLevel() }} RISK</span>
              </div>
              <button type="button" class="eproc-btn" (click)="issueCsmsCertificate()">
                Terbitkan Sertifikat CSMS Resmi
              </button>
            </div>
          </section>

          <!-- TAB 4: DETEKTOR KONFLIK KEPENTINGAN & BENEFICIAL OWNERSHIP -->
          <section *ngIf="activeTab() === 'BO_CONFLICT'" class="eproc-card">
            <h3 style="margin-top: 0;">Detektor Afiliasi & Beneficial Ownership (Anti-Bid-Rigging Engine)</h3>
            <table class="eproc-table">
              <thead>
                <tr>
                  <th>Perusahaan A</th>
                  <th>Perusahaan B</th>
                  <th>Jenis Konflik Afiliasi</th>
                  <th>Nama Pemilik Manfaat / Pengurus</th>
                  <th>NIK / NPWP Terdeteksi Sama</th>
                  <th>Rekomendasi Sistem</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let c of detectedBoConflicts()">
                  <td><strong>{{ c.vendorA }}</strong></td>
                  <td><strong>{{ c.vendorB }}</strong></td>
                  <td><span class="eproc-pill eproc-pill-danger">{{ c.relationType }}</span></td>
                  <td>{{ c.personName }}</td>
                  <td><code>{{ c.nik }}</code></td>
                  <td><strong style="color: #b91c1c;">BLOKIR DARI PAKET TENDER YANG SAMA</strong></td>
                </tr>
              </tbody>
            </table>
          </section>

          <!-- TAB 5 (FASE 2): PAGU RKAP, PR & KONSOLIDASI PAKET -->
          <section *ngIf="activeTab() === 'RKAP_PR_CONSOLIDATION'">
            <div class="eproc-card">
              <h3 style="margin-top: 0;">1. Pagu Anggaran RKAP (Capex & Opex) — Real-Time Budget Commitment Lock</h3>
              <table class="eproc-table">
                <thead>
                  <tr>
                    <th>Cost Center & Unit</th>
                    <th>Tipe</th>
                    <th>WBS / GL Account</th>
                    <th>Pagu Revisi (Rp)</th>
                    <th>Terkunci / Committed PR (Rp)</th>
                    <th>Realisasi Aktual (Rp)</th>
                    <th>Sisa Pagu Tersedia (Rp)</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let b of rkapBudgets()">
                    <td><strong>{{ b.costCenterCode }}</strong> — {{ b.costCenterName }}</td>
                    <td><span class="eproc-pill eproc-pill-warn">{{ b.budgetType }}</span></td>
                    <td><code>{{ b.wbsElementGlAccount }}</code></td>
                    <td>Rp {{ b.revisedAmount | number:'1.0-0' }}</td>
                    <td><strong style="color: #b45309;">Rp {{ b.committedAmount | number:'1.0-0' }}</strong></td>
                    <td>Rp {{ b.actualSpentAmount | number:'1.0-0' }}</td>
                    <td><strong style="color: #0f766e;">Rp {{ b.availableAmount | number:'1.0-0' }}</strong></td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div class="eproc-grid-2">
              <div class="eproc-card">
                <h3 style="margin-top: 0;">2. Buat Purchase Requisition (PR) Cabang / Divisi</h3>
                <label style="font-size: 0.8rem; font-weight: 600;">Pilih Sumber Pagu RKAP</label>
                <select class="eproc-input" [(ngModel)]="newPrForm.rkapBudgetId">
                  <option *ngFor="let b of rkapBudgets()" [value]="b.id">
                    {{ b.wbsElementGlAccount }} — Sisa Rp {{ b.availableAmount | number:'1.0-0' }}
                  </option>
                </select>

                <label style="font-size: 0.8rem; font-weight: 600; display: block; margin-top: 0.55rem;">Judul Kebutuhan PR & Unit Peminta</label>
                <input class="eproc-input" [(ngModel)]="newPrForm.prTitle" placeholder="Judul PR" style="margin-bottom: 0.4rem;" />
                <input class="eproc-input" [(ngModel)]="newPrForm.requesterUnitName" placeholder="Cabang / Regional Peminta" />

                <label style="font-size: 0.8rem; font-weight: 600; display: block; margin-top: 0.55rem;">Kode KIMAP & Nama Material/Jasa</label>
                <div style="display: flex; gap: 0.5rem;">
                  <input class="eproc-input" [(ngModel)]="newPrForm.materialCode" style="width: 42%;" />
                  <input class="eproc-input" [(ngModel)]="newPrForm.itemName" />
                </div>

                <label style="font-size: 0.8rem; font-weight: 600; display: block; margin-top: 0.55rem;">Kuantitas & Estimasi Harga Satuan (Rp)</label>
                <div style="display: flex; gap: 0.5rem;">
                  <input type="number" class="eproc-input" [(ngModel)]="newPrForm.quantity" style="width: 35%;" />
                  <input type="number" class="eproc-input" [(ngModel)]="newPrForm.unitPrice" />
                </div>

                <div *ngIf="prErrorMsg()" style="color: #b91c1c; font-size: 0.78rem; font-weight: 600; margin-top: 0.5rem;">
                  {{ prErrorMsg() }}
                </div>

                <button type="button" class="eproc-btn" style="margin-top: 0.75rem; width: 100%;" (click)="createNewPr()">
                  Submit PR & Kunci Pagu RKAP Otomatis
                </button>
              </div>

              <div class="eproc-card">
                <h3 style="margin-top: 0;">3. Konsolidasi PR Lintas Cabang Menjadi 1 Paket Pengadaan</h3>
                <table class="eproc-table">
                  <thead>
                    <tr>
                      <th>Pilih</th>
                      <th>Nomor PR & Cabang</th>
                      <th>KIMAP & Kuantitas</th>
                      <th>Total Estimasi</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let pr of purchaseRequisitions()">
                      <td>
                        <input
                          type="checkbox"
                          [disabled]="pr.status === 'CONSOLIDATED_TO_PACKAGE'"
                          [(ngModel)]="pr.selectedForConsolidation" />
                      </td>
                      <td>
                        <strong>{{ pr.prNumber }}</strong><br />
                        <small>{{ pr.requesterUnitName }}</small>
                      </td>
                      <td>
                        <code>{{ pr.materialCode }}</code><br />
                        <small>{{ pr.quantity }} {{ pr.uom }}</small>
                      </td>
                      <td>Rp {{ pr.totalValue | number:'1.0-0' }}</td>
                      <td>
                        <span
                          class="eproc-pill"
                          [ngClass]="pr.status === 'CONSOLIDATED_TO_PACKAGE' ? 'eproc-pill-success' : 'eproc-pill-warn'">
                          {{ pr.status === 'CONSOLIDATED_TO_PACKAGE' ? 'CONSOLIDATED' : 'READY' }}
                        </span>
                      </td>
                    </tr>
                  </tbody>
                </table>

                <div style="display: flex; gap: 0.5rem; margin-top: 0.75rem;">
                  <input class="eproc-input" [(ngModel)]="consolidationForm.packageName" placeholder="Nama Paket Hasil Konsolidasi" />
                  <select class="eproc-input" style="width: 190px;" [(ngModel)]="consolidationForm.envelopeMethod">
                    <option value="1_SAMPUL">1 Sampul</option>
                    <option value="2_SAMPUL">2 Sampul</option>
                    <option value="2_TAHAP">2 Tahap</option>
                  </select>
                </div>

                <button type="button" class="eproc-btn" style="margin-top: 0.65rem; width: 100%;" (click)="consolidateSelectedPrs()">
                  Gabungkan PR Terpilih Menjadi Paket Pengadaan
                </button>
              </div>
            </div>
          </section>

          <!-- TAB 6 (FASE 2): BOQ / AHSP & PENYEGELAN HPS TERENKRIPSI -->
          <section *ngIf="activeTab() === 'BOQ_ENCRYPTED_HPS'" class="eproc-card">
            <h3 style="margin-top: 0;">Daftar Paket Pengadaan, Struktur BoQ/AHSP & Penyegelan HPS Terenkripsi (Zero-Knowledge)</h3>
            <table class="eproc-table">
              <thead>
                <tr>
                  <th>Kode & Nama Paket</th>
                  <th>PR Sumber & Kuantitas Gabungan</th>
                  <th>Metode & Sampul</th>
                  <th>Pagu Anggaran (Rp)</th>
                  <th>Status Segel HPS / Owner Estimate</th>
                  <th>Status Paket</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let pkg of packages()">
                  <td>
                    <strong>{{ pkg.packageName }}</strong><br />
                    <small>{{ pkg.packageCode }} | Min TKDN: {{ pkg.minTkdnPct }}%</small>
                  </td>
                  <td>
                    <code>{{ pkg.sourcePrNumbers.join(', ') }}</code><br />
                    <small>Volume Gabungan: <strong>{{ pkg.consolidatedQty }} {{ pkg.uom }}</strong></small>
                  </td>
                  <td>
                    {{ pkg.sourcingMethod }}<br />
                    <span class="eproc-pill eproc-pill-warn">{{ pkg.envelopeMethod }}</span>
                  </td>
                  <td><strong>Rp {{ pkg.totalPaguBudget | number:'1.0-0' }}</strong></td>
                  <td>
                    <div *ngIf="pkg.sealedHpsCipherPreview">
                      <span class="eproc-pill eproc-pill-success">🔒 TERSEGEL AES-256-GCM</span><br />
                      <code style="font-size: 0.7rem; color: #475569;">Cipher: {{ pkg.sealedHpsCipherPreview }}</code><br />
                      <code style="font-size: 0.7rem; color: #0f766e;">SHA256: {{ pkg.sealedHpsSha256?.slice(0, 24) }}...</code>
                    </div>
                    <div *ngIf="!pkg.sealedHpsCipherPreview" style="display: flex; gap: 0.4rem; align-items: center;">
                      <input
                        type="number"
                        class="eproc-input"
                        style="width: 155px;"
                        [(ngModel)]="hpsInputMap[pkg.id]"
                        placeholder="Input Nilai HPS (Rp)" />
                      <button type="button" class="eproc-btn" (click)="sealPackageHps(pkg)">
                        Segel HPS
                      </button>
                    </div>
                  </td>
                  <td>
                    <span
                      class="eproc-pill"
                      [ngClass]="pkg.status === 'DOA_APPROVED_READY_FOR_TENDER' ? 'eproc-pill-success' : 'eproc-pill-warn'">
                      {{ pkg.status }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </section>

          <!-- TAB 7 (FASE 2): KOTAK PERSETUJUAN BERJENJANG (DOA INBOX) -->
          <section *ngIf="activeTab() === 'DOA_INBOX'" class="eproc-card">
            <h3 style="margin-top: 0;">Kotak Persetujuan Berjenjang — Delegation of Authority (DOA Matrix Inbox)</h3>
            <div *ngFor="let doa of doaInbox()" style="border: 1px solid #cbd5e1; border-radius: 8px; padding: 1rem; margin-bottom: 1rem;">
              <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem;">
                <div>
                  <strong style="font-size: 0.95rem;">{{ doa.packageCode }} — {{ doa.packageTitle }}</strong>
                  <div style="font-size: 0.8rem; color: #475569;">
                    Nilai Pagu Paket: <strong>Rp {{ doa.transactionAmountIdr | number:'1.0-0' }}</strong> |
                    Tahap Saat Ini: <strong>Step {{ doa.currentStepSequence }} dari {{ doa.totalStepsCount }}</strong>
                  </div>
                </div>
                <div style="display: flex; gap: 0.5rem; align-items: center;">
                  <span
                    class="eproc-pill"
                    [ngClass]="doa.overallStatus === 'APPROVED' ? 'eproc-pill-success' : 'eproc-pill-warn'">
                    {{ doa.overallStatus }}
                  </span>
                  <button
                    *ngIf="doa.overallStatus === 'IN_PROGRESS'"
                    type="button"
                    class="eproc-btn"
                    (click)="approveNextDoaStep(doa.id)">
                    Setujui Step {{ doa.currentStepSequence }} (TTE Approval)
                  </button>
                </div>
              </div>

              <table class="eproc-table">
                <thead>
                  <tr>
                    <th>Step</th>
                    <th>Role Approver</th>
                    <th>Jenjang Jabatan (DOA Tier)</th>
                    <th>Status Persetujuan</th>
                    <th>Pejabat & Catatan</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let step of doa.steps">
                    <td>Step {{ step.stepSequence }}</td>
                    <td><code>{{ step.roleCode }}</code></td>
                    <td><strong>{{ step.positionGrade }}</strong></td>
                    <td>
                      <span
                        class="eproc-pill"
                        [ngClass]="step.action === 'APPROVED' ? 'eproc-pill-success' : 'eproc-pill-warn'">
                        {{ step.action }}
                      </span>
                    </td>
                    <td>{{ step.actedBy || '-' }} {{ step.comments ? '— ' + step.comments : '' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

          <!-- TAB 8 (FASE 3): FORUM AANWIJZING & ENCRYPTED BID VAULT (SAMPUL 1 & SAMPUL 2) -->
          <section *ngIf="activeTab() === 'TENDER_BID_VAULT'">
            <div class="eproc-card">
              <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap;">
                <div>
                  <h3 style="margin: 0;">TND-BUMN-2026-0401 — Pengadaan Konsolidasi Gate Valve API 600 DN300 (50 Unit)</h3>
                  <div style="font-size: 0.8rem; color: #475569; margin-top: 0.25rem;">
                    Metode: <strong>TENDER UMUM | 2 SAMPUL | SISTEM NILAI (30% Teknis : 70% Komersial HEA)</strong> |
                    Pagu: <strong>Rp 6.250.000.000</strong> |
                    HPS PPK:
                    <strong *ngIf="isBidVaultOpened()" style="color: #0f766e;">Rp {{ tenderHpsAmount | number:'1.0-0' }} (Terdekripsi)</strong>
                    <span *ngIf="!isBidVaultOpened()" class="eproc-pill eproc-pill-warn">🔒 TERSEGEL (ZERO-KNOWLEDGE)</span>
                  </div>
                </div>
                <div>
                  <button
                    *ngIf="!isBidVaultOpened()"
                    type="button"
                    class="eproc-btn"
                    (click)="executeBidOpeningCeremony()">
                    🔓 Jalankan Bid Opening Ceremony (Dekripsi HPS & Sampul 2 Harga)
                  </button>
                  <span *ngIf="isBidVaultOpened()" class="eproc-pill eproc-pill-success">
                    ✓ SAMPUL HARGA & HPS TELAH DIBUKA RESMI OLEH POKJA
                  </span>
                </div>
              </div>
            </div>

            <div class="eproc-grid-2">
              <!-- FORUM AANWIJZING -->
              <div class="eproc-card">
                <h3 style="margin-top: 0;">1. Forum Aanwijzing (Identitas Peserta Disamarkan)</h3>
                <div *ngFor="let qa of aanwijzingList()" style="background: #f8fafc; border-left: 3px solid #0F4C81; padding: 0.65rem; margin-bottom: 0.65rem; font-size: 0.81rem;">
                  <div><strong>{{ qa.maskedAlias }}</strong> — <small>{{ qa.sectionRef }}</small></div>
                  <div style="margin: 0.25rem 0;"><em>"Q: {{ qa.question }}"</em></div>
                  <div style="color: #0f766e;"><strong>Jawaban Pokja:</strong> {{ qa.answer }}</div>
                </div>

                <label style="font-size: 0.78rem; font-weight: 600;">Tambah Pertanyaan Aanwijzing & Jawaban BAP Pokja</label>
                <input class="eproc-input" [(ngModel)]="newAanwijzing.question" placeholder="Pertanyaan peserta..." style="margin-bottom: 0.4rem;" />
                <input class="eproc-input" [(ngModel)]="newAanwijzing.answer" placeholder="Jawaban penjelasan Pokja..." />
                <button type="button" class="eproc-btn-outline" style="margin-top: 0.5rem; width: 100%;" (click)="addAanwijzingItem()">
                  Posting ke Berita Acara Penjelasan (BAP Aanwijzing)
                </button>
              </div>

              <!-- INPUT PENAWARAN VENDOR KE ENCRYPTED BID VAULT -->
              <div class="eproc-card">
                <h3 style="margin-top: 0;">2. Upload Sampul Penawaran Vendor ke Encrypted Bid Vault</h3>
                <label style="font-size: 0.78rem; font-weight: 600;">Nama Vendor Peserta</label>
                <input class="eproc-input" [(ngModel)]="newBidForm.vendorName" />

                <div style="display: flex; gap: 0.5rem; margin-top: 0.5rem;">
                  <div style="flex: 1;">
                    <label style="font-size: 0.78rem; font-weight: 600;">Harga Penawaran (HP) — Rp</label>
                    <input type="number" class="eproc-input" [(ngModel)]="newBidForm.priceHp" />
                  </div>
                  <div style="width: 110px;">
                    <label style="font-size: 0.78rem; font-weight: 600;">TKDN (%)</label>
                    <input type="number" class="eproc-input" [(ngModel)]="newBidForm.tkdnPct" />
                  </div>
                  <div style="width: 110px;">
                    <label style="font-size: 0.78rem; font-weight: 600;">Skor Teknis</label>
                    <input type="number" class="eproc-input" [(ngModel)]="newBidForm.techScore" />
                  </div>
                </div>

                <button type="button" class="eproc-btn" style="margin-top: 0.75rem; width: 100%;" (click)="submitEncryptedBidToVault()">
                  🔒 Enkripsi (AES-256-GCM) & Simpan ke Bid Vault
                </button>
              </div>
            </div>

            <!-- TABEL ENCRYPTED BID VAULT -->
            <div class="eproc-card">
              <h3 style="margin-top: 0;">3. Brankas Sampul Penawaran Terenkripsi (Encrypted Bid Vault)</h3>
              <table class="eproc-table">
                <thead>
                  <tr>
                    <th>Tanda Terima & Peserta</th>
                    <th>Sampul 1 (Admin & Teknis)</th>
                    <th>Sampul 2 (Ciphertext AES-256-GCM & Hash SHA-256)</th>
                    <th>Harga Penawaran (HP)</th>
                    <th>Deklarasi TKDN</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let sub of bidVaultSubmissions()">
                    <td>
                      <strong>{{ sub.vendorName }}</strong><br />
                      <small><code>{{ sub.receiptNo }}</code> | Skala: {{ sub.businessScale }}</small>
                    </td>
                    <td>
                      <span class="eproc-pill" [ngClass]="sub.techPassed ? 'eproc-pill-success' : 'eproc-pill-danger'">
                        {{ sub.techPassed ? 'LULUS TEKNIS (' + sub.techScore + ')' : 'GUGUR TEKNIS' }}
                      </span>
                    </td>
                    <td>
                      <code style="font-size: 0.72rem; color: #475569;">Cipher: {{ sub.cipherPreview }}</code><br />
                      <code style="font-size: 0.72rem; color: #0f766e;">SHA256: {{ sub.sha256Hash.slice(0, 24) }}...</code>
                    </td>
                    <td>
                      <strong *ngIf="sub.isOpened">Rp {{ sub.decryptedPriceHp | number:'1.0-0' }}</strong>
                      <span *ngIf="!sub.isOpened" class="eproc-pill eproc-pill-warn">🔒 TERKUNCI DI VAULT</span>
                    </td>
                    <td>
                      <strong *ngIf="sub.isOpened">{{ sub.declaredTkdnPct }}%</strong>
                      <span *ngIf="!sub.isOpened" class="eproc-pill eproc-pill-warn">🔒 TERKUNCI</span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

          <!-- TAB 9 (FASE 3): EVALUASI HEA TKDN & RUANG LIVE E-REVERSE AUCTION (ANTI-SNIPING) -->
          <section *ngIf="activeTab() === 'HEA_EAUCTION_ROOM'">
            <div class="eproc-card">
              <div style="display: flex; justify-content: space-between; align-items: center;">
                <div>
                  <h3 style="margin: 0;">Kertas Kerja Evaluasi Harga, Preferensi TKDN (HEA), & Peringkat Gabungan</h3>
                  <p style="font-size: 0.8rem; color: #475569; margin: 0.25rem 0 0 0;">
                    Perhatikan bagaimana <strong>Produk Dalam Negeri (TKDN &ge; 25%)</strong> mendapatkan Koefisien Preferensi (KP)
                    sehingga <strong>Nilai HEA-nya lebih rendah</strong> dan memenangkan peringkat atas produk impor yang harganya lebih murah!
                  </p>
                </div>
                <button
                  *ngIf="!isBidVaultOpened()"
                  type="button"
                  class="eproc-btn"
                  (click)="executeBidOpeningCeremony()">
                  Buka Sampul Harga Sekarang
                </button>
              </div>

              <table class="eproc-table" style="margin-top: 0.75rem;">
                <thead>
                  <tr>
                    <th>Peringkat</th>
                    <th>Nama Peserta</th>
                    <th>Skor Teknis (30%)</th>
                    <th>Harga Penawaran Aktual (HP)</th>
                    <th>TKDN & KP Preferensi</th>
                    <th>Harga Evaluasi Akhir (HEA)</th>
                    <th>Skor Gabungan</th>
                    <th>Aksi Live e-Reverse Auction</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let r of rankedHeaSubmissions()">
                    <td>
                      <span class="eproc-pill" [ngClass]="r.rank === 1 ? 'eproc-pill-success' : 'eproc-pill-warn'">
                        RANK #{{ r.rank }}
                      </span>
                    </td>
                    <td><strong>{{ r.vendorName }}</strong></td>
                    <td>{{ r.techScore }}</td>
                    <td><strong>Rp {{ r.decryptedPriceHp | number:'1.0-0' }}</strong></td>
                    <td>
                      TKDN: <strong>{{ r.declaredTkdnPct }}%</strong><br />
                      <small>KP: {{ r.kp | number:'1.4-4' }}</small>
                    </td>
                    <td>
                      <strong style="color: #0f766e;">Rp {{ r.calculatedHea | number:'1.0-0' }}</strong>
                    </td>
                    <td><strong>{{ r.combinedScore | number:'1.2-2' }}</strong></td>
                    <td>
                      <button
                        type="button"
                        class="eproc-btn-outline"
                        (click)="simulateEAuctionDropBid(r.participantId)">
                        ⬇ Turunkan Bid Rp 100 Jt (Di 2 Menit Terakhir)
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- LIVE E-REVERSE AUCTION LOG DENGAN ANTI-SNIPING AUTO-EXTEND -->
            <div class="eproc-card">
              <div style="display: flex; justify-content: space-between; align-items: center;">
                <h3 style="margin: 0;">Ruang Live e-Reverse Auction & Anti-Sniping Protection</h3>
                <div>
                  <span class="eproc-pill eproc-pill-warn">
                    Perpanjangan Waktu Otomatis (Auto-Extend +5 Menit): {{ eAuctionExtensionsCount() }}x dari Maks 5x
                  </span>
                </div>
              </div>
              <p style="font-size: 0.8rem; color: #475569;">
                Setiap kali peserta menurunkan harga pada <strong>3 menit terakhir</strong>, sistem otomatis memperpanjang waktu lelang
                selama <strong>5 menit (Anti-Sniping)</strong> dan menghitung ulang peringkat berbasis <strong>HEA TKDN</strong>.
              </p>

              <table class="eproc-table">
                <thead>
                  <tr>
                    <th>Bid #</th>
                    <th>Waktu Bid</th>
                    <th>Peserta</th>
                    <th>Harga Penawaran Baru (HP)</th>
                    <th>Nilai HEA Baru</th>
                    <th>Status Anti-Sniping</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let log of eAuctionLogs()">
                    <td>#{{ log.seq }}</td>
                    <td>{{ log.timestamp }}</td>
                    <td><strong>{{ log.vendorName }}</strong></td>
                    <td>Rp {{ log.newPriceHp | number:'1.0-0' }}</td>
                    <td><strong style="color: #0f766e;">Rp {{ log.newHea | number:'1.0-0' }}</strong></td>
                    <td>
                      <span *ngIf="log.triggeredAutoExtend" class="eproc-pill eproc-pill-danger">
                        ⏱ AUTO-EXTEND +5 MENIT DIPICU!
                      </span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

          <!-- TAB 10 (FASE 3): NEGOSIASI BAFO, PENETAPAN PEMENANG (BAHP) & MASA SANGGAH -->
          <section *ngIf="activeTab() === 'BAFO_WINNER_SANGGAH'" class="eproc-grid-2">
            <!-- BAFO & BAHP -->
            <div class="eproc-card">
              <h3 style="margin-top: 0;">1. Negosiasi Harga Akhir (BAFO) & Penetapan Pemenang (BAHP)</h3>
              <p style="font-size: 0.8rem; color: #475569;">
                Peringkat 1 saat ini: <strong>{{ topRankedCandidate()?.vendorName }}</strong>
                (Harga Terakhir: Rp {{ topRankedCandidate()?.decryptedPriceHp | number:'1.0-0' }}).
              </p>

              <label style="font-size: 0.8rem; font-weight: 600;">Nomor Berita Acara Hasil Pemilihan (BAHP)</label>
              <input class="eproc-input" [(ngModel)]="bafoForm.bahpNumber" />

              <label style="font-size: 0.8rem; font-weight: 600; display: block; margin-top: 0.6rem;">
                Harga Kesepakatan Negosiasi Akhir (BAFO) — Rp
              </label>
              <input type="number" class="eproc-input" [(ngModel)]="bafoForm.finalBafoPrice" />

              <div style="background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 8px; padding: 0.8rem; margin-top: 0.85rem; font-size: 0.82rem;">
                <div><strong>Penghematan (Cost Saving) terhadap HPS (Rp 6,00 M):</strong> Rp {{ (tenderHpsAmount - bafoForm.finalBafoPrice) | number:'1.0-0' }} ({{ (((tenderHpsAmount - bafoForm.finalBafoPrice) / tenderHpsAmount) * 100) | number:'1.2-2' }}%)</div>
                <div><strong>Penghematan (Cost Saving) terhadap Pagu (Rp 6,25 M):</strong> Rp {{ (6250000000 - bafoForm.finalBafoPrice) | number:'1.0-0' }} ({{ (((6250000000 - bafoForm.finalBafoPrice) / 6250000000) * 100) | number:'1.2-2' }}%)</div>
                <div><strong>Jaminan Pelaksanaan Wajib (5%):</strong> Rp {{ (bafoForm.finalBafoPrice * 0.05) | number:'1.0-0' }}</div>
              </div>

              <button type="button" class="eproc-btn" style="margin-top: 0.75rem; width: 100%;" (click)="announceWinnerBahp()">
                Tetapkan Pemenang (BAHP) & Buka Masa Sanggah (5 Hari Kerja)
              </button>

              <div *ngIf="tenderStageStatus()" style="margin-top: 0.75rem; text-align: center;">
                Status Tahapan Tender: <span class="eproc-pill eproc-pill-success">{{ tenderStageStatus() }}</span>
              </div>
            </div>

            <!-- MASA SANGGAH & PUTUSAN POKJA -->
            <div class="eproc-card">
              <h3 style="margin-top: 0;">2. Penanganan Sanggah Peserta & Putusan Pokja / PA-KPA</h3>
              <div *ngFor="let s of sanggahList()" style="border: 1px solid #cbd5e1; border-radius: 8px; padding: 0.8rem; margin-bottom: 0.75rem; font-size: 0.81rem;">
                <div style="display: flex; justify-content: space-between;">
                  <strong>{{ s.vendorName }} ({{ s.letterNo }})</strong>
                  <span class="eproc-pill" [ngClass]="s.status === 'REJECTED_TENDER_CONTINUES' ? 'eproc-pill-success' : 'eproc-pill-warn'">
                    {{ s.status }}
                  </span>
                </div>
                <div style="margin: 0.35rem 0; color: #334155;"><em>"{{ s.details }}"</em></div>
                <div *ngIf="s.responseNotes" style="color: #0f766e;">
                  <strong>Jawaban Resmi Pokja:</strong> {{ s.responseNotes }}
                </div>
                <button
                  *ngIf="s.status === 'PENDING'"
                  type="button"
                  class="eproc-btn"
                  style="margin-top: 0.5rem;"
                  (click)="rejectSanggahAndProceedToSppbj(s.id)">
                  Tolak Sanggahan (Sesuai Aturan HEA TKDN) & Lanjut ke Penerbitan SPPBJ
                </button>
              </div>
            </div>
          </section>

          <!-- TAB 11 (FASE 4): E-KATALOG INTERNAL, KONTRAK PAYUNG & CALL-OFF ORDER -->
          <section *ngIf="activeTab() === 'ECATALOG_CALL_OFF'">
            <div class="eproc-card">
              <h3 style="margin-top: 0;">1. Daftar Kontrak Payung Aktif (Umbrella Agreements) & Pemantauan Kuota Plafon</h3>
              <table class="eproc-table">
                <thead>
                  <tr>
                    <th>No. Kontrak Payung & Judul</th>
                    <th>Penyedia Terkontrak</th>
                    <th>Plafon Kuota Total</th>
                    <th>Kuota Terpakai</th>
                    <th>Sisa Plafon Kuota</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let ua of umbrellaAgreements()">
                    <td>
                      <strong>{{ ua.agreementNumber }}</strong><br />
                      <small style="color: #64748b;">{{ ua.title }} (Berlaku s/d {{ ua.validUntil }})</small>
                    </td>
                    <td>{{ ua.vendorName }}</td>
                    <td>Rp {{ ua.totalQuotaCeiling | number:'1.0-0' }}</td>
                    <td>Rp {{ ua.consumedQuotaAmount | number:'1.0-0' }}</td>
                    <td><strong style="color: #166534;">Rp {{ ua.remainingQuotaAmount | number:'1.0-0' }}</strong></td>
                    <td><span class="eproc-pill eproc-pill-success">{{ ua.status }}</span></td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div class="eproc-card">
              <h3 style="margin-top: 0;">2. Katalog Komoditas TKDN dengan Tiered Volume Pricing & Eksekusi Call-Off Order (E-Purchasing)</h3>
              <table class="eproc-table" style="margin-bottom: 1rem;">
                <thead>
                  <tr>
                    <th>Kode KIMAP & Nama Barang</th>
                    <th>Sertifikat TKDN</th>
                    <th>Skema Tiered Volume Pricing (Grosir Lintas Cabang)</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let item of catalogItems()">
                    <td>
                      <strong>{{ item.kimapCode }}</strong> — {{ item.itemName }}<br />
                      <small style="color: #64748b;">Vendor: {{ item.vendorName }} | Kontrak Payung: {{ item.agreementNumber }} | Lead Time: {{ item.leadTimeDays }} Hari</small>
                    </td>
                    <td>
                      <span class="eproc-pill eproc-pill-success">TKDN {{ item.verifiedTkdnPct }}%</span><br />
                      <small>{{ item.tkdnCertNo }}</small>
                    </td>
                    <td>
                      <div *ngFor="let t of item.tiers" style="font-size: 0.77rem;">
                        • Qty {{ t.minQty }} – {{ t.maxQty ?? '∞' }} {{ item.uom }}:
                        <strong>Rp {{ t.unitPrice | number:'1.0-0' }}/{{ item.uom }}</strong>
                        <span *ngIf="t.discountPct > 0" style="color: #166534;"> (Diskon Grosir -{{ t.discountPct }}%)</span>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>

              <div class="eproc-grid-2" style="background: #f8fafc; padding: 0.9rem; border-radius: 8px; border: 1px solid #cbd5e1;">
                <div>
                  <label style="font-size: 0.78rem; font-weight: 600;">Pilih Item E-Katalog:</label>
                  <select class="eproc-input" [(ngModel)]="callOffForm.catalogItemId">
                    <option *ngFor="let item of catalogItems()" [value]="item.id">
                      {{ item.kimapCode }} — {{ item.itemName }}
                    </option>
                  </select>
                </div>
                <div>
                  <label style="font-size: 0.78rem; font-weight: 600;">Unit / Cabang Pemesan:</label>
                  <input class="eproc-input" [(ngModel)]="callOffForm.branchCode" />
                </div>
                <div>
                  <label style="font-size: 0.78rem; font-weight: 600;">Kuantitas Pesanan (Otomatis Hitung Harga Tier):</label>
                  <input type="number" class="eproc-input" [(ngModel)]="callOffForm.orderedQty" />
                </div>
                <div style="display: flex; flex-direction: column; justify-content: center;">
                  <div style="font-size: 0.8rem;">
                    Harga Satuan Tier Aktif: <strong>Rp {{ previewCallOffTier().unitPrice | number:'1.0-0' }}</strong> (Diskon {{ previewCallOffTier().discountPct }}%)
                  </div>
                  <div style="font-size: 0.88rem; color: #0f766e; font-weight: 700;">
                    Total Nilai PO Call-Off: Rp {{ previewCallOffTotal() | number:'1.0-0' }}
                  </div>
                </div>
              </div>
              <button type="button" class="eproc-btn" style="margin-top: 0.75rem;" (click)="createCallOffOrder()">
                Terbitkan Call-Off Purchase Order & Potong Plafon Kuota Kontrak Payung
              </button>

              <h4 style="margin-bottom: 0.4rem; margin-top: 1rem;">Riwayat Call-Off Purchase Order (E-Purchasing)</h4>
              <table class="eproc-table">
                <thead>
                  <tr>
                    <th>No. PO Call-Off</th>
                    <th>Cabang</th>
                    <th>Item KIMAP</th>
                    <th>Qty</th>
                    <th>Harga Satuan Tier</th>
                    <th>Total PO</th>
                    <th>Waktu</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let po of callOffOrders()">
                    <td><strong>{{ po.poNumber }}</strong></td>
                    <td>{{ po.branchCode }}</td>
                    <td>{{ po.kimapCode }} — {{ po.itemName }}</td>
                    <td>{{ po.orderedQty }} {{ po.uom }}</td>
                    <td>Rp {{ po.appliedUnitPrice | number:'1.0-0' }} (-{{ po.discountPct }}%)</td>
                    <td><strong>Rp {{ po.totalOrderAmount | number:'1.0-0' }}</strong></td>
                    <td>{{ po.orderedAt }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

          <!-- TAB 12 (FASE 4): MANAJEMEN KONTRAK SIKLUS PENUH (e-CLM) -->
          <section *ngIf="activeTab() === 'ECLM_LIFECYCLE'">
            <div class="eproc-card">
              <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap;">
                <div>
                  <h3 style="margin: 0;">Siklus Kontrak Elektronik (e-CLM): {{ activeContract().contractNumber }}</h3>
                  <div style="font-size: 0.8rem; color: #475569; margin-top: 0.2rem;">
                    SPPBJ: <code>{{ activeContract().sppbjNumber }}</code> | Vendor: <strong>{{ activeContract().vendorName }}</strong> | Komitmen TKDN: <strong>{{ activeContract().committedTkdnPct }}%</strong>
                  </div>
                </div>
                <span class="eproc-pill eproc-pill-success" style="font-size: 0.82rem;">
                  STATUS: {{ activeContract().lifecycleStatus }}
                </span>
              </div>

              <div class="eproc-grid-3" style="margin-top: 0.85rem;">
                <div style="background: #f8fafc; padding: 0.75rem; border-radius: 8px; border: 1px solid #cbd5e1;">
                  <div style="font-size: 0.75rem; color: #64748b;">Nilai Kontrak Awal (Hasil BAFO)</div>
                  <div style="font-size: 1rem; font-weight: 700;">Rp {{ activeContract().initialContractAmount | number:'1.0-0' }}</div>
                  <small>HPS Acuan: Rp {{ activeContract().referenceHpsAmount | number:'1.0-0' }}</small>
                </div>
                <div style="background: #f8fafc; padding: 0.75rem; border-radius: 8px; border: 1px solid #cbd5e1;">
                  <div style="font-size: 0.75rem; color: #64748b;">Kewajiban Jaminan Pelaksanaan (5%)</div>
                  <div style="font-size: 1rem; font-weight: 700; color: #b45309;">Rp {{ activeContract().requiredPerformanceBondAmount | number:'1.0-0' }}</div>
                  <small>Status Bank Garansi: <strong>{{ activeContract().performanceBondStatus }}</strong></small>
                </div>
                <div style="background: #f0fdf4; padding: 0.75rem; border-radius: 8px; border: 1px solid #86efac;">
                  <div style="font-size: 0.75rem; color: #166534;">Nilai Kontrak Aktif (Setelah Addendum/CCO)</div>
                  <div style="font-size: 1rem; font-weight: 700; color: #166534;">Rp {{ activeContract().currentActiveContractAmount | number:'1.0-0' }}</div>
                  <small>No. SPMK: <strong>{{ activeContract().spmkNumber || 'BELUM DITERBITKAN' }}</strong></small>
                </div>
              </div>

              <div *ngIf="clmGuardMessage()" style="margin-top: 0.75rem; padding: 0.65rem 0.9rem; border-radius: 6px; background: #fef2f2; border: 1px solid #f87171; color: #991b1b; font-size: 0.81rem;">
                <strong>Proteksi Kepatuhan e-CLM:</strong> {{ clmGuardMessage() }}
              </div>
            </div>

            <div class="eproc-grid-2">
              <!-- REDLINING & VERIFIKASI JAMINAN PELAKSANAAN 5% -->
              <div class="eproc-card">
                <h4 style="margin-top: 0;">1. Contract Redlining & Verifikasi Jaminan Pelaksanaan (Performance Bond 5%)</h4>
                <div *ngFor="let r of activeContract().redlines" style="font-size: 0.78rem; padding: 0.45rem; border-bottom: 1px solid #e2e8f0;">
                  <strong>{{ r.versionLabel }}</strong> — <em>{{ r.clauseRef }}</em><br />
                  {{ r.notes }}
                </div>
                <button type="button" class="eproc-btn-outline" style="margin-top: 0.5rem; width: 100%;" (click)="addVendorContractRedline()">
                  + Tambah Revisi Klausul Legal (Contract Redlining v1.1)
                </button>

                <hr style="margin: 0.85rem 0; border: 0; border-top: 1px solid #e2e8f0;" />
                <label style="font-size: 0.78rem; font-weight: 600;">No. Bank Garansi Himbara / BUMN:</label>
                <input class="eproc-input" [(ngModel)]="bondForm.guaranteeNumber" style="margin-bottom: 0.45rem;" />
                <label style="font-size: 0.78rem; font-weight: 600;">Nominal Jaminan Pelaksanaan (Min Rp {{ activeContract().requiredPerformanceBondAmount | number:'1.0-0' }}):</label>
                <input type="number" class="eproc-input" [(ngModel)]="bondForm.bondAmount" />
                <button type="button" class="eproc-btn" style="margin-top: 0.6rem; width: 100%;" (click)="verifyPerformanceBond()">
                  Verifikasi Bank Garansi Jaminan Pelaksanaan (5%)
                </button>
              </div>

              <!-- TTE BSrE/PSrE, e-METERAI PERURI & PENERBITAN SPMK -->
              <div class="eproc-card">
                <h4 style="margin-top: 0;">2. Pembubuhan e-Meterai Peruri, TTE BSrE/PSrE & Penerbitan SPMK</h4>
                <div style="font-size: 0.79rem; margin-bottom: 0.65rem;">
                  <div>No. Serial e-Meterai Peruri: <code>{{ activeContract().emeteraiSerial || 'BELUM DIBUBUHI' }}</code></div>
                  <div>Hash Dokumen Kontrak TTE: <code>{{ activeContract().signedPdfHash || 'BELUM DITANDATANGANI' }}</code></div>
                </div>
                <button type="button" class="eproc-btn" style="width: 100%; margin-bottom: 0.5rem;" (click)="signContractWithTteAndEmeterai()">
                  Bubuhkan e-Meterai Peruri & Tanda Tangan Elektronik (TTE BSrE/PSrE)
                </button>
                <button type="button" class="eproc-btn" style="width: 100%; background: #0f766e;" (click)="issueContractSpmk()">
                  Terbitkan Surat Perintah Mulai Kerja (SPMK) — Wajib Bond + TTE
                </button>

                <hr style="margin: 0.85rem 0; border: 0; border-top: 1px solid #e2e8f0;" />
                <h4 style="margin: 0 0 0.45rem 0;">3. Addendum / Contract Change Order (CCO — Plafon Maks +10%)</h4>
                <label style="font-size: 0.78rem; font-weight: 600;">Nilai Tambah/Kurang Pekerjaan CCO (Rp):</label>
                <input type="number" class="eproc-input" [(ngModel)]="ccoForm.deltaAmount" style="margin-bottom: 0.45rem;" />
                <label style="font-size: 0.78rem; font-weight: 600;">Alasan Teknis CCO / Addendum:</label>
                <input class="eproc-input" [(ngModel)]="ccoForm.justification" />
                <button type="button" class="eproc-btn" style="margin-top: 0.6rem; width: 100%;" (click)="submitContractAddendumCco()">
                  Ajukan & Sahkan Addendum / CCO (Uji Guard Plafon 10%)
                </button>

                <div *ngFor="let cco of activeContract().addendums" style="margin-top: 0.55rem; font-size: 0.78rem; background: #f0fdf4; padding: 0.5rem; border-radius: 6px;">
                  <strong>{{ cco.addendumNumber }}</strong>: +Rp {{ cco.deltaAmount | number:'1.0-0' }} (Kumulatif <strong>+{{ cco.cumulativeIncreasePct }}%</strong>) → Kontrak Baru: Rp {{ cco.newAmount | number:'1.0-0' }}
                </div>
              </div>
            </div>
          </section>

          <!-- TAB 13 (FASE 5): EKSEKUSI PROYEK, KURVA S (SCM I/II/III), BAST & POST-AUDIT TKDN -->
          <section *ngIf="activeTab() === 'EXECUTION_SCURVE_BAST'">
            <div class="eproc-grid-2">
              <!-- KURVA S KONSTRUKSI & DETEKSI KONTRAK KRITIS (SCM I/II/III) -->
              <div class="eproc-card">
                <h3 style="margin-top: 0;">1. Pemantauan Kurva S Konstruksi & Eskalasi Show Cause Meeting (SCM I/II/III)</h3>
                <div style="font-size: 0.78rem; color: #475569; margin-bottom: 0.6rem;">
                  Aturan Kontrak Kritis: Periode Rencana 0%–70% kritis bila deviasi &le; -10% | Periode 70%–100% kritis bila deviasi &le; -5%.
                </div>
                <div class="eproc-grid-2">
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">Minggu Ke-:</label>
                    <input type="number" class="eproc-input" [(ngModel)]="scurveForm.weekNumber" />
                  </div>
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">Status SCM Saat Ini:</label>
                    <select class="eproc-input" [(ngModel)]="scurveForm.currentScm">
                      <option value="NONE">NONE (Normal)</option>
                      <option value="SCM_LEVEL_1">SCM Tingkat I (PPK)</option>
                      <option value="SCM_LEVEL_2">SCM Tingkat II (GM/VP)</option>
                    </select>
                  </div>
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">Progres Rencana Kumulatif (%):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="scurveForm.plannedPct" />
                  </div>
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">Progres Realisasi Fisik (%):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="scurveForm.actualPct" />
                  </div>
                </div>
                <button type="button" class="eproc-btn" style="margin-top: 0.65rem; width: 100%;" (click)="evaluateSCurveProgress()">
                  Evaluasi Deviasi Kurva S & Cek Eskalasi SCM
                </button>

                <table class="eproc-table" style="margin-top: 0.75rem;">
                  <thead>
                    <tr>
                      <th>Minggu</th>
                      <th>Rencana vs Realisasi</th>
                      <th>Deviasi</th>
                      <th>Status Kontrak / SCM</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let sc of scurveLogs()">
                      <td>Mg #{{ sc.weekNumber }}</td>
                      <td>{{ sc.plannedPct }}% vs <strong>{{ sc.actualPct }}%</strong></td>
                      <td [style.color]="sc.deviationPct < 0 ? '#b91c1c' : '#166534'">
                        <strong>{{ sc.deviationPct }}%</strong>
                      </td>
                      <td>
                        <span class="eproc-pill" [ngClass]="sc.isCritical ? 'eproc-pill-danger' : 'eproc-pill-success'">
                          {{ sc.triggeredScm }}
                        </span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- BAST / PHO & POST-AUDIT VERIFIKASI REALISASI TKDN -->
              <div class="eproc-card">
                <h3 style="margin-top: 0;">2. Penerbitan BAST / PHO & Post-Audit Realisasi TKDN (Surveyor Independen)</h3>
                <div style="background: #f8fafc; padding: 0.8rem; border-radius: 8px; border: 1px solid #cbd5e1; font-size: 0.8rem;">
                  <div>No. BAST: <strong>{{ bastState().bastNumber }}</strong> ({{ bastState().handoverType }})</div>
                  <div>Lembaga Verifikator: <strong>{{ bastState().surveyor }}</strong> (No. LHV: <code>{{ bastState().reportNo }}</code>)</div>
                  <div>Nilai Kontrak Total: <strong>Rp {{ bastState().contractValue | number:'1.0-0' }}</strong> | Nilai BAST Bruto: <strong>Rp {{ bastState().approvedGrossValue | number:'1.0-0' }}</strong></div>
                </div>

                <div class="eproc-grid-2" style="margin-top: 0.65rem;">
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">Komitmen TKDN Saat Tender (%):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="postAuditTkdnForm.committedPct" />
                  </div>
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">Realisasi TKDN Hasil Post-Audit (%):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="postAuditTkdnForm.actualPct" />
                  </div>
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">Hari Keterlambatan Serah Terima (Kalender):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="postAuditTkdnForm.delayDays" />
                  </div>
                  <div style="display: flex; flex-direction: column; justify-content: center;">
                    <div style="font-size: 0.77rem;">Shortfall TKDN: <strong style="color: #b91c1c;">{{ previewTkdnShortfallPct() }}%</strong></div>
                    <div style="font-size: 0.82rem; color: #b91c1c; font-weight: 700;">
                      Sanksi Finansial TKDN: Rp {{ previewTkdnSanctionAmount() | number:'1.0-0' }}
                    </div>
                  </div>
                </div>
                <button type="button" class="eproc-btn" style="margin-top: 0.65rem; width: 100%;" (click)="updateBastAndPostAuditTkdn()">
                  Sahkan BAST & Kunci Sanksi Post-Audit TKDN + Hari Keterlambatan
                </button>
              </div>
            </div>
          </section>

          <!-- TAB 14 (FASE 5): 3-WAY MATCHING E-INVOICING, PAJAK WAPU BUMN, BAP & RAPOR VPE -->
          <section *ngIf="activeTab() === 'FINANCE_3WAY_WAPU_VPE'">
            <div class="eproc-grid-2">
              <!-- 3-WAY MATCHING E-INVOICE, DENDA 1‰, PAJAK WAPU 030. & BAP -->
              <div class="eproc-card">
                <h3 style="margin-top: 0;">1. Engine 3-Way Matching e-Invoicing, Pajak WAPU BUMN (030.) & Penerbitan BAP</h3>
                <div *ngIf="financeGuardMessage()" style="margin-bottom: 0.65rem; padding: 0.6rem; border-radius: 6px; background: #fef2f2; border: 1px solid #f87171; color: #991b1b; font-size: 0.8rem;">
                  <strong>Guard Perpajakan & 3-Way Match:</strong> {{ financeGuardMessage() }}
                </div>

                <div class="eproc-grid-2">
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">No. Seri Faktur Pajak (Wajib 030. untuk WAPU):</label>
                    <input class="eproc-input" [(ngModel)]="invoiceForm.fakturNo" />
                  </div>
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">Nilai DPP Diklaim Vendor (Rp):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="invoiceForm.claimedDpp" />
                  </div>
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">Potongan Pengembalian DP (%):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="invoiceForm.dpRecoupmentPct" />
                  </div>
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">Potongan Retensi Pemeliharaan (%):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="invoiceForm.retentionPct" />
                  </div>
                </div>

                <button type="button" class="eproc-btn" style="margin-top: 0.65rem; width: 100%;" (click)="executeThreeWayMatchingAndIssueBap()">
                  Jalankan 3-Way Matching, Potong Denda/Pajak WAPU & Terbitkan BAP + SP2D
                </button>

                <div style="margin-top: 0.8rem; background: #f8fafc; padding: 0.8rem; border-radius: 8px; border: 1px solid #cbd5e1; font-size: 0.79rem;">
                  <div style="display: flex; justify-content: space-between; margin-bottom: 0.3rem;">
                    <span>Status 3-Way Matching & No. BAP:</span>
                    <span class="eproc-pill" [ngClass]="invoiceSettlement().isMatched ? 'eproc-pill-success' : 'eproc-pill-danger'">
                      {{ invoiceSettlement().bapNumber }}
                    </span>
                  </div>
                  <div>• DPP Bruto Sesuai BAST: <strong>Rp {{ invoiceSettlement().dppGross | number:'1.0-0' }}</strong></div>
                  <div>• Potongan Pengembalian Uang Muka (DP): -Rp {{ invoiceSettlement().dpDeduction | number:'1.0-0' }}</div>
                  <div>• Potongan Retensi Pemeliharaan (5%): -Rp {{ invoiceSettlement().retentionDeduction | number:'1.0-0' }}</div>
                  <div style="color: #b91c1c;">• Denda Keterlambatan (1‰/hari, Maks 5%): -Rp {{ invoiceSettlement().latePenalty | number:'1.0-0' }}</div>
                  <div style="color: #b91c1c;">• Sanksi Kekurangan Realisasi TKDN: -Rp {{ invoiceSettlement().tkdnPenalty | number:'1.0-0' }}</div>
                  <div>• PPN 11% (Faktur <code>030.</code>): Rp {{ invoiceSettlement().ppnAmount | number:'1.0-0' }} <em>(Dipungut langsung oleh WAPU BUMN ke Kas Negara)</em></div>
                  <div>• PPh Pasal 22/23 Dipotong (1.5%): -Rp {{ invoiceSettlement().pphWithheld | number:'1.0-0' }}</div>
                  <hr style="margin: 0.5rem 0; border: 0; border-top: 1px solid #cbd5e1;" />
                  <div style="font-size: 0.92rem; color: #166534; font-weight: 700;">
                    Nominal Bersih Ditransfer ke Vendor (SP2D): Rp {{ invoiceSettlement().netPayableToVendor | number:'1.0-0' }}
                  </div>
                </div>
              </div>

              <!-- VENDOR PERFORMANCE EVALUATION (VPE 5 DIMENSI) -->
              <div class="eproc-card">
                <h3 style="margin-top: 0;">2. Evaluasi Kinerja Penyedia Akhir Kontrak (VPE 5 Dimensi)</h3>
                <div class="eproc-grid-2">
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">1. Kualitas Barang/Jasa (Bobot 30%):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="vpeForm.quality" />
                  </div>
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">2. Ketepatan Waktu / SLA (Bobot 25%):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="vpeForm.delivery" />
                  </div>
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">3. Kepatuhan K3L / CSMS (Bobot 20%):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="vpeForm.hseCsms" />
                  </div>
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">4. Realisasi Komitmen TKDN (Bobot 15%):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="vpeForm.tkdn" />
                  </div>
                  <div>
                    <label style="font-size: 0.78rem; font-weight: 600;">5. Layanan & Responsivitas (Bobot 10%):</label>
                    <input type="number" class="eproc-input" [(ngModel)]="vpeForm.responsiveness" />
                  </div>
                  <div style="display: flex; flex-direction: column; justify-content: center; background: #f0fdf4; padding: 0.65rem; border-radius: 8px; border: 1px solid #86efac;">
                    <div style="font-size: 0.76rem; color: #166534;">Skor Akhir VPE & Predikat AVL:</div>
                    <div style="font-size: 1.05rem; font-weight: 700; color: #166534;">
                      {{ calculatedVpeScore() | number:'1.2-2' }} — {{ calculatedVpeGrade().grade }}
                    </div>
                    <small>{{ calculatedVpeGrade().recommendation }}</small>
                  </div>
                </div>
                <button type="button" class="eproc-btn" style="margin-top: 0.75rem; width: 100%;" (click)="publishVpeScorecardToAvl()">
                  Simpan Rapor VPE & Perbarui Predikat Vendor di Direktori AVL
                </button>
              </div>
            </div>
          </section>

          <!-- TAB 15: IMMUTABLE AUDIT TRAIL (SHA-256 HASH CHAIN) -->
          <section *ngIf="activeTab() === 'AUDIT_CHAIN'" class="eproc-card">
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <h3 style="margin: 0;">Immutable Audit Trail (SHA-256 Hash-Chained Forensic Log)</h3>
              <span class="eproc-pill eproc-pill-success">✓ INTEGRITAS HASH-CHAIN TERVERIFIKASI ({{ auditLogs().length }} Blok)</span>
            </div>
            <table class="eproc-table" style="margin-top: 0.75rem;">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Waktu (UTC)</th>
                  <th>Role Aktor</th>
                  <th>Modul & Aksi</th>
                  <th>Entitas</th>
                  <th>Prev Record Hash</th>
                  <th>Current Record Hash</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let log of auditLogs()">
                  <td>{{ log.seq }}</td>
                  <td>{{ log.timestamp }}</td>
                  <td><code>{{ log.actorRole }}</code></td>
                  <td><strong>{{ log.module }}</strong>: {{ log.action }}</td>
                  <td>{{ log.entityRef }}</td>
                  <td><code style="font-size: 0.72rem; color: #64748b;">{{ log.prevHash.slice(0, 16) }}...</code></td>
                  <td><code style="font-size: 0.72rem; color: #0f766e;">{{ log.currentHash.slice(0, 16) }}...</code></td>
                </tr>
              </tbody>
            </table>
          </section>
        </main>
      </div>
    </div>
  `,
})
export class EprocAppComponent {
  readonly configService = inject(EprocConfigService);

  // State Klien BUMN
  readonly selectedClientEnv = signal<'BUMN_KARYA' | 'BUMN_ENERGI'>('BUMN_KARYA');
  readonly activeClientCode = signal('BUMN_KARYA_01');
  readonly activeClientName = signal('PT BUMN Karya Persero Tbk');
  readonly activeClientNpwp = signal('01.001.002.3-093.000');
  readonly activeRegulation = signal('PER-2/MBU/03/2023');

  // State Portal, Role Persona, & Tab Kerja
  readonly activePortal = signal<EprocPortalType>('INTERNAL_BUMN_PORTAL');
  readonly activeRoleCode = signal<string>('POKJA_COMMITTEE');
  readonly activeTab = signal<ActiveWorkspaceTab>('TENDER_BID_VAULT');

  readonly personas: RolePersonaItem[] = [
    {
      roleCode: 'VMS_VERIFIER',
      roleName: 'Verifikator Kualifikasi & Legalitas Vendor (VMS)',
      portalTarget: 'INTERNAL_BUMN_PORTAL',
      positionGrade: 'BOD-3_MANAGER',
      sodConflicts: ['PPK', 'POKJA_COMMITTEE', 'VENDOR_ADMIN'],
      grantedPermissions: ['eproc.internal.vms_admin.verify_legal'],
    },
    {
      roleCode: 'HSE_CSMS_AUDITOR',
      roleName: 'Auditor K3L / HSE (CSMS Certification)',
      portalTarget: 'INTERNAL_BUMN_PORTAL',
      positionGrade: 'BOD-3_MANAGER',
      sodConflicts: ['VENDOR_ADMIN'],
      grantedPermissions: ['eproc.internal.vms_admin.audit_csms'],
    },
    {
      roleCode: 'PPK',
      roleName: 'Pejabat Pembuat Komitmen (Project Owner / Estimator)',
      portalTarget: 'INTERNAL_BUMN_PORTAL',
      positionGrade: 'BOD-2_VP_GM',
      sodConflicts: ['POKJA_COMMITTEE', 'BUYER_PEJABAT_PENGADAAN', 'VMS_VERIFIER', 'FINANCE_AP'],
      grantedPermissions: ['eproc.internal.planning.seal_hps', 'eproc.internal.eclm.issue_sppbj'],
    },
    {
      roleCode: 'POKJA_COMMITTEE',
      roleName: 'Kelompok Kerja Pemilihan (Panitia Tender UKPBJ)',
      portalTarget: 'INTERNAL_BUMN_PORTAL',
      positionGrade: 'COMMITTEE_SPECIALIST',
      sodConflicts: ['PA_KPA', 'PPK', 'FINANCE_AP', 'VENDOR_ADMIN'],
      grantedPermissions: ['eproc.internal.esourcing.open_envelopes', 'eproc.internal.esourcing.calculate_hea'],
    },
    {
      roleCode: 'PA_KPA',
      roleName: 'Direksi / Pengguna Anggaran (DOA Approver)',
      portalTarget: 'INTERNAL_BUMN_PORTAL',
      positionGrade: 'BOARD_OF_DIRECTORS',
      sodConflicts: ['POKJA_COMMITTEE', 'VENDOR_ADMIN'],
      grantedPermissions: ['eproc.internal.doa_inbox.approve'],
    },
    {
      roleCode: 'FINANCE_AP',
      roleName: 'Verifikator Keuangan & Pajak WAPU BUMN',
      portalTarget: 'INTERNAL_BUMN_PORTAL',
      positionGrade: 'BOD-3_MANAGER',
      sodConflicts: ['PPK', 'POKJA_COMMITTEE', 'WAREHOUSE_QC', 'VENDOR_ADMIN'],
      grantedPermissions: ['eproc.internal.finance.three_way_match'],
    },
    {
      roleCode: 'INTERNAL_AUDITOR',
      roleName: 'Satuan Pengawasan Intern (SPI) / Auditor BPK',
      portalTarget: 'INTERNAL_BUMN_PORTAL',
      positionGrade: 'AUDITOR_INSPECTOR',
      sodConflicts: ['PA_KPA', 'PPK', 'POKJA_COMMITTEE', 'FINANCE_AP'],
      grantedPermissions: ['eproc.internal.sysadmin.audit_hash_chain'],
    },
    {
      roleCode: 'VENDOR_ADMIN',
      roleName: 'Direktur / Admin Perusahaan Penyedia (Portal Vendor)',
      portalTarget: 'VENDOR_PORTAL',
      positionGrade: 'EXTERNAL_PROVIDER',
      sodConflicts: ['PA_KPA', 'PPK', 'POKJA_COMMITTEE', 'VMS_VERIFIER', 'FINANCE_AP'],
      grantedPermissions: ['eproc.vendor_portal.dashboard', 'eproc.vendor_portal.vms_profile'],
    },
  ];

  // State Fase 1: Daftar Vendor
  readonly vendors = signal<VendorViewModel[]>([
    {
      id: 'v-001',
      vendorCode: 'DRT-2026-0101',
      legalEntityType: 'PT',
      companyName: 'PT Karya Infrastruktur Nusantara Tbk',
      businessScale: 'BESAR',
      isBumnAffiliated: true,
      isPadiUmkm: false,
      nibNumber: '9120300451821',
      npwpNumber: '01.302.441.8-093.000',
      cityName: 'Jakarta Selatan',
      verificationStatus: 'VERIFIED_AVL',
      vpeGrade: 'PLATINUM',
      netWorthEquityKb: 105_000_000_000,
      highestExperienceNpt: 45_000_000_000,
      kemampuanDasarKd: 135_000_000_000,
      sisaKemampuanNyataSkn: 582_000_000_000,
      sisaKemampuanPaketSkp: 6,
      altmanZScore: 4.82,
      financialHealthZone: 'SAFE_ZONE',
      shareholderOwnerName: 'Ir. Hendra Wijaya, M.Eng',
      shareholderNik: '3174011508750001',
      csmsLevel: 'EXTREME',
      csmsScore: 90.85,
      csmsCertNo: 'CSMS-BUMN-2026-8841',
    },
    {
      id: 'v-002',
      vendorCode: 'DRT-2026-0102',
      legalEntityType: 'PT',
      companyName: 'PT Mitra Teknik Manufaktur Lokal',
      businessScale: 'MENENGAH',
      isBumnAffiliated: false,
      isPadiUmkm: true,
      nibNumber: '8120019283741',
      npwpNumber: '02.415.778.1-412.000',
      cityName: 'Surabaya',
      verificationStatus: 'VERIFIED_AVL',
      vpeGrade: 'GOLD',
      netWorthEquityKb: 15_000_000_000,
      highestExperienceNpt: 8_000_000_000,
      kemampuanDasarKd: 24_000_000_000,
      sisaKemampuanNyataSkn: 53_000_000_000,
      sisaKemampuanPaketSkp: 5,
      altmanZScore: 5.14,
      financialHealthZone: 'SAFE_ZONE',
      shareholderOwnerName: 'Surya Darmawan, S.E.',
      shareholderNik: '3578042209800003',
      csmsLevel: 'HIGH',
      csmsScore: 77.2,
      csmsCertNo: 'CSMS-BUMN-2026-4102',
    },
    {
      id: 'v-003',
      vendorCode: 'DRT-2026-0103',
      legalEntityType: 'CV',
      companyName: 'CV Cipta Sarana Elektrikal',
      businessScale: 'KECIL',
      isBumnAffiliated: false,
      isPadiUmkm: true,
      nibNumber: '0220109928312',
      npwpNumber: '03.889.112.4-412.000',
      cityName: 'Sidoarjo',
      verificationStatus: 'SUBMITTED',
      vpeGrade: 'UNRATED',
      netWorthEquityKb: 2_200_000_000,
      highestExperienceNpt: 1_500_000_000,
      kemampuanDasarKd: 4_500_000_000,
      sisaKemampuanNyataSkn: 2_500_000_000,
      sisaKemampuanPaketSkp: 4,
      altmanZScore: 3.12,
      financialHealthZone: 'SAFE_ZONE',
      shareholderOwnerName: 'Surya Darmawan, S.E.',
      shareholderNik: '3578042209800003',
      csmsLevel: 'LOW',
      csmsScore: 45.0,
    },
  ]);

  // State Fase 2: Pagu RKAP & PR
  readonly rkapBudgets = signal<RkapBudgetViewModel[]>([
    {
      id: 'rkap-capex-01',
      costCenterCode: 'CC-INFRA-001',
      costCenterName: 'Divisi Infrastruktur & EPC Strategis',
      budgetType: 'CAPEX',
      wbsElementGlAccount: 'WBS-2026-CAPEX-EPC-01',
      budgetName: 'Pagu Investasi Pembangunan Gardu & Stasiun Kompresor',
      revisedAmount: 150_000_000_000,
      committedAmount: 18_500_000_000,
      actualSpentAmount: 12_000_000_000,
      availableAmount: 119_500_000_000,
    },
    {
      id: 'rkap-opex-02',
      costCenterCode: 'CC-OPS-002',
      costCenterName: 'Divisi Operasional & Pemeliharaan Regional',
      budgetType: 'OPEX',
      wbsElementGlAccount: 'GL-510200-OPEX-VALVE',
      budgetName: 'Pagu Pengadaan Suku Cadang Industrial Valve & Pompa',
      revisedAmount: 45_000_000_000,
      committedAmount: 6_250_000_000,
      actualSpentAmount: 5_000_000_000,
      availableAmount: 33_750_000_000,
    },
  ]);

  readonly purchaseRequisitions = signal<PrViewModel[]>([
    {
      id: 'pr-01',
      prNumber: 'PR-2026-0201',
      prTitle: 'Kebutuhan Gate Valve API 600 DN300 — Regional Jawa Barat',
      rkapBudgetId: 'rkap-opex-02',
      wbsCode: 'GL-510200-OPEX-VALVE',
      requesterUnitName: 'Cabang Regional Jawa Barat (Bandung)',
      category: 'GOODS',
      materialCode: 'KIMAP-VLV-600-DN300',
      itemName: 'Gate Valve Carbon Steel API 600 Class 300 DN300',
      quantity: 20,
      uom: 'UNIT',
      unitPrice: 125_000_000,
      totalValue: 2_500_000_000,
      csmsLevel: 'MEDIUM',
      status: 'APPROVED_READY_FOR_SOURCING',
      selectedForConsolidation: true,
    },
    {
      id: 'pr-02',
      prNumber: 'PR-2026-0202',
      prTitle: 'Kebutuhan Gate Valve API 600 DN300 — Regional Jawa Timur',
      rkapBudgetId: 'rkap-opex-02',
      wbsCode: 'GL-510200-OPEX-VALVE',
      requesterUnitName: 'Cabang Regional Jawa Timur (Surabaya)',
      category: 'GOODS',
      materialCode: 'KIMAP-VLV-600-DN300',
      itemName: 'Gate Valve Carbon Steel API 600 Class 300 DN300',
      quantity: 30,
      uom: 'UNIT',
      unitPrice: 125_000_000,
      totalValue: 3_750_000_000,
      csmsLevel: 'MEDIUM',
      status: 'APPROVED_READY_FOR_SOURCING',
      selectedForConsolidation: true,
    },
  ]);

  readonly packages = signal<PackageViewModel[]>([
    {
      id: 'pkg-epc-01',
      packageCode: 'PKG-2026-0301',
      packageName: 'Tender EPC Pembangunan Stasiun Kompresor & Metering System',
      category: 'CONSTRUCTION',
      sourcingMethod: 'TENDER_UMUM',
      envelopeMethod: '2_SAMPUL',
      evaluationMethod: 'SISTEM_NILAI_SCORING',
      totalPaguBudget: 18_500_000_000,
      minTkdnPct: 35,
      sourcePrNumbers: ['PR-2026-0203'],
      consolidatedQty: 1,
      uom: 'LOT',
      status: 'HPS_SEALED_WAITING_DOA',
      sealedHpsCipherPreview: 'a94f81c2e03b75d4918a2f6e3c019b84...',
      sealedHpsSha256: 'e49b1207c8f3a5619d204b7e8a1c3f592048d7b6a1e3c9f04827d1b5a9e3c602',
      doaInstanceId: 'doa-01',
    },
  ]);

  readonly doaInbox = signal<DoaInstanceViewModel[]>([
    {
      id: 'doa-01',
      packageId: 'pkg-epc-01',
      packageCode: 'PKG-2026-0301',
      packageTitle: 'Tender EPC Pembangunan Stasiun Kompresor & Metering System',
      transactionAmountIdr: 18_500_000_000,
      currentStepSequence: 2,
      totalStepsCount: 5,
      overallStatus: 'IN_PROGRESS',
      steps: [
        { stepSequence: 1, roleCode: 'PPK', positionGrade: 'BOD-3_MANAGER', action: 'APPROVED', actedBy: 'Pejabat Pembuat Komitmen', comments: 'HPS disegel kriptografis AES-256-GCM.' },
        { stepSequence: 2, roleCode: 'PA_KPA', positionGrade: 'BOD-2_VP_GM', action: 'PENDING' },
        { stepSequence: 3, roleCode: 'PA_KPA', positionGrade: 'BOD-1_SVP', action: 'PENDING' },
        { stepSequence: 4, roleCode: 'PA_KPA', positionGrade: 'DIRECTOR_BIDANG', action: 'PENDING' },
        { stepSequence: 5, roleCode: 'PA_KPA', positionGrade: 'BOARD_OF_DIRECTORS_COLLEGIATE', action: 'PENDING' },
      ],
    },
  ]);

  // State Fase 3: Tender Engine, Aanwijzing, Encrypted Bid Vault, HEA, e-Reverse Auction, BAFO & Sanggah
  readonly tenderHpsAmount = 6_000_000_000;
  readonly isBidVaultOpened = signal<boolean>(false);
  readonly eAuctionExtensionsCount = signal<number>(0);
  readonly tenderStageStatus = signal<string>('AANWIJZING_AND_BID_SUBMISSION');

  readonly aanwijzingList = signal<AanwijzingItemViewModel[]>([
    {
      maskedAlias: 'Peserta #01 (Disamarkan)',
      sectionRef: 'BAB IV - Spesifikasi Teknis & Sertifikat TKDN',
      question: 'Apakah sertifikat TKDN Kemenperin minimal 40% wajib dilampirkan pada Sampul 1 atau Sampul 2?',
      answer: 'Salinan sertifikat TKDN dilampirkan pada Sampul 1 (Teknis) dan persentase TKDN dideklarasikan pada Sampul 2 untuk perhitungan HEA.',
    },
  ]);

  newAanwijzing = {
    question: '',
    answer: '',
  };

  newBidForm = {
    vendorName: 'PT Nusantara Valveindo Perkasa',
    priceHp: 5_600_000_000,
    tkdnPct: 50,
    techScore: 86,
  };

  readonly bidVaultSubmissions = signal<BidVaultItemViewModel[]>([
    {
      participantId: 'part-01',
      receiptNo: 'BVAULT-2026-001',
      vendorName: 'PT Mitra Teknik Manufaktur Lokal',
      businessScale: 'MENENGAH (PaDi UMKM)',
      techScore: 88,
      techPassed: true,
      cipherPreview: '7f2e91a4c8d03b6591e2a4c7d801b3f5...',
      sha256Hash: '9a1c3e5b7d9f02468ace13579bdf02468ace13579bdf02468ace13579bdf0246',
      hiddenPriceHp: 5_700_000_000,
      hiddenTkdnPct: 60,
      hiddenBmpPct: 12,
      isOpened: false,
    },
    {
      participantId: 'part-02',
      receiptNo: 'BVAULT-2026-002',
      vendorName: 'PT Global Valve Importindo',
      businessScale: 'BESAR (Impor)',
      techScore: 84,
      techPassed: true,
      cipherPreview: '3b8d19f0e4a62c7509b1d4e8a2c6f039...',
      sha256Hash: '4b8d19f0e4a62c7509b1d4e8a2c6f0394b8d19f0e4a62c7509b1d4e8a2c6f039',
      hiddenPriceHp: 5_350_000_000,
      hiddenTkdnPct: 10,
      hiddenBmpPct: 0,
      isOpened: false,
    },
    {
      participantId: 'part-03',
      receiptNo: 'BVAULT-2026-003',
      vendorName: 'PT Karya Infrastruktur Nusantara Tbk',
      businessScale: 'BESAR (Sinergi BUMN)',
      techScore: 90,
      techPassed: true,
      cipherPreview: 'c19a47e2b8d05f3612a9c4e7b0d2f851...',
      sha256Hash: 'c19a47e2b8d05f3612a9c4e7b0d2f851c19a47e2b8d05f3612a9c4e7b0d2f851',
      hiddenPriceHp: 5_850_000_000,
      hiddenTkdnPct: 45,
      hiddenBmpPct: 15,
      isOpened: false,
    },
  ]);

  readonly eAuctionLogs = signal<EAuctionBidLogViewModel[]>([]);

  bafoForm = {
    bahpNumber: 'BAHP-UKPBJ-2026-088',
    finalBafoPrice: 5_200_000_000,
  };

  readonly sanggahList = signal<SanggahItemViewModel[]>([
    {
      id: 'sgh-01',
      vendorName: 'PT Global Valve Importindo',
      letterNo: 'SRT-SGH-2026-019',
      category: 'PERHITUNGAN PREFERENSI HARGA TKDN (HEA)',
      details: 'Mengajukan sanggah karena harga penawaran nominal kami lebih rendah (Rp 5,35 Miliar) dibanding pemenang (Rp 5,70 Miliar).',
      status: 'PENDING',
    },
  ]);

  hpsInputMap: Record<string, number> = {};
  readonly prErrorMsg = signal<string>('');

  newPrForm = {
    rkapBudgetId: 'rkap-opex-02',
    prTitle: 'Kebutuhan Gate Valve API 600 DN300 — Regional Sumatera Utara',
    requesterUnitName: 'Cabang Regional Sumatera Utara (Medan)',
    materialCode: 'KIMAP-VLV-600-DN300',
    itemName: 'Gate Valve Carbon Steel API 600 Class 300 DN300',
    quantity: 15,
    unitPrice: 125_000_000,
  };

  consolidationForm = {
    packageName: 'Pengadaan Konsolidasi Gate Valve API 600 DN300 Lintas Regional',
    envelopeMethod: '2_SAMPUL',
  };

  newVendorForm = {
    companyName: 'PT Solusi Energi Terbarukan',
    businessScale: 'MENENGAH' as 'KECIL' | 'MENENGAH' | 'BESAR',
    nibNumber: '9120400887123',
    npwpNumber: '04.512.990.1-093.000',
    shareholderOwnerName: 'Dr. Raka Pradipta',
    shareholderNik: '3273051904820005',
    netWorthEquityKb: 20_000_000_000,
    highestExperienceNpt: 12_000_000_000,
    ongoingContractsValue: 15_000_000_000,
  };

  selectedCsmsVendorId = 'v-003';
  csmsFieldScore = 82;
  csmsElements = [
    { no: 1, name: 'Kepemimpinan & Komitmen Manajemen K3L', weight: 15, score: 85 },
    { no: 2, name: 'Kebijakan & Sasaran Strategis HSE', weight: 10, score: 80 },
    { no: 3, name: 'Organisasi, Kompetensi AK3 & Sumber Daya', weight: 15, score: 78 },
    { no: 4, name: 'Manajemen Risiko (HIRADC & JSA)', weight: 15, score: 84 },
    { no: 5, name: 'Prosedur Operasi & Izin Kerja Aman (PTW)', weight: 15, score: 82 },
    { no: 6, name: 'Pemantauan Kinerja K3L & Zero Fatality', weight: 10, score: 88 },
    { no: 7, name: 'Tanggap Darurat (ERP) & Investigasi Insiden', weight: 10, score: 76 },
    { no: 8, name: 'Audit Internal SMK3 & ISO 45001', weight: 10, score: 80 },
  ];

  readonly auditLogs = signal<AuditHashItem[]>([
    {
      seq: 1,
      timestamp: '2026-11-13 09:00:12 UTC',
      actorRole: 'VENDOR_ADMIN',
      module: 'VMS',
      action: 'VENDOR_REGISTERED',
      entityRef: 'DRT-2026-0101',
      prevHash: '0000000000000000000000000000000000000000000000000000000000000000',
      currentHash: '8f4b2e91c7d30481a6b5e2c9f1047d823b9a1c5e6f2d80417a9c3e5b1d8f402a',
    },
    {
      seq: 2,
      timestamp: '2026-11-13 09:15:44 UTC',
      actorRole: 'VMS_VERIFIER',
      module: 'VMS',
      action: 'AVL_APPROVED',
      entityRef: 'DRT-2026-0101',
      prevHash: '8f4b2e91c7d30481a6b5e2c9f1047d823b9a1c5e6f2d80417a9c3e5b1d8f402a',
      currentHash: '3c9d1a74e8b20593f1d6a4c8e2095b714a8f2d6e9c3b10528d7e4a6c9b2f108d',
    },
    {
      seq: 3,
      timestamp: '2026-11-13 09:40:05 UTC',
      actorRole: 'PPK',
      module: 'PLANNING',
      action: 'HPS_SEALED_AES256GCM',
      entityRef: 'PKG-2026-0301',
      prevHash: '3c9d1a74e8b20593f1d6a4c8e2095b714a8f2d6e9c3b10528d7e4a6c9b2f108d',
      currentHash: '7a1e5c82b4d90376e2f8a1c5d9043b682e7f1a4d8c2b90635a1e7d4c8b2f903e',
    },
  ]);

  readonly currentPersona = computed(
    () => this.personas.find((p) => p.roleCode === this.activeRoleCode()) ?? this.personas[0]
  );

  readonly detectedBoConflicts = computed(() => {
    const list = this.vendors();
    const conflicts: Array<{ vendorA: string; vendorB: string; relationType: string; personName: string; nik: string }> = [];
    for (let i = 0; i < list.length; i++) {
      for (let j = i + 1; j < list.length; j++) {
        if (list[i].shareholderNik && list[i].shareholderNik === list[j].shareholderNik) {
          conflicts.push({
            vendorA: list[i].companyName,
            vendorB: list[j].companyName,
            relationType: 'SHARED_ULTIMATE_BENEFICIAL_OWNER',
            personName: list[i].shareholderOwnerName,
            nik: list[i].shareholderNik,
          });
        }
      }
    }
    return conflicts;
  });

  readonly rankedHeaSubmissions = computed(() => {
    const opened = this.bidVaultSubmissions().filter((s) => s.isOpened && s.techPassed);
    if (opened.length === 0) return [];

    const withHea = opened.map((s) => {
      const hp = s.decryptedPriceHp ?? s.hiddenPriceHp;
      const tkdn = s.declaredTkdnPct ?? s.hiddenTkdnPct;
      const kp = tkdn >= 25 ? (tkdn / 100) * 0.25 : 0;
      const hea = (1 / (1 + kp)) * hp;
      return { ...s, decryptedPriceHp: hp, declaredTkdnPct: tkdn, kp, calculatedHea: hea };
    });

    const lowestHea = Math.min(...withHea.map((x) => x.calculatedHea));
    const scored = withHea
      .map((x) => {
        const commScore = (lowestHea / x.calculatedHea) * 100;
        const combinedScore = x.techScore * 0.3 + commScore * 0.7;
        return { ...x, combinedScore };
      })
      .sort((a, b) => b.combinedScore - a.combinedScore);

    return scored.map((item, idx) => ({ ...item, rank: idx + 1 }));
  });

  readonly topRankedCandidate = computed(() => this.rankedHeaSubmissions()[0] ?? null);

  selectPortal(portal: EprocPortalType): void {
    this.activePortal.set(portal);
  }

  switchClientEnvironment(envKey: 'BUMN_KARYA' | 'BUMN_ENERGI'): void {
    this.selectedClientEnv.set(envKey);
    if (envKey === 'BUMN_KARYA') {
      this.activeClientCode.set('BUMN_KARYA_01');
      this.activeClientName.set('PT BUMN Karya Persero Tbk');
      this.activeClientNpwp.set('01.001.002.3-093.000');
      this.activeRegulation.set('PER-2/MBU/03/2023');
      document.documentElement.style.setProperty('--eproc-primary-color', '#0F4C81');
    } else {
      this.activeClientCode.set('BUMN_ENERGI_02');
      this.activeClientName.set('PT BUMN Energi Nusantara Persero');
      this.activeClientNpwp.set('01.002.888.9-093.000');
      this.activeRegulation.set('PER-2/MBU/03/2023 & SK Direksi No. Kpts-09/2025');
      document.documentElement.style.setProperty('--eproc-primary-color', '#006A4E');
    }
  }

  switchRolePersona(roleCode: string): void {
    this.activeRoleCode.set(roleCode);
    const persona = this.personas.find((p) => p.roleCode === roleCode);
    if (persona) {
      this.activePortal.set(persona.portalTarget);
    }
  }

  filteredMenus(): EprocMainMenuItem[] {
    return EPROC_NAVIGATION_MENUS.filter((m) => m.portal === this.activePortal());
  }

  isSubMenuAllowed(allowedRoles: string[]): boolean {
    return allowedRoles.includes(this.activeRoleCode());
  }

  // Fase 3: Tambah Pertanyaan & Jawaban Aanwijzing
  addAanwijzingItem(): void {
    if (!this.newAanwijzing.question) return;
    const nextIdx = this.aanwijzingList().length + 1;
    this.aanwijzingList.update((list) => [
      ...list,
      {
        maskedAlias: `Peserta #0${nextIdx} (Disamarkan)`,
        sectionRef: 'Dokumen Pemilihan RKS & BoQ',
        question: this.newAanwijzing.question,
        answer: this.newAanwijzing.answer || 'Telah dicatat dalam Berita Acara Penjelasan (BAP).',
      },
    ]);
    this.newAanwijzing = { question: '', answer: '' };
    this.appendAudit('POKJA_COMMITTEE', 'ESOURCING', 'AANWIJZING_BAP_UPDATED', 'TND-BUMN-2026-0401');
  }

  // Fase 3: Upload Sampul Penawaran Terenkripsi ke Bid Vault
  submitEncryptedBidToVault(): void {
    const count = this.bidVaultSubmissions().length + 1;
    const receipt = `BVAULT-2026-00${count}`;
    const cipher = Array.from({ length: 32 }, () => Math.floor(Math.random() * 16).toString(16)).join('') + '...';
    const sha = Array.from({ length: 64 }, () => Math.floor(Math.random() * 16).toString(16)).join('');

    const item: BidVaultItemViewModel = {
      participantId: `part-0${count}`,
      receiptNo: receipt,
      vendorName: this.newBidForm.vendorName,
      businessScale: 'MENENGAH',
      techScore: Number(this.newBidForm.techScore),
      techPassed: Number(this.newBidForm.techScore) >= 70,
      cipherPreview: cipher,
      sha256Hash: sha,
      hiddenPriceHp: Number(this.newBidForm.priceHp),
      hiddenTkdnPct: Number(this.newBidForm.tkdnPct),
      hiddenBmpPct: 10,
      isOpened: this.isBidVaultOpened(),
      decryptedPriceHp: this.isBidVaultOpened() ? Number(this.newBidForm.priceHp) : undefined,
      declaredTkdnPct: this.isBidVaultOpened() ? Number(this.newBidForm.tkdnPct) : undefined,
    };

    this.bidVaultSubmissions.update((list) => [...list, item]);
    this.appendAudit('VENDOR_ADMIN', 'BID_VAULT', 'BID_ENVELOPE_SEALED_AES256GCM', receipt);
  }

  // Fase 3: Bid Opening Ceremony & Dekripsi Sampul 2 Harga
  executeBidOpeningCeremony(): void {
    this.isBidVaultOpened.set(true);
    this.tenderStageStatus.set('ENVELOPE_2_OPENED_AND_E_AUCTION_LIVE');
    this.bidVaultSubmissions.update((list) =>
      list.map((s) =>
        s.techPassed
          ? {
              ...s,
              isOpened: true,
              decryptedPriceHp: s.hiddenPriceHp,
              declaredTkdnPct: s.hiddenTkdnPct,
            }
          : s
      )
    );
    this.appendAudit('POKJA_COMMITTEE', 'BID_OPENING', 'DECRYPTED_HPS_AND_ENVELOPE2_HEA', 'TND-BUMN-2026-0401');
    this.activeTab.set('HEA_EAUCTION_ROOM');
  }

  // Fase 3: Simulasi Penurunan Harga di Ruang e-Reverse Auction + Anti-Sniping Auto-Extend
  simulateEAuctionDropBid(participantId: string): void {
    let targetVendor = '';
    let newHp = 0;
    let newHea = 0;

    this.bidVaultSubmissions.update((list) =>
      list.map((s) => {
        if (s.participantId !== participantId) return s;
        targetVendor = s.vendorName;
        const currPrice = s.decryptedPriceHp ?? s.hiddenPriceHp;
        newHp = currPrice - 100_000_000;
        const tkdn = s.declaredTkdnPct ?? s.hiddenTkdnPct;
        const kp = tkdn >= 25 ? (tkdn / 100) * 0.25 : 0;
        newHea = (1 / (1 + kp)) * newHp;
        return { ...s, decryptedPriceHp: newHp, hiddenPriceHp: newHp };
      })
    );

    if (this.eAuctionExtensionsCount() < 5) {
      this.eAuctionExtensionsCount.update((c) => c + 1);
    }

    this.eAuctionLogs.update((logs) => [
      {
        seq: logs.length + 1,
        vendorName: targetVendor,
        newPriceHp: newHp,
        newHea,
        triggeredAutoExtend: true,
        timestamp: new Date().toISOString().slice(11, 19) + ' UTC',
      },
      ...logs,
    ]);

    this.appendAudit('VENDOR_ADMIN', 'E_REVERSE_AUCTION', 'REALTIME_BID_LOWERED_AUTO_EXTEND', targetVendor);
  }

  // Fase 3: Penetapan Pemenang (BAHP) & Buka Masa Sanggah
  announceWinnerBahp(): void {
    this.tenderStageStatus.set('OBJECTION_SANGGAH_PERIOD (5 HARI KERJA)');
    this.appendAudit('POKJA_COMMITTEE', 'WINNER_BAHP', 'WINNER_ANNOUNCED_AND_SANGGAH_OPENED', this.bafoForm.bahpNumber);
  }

  // Fase 3: Tolak Sanggah & Lanjut ke Penerbitan SPPBJ (Fase 4)
  rejectSanggahAndProceedToSppbj(sanggahId: string): void {
    this.sanggahList.update((list) =>
      list.map((s) =>
        s.id === sanggahId
          ? {
              ...s,
              status: 'REJECTED_TENDER_CONTINUES',
              responseNotes:
                'Sesuai Permen BUMN PER-2/MBU/03/2023, produk dengan TKDN >= 25% berhak atas Preferensi Harga hingga 25% melalui rumus HEA. Sanggahan ditolak dan tender dilanjutkan ke SPPBJ.',
            }
          : s
      )
    );
    this.tenderStageStatus.set('READY_FOR_SPPBJ (SIAP KONTRAK FASE 4)');
    this.appendAudit('POKJA_COMMITTEE', 'SANGGAH_RESOLUTION', 'SANGGAH_REJECTED_READY_FOR_SPPBJ', 'TND-BUMN-2026-0401');
  }

  // Fase 2 Methods
  createNewPr(): void {
    this.prErrorMsg.set('');
    const totalVal = (Number(this.newPrForm.quantity) || 0) * (Number(this.newPrForm.unitPrice) || 0);
    const budget = this.rkapBudgets().find((b) => b.id === this.newPrForm.rkapBudgetId);
    if (!budget) return;

    if (totalVal <= 0 || totalVal > budget.availableAmount) {
      this.prErrorMsg.set(`Eproc:Budget:InsufficientRkapBalance — Nilai PR (Rp ${totalVal.toLocaleString()}) melebihi sisa pagu RKAP!`);
      return;
    }

    this.rkapBudgets.update((list) =>
      list.map((b) =>
        b.id === budget.id
          ? {
              ...b,
              committedAmount: b.committedAmount + totalVal,
              availableAmount: b.availableAmount - totalVal,
            }
          : b
      )
    );

    const prNo = `PR-2026-0${this.purchaseRequisitions().length + 201}`;
    const created: PrViewModel = {
      id: `pr-${Date.now()}`,
      prNumber: prNo,
      prTitle: this.newPrForm.prTitle,
      rkapBudgetId: budget.id,
      wbsCode: budget.wbsElementGlAccount,
      requesterUnitName: this.newPrForm.requesterUnitName,
      category: 'GOODS',
      materialCode: this.newPrForm.materialCode,
      itemName: this.newPrForm.itemName,
      quantity: Number(this.newPrForm.quantity),
      uom: 'UNIT',
      unitPrice: Number(this.newPrForm.unitPrice),
      totalValue: totalVal,
      csmsLevel: 'MEDIUM',
      status: 'APPROVED_READY_FOR_SOURCING',
      selectedForConsolidation: true,
    };

    this.purchaseRequisitions.update((list) => [created, ...list]);
    this.appendAudit('PPK', 'PLANNING', 'PR_CREATED_AND_BUDGET_LOCKED', prNo);
  }

  consolidateSelectedPrs(): void {
    const selected = this.purchaseRequisitions().filter(
      (p) => p.selectedForConsolidation && p.status === 'APPROVED_READY_FOR_SOURCING'
    );
    if (selected.length === 0) return;

    const pkgCode = `PKG-2026-0${this.packages().length + 301}`;
    const totalPagu = selected.reduce((acc, p) => acc + p.totalValue, 0);
    const totalQty = selected.reduce((acc, p) => acc + p.quantity, 0);
    const pkgId = `pkg-${Date.now()}`;

    const newPkg: PackageViewModel = {
      id: pkgId,
      packageCode: pkgCode,
      packageName: this.consolidationForm.packageName,
      category: selected[0].category,
      sourcingMethod: 'TENDER_UMUM',
      envelopeMethod: this.consolidationForm.envelopeMethod,
      evaluationMethod: 'HARGA_TERENDAH_GUGUR',
      totalPaguBudget: totalPagu,
      minTkdnPct: 40,
      sourcePrNumbers: selected.map((s) => s.prNumber),
      consolidatedQty: totalQty,
      uom: selected[0].uom,
      status: 'DRAFT_CONSOLIDATED',
    };

    this.hpsInputMap[pkgId] = Math.round(totalPagu * 0.95);

    const selectedIds = new Set(selected.map((s) => s.id));
    this.purchaseRequisitions.update((list) =>
      list.map((p) =>
        selectedIds.has(p.id) ? { ...p, status: 'CONSOLIDATED_TO_PACKAGE', selectedForConsolidation: false } : p
      )
    );

    this.packages.update((list) => [newPkg, ...list]);
    this.appendAudit('PPK', 'PLANNING', 'PR_CONSOLIDATED_TO_PACKAGE', pkgCode);
    this.activeTab.set('BOQ_ENCRYPTED_HPS');
  }

  sealPackageHps(pkg: PackageViewModel): void {
    const hpsAmount = Number(this.hpsInputMap[pkg.id]) || Math.round(pkg.totalPaguBudget * 0.95);
    if (hpsAmount <= 0 || hpsAmount > pkg.totalPaguBudget) return;

    const cipherPreview = Array.from({ length: 32 }, () => Math.floor(Math.random() * 16).toString(16)).join('') + '...';
    const sha256 = Array.from({ length: 64 }, () => Math.floor(Math.random() * 16).toString(16)).join('');
    const doaId = `doa-${Date.now()}`;

    const steps: DoaStepViewModel[] = [
      { stepSequence: 1, roleCode: 'PPK', positionGrade: 'BOD-3_MANAGER', action: 'APPROVED', actedBy: 'Pejabat Pembuat Komitmen', comments: 'HPS disegel kriptografis AES-256-GCM.' },
    ];
    if (pkg.totalPaguBudget > 100_000_000) {
      steps.push({ stepSequence: 2, roleCode: 'PA_KPA', positionGrade: 'BOD-2_VP_GM', action: 'PENDING' });
    }
    if (pkg.totalPaguBudget > 1_000_000_000) {
      steps.push({ stepSequence: 3, roleCode: 'PA_KPA', positionGrade: 'BOD-1_SVP', action: 'PENDING' });
      steps.push({ stepSequence: 4, roleCode: 'PA_KPA', positionGrade: 'DIRECTOR_BIDANG', action: 'PENDING' });
    }
    if (pkg.totalPaguBudget > 10_000_000_000) {
      steps.push({ stepSequence: 5, roleCode: 'PA_KPA', positionGrade: 'BOARD_OF_DIRECTORS_COLLEGIATE', action: 'PENDING' });
    }

    const doaInstance: DoaInstanceViewModel = {
      id: doaId,
      packageId: pkg.id,
      packageCode: pkg.packageCode,
      packageTitle: pkg.packageName,
      transactionAmountIdr: pkg.totalPaguBudget,
      currentStepSequence: steps.length > 1 ? 2 : 1,
      totalStepsCount: steps.length,
      overallStatus: steps.length === 1 ? 'APPROVED' : 'IN_PROGRESS',
      steps,
    };

    this.doaInbox.update((list) => [doaInstance, ...list]);
    this.packages.update((list) =>
      list.map((p) =>
        p.id === pkg.id
          ? {
              ...p,
              sealedHpsCipherPreview: cipherPreview,
              sealedHpsSha256: sha256,
              doaInstanceId: doaId,
              status: steps.length === 1 ? 'DOA_APPROVED_READY_FOR_TENDER' : 'HPS_SEALED_WAITING_DOA',
            }
          : p
      )
    );

    this.appendAudit('PPK', 'PLANNING', 'HPS_SEALED_AES256GCM', pkg.packageCode);
  }

  approveNextDoaStep(doaId: string): void {
    let finishedPkgId: string | null = null;
    let pkgCodeRef = '';

    this.doaInbox.update((list) =>
      list.map((d) => {
        if (d.id !== doaId || d.overallStatus !== 'IN_PROGRESS') return d;
        pkgCodeRef = d.packageCode;
        const currSeq = d.currentStepSequence;
        const updatedSteps = d.steps.map((s) =>
          s.stepSequence === currSeq
            ? { ...s, action: 'APPROVED' as const, actedBy: `Pejabat ${s.positionGrade}`, comments: 'Disetujui sesuai Matriks DOA BUMN.' }
            : s
        );

        const isFinal = currSeq >= d.totalStepsCount;
        if (isFinal) {
          finishedPkgId = d.packageId;
        }

        return {
          ...d,
          currentStepSequence: isFinal ? currSeq : currSeq + 1,
          overallStatus: isFinal ? 'APPROVED' : 'IN_PROGRESS',
          steps: updatedSteps,
        };
      })
    );

    if (finishedPkgId) {
      this.packages.update((list) =>
        list.map((p) => (p.id === finishedPkgId ? { ...p, status: 'DOA_APPROVED_READY_FOR_TENDER' } : p))
      );
    }

    this.appendAudit('PA_KPA', 'DOA_WORKFLOW', 'DOA_STEP_APPROVED', pkgCodeRef);
  }

  previewKd(): number {
    return 3 * (Number(this.newVendorForm.highestExperienceNpt) || 0);
  }

  previewSkn(): number {
    const kb = Number(this.newVendorForm.netWorthEquityKb) || 0;
    const ongoing = Number(this.newVendorForm.ongoingContractsValue) || 0;
    const factors =
      this.newVendorForm.businessScale === 'KECIL'
        ? { fl: 0.3, fp: 5 }
        : this.newVendorForm.businessScale === 'MENENGAH'
          ? { fl: 0.6, fp: 7 }
          : { fl: 0.8, fp: 8 };
    const mk = factors.fl * kb;
    const kn = factors.fp * mk;
    return kn - ongoing;
  }

  submitNewVendor(): void {
    const nextNo = this.vendors().length + 101;
    const code = `DRT-2026-0${nextNo}`;
    const newItem: VendorViewModel = {
      id: `v-${nextNo}`,
      vendorCode: code,
      legalEntityType: 'PT',
      companyName: this.newVendorForm.companyName,
      businessScale: this.newVendorForm.businessScale,
      isBumnAffiliated: false,
      isPadiUmkm: this.newVendorForm.businessScale === 'KECIL',
      nibNumber: this.newVendorForm.nibNumber,
      npwpNumber: this.newVendorForm.npwpNumber,
      cityName: 'Jakarta',
      verificationStatus: 'SUBMITTED',
      vpeGrade: 'UNRATED',
      netWorthEquityKb: Number(this.newVendorForm.netWorthEquityKb),
      highestExperienceNpt: Number(this.newVendorForm.highestExperienceNpt),
      kemampuanDasarKd: this.previewKd(),
      sisaKemampuanNyataSkn: this.previewSkn(),
      sisaKemampuanPaketSkp: 5,
      altmanZScore: 3.65,
      financialHealthZone: 'SAFE_ZONE',
      shareholderOwnerName: this.newVendorForm.shareholderOwnerName,
      shareholderNik: this.newVendorForm.shareholderNik,
      csmsLevel: 'NONE',
      csmsScore: 0,
    };

    this.vendors.update((list) => [newItem, ...list]);
    this.appendAudit('VENDOR_ADMIN', 'VMS', 'VENDOR_REGISTERED', code);
    this.activeTab.set('VMS_DIRECTORY');
  }

  approveVendorAvl(vendorId: string): void {
    let targetCode = '';
    this.vendors.update((list) =>
      list.map((v) => {
        if (v.id === vendorId) {
          targetCode = v.vendorCode;
          return { ...v, verificationStatus: 'VERIFIED_AVL', vpeGrade: 'GOLD' };
        }
        return v;
      })
    );
    if (targetCode) {
      this.appendAudit('VMS_VERIFIER', 'VMS', 'AVL_APPROVED', targetCode);
    }
  }

  calculatedCsmsFinalScore(): number {
    const docWeighted = this.csmsElements.reduce((acc, el) => acc + el.score * (el.weight / 100), 0);
    return docWeighted * 0.4 + Number(this.csmsFieldScore) * 0.6;
  }

  calculatedCsmsRiskLevel(): 'LOW' | 'MEDIUM' | 'HIGH' | 'EXTREME' {
    const score = this.calculatedCsmsFinalScore();
    if (score >= 85) return 'EXTREME';
    if (score >= 70) return 'HIGH';
    if (score >= 50) return 'MEDIUM';
    return 'LOW';
  }

  issueCsmsCertificate(): void {
    const score = Number(this.calculatedCsmsFinalScore().toFixed(2));
    const level = this.calculatedCsmsRiskLevel();
    const certNo = `CSMS-BUMN-2026-${Math.floor(1000 + Math.random() * 8999)}`;

    this.vendors.update((list) =>
      list.map((v) => (v.id === this.selectedCsmsVendorId ? { ...v, csmsLevel: level, csmsScore: score, csmsCertNo: certNo } : v))
    );
    this.appendAudit('HSE_CSMS_AUDITOR', 'CSMS', `CSMS_CERTIFIED_${level}`, certNo);
    this.activeTab.set('VMS_DIRECTORY');
  }

  // ============================================================================
  // STATE & LOGIKA INTERAKTIF FASE 4: E-KATALOG (KONTRAK PAYUNG) & E-CLM
  // ============================================================================

  readonly umbrellaAgreements = signal<UmbrellaAgreementViewModel[]>([
    {
      id: 'ua-pipe-01',
      agreementNumber: 'KP-BUMN/ENG-PIPE/2026/001',
      title: 'Kontrak Payung Penyediaan Pipa Baja Seamless & Valve API 5L/6D Regional Nasional',
      vendorName: 'PT Nusantara Valve & Piping Manufacturing',
      validUntil: '2027-12-31',
      totalQuotaCeiling: 25_000_000_000,
      consumedQuotaAmount: 4_500_000_000,
      remainingQuotaAmount: 20_500_000_000,
      status: 'ACTIVE',
    },
    {
      id: 'ua-it-02',
      agreementNumber: 'KP-BUMN/IT-WORKSTATION/2026/002',
      title: 'Kontrak Payung Pengadaan Perangkat Laptop & Workstation Enterprise TKDN',
      vendorName: 'PT Teknologi Komputasi Merah Putih',
      validUntil: '2026-12-31',
      totalQuotaCeiling: 8_000_000_000,
      consumedQuotaAmount: 1_200_000_000,
      remainingQuotaAmount: 6_800_000_000,
      status: 'ACTIVE',
    },
  ]);

  readonly catalogItems = signal<CatalogItemViewModel[]>([
    {
      id: 'cat-pipe-8in',
      umbrellaId: 'ua-pipe-01',
      agreementNumber: 'KP-BUMN/ENG-PIPE/2026/001',
      kimapCode: 'KIMAP-PIPE-8IN-CS',
      itemName: 'Carbon Steel Seamless Line Pipe 8 Inch Sch 40 API 5L Gr.B (6 Meter/Batang)',
      vendorName: 'PT Nusantara Valve & Piping Manufacturing',
      uom: 'BATANG',
      verifiedTkdnPct: 62.5,
      tkdnCertNo: 'TKDN-KEMENPERIN-2026-8812',
      leadTimeDays: 14,
      tiers: [
        { minQty: 1, maxQty: 49, unitPrice: 4_800_000, discountPct: 0 },
        { minQty: 50, maxQty: 199, unitPrice: 4_560_000, discountPct: 5 },
        { minQty: 200, maxQty: null, unitPrice: 4_320_000, discountPct: 10 },
      ],
    },
    {
      id: 'cat-laptop-pro',
      umbrellaId: 'ua-it-02',
      agreementNumber: 'KP-BUMN/IT-WORKSTATION/2026/002',
      kimapCode: 'KIMAP-IT-LPT-PRO14',
      itemName: 'Laptop Enterprise Core Ultra 7 / 32GB RAM / 1TB NVMe (Sertifikat TKDN + BMP)',
      vendorName: 'PT Teknologi Komputasi Merah Putih',
      uom: 'UNIT',
      verifiedTkdnPct: 41.2,
      tkdnCertNo: 'TKDN-KEMENPERIN-2026-4409',
      leadTimeDays: 7,
      tiers: [
        { minQty: 1, maxQty: 24, unitPrice: 18_500_000, discountPct: 0 },
        { minQty: 25, maxQty: 99, unitPrice: 17_575_000, discountPct: 5 },
        { minQty: 100, maxQty: null, unitPrice: 16_650_000, discountPct: 10 },
      ],
    },
  ]);

  readonly callOffOrders = signal<CallOffOrderViewModel[]>([]);

  callOffForm = {
    catalogItemId: 'cat-pipe-8in',
    branchCode: 'REG-KALTIM-04',
    orderedQty: 250,
  };

  previewCallOffTier(): { unitPrice: number; discountPct: number } {
    const item = this.catalogItems().find((c) => c.id === this.callOffForm.catalogItemId) ?? this.catalogItems()[0];
    const qty = Math.max(1, Number(this.callOffForm.orderedQty) || 1);
    const matched =
      item.tiers.find((t) => qty >= t.minQty && (t.maxQty === null || qty <= t.maxQty)) ?? item.tiers[item.tiers.length - 1];
    return { unitPrice: matched.unitPrice, discountPct: matched.discountPct };
  }

  previewCallOffTotal(): number {
    const qty = Math.max(1, Number(this.callOffForm.orderedQty) || 1);
    return qty * this.previewCallOffTier().unitPrice;
  }

  createCallOffOrder(): void {
    const item = this.catalogItems().find((c) => c.id === this.callOffForm.catalogItemId) ?? this.catalogItems()[0];
    const qty = Math.max(1, Number(this.callOffForm.orderedQty) || 1);
    const tier = this.previewCallOffTier();
    const total = qty * tier.unitPrice;

    const ua = this.umbrellaAgreements().find((u) => u.id === item.umbrellaId);
    if (!ua || total > ua.remainingQuotaAmount) {
      return;
    }

    const poNumber = `PO-CAT/${this.callOffForm.branchCode}/2026/000${this.callOffOrders().length + 1}`;
    this.umbrellaAgreements.update((list) =>
      list.map((u) =>
        u.id === item.umbrellaId
          ? {
              ...u,
              consumedQuotaAmount: u.consumedQuotaAmount + total,
              remainingQuotaAmount: u.remainingQuotaAmount - total,
            }
          : u
      )
    );

    this.callOffOrders.update((list) => [
      {
        poNumber,
        branchCode: this.callOffForm.branchCode,
        kimapCode: item.kimapCode,
        itemName: item.itemName,
        vendorName: item.vendorName,
        orderedQty: qty,
        uom: item.uom,
        appliedUnitPrice: tier.unitPrice,
        discountPct: tier.discountPct,
        totalOrderAmount: total,
        orderedAt: new Date().toISOString().slice(0, 19).replace('T', ' ') + ' UTC',
      },
      ...list,
    ]);

    this.appendAudit(this.activeRoleCode(), 'ECATALOG', 'CALL_OFF_PO_ISSUED', poNumber);
  }

  readonly clmGuardMessage = signal<string>('');

  readonly activeContract = signal<{
    sppbjNumber: string;
    contractNumber: string;
    vendorName: string;
    committedTkdnPct: number;
    referenceHpsAmount: number;
    initialContractAmount: number;
    currentActiveContractAmount: number;
    requiredPerformanceBondAmount: number;
    performanceBondStatus: 'UNVERIFIED' | 'VERIFIED_ACTIVE';
    emeteraiSerial: string;
    signedPdfHash: string;
    spmkNumber: string;
    lifecycleStatus: string;
    redlines: ContractRedlineViewModel[];
    addendums: ContractAddendumCcoViewModel[];
  }>({
    sppbjNumber: 'SPPBJ/BUMN-ENG/2026/001',
    contractNumber: 'KTR/BUMN-ENG/EPC-VALVE/2026/001',
    vendorName: 'PT Nusantara Valve & Piping Manufacturing',
    committedTkdnPct: 60.0,
    referenceHpsAmount: 6_000_000_000,
    initialContractAmount: 5_450_000_000,
    currentActiveContractAmount: 5_450_000_000,
    requiredPerformanceBondAmount: 272_500_000,
    performanceBondStatus: 'UNVERIFIED',
    emeteraiSerial: '',
    signedPdfHash: '',
    spmkNumber: '',
    lifecycleStatus: 'REDLINING_NEGOTIATION',
    redlines: [
      {
        versionLabel: 'v1.0 (LEGAL_BUMN)',
        authorRole: 'LEGAL_BUMN',
        authorName: 'Biro Hukum & Kepatuhan BUMN',
        clauseRef: 'Pasal 12 & Pasal 18 (Jaminan TKDN 60% & Denda 1‰/Hari)',
        notes: 'Draft awal kontrak standar BUMN memasukkan klausul denda keterlambatan 1‰ per hari (maks 5%) dan sanksi selisih preferensi TKDN.',
        timestamp: '2026-03-21 09:00 UTC',
      },
    ],
    addendums: [],
  });

  bondForm = {
    guaranteeNumber: 'BG-MANDIRI-2026-99881',
    bondAmount: 272_500_000,
  };

  ccoForm = {
    deltaAmount: 450_000_000,
    justification: 'Penambahan aksesoris flange & stud bolt lapangan (+8.26%)',
  };

  addVendorContractRedline(): void {
    const cur = this.activeContract();
    const nextVer = cur.redlines.length;
    this.activeContract.update((c) => ({
      ...c,
      redlines: [
        ...c.redlines,
        {
          versionLabel: `v1.${nextVer} (VENDOR_LEGAL)`,
          authorRole: 'VENDOR_LEGAL',
          authorName: 'Divisi Legal PT Nusantara Valve',
          clauseRef: 'Pasal 21 Force Majeure & Toleransi Pengapalan',
          notes: 'Penyesuaian klausul pemberitahuan tertulis Force Majeure 7x24 jam disepakati kedua belah pihak.',
          timestamp: new Date().toISOString().slice(0, 16).replace('T', ' ') + ' UTC',
        },
      ],
    }));
    this.appendAudit('VENDOR_ADMIN', 'ECLM', 'CONTRACT_REDLINE_ADDED', cur.contractNumber);
  }

  verifyPerformanceBond(): void {
    const cur = this.activeContract();
    if (Number(this.bondForm.bondAmount) < cur.requiredPerformanceBondAmount) {
      this.clmGuardMessage.set(
        `DITOLAK (Eproc:Clm:InsufficientPerformanceBondAmount): Nominal Bank Garansi (Rp ${Number(this.bondForm.bondAmount).toLocaleString()}) kurang dari kewajiban minimum 5% (Rp ${cur.requiredPerformanceBondAmount.toLocaleString()}).`
      );
      return;
    }
    this.clmGuardMessage.set('');
    this.activeContract.update((c) => ({
      ...c,
      performanceBondStatus: 'VERIFIED_ACTIVE',
      lifecycleStatus: 'PERFORMANCE_BOND_VERIFIED',
    }));
    this.appendAudit(this.activeRoleCode(), 'ECLM', 'PERFORMANCE_BOND_VERIFIED', this.bondForm.guaranteeNumber);
  }

  signContractWithTteAndEmeterai(): void {
    const cur = this.activeContract();
    if (cur.performanceBondStatus !== 'VERIFIED_ACTIVE') {
      this.clmGuardMessage.set(
        'DITOLAK (Eproc:Clm:PerformanceBondRequiredBeforeSigning): Kontrak dilarang ditandatangani (TTE) sebelum Jaminan Pelaksanaan 5% diverifikasi aktif!'
      );
      return;
    }
    this.clmGuardMessage.set('');
    const emeterai = 'EMTR-PERURI-2026-7741902';
    const hash = '9f4b2c8e1d7a6053c8b1e29473a10d5f6c8b2a1e4d9f0a7c3b5e8d1a2f6c9b40';
    this.activeContract.update((c) => ({
      ...c,
      emeteraiSerial: emeterai,
      signedPdfHash: hash,
      lifecycleStatus: 'SIGNED_TTE_EMETERAI',
    }));
    this.appendAudit(this.activeRoleCode(), 'ECLM', 'CONTRACT_SIGNED_TTE_EMETERAI', cur.contractNumber);
  }

  issueContractSpmk(): void {
    const cur = this.activeContract();
    if (cur.performanceBondStatus !== 'VERIFIED_ACTIVE' || !cur.signedPdfHash) {
      this.clmGuardMessage.set(
        'DITOLAK (Eproc:Clm:CannotIssueSpmkWithoutBondAndTte): SPMK dilarang diterbitkan sebelum Jaminan Pelaksanaan 5% diverifikasi aktif DAN Kontrak telah dibubuhi e-Meterai + TTE BSrE/PSrE!'
      );
      return;
    }
    this.clmGuardMessage.set('');
    const spmkNo = 'SPMK/BUMN-ENG/2026/001';
    this.activeContract.update((c) => ({
      ...c,
      spmkNumber: spmkNo,
      lifecycleStatus: 'SPMK_ISSUED_ACTIVE',
    }));
    this.appendAudit('PPK', 'ECLM', 'SPMK_ISSUED', spmkNo);
  }

  submitContractAddendumCco(): void {
    const cur = this.activeContract();
    if (!cur.spmkNumber) {
      this.clmGuardMessage.set(
        'DITOLAK (Eproc:Clm:ContractNotActiveForCco): Addendum/CCO hanya dapat diajukan setelah SPMK diterbitkan!'
      );
      return;
    }

    const delta = Number(this.ccoForm.deltaAmount) || 0;
    const newTotal = cur.currentActiveContractAmount + delta;
    const cumulativePct = Number((((newTotal - cur.initialContractAmount) / cur.initialContractAmount) * 100).toFixed(2));

    if (cumulativePct > 10.0) {
      this.clmGuardMessage.set(
        `DITOLAK (Eproc:Clm:CcoExceedsMaxTenPercentCeiling): Kenaikan kumulatif Addendum/CCO sebesar +${cumulativePct}% melampaui batas plafon regulasi 10% dari Nilai Kontrak Awal!`
      );
      return;
    }

    this.clmGuardMessage.set('');
    const addNo = `ADD-0${cur.addendums.length + 1}/KTR/BUMN-ENG/2026`;
    this.activeContract.update((c) => ({
      ...c,
      currentActiveContractAmount: newTotal,
      lifecycleStatus: 'ADDENDUM_AMENDED',
      addendums: [
        ...c.addendums,
        {
          seq: c.addendums.length + 1,
          addendumNumber: addNo,
          changeType: 'VOLUME_ADDITION_CCO',
          justification: this.ccoForm.justification,
          previousAmount: c.currentActiveContractAmount,
          deltaAmount: delta,
          newAmount: newTotal,
          cumulativeIncreasePct: cumulativePct,
          additionalDays: 14,
          boardOverride: false,
        },
      ],
    }));
    this.appendAudit('PPK', 'ECLM', 'ADDENDUM_CCO_APPROVED', addNo);
  }

  // ============================================================================
  // STATE & LOGIKA INTERAKTIF FASE 5: KURVA S (SCM), BAST POST-AUDIT TKDN,
  // 3-WAY MATCHING E-INVOICING, PAJAK WAPU BUMN, BAP & RAPOR VPE
  // ============================================================================

  scurveForm = {
    weekNumber: 12,
    plannedPct: 75.0,
    actualPct: 68.0,
    currentScm: 'SCM_LEVEL_1' as 'NONE' | 'SCM_LEVEL_1' | 'SCM_LEVEL_2',
  };

  readonly scurveLogs = signal<
    {
      weekNumber: number;
      plannedPct: number;
      actualPct: number;
      deviationPct: number;
      isCritical: boolean;
      triggeredScm: string;
    }[]
  >([
    {
      weekNumber: 8,
      plannedPct: 55.0,
      actualPct: 43.0,
      deviationPct: -12.0,
      isCritical: true,
      triggeredScm: 'SCM_LEVEL_1',
    },
  ]);

  evaluateSCurveProgress(): void {
    const planned = Number(this.scurveForm.plannedPct) || 0;
    const actual = Number(this.scurveForm.actualPct) || 0;
    const dev = Number((actual - planned).toFixed(2));
    const threshold = planned <= 70 ? -10.0 : -5.0;
    const isCrit = dev <= threshold;

    let nextScm = 'NONE';
    if (isCrit) {
      if (this.scurveForm.currentScm === 'NONE') nextScm = 'SCM_LEVEL_1';
      else if (this.scurveForm.currentScm === 'SCM_LEVEL_1') nextScm = 'SCM_LEVEL_2';
      else nextScm = 'SCM_LEVEL_3 (SURAT PERINGATAN PEMUTUSAN KONTRAK)';
    }

    this.scurveLogs.update((list) => [
      {
        weekNumber: Number(this.scurveForm.weekNumber),
        plannedPct: planned,
        actualPct: actual,
        deviationPct: dev,
        isCritical: isCrit,
        triggeredScm: nextScm,
      },
      ...list,
    ]);

    this.appendAudit('PPK', 'EXECUTION', `SCURVE_EVALUATED_${nextScm}`, `WEEK-${this.scurveForm.weekNumber}`);
  }

  readonly bastState = signal({
    bastNumber: 'BAST/BUMN-ENG/2026/001',
    handoverType: 'BAST_GOODS_100',
    surveyor: 'PT Surveyor Indonesia',
    reportNo: 'LHV-TKDN-SI-2026-9912',
    contractValue: 5_900_000_000,
    approvedGrossValue: 5_450_000_000,
  });

  postAuditTkdnForm = {
    committedPct: 60.0,
    actualPct: 58.0,
    delayDays: 8,
  };

  previewTkdnShortfallPct(): number {
    return Math.max(0, Number((Number(this.postAuditTkdnForm.committedPct) - Number(this.postAuditTkdnForm.actualPct)).toFixed(2)));
  }

  previewTkdnSanctionAmount(): number {
    const shortfall = this.previewTkdnShortfallPct();
    if (shortfall <= 0) return 0;
    const ratio = Math.min(shortfall / 100, 0.25);
    return Math.round(this.bastState().contractValue * ratio);
  }

  updateBastAndPostAuditTkdn(): void {
    this.appendAudit(
      'PPK',
      'EXECUTION',
      'BAST_AND_POST_AUDIT_TKDN_LOCKED',
      `${this.bastState().bastNumber} (Shortfall ${this.previewTkdnShortfallPct()}%)`
    );
    this.activeTab.set('FINANCE_3WAY_WAPU_VPE');
  }

  readonly financeGuardMessage = signal<string>('');

  invoiceForm = {
    fakturNo: '030.004-26.88991122',
    claimedDpp: 5_450_000_000,
    dpRecoupmentPct: 10.0,
    retentionPct: 5.0,
  };

  readonly invoiceSettlement = signal({
    isMatched: true,
    bapNumber: 'BAP/BUMN-FIN/2026/0001',
    dppGross: 5_450_000_000,
    dpDeduction: 545_000_000,
    retentionDeduction: 272_500_000,
    latePenalty: 43_600_000,
    tkdnPenalty: 118_000_000,
    ppnAmount: 509_575_000,
    pphWithheld: 69_487_500,
    netPayableToVendor: 4_401_412_500,
  });

  executeThreeWayMatchingAndIssueBap(): void {
    if (!this.invoiceForm.fakturNo.startsWith('030.')) {
      this.financeGuardMessage.set(
        `DITOLAK (Eproc:Finance:InvalidWapuFakturCode): Nomor Seri Faktur Pajak (${this.invoiceForm.fakturNo}) wajib menggunakan kode transaksi '030.' untuk BUMN Wajib Pungut (WAPU)!`
      );
      return;
    }

    const bastGross = this.bastState().approvedGrossValue;
    const claimed = Number(this.invoiceForm.claimedDpp) || 0;
    if (Math.abs(claimed - bastGross) > 1) {
      this.financeGuardMessage.set(
        `DITOLAK 3-WAY MATCHING: Nilai DPP e-Invoice Vendor (Rp ${claimed.toLocaleString()}) tidak cocok dengan Nilai Bruto BAST/GR (Rp ${bastGross.toLocaleString()})!`
      );
      this.invoiceSettlement.update((s) => ({ ...s, isMatched: false, bapNumber: 'BLOCKED_3WAY_MISMATCH' }));
      return;
    }

    this.financeGuardMessage.set('');
    const dpDed = Math.round(bastGross * (Number(this.invoiceForm.dpRecoupmentPct) / 100));
    const retDed = Math.round(bastGross * (Number(this.invoiceForm.retentionPct) / 100));
    const dppAfterDpRet = Math.max(0, bastGross - dpDed - retDed);

    const delayDays = Math.max(0, Number(this.postAuditTkdnForm.delayDays) || 0);
    const rawLate = Math.round(delayDays * 0.001 * bastGross);
    const maxCap = Math.round(this.bastState().contractValue * 0.05);
    const cappedLate = Math.min(rawLate, maxCap);
    const tkdnPen = this.previewTkdnSanctionAmount();

    const dppNet = Math.max(0, dppAfterDpRet - cappedLate - tkdnPen);
    const ppn = Math.round(dppAfterDpRet * 0.11);
    const pph = Math.round(dppAfterDpRet * 0.015);
    const netPayable = dppNet - pph; // PPN 11% dipungut langsung oleh WAPU BUMN ke Kas Negara

    const bapNo = 'BAP/BUMN-FIN/2026/0001 (SP2D READY)';
    this.invoiceSettlement.set({
      isMatched: true,
      bapNumber: bapNo,
      dppGross: bastGross,
      dpDeduction: dpDed,
      retentionDeduction: retDed,
      latePenalty: cappedLate,
      tkdnPenalty: tkdnPen,
      ppnAmount: ppn,
      pphWithheld: pph,
      netPayableToVendor: netPayable,
    });

    this.appendAudit('FINANCE_AP', 'FINANCE_3WAY_WAPU', 'BAP_AND_SP2D_ISSUED', bapNo);
  }

  vpeForm = {
    quality: 92,
    delivery: 80,
    hseCsms: 90,
    tkdn: 84,
    responsiveness: 88,
  };

  calculatedVpeScore(): number {
    return (
      Number(this.vpeForm.quality) * 0.3 +
      Number(this.vpeForm.delivery) * 0.25 +
      Number(this.vpeForm.hseCsms) * 0.2 +
      Number(this.vpeForm.tkdn) * 0.15 +
      Number(this.vpeForm.responsiveness) * 0.1
    );
  }

  calculatedVpeGrade(): { grade: string; recommendation: string } {
    const score = this.calculatedVpeScore();
    if (score >= 90) return { grade: 'PLATINUM', recommendation: 'Prioritas Utama Kontrak Payung & Tender Terbatas' };
    if (score >= 80) return { grade: 'GOLD', recommendation: 'Pertahankan Status AVL Aktif (Kinerja Baik)' };
    if (score >= 70) return { grade: 'SILVER', recommendation: 'Wajib Pembinaan Teknis & Evaluasi Berkala' };
    if (score >= 55) return { grade: 'RED_WARNING', recommendation: 'Surat Peringatan & Pembekuan Sementara AVL (Suspend 6 Bulan)' };
    return { grade: 'BLACKLIST_CANDIDATE', recommendation: 'Rekomendasi Daftar Hitam (Blacklist)' };
  }

  publishVpeScorecardToAvl(): void {
    const grade = this.calculatedVpeGrade().grade;
    this.vendors.update((list) =>
      list.map((v, idx) => (idx === 1 ? { ...v, vpeGrade: grade } : v))
    );
    this.appendAudit('PPK', 'VMS_VPE', `VPE_SCORECARD_PUBLISHED_${grade}`, 'DRT-2026-0102');
    this.activeTab.set('VMS_DIRECTORY');
  }

  private appendAudit(actorRole: string, module: string, action: string, entityRef: string): void {
    const currentList = this.auditLogs();
    const prevHash = currentList[currentList.length - 1]?.currentHash ?? '0'.repeat(64);
    const randomHex = Array.from({ length: 64 }, () => Math.floor(Math.random() * 16).toString(16)).join('');
    this.auditLogs.update((list) => [
      ...list,
      {
        seq: list.length + 1,
        timestamp: new Date().toISOString().replace('T', ' ').slice(0, 19) + ' UTC',
        actorRole,
        module,
        action,
        entityRef,
        prevHash,
        currentHash: randomHex,
      },
    ]);
  }
}
