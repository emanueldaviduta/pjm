import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { Account } from '../_models/account';
import { LoadState, errorState } from './load-state';

/** Registered users, used to pick people to add to a project. */
@Injectable({ providedIn: 'root' })
export class UserDirectoryService {
  private api = inject(HttpClient);
  private baseUrl = environment.apiUrl + '/account/users';

  readonly users = signal<Account[]>([]);
  readonly state = signal<LoadState>('idle');

  /** Loads the list once; later calls are no-ops unless `force` is set. */
  load(force = false) {
    if (!force && (this.state() === 'loading' || this.state() === 'ready')) return;
    this.state.set('loading');
    this.api.get<Account[]>(this.baseUrl).subscribe({
      next: users => {
        this.users.set(users);
        this.state.set('ready');
      },
      error: error => {
        console.error(error);
        this.state.set(errorState(error));
      },
    });
  }
}
