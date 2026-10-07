import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, beforeEach } from 'vitest';
import { ProjectMemberService } from './project-member.service';

const member = (roles: { id: number; name: string }[]) =>
  ({ userId: 5, firstName: 'Ana', lastName: 'Pop', email: 'ana@x.io', roles });

describe('ProjectMemberService', () => {
  let service: ProjectMemberService;
  let http: HttpTestingController;
  const list = () => http.expectOne(r => r.method === 'GET' && r.url.endsWith('/projects/1/members'));

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ProjectMemberService);
    http = TestBed.inject(HttpTestingController);
  });

  it('loads members of a project', () => {
    service.load(1);
    expect(service.state(1)).toBe('loading');
    list().flush([member([{ id: 2, name: 'Member' }])]);
    expect(service.members(1)).toHaveLength(1);
    expect(service.state(1)).toBe('ready');
    expect(service.members(2)).toEqual([]);
  });

  it('adds a role and reloads', () => {
    service.addRole(1, 5, 1).subscribe();
    const post = http.expectOne(r => r.method === 'POST' && r.url.endsWith('/projects/1/members'));
    expect(post.request.body).toEqual({ userId: 5, roleId: 1 });
    post.flush(null, { status: 201, statusText: 'Created' });
    list().flush([member([{ id: 1, name: 'Owner' }])]);
    expect(service.members(1)[0].roles[0].name).toBe('Owner');
  });

  it('reloads and surfaces the error on a duplicate (409)', () => {
    let status = 0;
    service.addRole(1, 5, 1).subscribe({ error: e => (status = e.status) });
    http.expectOne(r => r.method === 'POST').flush('', { status: 409, statusText: 'Conflict' });
    expect(status).toBe(409);
    list().flush([]);
  });

  it('removes one role', () => {
    service.removeRole(1, 5, 1).subscribe();
    http.expectOne(r => r.method === 'DELETE' && r.url.endsWith('/projects/1/members/5/roles/1')).flush(null);
    list().flush([member([{ id: 2, name: 'Member' }])]);
    expect(service.members(1)[0].roles).toHaveLength(1);
  });

  it('removes a member', () => {
    service.removeMember(1, 5).subscribe();
    http.expectOne(r => r.method === 'DELETE' && r.url.endsWith('/projects/1/members/5')).flush(null);
    list().flush([]);
    expect(service.members(1)).toEqual([]);
  });

  it('reloads on 404', () => {
    let status = 0;
    service.removeMember(1, 99).subscribe({ error: e => (status = e.status) });
    http.expectOne(r => r.method === 'DELETE').flush('', { status: 404, statusText: 'Not Found' });
    expect(status).toBe(404);
    list().flush([]);
  });

  it('marks the project unauthorized on 401', () => {
    service.load(1);
    list().flush('', { status: 401, statusText: 'Unauthorized' });
    expect(service.state(1)).toBe('unauthorized');
  });
});
