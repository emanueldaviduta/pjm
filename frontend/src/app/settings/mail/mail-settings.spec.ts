import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { MailSecurity, MailSettings, MailSettingsForm, MailSettingsPage } from './mail-settings';

const STORED: MailSettings = {
  enabled: true, host: 'imap.example.com', port: 993, security: MailSecurity.SslOnConnect, username: 'tasks@example.com',
  hasPassword: true, folder: 'INBOX', allowedSenders: ['alerts@example.com'], projectId: 7, pollIntervalSeconds: 60,
  lastPollAt: '2026-10-12T09:30:00Z', lastPollError: null, lastPollCreated: 2, attachmentStorageConfigured: true,
};

type Internals = {
  form(): MailSettingsForm;
  set<K extends keyof MailSettingsForm>(key: K, value: MailSettingsForm[K]): void;
  newSender: { set(v: string): void };
  senderInvalid(): boolean;
  addSender(): void;
  save(): void;
  test(): void;
};

describe('MailSettingsPage', () => {
  let fixture: ComponentFixture<MailSettingsPage>;
  let http: HttpTestingController;
  let ui: Internals;
  const el = () => fixture.nativeElement as HTMLElement;
  const text = () => el().textContent ?? '';

  async function open(settings: MailSettings = STORED) {
    await TestBed.configureTestingModule({
      imports: [MailSettingsPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(MailSettingsPage);
    ui = fixture.componentInstance as unknown as Internals;
    fixture.detectChanges();
    http.expectOne(r => r.url.endsWith('/settings/projects')).flush([{ id: 7, name: 'Ops' }]);
    http.expectOne(r => r.method === 'GET' && r.url.endsWith('/settings/mail')).flush(settings);
    fixture.detectChanges();
  }

  beforeEach(() => TestBed.resetTestingModule());

  it('fills the form from the server, never with a password', async () => {
    await open();
    expect(ui.form()).toMatchObject({ host: 'imap.example.com', projectId: 7, allowedSenders: ['alerts@example.com'], password: '' });
    expect(el().querySelector<HTMLInputElement>('#mail-password')!.value).toBe('');
    expect(el().querySelector('[data-test=password-set]')).toBeTruthy();
    expect(el().querySelector('[data-test=storage-notice]')).toBeNull();
    expect(text()).toContain('2 task(s) created');
  });

  it('warns when attachment storage is not configured', async () => {
    await open({ ...STORED, attachmentStorageConfigured: false });
    expect(el().querySelector('[data-test=storage-notice]')?.textContent).toContain('attachments will not be stored');
  });

  it('shows the error of the last poll', async () => {
    await open({ ...STORED, lastPollError: 'Connection refused' });
    expect(el().querySelector('[data-test=poll-status]')?.textContent).toContain('Connection refused');
  });

  it('saves without a password when the field is empty', async () => {
    await open();
    ui.set('pollIntervalSeconds', 120);
    ui.save();
    const put = http.expectOne(r => r.method === 'PUT' && r.url.endsWith('/settings/mail'));
    expect(put.request.body).toMatchObject({ pollIntervalSeconds: 120, password: null });
    put.flush({ ...STORED, pollIntervalSeconds: 120 });
  });

  it('sends a typed password', async () => {
    await open();
    ui.set('password', 'new-app-password');
    ui.save();
    expect(http.expectOne(r => r.method === 'PUT').request.body.password).toBe('new-app-password');
  });

  it('shows a validation message and keeps what was typed', async () => {
    await open();
    ui.set('projectId', null);
    ui.set('host', 'typed.example.com');
    ui.save();
    http.expectOne(r => r.method === 'PUT').flush('A target project is required.', { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();
    expect(el().querySelector('[data-test=save-error]')?.textContent).toContain('A target project is required.');
    expect(ui.form().host).toBe('typed.example.com');
  });

  it('reports a successful and a failed connection test', async () => {
    await open();
    ui.test();
    http.expectOne(r => r.method === 'POST' && r.url.endsWith('/settings/mail/test')).flush({ success: true, message: 'Connected.' });
    fixture.detectChanges();
    expect(el().querySelector('[data-test=test-result]')?.textContent).toContain('Connected.');

    ui.test();
    http.expectOne(r => r.method === 'POST').flush({ success: false, message: 'Authentication failed: the server rejected the username or password.' });
    fixture.detectChanges();
    expect(el().querySelector('[data-test=test-result]')?.textContent).toContain('Authentication failed');
  });

  it('adds valid senders once and refuses malformed ones', async () => {
    await open();
    ui.newSender.set('not-an-email');
    expect(ui.senderInvalid()).toBe(true);
    ui.addSender();
    ui.newSender.set('Ops@Example.com');
    ui.addSender();
    ui.newSender.set('ops@example.com');
    ui.addSender();
    expect(ui.form().allowedSenders).toEqual(['alerts@example.com', 'Ops@Example.com']);
  });
});
