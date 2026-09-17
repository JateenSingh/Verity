import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { AuthStore } from './auth-store';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()],
    });
  });

  afterEach(() => httpMock.verify());

  it('adds the bearer header for API requests when a token exists', () => {
    const stored = {
      token: 'my-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: 'u1', username: 'alice', role: 'User' },
    };
    localStorage.setItem('verity.auth', JSON.stringify(stored));

    // AuthStore reads localStorage in its constructor, so it must be
    // constructed (via first injection) after the storage is seeded.
    TestBed.inject(AuthStore);
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);

    http.get(`${environment.apiBaseUrl}/posts`).subscribe();
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/posts`);

    expect(req.request.headers.get('Authorization')).toBe('Bearer my-token');
    req.flush({});
  });

  it('does not add the header when no token exists', () => {
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);

    http.get(`${environment.apiBaseUrl}/posts`).subscribe();
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/posts`);

    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('does not add the header for a request that does not target the API', () => {
    const stored = {
      token: 'my-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: 'u1', username: 'alice', role: 'User' },
    };
    localStorage.setItem('verity.auth', JSON.stringify(stored));
    TestBed.inject(AuthStore);
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);

    http.get('https://example.com/other').subscribe();
    const req = httpMock.expectOne('https://example.com/other');

    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });
});
