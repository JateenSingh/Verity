import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PagedResult, PostSummary } from '../../../core/api/models';
import { PostListPage } from './post-list.page';

function samplePage(items: Partial<PostSummary>[] = []): PagedResult<PostSummary> {
  return {
    items: items.map((overrides, i) => ({
      id: `p${i}`,
      title: `Post ${i}`,
      excerpt: 'Excerpt',
      author: { id: 'a1', username: 'alice', role: 'User' },
      createdAt: new Date().toISOString(),
      likeCount: 0,
      commentCount: 0,
      tags: [],
      viewerHasLiked: false,
      ...overrides,
    })),
    page: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: items.length > 0 ? 1 : 0,
  };
}

describe('PostListPage', () => {
  let fixture: ComponentFixture<PostListPage>;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PostListPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap({}) },
            queryParamMap: of(convertToParamMap({})),
          },
        },
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(PostListPage);
  });

  afterEach(() => httpMock.verify());

  function pendingRequest() {
    return httpMock.expectOne((r) => r.url.startsWith(`${environment.apiBaseUrl}/posts`));
  }

  it('renders a loading skeleton while the request is in flight', () => {
    fixture.detectChanges();

    const skeletons = fixture.nativeElement.querySelectorAll('.post-list__skeleton');
    expect(skeletons.length).toBeGreaterThan(0);

    pendingRequest().flush(samplePage());
  });

  it('renders post cards on success', () => {
    fixture.detectChanges();
    pendingRequest().flush(samplePage([{ title: 'Hello world' }]));
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('app-post-card');
    expect(cards.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Hello world');
  });

  it('renders the empty state with a reset link when there are no posts', () => {
    fixture.detectChanges();
    pendingRequest().flush(samplePage([]));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No posts match these filters');
    const resetButton = fixture.nativeElement.querySelector('.empty-state__action');
    expect(resetButton).toBeTruthy();
  });

  it('renders an error alert with a retry action on failure', () => {
    fixture.detectChanges();
    pendingRequest().flush({ title: 'Server error', status: 500 }, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Server error');
    const retryButton = fixture.nativeElement.querySelector('.alert__retry');
    expect(retryButton).toBeTruthy();
  });
});
