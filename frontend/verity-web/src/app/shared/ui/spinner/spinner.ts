import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-spinner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="spinner" [attr.aria-label]="label()" role="status"></span>`,
  styles: [
    `
      .spinner {
        display: inline-block;
        width: 1.25rem;
        height: 1.25rem;
        border: 2px solid var(--color-border);
        border-top-color: var(--color-primary);
        border-radius: 50%;
        animation: spin 0.7s linear infinite;
      }

      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }
    `,
  ],
})
export class Spinner {
  readonly label = input('Loading');
}
