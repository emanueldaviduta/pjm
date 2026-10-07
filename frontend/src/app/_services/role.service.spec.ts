import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, beforeEach } from 'vitest';
import { RoleService } from './role.service';
import { UserDirectoryService } from './user-directory.service';

describe('catalog services', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  it('RoleService loads the roles once', () => {
    const service = TestBed.inject(RoleService);
    service.load();
    service.load();
    http.expectOne(r => r.url.endsWith('/roles')).flush([{ id: 1, name: 'Owner' }, { id: 2, name: 'Member' }]);
    service.load();
    http.expectNone(r => r.url.endsWith('/roles'));
    expect(service.roles().map(r => r.name)).toEqual(['Owner', 'Member']);
    expect(service.state()).toBe('ready');
  });

  it('UserDirectoryService loads the users once', () => {
    const service = TestBed.inject(UserDirectoryService);
    service.load();
    service.load();
    http.expectOne(r => r.url.endsWith('/account/users')).flush([
      { id: 1, firstName: 'Ana', lastName: 'Pop', email: 'ana@x.io', createdAt: '' },
    ]);
    service.load();
    http.expectNone(r => r.url.endsWith('/account/users'));
    expect(service.users()).toHaveLength(1);
    expect(service.state()).toBe('ready');
  });

  it('reports unauthorized on 401', () => {
    const service = TestBed.inject(RoleService);
    service.load();
    http.expectOne(r => r.url.endsWith('/roles')).flush('', { status: 401, statusText: 'Unauthorized' });
    expect(service.state()).toBe('unauthorized');
  });
});
