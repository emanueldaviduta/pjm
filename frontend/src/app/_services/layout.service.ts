import { Injectable, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter, map } from 'rxjs';

/** UI state of the app shell: the sidebar is a toggleable overlay on small screens. */
@Injectable({ providedIn: 'root' })
export class LayoutService {
  private router = inject(Router);

  readonly menuOpen = signal(false);

  /** True on routes declared with `data: { hideMenu: true }`, such as sign in and register. */
  readonly menuHidden = toSignal(
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      map(() => hidesMenu(this.router.routerState.snapshot.root)),
    ),
    { initialValue: false },
  );

  toggleMenu() {
    this.menuOpen.update(open => !open);
  }

  closeMenu() {
    this.menuOpen.set(false);
  }
}

function hidesMenu(route: ActivatedRouteSnapshot): boolean {
  while (route.firstChild) route = route.firstChild;
  return !!route.data['hideMenu'];
}
