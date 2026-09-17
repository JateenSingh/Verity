import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { EMPTY, catchError, switchMap } from 'rxjs';
import { ApiClient } from '../../core/api/api-client';
import { PagedResult, PostListQuery, PostSummary, ProblemDetails } from '../../core/api/models';
import { toProblem } from '../../core/errors/to-problem';

export type PostsStatus = 'idle' | 'loading' | 'error';

const DEFAULT_FILTERS: PostListQuery = { page: 1, pageSize: 20, sortBy: 'createdAt', sortDirection: 'desc' };

@Injectable({ providedIn: 'root' })
export class PostsStore {
  private readonly api = inject(ApiClient);

  readonly filters = signal<PostListQuery>(DEFAULT_FILTERS);
  readonly page = signal<PagedResult<PostSummary> | null>(null);
  readonly status = signal<PostsStatus>('idle');
  readonly error = signal<ProblemDetails | null>(null);

  constructor() {
    toObservable(this.filters)
      .pipe(
        switchMap((filters) => {
          this.status.set('loading');
          this.error.set(null);
          return this.api.listPosts(filters).pipe(
            catchError((error: unknown) => {
              const problem = error instanceof HttpErrorResponse ? toProblem(error) : (error as ProblemDetails);
              this.status.set('error');
              this.error.set(problem);
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((page) => {
        this.page.set(page);
        this.status.set('idle');
      });
  }

  setFilters(filters: PostListQuery): void {
    this.filters.set({ ...filters });
  }

  setPage(page: number): void {
    this.filters.update((f) => ({ ...f, page }));
  }

  reset(): void {
    this.filters.set({ ...DEFAULT_FILTERS });
  }

  reload(): void {
    this.filters.update((f) => ({ ...f }));
  }

  /** Applies a like/unlike result to the currently loaded page, if the post is on it. */
  applyLikeStatus(postId: string, likeCount: number, viewerHasLiked: boolean): void {
    const current = this.page();
    if (!current) {
      return;
    }
    this.page.set({
      ...current,
      items: current.items.map((p) => (p.id === postId ? { ...p, likeCount, viewerHasLiked } : p)),
    });
  }
}
