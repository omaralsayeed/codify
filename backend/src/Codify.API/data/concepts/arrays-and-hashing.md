# Arrays & Hashing

## Overview
Arrays are contiguous blocks of memory that store elements of the same type. Hashing uses a hash function to map data of arbitrary size to fixed-size values, enabling O(1) average-time lookups.

## When to Use
- Frequent lookups, insertions, and deletions.
- Checking for duplicates or counting frequencies.
- Two-sum problems and anagram detection.

## Common Patterns
- **Hash map / dictionary**: Store values and their indices for O(1) access.
- **Frequency counter**: Count occurrences using a dictionary.
- **Set**: Track seen elements to detect duplicates.

## Common Mistakes
- Forgetting that dictionary keys must be hashable.
- Assuming hash lookups are always O(1) without considering collisions.
- Modifying a collection while iterating over it.

## Example Hint Template
"Think about whether you can use a hash map to turn an O(n^2) search into an O(n) lookup."
