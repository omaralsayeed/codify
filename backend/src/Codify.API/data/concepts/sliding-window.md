# Sliding Window

## Overview
Sliding window maintains a subarray or substring that satisfies a condition while moving its start and end boundaries across the data.

## When to Use
- Subarray / substring problems with a size or sum constraint.
- Finding the longest or shortest valid contiguous segment.
- Problems with monotonic conditions.

## Common Patterns
- **Fixed-size window**: Move the window one element at a time.
- **Variable-size window**: Expand and contract based on conditions.
- **Two pointers as window bounds**: left and right pointers define the window.

## Common Mistakes
- Forgetting to shrink the window when the condition is violated.
- Updating the aggregated value incorrectly when the window slides.
- Off-by-one errors with window boundaries.

## Example Hint Template
"Try maintaining a window that satisfies the constraint, then shrink it from the left while it still holds."
