namespace Eproc.Permissions;

/// <summary>
/// Definisi Permission Hierarkis (eproc.*) untuk seluruh 25 Menu Utama dan 86 Sub-Menu,
/// serta aturan Segregation of Duties (SoD) antar Role BUMN.
/// </summary>
public static class EprocPermissions
{
    public const string GroupName = "eproc";

    public static class PublicPortal
    {
        public const string Default = GroupName + ".public";
        public const string TransparencyDashboard = Default + ".dashboard";
        public const string TenderAnnouncements = Default + ".announcements";
        public const string NationalBlacklist = Default + ".blacklist";
        public const string QrDocumentVerifier = Default + ".qr_verifier";
        public const string RegulationGuide = Default + ".regulations";
    }

    public static class VendorPortal
    {
        public const string Default = GroupName + ".vendor_portal";
        public const string Dashboard = Default + ".dashboard";
        public const string VmsProfile = Default + ".vms_profile";
        public const string CsmsQualification = Default + ".csms";
        public const string TenderBidding = Default + ".bidding";
        public const string EReverseAuctionRoom = Default + ".eauction";
        public const string ECatalogStore = Default + ".ecatalog_store";
        public const string ContractAndBonds = Default + ".contracts";
        public const string ExecutionAndBast = Default + ".execution_bast";
        public const string EInvoicing = Default + ".einvoicing";
        public const string VpeScorecard = Default + ".vpe_scorecard";
    }

    public static class InternalBumnPortal
    {
        public const string Default = GroupName + ".internal";

        public static class ExecutiveDashboard
        {
            public const string Default = InternalBumnPortal.Default + ".executive";
            public const string RkapAndCostSaving = Default + ".rkap_saving";
            public const string TkdnAndUmkmAnalytics = Default + ".tkdn_umkm";
            public const string TenderSlaMonitoring = Default + ".sla_monitoring";
            public const string CriticalContractEarlyWarning = Default + ".early_warning";
        }

        public static class DoaInbox
        {
            public const string Default = InternalBumnPortal.Default + ".doa_inbox";
            public const string ApproveTransactions = Default + ".approve";
            public const string DelegateAuthority = Default + ".delegate";
        }

        public static class VmsAndCsmsAdmin
        {
            public const string Default = InternalBumnPortal.Default + ".vms_admin";
            public const string VerifyVendorLegal = Default + ".verify_legal";
            public const string EvaluateFinancialSknKd = Default + ".evaluate_financial";
            public const string ManageAvlCommodity = Default + ".manage_avl";
            public const string AuditCsmsHse = Default + ".audit_csms";
            public const string ManageSanctionsBlacklist = Default + ".sanctions_blacklist";
        }

        public static class PlanningAndBudgeting
        {
            public const string Default = InternalBumnPortal.Default + ".planning";
            public const string ManageRkapCostCenter = Default + ".rkap";
            public const string ManageDrpAnnual = Default + ".drp";
            public const string CreatePurchaseRequisition = Default + ".pr_create";
            public const string ConsolidatePackages = Default + ".pr_consolidate";
            public const string SealEncryptedOwnerEstimateHps = Default + ".seal_hps";
            public const string ViewPriceIntelligence = Default + ".price_intelligence";
        }

        public static class ESourcingTender
        {
            public const string Default = InternalBumnPortal.Default + ".esourcing";
            public const string AssignCommittee = Default + ".assign_committee";
            public const string ManageRfpAndSchedule = Default + ".manage_rfp";
            public const string ConductAanwijzing = Default + ".aanwijzing";
            public const string OpenAndDecryptBidEnvelopes = Default + ".open_envelopes";
            public const string EvaluateAdminTechQualification = Default + ".evaluate_tech";
            public const string CalculateCommercialHeaTkdn = Default + ".calculate_hea";
            public const string HostEReverseAuction = Default + ".host_eauction";
            public const string ConductBafoNegotiation = Default + ".negotiate_bafo";
            public const string ProposeAndAnnounceWinner = Default + ".winner_determination";
            public const string ResolveObjectionsSanggah = Default + ".resolve_sanggah";
        }

