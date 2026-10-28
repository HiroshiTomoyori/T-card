using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class AttackSelectionSettingsEditor
{
    const string Path="Assets/Resources/TCardField/AttackSelectionSettings.asset";
    static AttackSelectionSettingsEditor(){EditorApplication.delayCall+=()=>Ensure();}
    public static AttackSelectionSettings Ensure(){var s=AssetDatabase.LoadAssetAtPath<AttackSelectionSettings>(Path);if(s!=null){UpgradeContrast(s);return s;}if(EditorApplication.isPlayingOrWillChangePlaymode)return null;s=ScriptableObject.CreateInstance<AttackSelectionSettings>();s.contrastRevision=1;AssetDatabase.CreateAsset(s,Path);AssetDatabase.SaveAssetIfDirty(s);return s;}
    static void UpgradeContrast(AttackSelectionSettings s){if(s.contrastRevision>=1)return;var color=s.lowlightColor;color.a=Mathf.Max(.5f,color.a);s.lowlightColor=color;s.contrastRevision=1;EditorUtility.SetDirty(s);AssetDatabase.SaveAssetIfDirty(s);}
    [MenuItem("Tools/T-card/Attack Selection Settings")]
    public static void Open(){Selection.activeObject=Ensure();EditorGUIUtility.PingObject(Selection.activeObject);}
}
