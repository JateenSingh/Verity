import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthStore } from '../auth/auth-store';
import { toProblem } from './to-problem';

/**
 * Normalises every HTTP error into ProblemDetails and rethrows it so
 * callers can still react locally. A 401 on an authenticated request means
 * the session expired server-side (not just "you must log in") - log out
 * and send the user back to login with a returnUrl so they land where they
 * were after re-authenticating.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  const wasAuthenticated = req.headers.has('Authorization');

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      const problem = toProblem(error);

      if (error.status === 401 && wasAuthenticated) {
        authStore.logout();
        void router.navigate(['/login'], {
          queryParams: { returnUrl: router.url, expired: true },
        });
      }

      return throwError(() => problem);
    }),
  );
};
