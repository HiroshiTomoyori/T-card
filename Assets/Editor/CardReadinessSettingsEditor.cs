using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class CardReadinessSettingsEditor
{
    const string Path="Assets/Resources/TCardField/CardReadinessSettings.asset";
    static CardReadinessSettingsEditor(){EditorApplication.delayCall+=()=>Ensure();}
    public static CardReadinessSettings Ensure(){var s=AssetDatabase.LoadAssetAtPath<CardReadinessSettings>(Path);if(s!=null)return s;if(EditorApplication.isPlayingOrWillChangePlaymode)return null;s=ScriptableObject.CreateInstance<CardReadinessSettings>();AssetDatabase.CreateAsset(s,Path);AssetDatabase.SaveAssetIfDirty(s);return s;}
    [MenuItem("Tools/T-card/Card Readiness Settings")]
    public static void Open(){Selection.activeObject=Ensure();EditorGUIUtility.PingObject(Selection.activeObject);}
}
