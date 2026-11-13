import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { EprocConfigService, initializeEprocApp } from './core/config/eproc-config.service';
import { eprocHttpInterceptor } from './core/interceptors/eproc-http.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter([]),
    provideHttpClient(withInterceptors([eprocHttpInterceptor])),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeEprocApp,
      deps: [EprocConfigService],
      multi: true,
    },
  ],
};
