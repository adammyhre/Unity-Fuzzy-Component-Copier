#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FuzzyComponentCopier {
    /// <summary>
    /// Displays entries for the Fuzzy Component Copier settings under the preferences window
    /// </summary>
    public class FuzzyCopierSettingsProvider : SettingsProvider {
        GUIContent useHungarianContent = null;

        public FuzzyCopierSettingsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        public override void OnGUI(string searchContext) {
            base.OnGUI(searchContext);

            GUILayout.Space(20f);

            if (useHungarianContent == null) {
                useHungarianContent = new GUIContent(
                    "Use Hungarian Algorithm",
                    "Use the Hungarian algorithm for optimal matching (slower) instead of the Greedy algorithm (faster but potentially suboptimal)");
            }

            bool useHungarian = EditorGUILayout.Toggle(useHungarianContent, FuzzyCopierSettings.UseHungarianAlgorithm);
            if (useHungarian != FuzzyCopierSettings.UseHungarianAlgorithm) {
                FuzzyCopierSettings.UseHungarianAlgorithm = useHungarian;
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider() {
            return new FuzzyCopierSettingsProvider("Preferences/Tools/Fuzzy Component Copier", SettingsScope.User);
        }
    }
}
#endif

