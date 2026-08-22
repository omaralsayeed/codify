-- ============================================================================
-- Codify Seed Data Script
-- Purpose: Populate database with realistic test data for development/testing
-- ============================================================================

-- Clear existing data (in correct order to respect foreign keys)
DELETE FROM [TestCaseResults];
DELETE FROM [FeedbackRecords];
DELETE FROM [HintLogs];
DELETE FROM [SubmissionResults];
DELETE FROM [Submissions];
DELETE FROM [TestCases];
DELETE FROM [ProblemTags];
DELETE FROM [ConceptTags];
DELETE FROM [ContestParticipants];
DELETE FROM [ContestProblems];
DELETE FROM [Contests];
DELETE FROM [InstructorStudents];
DELETE FROM [PerformanceProfiles];
DELETE FROM [Problems];
DELETE FROM [Users];

-- ============================================================================
-- USERS
-- ============================================================================

-- Admin User
INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [Status], [Organization], [Username], [Bio], [AvatarUrl], [Rating], [SolvedProblems], [CreatedAt], [LastLoginAt], [UpdatedAt], [IsDeleted], [ReviewedBy], [ReviewedAt])
VALUES 
('11111111-1111-1111-1111-111111111111', 'Admin User', 'admin@codify.com', '$2a$11$hashedpassword123456789012345678901234567890123', 0, 1, 'Codify Platform', 'admin', 'Platform administrator', NULL, 0, 0, '2024-01-01 10:00:00', '2024-12-01 09:00:00', '2024-12-01 09:00:00', 0, NULL, NULL);

-- Instructor 1 (Active)
INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [Status], [Organization], [Username], [Bio], [AvatarUrl], [Rating], [SolvedProblems], [CreatedAt], [LastLoginAt], [UpdatedAt], [IsDeleted], [ReviewedBy], [ReviewedAt])
VALUES 
('22222222-2222-2222-2222-222222222222', 'Dr. Sarah Johnson', 'sarah.johnson@university.edu', '$2a$11$hashedpassword123456789012345678901234567890123', 1, 1, 'State University', 'dr_sarah', 'Computer Science Professor specializing in Algorithms', NULL, 0, 0, '2024-01-15 14:30:00', '2024-12-01 08:15:00', '2024-12-01 08:15:00', 0, '11111111-1111-1111-1111-111111111111', '2024-01-16 10:00:00');

-- Instructor 2 (Pending Approval)
INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [Status], [Organization], [Username], [Bio], [AvatarUrl], [Rating], [SolvedProblems], [CreatedAt], [LastLoginAt], [UpdatedAt], [IsDeleted], [ReviewedBy], [ReviewedAt])
VALUES 
('33333333-3333-3333-3333-333333333333', 'Prof. Michael Chen', 'michael.chen@tech.edu', '$2a$11$hashedpassword123456789012345678901234567890123', 1, 0, 'Tech Institute', 'prof_chen', 'Teaching Data Structures and Algorithms', NULL, 0, 0, '2024-11-25 16:45:00', NULL, '2024-11-25 16:45:00', 0, NULL, NULL);

-- Student 1 (Active, Beginner)
INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [Status], [Organization], [Username], [Bio], [AvatarUrl], [Rating], [SolvedProblems], [CreatedAt], [LastLoginAt], [UpdatedAt], [IsDeleted], [ReviewedBy], [ReviewedAt])
VALUES 
('44444444-4444-4444-4444-444444444444', 'Alice Williams', 'alice.williams@student.edu', '$2a$11$hashedpassword123456789012345678901234567890123', 2, 1, NULL, 'alice_codes', 'CS sophomore learning algorithms', NULL, 1250, 8, '2024-02-01 09:00:00', '2024-12-01 10:30:00', '2024-12-01 10:30:00', 0, NULL, NULL);

