import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-tag-control',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './tag-control.html',
  styleUrl: './tag-control.scss',
})
export class TagControl {
  private readonly fb = inject(FormBuilder);

  readonly tagged = input.required<boolean>();
  readonly pending = input(false);
  readonly tag = output<string | undefined>();
  readonly untag = output<void>();

  protected readonly reasonOpen = signal(false);
  protected readonly form = this.fb.nonNullable.group({
    reason: ['', [Validators.maxLength(500)]],
  });

  protected openReason(): void {
    this.reasonOpen.set(true);
  }

  protected confirmTag(): void {
    const reason = this.form.getRawValue().reason.trim();
    this.tag.emit(reason || undefined);
    this.reasonOpen.set(false);
    this.form.reset({ reason: '' });
  }

  protected cancel(): void {
    this.reasonOpen.set(false);
    this.form.reset({ reason: '' });
  }
}
