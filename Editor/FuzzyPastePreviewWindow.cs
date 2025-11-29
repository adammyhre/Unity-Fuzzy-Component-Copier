#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif

namespace FuzzyComponentCopier {
    public class FuzzyPastePreviewWindow : EditorWindow {
        Component target;
        FuzzyClipboard cb;
        Vector2 scroll;
        List<Match> matches = new List<Match>();
        static FuzzyCopierAliases aliasesAsset;

        public void Init(Component t, FuzzyClipboard c) {
            target = t;
            cb = c;
            FindAllPossibleMatches();
        }

        void FindAllPossibleMatches() {
            matches.Clear();
            var targetSO = new SerializedObject(target);
            var targetProps = new List<SerializedProperty>();
            var it = targetSO.GetIterator();
            bool enter = true;

            // Collect all target properties
            while (it.NextVisible(enter)) {
                if (it.propertyPath == "m_Script") continue;
                enter = it.hasVisibleChildren && (it.isArray || it.propertyType == SerializedPropertyType.Generic);
                targetProps.Add(it.Copy());
            }

            // Generate all possible matches with scores
            var allMatches = new List<Match>();
            foreach (var targetProp in targetProps) {
                foreach (var sourceValue in cb.values) {
                    float score = CalculateScore(sourceValue, targetProp);
                    // Only include matches with meaningful scores (very low scores are noise)
                    // This threshold ensures type-incompatible matches with poor name similarity
                    // don't pollute the cost matrix, while still allowing edge cases
                    if (score > 0.1f) {
                        allMatches.Add(new Match {
                            source = sourceValue,
                            target = targetProp,
                            score = score
                        });
                    }
                }
            }

            // Use matching solver for optimal assignment (maximizes total score)
            matches = MatchingSolver.FindOptimalAssignment(allMatches, cb.values, targetProps);
        }

        float CalculateScore(FieldValue src, SerializedProperty dst) {
            var context = new ScoringContext {
                Source = src,
                Target = dst,
                TargetType = GetPropertyTypeFromSerializedProperty(dst, target.GetType()),
                SourceType = cb.sourceType,
                // Try to load aliases asset if it exists; do NOT force creation here
                AliasesAsset = GetAliasesAsset(createIfMissing: false),
                Window = this
            };

            // Try each scoring strategy in order of priority
            foreach (var strategy in GetScoringStrategies()) {
                var score = strategy.CalculateScore(context);
                if (score.HasValue) {
                    return score.Value;
                }
            }

            // Fallback: no match found
            return 0f;
        }

        interface IScoringStrategy {
            float? CalculateScore(ScoringContext context);
        }

        class ScoringContext {
            public FieldValue Source;
            public SerializedProperty Target;
            public Type TargetType;
            public string SourceType;
            public FuzzyCopierAliases AliasesAsset;
            public FuzzyPastePreviewWindow Window;
        }

        List<IScoringStrategy> GetScoringStrategies() {
            return new List<IScoringStrategy> {
                new ExactPathStrategy(),
                new ExactNameStrategy(),
                new UserAliasStrategy(),
                new DefaultMappingStrategy(),
                new FormerlySerializedAsStrategy(),
                new LevenshteinSimilarityStrategy()
            };
        }

        // Helper method for type-aware scoring pattern
        static float? ScoreIfTypeCompatible(
            ScoringContext ctx,
            bool condition,
            float compatibleScore,
            float incompatibleScore,
            bool allowNumericConversion = false,
            bool fullScoreIfSameComponentType = false) {

            if (!condition) return null;

            var typesCompatible = ctx.Window.AreTypesCompatible(ctx.Source.typeName, ctx.TargetType, allowNumericConversion);
            if (!typesCompatible) return incompatibleScore;

            // If we're pasting onto the same component type and the condition hit (e.g. exact name),
            // we can safely consider this a perfect match.
            if (fullScoreIfSameComponentType && IsSameComponentType(ctx)) return 1f;

            return compatibleScore;
        }

