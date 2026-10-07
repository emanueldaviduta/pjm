import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, tap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { ProjectMember } from '../_models/project-member';
import { LoadState, errorState } from './load-state';

/**
 * Members of projects. The server is the source of truth: every change reloads the
 * project's members, and so does a failed change, so the list never shows unsaved state.
 */
@Injectable({ providedIn: 'root' })
export class ProjectMemberService {
  private api = inject(HttpClient);
  private baseUrl = environment.apiUrl + '/projects';

  private readonly byProject = signal<Record<number, ProjectMember[]>>({});
  private readonly states = signal<Record<number, LoadState>>({});

  members(projectId: number) {
    return this.byProject()[projectId] ?? [];
  }

  state(projectId: number): LoadState {
    return this.states()[projectId] ?? 'idle';
  }

  /** Fetches the members of a project; always hits the server. */
  load(projectId: number) {
    this.setState(projectId, 'loading');
    this.api.get<ProjectMember[]>(this.url(projectId)).subscribe({
      next: members => {
        this.byProject.update(all => ({ ...all, [projectId]: members }));
        this.setState(projectId, 'ready');
      },
      error: error => {
        console.error(error);
        this.setState(projectId, errorState(error));
      },
    });
  }

  addRole(projectId: number, userId: number, roleId: number) {
    return this.changed(projectId, this.api.post<void>(this.url(projectId), { userId, roleId }));
  }

  removeRole(projectId: number, userId: number, roleId: number) {
    return this.changed(projectId, this.api.delete<void>(`${this.url(projectId)}/${userId}/roles/${roleId}`));
  }

  removeMember(projectId: number, userId: number) {
    return this.changed(projectId, this.api.delete<void>(`${this.url(projectId)}/${userId}`));
  }

  /** Reloads after success and after failure; the caller still sees the error. */
  private changed(projectId: number, request: Observable<void>) {
    return request.pipe(
      tap(() => this.load(projectId)),
      catchError(error => {
        this.load(projectId);
        return throwError(() => error);
      }),
    );
  }

  private url(projectId: number) {
    return `${this.baseUrl}/${projectId}/members`;
  }

  private setState(projectId: number, state: LoadState) {
    this.states.update(all => ({ ...all, [projectId]: state }));
  }
}
