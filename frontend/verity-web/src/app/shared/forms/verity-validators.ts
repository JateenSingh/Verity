import { AbstractControl, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';

/** Mirrors the API's username rule: ^[a-zA-Z0-9_]{3,32}$ */
export const usernamePattern = Validators.pattern(/^[a-zA-Z0-9_]{3,32}$/);

/** Mirrors PasswordPolicy: 8-128 chars, at least one letter and one digit. */
export function passwordPolicyValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string | null;
  if (!value) {
    return null;
  }
  const validLength = value.length >= 8 && value.length <= 128;
  const hasLetter = /[a-zA-Z]/.test(value);
  const hasDigit = /\d/.test(value);
  return validLength && hasLetter && hasDigit ? null : { passwordPolicy: true };
}

/** Cross-field validator placed on the form group, flags the confirm control. */
export function passwordsMatchValidator(passwordKey: string, confirmKey: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const password = group.get(passwordKey)?.value;
    const confirm = group.get(confirmKey)?.value;
    const confirmControl = group.get(confirmKey);

    if (confirmControl && password !== confirm) {
      confirmControl.setErrors({ ...confirmControl.errors, passwordMismatch: true });
    } else if (confirmControl?.errors) {
      const { passwordMismatch, ...rest } = confirmControl.errors;
      confirmControl.setErrors(Object.keys(rest).length ? rest : null);
    }

    return null;
  };
}
