import { Role } from './role';

/** A user on a project with every role they hold in that project. */
export interface ProjectMember {
  userId: number;
  firstName: string;
  lastName: string;
  email: string;
  roles: Role[];
}

export function memberName(member: Pick<ProjectMember, 'firstName' | 'lastName'>): string {
  return `${member.firstName} ${member.lastName}`.trim();
}

export function memberInitials(member: Pick<ProjectMember, 'firstName' | 'lastName'>): string {
  return `${member.firstName?.[0] ?? ''}${member.lastName?.[0] ?? ''}`.toUpperCase() || '?';
}
