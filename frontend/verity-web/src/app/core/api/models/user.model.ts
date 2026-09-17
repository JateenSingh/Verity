export type UserRole = 'User' | 'Moderator';

export interface UserSummary {
  id: string;
  username: string;
  role: UserRole;
}
