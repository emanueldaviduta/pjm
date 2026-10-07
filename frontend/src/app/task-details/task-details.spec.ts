import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { TaskItem, TaskPriority, TaskStatus } from '../_models/task-item';
import { TaskDetails } from './task-details';

const MEMBERS = [
  { userId: 5, firstName: 'Ana', lastName: 'Pop', email: 'ana@x.io', roles: [{ id: 2, name: 'Member' }] },
  { userId: 6, firstName: 'Bob', lastName: 'Ionescu', email: 'bob@x.io', roles: [{ id: 2, name: 'Member' }] },
];

const task = (extra: Partial<TaskItem> = {}): TaskItem => ({
  id: 10, projectId: 1, assignedId: null, title: 'Write docs', description: '',
  status: TaskStatus.Created, priority: TaskPriority.Medium, ...extra,
});

// The component keeps its template state protected; tests drive it the way the template does.
type Internals = {
  assignedId: { set(v: number | null): void; (): number | null };
  assigneeOptions(): { label: string; value: number }[];
  startEdit(): void;
  save(): void;
};

describe('TaskDetails assignee', () => {
  let fixture: ComponentFixture<TaskDetails>;
  let http: HttpTestingController;
  let ui: Internals;
  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';
  const membersReq = () => http.expectOne(r => r.method === 'GET' && r.url.endsWith('/projects/1/members'));

  const create = (t: TaskItem) => {
    fixture = TestBed.createComponent(TaskDetails);
    fixture.componentRef.setInput('task', t);
    fixture.componentRef.setInput('projectKey', 'WEB');
    ui = fixture.componentInstance as unknown as Internals;
    fixture.detectChanges();
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  it('shows Unassigned and the assignee name in the read-only view', () => {
    create(task());
    expect(text()).toContain('Unassigned');
    fixture.componentRef.setInput('task', task({ assignedId: 5, assigned: 'Ana Pop' }));
    fixture.detectChanges();
    expect(text()).toContain('Ana Pop');
  });

  it('loads the project members when editing starts and offers them as options', () => {
    create(task());
    ui.startEdit();
    membersReq().flush(MEMBERS);
    expect(ui.assigneeOptions()).toEqual([{ label: 'Ana Pop', value: 5 }, { label: 'Bob Ionescu', value: 6 }]);
  });

  it('does not reload members that are already loaded', () => {
    create(task());
    ui.startEdit();
    membersReq().flush(MEMBERS);
    ui.startEdit();
    http.expectNone(r => r.url.endsWith('/members'));
  });

  it('keeps a current assignee who is not a member as an option', () => {
    create(task({ assignedId: 9, assigned: 'Eve Old' }));
    ui.startEdit();
    membersReq().flush(MEMBERS);
    expect(ui.assigneeOptions().map(o => o.value)).toEqual([5, 6, 9]);
    expect(ui.assigneeOptions().at(-1)?.label).toBe('Eve Old');
  });

  it('starts from the task assignee', () => {
    create(task({ assignedId: 6, assigned: 'Bob Ionescu' }));
    ui.startEdit();
    membersReq().flush(MEMBERS);
    expect(ui.assignedId()).toBe(6);
  });

  it('saves the chosen assignee', () => {
    create(task());
    ui.startEdit();
    membersReq().flush(MEMBERS);
    ui.assignedId.set(5);
    ui.save();
    const put = http.expectOne(r => r.method === 'PUT' && r.url.endsWith('/taskitem/10'));
    expect(put.request.body.assignedId).toBe(5);
    put.flush({ ...task(), assignedId: 5, assigned: 'Ana Pop' });
  });

  it('saves null when the assignee is cleared', () => {
    create(task({ assignedId: 5, assigned: 'Ana Pop' }));
    ui.startEdit();
    membersReq().flush(MEMBERS);
    ui.assignedId.set(null);
    ui.save();
    const put = http.expectOne(r => r.method === 'PUT');
    expect(put.request.body.assignedId).toBeNull();
    put.flush(task());
  });
});
