import { Routes } from '@angular/router';
import { Home } from './home/home';
import { Projects } from './projects/projects';
import { Users } from './users/users';

export const routes: Routes = [
    { path: '', redirectTo: '/home', pathMatch: 'full' },
    { path: 'home', component: Home },
    { path: 'projects', component: Projects },
    { path: 'users', component: Users },
    { path: '**', redirectTo: '/home' }
];
