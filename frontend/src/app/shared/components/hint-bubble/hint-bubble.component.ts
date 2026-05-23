import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Hint } from '../../../core/models/hint.model';

@Component({
  selector: 'app-hint-bubble',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './hint-bubble.component.html',
  styleUrl: './hint-bubble.component.scss'
})
export class HintBubbleComponent {
  @Input() hint!: Hint;
}
