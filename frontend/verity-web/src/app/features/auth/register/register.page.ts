import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ProblemDetails } from '../../../core/api/models';
import { AuthStore } from '../../../core/auth/auth-store';
import { toProblem } from '../../../core/errors/to-problem';
import { ValidationMessage } from '../../../shared/forms/validation-message';
import { passwordPolicyValidator, passwordsMatchValidator, usernamePattern } from '../../../shared/forms/verity-validators';

@Component({
  selector: 'app-register-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, ValidationMessage],
  templateUrl: './register.page.html',
  styleUrl: '../auth-form.scss',
})
export class RegisterPage {
  private readonly fb = inject(FormBuilder);
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  protected readonly form = this.fb.nonNullable.group(
    {
      username: ['', [Validators.required, usernamePattern]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, passwordPolicyValidator]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatchValidator('password', 'confirmPassword') },
  );

  protected readonly submitting = signal(false);
  protected readonly formError = signal<ProblemDetails | null>(null);

  protected async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);

    try {
      const { username, email, password } = this.form.getRawValue();
      await this.authStore.register({ username, email, password });
      await this.router.navigateByUrl('/posts');
    } catch (err) {
      const problem = err instanceof HttpErrorResponse ? toProblem(err) : (err as ProblemDetails);
      this.formError.set(problem);
    } finally {
      this.submitting.set(false);
    }
  }
}
