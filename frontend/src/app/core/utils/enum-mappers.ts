/**
 * Enum mapping utilities for backend API integration.
 * Backend sends enums as numbers, frontend uses string literals.
 */

export type Difficulty = 'easy' | 'medium' | 'hard';
export type UserRole = 'student' | 'instructor';
export type SubmissionLanguage = 'Python' | 'CSharp';

/**
 * Map backend difficulty number to frontend string
 * Backend: 0 = Easy, 1 = Medium, 2 = Hard
 */
export function mapDifficulty(value: number): Difficulty {
  const map: Record<number, Difficulty> = {
    0: 'easy',
    1: 'medium',
    2: 'hard'
  };
  return map[value] ?? 'easy';
}

/**
 * Map frontend difficulty string to backend number
 */
export function difficultyToNumber(value: Difficulty): number {
  const map: Record<Difficulty, number> = {
    easy: 0,
    medium: 1,
    hard: 2
  };
  return map[value];
}

/**
 * Map backend role number to frontend string
 * Backend: 0 = Student, 1 = Instructor
 */
export function mapRole(value: number): UserRole {
  return value === 0 ? 'student' : 'instructor';
}

/**
 * Map frontend role string to backend number
 */
export function roleToNumber(value: UserRole): number {
  return value === 'student' ? 0 : 1;
}

/**
 * Map backend language number to frontend string
 * Backend: 0 = Python, 1 = CSharp
 */
export function mapLanguage(value: number): SubmissionLanguage {
  return value === 0 ? 'Python' : 'CSharp';
}

/**
 * Map frontend language string to backend number
 */
export function languageToNumber(value: SubmissionLanguage): number {
  return value === 'Python' ? 0 : 1;
}
