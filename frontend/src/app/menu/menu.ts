import { Component, OnInit, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { projectKey } from '../_models/project';
import { AccountService } from '../_services/account.service';
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
  protected account = inject(AccountService);

  protected readonly links = [
    { label: 'Home', routerLink: '/home', icon: 'pi pi-home' },
    { label: 'Projects', routerLink: '/projects', icon: 'pi pi-folder' },
    { label: 'Tasks', routerLink: '/tasks', icon: 'pi pi-check-square' },
  ];

  /** Shown only to admins; the routes are guarded and the API checks the role as well. */
  protected readonly settingsLinks = [
    { label: 'Users', routerLink: '/settings/users', icon: 'pi pi-users' },
    { label: 'Email → Project', routerLink: '/settings/mail', icon: 'pi pi-envelope' },
  ];

  protected readonly recentProjects = computed(() => this.projectService.projects().slice(0, 5));
  protected readonly projectKey = projectKey;

  ngOnInit(): void {
    this.projectService.load();
  }
}
