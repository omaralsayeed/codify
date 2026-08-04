# Recursion

## Overview
Recursion is a technique where a function calls itself to solve a problem by breaking it into smaller, self-similar subproblems.

## Key Idea
Every recursive solution needs:
1. A base case that stops the recursion.
2. A recursive case that moves toward the base case.

## Common Mistakes
- Forgetting the base case, leading to infinite recursion or stack overflow.
- Recursive calls that do not reduce the problem size.
- Recomputing the same subproblem many times instead of memoizing.

## When to Use
- Problems with self-similar structure (trees, graphs, divide and conquer).
- When an iterative solution would be significantly more complex.

## Hint Template
"Think about the simplest version of this problem. What is the base case? Then assume your function already works for a smaller input — how do you combine that result?"
