import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PostSummary } from '../../../core/api/models';
import { RelativeDatePipe } from '../../../shared/ui/pipes/relative-date.pipe';
import { TagBadge } from '../../../shared/ui/tag-badge/tag-badge';

@Component({
  selector: 'app-post-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RelativeDatePipe, TagBadge],
  templateUrl: './post-card.html',
  styleUrl: './post-card.scss',
})
export class PostCard {
  readonly post = input.required<PostSummary>();
}
