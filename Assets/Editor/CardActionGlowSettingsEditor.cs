using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class CardActionGlowSettingsEditor
{
    const string Path="Assets/Resources/TCardField/CardActionGlowSettings.asset";
    static CardActionGlowSettingsEditor(){EditorApplication.delayCall+=()=>Ensure();}
    public static CardActionGlowSettings Ensure()
    {
        var s=AssetDatabase.LoadAssetAtPath<CardActionGlowSettings>(Path);if(s!=null)return s;
        if(EditorApplication.isPlayingOrWillChangePlaymode)return null;
        if(!AssetDatabase.IsValidFolder("Assets/Resources/TCardField"))AssetDatabase.CreateFolder("Assets/Resources","TCardField");
        s=ScriptableObject.CreateInstance<CardActionGlowSettings>();AssetDatabase.CreateAsset(s,Path);AssetDatabase.SaveAssetIfDirty(s);return s;
    }
    [MenuItem("Tools/T-card/Card Action Glow Settings")]
    public static void Open(){var s=Ensure();Selection.activeObject=s;EditorGUIUtility.PingObject(s);}
}
