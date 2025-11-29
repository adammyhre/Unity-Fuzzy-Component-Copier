#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

namespace FuzzyComponentCopier {
    /// <summary>
    /// Greedy matching solver - faster but may not find optimal solution.
    /// Time complexity: O(n² log n) for sorting + O(n²) for matching.
    /// </summary>
    class GreedySolver : IMatchingSolver {
        public int[] Solve(float[,] costMatrix, int n, int m) {
            // Create list of all possible matches with their costs
            var matches = new List<(int source, int target, float cost)>();
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < m; j++) {
                    if (costMatrix[i, j] < float.MaxValue) {
                        matches.Add((i, j, costMatrix[i, j]));
                    }
                }
            }

            // Sort by cost (ascending - we want minimum cost)
            matches = matches.OrderBy(m => m.cost).ToList();

            // Greedy assignment: take best matches first, ensuring one-to-one
            int[] result = new int[n];
            for (int i = 0; i < n; i++) result[i] = -1; // Initialize as unmatched

            var usedTargets = new HashSet<int>();
            foreach (var match in matches) {
                // If both source and target are available, assign them
                if (result[match.source] == -1 && !usedTargets.Contains(match.target)) {
                    result[match.source] = match.target;
                    usedTargets.Add(match.target);
                }
            }

            return result;
        }
    }
}
#endif