using UnityEngine;
using System;

namespace CCP
{

/// <summary>
/// Conditionally shows a field in the Inspector based on a boolean or enum value.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class ShowIfAttribute : PropertyAttribute
{
    public string ConditionFieldName { get; private set; }
    public object[] CompareValues { get; private set; }
    public bool HasCompareValue { get; private set; }
    public string OrField { get; set; }

    /// <summary>
    /// Show field when the specified boolean field is true.
    /// </summary>
    /// <param name="conditionFieldName">Name of the boolean field to check.</param>
    public ShowIfAttribute(string conditionFieldName)
    {
        ConditionFieldName = conditionFieldName;
        HasCompareValue = false;
    }

    /// <summary>
    /// Show field when the specified field equals the given value.
    /// Works with enums, ints, and other comparable types.
    /// </summary>
    /// <param name="conditionFieldName">Name of the field to check.</param>
    /// <param name="compareValue">Value to compare against.</param>
    public ShowIfAttribute(string conditionFieldName, object compareValue)
    {
        ConditionFieldName = conditionFieldName;
        CompareValues = new object[] { compareValue };
        HasCompareValue = true;
    }

    /// <summary>
    /// Show field when the specified field equals any of the given values (OR logic).
    /// </summary>
    public ShowIfAttribute(string conditionFieldName, object compareValue1, object compareValue2)
    {
        ConditionFieldName = conditionFieldName;
        CompareValues = new object[] { compareValue1, compareValue2 };
        HasCompareValue = true;
    }
}

}
