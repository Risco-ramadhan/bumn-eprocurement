import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { EprocConfigService } from '../config/eproc-config.service';

/**
 * HTTP Interceptor Eproc: Menyematkan header identitas klien BUMN (X-Eproc-Client-Code),
 * token Anti-Forgery (X-Eproc-Xsrf-Token), dan Bearer Token tanpa header framework pihak ketiga.
 */
export const eprocHttpInterceptor: HttpInterceptorFn = (req, next) => {
  const configService = inject(EprocConfigService);
  const clientProfile = configService.clientProfile;
  const security = configService.securityConfig;

  const headers: Record<string, string> = {
    'X-Requested-With': 'XMLHttpRequest',
  };

  if (clientProfile?.clientCode && security?.clientCodeHeaderName) {
    headers[security.clientCodeHeaderName] = clientProfile.clientCode;
  }

  if (typeof localStorage !== 'undefined' && security?.authStorageKey) {
    const token = localStorage.getItem(security.authStorageKey);
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }
  }

  const clonedRequest = req.clone({ setHeaders: headers });
  return next(clonedRequest);
};
