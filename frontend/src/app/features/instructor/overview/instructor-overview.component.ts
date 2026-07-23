import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InstructorService } from '../../../core/services/instructor.service';
import { ClassProgress } from '../../../core/models/progress.model';

@Component({
  selector: 'app-instructor-overview',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instructor-overview.component.html',
  styleUrl: './instructor-overview.component.scss',
})
export class InstructorOverviewComponent {
  private readonly instructorSvc = inject(InstructorService);

  readonly progress: ClassProgress = this.instructorSvc.getClassProgress();

  readonly metrics = [
    {
      label: 'Active Students',
      value: this.progress.activeStudents,
      sub: `of ${this.progress.enrolledStudents} enrolled`,
      colorClass: 'card--teal',
      icon: '👥',
    },
    {
      label: 'Class Avg Score',
      value: this.progress.classAvgScore,
      sub: 'out of 100',
      colorClass: 'card--blue',
      icon: '📊',
    },
    {
      label: 'Integrity Flags',
      value: this.progress.integrityFlags,
      sub: 'need review',
      colorClass: this.progress.integrityFlags > 0 ? 'card--red' : 'card--teal',
      icon: '⚑',
    },
    {
      label: 'Assigned Problems',
      value: this.progress.assignedProblems,
      sub: 'total problems',
      colorClass: 'card--gold',
      icon: '🗂️',
    },
  ];

  barColor(pct: number): string {
    if (pct >= 70) return 'bar--teal';
    if (pct >= 55) return 'bar--blue';
    if (pct >= 40) return 'bar--gold';
    return 'bar--red';
  }
}
