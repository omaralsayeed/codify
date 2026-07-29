# Codify — API Specification

Base URL: `https://localhost:5001/api` in development. All endpoints use JSON. Protected endpoints require `Authorization: Bearer <token>`.

## Response Envelope

The API returns the following envelope:

```json
{
  "success": true,
  "data": { }
}
```

Errors are returned through the same envelope:

```json
{
  "success": false,
  "errorCode": "VALIDATION_ERROR",
  "message": "Human-readable error"
}
```

## Error Codes

| Code | Source |
|---|---|
| `NOT_FOUND` | `NotFoundException` |
| `FORBIDDEN` | `ForbiddenException` |
| `VALIDATION_ERROR` | `ValidationException` or model validation |
| `INTERNAL_ERROR` | Unhandled exception |

## Auth Module

### POST /auth/register

Public registration endpoint.

Request body:

```json
{
  "fullName": "Ahmed Hassan",
  "email": "ahmed@example.com",
  "password": "min8chars",
  "role": "Student"
}
```

Validation:

- `fullName` required, max 200 characters
- `email` required, valid email, max 320 characters
- `password` required, minimum 8 characters
- `role` required and must be a valid `UserRole`

Success response: `201 Created`

```json
{
  "success": true,
  "data": {
    "userId": "uuid",
    "email": "ahmed@example.com",
    "role": "Student"
  }
}
```

### POST /auth/login

Public login endpoint.

Request body:

```json
{
  "email": "ahmed@example.com",
  "password": "min8chars"
}
```

Success response: `200 OK`

```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOi...",
    "expiresAt": "2026-07-23T14:00:00Z",
    "user": {
      "userId": "uuid",
      "fullName": "Ahmed Hassan",
      "role": "Student"
    }
  }
}
```

### POST /auth/logout

Authenticated endpoint. Logout is stateless; the server does not revoke tokens.

Success response: `200 OK`

### GET /auth/me

Authenticated endpoint that returns the current user profile.

Success response: `200 OK`

```json
{
  "success": true,
  "data": {
    "userId": "uuid",
    "fullName": "Ahmed Hassan",
    "email": "ahmed@example.com",
    "role": "Student",
    "createdAt": "2026-07-23T00:00:00Z"
  }
}
```

## Problems Module

All problem routes require authentication.

### GET /problems

Returns a paged list of problems.

Query parameters:

- `difficulty` optional `Easy | Medium | Hard`
- `tag` optional concept tag name
- `search` optional title search string
- `page` default `1`
- `pageSize` default `20`

