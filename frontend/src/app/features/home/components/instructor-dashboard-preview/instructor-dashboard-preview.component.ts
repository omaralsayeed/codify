import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProgressService } from '../../../../core/services/progress.service';

interface IntegrityAlert { dotClass: string; text: string; time: string; }
interface StudentRow     { initials: string; name: string; score: number; integrityClass: string; integrityLabel: string; }

@Component({
  selector: 'app-instructor-dashboard-preview',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './instructor-dashboard-preview.component.html',
  styleUrl: './instructor-dashboard-preview.component.scss'
})
export class InstructorDashboardPreviewComponent {
  private progressSvc = inject(ProgressService);
  classProgress = this.progressSvc.getClassProgress();

  alerts: IntegrityAlert[] = [
    { dotClass: 'dot--red',  text: 'Omar S. — submission matches AI pattern (87% confidence)', time: '2 hours ago' },
    { dotClass: 'dot--gold', text: 'Sara M. — code structure similar to Karim A.',             time: 'Yesterday' },
    { dotClass: 'dot--blue', text: 'Nour T. — zero hints used, 100% score on Hard problem',    time: '2 days ago' },
  ];

  students: StudentRow[] = [
    { initials: 'KA', name: 'Karim Ahmed',   score: 92, integrityClass: 'badge--clean', integrityLabel: 'Clean'   },
    { initials: 'LM', name: 'Layla Mostafa', score: 88, integrityClass: 'badge--clean', integrityLabel: 'Clean'   },
    { initials: 'OS', name: 'Omar Sherif',   score: 85, integrityClass: 'badge--flag',  integrityLabel: 'Flagged' },
    { initials: 'SM', name: 'Sara Mahmoud',  score: 81, integrityClass: 'badge--warn',  integrityLabel: 'Review'  },
  ];

  barColor(pct: number): string {
    if (pct >= 70) return 'bar--teal';
    if (pct >= 55) return 'bar--blue';
    if (pct >= 44) return 'bar--gold';
    return 'bar--red';
  }
}
