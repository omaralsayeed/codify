export type Difficulty = 'easy' | 'medium' | 'hard';
export type Topic = 'dynamic-programming' | 'graphs' | 'recursion' | 'greedy' | 'arrays' | 'sorting' | 'binary-search' | 'trees';

export interface Problem {
  id: string;
  title: string;
  difficulty: Difficulty;
  topic: Topic;
  topicLabel: string;
  solvedCount?: number;
}
