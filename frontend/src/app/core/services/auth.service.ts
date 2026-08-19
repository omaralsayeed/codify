import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, TimeoutError } from 'rxjs';
import { map, catchError, switchMap, timeout } from 'rxjs/operators';
import { User, UpdateProfileDto } from '../models/user.model';
import { AuthResult, RegisterData } from '../models/auth.model';
import { mapRole, roleToNumber } from '../utils/enum-mappers';

// Backend API response interfaces
interface LoginApiResponse {
  token: string;
  expiresAt: string;
  user: {
    userId: string;
    fullName: string;
    role: number | string;
    avatarUrl?: string; // returned by backend once column is added
  };
}

interface RegisterApiResponse {
  userId: string;
  email: string;
  role: number | string;
  status?: string; // 'active' | 'pending' | 'Pending' — present once backend adds it
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
          // Prefer the avatarUrl from the backend response (works across devices).
          // Fall back to the localStorage key for sessions before backend support.
          const storedAvatarUrl =
            loginData.user.avatarUrl ??
            localStorage.getItem(`codify_avatar_${loginData.user.userId}`) ??
            undefined;

          const user: User = {
            id: loginData.user.userId,
            name: loginData.user.fullName,
            email: email,
            role: mapRole(loginData.user.role),
            avatarInitials: this.generateAvatarInitials(loginData.user.fullName),
            avatarUrl: storedAvatarUrl,
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
          // Handle pending instructor account — signal to component to redirect
          if (error?.error?.errorCode === 'ACCOUNT_PENDING') {
            return of({ success: false, pendingApproval: true } as AuthResult);
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
      role: roleToNumber(userData.role),
      organization: userData.organization ?? null
    };

    return this.http
      .post<ApiEnvelope<RegisterApiResponse>>(`${this.baseUrl}/auth/register`, body)
      .pipe(
        timeout(10000),
        switchMap(response => {
          const data = response.data;
          const roleStr = String(data.role).toLowerCase();
          const statusStr = String(data.status ?? '').toLowerCase();
          // Instructor registered — backend sets status='Pending' (PascalCase from C# enum)
          if ((roleStr === '1' || roleStr === 'instructor') && statusStr === 'pending') {
            return of({ success: true, pendingApproval: true } as AuthResult);
          }
          // Student registered — auto-login immediately
          return this.login(userData.email, userData.password);
        }),
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
    localStorage.removeItem('codify_user');
    localStorage.removeItem('codify_token');
    // codify_avatar_<userId> intentionally kept as a local fallback
    // until the backend fully returns avatarUrl on every login response.
    this._currentUser.set(null);
  }

  /**
   * Called after a successful Cloudinary upload.
   * 1. Patches the live user signal immediately (instant UI update).
   * 2. Saves the URL to localStorage as a fallback for the current browser.
   * 3. Persists the URL to the backend so it works on any device after login.
   */
  setAvatarUrl(url: string): void {
    const current = this._currentUser();
    if (!current) return;

    const updated: User = { ...current, avatarUrl: url };
    this._currentUser.set(updated);

    try {
      // Local fallback — used if backend hasn't shipped avatarUrl in login response yet
      localStorage.setItem(`codify_avatar_${current.id}`, url);
      localStorage.setItem('codify_user', JSON.stringify(updated));
    } catch {
      // localStorage full — image still visible in memory for this session
    }

    // Persist to backend — fire and forget (UI already updated above)
    const token = localStorage.getItem('codify_token');
    if (token) {
      this.http
        .put(
          `${this.baseUrl}/auth/avatar`,
          { avatarUrl: url },
          { headers: { Authorization: `Bearer ${token}` } }
        )
        .subscribe({
          error: err => console.warn('Avatar URL could not be saved to backend:', err)
        });
    }
  }

  /**
   * Updates the current user's profile fields.
   * Optimistic: patches the signal + localStorage immediately,
   * then fires PUT /api/auth/profile in the background.
   * Graceful fallback: if the endpoint doesn't exist yet (404/501)
   * the local update still sticks.
   */
  updateProfile(dto: UpdateProfileDto): Observable<{ success: boolean; error?: string }> {
    const current = this._currentUser();
    if (!current) return of({ success: false, error: 'Not logged in' });

    const updated: User = {
      ...current,
      name:           dto.fullName.trim() || current.name,
      avatarInitials: this.generateAvatarInitials(dto.fullName.trim() || current.name),
      headline:       dto.headline,
      bio:            dto.bio,
      organization:   dto.organization,
      social:         dto.social,
    };
    this._currentUser.set(updated);
    try { localStorage.setItem('codify_user', JSON.stringify(updated)); } catch { /* quota */ }

    const token = localStorage.getItem('codify_token');
    if (!token) return of({ success: true });

    return this.http
      .put<ApiEnvelope<null>>(
        `${this.baseUrl}/auth/profile`, dto,
        { headers: { Authorization: `Bearer ${token}` } },
      )
      .pipe(
        map(() => ({ success: true })),
        catchError(err => {
          if (err?.status === 404 || err?.status === 501) return of({ success: true });
          return of({ success: false, error: err?.error?.message ?? 'Could not save profile.' });
        }),
      );
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
      (user.role === 'student' || user.role === 'instructor' || user.role === 'admin') &&
      typeof user.avatarInitials === 'string'
    );
  }

  private clearSession(): void {
    localStorage.removeItem('codify_user');
    localStorage.removeItem('codify_token');
    this._currentUser.set(null);
  }
}
