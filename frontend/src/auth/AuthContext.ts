import { createContext } from 'react';
import type { components } from '../api/schema';
import type { Session } from '../api/session';

export type CurrentUser = components['schemas']['CurrentUserDto'];
export type Role = 'student' | 'professor' | 'department_head' | 'admin';

export type LoginResult = { kind: 'signed-in'; mustChangePassword: boolean } | { kind: 'choose-role'; roles: Role[] };

export interface AuthState {
  session: Session | null;
  user: CurrentUser | null;
  loadingUser: boolean;
  /** Signs in with an email or a national ID; asks for a role when the national ID has several accounts. */
  login(login: string, password: string, role?: Role): Promise<LoginResult>;
  logout(): Promise<void>;
  changePassword(currentPassword: string, newPassword: string): Promise<void>;
}

export const AuthContext = createContext<AuthState | null>(null);
