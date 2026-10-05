import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { projectKey } from '../_models/project';
import { PRIORITY_LABELS, STATUS_LABELS, TaskItem } from '../_models/task-item';
import { ProjectService } from '../_services/project.service';
import { TaskService } from '../_services/task.service';
import { TaskDetails } from '../task-details/task-details';

/** All tasks across projects. Becomes "My tasks" once tasks can be filtered by the signed-in user. */
@Component({
  imports: [DatePipe, RouterLink, TableModule, ButtonModule, TaskDetails],
  selector: 'app-tasks',
  styleUrl: './tasks.less',
  templateUrl: './tasks.html',
})
export class Tasks implements OnInit {
  protected taskService = inject(TaskService);
  private projectService = inject(ProjectService);

  // Indexed by numeric enum values; p-table rows are untyped.
  protected readonly statusLabels: Record<number, string> = STATUS_LABELS;
  protected readonly priorityLabels: Record<number, string> = PRIORITY_LABELS;

  protected readonly selectedId = signal<number | null>(null);
  protected readonly selectedTask = computed(() => this.taskService.tasks().find(t => t.id === this.selectedId()));

  ngOnInit(): void {
    this.projectService.load();
    this.taskService.load();
  }

  select(task: TaskItem) {
    this.selectedId.set(task.id);
  }

  projectKeyOf(projectId: number) {
    return projectKey(this.projectName(projectId));
  }

  projectName(projectId: number) {
    return this.projectService.projects().find(p => p.id === projectId)?.name ?? '';
  }

  taskKey(projectId: number, taskId: number) {
    return `${this.projectKeyOf(projectId)}-${taskId}`;
  }
}
