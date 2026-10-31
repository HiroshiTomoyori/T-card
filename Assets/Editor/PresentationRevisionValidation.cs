using System;
using System.IO;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class PresentationRevisionValidation
{
 const string Key="TCard.PresentationRevisionSuite.";
 static readonly string[] Menus={"Validate Summon Sequences (Play)","Validate Card Readiness (Play)","Validate Discard and Enemy Hand (Play)","Validate Enemy Presentation (Play)","Validate Attack Motion (Play)","Validate Field Geometry (Play)","Validate Act Resolution (Play)","Validate Attack Selection (Play)"};
 static readonly string[] Reports={"TCardSummonSequenceValidation","TCardReadinessValidation","TCardDiscardDisplayValidation","TCardEnemyPresentationValidation","TCardAttackMotionValidation","TCardFieldGeometryValidation","TCardActResolutionValidation","TCardAttackSelectionValidation"};
 static PresentationRevisionValidation(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/T-card/Run Presentation Checks",false,-10)]
 static void Begin(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;SessionState.SetInt(Key+"First",0);SessionState.SetInt(Key+"Limit",Menus.Length);SessionState.SetInt(Key+"Index",0);SessionState.SetString(Key+"State","start");}
 [MenuItem("Tools/T-card/Check Summon Effects",false,-9)]
 static void SummonOnly(){Begin();if(!EditorApplication.isPlayingOrWillChangePlaymode)SessionState.SetInt(Key+"Limit",1);}
 [MenuItem("Tools/T-card/Check Discard Alignment",false,-8)]
 static void DiscardOnly(){Begin();if(!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt(Key+"Index",2);SessionState.SetInt(Key+"First",2);SessionState.SetInt(Key+"Limit",3);}}
 [MenuItem("Tools/T-card/Check Field Card Sizes",false,-7)]
 static void GeometryOnly(){Begin();if(!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt(Key+"Index",5);SessionState.SetInt(Key+"First",5);SessionState.SetInt(Key+"Limit",6);}}
 [MenuItem("Tools/T-card/Check Act and Wall Revisions",false,-6)]
 static void ActAndWalls(){Begin();if(!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt(Key+"Index",3);SessionState.SetInt(Key+"First",3);SessionState.SetInt(Key+"Limit",Menus.Length);}}
 static void Tick()
 {
  string state=SessionState.GetString(Key+"State","");if(state==""||EditorApplication.isCompiling)return;int index=SessionState.GetInt(Key+"Index",0);
  if(state=="start"&&!EditorApplication.isPlayingOrWillChangePlaymode){if(index>=SessionState.GetInt(Key+"Limit",Menus.Length)){SessionState.SetString(Key+"State","");Debug.Log("PASS: Presentation revision suite complete ("+(index-SessionState.GetInt(Key+"First",0))+" validations). No scene saved.");return;}string path=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp",Reports[index],"result.txt");if(File.Exists(path)){if(File.Exists(path+".previous"))File.Delete(path+".previous");File.Move(path,path+".previous");}SessionState.SetFloat(Key+"Start",(float)EditorApplication.timeSinceStartup);SessionState.SetBool(Key+"Opened",false);SessionState.SetString(Key+"State","running");EditorApplication.ExecuteMenuItem("Tools/T-card/"+Menus[index]);return;}
  if(state!="running")return;
  if(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+"Start",0)>140){SessionState.SetString(Key+"State","");Debug.LogError("FAIL: suite timeout at "+Menus[index]);EditorApplication.isPlaying=false;return;}
  if(EditorApplication.isPlaying&&EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+"Start",0)>2&&!SessionState.GetBool(Key+"Opened",false)){var title=UnityEngine.Object.FindFirstObjectByType<TitleManager>();if(title!=null){SessionState.SetBool(Key+"Opened",true);title.SelectNormal();}}
  if(!EditorApplication.isPlayingOrWillChangePlaymode){string path=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp",Reports[index],"result.txt");if(!File.Exists(path))return;string report=File.ReadAllText(path);if(!report.StartsWith("PASS:")){SessionState.SetString(Key+"State","");Debug.LogError("Suite stopped: "+report);return;}SessionState.SetInt(Key+"Index",index+1);SessionState.SetString(Key+"State","start");}
 }
}
