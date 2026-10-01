import { Component, inject, signal, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { environment } from '../../environments/environment';
import { MessageService } from 'primeng/api';
import { Project } from '../_models/project';

@Component({
  imports: [ButtonModule, TableModule, DialogModule, ToastModule, FormsModule],
  providers: [MessageService],
  selector: 'app-projects',
  styleUrls: ['./projects.less'],
  templateUrl: './projects.html',
})
export class Projects implements OnInit {
  // projects!: Project[];
  project: Project = new Project('', '');
  api = inject(HttpClient);
  private messageService = inject(MessageService);

  isLoading = signal(false);
  projects = signal<Project[]>([]);

  ngOnInit(): void {
    this.isLoading.set(false);
    this.api.get<Project[]>(environment.apiUrl + '/projects').subscribe({
      next: (projects: Project[]) => {
        this.projects.set(projects);
      },
      error: error => {
        console.error(error);
        this.messageService.add({severity:'error', summary: 'Error', detail: 'Failed to load projects'});
      }
    });
  }

  displayDialog = false;

  addProject() {
    this.displayDialog = true;
  }

  saveProject() {
    this.isLoading.set(true);
    this.api.post(environment.apiUrl + '/projects', this.project).subscribe((objUpdated: Project) => {
      this.projects.update(projects => [...projects, objUpdated]);
      this.project = new Project('', '');
      this.isLoading.set(false);
      this.messageService.add({severity:'success', summary: 'Success', detail: 'Project saved successfully'});
    }, error => {
      console.error(error);
      this.isLoading.set(false);
      this.messageService.add({severity:'error', summary: 'Error', detail: 'Failed to save project'});
    });
    this.displayDialog = false;
  }
}