-- Student 2 (Active, Intermediate)
INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [Status], [Organization], [Username], [Bio], [AvatarUrl], [Rating], [SolvedProblems], [CreatedAt], [LastLoginAt], [UpdatedAt], [IsDeleted], [ReviewedBy], [ReviewedAt])
VALUES 
('55555555-5555-5555-5555-555555555555', 'Bob Martinez', 'bob.martinez@student.edu', '$2a$11$hashedpassword123456789012345678901234567890123', 2, 1, NULL, 'bob_dev', 'Preparing for tech interviews', NULL, 1680, 15, '2024-01-20 11:15:00', '2024-12-01 11:00:00', '2024-12-01 11:00:00', 0, NULL, NULL);

-- Student 3 (Active, Advanced)
INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [Status], [Organization], [Username], [Bio], [AvatarUrl], [Rating], [SolvedProblems], [CreatedAt], [LastLoginAt], [UpdatedAt], [IsDeleted], [ReviewedBy], [ReviewedAt])
VALUES 
('66666666-6666-6666-6666-666666666666', 'Charlie Davis', 'charlie.davis@student.edu', '$2a$11$hashedpassword123456789012345678901234567890123', 2, 1, NULL, 'charlie_ace', 'Competitive programmer, ICPC participant', NULL, 2100, 45, '2024-01-10 08:00:00', '2024-12-01 12:00:00', '2024-12-01 12:00:00', 0, NULL, NULL);

-- ============================================================================
-- INSTRUCTOR-STUDENT RELATIONSHIPS
-- ============================================================================

INSERT INTO [InstructorStudents] ([InstructorId], [StudentId], [CreatedAt])
VALUES 
('22222222-2222-2222-2222-222222222222', '44444444-4444-4444-4444-444444444444', '2024-02-01 10:00:00'),
('22222222-2222-2222-2222-222222222222', '55555555-5555-5555-5555-555555555555', '2024-02-01 10:00:00'),
('22222222-2222-2222-2222-222222222222', '66666666-6666-6666-6666-666666666666', '2024-02-01 10:00:00');

-- ============================================================================
-- PERFORMANCE PROFILES
-- ============================================================================

INSERT INTO [PerformanceProfiles] ([Id], [UserId], [TotalAttempts], [SuccessfulSubmissions], [AverageTime], [StrongConcepts], [WeakConcepts], [LastUpdated])
VALUES 
('77777777-7777-7777-7777-777777777777', '44444444-4444-4444-4444-444444444444', 12, 8, 45.5, 'Arrays,Strings', 'Dynamic Programming,Graphs', '2024-12-01 10:30:00'),
('88888888-8888-8888-8888-888888888888', '55555555-5555-5555-5555-555555555555', 25, 15, 32.8, 'Arrays,Hash Tables,Two Pointers', 'Backtracking,Bit Manipulation', '2024-12-01 11:00:00'),
('99999999-9999-9999-9999-999999999999', '66666666-6666-6666-6666-666666666666', 67, 45, 18.2, 'Dynamic Programming,Graphs,Trees,Binary Search', 'Advanced Math', '2024-12-01 12:00:00');

-- ============================================================================
-- CONCEPT TAGS
-- ============================================================================

