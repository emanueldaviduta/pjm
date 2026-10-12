import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AccountService } from '../_services/account.service';

/**
 * Settings pages are for admins only. Signed-out people go to sign in (and come back afterwards);
 * signed-in people without Admin go home. The server enforces the same rule on every request.
 */
export const adminGuard: CanActivateFn = (_route, state) => {
  const account = inject(AccountService);
  const router = inject(Router);

  if (!account.isSignedIn()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }
  return account.loadMe().pipe(map(me => (me?.isAdmin ? true : router.createUrlTree(['/home']))));
};
