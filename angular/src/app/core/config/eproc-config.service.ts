import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  EprocBootstrapConfiguration,
  EprocClientProfile,
  EprocRuntimeAppSettings,
} from './eproc-config.model';

/**
 * Layanan Bootstrap Konfigurasi Eproc (mengikuti perilaku APP_INITIALIZER + remoteEnv framework enterprise,
 * namun 100% bersih dengan namespace dan prefix 'eproc').
 * Memisahkan identitas klien BUMN melalui environment.ts & assets/appsettings.json tanpa tenant_id di database.
 */
@Injectable({ providedIn: 'root' })
export class EprocConfigService {
  private runtimeSettings!: EprocRuntimeAppSettings;
  private readonly bootstrapConfigSubject =
    new BehaviorSubject<EprocBootstrapConfiguration | null>(null);

  readonly bootstrapConfig$ = this.bootstrapConfigSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  async initializeApplication(): Promise<void> {
    // 1. Muat konfigurasi runtime dari /assets/appsettings.json (sesuai environment.remoteEnv.url)
    this.runtimeSettings = await firstValueFrom(
      this.http.get<EprocRuntimeAppSettings>(environment.remoteEnv.url)
    );

    this.applyClientThemeBranding(this.runtimeSettings.clientEnvironment);

    // 2. Sinkronkan konfigurasi aplikasi & permission dari server (/api/eproc/application-configuration)
    try {
      const apiUrl = `${this.runtimeSettings.remoteServiceBaseUrl}/api/eproc/application-configuration`;
      const serverConfig = await firstValueFrom(
        this.http.get<EprocBootstrapConfiguration>(apiUrl)
      );
      this.bootstrapConfigSubject.next(serverConfig);
      this.applyClientThemeBranding(serverConfig.clientEnvironment);
    } catch {
      // Fallback jika API belum menyala saat mode preview statis
    }
  }

  get apiBaseUrl(): string {
    return this.runtimeSettings?.remoteServiceBaseUrl || environment.apis.default.url;
  }

  get clientProfile(): EprocClientProfile {
    return (
      this.bootstrapConfigSubject.value?.clientEnvironment ??
      this.runtimeSettings?.clientEnvironment
    );
  }

  get securityConfig(): EprocRuntimeAppSettings['security'] {
    return this.runtimeSettings?.security;
  }

  hasPermission(permissionKey: string): boolean {
    const policies = this.bootstrapConfigSubject.value?.auth?.grantedPolicies;
    return policies ? Boolean(policies[permissionKey]) : false;
  }

  private applyClientThemeBranding(client: EprocClientProfile): void {
    if (typeof document !== 'undefined' && client?.primaryThemeColor) {
      document.documentElement.style.setProperty(
        '--eproc-primary-color',
        client.primaryThemeColor
      );
      document.title = `${client.clientShortName} — Enterprise e-Procurement (S2P)`;
    }
  }
}

export function initializeEprocApp(configService: EprocConfigService): () => Promise<void> {
  return () => configService.initializeApplication();
}
