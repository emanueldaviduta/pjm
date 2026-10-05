import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { Account, ChangePassword, Register } from '../_models/account';
import { AuthLogin, Login } from '../_models/login';
import { ProjectService } from './project.service';
import { TaskService } from './task.service';

const TOKEN_KEY = 'pjmToken';

@Injectable({ providedIn: 'root' })
export class AccountService {
  private api = inject(HttpClient);
  private projectService = inject(ProjectService);
  private taskService = inject(TaskService);
  private baseUrl = environment.apiUrl + '/account';

  readonly isSignedIn = signal(!!sessionStorage.getItem(TOKEN_KEY));

  login(credentials: Login) {
    return this.api.post<AuthLogin>(this.baseUrl + '/login', credentials).pipe(
      tap(response => this.startSession(response.token)),
    );
  }

  register(account: Register) {
    return this.api.post<AuthLogin>(this.baseUrl + '/register', account).pipe(
      tap(response => this.startSession(response.token)),
    );
  }

  logout() {
    sessionStorage.removeItem(TOKEN_KEY);
    this.isSignedIn.set(false);
  }

  getMe() {
    return this.api.get<Account>(this.baseUrl + '/me');
  }

  updateMe(changes: Pick<Account, 'firstName' | 'lastName'>) {
    return this.api.put<Account>(this.baseUrl + '/me', changes);
  }

  changePassword(passwords: ChangePassword) {
    return this.api.put<void>(this.baseUrl + '/password', passwords);
  }

  private startSession(token: string) {
    sessionStorage.setItem(TOKEN_KEY, token);
    this.isSignedIn.set(true);
    // Data loaded while signed out came back 401; fetch it again with the new token.
    this.projectService.load(true);
    this.taskService.load(true);
  }
}

/** The API answers business-rule failures with a plain-text 400 body; use it when present. */
export function apiErrorMessage(error: HttpErrorResponse, fallback: string) {
  if (error.status === 0) return 'Could not reach the server. Try again in a moment.';
  return error.status === 400 && typeof error.error === 'string' ? error.error : fallback;
}

/** Only follow app-relative URLs so a `returnUrl` param cannot redirect off-site. */
export function safeReturnUrl(url: string | undefined, fallback = '/projects') {
  return url?.startsWith('/') && !url.startsWith('//') ? url : fallback;
}
