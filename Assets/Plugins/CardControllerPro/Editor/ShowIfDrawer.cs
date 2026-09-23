using UnityEngine;
using UnityEditor;

namespace CCP
{

[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShouldShow(property))
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (ShouldShow(property))
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }

        // Collapse the space if the condition is false
        return -EditorGUIUtility.standardVerticalSpacing;
    }

    private bool ShouldShow(SerializedProperty property)
    {
        ShowIfAttribute showIf = (ShowIfAttribute)attribute;
        SerializedProperty conditionProperty = property.serializedObject.FindProperty(showIf.ConditionFieldName);

        if (conditionProperty == null)
        {
            Debug.LogWarning($"ShowIf: Could not find property '{showIf.ConditionFieldName}'");
            return true; // Show by default if condition field not found
        }

        bool primaryResult;
        if (showIf.HasCompareValue)
        {
            // Show if the property matches any of the compare values (OR logic)
            primaryResult = false;
            foreach (object value in showIf.CompareValues)
            {
                if (ComparePropertyValue(conditionProperty, value))
                {
                    primaryResult = true;
                    break;
                }
            }
        }
        else
        {
            // Boolean check
            primaryResult = conditionProperty.boolValue;
        }

        if (primaryResult)
            return true;

        // Check optional OrField (boolean) as a secondary OR condition
        if (!string.IsNullOrEmpty(showIf.OrField))
        {
            SerializedProperty orProperty = property.serializedObject.FindProperty(showIf.OrField);
            if (orProperty != null)
                return orProperty.boolValue;
        }

        return false;
    }

    private bool ComparePropertyValue(SerializedProperty property, object compareValue)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Enum:
                // Handle enum comparison
                if (compareValue is int intValue)
                {
                    return property.enumValueIndex == intValue;
                }
                // Handle enum value passed as the enum type itself
                if (compareValue != null && compareValue.GetType().IsEnum)
                {
                    return property.enumValueIndex == (int)compareValue;
                }
                return false;

            case SerializedPropertyType.Integer:
                if (compareValue is int intCompare)
                {
                    return property.intValue == intCompare;
                }
                return false;

            case SerializedPropertyType.Boolean:
                if (compareValue is bool boolCompare)
                {
                    return property.boolValue == boolCompare;
                }
                return false;

            case SerializedPropertyType.String:
                if (compareValue is string stringCompare)
                {
                    return property.stringValue == stringCompare;
                }
                return false;

            default:
                Debug.LogWarning($"ShowIf: Unsupported property type '{property.propertyType}'");
                return true;
        }
    }
}

}
