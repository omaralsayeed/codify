import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideMonacoEditor } from 'ngx-monaco-editor-v2';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(
      routes,
      withInMemoryScrolling({
        anchorScrolling: 'enabled',          // makes fragment links scroll to id
        scrollPositionRestoration: 'top',    // scroll to top on every route change
      })
    ),
    provideHttpClient(withInterceptorsFromDi()),
    provideMonacoEditor({ baseUrl: 'assets/monaco/vs' }),
  ]
};
