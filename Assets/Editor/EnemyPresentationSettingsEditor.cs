using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class EnemyPresentationSettingsEditor
{
 const string DrawPath="Assets/Resources/TCardDraw/EnemyPresentationSettings.asset",ResourcePath="Assets/Resources/TCardResource/EnemyLiquidResourceSettings.asset";
 static EnemyPresentationSettingsEditor(){EditorApplication.delayCall+=Ensure;}
 public static void Ensure(){if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Ensure;return;}if(!AssetDatabase.IsValidFolder("Assets/Resources/TCardDraw"))AssetDatabase.CreateFolder("Assets/Resources","TCardDraw");
  if(AssetDatabase.LoadAssetAtPath<EnemyPresentationSettings>(DrawPath)==null){var s=ScriptableObject.CreateInstance<EnemyPresentationSettings>();AssetDatabase.CreateAsset(s,DrawPath);AssetDatabase.SaveAssetIfDirty(s);}
  if(AssetDatabase.LoadAssetAtPath<LiquidResourceSettings>(ResourcePath)==null){var original=Resources.Load<LiquidResourceSettings>("TCardResource/LiquidResourceSettings");if(original==null)return;var s=Object.Instantiate(original);s.name="EnemyLiquidResourceSettings";s.panelSize=new Vector2(original.panelSize.x,original.panelSize.y*.72f);s.offset=new Vector2(-160,-100);s.fontSize=original.fontSize*.8f;s.numberOffset=new Vector2(original.numberOffset.x,original.numberOffset.y*.72f);s.dropFromRelease=true;s.dropDistance=90;s.dropCardWidth=90;AssetDatabase.CreateAsset(s,ResourcePath);AssetDatabase.SaveAssetIfDirty(s);}
 }
 [MenuItem("Tools/T-card/Enemy Draw Settings")]public static void Draw(){Ensure();Selection.activeObject=AssetDatabase.LoadAssetAtPath<EnemyPresentationSettings>(DrawPath);EditorGUIUtility.PingObject(Selection.activeObject);}
 [MenuItem("Tools/T-card/Enemy Resource UI Settings")]public static void Resource(){Ensure();Selection.activeObject=AssetDatabase.LoadAssetAtPath<LiquidResourceSettings>(ResourcePath);EditorGUIUtility.PingObject(Selection.activeObject);}
}
