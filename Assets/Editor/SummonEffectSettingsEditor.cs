using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class SummonEffectSettingsEditor
{
    const string Path="Assets/Resources/TCardField/SummonEffectSettings.asset";
    static SummonEffectSettingsEditor(){EditorApplication.delayCall+=Ensure;}
    static void Ensure(){if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Ensure;return;}if(AssetDatabase.LoadAssetAtPath<SummonEffectSettings>(Path)==null){var value=ScriptableObject.CreateInstance<SummonEffectSettings>();AssetDatabase.CreateAsset(value,Path);AssetDatabase.SaveAssetIfDirty(value);}}
    [MenuItem("Tools/T-card/Summon Effect Settings")]static void Open(){Ensure();Selection.activeObject=AssetDatabase.LoadAssetAtPath<SummonEffectSettings>(Path);}
}

[CustomEditor(typeof(SummonEffectSettings))]
public sealed class SummonEffectSettingsInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("波紋の色は Pulse Color。光柱・液体・波・グローの色は下の設定から変更できます。",MessageType.Info);
        if(GUILayout.Button("自分のチャージ：光柱・液体・グローの色"))
            Selection.activeObject=Resources.Load<LiquidResourceSettings>("TCardResource/LiquidResourceSettings");
        if(GUILayout.Button("敵のチャージ：光柱・液体・グローの色"))
            Selection.activeObject=Resources.Load<LiquidResourceSettings>("TCardResource/EnemyLiquidResourceSettings");
    }
}
