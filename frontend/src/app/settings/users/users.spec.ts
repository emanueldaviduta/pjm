import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MessageService } from 'primeng/api';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { SettingsUser, Users } from './users';

const ANA: SettingsUser = { id: 5, firstName: 'Ana', lastName: 'Pop', email: 'ana@x.io', isAdmin: true };
const BOB: SettingsUser = { id: 6, firstName: 'Bob', lastName: 'Ionescu', email: 'bob@x.io', isAdmin: false };

type Internals = { setAdmin(user: SettingsUser, isAdmin: boolean): void; users(): SettingsUser[] };

describe('Users settings', () => {
  let fixture: ComponentFixture<Users>;
  let http: HttpTestingController;
  let toast: ReturnType<typeof vi.fn>;
  let ui: Internals;

  const list = () => http.expectOne(r => r.method === 'GET' && r.url.endsWith('/settings/users'));

  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [Users],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Users);
    toast = vi.spyOn(fixture.debugElement.injector.get(MessageService), 'add') as never;
    ui = fixture.componentInstance as unknown as Internals;
    fixture.detectChanges();
    list().flush([ANA, BOB]);
    fixture.detectChanges();
  });

  it('lists users with their Admin switch', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Ana');
    expect(text).toContain('bob@x.io');
    expect(fixture.nativeElement.querySelectorAll('p-toggleswitch')).toHaveLength(2);
    expect(text).toContain('Add user');
  });

  it('grants Admin with role 3 and reloads', () => {
    ui.setAdmin(BOB, true);
    const post = http.expectOne(r => r.method === 'POST' && r.url.endsWith('/account/users/6/roles'));
    expect(post.request.body).toEqual({ roleId: 3 });
    post.flush({ id: 3, name: 'Admin' });
    list().flush([ANA, { ...BOB, isAdmin: true }]);
    expect(ui.users()[1].isAdmin).toBe(true);
    expect(toast).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('revokes Admin with a DELETE of role 3', () => {
    ui.setAdmin(ANA, false);
    http.expectOne(r => r.method === 'DELETE' && r.url.endsWith('/account/users/5/roles/3')).flush(null);
    list().flush([{ ...ANA, isAdmin: false }, BOB]);
    expect(ui.users()[0].isAdmin).toBe(false);
  });

  it('shows why the last admin cannot be revoked, and reloads the real state', () => {
    ui.setAdmin(ANA, false);
    http.expectOne(r => r.method === 'DELETE').flush('At least one admin must remain.', { status: 400, statusText: 'Bad Request' });
    list().flush([ANA, BOB]);
    expect(toast).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'error', detail: 'At least one admin must remain.' }),
    );
    expect(ui.users()[0].isAdmin).toBe(true);
  });
});
