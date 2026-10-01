import { Component } from '@angular/core';
import { MenuModule } from 'primeng/menu';

@Component({
  imports: [MenuModule],
  selector: 'app-menu',
  styleUrls: ['./menu.less'],
  templateUrl: './menu.html',
})
export class Menu {
  items = [
    { label: 'Home', routerLink: '/home', icon: 'pi pi-home' },
    { label: 'Projects', routerLink: '/projects', icon: 'pi pi-briefcase' },
    { label: 'Users', routerLink: '/users', icon: 'pi pi-users' }
  ];

  isDark(){
    return document.documentElement.classList.contains('app-dark');
  }

  changeDarkMode() {
    document.documentElement.classList.toggle('app-dark');
  }

}
