using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class FieldHighlightSettingsEditor
{
    const string Folder="Assets/Resources/TCardField";
    static FieldHighlightSettingsEditor(){EditorApplication.delayCall+=()=>Ensure();}
    public static FieldHighlightSettings Ensure()
    {
        var settings=AssetDatabase.LoadAssetAtPath<FieldHighlightSettings>(Folder+"/FieldHighlightSettings.asset");if(settings!=null)return settings;
        if(EditorApplication.isPlayingOrWillChangePlaymode)return null;
        if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Resources","TCardField");
        settings=ScriptableObject.CreateInstance<FieldHighlightSettings>();AssetDatabase.CreateAsset(settings,Folder+"/FieldHighlightSettings.asset");AssetDatabase.SaveAssetIfDirty(settings);return settings;
    }
    [MenuItem("Tools/T-card/Field Highlight Settings")]
    public static void Open(){var settings=Ensure();Selection.activeObject=settings;EditorGUIUtility.PingObject(settings);}
}
