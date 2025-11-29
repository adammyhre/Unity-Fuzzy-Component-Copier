#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using FuzzyComponentCopier;

[CustomEditor(typeof(FuzzyCopierAliases))]
public class FuzzyCopierAliasesEditor : Editor {
    public override void OnInspectorGUI() {
        var asset = target as FuzzyCopierAliases;
        
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Reset Default Mappings to Factory Defaults", GUILayout.Height(30))) {
            if (EditorUtility.DisplayDialog("Reset Default Mappings", 
                "This will reset all default mappings to the factory defaults. Continue?", 
                "Yes", "Cancel")) {
                asset.InitializeDefaultMappings();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Default Mappings: These are common field name mappings used when no user alias exists.\n\n" +
            "User Aliases: Custom mappings created through the 'Teach Alias' feature in the paste preview window.",
            MessageType.Info);
    }
}
#endif

