import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProgressService } from '../../../../core/services/progress.service';
import { ProblemService } from '../../../../core/services/problem.service';
import { DifficultyBadgeComponent } from '../../../../shared/components/difficulty-badge/difficulty-badge.component';

@Component({
  selector: 'app-student-dashboard-preview',
  standalone: true,
  imports: [CommonModule, DifficultyBadgeComponent],
  templateUrl: './student-dashboard-preview.component.html',
  styleUrl: './student-dashboard-preview.component.scss'
})
export class StudentDashboardPreviewComponent {
  private progressSvc = inject(ProgressService);
  private problemSvc  = inject(ProblemService);

  progress = this.progressSvc.getStudentProgress();
  problems = this.problemSvc.getRecommended();

  barColor(pct: number): string {
    if (pct >= 75) return 'bar--teal';
    if (pct >= 55) return 'bar--blue';
    if (pct >= 40) return 'bar--gold';
    return 'bar--red';
  }
}
