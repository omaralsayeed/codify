import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { HintBubbleComponent } from '../../../../shared/components/hint-bubble/hint-bubble.component';
import { Hint } from '../../../../core/models/hint.model';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-hero',
  standalone: true,
  imports: [HintBubbleComponent],
  templateUrl: './hero.component.html',
  styleUrl: './hero.component.scss'
})
export class HeroComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  sampleHint: Hint = {
    stepIndex: 2,
    totalSteps: 4,
    text: 'Think about what happens when you reach a cell you\'ve already explored. Could you store that result instead of recomputing it?',
    problemId: 'sample-1'
  };

  handleCTA(): void {
    if (this.auth.isLoggedIn()) {
      this.router.navigate(['/problems']);
    } else {
      this.router.navigate(['/auth/login']);
    }
  }
}
