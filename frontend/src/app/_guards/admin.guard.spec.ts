import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { Observable, firstValueFrom, isObservable, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { adminGuard } from './admin.guard';

describe('adminGuard', () => {
  let http: HttpTestingController;
  let router: Router;

  const run = (url = '/settings/mail') =>
    TestBed.runInInjectionContext(() =>
      adminGuard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    ) as boolean | UrlTree | Observable<boolean | UrlTree>;

  const resolve = (result: ReturnType<typeof run>) =>
    firstValueFrom(isObservable(result) ? result : of(result));

  const meReq = () => http.expectOne(r => r.url.endsWith('/account/me'));

  function setup(signedIn: boolean) {
    if (signedIn) sessionStorage.setItem('pjmToken', 'token');
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  }

  beforeEach(() => sessionStorage.clear());
  afterEach(() => sessionStorage.clear());

  it('lets an admin in', async () => {
    setup(true);
    const result = resolve(run());
    meReq().flush({ id: 1, firstName: 'A', lastName: 'B', email: 'a@x.io', createdAt: '', isAdmin: true });
    expect(await result).toBe(true);
  });

  it('sends a signed-in non-admin home', async () => {
    setup(true);
    const result = resolve(run());
    meReq().flush({ id: 2, firstName: 'C', lastName: 'D', email: 'c@x.io', createdAt: '', isAdmin: false });
    expect(router.serializeUrl((await result) as UrlTree)).toBe('/home');
  });

  it('sends a signed-out person to sign in, and back to the page afterwards', async () => {
    setup(false);
    const result = await resolve(run('/settings/mail'));
    expect(router.serializeUrl(result as UrlTree)).toBe('/login?returnUrl=%2Fsettings%2Fmail');
    http.expectNone(r => r.url.endsWith('/account/me'));
  });
});
