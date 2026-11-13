import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { EprocAppComponent } from './app/app.component';

bootstrapApplication(EprocAppComponent, appConfig).catch((err) =>
  console.error(err)
);
