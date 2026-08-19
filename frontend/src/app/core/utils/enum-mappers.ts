/**
 * Enum mapping utilities for backend API integration.
 * Backend may send enums as numbers or strings (e.g. System.Text.Json JsonStringEnumConverter).
 * Frontend uses normalized lowercase string literals.
 */

export type Difficulty = 'easy' | 'medium' | 'hard';
export type UserRole = 'student' | 'instructor' | 'admin';
export type SubmissionLanguage = 'Python' | 'CSharp';

/**
 * Map backend difficulty (number or string) to frontend string
 * 0 / 'Easy' -> 'easy'
 * 1 / 'Medium' -> 'medium'
 * 2 / 'Hard' -> 'hard'
 */
export function mapDifficulty(value: number | string | null | undefined): Difficulty {
  if (value === null || value === undefined) return 'easy';
  const str = String(value).trim().toLowerCase();
  if (str === '0' || str === 'easy') return 'easy';
  if (str === '1' || str === 'medium') return 'medium';
  if (str === '2' || str === 'hard') return 'hard';
  return 'easy';
}

/**
 * Map frontend difficulty string to backend number
 */
export function difficultyToNumber(value: Difficulty | string): number {
  const str = String(value).trim().toLowerCase();
  if (str === 'easy' || str === '0') return 0;
  if (str === 'medium' || str === '1') return 1;
  if (str === 'hard' || str === '2') return 2;
  return 0;
}

/**
 * Map backend role (number or string) to frontend string
 * 0 / 'Student' -> 'student'
 * 1 / 'Instructor' -> 'instructor'
 * 2 / 'Admin' -> 'admin'
 */
export function mapRole(value: number | string | null | undefined): UserRole {
  if (value === null || value === undefined) return 'student';
  const str = String(value).trim().toLowerCase();
  if (str === '2' || str === 'admin') return 'admin';
  if (str === '1' || str === 'instructor') return 'instructor';
  if (str === '0' || str === 'student') return 'student';
  return 'student';
}

/**
 * Map frontend role string to backend number
 */
export function roleToNumber(value: UserRole | string): number {
  const str = String(value).trim().toLowerCase();
  if (str === 'admin' || str === '2') return 2;
  if (str === 'instructor' || str === '1') return 1;
  if (str === 'student' || str === '0') return 0;
  return 0;
}

/**
 * Map backend language (number or string) to frontend string
 * 0 / 'Python' -> 'Python'
 * 1 / 'CSharp' -> 'CSharp'
 */
export function mapLanguage(value: number | string | null | undefined): SubmissionLanguage {
  if (value === null || value === undefined) return 'Python';
  const str = String(value).trim().toLowerCase();
  if (str === '1' || str === 'csharp' || str === 'c#') return 'CSharp';
  return 'Python';
}

/**
 * Map frontend language string to backend number
 */
export function languageToNumber(value: SubmissionLanguage | string): number {
  const str = String(value).trim().toLowerCase();
  if (str === 'csharp' || str === 'c#' || str === '1') return 1;
  return 0;
}
