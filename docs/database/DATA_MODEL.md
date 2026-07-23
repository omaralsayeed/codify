# Codify — Database Schema & Entity Reference

This file reflects the current EF Core model in the backend. It is the canonical reference for the tables and relationships that exist in the implementation today.

## Entity Overview

- `Users`
- `Problems`
- `ConceptTags`
- `ProblemTags`
- `TestCases`
- `Submissions`
- `SubmissionResults`
- `HintLogs`
- `PerformanceProfiles`
- `FeedbackRecords`

## 1. Users

| Field | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| FullName | nvarchar(200) | required |
| Email | nvarchar(320) | required, unique |
| PasswordHash | nvarchar(max) | BCrypt hash |
| Role | string | enum converted to string |
| CreatedAt | datetime2 | required |
| LastLoginAt | datetime2? | nullable |
| Username | nvarchar(100)? | optional profile field |
| Bio | nvarchar(max)? | optional profile field |
| AvatarUrl | nvarchar(500)? | optional profile field |
| Rating | decimal(10,2) | default 0 |
| SolvedProblems | int | default 0 |
| UpdatedAt | datetime2 | required |
| IsDeleted | bit | soft delete flag |

## 2. Problems

| Field | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| AuthorId | uniqueidentifier? | FK → Users, set null on delete |
| Title | nvarchar(300) | required |
| Slug | nvarchar(350) | required, unique |
| Statement | nvarchar(max) | required |
| Difficulty | string | enum converted to string |
| LanguageSupportJson | nvarchar(max) | JSON array of supported languages |
| Constraints | nvarchar(max) | required |
| TimeLimitMs | int | default 2000 |
| MemoryLimitMb | int | default 256 |
| IsPublic | bit | default true |
| IsActive | bit | active listing flag |
| AcceptedSubmissionsCount | int | default 0 |
| TotalSubmissionsCount | int | default 0 |
| CreatedAt | datetime2 | required |
| UpdatedAt | datetime2 | required |
| IsDeleted | bit | soft delete flag |

## 3. ConceptTags

| Field | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| Name | nvarchar(100) | required, unique |
| Slug | nvarchar(120) | required, unique |
| Description | nvarchar(max) | required |
| CreatedAt | datetime2 | required |
| UpdatedAt | datetime2 | required |
| IsDeleted | bit | soft delete flag |

## 4. ProblemTags

Junction table for the many-to-many relationship between Problems and ConceptTags.

| Field | Type | Notes |
|---|---|---|
| ProblemId | uniqueidentifier | PK part, FK → Problems |
| ConceptTagId | uniqueidentifier | PK part, FK → ConceptTags |

Composite primary key: `(ProblemId, ConceptTagId)`

## 5. TestCases

| Field | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| ProblemId | uniqueidentifier | FK → Problems |
| InputData | nvarchar(max) | required |
| ExpectedOutput | nvarchar(max) | required |
| IsSample | bit | shown in problem detail when true |
| VisibilityMode | string | enum converted to string |
| OrderIndex | int | default 0 |
| CreatedAt | datetime2 | required |
| UpdatedAt | datetime2 | required |
| IsDeleted | bit | soft delete flag |

## 6. Submissions

| Field | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| ProblemId | uniqueidentifier | FK → Problems |
| UserId | uniqueidentifier | FK → Users |
| Code | nvarchar(max) | required |
| Language | string | enum converted to string |
| Status | string | enum converted to string |
| SubmittedAt | datetime2 | required |
| ExecutionTimeMs | int? | nullable |
| MemoryUsedKb | int? | nullable |
| PassedTestCases | int | default 0 |
| TotalTestCases | int | default 0 |
| Score | decimal(5,2)? | nullable percentage score |
| UpdatedAt | datetime2 | required |
| IsDeleted | bit | soft delete flag |

## 7. SubmissionResults

| Field | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| SubmissionId | uniqueidentifier | FK → Submissions, unique |
| PassedTestCount | int | required |
| FailedTestCount | int | required |
| TotalTestCount | int | required |
| ErrorMessage | nvarchar(max)? | nullable |
| OutputSummary | nvarchar(max)? | nullable |

## 8. HintLogs

| Field | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| UserId | uniqueidentifier | FK → Users |
| ProblemId | uniqueidentifier | FK → Problems |
| HintLevel | int | 1 to 3 |
| RequestText | nvarchar(max)? | nullable |
| ResponseText | nvarchar(max) | required |
| CreatedAt | datetime2 | required |

The table exists in the schema, but the current runtime hint flow does not yet persist rows to it.

## 9. PerformanceProfiles

| Field | Type | Notes |
|---|---|---|
| UserId | uniqueidentifier | PK and FK → Users |
| WeakTopicsJson | nvarchar(max) | required JSON array |
| StrongTopicsJson | nvarchar(max) | required JSON array |
| SuccessRate | real | required |
| AverageAttempts | real | required |
| LastUpdatedAt | datetime2 | required |

## 10. FeedbackRecords

| Field | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| SubmissionId | uniqueidentifier | FK → Submissions |
| FeedbackType | string | enum converted to string |
| Message | nvarchar(max) | required |
| CreatedAt | datetime2 | required |

## Relationship Summary

- Users 1:1 PerformanceProfiles
- Users 1:N Submissions
- Users 1:N HintLogs
- Users 1:N Problems as author
- Problems 1:N TestCases
- Problems 1:N Submissions
- Problems 1:N HintLogs
- Problems N:M ConceptTags through ProblemTags
- Submissions 1:1 SubmissionResults
- Submissions 1:N FeedbackRecords

## Notes On Constraints

- Soft-delete filters are applied to Users, Problems, ConceptTags, TestCases, and Submissions.
- `Problem.AuthorId` is nullable and uses `ON DELETE SET NULL`.
- `SubmissionResult.SubmissionId` is unique, enforcing a one-to-one relationship.
- `ProblemTags` uses a composite primary key and cascade delete on both sides.