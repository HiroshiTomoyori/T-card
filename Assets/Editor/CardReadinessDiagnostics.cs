using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class CardReadinessDiagnostics
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;static bool started;static double deadline;
 static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardReadinessValidation");
 static CardReadinessDiagnostics(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/T-card/Validate Card Readiness (Play)")]
 static void Begin(){CardReadinessSettingsEditor.Ensure();SessionState.SetBool("TCardReadinessProbe",true);started=false;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.isPlaying=true;}
 static void Tick(){if(!SessionState.GetBool("TCardReadinessProbe",false)||started||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL: timeout");return;}var d=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(d!=null&&d.IsOpeningDecisionReady){started=true;d.StartCoroutine(Probe(d));}}
 static void Finish(string s){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),s);Debug.Log(s);SessionState.SetBool("TCardReadinessProbe",false);EditorApplication.isPlaying=false;}
 static void Check(bool x,string message){if(!x)throw new Exception(message);}
 static CardController Add(HandDealer d,Transform parent,string rank){var go=UnityEngine.Object.Instantiate(d.proHandCardPrefab,parent);var c=go.GetComponent<CardController>();c.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_"+rank+".asset"));return c;}
 static void Capture(string name){var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name+".png"),im.EncodeToPNG());UnityEngine.Object.Destroy(im);}
 static IEnumerator Probe(HandDealer d)
 {
  Directory.CreateDirectory(Folder);var t=d.turnManager;d.turnManager=null;typeof(HandDealer).GetMethod("CompleteOpeningSelection",Flags).Invoke(d,null);d.turnManager=t;typeof(TurnManager).GetField("inactivityTimersEnabled",Flags).SetValue(t,false);yield return new WaitForSeconds(.4f);
  try{
   foreach(var suit in new[]{"Spade","Heart","Club","Diamond"})foreach(var area in new[]{t.playerBattleArea,t.enemyBattleArea}){
    var go=UnityEngine.Object.Instantiate(d.proHandCardPrefab,area);
    try{
     var four=go.GetComponent<CardController>();four.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/"+suit+"_4.asset"));four.BeginNormalPlay();four.SetSummonSickness(true);
     Check(four.hasSummonSickness&&!CardActionIcon.CanAttack(four),suit+" 4 can attack on summon turn");
     Check(CardActionIcon.CanBlock(four),suit+" 4 lost guard ability");
     four.SetSummonSickness(false);Check(CardActionIcon.CanAttack(four),suit+" 4 cannot attack after readiness clears");
     four.MarkPlayedFromReverse();four.SetSummonSickness(true);Check(!four.hasSummonSickness&&CardActionIcon.CanAttack(four),suit+" reverse 4 lost exemption");
    }finally{UnityEngine.Object.Destroy(go);}
   }
  }catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return null;
  var normal=Add(d,t.playerBattleArea,"10");var guard=Add(d,t.playerBattleArea,"8");var reverse=Add(d,t.playerBattleArea,"Q");var rest=Add(d,t.playerBattleArea,"K");var enemy=Add(d,t.enemyBattleArea,"10");var enemyGuard=Add(d,t.enemyBattleArea,"8");
  yield return null;var layout=t.playerBattleArea.GetComponent<BattleAreaLayout>();var enemyLayout=t.enemyBattleArea.GetComponent<BattleAreaLayout>();layout.Refresh();enemyLayout.Refresh();foreach(var c in new[]{normal,guard,reverse,rest})layout.PlaceImmediately(c);enemyLayout.PlaceImmediately(enemy);enemyLayout.PlaceImmediately(enemyGuard);
  float baseY=normal.transform.localPosition.y,baseScale=normal.transform.localScale.x,enemyY=enemy.transform.localPosition.y;var original=normal.artworkImage.color;var originalRest=rest.artworkImage.color;var cg=normal.GetComponent<CanvasGroup>()??normal.gameObject.AddComponent<CanvasGroup>();cg.alpha=1;
  normal.BeginNormalPlay();normal.SetSummonSickness(true);guard.SetSummonSickness(true);enemyGuard.SetSummonSickness(true);reverse.MarkPlayedFromReverse();reverse.SetSummonSickness(true);rest.Tap();enemy.SetSummonSickness(true);
  layout.PlaceImmediately(normal);enemyLayout.PlaceImmediately(enemy);yield return new WaitForSeconds(.4f);var settings=CardReadinessSettings.Load();
  try{
   Check(normal.hasSummonSickness&&!guard.hasSummonSickness&&!enemyGuard.hasSummonSickness&&!reverse.hasSummonSickness,"Immunity or normal entry incorrect");
   Check(normal.artworkImage.color==original&&Mathf.Approximately(cg.alpha,1),"Readiness changed artwork color/alpha");
   Check(Mathf.Abs(normal.transform.localPosition.y-(baseY-settings.rearOffset))<.1f&&Mathf.Abs(normal.transform.localScale.x-baseScale*settings.preparingScale)<.01f,"Player readiness not rearward/scaled");
   Check(Mathf.Abs(enemy.transform.localPosition.y-(enemyY+settings.rearOffset))<.1f,"Enemy rear offset not mirrored");
   Check(normal.GetComponent<CardReadinessVisual>().IconVisible&&!guard.GetComponent<CardReadinessVisual>().IconVisible&&!reverse.GetComponent<CardReadinessVisual>().IconVisible,"Readiness icon eligibility");
   Check(Quaternion.Angle(rest.tapVisual!=null?rest.tapVisual.localRotation:rest.transform.localRotation,Quaternion.identity)<.1f,"Rest rotated card");
   float brightness=1-settings.restDarkness;Check(Mathf.Abs(rest.artworkImage.color.r-originalRest.r*brightness)<.001f&&rest.artworkImage.color.a==originalRest.a,"Rest tint too dark or alpha changed");
   Check(!CardActionIcon.CanAttack(normal)&&CardActionIcon.CanAttack(reverse),"Attack eligibility changed incorrectly");
   cg.alpha=.73f;normal.SetSummonSickness(false);normal.SetSummonSickness(true);Check(Mathf.Abs(cg.alpha-.73f)<.001f,"Readiness overwrites unrelated fades");normal.GetComponent<CardDrag>().EnablePlacedCardInput();Check(Mathf.Approximately(cg.alpha,1),"Placed card restored old sickness transparency");
   reverse.BeginNormalPlay();reverse.SetSummonSickness(true);Check(reverse.hasSummonSickness,"Normal replay retained reverse exemption");reverse.MarkPlayedFromReverse();
  }catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForEndOfFrame();Capture("preparing-and-rest");
  var routine=(IEnumerator)typeof(TurnManager).GetMethod("EnemyTurnRoutine",Flags).Invoke(t,null);
  try{routine.MoveNext();Check(!normal.hasSummonSickness,"Player readiness not cleared at enemy-turn entry");}catch(Exception e){Finish("FAIL: "+e);yield break;}finally{(routine as IDisposable)?.Dispose();}
  yield return new WaitForSeconds(.4f);
  try{Check(!normal.GetComponent<CardReadinessVisual>().IconVisible&&Mathf.Abs(normal.transform.localPosition.y-baseY)<.1f&&Mathf.Abs(normal.transform.localScale.x-baseScale)<.01f,"Player preparation visual did not return to normal");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForEndOfFrame();Capture("opponent-turn-cleared");
  enemy.BeginNormalPlay();enemy.SetSummonSickness(true);typeof(TurnManager).GetField("isPlayerTurnActive",Flags).SetValue(t,false);
  try{typeof(TurnManager).GetMethod("StartPlayerTurn",Flags).Invoke(t,new object[]{true});Check(!enemy.hasSummonSickness,"Enemy readiness not cleared at player-turn entry");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  Finish("PASS: All four suits of 4 on both sides cannot attack on summon turn, retain guard, can attack after readiness clears, and retain Reverse exemption. Real card prefabs/layout; rest upright with light tint and preserved alpha; readiness full color/opacity with rearward position, subtle size change and icon. Cannot-attack and reverse cards exempt, normal replay resets exemption; both opponent-turn entry points clear readiness before first yield. Placement restores opaque field card. No scene saved.");
 }
}
