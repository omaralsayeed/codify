import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-instructor-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './instructor-shell.component.html',
  styleUrl: './instructor-shell.component.scss',
})
export class InstructorShellComponent {
  readonly auth = inject(AuthService);
}
