#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FuzzyComponentCopier {
    [CreateAssetMenu(fileName = "FuzzyCopierAliases", menuName = "Tools/Fuzzy Copier Aliases", order = 1)]
    public class FuzzyCopierAliases : ScriptableObject {
        [Serializable]
        public class AliasMapping {
            public string sourceType;
            public string sourceFieldName;
            public string targetFieldName;
        }

        [Serializable]
        public class DefaultMapping {
            public string sourceName;
            public List<string> targetNames = new List<string>();
        }

        [Header("User-Defined Aliases")]
        [Tooltip("Custom aliases created through the Teach Alias feature")]
        public List<AliasMapping> aliases = new List<AliasMapping>();

        [Header("Default Mappings")]
        [Tooltip("Common field name mappings (e.g., Speed ↔ Velocity). These are used when no user alias exists.")]
        public List<DefaultMapping> defaultMappings = new List<DefaultMapping>();

        void OnEnable() {
            if (defaultMappings.Count == 0) {
                InitializeDefaultMappings();
            }
        }

        public void InitializeDefaultMappings() {
            defaultMappings.Clear();
            
            defaultMappings.Add(new DefaultMapping { sourceName = "Speed", targetNames = new List<string> { "Velocity", "MoveSpeed", "MaxSpeed" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "Velocity", targetNames = new List<string> { "Speed", "MoveSpeed", "MaxSpeed" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "Health", targetNames = new List<string> { "HitPoints", "HP", "Life", "HealthPoints" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "HitPoints", targetNames = new List<string> { "Health", "HP", "Life", "HealthPoints" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "Position", targetNames = new List<string> { "Location", "Pos", "Transform" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "Location", targetNames = new List<string> { "Position", "Pos", "Transform" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "Damage", targetNames = new List<string> { "AttackPower", "Attack", "Dmg", "Power" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "AttackPower", targetNames = new List<string> { "Damage", "Attack", "Dmg", "Power" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "CharacterName", targetNames = new List<string> { "Title", "Name", "CharacterName", "DisplayName" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "Title", targetNames = new List<string> { "CharacterName", "Name", "DisplayName" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "IsMoving", targetNames = new List<string> { "CanMove", "Moving", "IsMoving" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "CanMove", targetNames = new List<string> { "IsMoving", "Moving" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "IsActive", targetNames = new List<string> { "Enabled", "Active", "IsEnabled" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "Enabled", targetNames = new List<string> { "IsActive", "Active", "IsEnabled" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "TeamColor", targetNames = new List<string> { "FactionColor", "Color", "TeamColor" } });
            defaultMappings.Add(new DefaultMapping { sourceName = "FactionColor", targetNames = new List<string> { "TeamColor", "Color" } });

            EditorUtility.SetDirty(this);
        }

        public bool IsDefaultMapping(string sourceName, string targetName) {
            var mapping = defaultMappings.FirstOrDefault(m => 
                string.Equals(m.sourceName, sourceName, StringComparison.OrdinalIgnoreCase));
            
            if (mapping != null) {
                return mapping.targetNames.Any(t => string.Equals(t, targetName, StringComparison.OrdinalIgnoreCase));
            }
            
            return false;
        }

        public bool HasAlias(string sourceType, string sourceFieldName, string targetFieldName) {
            return aliases.Exists(a => 
                string.Equals(a.sourceType, sourceType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.sourceFieldName, sourceFieldName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.targetFieldName, targetFieldName, StringComparison.OrdinalIgnoreCase));
        }

        public void AddAlias(string sourceType, string sourceFieldName, string targetFieldName) {
            aliases.RemoveAll(a => 
                string.Equals(a.sourceType, sourceType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.sourceFieldName, sourceFieldName, StringComparison.OrdinalIgnoreCase));

            aliases.Add(new AliasMapping {
                sourceType = sourceType,
                sourceFieldName = sourceFieldName,
                targetFieldName = targetFieldName
            });

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void RemoveAlias(string sourceType, string sourceFieldName) {
            int removed = aliases.RemoveAll(a => 
                string.Equals(a.sourceType, sourceType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.sourceFieldName, sourceFieldName, StringComparison.OrdinalIgnoreCase));
            
            if (removed > 0) {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif

