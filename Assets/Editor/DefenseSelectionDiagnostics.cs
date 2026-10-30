using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
[InitializeOnLoad]
public static class DefenseSelectionDiagnostics
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;static bool started,awaitingDirectResult;static double deadline;
 static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardDefenseSelectionValidation");
 static DefenseSelectionDiagnostics(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/T-card/Validate Defense Selection (Play)")]
 static void Begin(){DefenseSelectionSettingsEditor.Ensure();SessionState.SetBool("TCardDefenseProbe",true);started=false;awaitingDirectResult=false;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.isPlaying=true;}
 static void Tick(){if(awaitingDirectResult&&EditorApplication.isPlaying){var turn=UnityEngine.Object.FindFirstObjectByType<TurnManager>();if(turn!=null&&turn.IsBattleEnded){awaitingDirectResult=false;Finish("PASS: Eligible guards still require block/pass confirmation; rested-only guards, no-guard walls and direct attacks resolve without pass prompts. Shared Attack Selection Settings drive defense colors, surface highlight, glow and stronger lowlight. Confirmation isolates attacker and actual defender/targets. No scene saved.");return;}}if(!SessionState.GetBool("TCardDefenseProbe",false)||started||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL: timeout");return;}var d=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(d!=null&&d.IsOpeningDecisionReady){started=true;d.StartCoroutine(Probe(d));}}
 static void Finish(string s){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),s);Debug.Log(s);SessionState.SetBool("TCardDefenseProbe",false);EditorApplication.isPlaying=false;}
 static void Check(bool x,string s){if(!x)throw new Exception(s);}
 static void Set(TurnManager t,string n,object v){typeof(TurnManager).GetField(n,Flags).SetValue(t,v);}
 static IEnumerator Call(TurnManager t,string n,params object[] args)=>(IEnumerator)typeof(TurnManager).GetMethod(n,Flags).Invoke(t,args);
 static CardController Add(HandDealer d,Transform area,string rank,bool tapped=false){var go=UnityEngine.Object.Instantiate(d.proHandCardPrefab,area);var c=go.GetComponent<CardController>();c.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_"+rank+".asset"));c.isTapped=tapped;c.hasSummonSickness=false;return c;}
 static Vector2 Center(CardController c)=>RectTransformUtility.WorldToScreenPoint(null,(c.artworkImage!=null?c.artworkImage.rectTransform:(RectTransform)c.transform).position);
 static void Click(Vector2 p){Canvas.ForceUpdateCanvases();var e=new PointerEventData(EventSystem.current){position=p,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);Check(hits.Count>0,"No raycast");var handled=ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerClickHandler);File.AppendAllText(Path.Combine(Folder,"clicks.txt"),p+" "+hits[0].gameObject.name+" -> "+(handled!=null?handled.name:"none")+"\n");}
 static void Click(UnityEngine.UI.Button b)=>Click(RectTransformUtility.WorldToScreenPoint(null,b.transform.position));
 static void Capture(string n){var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,n+".png"),im.EncodeToPNG());UnityEngine.Object.Destroy(im);}
 static int LiveWalls(HandDealer d)=>d.wallArea.Cast<Transform>().Count(x=>x.GetComponent<CardController>()!=null&&(x.GetComponent<CanvasGroup>()==null||x.GetComponent<CanvasGroup>().alpha>.01f));
 static IEnumerator Probe(HandDealer d)
 {
  Directory.CreateDirectory(Folder);var t=d.turnManager;d.turnManager=null;typeof(HandDealer).GetMethod("CompleteOpeningSelection",Flags).Invoke(d,null);d.turnManager=t;yield return new WaitForSeconds(.5f);
  Set(t,"isPlayerTurnActive",false);Set(t,"isPlayerMainPhase",false);Set(t,"isEndingTurn",false);Set(t,"actionGlowTurn",2);Set(t,"inactivityTimersEnabled",false);t.enemyAIBrain=null;
  var target=Add(d,t.playerBattleArea,"2",true);var guard=Add(d,t.playerBattleArea,"8");var guard2=Add(d,t.playerBattleArea,"8");var king=Add(d,t.enemyBattleArea,"K");var normal=Add(d,t.enemyBattleArea,"10",true);
  yield return null;foreach(var area in new[]{t.playerBattleArea,t.enemyBattleArea}){var layout=area.GetComponent<BattleAreaLayout>();layout.Refresh();foreach(var c in area.GetComponentsInChildren<CardController>())if(c.transform.parent==area)layout.PlaceImmediately(c);}
  t.StartCoroutine(Call(t,"EnemyAttackRestedCardRoutine",king,target));yield return new WaitForSeconds(.9f);var view=t.GetComponent<DefenseSelectionPresentation>();
  try{Check(t.DefenseSelectionActive&&view!=null,"Field defense did not open");Check(view.HighlightedGuardCount==2,"Eligible guard highlight count");Check(view.Graphic.material.GetColor("_DimColor")==AttackSelectionSettings.Load().lowlightColor&&view.Graphic.material.GetColor("_DimColor").a>=.5f,"Shared stronger lowlight absent");Check(view.CurrentTargetColor==AttackSelectionSettings.Load().monarchTargetColor,"Monarch target not purple");Check(view.CurrentAttackerColor==AttackSelectionSettings.Load().attackerColor,"Enemy attacker not configured emerald");Check(view.PassButton.gameObject.activeSelf,"Pass button absent");Check(t.noBlockButton==null||!t.noBlockButton.activeSelf,"Legacy pass button still visible");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForEndOfFrame();Capture("monarch-field-defense");
  try{Click(Center(guard));Check(t.HasPendingDefenseConfirmation&&t.DefenseSelectionActive&&guard.transform.parent==t.playerBattleArea&&target.transform.parent==t.playerBattleArea,"Block ran before confirmation");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return null;
  try{Click(view.BackButton);Check(!t.HasPendingDefenseConfirmation&&t.DefenseSelectionActive,"Back did not preserve defense choice");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return null;
  try{Click(Center(guard));Check(t.HasPendingDefenseConfirmation,"Second guard selection failed");}catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return null;yield return new WaitForEndOfFrame();Check(view.HighlightedGuardCount==1&&view.Graphic.material.GetInt("_Count")==2,"Defense confirmation lights unrelated cards");Capture("block-confirmation");
  try{Click(view.ConfirmButton);Check(!t.DefenseSelectionActive,"Confirmed guard did not execute");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSeconds(AttackMotionSettings.Load().Duration/AttackMotionSettings.Load().speed+.3f);
  try{Check(target.transform.parent==t.playerBattleArea&&guard.transform.parent!=t.playerBattleArea,"Guard did not protect field target");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  normal.isTapped=false;t.StartCoroutine(Call(t,"EnemyAttackRestedCardRoutine",normal,target));yield return new WaitForSeconds(.85f);
  try{Check(t.DefenseSelectionActive&&view.CurrentTargetColor==AttackSelectionSettings.Load().normalTargetColor,"Normal attack not red");Click(view.PassButton);Check(t.HasPendingDefenseConfirmation&&target.transform.parent==t.playerBattleArea,"Pass ran before confirmation");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return null;
  try{Click(view.BackButton);Check(t.DefenseSelectionActive&&!t.HasPendingDefenseConfirmation,"Pass cancel failed");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return null;
  try{Click(view.PassButton);Check(t.HasPendingDefenseConfirmation,"Second pass selection failed");}catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return null;
  try{Click(view.ConfirmButton);}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSeconds(AttackMotionSettings.Load().Duration/AttackMotionSettings.Load().speed+.3f);
  try{Check(target.transform.parent!=t.playerBattleArea&&guard2.transform.parent==t.playerBattleArea,"Pass did not resolve original field target");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  king.isTapped=false;t.StartCoroutine(Call(t,"EnemyBattlePhase"));yield return new WaitForSeconds(.9f);int before=LiveWalls(d);var chosen=t.IncomingWallTargets();
  try{Check(t.DefenseSelectionActive&&chosen.Count==2&&view.HighlightedWallCount==2,"Monarch two-wall targets were not frozen/highlighted");Check(view.CurrentTargetColor==AttackSelectionSettings.Load().monarchTargetColor,"Wall monarch not purple");Click(Center(guard2));Check(t.HasPendingDefenseConfirmation&&LiveWalls(d)==before,"Wall block ran before confirmation");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForEndOfFrame();Capture("monarch-wall-defense");yield return null;
  try{Click(view.ConfirmButton);}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSeconds(AttackMotionSettings.Load().Duration/AttackMotionSettings.Load().speed+.3f);
  try{Check(LiveWalls(d)==before&&guard2.transform.parent!=t.playerBattleArea,"Blocking monarch did not protect both walls");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  // A rested guard is not eligible and must never produce a pass prompt.
  var restedGuard=Add(d,t.playerBattleArea,"8",true);normal.isTapped=false;
  t.StartCoroutine(Call(t,"EnemyAttackRestedCardRoutine",normal,restedGuard));yield return new WaitForSeconds(.75f);
  try{Check(!t.DefenseSelectionActive&&!t.HasPendingDefenseConfirmation&&!view.PassButton.gameObject.activeSelf,"Rested-only guard produced a pass prompt");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSeconds(AttackMotionSettings.Load().Duration/AttackMotionSettings.Load().speed+.3f);
  try{Check(restedGuard.transform.parent!=t.playerBattleArea,"Rested guard target did not resolve automatically");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  foreach(Transform w in d.wallArea){var c=w.GetComponent<CardController>();if(c!=null){var data=UnityEngine.Object.Instantiate(c.data);data.effectTypes=Array.Empty<EffectType>();c.data=data;}}
  king.isTapped=false;t.StartCoroutine(Call(t,"EnemyBattlePhase"));yield return new WaitForSeconds(.8f);
  try{Check(!t.DefenseSelectionActive&&!t.HasPendingDefenseConfirmation,"No-guard wall attack requested confirmation");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSeconds(AttackMotionSettings.Load().Duration/AttackMotionSettings.Load().speed+1.5f);
  try{Check(LiveWalls(d)==before-2,"Automatic monarch damage did not break exactly two walls");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  foreach(Transform w in d.wallArea){var cg=w.GetComponent<CanvasGroup>();if(cg!=null){cg.alpha=0;cg.blocksRaycasts=false;}}
  normal.isTapped=false;awaitingDirectResult=true;t.StartCoroutine(Call(t,"EnemyBattlePhase"));
 }
}
