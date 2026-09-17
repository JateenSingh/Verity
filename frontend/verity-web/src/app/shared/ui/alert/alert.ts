import { ChangeDetectionStrategy, Component, ElementRef, afterNextRender, input, output, viewChild } from '@angular/core';

@Component({
  selector: 'app-alert',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './alert.html',
  styleUrl: './alert.scss',
})
export class Alert {
  readonly title = input.required<string>();
  readonly detail = input<string | undefined>();
  readonly showRetry = input(false);
  readonly retry = output<void>();

  private readonly alertEl = viewChild<ElementRef<HTMLElement>>('alertEl');

  constructor() {
    afterNextRender(() => this.alertEl()?.nativeElement.focus());
  }
}
