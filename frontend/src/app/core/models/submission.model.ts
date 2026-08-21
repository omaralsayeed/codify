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

export type FeedbackType = 'CodeQuality' | 'Optimization' | 'AiGenerated' | 'quality' | 'optimization' | 'anomaly';

export interface FeedbackItem {
  id: string;
  feedbackType: 'CodeQuality' | 'Optimization' | 'AiGenerated';
  message: string;
  confidence?: number | null;
  createdAt: string;
}

export interface FeedbackItemDisplay {
  type: 'quality' | 'optimization' | 'anomaly';
  message: string;
  severity: 'low' | 'medium' | 'high';
  // Optional fields for backward compatibility with template
  id?: string;
  title?: string;
  description?: string;
  lineStart?: number | null;
  lineEnd?: number | null;
}

export interface SubmissionFeedback {
  overallScore: number;
  feedbackItems: FeedbackItemDisplay[];
  summary?: string; // Optional summary field for template
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
  aiFeedback: FeedbackItemDisplay[];
  testCaseResults?: TestCaseResultDetail[];
}