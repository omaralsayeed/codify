import { Injectable, signal, computed } from '@angular/core';
import { Observable, of } from 'rxjs';
import { User } from '../models/user.model';
import { AuthResult, RegisterData } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Mock user list with two test users
  private mockUsers: User[] = [
    {
      id: '1',
      name: 'Test Student',
      email: 'student@codify.com',
      password: '123456',
      role: 'student',
      avatarInitials: 'TS',
      streak: 0
    },
    {
      id: '2',
      name: 'Test Instructor',
      email: 'instructor@codify.com',
      password: '123456',
      role: 'instructor',
      avatarInitials: 'TI'
    }
  ];

  // Signal-based state management
  private _currentUser = signal<User | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly user = this._currentUser.asReadonly(); // Alias for backward compatibility
  readonly isLoggedIn = computed(() => this._currentUser() !== null);

  constructor() {
    this.restoreSession();
  }

  login(email: string, password: string): Observable<AuthResult> {
    // Validate credentials against mock user list
    const user = this.mockUsers.find(
      u => u.email === email && u.password === password
    );

    if (user) {
      // Generate mock token
      const token = 'mock-token-' + Date.now();
      
      // Create user object without password for storage
      const userToStore = { ...user };
      delete userToStore.password;
      
      // Store in localStorage
      try {
        localStorage.setItem('codify_user', JSON.stringify(userToStore));
        localStorage.setItem('codify_token', token);
      } catch (error) {
        console.error('Failed to persist session:', error);
      }
      
      // Update currentUser signal
      this._currentUser.set(userToStore);
      
      return of({ success: true, user: userToStore });
    }

    return of({ success: false, error: 'Invalid email or password' });
  }

  register(userData: RegisterData): Observable<AuthResult> {
    // Generate unique user ID
    const id = Date.now().toString();
    
    // Generate avatarInitials from full name
    const avatarInitials = this.generateAvatarInitials(userData.fullName);
    
    // Create User object
    const newUser: User = {
      id,
      name: userData.fullName,
      email: userData.email,
      password: userData.password,
      role: userData.role,
      avatarInitials,
      streak: userData.role === 'student' ? 0 : undefined
    };
    
    // Add user to mock user list
    this.mockUsers.push(newUser);
    
    // Generate mock token
    const token = 'mock-token-' + Date.now();
    
    // Create user object without password for storage
    const userToStore = { ...newUser };
    delete userToStore.password;
    
    // Store in localStorage
    try {
      localStorage.setItem('codify_user', JSON.stringify(userToStore));
      localStorage.setItem('codify_token', token);
    } catch (error) {
      console.error('Failed to persist session:', error);
    }
    
    // Update currentUser signal
    this._currentUser.set(userToStore);
    
    return of({ success: true, user: userToStore });
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
