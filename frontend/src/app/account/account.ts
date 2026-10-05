import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { Account as AccountModel, MIN_PASSWORD_LENGTH } from '../_models/account';
import { AccountService, apiErrorMessage } from '../_services/account.service';
import { LoadState, errorState } from '../_services/load-state';

@Component({
  imports: [DatePipe, FormsModule, RouterLink, ButtonModule, InputTextModule, ToastModule],
  providers: [MessageService],
  selector: 'app-account',
  styleUrl: './account.less',
  templateUrl: './account.html',
})
export class Account implements OnInit {
  private accountService = inject(AccountService);
  private messageService = inject(MessageService);
  private router = inject(Router);

  protected readonly minPasswordLength = MIN_PASSWORD_LENGTH;

  protected readonly state = signal<LoadState>('idle');
  protected readonly account = signal<AccountModel | undefined>(undefined);
  protected readonly initials = computed(() => {
    const account = this.account();
    return account ? `${account.firstName[0] ?? ''}${account.lastName[0] ?? ''}`.toUpperCase() : '';
  });

  // Profile form
  protected readonly firstName = signal('');
  protected readonly lastName = signal('');
  protected readonly profileSubmitted = signal(false);
  protected readonly isSavingProfile = signal(false);
  protected readonly firstNameMissing = computed(() => !this.firstName().trim());
  protected readonly lastNameMissing = computed(() => !this.lastName().trim());
  protected readonly profileChanged = computed(() =>
    this.firstName().trim() !== this.account()?.firstName || this.lastName().trim() !== this.account()?.lastName,
  );

  // Password form
  protected readonly currentPassword = signal('');
  protected readonly newPassword = signal('');
  protected readonly confirmPassword = signal('');
  protected readonly passwordSubmitted = signal(false);
  protected readonly isSavingPassword = signal(false);
  protected readonly passwordError = signal('');
  protected readonly currentPasswordMissing = computed(() => !this.currentPassword());
  protected readonly newPasswordTooShort = computed(() => this.newPassword().length < MIN_PASSWORD_LENGTH);
  protected readonly passwordsDiffer = computed(() => this.newPassword() !== this.confirmPassword());

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.state.set('loading');
    this.accountService.getMe().subscribe({
      next: account => {
        this.setAccount(account);
        this.state.set('ready');
      },
      error: error => {
        console.error(error);
        this.state.set(errorState(error));
      },
    });
  }

  saveProfile() {
    this.profileSubmitted.set(true);
    if (this.firstNameMissing() || this.lastNameMissing() || this.isSavingProfile()) return;

    this.isSavingProfile.set(true);
    this.accountService.updateMe({ firstName: this.firstName().trim(), lastName: this.lastName().trim() }).subscribe({
      next: account => {
        this.setAccount(account);
        this.isSavingProfile.set(false);
        this.profileSubmitted.set(false);
        this.messageService.add({ severity: 'success', summary: 'Profile saved', detail: 'Your name was updated.' });
      },
      error: (error: HttpErrorResponse) => {
        console.error(error);
        this.isSavingProfile.set(false);
        this.messageService.add({
          severity: 'error', summary: 'Error', detail: apiErrorMessage(error, 'Failed to save your profile.'),
        });
      },
    });
  }

  changePassword() {
    this.passwordSubmitted.set(true);
    this.passwordError.set('');
    if (this.currentPasswordMissing() || this.newPasswordTooShort() || this.passwordsDiffer()) return;
    if (this.isSavingPassword()) return;

    this.isSavingPassword.set(true);
    this.accountService.changePassword({
      currentPassword: this.currentPassword(),
      newPassword: this.newPassword(),
    }).subscribe({
      next: () => {
        this.isSavingPassword.set(false);
        this.passwordSubmitted.set(false);
        this.currentPassword.set('');
        this.newPassword.set('');
        this.confirmPassword.set('');
        this.messageService.add({
          severity: 'success', summary: 'Password changed', detail: 'Use your new password next time you sign in.',
        });
      },
      error: (error: HttpErrorResponse) => {
        console.error(error);
        this.isSavingPassword.set(false);
        this.passwordError.set(apiErrorMessage(error, 'Failed to change your password.'));
      },
    });
  }

  signOut() {
    this.accountService.logout();
    this.router.navigate(['/login']);
  }

  private setAccount(account: AccountModel) {
    this.account.set(account);
    this.firstName.set(account.firstName);
    this.lastName.set(account.lastName);
  }
}
