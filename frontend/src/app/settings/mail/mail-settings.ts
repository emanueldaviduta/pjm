import { DatePipe } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ChipModule } from 'primeng/chip';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { environment } from '../../../environments/environment';
import { apiErrorMessage } from '../../_services/account.service';

export enum MailSecurity {
  Auto = 0,
  SslOnConnect = 1,
  StartTls = 2,
  None = 3,
}

/** Settings as the server returns them: never the password, only whether one is stored. */
export interface MailSettings {
  enabled: boolean;
  host: string;
  port: number;
  security: MailSecurity;
  username: string;
  hasPassword: boolean;
  folder: string;
  allowedSenders: string[];
  projectId: number | null;
  pollIntervalSeconds: number;
  lastPollAt: string | null;
  lastPollError: string | null;
  lastPollCreated: number | null;
  attachmentStorageConfigured: boolean;
}

export interface MailSettingsForm {
  enabled: boolean;
  host: string;
  port: number;
  security: MailSecurity;
  username: string;
  /** Left empty to keep the stored password. */
  password: string;
  folder: string;
  allowedSenders: string[];
  projectId: number | null;
  pollIntervalSeconds: number;
}

interface ProjectOption {
  id: number;
  name: string;
}

const EMAIL = /^[^\s@<>]+@[^\s@<>]+\.[^\s@<>]+$/;

/** Settings → Email → Project: the mailbox whose emails from allowed senders become tasks on a project. */
@Component({
  imports: [
    ButtonModule, ChipModule, DatePipe, FormsModule, InputNumberModule, InputTextModule, MessageModule, SelectModule,
    ToastModule, ToggleSwitchModule,
  ],
  providers: [MessageService],
  selector: 'app-mail-settings',
  styleUrl: './mail-settings.less',
  templateUrl: './mail-settings.html',
})
export class MailSettingsPage implements OnInit {
  private api = inject(HttpClient);
  private messages = inject(MessageService);
  private baseUrl = environment.apiUrl + '/settings';

  protected readonly securityOptions = [
    { label: 'Automatic', value: MailSecurity.Auto },
    { label: 'SSL/TLS', value: MailSecurity.SslOnConnect },
    { label: 'STARTTLS', value: MailSecurity.StartTls },
    { label: 'None (unencrypted)', value: MailSecurity.None },
  ];

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly testing = signal(false);
  protected readonly projects = signal<ProjectOption[]>([]);
  /** The last loaded or saved settings: status panel, "password set" hint, storage notice. */
  protected readonly status = signal<MailSettings | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly testResult = signal<{ success: boolean; message: string } | null>(null);

  protected readonly form = signal<MailSettingsForm>({
    enabled: false, host: '', port: 993, security: MailSecurity.Auto, username: '', password: '', folder: 'INBOX',
    allowedSenders: [], projectId: null, pollIntervalSeconds: 60,
  });
  protected readonly newSender = signal('');
  protected readonly senderInvalid = computed(() => !!this.newSender().trim() && !EMAIL.test(this.newSender().trim()));

  ngOnInit(): void {
    this.api.get<ProjectOption[]>(this.baseUrl + '/projects').subscribe({
      next: projects => this.projects.set(projects),
      error: () => this.toast('error', 'Failed to load projects'),
    });
    this.api.get<MailSettings>(this.baseUrl + '/mail').subscribe({
      next: settings => {
        this.apply(settings);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load the mail settings.');
      },
    });
  }

  protected set<K extends keyof MailSettingsForm>(key: K, value: MailSettingsForm[K]) {
    this.form.update(form => ({ ...form, [key]: value }));
  }

  protected addSender() {
    const sender = this.newSender().trim();
    if (!sender || this.senderInvalid()) return;
    if (!this.form().allowedSenders.some(s => s.toLowerCase() === sender.toLowerCase())) {
      this.set('allowedSenders', [...this.form().allowedSenders, sender]);
    }
    this.newSender.set('');
  }

  protected removeSender(sender: string) {
    this.set('allowedSenders', this.form().allowedSenders.filter(s => s !== sender));
  }

  protected save() {
    this.saving.set(true);
    this.error.set(null);
    this.api.put<MailSettings>(this.baseUrl + '/mail', this.body()).subscribe({
      next: settings => {
        this.saving.set(false);
        this.apply(settings);
        this.toast('success', 'Mail settings saved');
      },
      error: (error: HttpErrorResponse) => {
        // Keep what the admin typed; show why the server refused it.
        this.saving.set(false);
        this.error.set(apiErrorMessage(error, 'Failed to save the mail settings.'));
      },
    });
  }

  protected test() {
    this.testing.set(true);
    this.testResult.set(null);
    this.api.post<{ success: boolean; message: string }>(this.baseUrl + '/mail/test', this.body()).subscribe({
      next: result => {
        this.testing.set(false);
        this.testResult.set(result);
      },
      error: (error: HttpErrorResponse) => {
        this.testing.set(false);
        this.testResult.set({ success: false, message: apiErrorMessage(error, 'The test could not be run.') });
      },
    });
  }

  /** The request body; an empty password is sent as null so the stored one is kept. */
  private body() {
    const { password, ...rest } = this.form();
    return { ...rest, password: password ? password : null };
  }

  private apply(settings: MailSettings) {
    this.status.set(settings);
    this.form.set({
      enabled: settings.enabled,
      host: settings.host,
      port: settings.port,
      security: settings.security,
      username: settings.username,
      password: '',
      folder: settings.folder,
      allowedSenders: [...settings.allowedSenders],
      projectId: settings.projectId,
      pollIntervalSeconds: settings.pollIntervalSeconds,
    });
  }

  private toast(severity: 'success' | 'error', detail: string) {
    this.messages.add({ severity, summary: severity === 'success' ? 'Done' : 'Error', detail });
  }
}
