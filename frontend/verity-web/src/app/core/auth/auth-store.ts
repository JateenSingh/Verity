import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../api/api-client';
import { AuthResponse, LoginRequest, RegisterRequest, UserSummary } from '../api/models';

const STORAGE_KEY = 'verity.auth';

interface StoredAuth {
  token: string;
  expiresAt: string;
  user: UserSummary;
}

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(ApiClient);

  private readonly userSignal = signal<UserSummary | null>(null);
  private readonly tokenSignal = signal<string | null>(null);
  private expiresAt: string | null = null;

  readonly user = this.userSignal.asReadonly();
  readonly token = this.tokenSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.tokenSignal() !== null);
  readonly isModerator = computed(() => this.userSignal()?.role === 'Moderator');

  constructor() {
    this.restore();
  }

  async login(request: LoginRequest): Promise<void> {
    const response = await firstValueFrom(this.api.login(request));
    this.applyAuthResponse(response);
  }

  async register(request: RegisterRequest): Promise<void> {
    const response = await firstValueFrom(this.api.register(request));
    this.applyAuthResponse(response);
  }

  logout(): void {
    this.userSignal.set(null);
    this.tokenSignal.set(null);
    this.expiresAt = null;
    localStorage.removeItem(STORAGE_KEY);
  }

  private applyAuthResponse(response: AuthResponse): void {
    this.userSignal.set(response.user);
    this.tokenSignal.set(response.accessToken);
    this.expiresAt = response.expiresAt;
    this.persist();
  }

  private persist(): void {
    if (!this.tokenSignal() || !this.userSignal() || !this.expiresAt) {
      return;
    }
    const stored: StoredAuth = {
      token: this.tokenSignal()!,
      expiresAt: this.expiresAt,
      user: this.userSignal()!,
    };
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
    } catch {
      // Storage can be unavailable (private browsing, quota); the in-memory
      // session still works for the current tab.
    }
  }

  private restore(): void {
    let raw: string | null = null;
    try {
      raw = localStorage.getItem(STORAGE_KEY);
    } catch {
      return;
    }
    if (!raw) {
      return;
    }

    try {
      const stored = JSON.parse(raw) as StoredAuth;
      if (new Date(stored.expiresAt).getTime() <= Date.now()) {
        localStorage.removeItem(STORAGE_KEY);
        return;
      }
      this.userSignal.set(stored.user);
      this.tokenSignal.set(stored.token);
      this.expiresAt = stored.expiresAt;
    } catch {
      localStorage.removeItem(STORAGE_KEY);
    }
  }
}
