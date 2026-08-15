import { Component, OnInit, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterModule } from '@angular/router';
import { Difficulty, Problem, Topic } from '../../core/models/problem.model';
import { ProblemService } from '../../core/services/problem.service';
import { DifficultyBadgeComponent } from '../../shared/components/difficulty-badge/difficulty-badge.component';

@Component({
  selector: 'app-problem-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink, RouterModule, DifficultyBadgeComponent],
  templateUrl: './problem-list.component.html',
  styleUrl: './problem-list.component.scss'
})
export class ProblemListComponent implements OnInit {
  protected readonly topics: TopicOption[] = [
    { label: 'All topics', value: 'all' },
    { label: 'Dynamic Programming', value: 'dynamic-programming' },
    { label: 'Graphs', value: 'graphs' },
    { label: 'Recursion', value: 'recursion' },
    { label: 'Greedy', value: 'greedy' },
    { label: 'Arrays', value: 'arrays' },
    { label: 'Sorting', value: 'sorting' },
    { label: 'Binary Search', value: 'binary-search' },
    { label: 'Trees', value: 'trees' }
  ];

  protected readonly difficulties: DifficultyOption[] = [
    { label: 'All levels', value: 'all' },
    { label: 'Easy', value: 'easy' },
    { label: 'Medium', value: 'medium' },
    { label: 'Hard', value: 'hard' }
  ];

  protected selectedTopic: TopicFilter = 'all';
  protected selectedDifficulty: DifficultyFilter = 'all';
  protected allProblems: Problem[] = [];
  protected isLoading = true;
  protected errorMessage = '';
  protected animated = false;

  // Drives the skeleton — 8 placeholder rows while data loads
  protected readonly skeletonRows = Array(8).fill(0);

  constructor(
    private readonly problemService: ProblemService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadProblems();
  }

  private loadProblems(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.animated = false;
    
    this.problemService.getAll().subscribe({
      next: (problems) => {
        this.allProblems = problems;
        this.isLoading = false;
        this.animated = true;
        this.cdr.markForCheck(); // tell Angular to re-render immediately
      },
      error: (error) => {
        console.error('Failed to load problems:', error);
        this.errorMessage = 'Failed to load problems. Please try again.';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  protected get filteredProblems(): Problem[] {
    return this.allProblems.filter(problem => {
      const matchesTopic = this.selectedTopic === 'all' || problem.topic === this.selectedTopic;
      const matchesDifficulty = this.selectedDifficulty === 'all' || problem.difficulty === this.selectedDifficulty;
      return matchesTopic && matchesDifficulty;
    });
  }

  protected get solvedTotal(): number {
    return this.filteredProblems.reduce((count, problem) => count + (problem.solvedCount ?? 0), 0);
  }

  protected trackByProblemId(_index: number, problem: Problem): string {
    return problem.id;
  }
}

type TopicFilter = Topic | 'all';
type DifficultyFilter = Difficulty | 'all';

interface TopicOption {
  label: string;
  value: TopicFilter;
}

interface DifficultyOption {
  label: string;
  value: DifficultyFilter;
}
