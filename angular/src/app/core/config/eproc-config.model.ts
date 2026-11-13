export interface EprocEnvironment {
  production: boolean;
  application: {
    name: string;
    prefix: 'eproc';
    logoUrl: string;
  };
  apis: {
    default: {
      url: string;
      rootNamespace: string;
    };
  };
  remoteEnv: {
    url: string;
    mergeStrategy: 'deepmerge' | 'overwrite';
  };
}

export interface EprocRuntimeAppSettings {
  remoteServiceBaseUrl: string;
  signalRHubUrl: string;
  clientEnvironment: EprocClientProfile;
  security: {
    xsrfCookieName: string;
    xsrfHeaderName: string;
    clientCodeHeaderName: string;
    authStorageKey: string;
  };
}

export interface EprocClientProfile {
  clientCode: string;
  companyName: string;
  clientShortName: string;
  npwp: string;
  isWapuPpn: boolean;
  regulationReference: string;
  allowSubsidiaryConsolidation: boolean;
  defaultCurrency: string;
  primaryThemeColor: string;
  logoUrl: string;
  supportEmail: string;
}

export interface EprocFeatureFlags {
  enableCsmsModule: boolean;
  enableEReverseAuction: boolean;
  enableECatalogUmbrella: boolean;
  enableTwoStageTender: boolean;
  enableBeautyContest: boolean;
  mandatoryTkdnThresholdPct: number;
  minTkdnPreferenceEligiblePct: number;
  goodsTkdnMaxPreferencePct: number;
  constructionTkdnMaxPreferencePct: number;
  defaultLatePenaltyPerMille: number;
  maxPenaltyCapPct: number;
}

export interface EprocBootstrapConfiguration {
  clientEnvironment: EprocClientProfile;
  features: EprocFeatureFlags;
  integrations: {
    erpProvider: string;
    digitalSignatureProvider: string;
    eMeteraiProvider: string;
    padiUmkmSyncEnabled: boolean;
  };
  currentUser: {
    isAuthenticated: boolean;
    userId?: string;
    userName?: string;
    fullName?: string;
    email?: string;
    positionGrade?: string;
    roles: string[];
    companyEntityId?: string;
    vendorId?: string;
  };
  auth: {
    grantedPolicies: Record<string, boolean>;
  };
  navigationSummary: {
    publicPortalMainMenus: number;
    vendorPortalMainMenus: number;
    vendorPortalSubMenus: number;
    internalPortalMainMenus: number;
    internalPortalSubMenus: number;
    totalMainMenus: number;
    totalSubMenus: number;
  };
  serverTimeUtc: string;
}
