import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { Role } from '../_models/role';
import { LoadState, errorState } from './load-state';

/** Read-only catalog of roles (Owner, Member, ...). */
@Injectable({ providedIn: 'root' })
export class RoleService {
  private api = inject(HttpClient);
  private baseUrl = environment.apiUrl + '/roles';

  readonly roles = signal<Role[]>([]);
  readonly state = signal<LoadState>('idle');

  /** Loads the catalog once; later calls are no-ops unless `force` is set. */
  load(force = false) {
    if (!force && (this.state() === 'loading' || this.state() === 'ready')) return;
    this.state.set('loading');
    this.api.get<Role[]>(this.baseUrl).subscribe({
      next: roles => {
        this.roles.set(roles);
        this.state.set('ready');
      },
      error: error => {
        console.error(error);
        this.state.set(errorState(error));
      },
    });
  }
}
