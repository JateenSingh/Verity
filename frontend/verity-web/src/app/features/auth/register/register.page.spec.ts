import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { RegisterPage } from './register.page';

describe('RegisterPage', () => {
  let fixture: ComponentFixture<RegisterPage>;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RegisterPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(RegisterPage);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('shows a validation message for an invalid field once touched', () => {
    const usernameControl = fixture.componentInstance['form'].controls.username;
    usernameControl.setValue('ab'); // below the 3-char minimum
    usernameControl.markAsTouched();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('not in the expected format');
  });

  it('shows a mismatch message when passwords differ', () => {
    const form = fixture.componentInstance['form'];
    form.controls.password.setValue('TestOnly_Register1!');
    form.controls.confirmPassword.setValue('Different1');
    form.controls.confirmPassword.markAsTouched();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Passwords do not match');
  });

  it('maps a 409 conflict response to a form-level alert', async () => {
    const form = fixture.componentInstance['form'];
    form.setValue({
      username: 'existinguser',
      email: 'existing@example.test',
      password: 'TestOnly_Register1!',
      confirmPassword: 'TestOnly_Register1!',
    });
    fixture.detectChanges();

    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/register`);
    req.flush(
      { title: 'Username is already taken.', status: 409, type: 'https://verity.local/problems/username-taken' },
      { status: 409, statusText: 'Conflict' },
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Username is already taken.');
  });
});
