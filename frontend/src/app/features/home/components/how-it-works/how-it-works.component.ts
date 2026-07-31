import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

interface Step {
  num: string;
  title: string;
  desc: string;
  previewTitle: string;
  previewText: string;
  tags: string[];
  activeTag: string;
}

@Component({
  selector: 'app-how-it-works',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './how-it-works.component.html',
  styleUrl: './how-it-works.component.scss'
})
export class HowItWorksComponent {
  activeIndex = signal(0);

  steps: Step[] = [
    { num: '01', title: 'Choose a problem',    desc: 'Browse by topic, difficulty, or your weak areas. Each problem is tagged and curated.',                                         previewTitle: 'Pick your challenge',          previewText: 'Filter by topic — Dynamic Programming, Graphs, Recursion, Greedy, and more.',           tags: ['Dynamic Programming','Graphs','Recursion','Greedy'], activeTag: 'Dynamic Programming' },
    { num: '02', title: 'Attempt a solution',  desc: 'Write your code in the built-in editor. Take as long as you need.',                                                           previewTitle: 'Your editor, your pace',       previewText: 'A clean code editor with syntax highlighting. No distractions.',                        tags: ['Python','Run tests','Save draft'],                   activeTag: 'Python' },
    { num: '03', title: 'Request an AI hint',  desc: 'Stuck? Get a nudge — not an answer. The AI guides your reasoning step by step.',                                              previewTitle: 'Hints that teach, not tell',   previewText: 'The AI analyses exactly where you are and gives a targeted nudge.',                     tags: ['Step 2 of 4','Context-aware','Progressive'],        activeTag: 'Step 2 of 4' },
    { num: '04', title: 'Submit & get feedback', desc: 'Receive detailed analysis on logic, complexity, and code quality — not just a score.',                                      previewTitle: 'Real feedback, not a score',   previewText: 'See time complexity, logic review, edge cases missed, and optimization suggestions.',   tags: ['O(n log n) → O(n)','Edge case found'],              activeTag: 'O(n log n) → O(n)' },
    { num: '05', title: 'Track your progress', desc: 'Your profile updates automatically. The system recommends what to tackle next.',                                              previewTitle: 'Your learning profile evolves', previewText: 'Every submission updates your skill map. Codify knows what to surface next.',           tags: ['DP strength: 72%','Graphs: improve'],               activeTag: 'DP strength: 72%' },
  ];

  get progress(): number {
    return ((this.activeIndex() + 1) / this.steps.length) * 100;
  }

  setStep(i: number): void {
    this.activeIndex.set(i);
  }
}
