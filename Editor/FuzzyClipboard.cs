#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace FuzzyComponentCopier {
    [Serializable]
    public class FuzzyClipboard {
        public string sourceType;
        public List<FieldValue> values = new List<FieldValue>();

        public static FuzzyClipboard Capture(Component source) {
            var cb = new FuzzyClipboard { sourceType = source.GetType().AssemblyQualifiedName };

            var so = new SerializedObject(source);
            var it = so.GetIterator();
            bool enter = true;

            while (it.NextVisible(enter)) {
                if (it.propertyPath == "m_Script") continue;
                enter = it.hasVisibleChildren && (it.isArray || it.propertyType == SerializedPropertyType.Generic);

                var fv = new FieldValue {
                    propertyPath = it.propertyPath,
                    displayName = it.displayName,
                    niceName = it.displayName,
                    serializedType = it.propertyType,
                    typeName = GetPropertyTypeName(it, source.GetType()) ?? GuessType(it),
                    value = GetPropertyValueAsString(it),
                    references = CollectRefs(it),
                    formerNames = GetFormerlySerializedNames(source.GetType(), it.propertyPath)
                };
                cb.values.Add(fv);
            }

            return cb;
        }

        static List<string> GetFormerlySerializedNames(Type type, string path) {
            var list = new List<string>();
            var fi = FuzzyCopier.GetFieldInfoFromProperty(type, path);

            if (fi != null) {
                var attr = fi.GetCustomAttribute<FormerlySerializedAsAttribute>();
                if (attr != null) list.Add(attr.oldName);
            }

            return list;
        }

        static Object[] CollectRefs(SerializedProperty p) {
            switch (p.propertyType) {
                case SerializedPropertyType.ObjectReference:
                    return p.objectReferenceValue != null
                        ? new[] { p.objectReferenceValue }
                        : Array.Empty<Object>();
                case SerializedPropertyType.ManagedReference:
                    var managedRef = p.managedReferenceValue;

                    if (managedRef != null) {
                        var objects = new List<Object>();

                        if (managedRef is Object obj) {
                            objects.Add(obj);
                        }

                        return EditorUtility.CollectDependencies(objects.ToArray());
                    }

                    return Array.Empty<Object>();
                default:
                    return Array.Empty<Object>();
            }
        }

        static string GetPropertyTypeName(SerializedProperty prop, Type componentType) {
            var fieldInfo = FuzzyCopier.GetFieldInfoFromProperty(componentType, prop.propertyPath);
            return fieldInfo != null ? fieldInfo.FieldType.AssemblyQualifiedName : null;
        }

        static readonly Dictionary<SerializedPropertyType, Func<SerializedProperty, string>> PropertyValueSerializers =
            new Dictionary<SerializedPropertyType, Func<SerializedProperty, string>> {
                { SerializedPropertyType.ManagedReference, SerializeManagedReference },
                { SerializedPropertyType.String, p => p.stringValue },
                { SerializedPropertyType.Integer, p => p.intValue.ToString() },
                { SerializedPropertyType.Float, p => p.floatValue.ToString() },
                { SerializedPropertyType.Boolean, p => p.boolValue.ToString() },
                { SerializedPropertyType.Vector2, p => JsonUtility.ToJson(p.vector2Value) },
                { SerializedPropertyType.Vector3, p => JsonUtility.ToJson(p.vector3Value) },
                { SerializedPropertyType.Vector4, p => JsonUtility.ToJson(p.vector4Value) },
                { SerializedPropertyType.Quaternion, p => JsonUtility.ToJson(p.quaternionValue) },
                { SerializedPropertyType.Color, p => JsonUtility.ToJson(p.colorValue) },
                { SerializedPropertyType.Rect, p => JsonUtility.ToJson(p.rectValue) },
                { SerializedPropertyType.Bounds, p => JsonUtility.ToJson(p.boundsValue) }
            };

        static string GetPropertyValueAsString(SerializedProperty prop) {
            if (PropertyValueSerializers.TryGetValue(prop.propertyType, out var serializer)) {
                return serializer(prop);
            }

            return "{}";
        }

        static string SerializeManagedReference(SerializedProperty prop) {
            var managedRef = prop.managedReferenceValue;

            if (managedRef != null) {
                // Store the actual runtime type for polymorphic support
                var runtimeType = managedRef.GetType();
                var json = JsonUtility.ToJson(managedRef);
                // Store type information with the JSON for polymorphic deserialization
                return $"{{\"$type\":\"{runtimeType.AssemblyQualifiedName}\",\"data\":{json}}}";
            }

            return "{}";
        }

        static string GuessType(SerializedProperty p) => p.propertyType switch {
            SerializedPropertyType.Integer => typeof(int).AssemblyQualifiedName,
            SerializedPropertyType.Float => typeof(float).AssemblyQualifiedName,
            SerializedPropertyType.String => typeof(string).AssemblyQualifiedName,
            SerializedPropertyType.Vector3 => typeof(Vector3).AssemblyQualifiedName,
            _ => "System.Object"
        };
    }
}
#endif