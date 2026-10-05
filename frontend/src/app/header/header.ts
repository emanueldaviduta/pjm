import { Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { MenuModule } from 'primeng/menu';
import { AccountService } from '../_services/account.service';
import { LayoutService } from '../_services/layout.service';
import { ThemeService } from '../_services/theme.service';

@Component({
  imports: [RouterLink, FormsModule, ButtonModule, IconFieldModule, InputIconModule, InputTextModule, MenuModule],
  selector: 'app-header',
  styleUrl: './header.less',
  templateUrl: './header.html',
})
export class Header {
  protected theme = inject(ThemeService);
  protected layout = inject(LayoutService);
  protected account = inject(AccountService);
  private router = inject(Router);

  protected query = '';

  protected readonly accountItems = computed<MenuItem[]>(() =>
    this.account.isSignedIn()
      ? [
          { label: 'Account', icon: 'pi pi-user', routerLink: '/account' },
          { label: 'Users', icon: 'pi pi-users', routerLink: '/users' },
          { separator: true },
          { label: 'Sign out', icon: 'pi pi-sign-out', command: () => this.signOut() },
        ]
      : [
          { label: 'Sign in', icon: 'pi pi-sign-in', routerLink: '/login' },
          { label: 'Create account', icon: 'pi pi-user-plus', routerLink: '/register' },
        ],
  );

  search() {
    const q = this.query.trim();
    this.router.navigate(['/projects'], { queryParams: q ? { q } : {} });
  }

  signOut() {
    this.account.logout();
    this.router.navigate(['/login']);
  }
}
