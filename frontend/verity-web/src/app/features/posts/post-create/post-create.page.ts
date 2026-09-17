import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiClient } from '../../../core/api/api-client';
import { ProblemDetails } from '../../../core/api/models';
import { toProblem } from '../../../core/errors/to-problem';
import { ValidationMessage } from '../../../shared/forms/validation-message';

@Component({
  selector: 'app-post-create-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, ValidationMessage],
  templateUrl: './post-create.page.html',
  styleUrl: './post-create.page.scss',
})
export class PostCreatePage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiClient);
  private readonly router = inject(Router);

  protected readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
    body: ['', [Validators.required, Validators.maxLength(10000)]],
  });

  protected readonly submitting = signal(false);
  protected readonly formError = signal<ProblemDetails | null>(null);

  protected submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);

    this.api.createPost(this.form.getRawValue()).subscribe({
      next: (post) => {
        this.submitting.set(false);
        void this.router.navigate(['/posts', post.id]);
      },
      error: (err: unknown) => {
        this.submitting.set(false);
        this.formError.set(err instanceof HttpErrorResponse ? toProblem(err) : (err as ProblemDetails));
      },
    });
  }
}
