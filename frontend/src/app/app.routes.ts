import { Routes } from '@angular/router';
import { Home } from './home/home';
import { Projects } from './projects/projects';
import { ProjectDetail } from './project-detail/project-detail';
import { Tasks } from './tasks/tasks';
import { Users } from './settings/users/users';
import { MailSettingsPage } from './settings/mail/mail-settings';
import { adminGuard } from './_guards/admin.guard';
import { Login } from './login/login';
import { Register } from './register/register';
import { Account } from './account/account';

export const routes: Routes = [
    { path: '', redirectTo: '/home', pathMatch: 'full' },
    { path: 'home', component: Home, title: 'Home · PJM' },
    { path: 'login', component: Login, title: 'Sign in · PJM', data: { hideMenu: true } },
    { path: 'register', component: Register, title: 'Create account · PJM', data: { hideMenu: true } },
    { path: 'account', component: Account, title: 'Account · PJM' },
    { path: 'projects', component: Projects, title: 'Projects · PJM' },
    { path: 'projects/:id', component: ProjectDetail, title: 'Project · PJM' },
    { path: 'tasks', component: Tasks, title: 'Tasks · PJM' },
    { path: 'users', redirectTo: '/settings/users', pathMatch: 'full' },
    {
        path: 'settings',
        canActivate: [adminGuard],
        children: [
            { path: '', redirectTo: 'users', pathMatch: 'full' },
            { path: 'users', component: Users, title: 'Users · Settings · PJM' },
            { path: 'mail', component: MailSettingsPage, title: 'Email → Project · Settings · PJM' },
        ],
    },
    { path: '**', redirectTo: '/home' }
];
