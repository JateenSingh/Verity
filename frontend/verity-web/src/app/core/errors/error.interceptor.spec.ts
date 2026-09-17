import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { AuthStore } from '../auth/auth-store';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: Router;
  let authStore: AuthStore;

  beforeEach(() => {
    localStorage.setItem(
      'verity.auth',
      JSON.stringify({
        token: 't',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        user: { id: 'u1', username: 'alice', role: 'User' },
      }),
    );

    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([errorInterceptor])), provideHttpClientTesting(), provideRouter([])],
    });

    authStore = TestBed.inject(AuthStore);
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('maps a ProblemDetails error body through to the caller', () => {
    let caught: unknown;
    http.get('/api/v1/posts/x').subscribe({ error: (err) => (caught = err) });

    const req = httpMock.expectOne('/api/v1/posts/x');
    req.flush({ title: 'Not Found', status: 404, type: 'https://verity.local/problems/not-found' }, { status: 404, statusText: 'Not Found' });

    expect(caught).toEqual(expect.objectContaining({ title: 'Not Found', status: 404 }));
  });

  it('maps a network failure to a synthetic Network error problem', () => {
    let caught: unknown;
    http.get('/api/v1/posts/x').subscribe({ error: (err) => (caught = err) });

    const req = httpMock.expectOne('/api/v1/posts/x');
    req.error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });

    expect(caught).toEqual(expect.objectContaining({ title: 'Network error', status: 0 }));
  });

  it('logs out and redirects to /login on a 401 for an authenticated request', () => {
    http
      .get('/api/v1/users/me', { headers: { Authorization: 'Bearer t' } })
      .subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/v1/users/me');
    req.flush({ title: 'Unauthorized', status: 401 }, { status: 401, statusText: 'Unauthorized' });

    expect(authStore.isAuthenticated()).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/login'], expect.objectContaining({ queryParams: expect.objectContaining({ expired: true }) }));
  });
});
