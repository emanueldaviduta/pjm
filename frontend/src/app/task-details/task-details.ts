import { Component, computed, inject, input, linkedSignal, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AutoFocusModule } from 'primeng/autofocus';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import {
  BOARD_STATUSES, PRIORITY_LABELS, STATUS_LABELS, TaskItem, TaskPriority, TaskStatus, toDateOnly,
} from '../_models/task-item';
import { memberName } from '../_models/project-member';
import { ProjectMemberService } from '../_services/project-member.service';
import { TaskService } from '../_services/task.service';

/** Shows one task and lets the user edit its fields in place. */
@Component({
  imports: [
    DatePipe, FormsModule, AutoFocusModule, ButtonModule, DatePickerModule, InputTextModule, SelectModule,
    TextareaModule,
  ],
  selector: 'app-task-details',
  styleUrl: './task-details.less',
  templateUrl: './task-details.html',
})
export class TaskDetails {
  private taskService = inject(TaskService);
  private memberService = inject(ProjectMemberService);

  readonly task = input.required<TaskItem>();
  /** Project key used to label the task, e.g. "WEB" for WEB-12. */
  readonly projectKey = input.required<string>();
  readonly closed = output<void>();

  // Indexed by numeric enum values.
  protected readonly statusLabels: Record<number, string> = STATUS_LABELS;
  protected readonly priorityLabels: Record<number, string> = PRIORITY_LABELS;
  protected readonly statusOptions = BOARD_STATUSES.map(value => ({ label: STATUS_LABELS[value], value }));
  protected readonly priorityOptions = [TaskPriority.Low, TaskPriority.Medium, TaskPriority.High, TaskPriority.Critical]
    .map(value => ({ label: PRIORITY_LABELS[value], value }));

  /** Leaves edit mode whenever a different task is selected. */
  protected readonly editing = linkedSignal({ source: () => this.task().id, computation: () => false });

  protected readonly title = signal('');
  protected readonly description = signal('');
  protected readonly status = signal(TaskStatus.Created);
  protected readonly priority = signal(TaskPriority.Medium);
  protected readonly dueDate = signal<Date | null>(null);
  protected readonly assignedId = signal<number | null>(null);
  protected readonly submitted = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly error = signal('');
  /** Project members, plus the current assignee when they are not (or no longer) a member. */
  protected readonly assigneeOptions = computed(() => {
    const task = this.task();
    const options = this.memberService
      .members(task.projectId)
      .map(member => ({ label: memberName(member), value: member.userId }));
    if (task.assignedId !== null && !options.some(o => o.value === task.assignedId)) {
      options.push({ label: task.assigned || `User ${task.assignedId}`, value: task.assignedId });
    }
    return options;
  });
  protected readonly titleMissing = computed(() => !this.title().trim());

  startEdit() {
    const task = this.task();
    this.title.set(task.title);
    this.description.set(task.description ?? '');
    this.status.set(task.status);
    this.priority.set(task.priority);
    this.dueDate.set(task.dueDate ? new Date(task.dueDate) : null);
    this.assignedId.set(task.assignedId);
    if (this.memberService.state(task.projectId) === 'idle') this.memberService.load(task.projectId);
    this.submitted.set(false);
    this.error.set('');
    this.editing.set(true);
  }

  cancelEdit() {
    this.editing.set(false);
  }

  save() {
    this.submitted.set(true);
    this.error.set('');
    if (this.titleMissing() || this.isSaving()) return;

    this.isSaving.set(true);
    this.taskService.update({
      ...this.task(),
      title: this.title().trim(),
      description: this.description().trim(),
      status: this.status(),
      priority: this.priority(),
      dueDate: toDateOnly(this.dueDate()),
      assignedId: this.assignedId(),
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.editing.set(false);
      },
      error: error => {
        console.error(error);
        this.isSaving.set(false);
        this.error.set('The task could not be saved. Try again.');
      },
    });
  }
}
