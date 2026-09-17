import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthStore } from './auth-store';

describe('authGuard', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  function run(url: string) {
    const state = { url } as RouterStateSnapshot;
    return TestBed.runInInjectionContext(() => authGuard({} as ActivatedRouteSnapshot, state));
  }

  it('redirects to /login with a returnUrl when anonymous', () => {
    const result = run('/posts/new') as UrlTree;

    expect(result).toBeInstanceOf(UrlTree);
    expect(result.toString()).toContain('/login');
    expect(result.toString()).toContain('returnUrl=%2Fposts%2Fnew');
  });

  it('allows navigation when authenticated', () => {
    localStorage.setItem(
      'verity.auth',
      JSON.stringify({
        token: 't',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        user: { id: 'u1', username: 'alice', role: 'User' },
      }),
    );
    TestBed.inject(AuthStore);

    const result = run('/posts/new');

    expect(result).toBe(true);
  });
});
