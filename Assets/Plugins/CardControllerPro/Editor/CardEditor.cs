using UnityEngine;
using UnityEditor;

namespace CCP
{

[CustomEditor(typeof(Card))]
public class CardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SerializedProperty cardDataProp = serializedObject.FindProperty("cardData");
        if (cardDataProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "No CardData assigned. Assign a CardData asset to configure card type and animations.",
                MessageType.Warning
            );
        }
    }
}

}
