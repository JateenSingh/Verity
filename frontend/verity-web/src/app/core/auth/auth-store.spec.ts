import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth-store';

const STORAGE_KEY = 'verity.auth';

describe('AuthStore', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('restores a valid persisted session', () => {
    const stored = {
      token: 'abc123',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: 'u1', username: 'alice', role: 'User' },
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));

    const store = TestBed.inject(AuthStore);

    expect(store.isAuthenticated()).toBe(true);
    expect(store.token()).toBe('abc123');
    expect(store.user()?.username).toBe('alice');
  });

  it('discards an expired persisted session', () => {
    const stored = {
      token: 'abc123',
      expiresAt: new Date(Date.now() - 60_000).toISOString(),
      user: { id: 'u1', username: 'alice', role: 'User' },
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));

    const store = TestBed.inject(AuthStore);

    expect(store.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
  });

  it('logout() clears both signals and storage', () => {
    const stored = {
      token: 'abc123',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: 'u1', username: 'alice', role: 'User' },
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
    const store = TestBed.inject(AuthStore);

    store.logout();

    expect(store.isAuthenticated()).toBe(false);
    expect(store.user()).toBeNull();
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
  });

  it('isModerator is computed from the restored role', () => {
    const stored = {
      token: 'abc123',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: 'u1', username: 'mod', role: 'Moderator' },
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));

    const store = TestBed.inject(AuthStore);

    expect(store.isModerator()).toBe(true);
  });
});
