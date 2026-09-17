import { ApplicationRef, provideZonelessChangeDetection } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { PagedResult, PostSummary } from '../../core/api/models';
import { PostsStore } from './posts.store';

function samplePage(overrides: Partial<PagedResult<PostSummary>> = {}): PagedResult<PostSummary> {
  return {
    items: [
      {
        id: 'p1',
        title: 'Title',
        excerpt: 'Excerpt',
        author: { id: 'a1', username: 'alice', role: 'User' },
        createdAt: new Date().toISOString(),
        likeCount: 0,
        commentCount: 0,
        tags: [],
        viewerHasLiked: false,
      },
    ],
    page: 1,
    pageSize: 20,
    totalCount: 1,
    totalPages: 1,
    ...overrides,
  };
}

describe('PostsStore', () => {
  let store: PostsStore;
  let httpMock: HttpTestingController;
  let appRef: ApplicationRef;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    store = TestBed.inject(PostsStore);
    httpMock = TestBed.inject(HttpTestingController);
    appRef = TestBed.inject(ApplicationRef);
  });

  afterEach(() => httpMock.verify());

  // toObservable() bridges signal changes to the switchMap pipeline via an
  // effect, which is scheduled (not synchronous) - whenStable() flushes it,
  // same as fixture.detectChanges() would for a component-bound signal.
  function flush() {
    return appRef.whenStable();
  }

  it('load() sets loading then idle with the page on success', async () => {
    store.setFilters({ page: 1, pageSize: 20 });
    await flush();

    expect(store.status()).toBe('loading');

    const req = httpMock.expectOne((r) => r.url.startsWith(`${environment.apiBaseUrl}/posts`));
    req.flush(samplePage());
    await flush();

    expect(store.status()).toBe('idle');
    expect(store.page()?.items).toHaveLength(1);
  });

  it('a filter change cancels the previous in-flight request', async () => {
    store.setFilters({ page: 1, pageSize: 20, author: 'alice' });
    await flush();
    const first = httpMock.expectOne((r) => r.url.startsWith(`${environment.apiBaseUrl}/posts`));

    store.setFilters({ page: 1, pageSize: 20, author: 'bob' });
    await flush();
    const second = httpMock.expectOne((r) => r.url.startsWith(`${environment.apiBaseUrl}/posts`));

    expect(first.cancelled).toBe(true);

    second.flush(samplePage({ items: [{ ...samplePage().items[0], author: { id: 'a2', username: 'bob', role: 'User' } }] }));
    await flush();

    expect(store.page()?.items[0].author.username).toBe('bob');
  });

  it('applyLikeStatus updates the matching post on the current page', async () => {
    store.setFilters({ page: 1, pageSize: 20 });
    await flush();
    const req = httpMock.expectOne((r) => r.url.startsWith(`${environment.apiBaseUrl}/posts`));
    req.flush(samplePage());
    await flush();

    store.applyLikeStatus('p1', 5, true);

    const updated = store.page()?.items.find((p) => p.id === 'p1');
    expect(updated?.likeCount).toBe(5);
    expect(updated?.viewerHasLiked).toBe(true);

    // Simulate a rollback after a failed request.
    store.applyLikeStatus('p1', 0, false);
    const rolledBack = store.page()?.items.find((p) => p.id === 'p1');
    expect(rolledBack?.likeCount).toBe(0);
    expect(rolledBack?.viewerHasLiked).toBe(false);
  });
});
