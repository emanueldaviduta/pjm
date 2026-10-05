import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { NewTaskItem, TaskItem, TaskStatus } from '../_models/task-item';
import { LoadState, errorState } from './load-state';

export interface ProjectTaskStats {
  done: number;
  total: number;
  percent: number;
}

@Injectable({ providedIn: 'root' })
export class TaskService {
  private api = inject(HttpClient);
  private baseUrl = environment.apiUrl + '/taskitem';

  readonly tasks = signal<TaskItem[]>([]);
  readonly state = signal<LoadState>('idle');

  /** Done / total counts per project id. */
  readonly statsByProject = computed(() => {
    const stats = new Map<number, ProjectTaskStats>();
    for (const task of this.tasks()) {
      const entry = stats.get(task.projectId) ?? { done: 0, total: 0, percent: 0 };
      entry.total++;
      if (task.status === TaskStatus.Completed) entry.done++;
      entry.percent = Math.round((entry.done / entry.total) * 100);
      stats.set(task.projectId, entry);
    }
    return stats;
  });

  /** Open tasks with a due date in the next `days` days (overdue included), soonest first. */
  dueWithin(days: number) {
    return computed(() => {
      const limit = Date.now() + days * 24 * 60 * 60 * 1000;
      return this.tasks()
        .filter(task => task.status !== TaskStatus.Completed && task.dueDate && Date.parse(task.dueDate) <= limit)
        .sort((a, b) => Date.parse(a.dueDate!) - Date.parse(b.dueDate!));
    });
  }

  forProject(projectId: number) {
    return computed(() => this.tasks().filter(task => task.projectId === projectId));
  }

  /** Loads the list once; later calls are no-ops unless `force` is set. */
  load(force = false) {
    if (!force && (this.state() === 'loading' || this.state() === 'ready')) return;
    this.state.set('loading');
    this.api.get<TaskItem[]>(this.baseUrl).subscribe({
      next: tasks => {
        this.tasks.set(tasks);
        this.state.set('ready');
      },
      error: error => {
        console.error(error);
        this.state.set(errorState(error));
      },
    });
  }

  create(task: NewTaskItem) {
    return this.api.post<TaskItem>(this.baseUrl, task).pipe(
      tap(created => this.tasks.update(tasks => [...tasks, created])),
    );
  }

  update(task: TaskItem) {
    return this.api.put<TaskItem>(`${this.baseUrl}/${task.id}`, task).pipe(
      tap(updated => this.tasks.update(tasks => tasks.map(t => (t.id === updated.id ? updated : t)))),
    );
  }
}
