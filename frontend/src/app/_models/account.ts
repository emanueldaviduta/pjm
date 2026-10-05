
export interface Account {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  createdAt: string;
}

export interface Register {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface ChangePassword {
  currentPassword: string;
  newPassword: string;
}

/** Minimum password length, matching the API's validation. */
export const MIN_PASSWORD_LENGTH = 8;
