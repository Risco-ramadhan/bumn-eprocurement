using Eproc.Configuration;

namespace Eproc.Configuration;

/// <summary>
/// Kontrak DTO Bootstrap (/api/eproc/application-configuration) yang dipanggil oleh Angular APP_INITIALIZER.
/// Mengikuti perilaku standar framework enterprise untuk memuat profil klien BUMN dari environment/appsettings.json,
/// feature flags, integrasi, dan daftar permission user tanpa ketergantungan tenant_id di database.
/// </summary>
public sealed class EprocApplicationConfigurationDto
{
    public EprocClientOptions ClientEnvironment { get; set; } = new();
    public EprocFeatureOptions Features { get; set; } = new();
    public EprocIntegrationOptions Integrations { get; set; } = new();
    public EprocCurrentUserDto CurrentUser { get; set; } = new();
    public EprocAuthConfigDto Auth { get; set; } = new();
    public EprocNavigationSummaryDto NavigationSummary { get; set; } = new();
    public DateTimeOffset ServerTimeUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EprocCurrentUserDto
{
    public bool IsAuthenticated { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PositionGrade { get; set; }
    public string[] Roles { get; set; } = [];
    public Guid? CompanyEntityId { get; set; }
    public Guid? VendorId { get; set; }
}

public sealed class EprocAuthConfigDto
{
    public Dictionary<string, bool> GrantedPolicies { get; set; } = new();
}

public sealed class EprocNavigationSummaryDto
{
    public int PublicPortalMainMenus { get; set; } = 5;
    public int VendorPortalMainMenus { get; set; } = 9;
    public int VendorPortalSubMenus { get; set; } = 34;
    public int InternalPortalMainMenus { get; set; } = 11;
    public int InternalPortalSubMenus { get; set; } = 52;
    public int TotalMainMenus => PublicPortalMainMenus + VendorPortalMainMenus + InternalPortalMainMenus;
    public int TotalSubMenus => VendorPortalSubMenus + InternalPortalSubMenus;
}
