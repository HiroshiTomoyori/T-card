using System;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Serializable values avoid carrying Play Mode instance IDs across domain reloads.
public static class PlayModeEditValue
{
    [Serializable] public sealed class Value
    {
        public SerializedPropertyType type;
        public long integer;
        public double number;
        public bool flag;
        public string text,reference,managedType;
        public Color color;
        public Vector4 vector;
        public Quaternion rotation;
        public Rect rect;
        public Bounds bounds;
        public Vector2Int vector2Int;
        public Vector3Int vector3Int;
        public RectInt rectInt;
        public BoundsInt boundsInt;
        public AnimationCurve curve;
        public Gradient gradient;
    }
    public static Value Read(SerializedProperty p,Func<Object,string> identify)
    {
        var v=new Value{type=p.propertyType};
        switch(p.propertyType)
        {
            case SerializedPropertyType.Integer:v.integer=p.longValue;break;
            case SerializedPropertyType.LayerMask:case SerializedPropertyType.Enum:case SerializedPropertyType.Character:case SerializedPropertyType.ArraySize:v.integer=p.intValue;break;
            case SerializedPropertyType.Boolean:v.flag=p.boolValue;break;
            case SerializedPropertyType.Float:v.number=p.doubleValue;break;
            case SerializedPropertyType.String:v.text=p.stringValue;break;
            case SerializedPropertyType.Color:v.color=p.colorValue;break;
            case SerializedPropertyType.ObjectReference:v.reference=p.objectReferenceValue==null?"":identify(p.objectReferenceValue);if(v.reference==null)return null;break;
            case SerializedPropertyType.ExposedReference:v.reference=p.exposedReferenceValue==null?"":identify(p.exposedReferenceValue);if(v.reference==null)return null;break;
            case SerializedPropertyType.Vector2:v.vector=p.vector2Value;break;
            case SerializedPropertyType.Vector3:v.vector=p.vector3Value;break;
            case SerializedPropertyType.Vector4:v.vector=p.vector4Value;break;
            case SerializedPropertyType.Rect:v.rect=p.rectValue;break;
            case SerializedPropertyType.Bounds:v.bounds=p.boundsValue;break;
            case SerializedPropertyType.Quaternion:v.rotation=p.quaternionValue;break;
            case SerializedPropertyType.Vector2Int:v.vector2Int=p.vector2IntValue;break;
            case SerializedPropertyType.Vector3Int:v.vector3Int=p.vector3IntValue;break;
            case SerializedPropertyType.RectInt:v.rectInt=p.rectIntValue;break;
            case SerializedPropertyType.BoundsInt:v.boundsInt=p.boundsIntValue;break;
            case SerializedPropertyType.AnimationCurve:v.curve=p.animationCurveValue;break;
            case SerializedPropertyType.Gradient:v.gradient=p.gradientValue;break;
            case SerializedPropertyType.Hash128:v.text=p.hash128Value.ToString();break;
            case SerializedPropertyType.ManagedReference:
                v.managedType=p.managedReferenceFullTypename;v.text=p.managedReferenceValue==null?null:JsonUtility.ToJson(p.managedReferenceValue);break;
            default:return null;
        }
        return v;
    }
    public static bool Write(SerializedProperty p,Value v,Func<string,Object> resolve)
    {
        if(p.propertyType!=v.type)return false;
        switch(v.type)
        {
            case SerializedPropertyType.Integer:p.longValue=v.integer;break;
            case SerializedPropertyType.LayerMask:case SerializedPropertyType.Enum:case SerializedPropertyType.Character:case SerializedPropertyType.ArraySize:p.intValue=(int)v.integer;break;
            case SerializedPropertyType.Boolean:p.boolValue=v.flag;break;
            case SerializedPropertyType.Float:p.doubleValue=v.number;break;
            case SerializedPropertyType.String:p.stringValue=v.text;break;
            case SerializedPropertyType.Color:p.colorValue=v.color;break;
            case SerializedPropertyType.ObjectReference:case SerializedPropertyType.ExposedReference:
                Object reference=string.IsNullOrEmpty(v.reference)?null:resolve(v.reference);if(!string.IsNullOrEmpty(v.reference)&&reference==null)return false;
                if(v.type==SerializedPropertyType.ObjectReference)p.objectReferenceValue=reference;else p.exposedReferenceValue=reference;break;
            case SerializedPropertyType.Vector2:p.vector2Value=v.vector;break;
            case SerializedPropertyType.Vector3:p.vector3Value=v.vector;break;
            case SerializedPropertyType.Vector4:p.vector4Value=v.vector;break;
            case SerializedPropertyType.Rect:p.rectValue=v.rect;break;
            case SerializedPropertyType.Bounds:p.boundsValue=v.bounds;break;
            case SerializedPropertyType.Quaternion:p.quaternionValue=v.rotation;break;
            case SerializedPropertyType.Vector2Int:p.vector2IntValue=v.vector2Int;break;
            case SerializedPropertyType.Vector3Int:p.vector3IntValue=v.vector3Int;break;
            case SerializedPropertyType.RectInt:p.rectIntValue=v.rectInt;break;
            case SerializedPropertyType.BoundsInt:p.boundsIntValue=v.boundsInt;break;
            case SerializedPropertyType.AnimationCurve:p.animationCurveValue=v.curve;break;
            case SerializedPropertyType.Gradient:p.gradientValue=v.gradient;break;
            case SerializedPropertyType.Hash128:p.hash128Value=Hash128.Parse(v.text);break;
            case SerializedPropertyType.ManagedReference:
                if(string.IsNullOrEmpty(v.managedType)){p.managedReferenceValue=null;break;}
                int space=v.managedType.IndexOf(' ');if(space<0)return false;
                var type=Type.GetType(v.managedType.Substring(space+1)+", "+v.managedType.Substring(0,space));if(type==null)return false;
                p.managedReferenceValue=JsonUtility.FromJson(v.text,type);break;
            default:return false;
        }
        return true;
    }
}
