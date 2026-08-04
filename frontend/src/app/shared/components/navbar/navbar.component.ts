import { Component, inject, HostListener } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

/** Converts a display name to a URL-safe username slug. */
function toSlug(name: string): string {
  return name.trim().toLowerCase().replace(/\s+/g, '_');
}

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss'
})
export class NavbarComponent {
  auth = inject(AuthService);
  readonly router = inject(Router);

  isNotifOpen      = false;
  isProfileOpen    = false;
  isMobileMenuOpen = false;
  streakDays       = 1;

  /** URL to the current user's public profile page. */
  get profileUrl(): string {
    const user = this.auth.user();
    return user ? `/profile/${toSlug(user.name)}` : '/';
  }

  logout(): void {
    this.auth.logout();
    this.isMobileMenuOpen = false;
    this.isProfileOpen    = false;
    this.router.navigate(['/']);
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
    this.isProfileOpen    = false;
    this.isMobileMenuOpen = false;
    this.isNotifOpen      = false;
  }

  toggleMobileMenu(event: Event): void {
    event.stopPropagation();
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
    this.isNotifOpen   = false;
    this.isProfileOpen = false;
  }

  toggleNotifications(event: Event): void {
    event.stopPropagation();
    this.isNotifOpen   = !this.isNotifOpen;
    this.isProfileOpen = false;
  }

  toggleProfile(event: Event): void {
    event.stopPropagation();
    this.isProfileOpen = !this.isProfileOpen;
    this.isNotifOpen   = false;
  }

  @HostListener('document:click')
  closeDropdowns(): void {
    this.isNotifOpen      = false;
    this.isProfileOpen    = false;
    this.isMobileMenuOpen = false;
  }
}
