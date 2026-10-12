export class Project {
  id?: number;
  name?: string;
  description?: string;
  createdAt?: string;
  updatedAt?: string;
  isDeleted?: boolean;
  /** Names of the roles the signed-in user holds in this project, as sent by the server. */
  roles?: string[];
  constructor(name: string, description: string) {
    this.name = name;
    this.description = description;
  }
}

export const OWNER_ROLE = 'Owner';

/** Whether the signed-in user may manage the project's members (and delete it). */
export function isOwner(project?: Pick<Project, 'roles'>): boolean {
  return !!project?.roles?.includes(OWNER_ROLE);
}

/**
 * Short code shown next to a project and used as the prefix of its task ids (e.g. WEB-12).
 * Derived from the name until the backend stores a real key.
 */
export function projectKey(name?: string): string {
  const words = (name ?? '')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toUpperCase()
    .replace(/[^A-Z0-9 ]/g, ' ')
    .split(' ')
    .filter(Boolean);
  if (!words.length) return '—';
  if (words.length === 1) return words[0].slice(0, 3);
  return words.slice(0, 3).map(word => word[0]).join('');
}
