# Linked Lists

## Overview
A linked list is a linear collection of nodes where each node contains data and a reference to the next node.

## When to Use
- Frequent insertions and deletions at known positions.
- Implementing stacks, queues, or adjacency lists.
- Problems requiring reordering without shifting elements.

## Common Patterns
- **Fast and slow pointers**: Detect cycles and find the middle.
- **Reversal**: Reverse a list iteratively or recursively.
- **Merge**: Combine two sorted lists.

## Common Mistakes
- Losing track of the next pointer during reversal.
- Not handling the null tail correctly.
- Creating cycles accidentally.

## Example Hint Template
"Consider using two pointers moving at different speeds to detect cycles or find the middle node."
