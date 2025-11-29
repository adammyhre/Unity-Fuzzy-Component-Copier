#if UNITY_EDITOR
using UnityEngine;

namespace FuzzyComponentCopier {
    /// <summary>
    /// Utility methods for computing Levenshtein distance – the minimum number of
    /// single–character edits needed to change one string into another.
    /// </summary>
    /// <remarks>
    /// This implementation returns a normalized similarity score in the range [0,1],
    /// where 1 means the strings are identical and 0 means they share no common
    /// characters at the same positions. It is used by the fuzzy copier to compare
    /// field names in a human-friendly way.
    /// </remarks>
    public static class LevenshteinDistance {
        /// <summary>
        /// Computes a normalized Levenshtein similarity score between two strings.
        /// </summary>
        /// <param name="s">The first string.</param>
        /// <param name="t">The second string.</param>
        /// <returns>
        /// A value between 0 and 1 where 1 means the strings are identical and 0
        /// means they are completely different.
        /// </returns>
        public static float ComputeNormalized(string s, string t) {
            if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 1f : 0f;
            if (string.IsNullOrEmpty(t)) return 0f;

            int n = s.Length, m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) { }

            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            for (int j = 1; j <= m; j++) {
                int cost = t[j - 1] == s[i - 1] ? 0 : 1;
                d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }

            return 1f - (float)d[n, m] / Mathf.Max(s.Length, t.Length);
        }
    }
}
#endif