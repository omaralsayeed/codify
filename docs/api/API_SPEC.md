# API_SPEC.md
# Codify — API Specification

> Base URL: `https://localhost:5001/api` (development)
> All requests and responses use `application/json`.
> All protected routes require `Authorization: Bearer <token>` header.

---

## Standard Response Envelope

Every API response uses this shape:

```json
// Success
{
  "success": true,
  "data": { ... },
  "message": "Optional message"
}

// Error
{
  "success": false,
  "errorCode": "VALIDATION_ERROR",
  "message": "Human-readable error",
  "details": { "field": "reason" }
}
```

**Standard Error Codes:**

| Code | Meaning |
|------|---------|
| `UNAUTHORIZED` | Missing or invalid JWT |
| `FORBIDDEN` | Valid JWT but wrong role |
| `NOT_FOUND` | Resource doesn't exist |
| `VALIDATION_ERROR` | Request body failed validation |
| `EXECUTION_TIMEOUT` | Code ran longer than allowed |
| `AI_UNAVAILABLE` | LLM or vector DB is down |
| `UNSUPPORTED_LANGUAGE` | Submitted language not supported |

---

## Auth Endpoints

### POST /auth/register
Register a new user.

**Access:** Public  
**Body:**
```json
{
  "fullName": "Ahmed Hassan",
  "email": "ahmed@example.com",
  "password": "min8chars",
  "role": "Student"
}
```
**Response 201:**
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

---

### POST /auth/login
Authenticate and receive a JWT.

**Access:** Public  
**Body:**
```json
{
  "email": "ahmed@example.com",
  "password": "min8chars"
}
```
**Response 200:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGci...",
    "expiresAt": "2025-01-01T12:00:00Z",
    "user": {
      "userId": "uuid",
      "fullName": "Ahmed Hassan",
      "role": "Student"
    }
  }
}
```

---

### POST /auth/logout
Invalidate the current session (client-side token removal).

**Access:** Authenticated  
**Response 200:** `{ "success": true }`

---

### GET /auth/me
Get the currently authenticated user's profile.

**Access:** Authenticated  
**Response 200:**
```json
{
  "success": true,
  "data": {
    "userId": "uuid",
    "fullName": "Ahmed Hassan",
    "email": "ahmed@example.com",
    "role": "Student",
    "createdAt": "2025-01-01T00:00:00Z"
  }
}
```

---

## Problem Endpoints

### GET /problems
List all active problems. Students see active only; instructors see all.

**Access:** Authenticated  
**Query params:**
- `difficulty` — `Easy`, `Medium`, `Hard`
- `tag` — concept tag name
- `search` — keyword in title
- `page` — default 1
- `pageSize` — default 20

**Response 200:**
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
    "totalCount": 42,
    "page": 1,
    "pageSize": 20
  }
}
```

---

### GET /problems/{id}
Get full problem detail including sample test cases.

**Access:** Authenticated  
**Response 200:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "title": "Two Sum",
    "statement": "Given an array of integers...",
    "difficulty": "Easy",
    "constraints": "1 <= nums.length <= 10^4",
    "languageSupport": ["Python", "CSharp"],
    "tags": ["Arrays & Hashing"],
    "sampleTestCases": [
      { "input": "[2,7,11,15]\n9", "expectedOutput": "[0,1]" }
    ]
  }
}
```

---

### POST /problems
Create a new problem.

**Access:** Instructor only  
**Body:**
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
      "inputData": "[2,7,11,15]\n9",
      "expectedOutput": "[0,1]",
      "isSample": true,
      "visibilityMode": "Public"
    }
  ]
}
```
**Response 201:** Full problem object

---

### PUT /problems/{id}
Update an existing problem.

**Access:** Instructor only  
**Body:** Same shape as POST, all fields optional.  
**Response 200:** Updated problem object

---

### DELETE /problems/{id}
Soft-delete a problem (sets `IsActive = false`).

