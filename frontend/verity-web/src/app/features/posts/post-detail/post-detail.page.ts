import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ApiClient } from '../../../core/api/api-client';
import { AuthStore } from '../../../core/auth/auth-store';
import { Comment, PagedResult, PostDetail, PostTagName, ProblemDetails } from '../../../core/api/models';
import { toProblem } from '../../../core/errors/to-problem';
import { Alert } from '../../../shared/ui/alert/alert';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { RelativeDatePipe } from '../../../shared/ui/pipes/relative-date.pipe';
import { TagBadge } from '../../../shared/ui/tag-badge/tag-badge';
import { PostsStore } from '../posts.store';
import { CommentForm } from './comment-form';
import { CommentList } from './comment-list';
import { LikeButton } from './like-button';
import { TagControl } from './tag-control';

type Status = 'loading' | 'error' | 'idle';
const MISLEADING_OR_FALSE: PostTagName = 'MisleadingOrFalse';

@Component({
  selector: 'app-post-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Alert, Spinner, RelativeDatePipe, TagBadge, CommentList, CommentForm, LikeButton, TagControl],
  templateUrl: './post-detail.page.html',
  styleUrl: './post-detail.page.scss',
})
export class PostDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiClient);
  protected readonly authStore = inject(AuthStore);
  private readonly postsStore = inject(PostsStore);

  private readonly postId = this.route.snapshot.paramMap.get('id')!;

  protected readonly post = signal<PostDetail | null>(null);
  protected readonly status = signal<Status>('loading');
  protected readonly error = signal<ProblemDetails | null>(null);

  protected readonly comments = signal<Comment[]>([]);
  protected readonly commentsPage = signal(1);
  protected readonly commentsTotalPages = signal(1);
  protected readonly commentPending = signal(false);

  protected readonly likePending = signal(false);
  protected readonly tagPending = signal(false);

  private readonly commentForm = viewChild(CommentForm);

  protected readonly isOwnPost = computed(() => {
    const p = this.post();
    const user = this.authStore.user();
    return !!p && !!user && p.author.id === user.id;
  });

  protected readonly likeDisabled = computed(() => !this.authStore.isAuthenticated() || this.isOwnPost());
  protected readonly likeDisabledReason = computed(() => {
    if (!this.authStore.isAuthenticated()) {
      return 'Log in to like';
    }
    if (this.isOwnPost()) {
      return 'You cannot like your own post';
    }
    return undefined;
  });

  protected readonly isTagged = computed(() => this.post()?.tags.some((t) => t.tag === MISLEADING_OR_FALSE) ?? false);

  constructor() {
    this.loadPost();
    this.loadComments(1);
  }

  protected onRetry(): void {
    this.loadPost();
    this.loadComments(this.commentsPage());
  }

  protected onCommentsPageChange(page: number): void {
    this.loadComments(page);
  }

  protected onSubmitComment(body: string): void {
    this.commentPending.set(true);
    this.api.addComment(this.postId, { body }).subscribe({
      next: (comment) => {
        this.commentPending.set(false);
        this.commentForm()?.reset();
        if (this.commentsPage() === this.commentsTotalPages() || this.comments().length === 0) {
          this.comments.update((c) => [...c, comment]);
        }
        this.post.update((p) => (p ? { ...p, commentCount: p.commentCount + 1 } : p));
      },
      error: () => this.commentPending.set(false),
    });
  }

  protected onToggleLike(): void {
    const p = this.post();
    if (!p || this.likePending()) {
      return;
    }

    const wasLiked = p.viewerHasLiked;
    const previousCount = p.likeCount;
    const optimisticCount = wasLiked ? previousCount - 1 : previousCount + 1;

    this.post.set({ ...p, viewerHasLiked: !wasLiked, likeCount: optimisticCount });
    this.postsStore.applyLikeStatus(p.id, optimisticCount, !wasLiked);
    this.likePending.set(true);

    const request = wasLiked ? this.api.unlike(p.id) : this.api.like(p.id);
    request.subscribe({
      next: (status) => {
        this.likePending.set(false);
        this.post.update((current) => (current ? { ...current, likeCount: status.likeCount, viewerHasLiked: status.viewerHasLiked } : current));
        this.postsStore.applyLikeStatus(p.id, status.likeCount, status.viewerHasLiked);
      },
      error: () => {
        this.likePending.set(false);
        this.post.set(p);
        this.postsStore.applyLikeStatus(p.id, previousCount, wasLiked);
      },
    });
  }

  protected onTag(reason: string | undefined): void {
    const p = this.post();
    if (!p) {
      return;
    }
    this.tagPending.set(true);
    this.api.addTag(p.id, MISLEADING_OR_FALSE, reason).subscribe({
      next: (updated) => {
        this.tagPending.set(false);
        this.post.set(updated);
      },
      error: () => this.tagPending.set(false),
    });
  }

  protected onUntag(): void {
    const p = this.post();
    if (!p) {
      return;
    }
    this.tagPending.set(true);
    this.api.removeTag(p.id, MISLEADING_OR_FALSE).subscribe({
      next: () => {
        this.tagPending.set(false);
        this.post.update((current) => (current ? { ...current, tags: current.tags.filter((t) => t.tag !== MISLEADING_OR_FALSE) } : current));
      },
      error: () => this.tagPending.set(false),
    });
  }

  private loadPost(): void {
    this.status.set('loading');
    this.error.set(null);
    this.api.getPost(this.postId).subscribe({
      next: (post) => {
        this.post.set(post);
        this.status.set('idle');
      },
      error: (err: unknown) => {
        this.status.set('error');
        this.error.set(err instanceof HttpErrorResponse ? toProblem(err) : (err as ProblemDetails));
      },
    });
  }

  private loadComments(page: number): void {
    this.api.listComments(this.postId, { page, pageSize: 10, sortDirection: 'asc' }).subscribe({
      next: (result: PagedResult<Comment>) => {
        this.comments.set(result.items);
        this.commentsPage.set(result.page);
        this.commentsTotalPages.set(result.totalPages);
      },
    });
  }
}