        static bool IsSameComponentType(ScoringContext ctx) {
            var srcComponentType = Type.GetType(ctx.SourceType);
            var dstComponentType = ctx.Window.target != null ? ctx.Window.target.GetType() : null;
            return srcComponentType != null && dstComponentType != null && srcComponentType == dstComponentType;
        }

        class ExactPathStrategy : IScoringStrategy {
            public float? CalculateScore(ScoringContext ctx) =>
                ctx.Source.propertyPath == ctx.Target.propertyPath ? 1f : null;
        }

        class ExactNameStrategy : IScoringStrategy {
            public float? CalculateScore(ScoringContext ctx) =>
                ScoreIfTypeCompatible(ctx,
                    string.Equals(ctx.Source.niceName, ctx.Target.displayName, StringComparison.OrdinalIgnoreCase),
                    0.98f,
                    0.90f,
                    allowNumericConversion: false,
                    fullScoreIfSameComponentType: true);
        }

        class UserAliasStrategy : IScoringStrategy {
            public float? CalculateScore(ScoringContext ctx) =>
                ScoreIfTypeCompatible(ctx,
                    ctx.AliasesAsset?.HasAlias(ctx.SourceType, ctx.Source.niceName, ctx.Target.displayName) ?? false,
                    0.95f, 0.75f, allowNumericConversion: true);
        }

        class DefaultMappingStrategy : IScoringStrategy {
            public float? CalculateScore(ScoringContext ctx) =>
                ScoreIfTypeCompatible(ctx,
                    ctx.Window.IsDefaultMapping(ctx.Source.niceName, ctx.Target.displayName),
                    0.92f, 0.70f, allowNumericConversion: true);
        }

        class FormerlySerializedAsStrategy : IScoringStrategy {
            public float? CalculateScore(ScoringContext ctx) =>
                ScoreIfTypeCompatible(ctx,
                    ctx.Source.formerNames.Contains(ctx.Target.name) ||
                    ctx.Source.formerNames.Any(n => ctx.Target.propertyPath.EndsWith((string)n)),
                    0.90f, 0.65f);
        }

        class LevenshteinSimilarityStrategy : IScoringStrategy {
            public float? CalculateScore(ScoringContext ctx) {
                // Always calculate name similarity first
                float nameSimilarity = 1f - LevenshteinDistance.ComputeNormalized(ctx.Source.niceName, ctx.Target.displayName);
                float score = nameSimilarity * 0.80f;

                // Type compatibility check: if types don't match, heavily penalize
                // but don't completely eliminate (allows for edge cases with explicit mappings)
                if (ctx.Window.AreTypesCompatible(ctx.Source.typeName, ctx.TargetType)) {
                    score += 0.10f; // Bonus for type compatibility
                } else {
                    // Heavy penalty for type mismatch - but still allow if name is very similar
                    // This prevents float->int matches unless explicitly allowed via aliases/mappings
                    score *= 0.1f; // 10% of original score
                }

                // Cap at 0.99f (never 100% unless exact path match)
                return Mathf.Min(score, 0.99f);
            }
        }

        bool IsDefaultMapping(string sourceName, string targetName) {
            var aliases = GetAliasesAsset(createIfMissing: false);
            return aliases != null && aliases.IsDefaultMapping(sourceName, targetName);
        }

        void OnGUI() {
            GUILayout.Label("Fuzzy Paste → " + target.GetType().Name, EditorStyles.boldLabel);
            
            // Show which algorithm is being used
            string algorithmName = FuzzyCopierSettings.UseHungarianAlgorithm ? "Hungarian (Optimal)" : "Greedy (Fast)";
            EditorGUILayout.LabelField($"Algorithm: {algorithmName}", EditorStyles.miniLabel);
            
            GUILayout.Label($"Found {matches.Count} intelligent matches:");

            scroll = GUILayout.BeginScrollView(scroll);

            foreach (var m in matches) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"{m.source.niceName}  →  {m.target.displayName}", GUILayout.Width(300));
                GUILayout.Label($"{(m.score * 100):F1}%", GUILayout.Width(60));
                if (DrawButton("Paste", null, GUILayout.Width(50))) ApplySingle(m);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10f);
            EditorGUILayout.BeginVertical();

