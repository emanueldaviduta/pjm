import { Component, computed, inject, input, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AutoFocusModule } from 'primeng/autofocus';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MIN_PASSWORD_LENGTH } from '../_models/account';
import { AccountService, apiErrorMessage, safeReturnUrl } from '../_services/account.service';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

@Component({
  imports: [FormsModule, RouterLink, AutoFocusModule, ButtonModule, InputTextModule],
  selector: 'app-register',
  styleUrl: '../login/login.less',
  templateUrl: './register.html',
})
export class Register {
  private account = inject(AccountService);
  private router = inject(Router);

  /** Where to go after registering; bound from the `?returnUrl=` query param. */
  readonly returnUrl = input<string>();

  protected readonly minPasswordLength = MIN_PASSWORD_LENGTH;

  protected readonly firstName = signal('');
  protected readonly lastName = signal('');
  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly confirmPassword = signal('');
  protected readonly submitted = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly error = signal('');

  protected readonly firstNameMissing = computed(() => !this.firstName().trim());
  protected readonly lastNameMissing = computed(() => !this.lastName().trim());
  protected readonly emailError = computed(() => {
    const email = this.email().trim();
    if (!email) return 'Enter your email.';
    return EMAIL_PATTERN.test(email) ? '' : 'Enter a valid email, like name@example.com.';
  });
  protected readonly passwordTooShort = computed(() => this.password().length < MIN_PASSWORD_LENGTH);
  protected readonly passwordsDiffer = computed(() => this.password() !== this.confirmPassword());
  protected readonly invalid = computed(() =>
    this.firstNameMissing() || this.lastNameMissing() || !!this.emailError() ||
    this.passwordTooShort() || this.passwordsDiffer(),
  );

  onSubmit() {
    this.submitted.set(true);
    this.error.set('');
    if (this.invalid() || this.isSaving()) return;

    this.isSaving.set(true);
    this.account.register({
      firstName: this.firstName().trim(),
      lastName: this.lastName().trim(),
      email: this.email().trim(),
      password: this.password(),
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.router.navigateByUrl(safeReturnUrl(this.returnUrl()));
      },
      error: (error: HttpErrorResponse) => {
        console.error('Registration failed', error);
        this.isSaving.set(false);
        this.error.set(apiErrorMessage(error, 'Your account could not be created. Check the details and try again.'));
      },
    });
  }
}
