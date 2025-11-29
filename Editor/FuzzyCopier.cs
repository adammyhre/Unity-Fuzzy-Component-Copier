#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FuzzyComponentCopier {
    public static class FuzzyCopier {
        static FuzzyClipboard clipboard;

        // Static helper for getting field info from property path
        public static FieldInfo GetFieldInfoFromProperty(Type type, string propertyPath) {
            var parts = propertyPath.Split('.');
            FieldInfo fieldInfo = null;
            Type currentType = type;

            foreach (var part in parts) {
                if (part == "Array" || part.StartsWith("data[")) continue;

                fieldInfo = currentType?.GetField(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fieldInfo == null) return null;

                currentType = fieldInfo.FieldType;

                if (currentType.IsArray) {
                    currentType = currentType.GetElementType();
                }
            }

            return fieldInfo;
        }

        [MenuItem("CONTEXT/Component/Fuzzy Copy Values", priority = 100)]
        static void Copy(MenuCommand cmd) {
            var component = cmd.context as Component;

            if (component == null) {
                EditorUtility.DisplayDialog("Fuzzy Copy", "Invalid component selected!", "OK");
                return;
            }

            clipboard = FuzzyClipboard.Capture(component);
        }

        [MenuItem("CONTEXT/Component/Fuzzy Paste Values", priority = 101)]
        static void Paste(MenuCommand cmd) {
            var target = cmd.context as Component;

            if (target == null) {
                EditorUtility.DisplayDialog("Fuzzy Paste", "Invalid component selected!", "OK");
                return;
            }

            if (clipboard == null) {
                EditorUtility.DisplayDialog("Fuzzy Paste", "Nothing copied yet!", "OK");
                return;
            }

            var window = EditorWindow.GetWindow<FuzzyPastePreviewWindow>(true, "Fuzzy Paste Preview", true);
            window.Init(target, clipboard);
        }

        [MenuItem("CONTEXT/Component/Fuzzy Paste Values", true)]
        static bool PasteValidate() => clipboard != null;
    }
}
#endif