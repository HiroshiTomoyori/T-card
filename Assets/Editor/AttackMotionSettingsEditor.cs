using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class AttackMotionSettingsEditor
{
    const string Path="Assets/Resources/TCardField/AttackMotionSettings.asset";
    static AttackMotionSettingsEditor(){EditorApplication.delayCall+=Ensure;}
    public static void Ensure(){if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Ensure;return;}if(AssetDatabase.LoadAssetAtPath<AttackMotionSettings>(Path)==null){var s=ScriptableObject.CreateInstance<AttackMotionSettings>();AssetDatabase.CreateAsset(s,Path);AssetDatabase.SaveAssetIfDirty(s);}}
    [MenuItem("Tools/T-card/Attack Motion Settings")] public static void Open(){Ensure();Selection.activeObject=AssetDatabase.LoadAssetAtPath<AttackMotionSettings>(Path);EditorGUIUtility.PingObject(Selection.activeObject);}
}