            if (DrawButton("APPLY ALL MATCHES (Undoable)", new Color(0.25f, 0.8f, 0.25f), GUILayout.Height(24f))) {
                Undo.RecordObject(target, "Fuzzy Paste");
                foreach (var m in matches) ApplySingle(m);
                EditorUtility.SetDirty(target);
                Close();
            }

            EditorGUILayout.Space(4f);

            if (DrawButton("Teach Permanent Alias", new Color(0.25f, 0.7f, 0.9f), GUILayout.Height(22f))) {
                TeachAlias();
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(2f);
            EditorGUILayout.EndHorizontal();
        }

        bool DrawButton(string label, Color? color, params GUILayoutOption[] options) {
            var previousColor = GUI.color;
            if (color.HasValue) {
                GUI.color = color.Value;
            }

            bool clicked;
#if ODIN_INSPECTOR
            clicked = GUILayout.Button(label, SirenixGUIStyles.Button, options);
#else
            clicked = GUILayout.Button(label, options);
#endif
            GUI.color = previousColor;
            return clicked;
        }

        void ApplySingle(Match m) {
            try {
                if (m.target.propertyType == SerializedPropertyType.ObjectReference)
                    m.target.objectReferenceValue = m.source.references?.FirstOrDefault() as Object;
                else if (m.target.propertyType == SerializedPropertyType.ManagedReference)
                    ApplyManagedReference(m.target, m.source.value, m.source.typeName);
                else if (m.target.propertyType == SerializedPropertyType.String)
                    m.target.stringValue = m.source.value;
                else if (m.target.propertyType == SerializedPropertyType.Integer)
                    m.target.intValue = ParseInt(m.source.value);
                else if (m.target.propertyType == SerializedPropertyType.Float)
                    m.target.floatValue = ParseFloat(m.source.value);
                else if (m.target.propertyType == SerializedPropertyType.Boolean)
                    m.target.boolValue = ParseBool(m.source.value);
                else
                    SetPropertyValue(m.target, ConvertValue(
                        ParseValue(m.source.value, m.source.typeName),
                        m.target));

                m.target.serializedObject.ApplyModifiedProperties();
            }
            catch {
                /* silently ignore truly incompatible */
            }
        }

        int ParseInt(string value) {
            if (int.TryParse(value, out int result)) return result;
            if (float.TryParse(value, out float f)) return (int)f;
            return 0;
        }

        float ParseFloat(string value) {
            if (float.TryParse(value, out float result)) return result;
            if (int.TryParse(value, out int i)) return i;
            return 0f;
        }

        bool ParseBool(string value) {
            if (bool.TryParse(value, out bool result)) return result;
            // Also handle "True"/"False" and "1"/"0"
            if (string.Equals(value, "True", StringComparison.OrdinalIgnoreCase) || value == "1") return true;
            if (string.Equals(value, "False", StringComparison.OrdinalIgnoreCase) || value == "0") return false;
            return false;
        }

