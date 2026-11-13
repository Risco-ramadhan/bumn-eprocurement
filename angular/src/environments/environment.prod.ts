import { EprocEnvironment } from '../app/core/config/eproc-config.model';

export const environment: EprocEnvironment = {
  production: true,
  application: {
    name: 'Eproc Enterprise S2P',
    prefix: 'eproc',
    logoUrl: '/assets/branding/logo-client.svg',
  },
  apis: {
    default: {
      url: '',
      rootNamespace: 'Eproc',
    },
  },
  remoteEnv: {
    url: '/assets/appsettings.json',
    mergeStrategy: 'overwrite',
  },
};
