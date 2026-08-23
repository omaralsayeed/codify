export interface User {
  id: string;
  name: string;
  email: string;
  role: 'student' | 'instructor' | 'admin';
  avatarInitials: string;
  avatarUrl?: string;
  streak?: number;
  username?: string;
  joinedAt?: string;
  password?: string;
  /** Subscription plan — set client-side after Stripe redirect, persisted in localStorage */
  plan?: 'free' | 'learner' | 'proplus';
  // Extended profile fields
  headline?: string;
  bio?: string;
  organization?: string;
  social?: {
    linkedin?: string;
    github?: string;
    twitter?: string;
  };
}

export interface UpdateProfileDto {
  fullName: string;
  headline?: string;
  bio?: string;
  organization?: string;
  social?: {
    linkedin?: string;
    github?: string;
    twitter?: string;
  };
}
