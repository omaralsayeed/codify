export interface User {
  id: string;
  name: string;
  email: string;
  role: 'student' | 'instructor';
  avatarInitials: string;
  streak?: number;
  username?: string;   // URL-safe slug derived from name, e.g. "test_student"
  joinedAt?: string;   // ISO date string
  password?: string;   // Optional, for mock data only
}
