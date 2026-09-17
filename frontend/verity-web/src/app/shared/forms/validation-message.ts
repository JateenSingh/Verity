import { ChangeDetectionStrategy, Component, effect, input, signal } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { merge } from 'rxjs';
import { firstErrorMessage } from './error-map';

@Component({
  selector: 'app-validation-message',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (shouldShow()) {
      <p class="validation-message">{{ message() }}</p>
    }
  `,
  styles: [
    `
      .validation-message {
        margin: var(--space-1) 0 0;
        font-size: 0.8rem;
        color: var(--color-danger);
      }
    `,
  ],
})
export class ValidationMessage {
  readonly control = input.required<AbstractControl | null>();

  // AbstractControl mutates in place (same reference), so OnPush's input-
  // reference check alone would never re-render this on status/value
  // changes - subscribe to its own change streams and bump a signal instead.
  private readonly tick = signal(0);

  constructor() {
    effect((onCleanup) => {
      const c = this.control();
      if (!c) {
        return;
      }
      const subscription = merge(c.valueChanges, c.statusChanges).subscribe(() => this.tick.update((v) => v + 1));
      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected shouldShow(): boolean {
    this.tick();
    const c = this.control();
    return !!c && c.invalid && (c.touched || c.dirty);
  }

  protected message(): string | null {
    this.tick();
    return firstErrorMessage(this.control()?.errors ?? null);
  }
}