        void ApplyManagedReference(SerializedProperty prop, string jsonValue, string declaredTypeName) {
            if (string.IsNullOrEmpty(jsonValue) || jsonValue == "{}") {
                prop.managedReferenceValue = null;
                return;
            }

            Type targetType;
        
            // Check if JSON contains type information (for polymorphic types)
            if (jsonValue.Contains("\"$type\"")) {
                try {
                    // Parse the wrapper to get the actual runtime type
                    var wrapper = JsonUtility.FromJson<ManagedReferenceWrapper>(jsonValue);
                    if (!string.IsNullOrEmpty(wrapper.type)) {
                        targetType = Type.GetType(wrapper.type);
                        if (targetType != null && !string.IsNullOrEmpty(wrapper.data)) {
                            // Deserialize using the actual runtime type
                            prop.managedReferenceValue = JsonUtility.FromJson(wrapper.data, targetType);
                            return;
                        }
                    }
                } catch {
                    // Fall through to regular deserialization
                }
            }

            // Fallback to declared type
            targetType = Type.GetType(declaredTypeName);
            if (targetType == null) return;

            try {
                prop.managedReferenceValue = JsonUtility.FromJson(jsonValue, targetType);
            } catch {
                // If that fails, try to find a compatible type
                var fieldType = GetPropertyTypeFromSerializedProperty(prop, target.GetType());
                if (fieldType != null && fieldType != targetType) {
                    try {
                        prop.managedReferenceValue = JsonUtility.FromJson(jsonValue, fieldType);
                    } catch {
                        // Last resort: try base types
                        var baseType = fieldType.BaseType;
                        while (baseType != null && baseType != typeof(object)) {
                            try {
                                prop.managedReferenceValue = JsonUtility.FromJson(jsonValue, baseType);
                                break;
                            } catch {
                                baseType = baseType.BaseType;
                            }
                        }
                    }
                }
            }
        }

        [Serializable]
        class ManagedReferenceWrapper {
            public string type;
            public string data;
        }

        object ParseValue(string value, string typeName) {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(typeName)) return null;

            var type = Type.GetType(typeName);
            if (type == null) return null;

            // Handle primitive types
            if (type == typeof(int)) return ParseInt(value);
            if (type == typeof(float)) return ParseFloat(value);
            if (type == typeof(bool)) return ParseBool(value);
            if (type == typeof(string)) return value;

