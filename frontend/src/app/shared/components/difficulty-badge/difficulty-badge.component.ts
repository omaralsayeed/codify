import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Difficulty } from '../../../core/models/problem.model';

@Component({
  selector: 'app-difficulty-badge',
  standalone: true,
  imports: [CommonModule],
  template: `<span class="badge" [ngClass]="'badge--' + difficulty">{{ difficulty | titlecase }}</span>`
})
export class DifficultyBadgeComponent {
  @Input() difficulty!: Difficulty;
}
