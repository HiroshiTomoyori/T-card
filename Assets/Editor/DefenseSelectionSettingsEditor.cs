using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class DefenseSelectionSettingsEditor
{
    const string Path="Assets/Resources/TCardField/DefenseSelectionSettings.asset";
    static DefenseSelectionSettingsEditor(){EditorApplication.delayCall+=()=>Ensure();}
    public static DefenseSelectionSettings Ensure(){var s=AssetDatabase.LoadAssetAtPath<DefenseSelectionSettings>(Path);if(s!=null)return s;if(EditorApplication.isPlayingOrWillChangePlaymode)return null;s=ScriptableObject.CreateInstance<DefenseSelectionSettings>();AssetDatabase.CreateAsset(s,Path);AssetDatabase.SaveAssetIfDirty(s);return s;}
    [MenuItem("Tools/T-card/Defense Selection Settings")]
    public static void Open(){Selection.activeObject=Ensure();EditorGUIUtility.PingObject(Selection.activeObject);}
}

[CustomEditor(typeof(DefenseSelectionSettings))]
public sealed class DefenseSelectionSettingsInspector:Editor
{
    public override void OnInspectorGUI(){EditorGUILayout.HelpBox("色・発光・ハイライト・ローライトは Attack Selection Settings と共通です。",MessageType.Info);if(GUILayout.Button("Attack Selection Settings を開く"))AttackSelectionSettingsEditor.Open();DrawDefaultInspector();}
}
