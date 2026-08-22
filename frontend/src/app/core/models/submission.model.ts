export type SubmissionStatus = 'Accepted' | 'WrongAnswer' | 'RuntimeError' | 'TimeLimitExceeded' | 'Pending' | 'Running';
export type SubmissionLanguage = 'Python' | 'CSharp' | 'JavaScript' | 'Java' | 'Cpp';

export interface ServiceError {
  code: string;
  status?: number;
  message: string;
}

export interface RunCodeRequest {
  problemId: string;
  code: string;
  language: SubmissionLanguage;
}

export interface TestCaseResult {
  input: string;
  expectedOutput: string;
  actualOutput: string;
  passed: boolean;
}

export interface RunCodeResponse {
  stdout: string;
  stderr: string;
  executionTimeMs: number;
  status: SubmissionStatus;
  testResults: TestCaseResult[];
}

export interface CreateSubmissionRequest {
  problemId: string;
  code: string;
  language: SubmissionLanguage;
}

export interface SubmissionResult {
  passedTestCount: number;
  failedTestCount: number;
  totalTestCount: number;
  errorMessage?: string;
  outputSummary?: string;
}

export type FeedbackType = 'quality' | 'optimization' | 'anomaly';

export interface FeedbackItem {
  id: string;
  type: FeedbackType;
  title: string;
  description: string;
  message?: string;
  lineStart: number | null;
  lineEnd: number | null;
  severity: 'low' | 'medium' | 'high';
}

export interface SubmissionFeedback {
  submissionId: string;
  overallScore: number;
  summary: string;
  feedbackItems: FeedbackItem[];
}

export interface TestCaseResultDetail {
  testCaseId: string;
  orderIndex: number;
  isSample: boolean;
  verdict: string;
  executionTimeMs: number;
  memoryUsedKb: number;
  actualOutput: string | null;
  stderr: string | null;
}

export interface SubmissionDetailResponse {
  submissionId: string;
  problemId: string;
  userId: string;
  code: string;
  language: string;
  status: SubmissionStatus;
  submittedAt: string;
  executionTimeMs: number;
  memoryUsedKb: number;
  passedTestCases: number;
  totalTestCases: number;
  score: number;
  result: SubmissionResult;
  aiFeedback: FeedbackItem[];
  testCaseResults?: TestCaseResultDetail[];
}

/** Lightweight summary returned by GET /api/problems/:id/submissions */
export interface SubmissionSummaryResponse {
  submissionId: string;
  language: string;
  status: SubmissionStatus;
  submittedAt: string;
  executionTimeMs: number | null;
  memoryUsedKb: number | null;
  passedTestCases: number;
  totalTestCases: number;
}