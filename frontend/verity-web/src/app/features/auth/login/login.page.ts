import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthStore } from '../../../core/auth/auth-store';
import { toProblem } from '../../../core/errors/to-problem';
import { ProblemDetails } from '../../../core/api/models';
import { ValidationMessage } from '../../../shared/forms/validation-message';

@Component({
  selector: 'app-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, ValidationMessage],
  templateUrl: './login.page.html',
  styleUrl: '../auth-form.scss',
})
export class LoginPage {
  private readonly fb = inject(FormBuilder);
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  protected readonly submitting = signal(false);
  protected readonly formError = signal<ProblemDetails | null>(null);
  protected readonly sessionExpired = this.route.snapshot.queryParamMap.get('expired') === 'true';

  protected async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);

    try {
      await this.authStore.login(this.form.getRawValue());
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/posts';
      await this.router.navigateByUrl(returnUrl);
    } catch (err) {
      const problem = err instanceof HttpErrorResponse ? toProblem(err) : (err as ProblemDetails);
      this.formError.set(problem);
    } finally {
      this.submitting.set(false);
    }
  }
}
