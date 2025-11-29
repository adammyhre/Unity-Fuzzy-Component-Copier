#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace FuzzyComponentCopier {
    interface IMatchingSolver {
        /// <summary>
        /// Solves the assignment problem: finds which source should match which target.
        /// </summary>
        /// <param name="costMatrix">Cost matrix where costMatrix[i,j] is the cost of matching source i to target j</param>
        /// <param name="n">Number of sources</param>
        /// <param name="m">Number of targets</param>
        /// <returns>Assignment array: result[i] = j means source i is matched to target j, or -1 if unmatched</returns>
        int[] Solve(float[,] costMatrix, int n, int m);
    }
    
    /// <summary>
    /// Matching solver that finds optimal assignment between sources and targets.
    /// Supports multiple solver algorithms (Hungarian, Greedy, etc.).
    /// </summary>
    public static class MatchingSolver {
        static IMatchingSolver GetSolver() {
            return FuzzyCopierSettings.UseHungarianAlgorithm 
                ? new HungarianSolver() 
                : new GreedySolver();
        }

        /// <summary>
        /// Finds the optimal assignment of sources to targets that maximizes total match score.
        /// </summary>
        public static List<Match> FindOptimalAssignment(List<Match> allMatches, List<FieldValue> sources, List<SerializedProperty> targets) {
            if (allMatches.Count == 0) return new List<Match>();

            // Build index maps: map property paths to matrix indices for efficient lookup
            var sourceIndexMap = sources.Select((s, i) => new { s.propertyPath, i })
                .ToDictionary(x => x.propertyPath, x => x.i);
            var targetIndexMap = targets.Select((t, i) => new { t.propertyPath, i })
                .ToDictionary(x => x.propertyPath, x => x.i);

            int n = sources.Count;
            int m = targets.Count;

            // Create cost matrix: solver minimizes cost, but we want to maximize score
            // Solution: negate all scores so maximizing score = minimizing negative score
            float[,] costMatrix = new float[n, m];
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < m; j++) {
                    costMatrix[i, j] = float.MaxValue; // No match = infinite cost
                }
            }

            // Fill in actual match scores (negated: high score = low cost)
            foreach (var match in allMatches) {
                if (sourceIndexMap.TryGetValue(match.source.propertyPath, out int srcIdx) &&
                    targetIndexMap.TryGetValue(match.target.propertyPath, out int tgtIdx)) {
                    costMatrix[srcIdx, tgtIdx] = -match.score; // Negate: score 0.9 → cost -0.9
                }
            }

            // Run solver to find optimal assignment
            // Returns: assignment[i] = j means source i is matched to target j, or -1 if unmatched
            int[] assignment = GetSolver().Solve(costMatrix, n, m);

            // Convert assignment array back to Match objects
            var result = new List<Match>();
            for (int i = 0; i < n; i++) {
                int j = assignment[i];
                // Verify this is a valid match (not -1 and not infinite cost)
                if (j >= 0 && j < m && costMatrix[i, j] < float.MaxValue) {
                    // Find the original match object to preserve score and other data
                    var sourcePath = sources[i].propertyPath;
                    var targetPath = targets[j].propertyPath;
                    var match = allMatches.FirstOrDefault(m => 
                        m.source.propertyPath == sourcePath && 
                        m.target.propertyPath == targetPath);
                    if (match != null) {
                        result.Add(match);
                    }
                }
            }

            return result.OrderByDescending(m => m.score).ToList();
        }
    }

    [Serializable]
    public class FieldValue {
        public string propertyPath;
        public string displayName;
        public string niceName;
        public SerializedPropertyType serializedType;
        public string typeName;
        public string value;
        public Object[] references;
        public List<string> formerNames = new List<string>();
    }    
    
    public class Match {
        public FieldValue source;
        public SerializedProperty target;
        public float score;
    }
}
#endif