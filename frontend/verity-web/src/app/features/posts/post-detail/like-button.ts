import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-like-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './like-button.html',
  styleUrl: './like-button.scss',
})
export class LikeButton {
  readonly liked = input.required<boolean>();
  readonly count = input.required<number>();
  readonly disabled = input(false);
  readonly disabledReason = input<string | undefined>();
  readonly pending = input(false);
  readonly toggle = output<void>();

  protected readonly title = computed(() => (this.disabled() ? (this.disabledReason() ?? '') : ''));
}
