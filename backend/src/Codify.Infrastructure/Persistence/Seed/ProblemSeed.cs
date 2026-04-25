using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Persistence.Seed;

public static class ProblemSeed
{
    public static async Task SeedAsync(CodifyDbContext db)
    {
        if (await db.Problems.AnyAsync()) return;

        // Fetch tags we need by name
        var tags = await db.ConceptTags.ToListAsync();
        ConceptTag Tag(string name) =>
            tags.First(t => t.Name == name);

        var problems = new List<(Problem Problem, string[] TagNames, (string Input, string Output, bool IsSample)[] Cases)>
        {
            // ── 1. Two Sum ────────────────────────────────────────────────────
            (
                Problem.Create(
                    title: "Two Sum",
                    statement: """
                        Given an array of integers `nums` and an integer `target`, return the **indices** of the two numbers that add up to `target`.

                        You may assume that each input would have **exactly one solution**, and you may not use the same element twice.

                        You can return the answer in any order.
                        """,
                    difficulty: Difficulty.Easy,
                    constraints: "2 ≤ nums.length ≤ 10⁴\n-10⁹ ≤ nums[i] ≤ 10⁹\n-10⁹ ≤ target ≤ 10⁹",
                    languageSupportJson: """["Python","CSharp"]""",
                    timeLimitMs: 1000,
                    memoryLimitMb: 128),
                ["Arrays & Hashing"],
                [
                    ("[2,7,11,15]\n9",  "[0,1]",  true),
                    ("[3,2,4]\n6",      "[1,2]",  true),
                    ("[3,3]\n6",        "[0,1]",  false),
                    ("[1,2,3,4,5]\n9",  "[3,4]",  false),
                    ("[-1,-2,-3,-4,-5]\n-8", "[-4,-3]", false),  // note: 0-indexed answer
                ]
            ),

            // ── 2. Valid Parentheses ──────────────────────────────────────────
            (
                Problem.Create(
                    title: "Valid Parentheses",
                    statement: """
                        Given a string `s` containing just the characters `'('`, `')'`, `'{'`, `'}'`, `'['` and `']'`, determine if the input string is **valid**.

                        An input string is valid if:
                        - Open brackets must be closed by the same type of brackets.
                        - Open brackets must be closed in the correct order.
                        - Every close bracket has a corresponding open bracket of the same type.
                        """,
                    difficulty: Difficulty.Easy,
                    constraints: "1 ≤ s.length ≤ 10⁴\ns consists of parentheses only '()[]{}'",
                    languageSupportJson: """["Python","CSharp"]""",
                    timeLimitMs: 1000,
                    memoryLimitMb: 128),
                ["Linked Lists"],
                [
                    ("()",      "true",  true),
                    ("()[]{}", "true",  true),
                    ("(]",     "false", false),
                    ("([)]",   "false", false),
                    ("{[]}",   "true",  false),
                ]
            ),

            // ── 3. Binary Search ─────────────────────────────────────────────
            (
                Problem.Create(
                    title: "Binary Search",
                    statement: """
                        Given an array of integers `nums` which is sorted in ascending order, and an integer `target`, write a function to search `target` in `nums`.

                        If `target` exists, return its index. Otherwise, return `-1`.

                        You must write an algorithm with **O(log n)** runtime complexity.
                        """,
                    difficulty: Difficulty.Easy,
                    constraints: "1 ≤ nums.length ≤ 10⁴\n-10⁴ < nums[i], target < 10⁴\nAll integers in nums are unique.\nnums is sorted in ascending order.",
                    languageSupportJson: """["Python","CSharp"]""",
                    timeLimitMs: 1000,
                    memoryLimitMb: 128),
                ["Binary Search"],
                [
                    ("[-1,0,3,5,9,12]\n9",  "4",  true),
                    ("[-1,0,3,5,9,12]\n2",  "-1", true),
                    ("[5]\n5",               "0",  false),
                    ("[5]\n-5",              "-1", false),
                    ("[1,2,3,4,5,6,7,8]\n6","5",  false),
                ]
            ),

            // ── 4. Maximum Subarray ───────────────────────────────────────────
            (
                Problem.Create(
                    title: "Maximum Subarray",
                    statement: """
                        Given an integer array `nums`, find the **subarray** with the largest sum, and return its sum.

                        A **subarray** is a contiguous non-empty sequence of elements within an array.
                        """,
                    difficulty: Difficulty.Medium,
                    constraints: "1 ≤ nums.length ≤ 10⁵\n-10⁴ ≤ nums[i] ≤ 10⁴",
                    languageSupportJson: """["Python","CSharp"]""",
                    timeLimitMs: 2000,
                    memoryLimitMb: 256),
                ["Dynamic Programming", "Greedy"],
                [
                    ("[-2,1,-3,4,-1,2,1,-5,4]", "6",  true),
                    ("[1]",                       "1",  true),
                    ("[5,4,-1,7,8]",              "23", false),
                    ("[-1,-2,-3,-4]",             "-1", false),
                    ("[1,-1,1,-1,1]",             "1",  false),
                ]
            ),

            // ── 5. Climbing Stairs ────────────────────────────────────────────
            (
                Problem.Create(
                    title: "Climbing Stairs",
                    statement: """
                        You are climbing a staircase. It takes `n` steps to reach the top.

                        Each time you can either climb `1` or `2` steps. In how many distinct ways can you climb to the top?
                        """,
                    difficulty: Difficulty.Easy,
                    constraints: "1 ≤ n ≤ 45",
                    languageSupportJson: """["Python","CSharp"]""",
                    timeLimitMs: 1000,
                    memoryLimitMb: 128),
                ["Dynamic Programming", "Recursion"],
                [
                    ("2", "2",  true),
                    ("3", "3",  true),
                    ("1", "1",  false),
                    ("5", "8",  false),
                    ("10","89", false),
                ]
            ),
        };

        foreach (var (problem, tagNames, cases) in problems)
        {
            // Link tags
            foreach (var tagName in tagNames)
            {
                var tag = Tag(tagName);
                problem.ProblemTags.Add(ProblemTag.Create(problem.Id, tag.Id));
            }

            // Add test cases with order index
            int order = 0;
            foreach (var (input, output, isSample) in cases)
            {
                problem.TestCases.Add(TestCase.Create(
                    problemId: problem.Id,
                    inputData: input,
                    expectedOutput: output,
                    isSample: isSample,
                    visibilityMode: isSample ? TestCaseVisibility.Public : TestCaseVisibility.Hidden,
                    orderIndex: order++));
            }

            db.Problems.Add(problem);
        }

        await db.SaveChangesAsync();
    }
}
