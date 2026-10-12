import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { Account } from '../_models/account';
import { apiErrorMessage } from '../_services/account.service';
import { ProjectMember, memberInitials, memberName } from '../_models/project-member';
import { Role } from '../_models/role';
import { ProjectMemberService } from '../_services/project-member.service';
import { ProjectService } from '../_services/project.service';
import { RoleService } from '../_services/role.service';
import { UserDirectoryService } from '../_services/user-directory.service';

interface UserOption {
  id: number;
  label: string;
  isMember: boolean;
}

const DEFAULT_ROLE = 'Member';

/** Lists the members of a project; owners can also add people, change their roles and remove them. */
@Component({
  imports: [AutoCompleteModule, ButtonModule, ConfirmDialogModule, DialogModule, FormsModule, RouterLink, SelectModule],
  providers: [ConfirmationService],
  selector: 'app-project-members',
  styleUrl: './project-members.less',
  templateUrl: './project-members.html',
})
export class ProjectMembers implements OnInit {
  readonly projectId = input.required<number>();
  /** Only owners get the add, change-role and remove controls; the server enforces the same rule. */
  readonly canManage = input(false);

  protected memberService = inject(ProjectMemberService);
  protected roleService = inject(RoleService);
  protected userDirectory = inject(UserDirectoryService);
  private projectService = inject(ProjectService);
  private confirmation = inject(ConfirmationService);
  private messages = inject(MessageService);

  protected readonly initials = memberInitials;
  protected readonly nameOf = memberName;

  protected readonly members = computed(() => this.memberService.members(this.projectId()));
  protected readonly state = computed(() => this.memberService.state(this.projectId()));

  // Add-member dialog.
  protected readonly dialogOpen = signal(false);
  protected readonly selectedUser = signal<UserOption | null>(null);
  protected readonly roleId = signal<number | null>(null);
  protected readonly suggestions = signal<UserOption[]>([]);
  protected readonly isSaving = signal(false);

  /** Roles the selected user already holds in this project; they cannot be added again. */
  private readonly heldRoleIds = computed(() => {
    const user = this.selectedUser();
    const member = user && this.members().find(m => m.userId === user.id);
    return new Set(member?.roles.map(r => r.id) ?? []);
  });

  protected readonly roleOptions = computed(() =>
    this.roleService.roles().map(role => ({ ...role, disabled: this.heldRoleIds().has(role.id) })),
  );

  protected readonly canConfirm = computed(
    () => !!this.selectedUser() && this.roleId() !== null && !this.heldRoleIds().has(this.roleId()!) && !this.isSaving(),
  );

  ngOnInit(): void {
    this.reload();
    this.roleService.load();
  }

  protected reload() {
    this.memberService.load(this.projectId());
  }

  /** Roles a member does not hold yet, offered by the "add role" control. */
  protected availableRoles(member: ProjectMember): Role[] {
    const held = new Set(member.roles.map(r => r.id));
    return this.roleService.roles().filter(r => !held.has(r.id));
  }

  // --- Add member -------------------------------------------------------------------------

  protected openAdd() {
    this.userDirectory.load();
    this.roleService.load();
    this.selectedUser.set(null);
    this.suggestions.set([]);
    this.roleId.set(this.roleService.roles().find(r => r.name === DEFAULT_ROLE)?.id ?? null);
    this.dialogOpen.set(true);
  }

  protected search(query: string) {
    const needle = query.trim().toLowerCase();
    const memberIds = new Set(this.members().map(m => m.userId));
    this.suggestions.set(
      this.userDirectory
        .users()
        .filter(u => !needle || this.userText(u).toLowerCase().includes(needle))
        .map(u => ({ id: u.id, label: this.userText(u), isMember: memberIds.has(u.id) })),
    );
  }

  protected confirmAdd() {
    const user = this.selectedUser();
    const roleId = this.roleId();
    if (!user || roleId === null || !this.canConfirm()) return;

    this.isSaving.set(true);
    this.memberService.addRole(this.projectId(), user.id, roleId).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.dialogOpen.set(false);
        this.toast('success', `${user.label} added to the project`);
        this.afterChange();
      },
      error: error => {
        this.isSaving.set(false);
        this.fail(error, 'Failed to add member', 'That user already has this role in the project');
      },
    });
  }

  // --- Change roles -----------------------------------------------------------------------

  protected addRole(member: ProjectMember, roleId: number | null) {
    if (roleId === null) return;
    this.memberService.addRole(this.projectId(), member.userId, roleId).subscribe({
      next: () => this.afterChange(),
      error: error => this.fail(error, 'Failed to add role', 'That user already has this role in the project'),
    });
  }

  /** Removes one role; taking the last one away removes the member, so that asks first. */
  protected removeRole(member: ProjectMember, role: Role) {
    if (member.roles.length <= 1) {
      this.removeMember(member);
      return;
    }
    this.memberService.removeRole(this.projectId(), member.userId, role.id).subscribe({
      next: () => this.afterChange(),
      error: error => this.fail(error, 'Failed to remove role'),
    });
  }

  protected removeMember(member: ProjectMember) {
    this.confirmation.confirm({
      header: 'Remove member',
      message: `Remove ${memberName(member)} from this project? They lose all their roles in it.`,
      acceptLabel: 'Remove',
      rejectLabel: 'Cancel',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', variant: 'outlined' },
      accept: () =>
        this.memberService.removeMember(this.projectId(), member.userId).subscribe({
          next: () => {
            this.toast('success', `${memberName(member)} removed from the project`);
            this.afterChange();
          },
          error: error => this.fail(error, 'Failed to remove member'),
        }),
    });
  }

  private userText(user: Account) {
    return `${user.firstName} ${user.lastName} (${user.email})`;
  }

  /** The change may have touched the signed-in user's own roles, so refresh which projects they own or see. */
  private afterChange() {
    this.projectService.load(true);
  }

  private fail(error: unknown, detail: string, conflictDetail?: string) {
    const status = error instanceof HttpErrorResponse ? error.status : 0;
    if (status === 401) {
      this.toast('error', 'Your session expired. Sign in again.');
    } else if (status === 403) {
      this.toast('error', 'Only project owners can manage members.');
      this.afterChange();
    } else if (status === 409 && conflictDetail) {
      this.toast('warn', conflictDetail);
    } else if (status === 400) {
      // e.g. "A project must keep at least one owner."
      this.toast('error', apiErrorMessage(error as HttpErrorResponse, detail));
    } else {
      this.toast('error', detail);
    }
  }

  private toast(severity: 'success' | 'warn' | 'error', detail: string) {
    const summary = severity === 'success' ? 'Done' : severity === 'warn' ? 'Not added' : 'Error';
    this.messages.add({ severity, summary, detail });
  }
}
