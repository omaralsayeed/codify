# Database Schema — ER Diagram Reference

> Add this file to your project docs (e.g. `docs/DATABASE.md`) so any AI assistant
> working in this codebase has full context about the database structure and relationships.

---

## Overview

This is a **competitive-programming / online-judge** platform database.  
It supports: user authentication & roles, coding problems, test-case execution, code submissions, AI-powered hints & feedback, student learning profiles, tag-based performance tracking, comments, and likes.

---

## Tables & Columns

### `roles`
Stores user roles (e.g. admin, student, instructor).

| Column | Type | Notes |
|---|---|---|
| role_id | PK | |
| name | VARCHAR | Unique role name |
| description | TEXT | |
| created_at | TIMESTAMP | |
| updated_at | TIMESTAMP | |
| is_deleted | BOOLEAN | Soft delete |

---

### `permissions`
Granular permission definitions.

| Column | Type | Notes |
|---|---|---|
| permission_id | PK | |
| name | VARCHAR | Unique permission name |
| description | TEXT | |
| created_at | TIMESTAMP | |
| updated_at | TIMESTAMP | |
| is_deleted | BOOLEAN | Soft delete |

---

### `role_permissions`
Junction table — many-to-many between `roles` and `permissions`.

| Column | Type | Notes |
|---|---|---|
| role_id | FK → roles | Composite PK |
| permission_id | FK → permissions | Composite PK |

---

### `users`
Core user accounts.

| Column | Type | Notes |
|---|---|---|
| user_id | PK | |
| username | VARCHAR | Unique |
| email | VARCHAR | Unique |
| password_hash | VARCHAR | |
| full_name | VARCHAR | |
| bio | TEXT | |
| avatar_url | VARCHAR | |
| role_id | FK → roles | User's assigned role |
| rating | NUMERIC | Competitive rating score |
| solved_problems | INT | Denormalized counter |
| created_at | TIMESTAMP | |
| updated_at | TIMESTAMP | |
| is_deleted | BOOLEAN | Soft delete |

---

### `difficulties`
Difficulty levels for problems (e.g. Easy, Medium, Hard).

| Column | Type | Notes |
|---|---|---|
| difficulty_id | PK | |
| name | VARCHAR | Unique |
| description | TEXT | |
| created_at | TIMESTAMP | |
| updated_at | TIMESTAMP | |
| is_deleted | BOOLEAN | Soft delete |

---

### `programming_languages`
Supported submission languages (e.g. Python, C++, Java).

| Column | Type | Notes |
|---|---|---|
| language_id | PK | |
| name | VARCHAR | Unique |
| extension | VARCHAR | File extension e.g. `.py` |
| is_active | BOOLEAN | Whether currently enabled |
| created_at | TIMESTAMP | |
| updated_at | TIMESTAMP | |
| is_deleted | BOOLEAN | Soft delete |

---

### `tags`
Topic tags for categorizing problems (e.g. "Dynamic Programming", "Graphs").

| Column | Type | Notes |
|---|---|---|
| tag_id | PK | |
| name | VARCHAR | Unique |
| slug | VARCHAR | URL-safe unique identifier |
| created_at | TIMESTAMP | |
| updated_at | TIMESTAMP | |
| is_deleted | BOOLEAN | Soft delete |

---

### `problems`
Coding challenges/questions on the platform.

| Column | Type | Notes |
|---|---|---|
| problem_id | PK | |
| title | VARCHAR | |
| slug | VARCHAR | Unique URL-safe identifier |
| description | TEXT | Problem statement (markdown) |
| difficulty_id | FK → difficulties | |
| author_id | FK → users | Problem creator |
| time_limit_ms | INT | Execution time limit in milliseconds |
| memory_limit_mb | INT | Memory limit in megabytes |
| is_public | BOOLEAN | Visibility flag |
| accepted_submissions_count | INT | Denormalized counter |
| total_submissions_count | INT | Denormalized counter |
| created_at | TIMESTAMP | |
| updated_at | TIMESTAMP | |
| is_deleted | BOOLEAN | Soft delete |

---

### `problem_tags`
Junction table — many-to-many between `problems` and `tags`.

