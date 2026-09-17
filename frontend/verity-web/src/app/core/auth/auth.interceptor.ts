import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthStore } from './auth-store';

/**
 * Attaches the bearer token only to requests targeting our own API - never
 * to third-party requests that might accidentally share an HttpClient.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authStore = inject(AuthStore);
  const token = authStore.token();

  const isApiRequest = req.url.startsWith(environment.apiBaseUrl) || req.url.includes(environment.apiBaseUrl);

  if (token && isApiRequest) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    });
  }

  return next(req);
};