INSERT INTO [ConceptTags] ([Id], [Name], [Description], [CreatedAt])
VALUES 
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Arrays & Hashing', 'Problems involving array manipulation and hash table usage', '2024-01-01 12:00:00'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Two Pointers', 'Problems using two-pointer technique', '2024-01-01 12:00:00'),
('cccccccc-cccc-cccc-cccc-cccccccccccc', 'Sliding Window', 'Problems involving sliding window technique', '2024-01-01 12:00:00'),
('dddddddd-dddd-dddd-dddd-dddddddddddd', 'Binary Search', 'Problems requiring binary search algorithm', '2024-01-01 12:00:00'),
('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 'Dynamic Programming', 'Problems solved using dynamic programming', '2024-01-01 12:00:00');

-- ============================================================================
-- PROBLEMS
-- ============================================================================

-- Problem 1: Two Sum (Easy)
INSERT INTO [Problems] ([Id], [Title], [Slug], [Statement], [Difficulty], [LanguageSupportJson], [Constraints], [AuthorId], [TimeLimitMs], [MemoryLimitMb], [IsPublic], [IsActive], [AcceptedSubmissionsCount], [TotalSubmissionsCount], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES 
('8f79d1fc-0492-4164-a48b-313e57cdc216', 'Two Sum', 'two-sum', 
'Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.

You may assume that each input would have exactly one solution, and you may not use the same element twice.

You can return the answer in any order.

Example 1:
Input: nums = [2,7,11,15], target = 9
Output: [0,1]
Explanation: Because nums[0] + nums[1] == 9, we return [0, 1].

Example 2:
Input: nums = [3,2,4], target = 6
Output: [1,2]

Example 3:
Input: nums = [3,3], target = 6
Output: [0,1]', 
0, '["python","javascript","java","cpp"]', 
'2 <= nums.length <= 10^4
-10^9 <= nums[i] <= 10^9
-10^9 <= target <= 10^9
Only one valid answer exists.', 
'22222222-2222-2222-2222-222222222222', 2000, 256, 1, 1, 156, 342, '2024-02-10 10:00:00', '2024-12-01 10:30:00', 0);

-- Problem 2: Valid Parentheses (Easy)
INSERT INTO [Problems] ([Id], [Title], [Slug], [Statement], [Difficulty], [LanguageSupportJson], [Constraints], [AuthorId], [TimeLimitMs], [MemoryLimitMb], [IsPublic], [IsActive], [AcceptedSubmissionsCount], [TotalSubmissionsCount], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES 
('aabbccdd-eeff-0011-2233-445566778899', 'Valid Parentheses', 'valid-parentheses',
'Given a string s containing just the characters ''('', '')'', ''{'', ''}'', ''['' and '']'', determine if the input string is valid.

An input string is valid if:
1. Open brackets must be closed by the same type of brackets.
2. Open brackets must be closed in the correct order.
3. Every close bracket has a corresponding open bracket of the same type.

Example 1:
Input: s = "()"
Output: true

Example 2:
Input: s = "()[]{}"
Output: true

Example 3:
Input: s = "(]"
Output: false',
0, '["python","javascript","java","cpp"]',
'1 <= s.length <= 10^4
s consists of parentheses only ''()[]{}''.',
'22222222-2222-2222-2222-222222222222', 2000, 256, 1, 1, 89, 123, '2024-02-12 11:00:00', '2024-12-01 10:30:00', 0);

-- Problem 3: Best Time to Buy and Sell Stock (Easy)
INSERT INTO [Problems] ([Id], [Title], [Slug], [Statement], [Difficulty], [LanguageSupportJson], [Constraints], [AuthorId], [TimeLimitMs], [MemoryLimitMb], [IsPublic], [IsActive], [AcceptedSubmissionsCount], [TotalSubmissionsCount], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES 
('10203040-5060-7080-90a0-b0c0d0e0f000', 'Best Time to Buy and Sell Stock', 'best-time-to-buy-and-sell-stock',
'You are given an array prices where prices[i] is the price of a given stock on the ith day.

You want to maximize your profit by choosing a single day to buy one stock and choosing a different day in the future to sell that stock.

Return the maximum profit you can achieve from this transaction. If you cannot achieve any profit, return 0.

Example 1:
Input: prices = [7,1,5,3,6,4]
Output: 5
Explanation: Buy on day 2 (price = 1) and sell on day 5 (price = 6), profit = 6-1 = 5.

Example 2:
Input: prices = [7,6,4,3,1]
Output: 0
Explanation: No transactions are done, max profit = 0.',
0, '["python","javascript","java","cpp"]',
'1 <= prices.length <= 10^5
0 <= prices[i] <= 10^4',
'22222222-2222-2222-2222-222222222222', 2000, 256, 1, 1, 67, 145, '2024-02-15 09:00:00', '2024-12-01 10:30:00', 0);

-- ============================================================================
-- PROBLEM TAGS (Linking problems to concepts)
-- ============================================================================

INSERT INTO [ProblemTags] ([ProblemId], [TagId])
VALUES 
-- Two Sum
('8f79d1fc-0492-4164-a48b-313e57cdc216', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'), -- Arrays & Hashing

-- Valid Parentheses (no tags in current schema but adding Arrays)
('aabbccdd-eeff-0011-2233-445566778899', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'), -- Arrays & Hashing

-- Best Time to Buy and Sell Stock
('10203040-5060-7080-90a0-b0c0d0e0f000', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'), -- Arrays & Hashing
('10203040-5060-7080-90a0-b0c0d0e0f000', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'); -- Two Pointers

-- ============================================================================
-- TEST CASES
-- ============================================================================

-- Two Sum Test Cases
INSERT INTO [TestCases] ([Id], [ProblemId], [Input], [ExpectedOutput], [IsHidden], [CreatedAt])
VALUES 
('tc000001-0000-0000-0000-000000000001', '8f79d1fc-0492-4164-a48b-313e57cdc216', '[2,7,11,15]
9', '[0,1]', 0, '2024-02-10 10:30:00'),
('tc000002-0000-0000-0000-000000000002', '8f79d1fc-0492-4164-a48b-313e57cdc216', '[3,2,4]
6', '[1,2]', 0, '2024-02-10 10:30:00'),
('tc000003-0000-0000-0000-000000000003', '8f79d1fc-0492-4164-a48b-313e57cdc216', '[3,3]
6', '[0,1]', 0, '2024-02-10 10:30:00'),
('tc000004-0000-0000-0000-000000000004', '8f79d1fc-0492-4164-a48b-313e57cdc216', '[1,5,3,7,9]
12', '[1,3]', 1, '2024-02-10 10:30:00');

-- Valid Parentheses Test Cases
INSERT INTO [TestCases] ([Id], [ProblemId], [Input], [ExpectedOutput], [IsHidden], [CreatedAt])
VALUES 
('tc000005-0000-0000-0000-000000000005', 'aabbccdd-eeff-0011-2233-445566778899', '()', 'true', 0, '2024-02-12 11:30:00'),
('tc000006-0000-0000-0000-000000000006', 'aabbccdd-eeff-0011-2233-445566778899', '()[]{}', 'true', 0, '2024-02-12 11:30:00'),
('tc000007-0000-0000-0000-000000000007', 'aabbccdd-eeff-0011-2233-445566778899', '(]', 'false', 0, '2024-02-12 11:30:00'),
('tc000008-0000-0000-0000-000000000008', 'aabbccdd-eeff-0011-2233-445566778899', '([)]', 'false', 1, '2024-02-12 11:30:00');

-- ============================================================================
-- SUBMISSIONS (Alice's submission history for Two Sum)
-- ============================================================================

-- Attempt 1: Wrong Answer (used brute force, output format wrong)
INSERT INTO [Submissions] ([Id], [ProblemId], [UserId], [Code], [Language], [Status], [SubmittedAt], [ExecutionTimeMs], [MemoryUsedKb], [PassedTestCases], [TotalTestCases], [Score], [UpdatedAt], [IsDeleted])
VALUES 
('sub00001-0000-0000-0000-000000000001', '8f79d1fc-0492-4164-a48b-313e57cdc216', '44444444-4444-4444-4444-444444444444',
'def twoSum(nums, target):
    for i in range(len(nums)):
        for j in range(i+1, len(nums)):
            if nums[i] + nums[j] == target:
                return i, j  # Wrong: should return [i, j]
    return None', 
0, 4, '2024-11-30 14:15:00', 145, 1024, 0, 4, 0, '2024-11-30 14:15:05', 0);

-- Attempt 2: Time Limit Exceeded (correct logic but O(n²))
INSERT INTO [Submissions] ([Id], [ProblemId], [UserId], [Code], [Language], [Status], [SubmittedAt], [ExecutionTimeMs], [MemoryUsedKb], [PassedTestCases], [TotalTestCases], [Score], [UpdatedAt], [IsDeleted])
VALUES 
('sub00002-0000-0000-0000-000000000002', '8f79d1fc-0492-4164-a48b-313e57cdc216', '44444444-4444-4444-4444-444444444444',
'def twoSum(nums, target):
    for i in range(len(nums)):
        for j in range(i+1, len(nums)):
            if nums[i] + nums[j] == target:
                return [i, j]
    return []', 
0, 5, '2024-11-30 15:30:00', 2100, 1056, 3, 4, 75, '2024-11-30 15:30:08', 0);

-- Attempt 3: Accepted (using hash map)
INSERT INTO [Submissions] ([Id], [ProblemId], [UserId], [Code], [Language], [Status], [SubmittedAt], [ExecutionTimeMs], [MemoryUsedKb], [PassedTestCases], [TotalTestCases], [Score], [UpdatedAt], [IsDeleted])
VALUES 
('sub00003-0000-0000-0000-000000000003', '8f79d1fc-0492-4164-a48b-313e57cdc216', '44444444-4444-4444-4444-444444444444',
'def twoSum(nums, target):
    seen = {}
    for i, num in enumerate(nums):
        complement = target - num
        if complement in seen:
            return [seen[complement], i]
        seen[num] = i
    return []', 
0, 1, '2024-12-01 09:45:00', 125, 1248, 4, 4, 100, '2024-12-01 09:45:02', 0);

-- Bob's submissions for Two Sum (Accepted on first try)
INSERT INTO [Submissions] ([Id], [ProblemId], [UserId], [Code], [Language], [Status], [SubmittedAt], [ExecutionTimeMs], [MemoryUsedKb], [PassedTestCases], [TotalTestCases], [Score], [UpdatedAt], [IsDeleted])
VALUES 
('sub00004-0000-0000-0000-000000000004', '8f79d1fc-0492-4164-a48b-313e57cdc216', '55555555-5555-5555-5555-555555555555',
'def twoSum(nums, target):
    num_map = {}
    for i, num in enumerate(nums):
        diff = target - num
        if diff in num_map:
            return [num_map[diff], i]
        num_map[num] = i
    return []', 
0, 1, '2024-11-28 10:20:00', 98, 1180, 4, 4, 100, '2024-11-28 10:20:01', 0);

-- Charlie's submission for Valid Parentheses (Accepted)
INSERT INTO [Submissions] ([Id], [ProblemId], [UserId], [Code], [Language], [Status], [SubmittedAt], [ExecutionTimeMs], [MemoryUsedKb], [PassedTestCases], [TotalTestCases], [Score], [UpdatedAt], [IsDeleted])
VALUES 
('sub00005-0000-0000-0000-000000000005', 'aabbccdd-eeff-0011-2233-445566778899', '66666666-6666-6666-6666-666666666666',
'def isValid(s):
    stack = []
    mapping = {")": "(", "}": "{", "]": "["}
    for char in s:
        if char in mapping:
            top = stack.pop() if stack else "#"
            if mapping[char] != top:
                return False
        else:
            stack.append(char)
    return not stack', 
0, 1, '2024-11-29 16:30:00', 67, 896, 4, 4, 100, '2024-11-29 16:30:01', 0);

-- ============================================================================
-- SUBMISSION RESULTS
-- ============================================================================

INSERT INTO [SubmissionResults] ([Id], [SubmissionId], [Status], [ExecutionTimeMs], [MemoryUsedKb], [ErrorMessage], [TestCasesPassed], [TestCasesTotal], [CreatedAt])
VALUES 
('sr000001-0000-0000-0000-000000000001', 'sub00001-0000-0000-0000-000000000001', 4, 145, 1024, 'Expected output: [0,1], Got: (0, 1)', 0, 4, '2024-11-30 14:15:05'),
('sr000002-0000-0000-0000-000000000002', 'sub00002-0000-0000-0000-000000000002', 5, 2100, 1056, 'Time limit exceeded (limit: 2000ms)', 3, 4, '2024-11-30 15:30:08'),
('sr000003-0000-0000-0000-000000000003', 'sub00003-0000-0000-0000-000000000003', 1, 125, 1248, NULL, 4, 4, '2024-12-01 09:45:02'),
('sr000004-0000-0000-0000-000000000004', 'sub00004-0000-0000-0000-000000000004', 1, 98, 1180, NULL, 4, 4, '2024-11-28 10:20:01'),
('sr000005-0000-0000-0000-000000000005', 'sub00005-0000-0000-0000-000000000005', 1, 67, 896, NULL, 4, 4, '2024-11-29 16:30:01');

-- ============================================================================
-- TEST CASE RESULTS
-- ============================================================================

-- Alice's first attempt (all failed)
INSERT INTO [TestCaseResults] ([Id], [SubmissionId], [TestCaseId], [Passed], [ExecutionTimeMs], [MemoryUsedKb], [ActualOutput], [ErrorMessage], [CreatedAt])
VALUES 
('tcr00001-0000-0000-0000-000000000001', 'sub00001-0000-0000-0000-000000000001', 'tc000001-0000-0000-0000-000000000001', 0, 35, 256, '(0, 1)', 'Output format mismatch', '2024-11-30 14:15:05'),
('tcr00002-0000-0000-0000-000000000002', 'sub00001-0000-0000-0000-000000000001', 'tc000002-0000-0000-0000-000000000002', 0, 36, 256, '(1, 2)', 'Output format mismatch', '2024-11-30 14:15:05'),
('tcr00003-0000-0000-0000-000000000003', 'sub00001-0000-0000-0000-000000000001', 'tc000003-0000-0000-0000-000000000003', 0, 34, 256, '(0, 1)', 'Output format mismatch', '2024-11-30 14:15:05'),
('tcr00004-0000-0000-0000-000000000004', 'sub00001-0000-0000-0000-000000000001', 'tc000004-0000-0000-0000-000000000004', 0, 40, 256, '(1, 3)', 'Output format mismatch', '2024-11-30 14:15:05');

-- Alice's second attempt (3 passed, 1 TLE)
INSERT INTO [TestCaseResults] ([Id], [SubmissionId], [TestCaseId], [Passed], [ExecutionTimeMs], [MemoryUsedKb], [ActualOutput], [ErrorMessage], [CreatedAt])
VALUES 
('tcr00005-0000-0000-0000-000000000005', 'sub00002-0000-0000-0000-000000000002', 'tc000001-0000-0000-0000-000000000001', 1, 42, 264, '[0, 1]', NULL, '2024-11-30 15:30:08'),
('tcr00006-0000-0000-0000-000000000006', 'sub00002-0000-0000-0000-000000000002', 'tc000002-0000-0000-0000-000000000002', 1, 45, 264, '[1, 2]', NULL, '2024-11-30 15:30:08'),
('tcr00007-0000-0000-0000-000000000007', 'sub00002-0000-0000-0000-000000000002', 'tc000003-0000-0000-0000-000000000003', 1, 43, 264, '[0, 1]', NULL, '2024-11-30 15:30:08'),
('tcr00008-0000-0000-0000-000000000008', 'sub00002-0000-0000-0000-000000000002', 'tc000004-0000-0000-0000-000000000004', 0, 2100, 264, NULL, 'Time limit exceeded', '2024-11-30 15:30:08');

-- Alice's third attempt (all passed)
INSERT INTO [TestCaseResults] ([Id], [SubmissionId], [TestCaseId], [Passed], [ExecutionTimeMs], [MemoryUsedKb], [ActualOutput], [ErrorMessage], [CreatedAt])
VALUES 
('tcr00009-0000-0000-0000-000000000009', 'sub00003-0000-0000-0000-000000000003', 'tc000001-0000-0000-0000-000000000001', 1, 30, 312, '[0, 1]', NULL, '2024-12-01 09:45:02'),
('tcr00010-0000-0000-0000-000000000010', 'sub00003-0000-0000-0000-000000000003', 'tc000002-0000-0000-0000-000000000002', 1, 31, 312, '[1, 2]', NULL, '2024-12-01 09:45:02'),
('tcr00011-0000-0000-0000-000000000011', 'sub00003-0000-0000-0000-000000000003', 'tc000003-0000-0000-0000-000000000003', 1, 32, 312, '[0, 1]', NULL, '2024-12-01 09:45:02'),
('tcr00012-0000-0000-0000-000000000012', 'sub00003-0000-0000-0000-000000000003', 'tc000004-0000-0000-0000-000000000004', 1, 32, 312, '[1, 3]', NULL, '2024-12-01 09:45:02');

-- ============================================================================
-- HINT LOGS (Alice's hint history for Two Sum)
-- ============================================================================

-- Hint after first failed attempt
INSERT INTO [HintLogs] ([Id], [UserId], [ProblemId], [HintLevel], [RequestText], [ResponseText], [ToolsUsedJson], [ReasoningSummary], [ModelUsed], [TokenCount], [LatencyMs], [CreatedAt])
VALUES 
('hint0001-0000-0000-0000-000000000001', '44444444-4444-4444-4444-444444444444', '8f79d1fc-0492-4164-a48b-313e57cdc216', 1, 
'I tried returning a tuple but the test expects a list',
'Pay attention to the expected output format in the problem description. Python has different ways to return multiple values - make sure you''re using the one that matches the examples.',
'[]', 'First hint focusing on output format issue without revealing the solution', 'global.anthropic.claude-sonnet-4-5-20250929-v1:0', 245, 1250, '2024-11-30 14:20:00');

-- Hint after second attempt (TLE)
INSERT INTO [HintLogs] ([Id], [UserId], [ProblemId], [HintLevel], [RequestText], [ResponseText], [ToolsUsedJson], [ReasoningSummary], [ModelUsed], [TokenCount], [LatencyMs], [CreatedAt])
VALUES 
('hint0002-0000-0000-0000-000000000002', '44444444-4444-4444-4444-444444444444', '8f79d1fc-0492-4164-a48b-313e57cdc216', 2,
'My solution works but it''s too slow on large inputs',
'Your nested loops create O(n²) time complexity. Consider: what if you could check whether a complement exists in constant time? Think about a data structure that provides O(1) lookups.',
'["search_knowledge_base","get_previous_hints"]', 'Student has correct logic but needs optimization. Hinting at hash map without giving full solution.', 'global.anthropic.claude-sonnet-4-5-20250929-v1:0', 456, 2340, '2024-11-30 16:00:00');

-- Hint before third attempt (more specific)
INSERT INTO [HintLogs] ([Id], [UserId], [ProblemId], [HintLevel], [RequestText], [ResponseText], [ToolsUsedJson], [ReasoningSummary], [ModelUsed], [TokenCount], [LatencyMs], [CreatedAt])
VALUES 
('hint0003-0000-0000-0000-000000000003', '44444444-4444-4444-4444-444444444444', '8f79d1fc-0492-4164-a48b-313e57cdc216', 3,
'I understand I need O(1) lookup, but how do I store the values?',
'Store each number as you iterate, with the number as the key and its index as the value. For each new number, calculate what complement you need, then check if that complement already exists in your storage.',
'["search_knowledge_base","get_attempt_history","check_partial_code"]', 'Third attempt - student understands concept, giving more concrete guidance on implementation', 'global.anthropic.claude-sonnet-4-5-20250929-v1:0', 512, 3150, '2024-12-01 09:30:00');

-- Bob's hint (accepted after hint)
INSERT INTO [HintLogs] ([Id], [UserId], [ProblemId], [HintLevel], [RequestText], [ResponseText], [ToolsUsedJson], [ReasoningSummary], [ModelUsed], [TokenCount], [LatencyMs], [CreatedAt])
VALUES 
('hint0004-0000-0000-0000-000000000004', '55555555-5555-5555-5555-555555555555', '8f79d1fc-0492-4164-a48b-313e57cdc216', 1,
'How can I solve this efficiently?',
'Think about what information you need to remember as you iterate through the array. For each number, what would make it easy to find its pair?',
'["get_attempt_history","search_knowledge_base"]', 'Experienced student asking for efficiency hint', 'global.anthropic.claude-sonnet-4-5-20250929-v1:0', 312, 1680, '2024-11-28 10:00:00');

-- ============================================================================
-- FEEDBACK RECORDS
-- ============================================================================

INSERT INTO [FeedbackRecords] ([Id], [SubmissionId], [Type], [Category], [Message], [CodeSnippet], [LineNumber], [Severity], [CreatedAt])
VALUES 
('fb000001-0000-0000-0000-000000000001', 'sub00001-0000-0000-0000-000000000001', 1, 'Output Format', 'Return value should be a list [i, j], not a tuple (i, j)', 'return i, j', 4, 2, '2024-11-30 14:15:05'),
('fb000002-0000-0000-0000-000000000002', 'sub00002-0000-0000-0000-000000000002', 0, 'Performance', 'Nested loops cause O(n²) complexity. Consider using a hash map for O(1) lookups.', 'for i in range(len(nums)):\n    for j in range(i+1, len(nums)):', 2, 1, '2024-11-30 15:30:08'),
('fb000003-0000-0000-0000-000000000003', 'sub00003-0000-0000-0000-000000000003', 2, 'Best Practice', 'Excellent use of hash map! This achieves optimal O(n) time complexity.', 'seen = {}\nfor i, num in enumerate(nums):', 2, 0, '2024-12-01 09:45:02');

-- ============================================================================
-- CONTESTS
-- ============================================================================

INSERT INTO [Contests] ([Id], [Title], [Description], [StartTime], [EndTime], [CreatedBy], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES 
('contest1-0000-0000-0000-000000000001', 'Weekly Challenge #1', 'Easy problems for beginners', '2024-12-05 09:00:00', '2024-12-05 11:00:00', '22222222-2222-2222-2222-222222222222', 1, '2024-11-28 10:00:00', '2024-11-28 10:00:00', 0);

INSERT INTO [ContestProblems] ([ContestId], [ProblemId], [Points], [DisplayOrder])
VALUES 
('contest1-0000-0000-0000-000000000001', '8f79d1fc-0492-4164-a48b-313e57cdc216', 100, 1),
('contest1-0000-0000-0000-000000000001', 'aabbccdd-eeff-0011-2233-445566778899', 150, 2);

INSERT INTO [ContestParticipants] ([ContestId], [UserId], [Score], [Rank], [JoinedAt])
VALUES 
('contest1-0000-0000-0000-000000000001', '44444444-4444-4444-4444-444444444444', 100, 2, '2024-12-05 09:05:00'),
('contest1-0000-0000-0000-000000000001', '55555555-5555-5555-5555-555555555555', 250, 1, '2024-12-05 09:02:00');

-- ============================================================================
-- Summary Statistics
-- ============================================================================

SELECT 'Seed data insertion complete!' AS Status;
SELECT COUNT(*) AS TotalUsers FROM [Users];
SELECT COUNT(*) AS TotalProblems FROM [Problems];
SELECT COUNT(*) AS TotalSubmissions FROM [Submissions];
SELECT COUNT(*) AS TotalHints FROM [HintLogs];
SELECT COUNT(*) AS TotalTestCases FROM [TestCases];
