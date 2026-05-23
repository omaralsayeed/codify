import { Component, inject, HostListener } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss'
})
export class NavbarComponent {
  auth = inject(AuthService);
  private router = inject(Router);

  isNotifOpen = false;
  isProfileOpen = false;
  isMobileMenuOpen = false;
  streakDays = 1;

  logout(): void {
    this.auth.logout();
    this.isMobileMenuOpen = false;
    this.router.navigate(['/']);
  }

  toggleMobileMenu(event: Event): void {
    event.stopPropagation();
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
    this.isNotifOpen = false;
    this.isProfileOpen = false;
  }

  toggleNotifications(event: Event): void {
    event.stopPropagation();
    this.isNotifOpen = !this.isNotifOpen;
    this.isProfileOpen = false;
  }

  toggleProfile(event: Event): void {
    event.stopPropagation();
    this.isProfileOpen = !this.isProfileOpen;
    this.isNotifOpen = false;
  }

  @HostListener('document:click')
  closeDropdowns(): void {
    this.isNotifOpen = false;
    this.isProfileOpen = false;
    this.isMobileMenuOpen = false;
  }
}
