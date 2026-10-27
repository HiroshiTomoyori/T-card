using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class CardPlaySettingsEditor
{
    const string Folder="Assets/Resources/TCardPlay";
    const string Path=Folder+"/CardPlaySettings.asset";
    static CardPlaySettingsEditor(){EditorApplication.delayCall+=()=>Ensure();}
    public static CardPlaySettings Ensure()
    {
        var settings=AssetDatabase.LoadAssetAtPath<CardPlaySettings>(Path);if(settings!=null)return settings;
        if(EditorApplication.isPlayingOrWillChangePlaymode)return null;
        if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Resources","TCardPlay");
        settings=ScriptableObject.CreateInstance<CardPlaySettings>();AssetDatabase.CreateAsset(settings,Path);AssetDatabase.SaveAssets();return settings;
    }
    [MenuItem("Tools/T-card/Card Play Settings")]
    public static void Open(){var value=Ensure();Selection.activeObject=value;EditorGUIUtility.PingObject(value);}
}
