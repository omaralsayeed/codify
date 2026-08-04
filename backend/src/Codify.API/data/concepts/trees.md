# Trees

## Overview
A tree is a hierarchical graph with no cycles, consisting of a root node and child nodes. Binary trees are the most common variant in interviews.

## Key Ideas
- Traversal orders: pre-order, in-order, post-order, level-order (BFS).
- Special trees: Binary Search Tree (BST), balanced trees (AVL, Red-Black), heaps.
- Recursion is natural for tree problems because each subtree is itself a tree.

## Common Mistakes
- Forgetting to handle the null/empty node as the base case.
- Passing state down the tree incorrectly.
- Confusing tree height vs depth terminology.

## When to Use
- Hierarchical data (file systems, organization charts, HTML DOM).
- Problems requiring ordered search or priority access.

## Hint Template
"Think recursively: what information do you need from the left and right subtrees? How can you combine those results at the current node to answer the problem?"
