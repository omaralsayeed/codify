# Graphs

## Overview
A graph is a collection of nodes (vertices) connected by edges. Graph problems are common in routing, networks, dependency resolution, and game states.

## Key Ideas
- Representation: adjacency list, adjacency matrix, edge list.
- Traversal: Breadth-First Search (BFS) for shortest path on unweighted graphs; Depth-First Search (DFS) for connectivity, cycles, and topological sort.
- Weighted shortest path: Dijkstra (non-negative weights), Bellman-Ford (negative weights), Floyd-Warshall (all-pairs).

## Common Mistakes
- Confusing BFS and DFS use cases.
- Not tracking visited nodes, causing infinite loops.
- Using Dijkstra with negative edge weights.

## When to Use
- Relationships between entities (roads, dependencies, social networks).
- Problems that can be modeled as states and transitions.

## Hint Template
"How can you model the problem as a graph? Identify the nodes, edges, and what you are searching for — then choose the right traversal or shortest-path algorithm."
