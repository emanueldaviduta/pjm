import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { AutoFocusModule } from 'primeng/autofocus';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { projectKey } from '../_models/project';
import {
  BOARD_STATUSES, PRIORITY_LABELS, STATUS_LABELS, TaskItem, TaskPriority, TaskStatus, toDateOnly,
} from '../_models/task-item';
import { AccountService } from '../_services/account.service';
import { ProjectService } from '../_services/project.service';
import { TaskService } from '../_services/task.service';
import { TaskDetails } from '../task-details/task-details';

/** All tasks across projects. Becomes "My tasks" once tasks can be filtered by the signed-in user. */
@Component({
  imports: [
    DatePipe, FormsModule, RouterLink, AutoFocusModule, ButtonModule, DatePickerModule, DialogModule,
    InputTextModule, SelectModule, TableModule, TaskDetails, TextareaModule, ToastModule,
  ],
  providers: [MessageService],
  selector: 'app-tasks',
  styleUrl: './tasks.less',
  templateUrl: './tasks.html',
})
export class Tasks implements OnInit {
  protected account = inject(AccountService);
  protected taskService = inject(TaskService);
  private projectService = inject(ProjectService);
  private messageService = inject(MessageService);

  // Indexed by numeric enum values; p-table rows are untyped.
  protected readonly statusLabels: Record<number, string> = STATUS_LABELS;
  protected readonly priorityLabels: Record<number, string> = PRIORITY_LABELS;

  protected readonly statusOptions = BOARD_STATUSES.map(value => ({ label: STATUS_LABELS[value], value }));
  protected readonly priorityOptions = [TaskPriority.Low, TaskPriority.Medium, TaskPriority.High, TaskPriority.Critical]
    .map(value => ({ label: PRIORITY_LABELS[value], value }));
  protected readonly projectOptions = computed(() =>
    this.projectService.projects().map(p => ({ label: p.name, value: p.id! })),
  );

  // Project filter; 0 means all projects.
  protected readonly projectFilter = signal(0);
  protected readonly filterOptions = computed(() => [{ label: 'All', value: 0 }, ...this.projectOptions()]);
  protected readonly visibleTasks = computed(() => {
    const projectId = this.projectFilter();
    const tasks = this.taskService.tasks();
    return projectId ? tasks.filter(t => t.projectId === projectId) : tasks;
  });

  protected readonly selectedId = signal<number | null>(null);
  protected readonly selectedTask = computed(() => this.taskService.tasks().find(t => t.id === this.selectedId()));

  // New task dialog
  protected readonly displayDialog = signal(false);
  protected readonly title = signal('');
  protected readonly projectId = signal<number | null>(null);
  protected readonly status = signal(TaskStatus.Created);
  protected readonly priority = signal(TaskPriority.Medium);
  protected readonly dueDate = signal<Date | null>(null);
  protected readonly description = signal('');
  protected readonly submitted = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly titleMissing = computed(() => !this.title().trim());
  protected readonly projectMissing = computed(() => !this.projectId());

  ngOnInit(): void {
    this.projectService.load();
    this.taskService.load();
  }

  select(task: TaskItem) {
    this.selectedId.set(task.id);
  }

  setProjectFilter(projectId: number) {
    this.projectFilter.set(projectId);
    // Close the details panel if its task is no longer in the list.
    if (projectId && this.selectedTask()?.projectId !== projectId) this.selectedId.set(null);
  }

  openCreate() {
    this.title.set('');
    // Start in the filtered project, or the only project when there is just one.
    const projects = this.projectOptions();
    this.projectId.set(this.projectFilter() || (projects.length === 1 ? projects[0].value : null));
    this.status.set(TaskStatus.Created);
    this.priority.set(TaskPriority.Medium);
    this.dueDate.set(null);
    this.description.set('');
    this.submitted.set(false);
    this.displayDialog.set(true);
  }

  saveTask() {
    this.submitted.set(true);
    if (this.titleMissing() || this.projectMissing() || this.isSaving()) return;

    this.isSaving.set(true);
    this.taskService.create({
      projectId: this.projectId()!,
      assignedId: 0,
      title: this.title().trim(),
      description: this.description().trim(),
      status: this.status(),
      priority: this.priority(),
      dueDate: toDateOnly(this.dueDate()),
    }).subscribe({
      next: created => {
        this.isSaving.set(false);
        this.displayDialog.set(false);
        // Same rule as setProjectFilter: only show details for a task the filtered list includes.
        const filter = this.projectFilter();
        if (!filter || created.projectId === filter) this.selectedId.set(created.id);
        this.messageService.add({
          severity: 'success', summary: 'Task created',
          detail: `${this.taskKey(created.projectId, created.id)} was added to ${this.projectName(created.projectId)}.`,
        });
      },
      error: error => {
        console.error(error);
        this.isSaving.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to create task' });
      },
    });
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
