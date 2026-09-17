import { ValidationErrors } from '@angular/forms';

const MESSAGES: Record<string, (error: unknown) => string> = {
  required: () => 'This field is required.',
  email: () => 'Enter a valid email address.',
  pattern: () => 'This value is not in the expected format.',
  minlength: (e) => `Must be at least ${(e as { requiredLength: number }).requiredLength} characters.`,
  maxlength: (e) => `Must be at most ${(e as { requiredLength: number }).requiredLength} characters.`,
  passwordPolicy: () => 'Must be 8-128 characters and include at least one letter and one digit.',
  passwordMismatch: () => 'Passwords do not match.',
};

/** Picks the first validation error on a control and renders it as text. */
export function firstErrorMessage(errors: ValidationErrors | null): string | null {
  if (!errors) {
    return null;
  }
  const [key, value] = Object.entries(errors)[0] ?? [];
  if (!key) {
    return null;
  }
  return (MESSAGES[key] ?? (() => 'This value is invalid.'))(value);
}