| Column | Type | Notes |
|---|---|---|
| problem_id | FK → problems | Composite PK |
| tag_id | FK → tags | Composite PK |

---

### `test_cases`
Input/output pairs used to judge submissions for a problem.

| Column | Type | Notes |
|---|---|---|
| test_case_id | PK | |
| problem_id | FK → problems | |
| input | TEXT | |
| expected_output | TEXT | |
| is_sample | BOOLEAN | Whether shown to users as an example |
| order_index | INT | Display/execution order |
| created_at | TIMESTAMP | |
| updated_at | TIMESTAMP | |
| is_deleted | BOOLEAN | Soft delete |

---

### `submissions`
Code submissions made by users for problems.

| Column | Type | Notes |
|---|---|---|
| submission_id | PK | |
| user_id | FK → users | |
| problem_id | FK → problems | |
| language_id | FK → programming_languages | |
| code | TEXT | Submitted source code |
| status | VARCHAR | `pending`, `accepted`, `wrong_answer`, `runtime_error`, `time_limit_exceeded`, `compilation_error` |
| runtime_ms | INT | Actual execution time |
| memory_mb | NUMERIC | Actual memory used |
| passed_test_cases | INT | How many test cases passed |
| total_test_cases | INT | Total test cases run |
| score | NUMERIC | Score awarded |
| submitted_at | TIMESTAMP | |
| created_at | TIMESTAMP | |
| updated_at | TIMESTAMP | |
| is_deleted | BOOLEAN | Soft delete |

---

### `ai_hints`
AI-generated hints provided to users while solving a problem.

| Column | Type | Notes |
|---|---|---|
| hint_id | PK | |
| user_id | FK → users | |
| problem_id | FK → problems | |
| submission_id | FK → submissions (nullable) | Linked submission if hint was triggered by one |
| hint_text | TEXT | The hint content |
| step_number | INT | Progressive hint step (1 = most vague) |
| generated_by_ai_model | VARCHAR | AI model identifier used to generate the hint |
| created_at | TIMESTAMP | |

---

### `code_feedbacks`
AI-generated feedback on a specific code submission.

| Column | Type | Notes |
|---|---|---|
| feedback_id | PK | |
| submission_id | FK → submissions | |
| logic_score | NUMERIC | Score for code logic correctness |
| optimization_score | NUMERIC | Score for code efficiency |
| feedback_text | TEXT | Detailed AI feedback |
| ai_detected_issues | TEXT | List of issues found by AI |
| ai_generated_code_probability | NUMERIC | Probability (0–1) that code was AI-generated |
| created_at | TIMESTAMP | |

---

### `student_learning_profiles`
One-to-one profile per user tracking their overall learning progress.

| Column | Type | Notes |
|---|---|---|
| profile_id | PK | |
| user_id | FK → users (UNIQUE) | One profile per user |
| last_recommended_problem_id | FK → problems (nullable) | Last problem recommended to user |
| total_hints_used | INT | Cumulative hint count |
| created_at | TIMESTAMP | |
| updated_at | TIMESTAMP | |
| overall_strength_score | NUMERIC | Aggregate score across all tags |
| total_tags_practiced | INT | Count of unique tags attempted |

---

### `user_tag_performance`
Tracks a user's performance broken down by each problem tag.

| Column | Type | Notes |
|---|---|---|
| user_tag_performance_id | PK | |
| user_id | FK → users | |
| tag_id | FK → tags | |
| attempts_count | INT | Total problems attempted under this tag |
| solved_count | INT | Total problems solved under this tag |
| success_rate | NUMERIC | `solved_count / attempts_count` (0–1) |
| average_execution_time_ms | NUMERIC | Avg runtime for accepted submissions |
| last_practiced_at | TIMESTAMP | |
| strength_score | NUMERIC | Composite skill score for this tag |
| level | VARCHAR | e.g. `beginner`, `intermediate`, `advanced` |
| created_at | TIMESTAMP | |
| updated_at | TIMESTAMP | |

**Unique constraint:** `(user_id, tag_id)`

---

### `likes`
Polymorphic likes — a user can like problems, comments, submissions, etc.

