export interface User {
  id: string;
  name: string;
  email: string;
  role: 'student' | 'instructor';
  avatarInitials: string;
  streak?: number;
  password?: string; // Optional, for mock data only
}
