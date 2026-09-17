import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { moderatorGuard } from './moderator.guard';
import { AuthStore } from './auth-store';

describe('moderatorGuard', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  function run(url: string) {
    const state = { url } as RouterStateSnapshot;
    return TestBed.runInInjectionContext(() => moderatorGuard({} as ActivatedRouteSnapshot, state));
  }

  function seedUser(role: 'User' | 'Moderator') {
    localStorage.setItem(
      'verity.auth',
      JSON.stringify({
        token: 't',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        user: { id: 'u1', username: 'someone', role },
      }),
    );
    TestBed.inject(AuthStore);
  }

  it('redirects to /login with a returnUrl when anonymous', () => {
    const result = run('/moderation') as UrlTree;

    expect(result).toBeInstanceOf(UrlTree);
    expect(result.toString()).toContain('/login');
  });

  it('allows navigation for a moderator', () => {
    seedUser('Moderator');

    expect(run('/moderation')).toBe(true);
  });

  it('redirects a regular (non-moderator) authenticated user away', () => {
    seedUser('User');

    const result = run('/moderation') as UrlTree;

    expect(result).toBeInstanceOf(UrlTree);
    expect(result.toString()).not.toContain('/login');
  });
});