        public static class ECatalogAndUmbrella
        {
            public const string Default = InternalBumnPortal.Default + ".ecatalog";
            public const string ManageUmbrellaAgreements = Default + ".umbrella";
            public const string CurateCatalogProducts = Default + ".curate_products";
            public const string CheckoutCallOffOrder = Default + ".call_off_order";
        }

        public static class ContractLifecycle
        {
            public const string Default = InternalBumnPortal.Default + ".eclm";
            public const string IssueSppbj = Default + ".issue_sppbj";
            public const string DraftAndRedlineContract = Default + ".redline_contract";
            public const string SignDigitalContractTte = Default + ".sign_tte";
            public const string VerifyGuaranteesBankBonds = Default + ".verify_bonds";
            public const string IssueSpmk = Default + ".issue_spmk";
            public const string ProcessAddendumCco = Default + ".process_cco";
        }

        public static class ProjectExecutionAndBast
        {
            public const string Default = InternalBumnPortal.Default + ".execution";
            public const string InspectWarehouseQcAndGr = Default + ".warehouse_gr";
            public const string MonitorScurveProgress = Default + ".scurve_monitor";
            public const string IssueShowCauseMeetingScm = Default + ".issue_scm";
            public const string ApproveServiceEntrySheetSes = Default + ".approve_ses";
            public const string IssueBastPhoFho = Default + ".issue_bast";
            public const string VerifyPostAuditTkdnAndPenalties = Default + ".verify_post_tkdn";
        }

        public static class FinanceWapuAndPayment
        {
            public const string Default = InternalBumnPortal.Default + ".finance";
            public const string VerifyThreeWayMatching = Default + ".three_way_match";
            public const string VerifyTaxWapuAndEbupot = Default + ".verify_tax_wapu";
            public const string IssueBapPaymentMinutes = Default + ".issue_bap";
            public const string DisburseErpPayment = Default + ".disburse_erp";
        }

        public static class VendorPerformanceVpe
        {
            public const string Default = InternalBumnPortal.Default + ".vpe";
            public const string ScoreVendorPerformance = Default + ".score_vendor";
            public const string ViewVendorRatingLedger = Default + ".rating_ledger";
        }

        public static class SystemAdminAndAudit
        {
            public const string Default = InternalBumnPortal.Default + ".sysadmin";
            public const string ManageOrgAndUsersSod = Default + ".org_users_sod";
            public const string ConfigureDoaMatrices = Default + ".configure_doa";
            public const string InspectImmutableAuditChain = Default + ".audit_hash_chain";
        }
    }

