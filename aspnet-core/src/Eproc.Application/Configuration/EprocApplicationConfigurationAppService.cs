using Eproc.Permissions;

namespace Eproc.Configuration;

public sealed class EprocApplicationConfigurationAppService
{
    private readonly EprocClientOptions _clientOptions;
    private readonly EprocFeatureOptions _featureOptions;
    private readonly EprocIntegrationOptions _integrationOptions;

    public EprocApplicationConfigurationAppService(
        EprocClientOptions clientOptions,
        EprocFeatureOptions featureOptions,
        EprocIntegrationOptions integrationOptions)
    {
        _clientOptions = clientOptions;
        _featureOptions = featureOptions;
        _integrationOptions = integrationOptions;
    }

    public EprocApplicationConfigurationDto GetConfiguration(EprocCurrentUserDto? currentUser = null)
    {
        var policies = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var perm in EprocPermissions.GetAllPermissions())
        {
            policies[perm] = true;
        }

        return new EprocApplicationConfigurationDto
        {
            ClientEnvironment = _clientOptions,
            Features = _featureOptions,
            Integrations = _integrationOptions,
            CurrentUser = currentUser ?? new EprocCurrentUserDto { IsAuthenticated = false },
            Auth = new EprocAuthConfigDto { GrantedPolicies = policies },
            NavigationSummary = new EprocNavigationSummaryDto(),
            ServerTimeUtc = DateTimeOffset.UtcNow
        };
    }
}
