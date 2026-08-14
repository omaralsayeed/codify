import {
  Component, inject, OnInit, OnDestroy, HostListener, signal,
} from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.scss',
})
export class AdminShellComponent implements OnInit, OnDestroy {
  readonly auth   = inject(AuthService);
  private  router = inject(Router);

  // Dynamic bottom offset for the floating sidebar
  // Starts at 16px gap, increases as footer scrolls into view
  sidebarBottom = signal('16px');

  private readonly NAVBAR_HEIGHT  = 64;
  private readonly FOOTER_HEIGHT  = 112; // approximate footer height
  private readonly GAP            = 16;

  ngOnInit(): void {
    this.updateSidebarBottom();
  }

  ngOnDestroy(): void {}

  @HostListener('window:scroll')
  onScroll(): void {
    this.updateSidebarBottom();
  }

  private updateSidebarBottom(): void {
    const scrollY      = window.scrollY;
    const windowH      = window.innerHeight;
    const docH         = document.documentElement.scrollHeight;

    // How many px of the footer are currently visible in viewport
    const footerTop    = docH - this.FOOTER_HEIGHT;
    const footerVisible = Math.max(0, (scrollY + windowH) - footerTop);

    // bottom = GAP + however much footer is visible
    const bottom = this.GAP + footerVisible;
    this.sidebarBottom.set(`${bottom}px`);
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }
}
