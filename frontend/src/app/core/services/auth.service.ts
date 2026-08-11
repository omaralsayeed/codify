import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, TimeoutError } from 'rxjs';
import { map, catchError, switchMap, timeout } from 'rxjs/operators';
import { User } from '../models/user.model';
import { AuthResult, RegisterData } from '../models/auth.model';
import { mapRole, roleToNumber } from '../utils/enum-mappers';

// Backend API response interfaces
interface LoginApiResponse {
  token: string;
  expiresAt: string;
  user: {
    userId: string;
    fullName: string;
    role: number;
  };
}

interface RegisterApiResponse {
  userId: string;
  email: string;
  role: number;
}

interface ApiEnvelope<T> {
  data: T;
}

interface ApiError {
  message: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5237/api';

  // Signal-based state management
  private _currentUser = signal<User | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly user = this._currentUser.asReadonly(); // Alias for backward compatibility
  readonly isLoggedIn = computed(() => this._currentUser() !== null);

  constructor() {
    this.restoreSession();
  }

  login(email: string, password: string): Observable<AuthResult> {
    return this.http
      .post<ApiEnvelope<LoginApiResponse>>(`${this.baseUrl}/auth/login`, { email, password })
      .pipe(
        timeout(10000),
        map(response => response.data),
        map(loginData => {
          const user: User = {
            id: loginData.user.userId,
            name: loginData.user.fullName,
            email: email,
            role: mapRole(loginData.user.role),
            avatarInitials: this.generateAvatarInitials(loginData.user.fullName),
            streak: 0
          };
          try {
            localStorage.setItem('codify_token', loginData.token);
            localStorage.setItem('codify_user', JSON.stringify(user));
          } catch (error) {
            console.error('Failed to persist session:', error);
          }
          this._currentUser.set(user);
          return { success: true, user };
        }),
        catchError(error => {
          if (error instanceof TimeoutError) {
            return of({ success: false, error: 'Server is not responding. Please check your connection.' });
          }
          const message = error.error?.message || 'Invalid email or password';
          return of({ success: false, error: message });
        })
      );
  }

  register(userData: RegisterData): Observable<AuthResult> {
    const body = {
      fullName: userData.fullName,
      email: userData.email,
      password: userData.password,
      role: roleToNumber(userData.role)
    };

    return this.http
      .post<any>(`${this.baseUrl}/auth/register`, body)
      .pipe(
        timeout(10000),
        switchMap(() => this.login(userData.email, userData.password)),
        catchError(error => {
          if (error instanceof TimeoutError) {
            return of({ success: false, error: 'Server is not responding. Please try again.' } as AuthResult);
          }
          const msg =
            error?.error?.message ||
            error?.error?.title ||
            error?.message ||
            'Registration failed. Please try again.';
          return of({ success: false, error: msg } as AuthResult);
        })
      );
  }

  logout(): void {
    // Remove from localStorage
    localStorage.removeItem('codify_user');
    localStorage.removeItem('codify_token');
    
    // Set currentUser signal to null
    this._currentUser.set(null);
  }

  private restoreSession(): void {
    try {
      const userJson = localStorage.getItem('codify_user');
      const token = localStorage.getItem('codify_token');
      
      if (userJson && token) {
        const user = JSON.parse(userJson);
        
        // Validate user structure
        if (this.isValidUser(user)) {
          this._currentUser.set(user);
        } else {
          this.clearSession();
        }
      }
    } catch (error) {
      console.error('Failed to restore session:', error);
      this.clearSession();
    }
  }

  private generateAvatarInitials(fullName: string): string {
    const words = fullName.trim().split(/\s+/);
    
    if (words.length === 0 || words[0] === '') {
      return 'U';
    }
    
    if (words.length === 1) {
      // Single word: take first two letters
      return words[0].substring(0, 2).toUpperCase();
    }
    
    // Multiple words: first letter of first and last word
    const firstInitial = words[0][0];
    const lastInitial = words[words.length - 1][0];
    return (firstInitial + lastInitial).toUpperCase();
  }

  private isValidUser(user: any): boolean {
    return (
      user &&
      typeof user.id === 'string' &&
      typeof user.name === 'string' &&
      typeof user.email === 'string' &&
      (user.role === 'student' || user.role === 'instructor') &&
      typeof user.avatarInitials === 'string'
    );
  }

  private clearSession(): void {
    localStorage.removeItem('codify_user');
    localStorage.removeItem('codify_token');
    this._currentUser.set(null);
  }
}
