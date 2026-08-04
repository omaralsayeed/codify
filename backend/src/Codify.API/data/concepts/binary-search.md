# Binary Search

## Overview
Binary search repeatedly divides a sorted search interval in half, reducing the problem size exponentially.

## When to Use
- Searching in sorted arrays or answer spaces.
- Finding boundaries (first/last occurrence, lower/upper bound).
- Monotonic predicate problems.

## Common Patterns
- **Standard binary search**: Track left and right pointers, compare with midpoint.
- **Lower/upper bound**: Adjust pointers based on predicate results.
- **Binary search on answer**: Search over a range of possible answers.

## Common Mistakes
- Infinite loops due to incorrect midpoint calculation.
- Off-by-one errors with left/right boundaries.
- Applying binary search to unsorted data.

## Example Hint Template
"If the data is sorted, can you eliminate half of the remaining elements at each step?"
