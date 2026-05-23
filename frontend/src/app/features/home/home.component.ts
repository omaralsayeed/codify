import { Component } from '@angular/core';
import { HeroComponent }                       from './components/hero/hero.component';
import { FeaturesSectionComponent }            from './components/features-section/features-section.component';
import { HowItWorksComponent }                 from './components/how-it-works/how-it-works.component';
import { StudentDashboardPreviewComponent }    from './components/student-dashboard-preview/student-dashboard-preview.component';
import { InstructorDashboardPreviewComponent } from './components/instructor-dashboard-preview/instructor-dashboard-preview.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    HeroComponent,
    FeaturesSectionComponent,
    HowItWorksComponent,
    StudentDashboardPreviewComponent,
    InstructorDashboardPreviewComponent,
  ],
  template: `
    <app-hero />
    <app-features-section />
    <app-how-it-works />
    <app-student-dashboard-preview />
    <app-instructor-dashboard-preview />
  `
})
export class HomeComponent {}
