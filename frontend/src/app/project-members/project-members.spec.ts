import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Confirmation, ConfirmationService, MessageService } from 'primeng/api';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ProjectMembers } from './project-members';

const ROLES = [{ id: 1, name: 'Owner' }, { id: 2, name: 'Member' }];
const USERS = [
  { id: 5, firstName: 'Ana', lastName: 'Pop', email: 'ana@x.io', createdAt: '' },
  { id: 6, firstName: 'Bob', lastName: 'Ionescu', email: 'bob@x.io', createdAt: '' },
];
const ana = (roles = [ROLES[1]]) => ({ userId: 5, firstName: 'Ana', lastName: 'Pop', email: 'ana@x.io', roles });

// The component keeps its template state protected; tests drive it the way the template does.
type Internals = {
  members(): unknown[];
  canConfirm(): boolean;
  roleId(): number | null;
  selectedUser: { set(v: unknown): void };
  openAdd(): void;
  search(q: string): void;
  suggestions(): { id: number; isMember: boolean }[];
  confirmAdd(): void;
  addRole(member: unknown, roleId: number): void;
  removeRole(member: unknown, role: unknown): void;
  removeMember(member: unknown): void;
};

describe('ProjectMembers', () => {
  let fixture: ComponentFixture<ProjectMembers>;
  let http: HttpTestingController;
  let messages: MessageService;
  let ui: Internals;
  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  const membersReq = () => http.expectOne(r => r.method === 'GET' && r.url.endsWith('/projects/1/members'));
  const rolesReq = () => http.expectOne(r => r.url.endsWith('/roles'));

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectMembers],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), MessageService],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    messages = TestBed.inject(MessageService);
    vi.spyOn(messages, 'add');
    fixture = TestBed.createComponent(ProjectMembers);
    fixture.componentRef.setInput('projectId', 1);
    ui = fixture.componentInstance as unknown as Internals;
    fixture.detectChanges();
  });

  const load = (members: unknown[]) => {
    membersReq().flush(members);
    rolesReq().flush(ROLES);
    fixture.detectChanges();
  };

  describe('list states', () => {
    it('shows a loading state first', () => {
      expect(text()).toContain('Loading members');
      membersReq().flush([]);
      rolesReq().flush(ROLES);
    });

    it('shows each member once with a badge per role', () => {
      load([ana([ROLES[0], ROLES[1]])]);
      expect(text()).toContain('Ana Pop');
      expect(text()).toContain('ana@x.io');
      expect(text()).toContain('Owner');
      expect(text()).toContain('Member');
      expect(fixture.nativeElement.querySelectorAll('.member')).toHaveLength(1);
      expect(fixture.nativeElement.querySelectorAll('.role-chip')).toHaveLength(2);
    });

    it('shows an empty state with no members', () => {
      load([]);
      expect(text()).toContain('No members yet');
      expect(text()).toContain('Add member');
    });

    it('shows an error with retry and reloads on retry', () => {
      membersReq().flush('', { status: 500, statusText: 'Server Error' });
      rolesReq().flush(ROLES);
      fixture.detectChanges();
      expect(text()).toContain('Could not load the members');
      const retry = [...fixture.nativeElement.querySelectorAll('button')].find(b => b.textContent.includes('Try again'));
      retry.click();
      membersReq().flush([]);
      fixture.detectChanges();
      expect(text()).toContain('No members yet');
    });

    it('asks to sign in again on 401', () => {
      membersReq().flush('', { status: 401, statusText: 'Unauthorized' });
      rolesReq().flush(ROLES);
      fixture.detectChanges();
      expect(text()).toContain('Sign in again');
      expect(fixture.nativeElement.querySelector('a[href*="login"]')).toBeTruthy();
    });
  });

  describe('add member', () => {
    const openDialog = (members: unknown[] = []) => {
      load(members);
      ui.openAdd();
      http.expectOne(r => r.url.endsWith('/account/users')).flush(USERS);
    };

    it('defaults the role to Member and disables confirm until a user is chosen', () => {
      openDialog();
      expect(ui.roleId()).toBe(2);
      expect(ui.canConfirm()).toBe(false);
      ui.selectedUser.set({ id: 6, label: 'Bob Ionescu (bob@x.io)', isMember: false });
      expect(ui.canConfirm()).toBe(true);
    });

    it('filters users by name or email, and returns nothing for no match', () => {
      openDialog();
      ui.search('ion');
      expect(ui.suggestions().map(s => s.id)).toEqual([6]);
      ui.search('ana@');
      expect(ui.suggestions().map(s => s.id)).toEqual([5]);
      ui.search('zzz');
      expect(ui.suggestions()).toEqual([]);
    });

    it('marks existing members and blocks a role they already hold', () => {
      openDialog([ana()]);
      ui.search('ana');
      expect(ui.suggestions()[0].isMember).toBe(true);
      ui.selectedUser.set(ui.suggestions()[0]);
      expect(ui.canConfirm()).toBe(false); // default Member is already held
    });

    it('adds the member, closes, reloads and confirms with a toast', () => {
      openDialog();
      ui.selectedUser.set({ id: 6, label: 'Bob Ionescu (bob@x.io)', isMember: false });
      ui.confirmAdd();
      const post = http.expectOne(r => r.method === 'POST' && r.url.endsWith('/projects/1/members'));
      expect(post.request.body).toEqual({ userId: 6, roleId: 2 });
      post.flush(null, { status: 201, statusText: 'Created' });
      membersReq().flush([{ ...ana(), userId: 6, firstName: 'Bob' }]);
      expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    });

    it('explains a 409 and refreshes the list', () => {
      openDialog();
      ui.selectedUser.set({ id: 6, label: 'Bob Ionescu (bob@x.io)', isMember: false });
      ui.confirmAdd();
      http.expectOne(r => r.method === 'POST').flush('', { status: 409, statusText: 'Conflict' });
      membersReq().flush([]);
      expect(messages.add).toHaveBeenCalledWith(
        expect.objectContaining({ severity: 'warn', detail: expect.stringContaining('already has this role') }),
      );
    });

    it('reports a generic failure', () => {
      openDialog();
      ui.selectedUser.set({ id: 6, label: 'Bob', isMember: false });
      ui.confirmAdd();
      http.expectOne(r => r.method === 'POST').flush('', { status: 500, statusText: 'Server Error' });
      membersReq().flush([]);
      expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
    });
  });

  describe('edit and remove', () => {
    it('adds a second role to a member', () => {
      load([ana()]);
      ui.addRole(ana(), 1);
      const post = http.expectOne(r => r.method === 'POST');
      expect(post.request.body).toEqual({ userId: 5, roleId: 1 });
      post.flush(null, { status: 201, statusText: 'Created' });
      membersReq().flush([ana([ROLES[0], ROLES[1]])]);
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelectorAll('.role-chip')).toHaveLength(2);
    });

    it('removes one of two roles without asking', () => {
      load([ana([ROLES[0], ROLES[1]])]);
      const confirm = vi.spyOn(fixture.debugElement.injector.get(ConfirmationService), 'confirm');
      ui.removeRole(ana([ROLES[0], ROLES[1]]), ROLES[0]);
      http.expectOne(r => r.method === 'DELETE' && r.url.endsWith('/members/5/roles/1')).flush(null);
      membersReq().flush([ana()]);
      fixture.detectChanges();
      expect(confirm).not.toHaveBeenCalled();
      expect(fixture.nativeElement.querySelectorAll('.role-chip')).toHaveLength(1);
    });

    it('asks first when removing the last role, and does nothing on cancel', () => {
      load([ana()]);
      const confirm = vi.spyOn(fixture.debugElement.injector.get(ConfirmationService), 'confirm').mockImplementation(() => undefined as never);
      ui.removeRole(ana(), ROLES[1]);
      expect(confirm).toHaveBeenCalledOnce();
      http.expectNone(r => r.method === 'DELETE');
      expect(ui.members()).toHaveLength(1);
    });

    it('removes the member once confirmed', () => {
      load([ana()]);
      vi.spyOn(fixture.debugElement.injector.get(ConfirmationService), 'confirm').mockImplementation(
        ((c: Confirmation) => c.accept?.()) as never,
      );
      ui.removeMember(ana());
      http.expectOne(r => r.method === 'DELETE' && r.url.endsWith('/members/5')).flush(null);
      membersReq().flush([]);
      fixture.detectChanges();
      expect(text()).toContain('No members yet');
      expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    });

    it('shows the server state again when a removal fails', () => {
      load([ana()]);
      vi.spyOn(fixture.debugElement.injector.get(ConfirmationService), 'confirm').mockImplementation(
        ((c: Confirmation) => c.accept?.()) as never,
      );
      ui.removeMember(ana());
      http.expectOne(r => r.method === 'DELETE').flush('', { status: 404, statusText: 'Not Found' });
      membersReq().flush([ana()]);
      fixture.detectChanges();
      expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
      expect(text()).toContain('Ana Pop');
    });

    it('tells the person to sign in again when an action returns 401', () => {
      load([ana()]);
      ui.addRole(ana(), 1);
      http.expectOne(r => r.method === 'POST').flush('', { status: 401, statusText: 'Unauthorized' });
      membersReq().flush('', { status: 401, statusText: 'Unauthorized' });
      fixture.detectChanges();
      expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: expect.stringContaining('session expired') }));
      expect(text()).toContain('Sign in again');
    });
  });
});
