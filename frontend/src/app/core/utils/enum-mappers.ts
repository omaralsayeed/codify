/**
 * Enum mapping utilities for backend API integration.
 * Backend sends enums as numbers, frontend uses string literals.
 */

export type Difficulty = 'easy' | 'medium' | 'hard';
export type UserRole = 'student' | 'instructor' | 'admin';
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
 * Backend: 0 = Student, 1 = Instructor, 2 = Admin
 */
export function mapRole(value: number): UserRole {
  if (value === 0) return 'student';
  if (value === 1) return 'instructor';
  if (value === 2) return 'admin';
  return 'student';
}

/**
 * Map frontend role string to backend number
 */
export function roleToNumber(value: UserRole): number {
  if (value === 'student')    return 0;
  if (value === 'instructor') return 1;
  if (value === 'admin')      return 2;
  return 0;
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
