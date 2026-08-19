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
        path: 'students/:id',
        loadComponent: () =>
          import('./student-detail/instructor-student-detail.component').then(m => m.InstructorStudentDetailComponent),
      },
      {
        path: 'integrity',
        loadComponent: () =>
          import('./integrity/instructor-integrity.component').then(m => m.InstructorIntegrityComponent),
      },
      {
        path: 'contests',
        loadComponent: () =>
          import('./contests/instructor-contests.component').then(m => m.InstructorContestsComponent),
      },
      {
        path: 'contests/new',
        loadComponent: () =>
          import('./contest-create/instructor-contest-create.component').then(m => m.InstructorContestCreateComponent),
      },
      {
        path: 'contests/:id',
        loadComponent: () =>
          import('./contest-detail/instructor-contest-detail.component').then(m => m.InstructorContestDetailComponent),
      },
    ],
  },
  {
    path: 'profile',
    loadComponent: () =>
      import('./instructor-profile/instructor-profile.component').then(m => m.InstructorProfileComponent),
    canActivate: [authGuard, instructorGuard],
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];
