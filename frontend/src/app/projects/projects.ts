import { Component, inject, signal, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { environment } from '../../environments/environment.development';

@Component({
  imports: [ButtonModule, TableModule, DialogModule, FormsModule],
  selector: 'app-projects',
  styleUrls: ['./projects.less'],
  templateUrl: './projects.html',
})
export class Projects implements OnInit {
  projects!: Project[];
  project: Project = new Project('', '');
  api = inject(HttpClient);

  isLoading = signal(false);

  ngOnInit(): void {
    this.isLoading.set(false);
    this.api.get<Project[]>(environment.apiUrl + '/projects').subscribe((projects: Project[]) => {
      console.log(projects);
      this.projects = projects;
    });
  }

  displayDialog = false;

  addProject() {
    this.displayDialog = true;
  }

  saveProject() {
    this.isLoading.set(true);
    this.api.post(environment.apiUrl + '/projects', this.project).subscribe((objUpdated: Project) => {
      this.projects.push(objUpdated);
      this.project = new Project('', '');
      this.isLoading.set(false);
    });
    this.displayDialog = false;
  }
}

class Project {
  id?: number;
  name?: string;
  description?: string;
  constructor(name: string, description: string) {
    this.name = name;
    this.description = description;
  }
}