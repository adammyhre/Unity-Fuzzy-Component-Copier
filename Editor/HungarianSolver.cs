#if UNITY_EDITOR
using System;

namespace FuzzyComponentCopier {
    /// <summary>
    /// Core Hungarian Algorithm (Munkres) solver.
    /// Implements the optimal assignment algorithm with O(n³) time complexity.
    /// </summary>
    class HungarianSolver : IMatchingSolver {
        public int[] Solve(float[,] cost, int n, int m) {
            int size = Math.Max(n, m); // Hungarian algorithm requires square matrix
        
            // Create square cost matrix (pad with infinite cost for missing entries)
            float[,] squareCost = new float[size, size];
            for (int i = 0; i < size; i++) {
                for (int j = 0; j < size; j++) {
                    if (i < n && j < m) {
                        squareCost[i, j] = cost[i, j];
                    } else {
                        squareCost[i, j] = float.MaxValue; // No match = infinite cost
                    }
                }
            }

            // Run Hungarian algorithm on square matrix
            int[] assignment = SolveInternal(squareCost, size);

            // Convert to result format (only return assignments for actual sources)
            int[] result = new int[n];
            for (int i = 0; i < n; i++) {
                int j = assignment[i];
                result[i] = (j < m) ? j : -1; // -1 means unmatched
            }

            return result;
        }

        /// <summary>
        /// Core Hungarian algorithm implementation (Munkres algorithm).
        /// Uses dual variables and augmenting paths to find minimum cost perfect matching.
        /// </summary>
        /// <param name="cost">Cost matrix (n×n), where cost[i,j] is the cost of matching i to j</param>
        /// <param name="n">Size of the square matrix</param>
        /// <returns>Assignment array: result[i] = j means row i is assigned to column j</returns>
        static int[] SolveInternal(float[,] cost, int n) {
            // Dual variables for the linear programming relaxation:
            // u[i]: dual variable for row i (source)
            // v[j]: dual variable for column j (target)
            // These maintain the constraint: u[i] + v[j] <= cost[i,j] for all edges
            float[] u = new float[n + 1]; // Row duals (indexed 1..n)
            float[] v = new float[n];     // Column duals (indexed 0..n-1)
        
            // Matching arrays:
            // p[j]: which row is currently matched to column j (0 = unmatched)
            // way[j]: previous column in the augmenting path (for path reconstruction)
            int[] p = new int[n];   // p[j] = i means column j is matched to row i
            int[] way = new int[n]; // Used to reconstruct augmenting paths

            // Process each row (source) one by one to build the matching
            for (int i = 1; i <= n; i++) {
                // Start augmenting path from row i
                p[0] = i;   // Column 0 is a sentinel, p[0] stores the current row we're matching
                int j0 = 0; // Current column in the augmenting path (start at sentinel)
            
                // minv[j]: minimum reduced cost for column j (cost - u[i] - v[j])
                // Used to find the next column to add to the augmenting path
                float[] minv = new float[n];
                for (int j = 0; j < n; j++) minv[j] = float.MaxValue;
            
                // Track which columns are already in the current augmenting path
                bool[] used = new bool[n];

                // Find augmenting path: alternate between matched and unmatched edges
                // This loop continues until we find a path that increases the matching
                do {
                    used[j0] = true; // Mark current column as used in this path
                    int i0 = p[j0];  // Get the row matched to column j0
                
                    // Safety check: ensure i0 is valid (should always be 1..n)
                    if (i0 <= 0 || i0 > n) {
                        break;
                    }
                
                    // Find the best next column to add to the augmenting path
                    float delta = float.MaxValue; // Minimum reduced cost found
                    int j1 = -1;                  // Column with minimum reduced cost

                    // Check all unused columns to find the one with minimum reduced cost
                    for (int j = 0; j < n; j++) {
                        if (!used[j]) {
                            // Reduced cost: actual cost minus dual variables
                            // This measures how "cheap" it would be to use edge (i0, j)
                            float cur = cost[i0 - 1, j] - u[i0] - v[j];
                        
                            // Update minimum reduced cost for this column
                            if (cur < minv[j]) {
                                minv[j] = cur;
                                way[j] = j0; // Remember path: column j came from column j0
                            }
                        
                            // Track the column with globally minimum reduced cost
                            if (minv[j] < delta) {
                                delta = minv[j];
                                j1 = j; // This is the best next column to explore
                            }
                        }
                    }

                    // Safety check: if no valid match found, break to avoid infinite loop
                    // This can happen if all remaining columns have infinite cost
                    if (j1 == -1 || delta >= float.MaxValue - 1f) {
                        break;
                    }

                    // Update dual variables to maintain optimality conditions
                    // This is the key step: adjust u and v so the reduced costs stay valid
                    for (int j = 0; j < n; j++) {
                        if (used[j]) {
                            // For columns already in path: adjust their duals
                            u[p[j]] += delta; // Increase row dual
                            v[j] -= delta;    // Decrease column dual (maintains u[i] + v[j] <= cost)
                        } else {
                            // For unused columns: adjust their minimum reduced cost
                            minv[j] -= delta; // This effectively "shifts" all reduced costs by delta
                        }
                    }

                    // Move to the next column in the augmenting path
                    j0 = j1;
                } while (p[j0] != 0); // Continue until we reach an unmatched column (p[j0] == 0)

                // Reconstruct and apply the augmenting path
                // This updates the matching: flip edges along the path to increase matching size
                do {
                    int j1 = way[j0]; // Previous column in the path
                    p[j0] = p[j1];    // Transfer the matching: column j0 now matches what j1 matched
                    j0 = j1;          // Move backwards along the path
                } while (j0 != 0);    // Continue until we reach the start (sentinel column 0)
            }

            // Convert matching array to assignment result
            // p[j] = i means column j is matched to row i
            // We want result[i] = j (row i is matched to column j)
            int[] result = new int[n];
            for (int j = 0; j < n; j++) {
                if (p[j] > 0) {
                    result[p[j] - 1] = j; // Convert: p[j] is 1-indexed, result is 0-indexed
                }
            }

            return result;
        }
    }
}
#endif