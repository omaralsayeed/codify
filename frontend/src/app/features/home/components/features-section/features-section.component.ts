import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

interface Feature {
  icon: string;
  title: string;
  description: string;
  badgeText: string;
  badgeClass: string;
  iconBg: string;
}

@Component({
  selector: 'app-features-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './features-section.component.html',
  styleUrl: './features-section.component.scss'
})
export class FeaturesSectionComponent {
  features: Feature[] = [
    { icon: '🧠', title: 'AI Hint System',       description: 'Get guided step-by-step hints that push your thinking forward without revealing the solution.',  badgeText: 'Core feature',    badgeClass: 'badge--ai',   iconBg: 'teal' },
    { icon: '📊', title: 'Code Quality Analysis', description: 'Submit your solution and receive intelligent feedback on logic, time complexity, and optimization.', badgeText: 'AI-powered',      badgeClass: 'badge--clean', iconBg: 'blue' },
    { icon: '🎯', title: 'Weakness Tracking',     description: 'Your learning profile maps exactly which topics you\'re strong in and where to focus next.',        badgeText: 'Personalized',    badgeClass: 'badge--med',   iconBg: 'gold' },
    { icon: '🔍', title: 'Integrity Detection',   description: 'Instructors can identify AI-generated or plagiarized submissions with behavioral analysis.',         badgeText: 'For instructors', badgeClass: 'badge--hard',  iconBg: 'pink' },
    { icon: '🗂️', title: 'Concept Tagging',       description: 'Every problem is tagged with algorithmic concepts so you can drill exactly the topic you want.',    badgeText: 'Organized',       badgeClass: 'badge--ai',   iconBg: 'teal' },
    { icon: '📋', title: 'Instructor Dashboard',  description: 'Monitor class-wide progress, view per-student performance, and get notified about integrity concerns.', badgeText: 'For educators', badgeClass: 'badge--clean', iconBg: 'blue' },
  ];
}
