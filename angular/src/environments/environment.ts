import { EprocEnvironment } from '../app/core/config/eproc-config.model';

export const environment: EprocEnvironment = {
  production: false,
  application: {
    name: 'Eproc Enterprise S2P',
    prefix: 'eproc',
    logoUrl: '/assets/branding/logo-bumn-karya.svg',
  },
  apis: {
    default: {
      url: 'https://localhost:44388',
      rootNamespace: 'Eproc',
    },
  },
  remoteEnv: {
    url: '/assets/appsettings.json',
    mergeStrategy: 'deepmerge',
  },
};
