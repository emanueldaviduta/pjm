import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { environment } from '../../../environments/environment';
import { User } from '../../_models/user';
import { AccountService, apiErrorMessage } from '../../_services/account.service';

/** A user as listed for admins, with whether they hold the global Admin role. */
export interface SettingsUser {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  isAdmin: boolean;
}

/** Id of the global Admin role in the role catalog. */
export const ADMIN_ROLE_ID = 3;

/** Settings → Users: everyone who can sign in, with an Admin switch per person. */
@Component({
  imports: [ButtonModule, DialogModule, FormsModule, InputTextModule, TableModule, ToastModule, ToggleSwitchModule],
  providers: [MessageService],
  selector: 'app-users',
  styleUrl: './users.less',
  templateUrl: './users.html',
})
export class Users implements OnInit {
  private api = inject(HttpClient);
  private account = inject(AccountService);
  private messageService = inject(MessageService);
  private baseUrl = environment.apiUrl;

  protected readonly users = signal<SettingsUser[]>([]);
  protected readonly loading = signal(true);
  /** Ids of users whose Admin role is being changed. */
  protected readonly saving = signal<ReadonlySet<number>>(new Set());

  displayDialog = false;
  user: User = new User();

  ngOnInit(): void {
    this.load();
  }

  protected load() {
    this.api.get<SettingsUser[]>(this.baseUrl + '/settings/users').subscribe({
      next: users => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast('error', 'Failed to load users');
      },
    });
  }

  /** Grants or revokes Admin; on failure shows the server's reason and reloads the real state. */
  protected setAdmin(user: SettingsUser, isAdmin: boolean) {
    this.saving.update(ids => new Set(ids).add(user.id));
    const url = `${this.baseUrl}/account/users/${user.id}/roles`;
    const request = isAdmin
      ? this.api.post(url, { roleId: ADMIN_ROLE_ID })
      : this.api.delete(`${url}/${ADMIN_ROLE_ID}`);
    request.subscribe({
      next: () => {
        this.done(user.id);
        this.toast('success', `${user.firstName} ${user.lastName} ${isAdmin ? 'is now an admin' : 'is no longer an admin'}`);
        // Revoking your own Admin role hides Settings on the next load.
        if (user.id === this.account.me()?.id) {
          this.account.refreshMe();
        }
        this.load();
      },
      error: (error: HttpErrorResponse) => {
        this.done(user.id);
        this.toast('error', apiErrorMessage(error, 'Failed to change the Admin role'));
        this.load();
      },
    });
  }

  protected isSaving(id: number) {
    return this.saving().has(id);
  }

  addUser() {
    this.user = new User();
    this.displayDialog = true;
  }

  saveUser() {
    if (this.user.firstName && this.user.lastName && this.user.email) {
      this.api.post(this.baseUrl + '/account/register', this.user).subscribe({
        next: () => {
          this.displayDialog = false;
          this.toast('success', 'User saved successfully');
          this.load();
        },
        error: (error: HttpErrorResponse) => this.toast('error', apiErrorMessage(error, 'Failed to save user')),
      });
    }
  }

  private done(id: number) {
    this.saving.update(ids => {
      const next = new Set(ids);
      next.delete(id);
      return next;
    });
  }

  private toast(severity: 'success' | 'error', detail: string) {
    this.messageService.add({ severity, summary: severity === 'success' ? 'Done' : 'Error', detail });
  }
}