**Access:** Instructor only  
**Response 200:** `{ "success": true }`

---

## Submission Endpoints

### POST /submissions
Submit code for a problem.

**Access:** Student only  
**Body:**
```json
{
  "problemId": "uuid",
  "code": "def two_sum(nums, target):\n    ...",
  "language": "Python"
}
```
**Response 202:** (Accepted — execution is async)
```json
{
  "success": true,
  "data": {
    "submissionId": "uuid",
    "status": "Pending"
  }
}
```

---

### GET /submissions/{id}
Get the result of a specific submission.

**Access:** Owner student or Instructor  
**Response 200:**
```json
{
  "success": true,
  "data": {
    "submissionId": "uuid",
    "status": "Accepted",
    "executionTimeMs": 42,
    "memoryUsedKb": 8192,
    "result": {
      "passedTestCount": 10,
      "failedTestCount": 0,
      "totalTestCount": 10
    },
    "aiFeedback": [
      {
        "type": "CodeQuality",
        "message": "Consider using a hash map for O(n) lookup..."
      }
    ]
  }
}
```

---

### GET /problems/{id}/submissions
List all submissions by the current student for a problem.

**Access:** Authenticated (students see own, instructors see all)  
**Response 200:** Array of submission summaries

---

## AI / Hint Endpoints

### POST /ai/hints
Request a hint for the current problem.

**Access:** Student only  
**Body:**
```json
{
  "problemId": "uuid"
}
```
**Response 200:**
```json
{
  "success": true,
  "data": {
    "hintLevel": 1,
    "hintText": "Think about what data structure allows O(1) lookups...",
    "hasMoreHints": true
  }
}
```

**Notes:**
- Each call increments the hint level for this user+problem.
- At level 3, `hasMoreHints` is false.
- Rate limit: 10 hint requests per student per hour.

---

### GET /ai/hints/history
Get all hints the current student has received for a problem.

**Access:** Student only  
**Query params:** `problemId` (required)  
**Response 200:** Array of hint log entries

---

## Analytics Endpoints

### GET /analytics/student/{id}
Get a student's performance profile.

**Access:** The student themselves, or any Instructor  
**Response 200:**
```json
{
  "success": true,
  "data": {
    "userId": "uuid",
    "successRate": 0.62,
    "averageAttempts": 2.4,
    "weakTopics": ["Dynamic Programming", "Graphs"],
    "strongTopics": ["Arrays & Hashing", "Two Pointers"],
    "recentActivity": [...]
  }
}
```

---

### GET /analytics/topic/{topicId}
Get aggregate stats for a concept tag.

**Access:** Instructor only  
**Response 200:** Topic-level performance summary across all students

---

### GET /analytics/instructor/overview
Get a full cohort overview for the instructor's students.

**Access:** Instructor only  
**Response 200:**
```json
{
  "success": true,
  "data": {
    "totalStudents": 45,
    "averageSuccessRate": 0.54,
    "mostChallengingTopic": "Dynamic Programming",
    "integrityFlags": 3,
    "topPerformers": [...],
    "studentsNeedingAttention": [...]
  }
}
```

---

## Execution Endpoint

### POST /execution/run
Run code against sample test cases only (not full evaluation — used for "Run" before submitting).

**Access:** Student only  
**Body:**
```json
{
  "problemId": "uuid",
  "code": "def solution():\n    ...",
  "language": "Python"
}
```
**Response 200:**
```json
{
  "success": true,
  "data": {
    "stdout": "Output here",
    "stderr": "",
    "executionTimeMs": 18,
    "status": "Accepted"
  }
}
```

---

## Concept Tag Endpoints

### GET /tags
List all concept tags.

**Access:** Authenticated  
**Response 200:** Array of `{ id, name, description }`

---

## Rate Limits

| Endpoint | Limit |
|----------|-------|
| POST /ai/hints | 10/hour per user |
| POST /submissions | 30/hour per user |
| POST /execution/run | 60/hour per user |
