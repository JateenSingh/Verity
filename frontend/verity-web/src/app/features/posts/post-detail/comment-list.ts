import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Comment } from '../../../core/api/models';
import { Pagination } from '../../../shared/ui/pagination/pagination';
import { EmptyState } from '../../../shared/ui/empty-state/empty-state';
import { RelativeDatePipe } from '../../../shared/ui/pipes/relative-date.pipe';

@Component({
  selector: 'app-comment-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Pagination, EmptyState, RelativeDatePipe],
  templateUrl: './comment-list.html',
  styleUrl: './comment-list.scss',
})
export class CommentList {
  readonly comments = input.required<Comment[]>();
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly pageChange = output<number>();
}
