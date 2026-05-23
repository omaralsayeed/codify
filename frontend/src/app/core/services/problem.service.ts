import { Injectable } from '@angular/core';
import { Problem } from '../models/problem.model';

@Injectable({ providedIn: 'root' })
export class ProblemService {
  private readonly problems: Problem[] = [
    { id: '1', title: 'Coin Change II', difficulty: 'medium', topic: 'dynamic-programming', topicLabel: 'Dynamic Programming', solvedCount: 13210 },
    { id: '2', title: 'Number of Islands', difficulty: 'hard', topic: 'graphs', topicLabel: 'Graphs · BFS', solvedCount: 9210 },
    { id: '3', title: 'Climbing Stairs', difficulty: 'easy', topic: 'recursion', topicLabel: 'Recursion · Memo', solvedCount: 22104 },
    { id: '4', title: 'Merge Intervals', difficulty: 'medium', topic: 'sorting', topicLabel: 'Sorting · Intervals', solvedCount: 16884 },
    { id: '5', title: 'Two Sum', difficulty: 'easy', topic: 'arrays', topicLabel: 'Arrays · Hash Map', solvedCount: 36045 },
    { id: '6', title: 'Binary Search', difficulty: 'easy', topic: 'binary-search', topicLabel: 'Binary Search', solvedCount: 28791 },
    { id: '7', title: 'Course Schedule', difficulty: 'medium', topic: 'graphs', topicLabel: 'Graphs · Topological Sort', solvedCount: 11762 },
    { id: '8', title: 'Maximum Subarray', difficulty: 'medium', topic: 'greedy', topicLabel: 'Greedy · Kadane', solvedCount: 19503 },
    { id: '9', title: 'Lowest Common Ancestor', difficulty: 'hard', topic: 'trees', topicLabel: 'Trees · DFS', solvedCount: 8540 }
  ];

  getAll(): Problem[] {
    return this.problems;
  }

  getRecommended(): Problem[] {
    return this.problems.slice(0, 3);
  }
}
