import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { AutoFocusModule } from 'primeng/autofocus';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressBarModule } from 'primeng/progressbar';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { Project, projectKey } from '../_models/project';
import { TaskStatus } from '../_models/task-item';
import { ProjectService } from '../_services/project.service';
import { TaskService } from '../_services/task.service';

type SortBy = 'updated' | 'name';

@Component({
  imports: [
    DatePipe, FormsModule, RouterLink, AutoFocusModule, ButtonModule, DialogModule, InputTextModule,
    ProgressBarModule, SelectModule, TextareaModule, ToastModule,
  ],
  providers: [MessageService],
  selector: 'app-projects',
  styleUrls: ['./projects.less'],
  templateUrl: './projects.html',
})
export class Projects implements OnInit {
  protected projectService = inject(ProjectService);
  protected taskService = inject(TaskService);
  private messageService = inject(MessageService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  protected readonly projectKey = projectKey;
  protected readonly sortOptions = [
    { label: 'Last updated', value: 'updated' },
    { label: 'Name', value: 'name' },
  ];

  protected readonly query = signal('');
  protected readonly sortBy = signal<SortBy>('updated');

  protected readonly visibleProjects = computed(() => {
    const q = this.query().toLowerCase();
    const projects = this.projectService
      .projects()
      .filter(p => !q || p.name?.toLowerCase().includes(q) || p.description?.toLowerCase().includes(q));
    return this.sortBy() === 'name'
      ? [...projects].sort((a, b) => (a.name ?? '').localeCompare(b.name ?? ''))
      : projects;
  });

  protected readonly summary = computed(() => {
    const count = this.projectService.projects().length;
    const open = this.taskService.tasks().filter(t => t.status !== TaskStatus.Completed).length;
    return `${count} ${count === 1 ? 'project' : 'projects'} · ${open} open ${open === 1 ? 'task' : 'tasks'}`;
  });

  protected readonly dueSoon = this.taskService.dueWithin(7);

  // New project dialog
  protected readonly displayDialog = signal(false);
  protected readonly name = signal('');
  protected readonly description = signal('');
  protected readonly submitted = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly nameMissing = computed(() => !this.name().trim());
  protected readonly keyPreview = computed(() => projectKey(this.name() || 'Key'));

  ngOnInit(): void {
    this.projectService.load();
    this.taskService.load();

    this.route.queryParamMap.subscribe(params => {
      this.query.set(params.get('q') ?? '');
      if (params.has('new')) {
        this.openCreate();
        // Drop the flag so closing the dialog and reloading does not reopen it.
        this.router.navigate([], { queryParams: { new: null }, queryParamsHandling: 'merge', replaceUrl: true });
      }
    });
  }

  projectName(projectId: number) {
    return this.projectService.projects().find(p => p.id === projectId)?.name ?? '';
  }

  openCreate() {
    this.name.set('');
    this.description.set('');
    this.submitted.set(false);
    this.displayDialog.set(true);
  }

  saveProject() {
    this.submitted.set(true);
    if (this.nameMissing() || this.isSaving()) return;

    this.isSaving.set(true);
    this.projectService.create(new Project(this.name().trim(), this.description().trim())).subscribe({
      next: created => {
        this.isSaving.set(false);
        this.displayDialog.set(false);
        this.messageService.add({ severity: 'success', summary: 'Project created', detail: `${created.name} is ready. Add its first tasks.` });
        this.router.navigate(['/projects', created.id]);
      },
      error: error => {
        console.error(error);
        this.isSaving.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to save project' });
      },
    });
  }
}