Success response:

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "uuid",
        "title": "Two Sum",
        "difficulty": "Easy",
        "tags": ["Arrays & Hashing"],
        "isActive": true
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20
  }
}
```

### GET /problems/{id}

Returns the full problem detail including sample test cases.

Path parameters:

- `id` required GUID

Success response:

```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "title": "Two Sum",
    "slug": "two-sum",
    "statement": "Given an array of integers...",
    "difficulty": "Easy",
    "constraints": "1 <= nums.length <= 10^4",
    "languageSupport": ["Python", "CSharp"],
    "tags": ["Arrays & Hashing"],
    "sampleTestCases": [
      {
        "input": "2 7 11 15\n9",
        "expectedOutput": "0 1"
      }
    ],
    "isActive": true,
    "isPublic": true,
    "timeLimitMs": 2000,
    "memoryLimitMb": 256,
    "acceptedSubmissionsCount": 0,
    "totalSubmissionsCount": 0
  }
}
```

### POST /problems

Instructor-only endpoint that creates a problem.

Request body:

```json
{
  "title": "Two Sum",
  "statement": "Given an array...",
  "difficulty": "Easy",
  "constraints": "...",
  "languageSupport": ["Python", "CSharp"],
  "tagIds": ["uuid1", "uuid2"],
  "testCases": [
    {
      "inputData": "2 7 11 15\n9",
      "expectedOutput": "0 1",
      "isSample": true,
      "visibilityMode": "Public"
    }
  ],
  "timeLimitMs": 2000,
  "memoryLimitMb": 256
}
```

Validation:

- `title` required, max 300 characters
- `statement` required
- `difficulty` required
- `timeLimitMs` must be between 100 and 30000
- `memoryLimitMb` must be between 16 and 1024

Success response: `201 Created`

### PUT /problems/{id}

Instructor-only endpoint that updates a problem. All fields are optional.

Request body fields:

- `title`
- `statement`
- `difficulty`
- `constraints`
- `languageSupport`
- `tagIds`

Success response: `200 OK`

### DELETE /problems/{id}

Instructor-only soft delete. The problem is marked deleted and hidden by the query filter.

Success response: `200 OK`

### GET /problems/{id}/submissions

Returns submissions for a problem. Students see their own submissions; instructors see all submissions for the problem.

Success response: `200 OK`

## Submissions Module

### POST /submissions

Student-only endpoint that creates a submission.

Request body:

```json
{
  "problemId": "uuid",
  "code": "def two_sum(nums, target):\n    ...",
  "language": "Python"
}
```

Validation:

- `problemId` required
- `code` required
- `language` required and must be a valid `SubmissionLanguage`

Success response: `202 Accepted`

```json
{
  "success": true,
  "data": {
    "submissionId": "uuid",
    "problemId": "uuid",
    "userId": "uuid",
    "status": "Pending"
  }
}
```

### GET /submissions/{id}

Returns a submission and its result details.

Permissions:

- owner student
- instructor

Success response:

```json
{
  "success": true,
  "data": {
    "submissionId": "uuid",
    "problemId": "uuid",
    "userId": "uuid",
    "code": "...",
    "language": "Python",
    "status": "Accepted",
    "submittedAt": "2026-07-23T00:00:00Z",
    "executionTimeMs": 42,
    "memoryUsedKb": 8192,
    "passedTestCases": 10,
    "totalTestCases": 10,
    "score": 100,
    "result": {
      "passedTestCount": 10,
      "failedTestCount": 0,
      "totalTestCount": 10,
      "errorMessage": null,
      "outputSummary": null
    },
    "aiFeedback": [
      {
        "type": "Optimization",
        "message": "Consider using a hash map for O(n) lookup."
      }
    ]
  }
}
```

## Execution Module

### POST /execution/run

Student-only endpoint for the pre-submit run flow. The current implementation only returns sample-case placeholders.

Request body:

```json
{
  "problemId": "uuid",
  "code": "def solution():\n    ...",
  "language": "Python"
}
```

Success response: `200 OK`

```json
{
  "success": true,
  "data": {
    "stdout": "",
    "stderr": "",
    "executionTimeMs": 0,
    "status": "Pending",
    "testResults": [
      {
        "input": "...",
        "expectedOutput": "...",
        "actualOutput": "",
        "passed": false
      }
    ]
  }
}
```

## AI Module

### POST /ai/hints

Student-only active hint endpoint implemented by `AiController`.

Request body:

```json
{
  "problemId": "uuid",
  "studentCode": "def solution():\n    ...",
  "hintLevel": 1,
  "previousHints": ["Try thinking about lookups."],
  "attemptCount": 2,
  "lastSubmissionStatus": "WrongAnswer"
}
```

Validation:

- `problemId` required
- `studentCode` required
- `hintLevel` must be between 1 and 3

Success response:

```json
{
  "success": true,
  "data": {
    "hintText": "Think about which data structure gives you constant-time lookups.",
    "hintLevel": 1,
    "followUpQuestion": "What would you store as the key and value?",
    "hasMoreHints": true
  }
}
```

Notes:

- Rate limit: 10 requests per hour per user.
- The current runtime path does not persist hint logs yet.

### POST /api/ai/hints

`HintsController` also declares this route, but it currently returns `501 Not Implemented` and conflicts with the active endpoint above. Treat it as a placeholder until the duplicate controller is removed.

### GET /api/ai/hints/history

Declared in `HintsController` and currently returns `501 Not Implemented`.

## Tags Module

### GET /tags

Returns all non-deleted concept tags.

### GET /tags/{id}

Returns a single concept tag.

### POST /tags

Instructor-only endpoint that creates a concept tag.

Request body:

```json
{
  "name": "Dynamic Programming",
  "description": "Breaks a problem into overlapping subproblems."
}
```

### PUT /tags/{id}

Instructor-only endpoint that updates a concept tag.

### DELETE /tags/{id}

Instructor-only soft delete.

### GET /tags/problems/{problemId}

Returns all tags applied to a problem.

### POST /tags/problems/{problemId}/{conceptTagId}

Instructor-only endpoint that adds a tag to a problem.

### DELETE /tags/problems/{problemId}/{conceptTagId}

Instructor-only endpoint that removes a tag from a problem.

## Rate Limits

| Endpoint | Limit |
|---|---|
| POST /submissions | 30/hour per user |
| POST /execution/run | 60/hour per user |
| POST /ai/hints | 10/hour per user |