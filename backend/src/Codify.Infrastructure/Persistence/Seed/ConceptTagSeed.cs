using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Persistence.Seed;

public static class ConceptTagSeed
{
    public static async Task SeedAsync(CodifyDbContext db)
    {
        if (await db.ConceptTags.AnyAsync()) return;

        var tags = new[]
        {
            ("Arrays & Hashing", "Techniques involving arrays and hash maps for efficient lookups and grouping."),
            ("Two Pointers", "Using two indices to traverse data structures, often reducing O(n²) to O(n)."),
            ("Sliding Window", "Maintaining a window over a sequence to compute results without recomputation."),
            ("Binary Search", "Efficiently searching sorted data by halving the search space each step."),
            ("Linked Lists", "Pointer-based linear data structures with O(1) insert/delete at known positions."),
            ("Trees", "Hierarchical data structures including BSTs, AVL trees, and traversal algorithms."),
            ("Graphs", "Nodes and edges — BFS, DFS, shortest paths, and connectivity problems."),
            ("Dynamic Programming", "Breaking problems into overlapping subproblems and caching results."),
            ("Greedy", "Making locally optimal choices at each step to find a global optimum."),
            ("Backtracking", "Exploring all possibilities by building candidates and abandoning invalid ones."),
            ("Recursion", "Solving problems by having a function call itself on smaller subproblems."),
            ("Sorting", "Ordering elements — merge sort, quick sort, counting sort, and their applications.")
        };

        foreach (var (name, description) in tags)
            db.ConceptTags.Add(ConceptTag.Create(name, description));

        await db.SaveChangesAsync();
    }
}
