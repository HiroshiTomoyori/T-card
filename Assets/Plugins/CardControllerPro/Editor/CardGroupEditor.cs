using UnityEditor;
using UnityEngine;

namespace CCP
{

[CustomEditor(typeof(CardGroup))]
public class CardGroupEditor : Editor
{
    private const string FeedbackEmailUrl = "mailto:dannydeerboi@gmail.com";
    private const string AssetStoreUrl =
        "https://assetstore.unity.com/packages/tools/gui/card-controller-pro-370860";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(16f);
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold
        };
        if (GUILayout.Button(
                "Got feedback? Feel free to email us!",
                buttonStyle,
                GUILayout.Height(40f)))
        {
            Application.OpenURL(FeedbackEmailUrl);
        }

        if (GUILayout.Button(
                "Please consider leaving a review, it would help us a ton! 🙏",
                buttonStyle,
                GUILayout.Height(40f)))
        {
            Application.OpenURL(AssetStoreUrl);
        }
    }
}

}