| Column | Type | Notes |
|---|---|---|
| like_id | PK | |
| user_id | FK → users | |
| likeable_type | VARCHAR | Entity type: `'problem'`, `'comment'`, `'submission'` |
| likeable_id | INT | PK of the liked entity |
| created_at | TIMESTAMP | |

**Unique constraint:** `(user_id, likeable_type, likeable_id)`

---

### `comments`
Polymorphic comments with support for nested replies (self-referencing).

| Column | Type | Notes |
|---|---|---|
| comment_id | PK | |
| user_id | FK → users | |
| commentable_type | VARCHAR | Entity type: `'problem'`, `'submission'` |
| commentable_id | INT | PK of the commented entity |
| parent_id | FK → comments (nullable) | Parent comment for nested replies |
| content | TEXT | Comment body |
| created_at | TIMESTAMP | |
| updated_at | TIMESTAMP | |
| is_deleted | BOOLEAN | Soft delete |

---

## Entity Relationships Summary

```
roles ──< role_permissions >── permissions
roles ──< users
users ──< submissions >── problems
users ──< ai_hints >── problems
users ──< comments (polymorphic)
users ──< likes (polymorphic)
users ──1 student_learning_profiles
users ──< user_tag_performance >── tags
problems ──< problem_tags >── tags
problems ──< test_cases
problems ──< submissions ──< code_feedbacks
submissions ──< ai_hints
difficulties ──< problems
programming_languages ──< submissions
comments ──< comments  (self-referencing, for nested replies)
```

---

## Design Patterns Used

1. **Soft Deletes** — Most tables have `is_deleted BOOLEAN`. Always filter with `WHERE is_deleted = FALSE` unless fetching deleted records intentionally.

2. **Polymorphic Associations** — `likes` and `comments` use `(likeable_type / commentable_type, likeable_id / commentable_id)` pairs to attach to multiple entity types without separate junction tables.

3. **Denormalized Counters** — `problems.accepted_submissions_count`, `problems.total_submissions_count`, and `users.solved_problems` are maintained as counters to avoid expensive COUNT queries. Update them via triggers or application logic on submission events.

4. **One-to-One** — `student_learning_profiles.user_id` has a UNIQUE constraint, enforcing one profile per user.

5. **Many-to-Many Junctions** — `role_permissions` and `problem_tags` are explicit junction tables with composite PKs.

6. **Timestamps** — All tables have `created_at`. Mutable tables also have `updated_at`. Update `updated_at` via application logic or a DB trigger on every UPDATE.

---

## Common Query Patterns

```sql
-- Get all problems with difficulty and tags
SELECT p.*, d.name AS difficulty, array_agg(t.name) AS tags
FROM problems p
LEFT JOIN difficulties d ON d.difficulty_id = p.difficulty_id
LEFT JOIN problem_tags pt ON pt.problem_id = p.problem_id
LEFT JOIN tags t ON t.tag_id = pt.tag_id
WHERE p.is_deleted = FALSE AND p.is_public = TRUE
GROUP BY p.problem_id, d.name;

-- Get a user's accepted submissions
SELECT s.*, pl.name AS language
FROM submissions s
JOIN programming_languages pl ON pl.language_id = s.language_id
WHERE s.user_id = :user_id AND s.status = 'accepted' AND s.is_deleted = FALSE;

-- Get tag performance for a user
SELECT t.name, utp.solved_count, utp.attempts_count, utp.strength_score, utp.level
FROM user_tag_performance utp
JOIN tags t ON t.tag_id = utp.tag_id
WHERE utp.user_id = :user_id
ORDER BY utp.strength_score DESC;

-- Get AI hints for a user on a problem
SELECT * FROM ai_hints
WHERE user_id = :user_id AND problem_id = :problem_id
ORDER BY step_number ASC;

-- Get comments for a problem (top-level only, no replies)
SELECT c.*, u.username, u.avatar_url
FROM comments c
JOIN users u ON u.user_id = c.user_id
WHERE c.commentable_type = 'problem'
  AND c.commentable_id = :problem_id
  AND c.parent_id IS NULL
  AND c.is_deleted = FALSE
ORDER BY c.created_at DESC;
```