            // Try JSON deserialization for complex types
            try {
                return JsonUtility.FromJson(value, type);
            } catch {
                return null;
            }
        }

        object ConvertValue(object src, SerializedProperty dst) {
            if (src == null) return null;
            var targetType = GetPropertyTypeFromSerializedProperty(dst, target.GetType());
            if (targetType != null && targetType.IsInstanceOfType(src)) return src;

            // Smart coercion
            if (targetType == typeof(float) && src is int i) return (float)i;
            if (targetType == typeof(int) && src is float f) return (int)f;
            if (targetType == typeof(Vector3) && src is Vector2 v2) return new Vector3(v2.x, v2.y, 0);
            // ... add more as needed

            return src;
        }

        void SetPropertyValue(SerializedProperty prop, object value) {
            if (value == null) return;

            switch (prop.propertyType) {
                case SerializedPropertyType.Integer:
                    if (value is int intVal) prop.intValue = intVal;
                    else if (value is long longVal) prop.intValue = (int)longVal;
                    break;
                case SerializedPropertyType.Float:
                    if (value is float floatVal) prop.floatValue = floatVal;
                    else if (value is double doubleVal) prop.floatValue = (float)doubleVal;
                    else if (value is int intToFloat) prop.floatValue = intToFloat;
                    break;
                case SerializedPropertyType.Boolean:
                    if (value is bool boolVal) prop.boolValue = boolVal; break;
                case SerializedPropertyType.Vector2:
                    if (value is Vector2 v2) prop.vector2Value = v2; break;
                case SerializedPropertyType.Vector3:
                    if (value is Vector3 v3) prop.vector3Value = v3; break;
                case SerializedPropertyType.Vector4:
                    if (value is Vector4 v4) prop.vector4Value = v4; break;
                case SerializedPropertyType.Quaternion:
                    if (value is Quaternion q) prop.quaternionValue = q; break;
                case SerializedPropertyType.Color:
                    if (value is Color c) prop.colorValue = c; break;
                case SerializedPropertyType.Rect:
                    if (value is Rect r) prop.rectValue = r; break;
                case SerializedPropertyType.Bounds:
                    if (value is Bounds b) prop.boundsValue = b; break;
            }
        }

        Type GetPropertyTypeFromSerializedProperty(SerializedProperty prop, Type componentType) {
            try {
                var fieldInfo = FuzzyCopier.GetFieldInfoFromProperty(componentType, prop.propertyPath);
                if (fieldInfo != null) {
                    return fieldInfo.FieldType;
                }
            } catch { }
            return null;
        }

        void TeachAlias() {
            if (matches.Count == 0) {
                EditorUtility.DisplayDialog("Teach Alias", "No matches available to create aliases from.", "OK");
                return;
            }

            // Show a popup to select which match to create an alias for
            var menu = new GenericMenu();
        
            foreach (var match in matches) {
                var m = match; // Capture for closure
                string menuText = $"{m.source.niceName} → {m.target.displayName}";
                bool isAliased = GetAliasesAsset(createIfMissing: false)?.HasAlias(cb.sourceType, m.source.niceName, m.target.displayName) ?? false;
            
                if (isAliased) {
                    menuText += " (Already aliased)";
                    menu.AddDisabledItem(new GUIContent(menuText));
                } else {
                    menu.AddItem(new GUIContent(menuText), false, () => CreateAlias(m));
                }
            }

            menu.ShowAsContext();
        }

        void CreateAlias(Match match) {
            var aliases = GetAliasesAsset(createIfMissing: true);
            if (aliases == null) {
                EditorUtility.DisplayDialog("Error", "Could not load aliases asset. Please create one via Assets > Create > Tools > Fuzzy Copier Aliases", "OK");
                return;
            }

            aliases.AddAlias(cb.sourceType, match.source.niceName, match.target.displayName);
        
            // Refresh matches to show updated scores
            FindAllPossibleMatches();
            Repaint();
        
            EditorUtility.DisplayDialog("Alias Created", 
                $"Created alias: {match.source.niceName} → {match.target.displayName}\n\nThis mapping will be remembered for future pastes.", 
                "OK");
        }

        static FuzzyCopierAliases GetAliasesAsset(bool createIfMissing) {
            if (aliasesAsset != null) return aliasesAsset;

            // Try to find existing asset first
            string[] guids = AssetDatabase.FindAssets("t:FuzzyCopierAliases");
            if (guids.Length > 0) {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                aliasesAsset = AssetDatabase.LoadAssetAtPath<FuzzyCopierAliases>(path);
                // Ensure default mappings are initialized
                if (aliasesAsset != null && aliasesAsset.defaultMappings.Count == 0) {
                    aliasesAsset.InitializeDefaultMappings();
                }
                return aliasesAsset;
            }

            // If we are not allowed to create a new asset, just return null.
            // This allows the tool to function without an aliases asset.
            if (!createIfMissing) return null;

            // No existing asset found - optionally prompt user to create one
            if (EditorUtility.DisplayDialog("Fuzzy Copier Aliases", 
                    "No FuzzyCopierAliases asset found. Would you like to create one?\n\n" +
                    "You can also create one manually via: Assets > Create > Tools > Fuzzy Copier Aliases", 
                    "Create", "Cancel")) {
            
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create Fuzzy Copier Aliases",
                    "FuzzyCopierAliases",
                    "asset",
                    "Choose where to save the FuzzyCopierAliases asset");
            
                if (!string.IsNullOrEmpty(path)) {
                    aliasesAsset = CreateInstance<FuzzyCopierAliases>();
                    aliasesAsset.InitializeDefaultMappings();
                    AssetDatabase.CreateAsset(aliasesAsset, path);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"Created FuzzyCopierAliases asset at {path}");
                    return aliasesAsset;
                }
            }

            return null;
        }

        bool AreTypesCompatible(string srcTypeName, Type dstType, bool allowNumericConversion = false) {
            if (dstType == null) return false;
            var srcType = Type.GetType(srcTypeName);
            if (srcType == null) return false;
        
            // Exact type match
            if (dstType == srcType) return true;
        
            // Allow assignable types (base/derived classes)
            if (dstType.IsAssignableFrom(srcType)) return true;
        
            // Allow int/float conversion if requested (for default mappings and user-defined mappings)
            if (allowNumericConversion) {
                return (dstType == typeof(float) && srcType == typeof(int)) ||
                       (dstType == typeof(int) && srcType == typeof(float));
            }
        
            return false;
        }
    }
}
#endif