    public static IReadOnlyList<string> GetAllPermissions() =>
    [
        PublicPortal.TransparencyDashboard,
        PublicPortal.TenderAnnouncements,
        PublicPortal.NationalBlacklist,
        PublicPortal.QrDocumentVerifier,
        PublicPortal.RegulationGuide,

        VendorPortal.Dashboard,
        VendorPortal.VmsProfile,
        VendorPortal.CsmsQualification,
        VendorPortal.TenderBidding,
        VendorPortal.EReverseAuctionRoom,
        VendorPortal.ECatalogStore,
        VendorPortal.ContractAndBonds,
        VendorPortal.ExecutionAndBast,
        VendorPortal.EInvoicing,
        VendorPortal.VpeScorecard,

        InternalBumnPortal.ExecutiveDashboard.RkapAndCostSaving,
        InternalBumnPortal.ExecutiveDashboard.TkdnAndUmkmAnalytics,
        InternalBumnPortal.ExecutiveDashboard.TenderSlaMonitoring,
        InternalBumnPortal.ExecutiveDashboard.CriticalContractEarlyWarning,

        InternalBumnPortal.DoaInbox.ApproveTransactions,
        InternalBumnPortal.DoaInbox.DelegateAuthority,

        InternalBumnPortal.VmsAndCsmsAdmin.VerifyVendorLegal,
        InternalBumnPortal.VmsAndCsmsAdmin.EvaluateFinancialSknKd,
        InternalBumnPortal.VmsAndCsmsAdmin.ManageAvlCommodity,
        InternalBumnPortal.VmsAndCsmsAdmin.AuditCsmsHse,
        InternalBumnPortal.VmsAndCsmsAdmin.ManageSanctionsBlacklist,

        InternalBumnPortal.PlanningAndBudgeting.ManageRkapCostCenter,
        InternalBumnPortal.PlanningAndBudgeting.ManageDrpAnnual,
        InternalBumnPortal.PlanningAndBudgeting.CreatePurchaseRequisition,
        InternalBumnPortal.PlanningAndBudgeting.ConsolidatePackages,
        InternalBumnPortal.PlanningAndBudgeting.SealEncryptedOwnerEstimateHps,
        InternalBumnPortal.PlanningAndBudgeting.ViewPriceIntelligence,

        InternalBumnPortal.ESourcingTender.AssignCommittee,
        InternalBumnPortal.ESourcingTender.ManageRfpAndSchedule,
        InternalBumnPortal.ESourcingTender.ConductAanwijzing,
        InternalBumnPortal.ESourcingTender.OpenAndDecryptBidEnvelopes,
        InternalBumnPortal.ESourcingTender.EvaluateAdminTechQualification,
        InternalBumnPortal.ESourcingTender.CalculateCommercialHeaTkdn,
        InternalBumnPortal.ESourcingTender.HostEReverseAuction,
        InternalBumnPortal.ESourcingTender.ConductBafoNegotiation,
        InternalBumnPortal.ESourcingTender.ProposeAndAnnounceWinner,
        InternalBumnPortal.ESourcingTender.ResolveObjectionsSanggah,

        InternalBumnPortal.ECatalogAndUmbrella.ManageUmbrellaAgreements,
        InternalBumnPortal.ECatalogAndUmbrella.CurateCatalogProducts,
        InternalBumnPortal.ECatalogAndUmbrella.CheckoutCallOffOrder,

        InternalBumnPortal.ContractLifecycle.IssueSppbj,
        InternalBumnPortal.ContractLifecycle.DraftAndRedlineContract,
        InternalBumnPortal.ContractLifecycle.SignDigitalContractTte,
        InternalBumnPortal.ContractLifecycle.VerifyGuaranteesBankBonds,
        InternalBumnPortal.ContractLifecycle.IssueSpmk,
        InternalBumnPortal.ContractLifecycle.ProcessAddendumCco,

        InternalBumnPortal.ProjectExecutionAndBast.InspectWarehouseQcAndGr,
        InternalBumnPortal.ProjectExecutionAndBast.MonitorScurveProgress,
        InternalBumnPortal.ProjectExecutionAndBast.IssueShowCauseMeetingScm,
        InternalBumnPortal.ProjectExecutionAndBast.ApproveServiceEntrySheetSes,
        InternalBumnPortal.ProjectExecutionAndBast.IssueBastPhoFho,
        InternalBumnPortal.ProjectExecutionAndBast.VerifyPostAuditTkdnAndPenalties,

        InternalBumnPortal.FinanceWapuAndPayment.VerifyThreeWayMatching,
        InternalBumnPortal.FinanceWapuAndPayment.VerifyTaxWapuAndEbupot,
        InternalBumnPortal.FinanceWapuAndPayment.IssueBapPaymentMinutes,
        InternalBumnPortal.FinanceWapuAndPayment.DisburseErpPayment,

        InternalBumnPortal.VendorPerformanceVpe.ScoreVendorPerformance,
        InternalBumnPortal.VendorPerformanceVpe.ViewVendorRatingLedger,

        InternalBumnPortal.SystemAdminAndAudit.ManageOrgAndUsersSod,
        InternalBumnPortal.SystemAdminAndAudit.ConfigureDoaMatrices,
        InternalBumnPortal.SystemAdminAndAudit.InspectImmutableAuditChain
    ];
}
