import { Component, computed, inject, input, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AutoFocusModule } from 'primeng/autofocus';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { AccountService, safeReturnUrl } from '../_services/account.service';

@Component({
  imports: [FormsModule, RouterLink, AutoFocusModule, ButtonModule, InputTextModule],
  selector: 'app-login',
  styleUrl: './login.less',
  templateUrl: './login.html',
})
export class Login {
  private account = inject(AccountService);
  private router = inject(Router);

  /** Where to go after signing in; bound from the `?returnUrl=` query param. */
  readonly returnUrl = input<string>();

  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly submitted = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly error = signal('');

  protected readonly emailMissing = computed(() => !this.email().trim());
  protected readonly passwordMissing = computed(() => !this.password());

  onSubmit() {
    this.submitted.set(true);
    this.error.set('');
    if (this.emailMissing() || this.passwordMissing() || this.isSaving()) return;

    this.isSaving.set(true);
    this.account.login({ email: this.email().trim(), password: this.password() }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.router.navigateByUrl(safeReturnUrl(this.returnUrl()));
      },
      error: (error: HttpErrorResponse) => {
        console.error('Login failed', error);
        this.isSaving.set(false);
        this.error.set(
          error.status === 400 || error.status === 401
            ? 'Email or password is incorrect.'
            : 'Could not reach the server. Try again in a moment.',
        );
      },
    });
  }
}
