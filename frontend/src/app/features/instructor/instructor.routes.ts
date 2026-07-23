import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { instructorGuard } from '../../core/guards/instructor.guard';

export const INSTRUCTOR_ROUTES: Routes = [
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./shell/instructor-shell.component').then(m => m.InstructorShellComponent),
    canActivate: [authGuard, instructorGuard],
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        loadComponent: () =>
          import('./overview/instructor-overview.component').then(m => m.InstructorOverviewComponent),
      },
      {
        path: 'students',
        loadComponent: () =>
          import('./students/instructor-students.component').then(m => m.InstructorStudentsComponent),
      },
      {
        path: 'integrity',
        loadComponent: () =>
          import('./integrity/instructor-integrity.component').then(m => m.InstructorIntegrityComponent),
      },
    ],
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];
