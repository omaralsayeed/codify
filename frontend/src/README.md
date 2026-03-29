# Frontend — Angular Application

## Structure

```
src/
├── app/
│   ├── auth/                    # Login, Register
│   │   ├── login/
│   │   └── register/
│   ├── student/                 # Student-facing pages
│   │   ├── problem-list/        # Browse problems
│   │   ├── problem-detail/      # Solve + hint panel + editor
│   │   ├── submission-result/   # View submission result + AI feedback
│   │   └── progress/            # Personal performance dashboard
│   ├── instructor/              # Instructor-facing pages
│   │   ├── dashboard/           # Overview metrics
│   │   ├── students/            # Student list + drill-down
│   │   └── topics/              # Topic analytics
│   ├── shared/
│   │   ├── components/          # Reusable: LoadingSpinner, ErrorMessage, Badge
│   │   ├── services/            # API service wrappers
│   │   └── models/              # TypeScript interfaces (match API responses)
│   └── core/
│       ├── guards/              # AuthGuard, RoleGuard
│       └── interceptors/        # JwtInterceptor, ErrorInterceptor
├── environments/
│   ├── environment.ts           # Production (empty apiUrl)
│   └── environment.development.ts  # Local dev (localhost:5001)
└── assets/
```

## Key Components

### Problem Detail Page
The most complex page. Contains:
- Markdown problem statement renderer
- Code editor (Monaco or CodeMirror)
- "Run" button (sample tests only)
- "Submit" button (full evaluation)
- Hint panel (collapsible, shows hints in order)
- Submission result panel

### Hint Panel
- Displays hints in order (level 1 → 2 → 3)
- "Get Hint" button triggers POST /ai/hints
- Shows loading state while waiting for AI
- Disables button after level 3

### Submission Result Panel
- Shows pass/fail counts
- Color-coded status badge (green Accepted, red WrongAnswer, etc.)
- Expandable AI feedback section

## Models (match API response shapes)

```typescript
// shared/models/problem.model.ts
export interface Problem {
  id: string;
  title: string;
  statement: string;
  difficulty: 'Easy' | 'Medium' | 'Hard';
  tags: string[];
  constraints: string;
  sampleTestCases: TestCase[];
}

// shared/models/submission.model.ts
export interface Submission {
  submissionId: string;
  status: SubmissionStatus;
  executionTimeMs: number;
  result: SubmissionResult;
  aiFeedback: FeedbackRecord[];
}

export type SubmissionStatus =
  | 'Pending' | 'Running' | 'Accepted'
  | 'WrongAnswer' | 'RuntimeError'
  | 'TimeLimitExceeded' | 'CompileError';
```

## Service Pattern

```typescript
// shared/services/submission.service.ts
@Injectable({ providedIn: 'root' })
export class SubmissionService {
  constructor(private http: HttpClient) {}

  submit(request: CreateSubmissionRequest): Observable<{ submissionId: string }> {
    return this.http.post<ApiResponse<{ submissionId: string }>>(
      `${environment.apiUrl}/submissions`, request
    ).pipe(map(r => r.data));
  }

  getById(id: string): Observable<Submission> {
    return this.http.get<ApiResponse<Submission>>(
      `${environment.apiUrl}/submissions/${id}`
    ).pipe(map(r => r.data));
  }
}
```

## See Also

- [API_SPEC.md](../../docs/api/API_SPEC.md) for all endpoint shapes
- [CONVENTIONS.md](../../CONVENTIONS.md) for naming and style rules
- [ENV_SETUP.md](../../ENV_SETUP.md) for running locally
