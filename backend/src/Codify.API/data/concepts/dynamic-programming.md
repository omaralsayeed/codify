# Dynamic Programming

## Overview
Dynamic Programming (DP) solves complex problems by breaking them into overlapping subproblems, solving each subproblem once, and storing the results for reuse.

## Key Idea
Two main approaches:
1. Top-down with memoization (recursive + cache).
2. Bottom-up with tabulation (iterative, builds a table).

## Common Mistakes
- Not identifying the correct state / subproblem definition.
- Forgetting to initialize the base cases in the table.
- Choosing a state that is too large to compute within time limits.

## When to Use
- Problems asking for an optimal value (minimum, maximum, count) with overlapping subproblems.
- Problems with constraints that suggest polynomial-time solutions.

## Hint Template
"Can you define a state that captures the important information at each step? Write a recurrence relation that expresses the answer in terms of smaller states, and decide whether to use memoization or tabulation."
