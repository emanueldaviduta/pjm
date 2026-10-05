import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { Project } from '../_models/project';
import { LoadState, errorState } from './load-state';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private api = inject(HttpClient);
  private baseUrl = environment.apiUrl + '/projects';

  private readonly all = signal<Project[]>([]);
  readonly state = signal<LoadState>('idle');

  /** Projects that are not soft-deleted, most recently updated first. */
  readonly projects = computed(() =>
    this.all()
      .filter(project => !project.isDeleted)
      .sort((a, b) => (b.updatedAt ?? b.createdAt ?? '').localeCompare(a.updatedAt ?? a.createdAt ?? '')),
  );

  /** Loads the list once; later calls are no-ops unless `force` is set. */
  load(force = false) {
    if (!force && (this.state() === 'loading' || this.state() === 'ready')) return;
    this.state.set('loading');
    this.api.get<Project[]>(this.baseUrl).subscribe({
      next: projects => {
        this.all.set(projects);
        this.state.set('ready');
      },
      error: error => {
        console.error(error);
        this.state.set(errorState(error));
      },
    });
  }

  getById(id: number) {
    return computed(() => this.all().find(project => project.id === id));
  }

  create(project: Project) {
    return this.api.post<Project>(this.baseUrl, project).pipe(
      tap(created => this.all.update(projects => [...projects, created])),
    );
  }
}
