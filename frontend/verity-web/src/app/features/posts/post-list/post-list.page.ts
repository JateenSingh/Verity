import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { PostListQuery } from '../../../core/api/models';
import { Alert } from '../../../shared/ui/alert/alert';
import { EmptyState } from '../../../shared/ui/empty-state/empty-state';
import { Pagination } from '../../../shared/ui/pagination/pagination';
import { PostsStore } from '../posts.store';
import { PostCard } from './post-card';
import { PostFilters } from './post-filters';

@Component({
  selector: 'app-post-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PostFilters, PostCard, Pagination, Alert, EmptyState],
  templateUrl: './post-list.page.html',
  styleUrl: './post-list.page.scss',
})
export class PostListPage {
  protected readonly store = inject(PostsStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly initialFilters: PostListQuery;

  constructor() {
    this.initialFilters = this.queryParamsToFilters(this.route.snapshot.queryParamMap);
    this.store.setFilters(this.initialFilters);

    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      const fromUrl = this.queryParamsToFilters(params);
      if (JSON.stringify(fromUrl) !== JSON.stringify(this.store.filters())) {
        this.store.setFilters(fromUrl);
      }
    });
  }

  protected onApplyFilters(filters: PostListQuery): void {
    this.updateUrlAndStore({ ...filters, page: 1, pageSize: this.store.filters().pageSize });
  }

  protected onResetFilters(): void {
    this.store.reset();
    void this.router.navigate([], { queryParams: {} });
  }

  protected onPageChange(page: number): void {
    this.updateUrlAndStore({ ...this.store.filters(), page });
  }

  protected onRetry(): void {
    this.store.reload();
  }

  private updateUrlAndStore(filters: PostListQuery): void {
    this.store.setFilters(filters);
    void this.router.navigate([], { queryParams: this.filtersToQueryParams(filters) });
  }

  private queryParamsToFilters(params: import('@angular/router').ParamMap): PostListQuery {
    return {
      page: params.get('page') ? Number(params.get('page')) : 1,
      pageSize: params.get('pageSize') ? Number(params.get('pageSize')) : 20,
      dateFrom: params.get('dateFrom') ?? undefined,
      dateTo: params.get('dateTo') ?? undefined,
      author: params.get('author') ?? undefined,
      tag: (params.get('tag') as PostListQuery['tag']) ?? undefined,
      untagged: params.get('untagged') === 'true',
      sortBy: (params.get('sortBy') as PostListQuery['sortBy']) ?? 'createdAt',
      sortDirection: (params.get('sortDirection') as PostListQuery['sortDirection']) ?? 'desc',
    };
  }

  private filtersToQueryParams(filters: PostListQuery): Record<string, string | number | null> {
    return {
      page: filters.page && filters.page > 1 ? filters.page : null,
      pageSize: filters.pageSize && filters.pageSize !== 20 ? filters.pageSize : null,
      dateFrom: filters.dateFrom ?? null,
      dateTo: filters.dateTo ?? null,
      author: filters.author ?? null,
      tag: filters.tag ?? null,
      untagged: filters.untagged ? 'true' : null,
      sortBy: filters.sortBy && filters.sortBy !== 'createdAt' ? filters.sortBy : null,
      sortDirection: filters.sortDirection && filters.sortDirection !== 'desc' ? filters.sortDirection : null,
    };
  }
}
