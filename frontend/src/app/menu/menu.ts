import { Component, OnInit, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { projectKey } from '../_models/project';
import { LayoutService } from '../_services/layout.service';
import { ProjectService } from '../_services/project.service';

@Component({
  imports: [RouterLink, RouterLinkActive],
  selector: 'app-menu',
  styleUrls: ['./menu.less'],
  templateUrl: './menu.html',
})
export class Menu implements OnInit {
  protected layout = inject(LayoutService);
  private projectService = inject(ProjectService);

  protected readonly links = [
    { label: 'Home', routerLink: '/home', icon: 'pi pi-home' },
    { label: 'Projects', routerLink: '/projects', icon: 'pi pi-folder' },
    { label: 'Tasks', routerLink: '/tasks', icon: 'pi pi-check-square' },
    { label: 'Users', routerLink: '/users', icon: 'pi pi-users' },
  ];

  protected readonly recentProjects = computed(() => this.projectService.projects().slice(0, 5));
  protected readonly projectKey = projectKey;

  ngOnInit(): void {
    this.projectService.load();
  }
}
