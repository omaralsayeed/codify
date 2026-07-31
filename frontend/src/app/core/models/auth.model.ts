import { User } from './user.model';

export interface AuthResult {
  success: boolean;
  error?: string;
  user?: User;
}

export interface RegisterData {
  fullName: string;
  email: string;
  password: string;
  role: 'student' | 'instructor';
}

export interface LoginFormData {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface RegisterFormData {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
  role: 'student' | 'instructor' | '';
}

export interface ForgotPasswordFormData {
  email: string;
}
