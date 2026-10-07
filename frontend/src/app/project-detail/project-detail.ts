import { Component, OnInit, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { AutoFocusModule } from 'primeng/autofocus';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressBarModule } from 'primeng/progressbar';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { projectKey } from '../_models/project';
import {
  BOARD_STATUSES, PRIORITY_LABELS, STATUS_LABELS, TaskItem, TaskPriority, TaskStatus,
} from '../_models/task-item';
import { ProjectService } from '../_services/project.service';
import { TaskService } from '../_services/task.service';
import { TaskDetails } from '../task-details/task-details';

type View = 'board' | 'list';

@Component({
  imports: [
    DatePipe, FormsModule, RouterLink, AutoFocusModule, ButtonModule, InputTextModule, ProgressBarModule,
    SelectButtonModule, TableModule, TaskDetails, ToastModule,
  ],
  providers: [MessageService],
  selector: 'app-project-detail',
  styleUrl: './project-detail.less',
  templateUrl: './project-detail.html',
})
export class ProjectDetail implements OnInit {
  /** Route parameter `:id`, bound through withComponentInputBinding(). */
  readonly id = input.required({ transform: numberAttribute });

  protected projectService = inject(ProjectService);
  protected taskService = inject(TaskService);
  private messageService = inject(MessageService);

  // Indexed by numeric enum values; p-table rows are untyped.
  protected readonly statusLabels: Record<number, string> = STATUS_LABELS;
  protected readonly priorityLabels: Record<number, string> = PRIORITY_LABELS;
  protected readonly viewOptions = [
    { label: 'Board', value: 'board', icon: 'pi pi-th-large' },
    { label: 'List', value: 'list', icon: 'pi pi-list' },
  ];

  protected readonly project = computed(() => this.projectService.projects().find(p => p.id === this.id()));
  protected readonly key = computed(() => projectKey(this.project()?.name));
  protected readonly tasks = computed(() => this.taskService.tasks().filter(t => t.projectId === this.id()));

  protected readonly columns = computed(() =>
    BOARD_STATUSES.map(status => ({
      status,
      label: STATUS_LABELS[status],
      tasks: this.tasks().filter(t => t.status === status),
    })),
  );

  protected readonly progress = computed(() => {
    const total = this.tasks().length;
    const done = this.tasks().filter(t => t.status === TaskStatus.Completed).length;
    return { done, total, percent: total ? Math.round((done / total) * 100) : 0 };
  });

  protected readonly view = signal<View>('board');
  protected readonly selectedId = signal<number | null>(null);
  protected readonly selectedTask = computed(() => this.tasks().find(t => t.id === this.selectedId()));

  // Quick add: one column at a time shows an inline title input.
  protected readonly addingTo = signal<TaskStatus | null>(null);
  protected readonly draft = signal('');
  protected readonly isSaving = signal(false);

  ngOnInit(): void {
    this.projectService.load();
    this.taskService.load();
  }

  select(task: TaskItem) {
    this.selectedId.set(task.id);
  }

  startAdd(status: TaskStatus) {
    this.draft.set('');
    this.addingTo.set(status);
  }

  cancelAdd() {
    this.addingTo.set(null);
    this.draft.set('');
  }

  /** Saves the draft and keeps the input open so the next task can be typed right away. */
  addTask(status: TaskStatus) {
    const title = this.draft().trim();
    if (!title || this.isSaving()) return;

    this.isSaving.set(true);
    this.taskService
      .create({
        projectId: this.id(),
        assignedId: null,
        title,
        description: '',
        status,
        priority: TaskPriority.Medium,
      })
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.draft.set('');
        },
        error: error => {
          console.error(error);
          this.isSaving.set(false);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to add task' });
        },
      });
  }
}
