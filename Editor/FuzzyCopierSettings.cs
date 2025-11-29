#if UNITY_EDITOR
using UnityEditor;

namespace FuzzyComponentCopier {
    /// <summary>
    /// Holds the settings for the Fuzzy Component Copier
    /// </summary>
    public static class FuzzyCopierSettings {
        const string USE_HUNGARIAN_ALGORITHM_PREF = "FuzzyComponentCopier.UseHungarianAlgorithm";
        static bool? useHungarianAlgorithm;

        /// <summary>
        /// Whether to use the Hungarian algorithm (optimal but slower) or Greedy algorithm (fast but potentially suboptimal).
        /// Default is false (Greedy).
        /// </summary>
        public static bool UseHungarianAlgorithm {
            get {
                useHungarianAlgorithm ??= EditorPrefs.GetBool(USE_HUNGARIAN_ALGORITHM_PREF, false);
                return useHungarianAlgorithm.Value;
            }
            set {
                useHungarianAlgorithm = value;
                EditorPrefs.SetBool(USE_HUNGARIAN_ALGORITHM_PREF, useHungarianAlgorithm.Value);
            }
        }
    }
}
#endif

