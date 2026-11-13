namespace Eproc.Configuration;

/// <summary>
/// Konfigurasi identitas klien BUMN yang dimuat dari appsettings.json atau Environment Variables (EPROC_CLIENT_*).
/// Setiap klien BUMN di-deploy di server masing-masing (Dedicated Instance) tanpa pencampuran tenant_id di database.
/// </summary>
public sealed class EprocClientOptions
{
    public const string SectionName = "EprocClient";

    public string ClientCode { get; set; } = "BUMN_DEFAULT_01";
    public string CompanyName { get; set; } = "PT BUMN Persero Tbk";
    public string ClientShortName { get; set; } = "BUMN Persero";
    public string Npwp { get; set; } = "01.000.000.0-093.000";
    public bool IsWapuPpn { get; set; } = true;
    public string RegulationReference { get; set; } = "PER-2/MBU/03/2023";
    public bool AllowSubsidiaryConsolidation { get; set; } = true;
    public string DefaultCurrency { get; set; } = "IDR";
    public string PrimaryThemeColor { get; set; } = "#0F4C81";
    public string LogoUrl { get; set; } = "/assets/branding/logo-client.svg";
    public string SupportEmail { get; set; } = "helpdesk.eproc@bumn.co.id";
}

/// <summary>
/// Feature flags & parameter regulasi pengadaan BUMN per server instalasi.
/// </summary>
public sealed class EprocFeatureOptions
{
    public const string SectionName = "EprocFeatures";

    public bool EnableCsmsModule { get; set; } = true;
    public bool EnableEReverseAuction { get; set; } = true;
    public bool EnableECatalogUmbrella { get; set; } = true;
    public bool EnableTwoStageTender { get; set; } = true;
    public bool EnableBeautyContest { get; set; } = true;

    /// <summary>Batas wajib penggunaan Produk Dalam Negeri jika TKDN + BMP >= 40%.</summary>
    public decimal MandatoryTkdnThresholdPct { get; set; } = 40.0m;

    /// <summary>Ambang batas minimal TKDN untuk mendapatkan Preferensi Harga (HEA) >= 25%.</summary>
    public decimal MinTkdnPreferenceEligiblePct { get; set; } = 25.0m;

    /// <summary>Batas maksimal preferensi harga untuk Pengadaan Barang (25% = 0.25).</summary>
    public decimal GoodsTkdnMaxPreferencePct { get; set; } = 25.0m;

    /// <summary>Batas maksimal preferensi harga untuk Pekerjaan Konstruksi (7.5% = 0.075).</summary>
    public decimal ConstructionTkdnMaxPreferencePct { get; set; } = 7.5m;

    /// <summary>Tarif denda keterlambatan harian dalam permil (1.0 = 1/1000 per hari).</summary>
    public decimal DefaultLatePenaltyPerMille { get; set; } = 1.0m;

    /// <summary>Plafon maksimal denda keterlambatan terhadap nilai kontrak (5.0%).</summary>
    public decimal MaxPenaltyCapPct { get; set; } = 5.0m;
}

/// <summary>
/// Konfigurasi integrasi pihak ketiga per server BUMN (SAP/Oracle, BSrE, Peruri, PaDi UMKM).
/// </summary>
public sealed class EprocIntegrationOptions
{
    public const string SectionName = "EprocIntegrations";

    public string ErpProvider { get; set; } = "SAP_S4HANA";
    public string DigitalSignatureProvider { get; set; } = "BSRE_BSSN";
    public string EMeteraiProvider { get; set; } = "PERURI";
    public bool PadiUmkmSyncEnabled { get; set; } = true;
}
