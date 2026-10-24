using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CoinTossManager))]
public sealed class CoinTossInspector : Editor
{
    private Editor settingsEditor;
    private bool expanded = true;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var manager = (CoinTossManager)target;
        if (manager.settings == null) return;
        EditorGUILayout.Space();
        expanded = EditorGUILayout.Foldout(expanded, "Coin Settings / 演出の調整", true);
        if (!expanded) return;
        Editor.CreateCachedEditor(manager.settings, null, ref settingsEditor);
        settingsEditor.OnInspectorGUI();
        EditorGUILayout.HelpBox("Duration = 秒数 / Playback Speed = 再生倍率。色・大きさ・遠近・跳ね方・回転・厚みはこのアセットから調整できます。Play中の設定変更もアセットに保存されます。", MessageType.Info);
    }
    private void OnDisable() { if (settingsEditor != null) DestroyImmediate(settingsEditor); }
}
