import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, describe, expect, it } from 'vitest';
import { AccountService } from '../_services/account.service';
import { Menu } from './menu';

describe('Menu', () => {
  let fixture: ComponentFixture<Menu>;

  async function render(isAdmin: boolean) {
    await TestBed.configureTestingModule({
      imports: [Menu],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    TestBed.inject(AccountService).me.set({ id: 1, firstName: 'A', lastName: 'B', email: 'a@x.io', createdAt: '', isAdmin });
    fixture = TestBed.createComponent(Menu);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController).match(() => true).forEach(r => r.flush([]));
    fixture.detectChanges();
  }

  const links = () =>
    [...(fixture.nativeElement as HTMLElement).querySelectorAll('a')].map(a => (a.textContent ?? '').trim());

  afterEach(() => TestBed.resetTestingModule());

  it('shows Settings with Users and Email → Project to an admin', async () => {
    await render(true);
    expect(fixture.nativeElement.querySelector('[data-test=settings-menu]')?.textContent).toContain('Settings');
    expect(links()).toEqual(expect.arrayContaining(['Users', 'Email → Project']));
  });

  it('shows neither Settings nor Users to a non-admin', async () => {
    await render(false);
    expect(fixture.nativeElement.querySelector('[data-test=settings-menu]')).toBeNull();
    expect(links()).not.toContain('Users');
    expect(links()).not.toContain('Email → Project');
    expect(links()).toEqual(['Home', 'Projects', 'Tasks']);
  });
});
