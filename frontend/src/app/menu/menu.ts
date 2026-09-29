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
    { label: 'Home', routerLink: '/home' },
    { label: 'Projects', routerLink: '/projects' }
  ];
}
