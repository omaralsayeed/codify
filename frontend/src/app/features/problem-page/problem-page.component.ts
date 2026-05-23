import { Component } from '@angular/core';

@Component({
  selector: 'app-problem-page',
  standalone: true,
  imports: [],
  templateUrl: './problem-page.component.html',
  styleUrl: './problem-page.component.scss'
})
export class ProblemPageComponent {
  isLeftPanelVisible: boolean = true;
  isRightPanelVisible: boolean = true;
  isEditorFullscreen: boolean = false;
  isBottomPanelOpen: boolean = true;
  activeTab: 'description' | 'editorial' | 'solutions' | 'submissions' = 'description';
  selectedLanguage: string = 'python';
  splitPosition: number = 50; // percentage for the draggable divider
}
