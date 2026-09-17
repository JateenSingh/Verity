import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { PostTag } from '../../../core/api/models';

@Component({
  selector: 'app-tag-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="tag-badge" [attr.title]="tag().reason ? tag().reason : null">
      Misleading or false information
    </span>
  `,
  styles: [
    `
      .tag-badge {
        display: inline-block;
        background: var(--color-tag-bg);
        color: var(--color-tag);
        border-radius: var(--radius-sm);
        padding: var(--space-1) var(--space-2);
        font-size: 0.75rem;
        font-weight: 600;
        cursor: default;
      }
    `,
  ],
})
export class TagBadge {
  readonly tag = input.required<PostTag>();
}
