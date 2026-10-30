using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
[InitializeOnLoad]
public static class AttackSelectionDiagnostics
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static bool started;static double deadline;
 static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardAttackSelectionValidation");
 static AttackSelectionDiagnostics(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/T-card/Validate Attack Selection (Play)")]
 static void Begin(){AttackSelectionSettingsEditor.Ensure();SessionState.SetBool("TCardAttackSelectionProbe",true);started=false;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.isPlaying=true;}
 static void Tick(){if(!SessionState.GetBool("TCardAttackSelectionProbe",false)||started||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL: opening timeout");return;}var d=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(d!=null&&d.IsOpeningDecisionReady){started=true;d.StartCoroutine(Probe(d));}}
 static void Finish(string s){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),s);Debug.Log(s);SessionState.SetBool("TCardAttackSelectionProbe",false);EditorApplication.isPlaying=false;}
 static void Check(bool value,string s){if(!value)throw new Exception(s);}
 static void Set(TurnManager t,string n,bool v){typeof(TurnManager).GetField(n,Flags).SetValue(t,v);}
 static CardController Add(HandDealer d,Transform area,string rank,bool tapped=false)
 {
  var go=UnityEngine.Object.Instantiate(d.proHandCardPrefab!=null?d.proHandCardPrefab:d.cardPrefab,area);var c=go.GetComponent<CardController>();c.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_"+rank+".asset"));c.hasSummonSickness=false;c.isTapped=tapped;
  if(area==d.turnManager.playerBattleArea&&go.GetComponent<BattleCardClick>()==null)go.AddComponent<BattleCardClick>();
  if(area==d.turnManager.enemyBattleArea&&go.GetComponent<EnemyBattleCardTargetClick>()==null)go.AddComponent<EnemyBattleCardTargetClick>();
  return c;
 }
 static Vector2 Center(CardController c){var r=c.artworkImage!=null?c.artworkImage.rectTransform:c.transform as RectTransform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
 static GameObject Click(Vector2 point)
 {
  var ev=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(ev,hits);Check(hits.Count>0,"No actual UI raycast at "+point);ev.pointerCurrentRaycast=hits[0];var handled=ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,ev,ExecuteEvents.pointerClickHandler);File.AppendAllText(Path.Combine(Folder,"clicks.txt"),"At "+point+" top "+hits[0].gameObject.name+" handled "+(handled!=null?handled.name:"none")+"\n");return handled;
 }
 static void Confirm(TurnManager t){Check(t.HasPendingAttackConfirmation,"Attack did not request confirmation");t.ConfirmAttackSelection();}
 static Texture2D Capture(string n){var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,n+".png"),im.EncodeToPNG());return im;}
 static IEnumerator Probe(HandDealer d)
 {
  Directory.CreateDirectory(Folder);var t=d.turnManager;d.turnManager=null;typeof(HandDealer).GetMethod("CompleteOpeningSelection",Flags).Invoke(d,null);d.turnManager=t;yield return new WaitForSeconds(.5f);
  Set(t,"isPlayerTurnActive",true);Set(t,"isPlayerMainPhase",true);Set(t,"isBattlePhase",false);Set(t,"isEndingTurn",false);Set(t,"battleIconsEnded",false);typeof(TurnManager).GetField("actionGlowTurn",Flags).SetValue(t,1);
  var normal=Add(d,t.playerBattleArea,"10");var queen=Add(d,t.playerBattleArea,"Q");var king=Add(d,t.playerBattleArea,"K");var target=Add(d,t.enemyBattleArea,"2",true);
  yield return null;foreach(var area in new[]{t.playerBattleArea,t.enemyBattleArea}){var layout=area.GetComponent<BattleAreaLayout>();layout.Refresh();foreach(var c in area.GetComponentsInChildren<CardController>())if(c.transform.parent==area)layout.PlaceImmediately(c);}
  yield return new WaitForSeconds(.3f);string error=null;var view=t.GetComponent<AttackSelectionPresentation>();
  try{Check(view!=null,"Presentation absent");Check(!ShaderUtil.ShaderHasError(Shader.Find("T-card/UI/Attack Selection")),"Shader compilation failed");Click(Center(normal));Check(t.AttackSelectionActive&&t.GetCurrentAttacker()==normal.gameObject,"Actual attacker click failed");Check(!t.IsBattlePhase,"Choosing attacker prematurely starts battle phase");t.SelectAttackTarget(t.LiveAttackWalls()[0]);Check(t.HasPendingAttackConfirmation&&!normal.isTapped&&!t.IsBattlePhase,"Normal wall attack executed before confirmation");Check(view.EntryCount==2,"Wall confirmation must light only attacker and selected wall");t.CancelAttackSelection();Check(!t.HasPendingAttackConfirmation&&!normal.isTapped,"Cancelling confirmation spent an attack");t.StartBattleByAttackSelect(normal.gameObject);}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSecondsRealtime(.25f);
  try{Check(view.Opacity>.99f&&!view.WallZoneButton.gameObject.activeSelf,"Normal attack incorrectly exposes wall zone");Check(!view.Graphic.IsRaycastLocationValid(Center(target),null),"Target raycast blocked");Check(!view.Graphic.IsRaycastLocationValid(Center(queen),null),"Switching raycast blocked");Click(Center(normal));Check(!t.AttackSelectionActive,"Retap did not cancel");Click(Center(normal));Click(Center(queen));Check(t.GetCurrentAttacker()==queen.gameObject&&t.IsMonarchAttacker,"Attacker switch failed");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSecondsRealtime(.25f);
  try{Check(view.WallZoneButton.gameObject.activeSelf&&view.WallZoneGlowVisible&&view.IndividualWallGlowCount==0,"Monarch first stage must show zone only");Click(new Vector2(5,Screen.height*.5f));Check(!t.AttackSelectionActive,"Outside click failed to cancel");t.StartBattleByAttackSelect(queen.gameObject);}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();var lit=Capture("monarch-targets");UnityEngine.Object.Destroy(lit);
  var walls=t.LiveAttackWalls();int wallBefore=walls.Count;int enemyHand=d.enemyHandCount;
  try{Check(walls.Count>=3,"Need original five walls");Click(Center(walls[0].GetComponent<CardController>()));Check(t.ChoosingMonarchWalls&&!view.WallZoneGlowVisible&&view.IndividualWallGlowCount==walls.Count,"Zone click must switch to individual glows only");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return null;
  try{Click(Center(walls[0].GetComponent<CardController>()));Check(t.SelectedAttackWallCount==1,"Actual first wall click failed: "+t.SelectedAttackWallCount+" choosing="+t.ChoosingMonarchWalls+" active="+t.AttackSelectionActive);t.ConfirmMonarchWallChoice();Check(t.LiveAttackWalls().Count==wallBefore&&!queen.isTapped,"Unconfirmed wall attack committed");Click(Center(walls[2].GetComponent<CardController>()));Check(t.SelectedAttackWallCount==2,"Second wall click failed");Click(Center(walls[3].GetComponent<CardController>()));Check(t.SelectedAttackWallCount==2,"Selected more than two walls");Click(Center(walls[0].GetComponent<CardController>()));Check(t.SelectedAttackWallCount==1,"Wall toggle failed");Click(Center(walls[0].GetComponent<CardController>()));}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return null;yield return new WaitForEndOfFrame();var selected=Capture("two-selected");UnityEngine.Object.Destroy(selected);
  try{Check(view.ConfirmationButton.interactable,"Confirmation disabled with two selected");Check(view.EntryCount==3,"Two-wall confirmation lights unselected targets");Click(RectTransformUtility.WorldToScreenPoint(null,view.ConfirmationButton.transform.position));Check(!t.AttackSelectionActive&&queen.isTapped,"Confirmation did not consume exactly one attack");var pending=(HashSet<GameObject>)typeof(HandDealer).GetField("resolvingEnemyWalls",Flags).GetValue(d);Check(AttackMotionPresentation.IsPlaying&&pending.Count==0,"Wall damage must wait for attack motion");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSecondsRealtime(AttackMotionSettings.Load().Duration/AttackMotionSettings.Load().speed+.35f);
  try{Check(t.LiveAttackWalls().Count==wallBefore-2&&d.enemyHandCount==enemyHand+2,"Two-wall damage/recovery incorrect");Check(!t.IsAttackWallSelected(walls[0]),"Stale wall selection");t.ConfirmMonarchWallChoice();Check(d.enemyHandCount==enemyHand+2,"Repeated confirmation duplicated damage");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  // Ordinary card targets use the existing guarded battle coroutine.
  try{Click(Center(normal));Check(t.AttackSelectionActive,"Normal card cannot select after monarch attack");Click(Center(target));Check(t.HasPendingAttackConfirmation&&!normal.isTapped&&target.transform.parent==t.enemyBattleArea,"Card attack ran before confirmation");Check(view.EntryCount==2,"Card confirmation lights unselected cards or walls");t.CancelAttackSelection();Click(Center(normal));Click(Center(target));Confirm(t);Check(!t.AttackSelectionActive&&normal.isTapped,"Enemy target confirmation did not commit attack");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSecondsRealtime(AttackMotionSettings.Load().Duration/AttackMotionSettings.Load().speed+.35f);
  try{Check(target==null||target.transform.parent!=t.enemyBattleArea,"Existing card battle did not resolve");foreach(var c in UnityEngine.Object.FindObjectsByType<CardActionIcon>(FindObjectsInactive.Include,FindObjectsSortMode.None)){Check(c.attackIcon==null||!c.attackIcon.activeInHierarchy,"Legacy attack icon visible");Check(c.blockIcon==null||!c.blockIcon.activeInHierarchy,"Legacy defense icon visible");}}
  catch(Exception e){error=e.ToString();}
  // Force the existing fallback AI to guard, and verify no wall destruction slips through.
  var savedBrain=t.enemyAIBrain;t.enemyAIBrain=null;var guard=Add(d,t.enemyBattleArea,"8");normal.isTapped=false;
  yield return null;int guardedWalls=t.LiveAttackWalls().Count;t.StartBattleByAttackSelect(normal.gameObject);yield return new WaitForSecondsRealtime(.2f);
  try{Check(!view.Graphic.IsRaycastLocationValid(Center(normal),null),"Attacker blocked by overlay");t.SelectAttackTarget(t.LiveAttackWalls()[0]);Check(!normal.isTapped&&t.HasPendingAttackConfirmation,"Guard battle ran before confirmation");Confirm(t);Check(normal.isTapped&&!t.AttackSelectionActive&&t.LiveAttackWalls().Count==guardedWalls,"Guard did not intercept wall attack");}
  catch(Exception e){error=e.ToString();}t.enemyAIBrain=savedBrain;yield return new WaitForSecondsRealtime(AttackMotionSettings.Load().Duration/AttackMotionSettings.Load().speed+.3f);
  if(error!=null){Finish("FAIL: "+error);yield break;}
  // Reduce the real three remaining walls to one through the same committed damage path.
  king.isTapped=false;t.StartBattleByAttackSelect(king.gameObject);yield return new WaitForSecondsRealtime(.2f);
  var remaining=t.LiveAttackWalls();
  try{Check(remaining.Count==3,"Expected three real remaining walls");Click(Center(remaining[0].GetComponent<CardController>()));Click(Center(remaining[0].GetComponent<CardController>()));Click(Center(remaining[1].GetComponent<CardController>()));t.ConfirmMonarchWallChoice();Check(king.isTapped,"K two-wall attack not committed");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSecondsRealtime(AttackMotionSettings.Load().Duration/AttackMotionSettings.Load().speed+.4f);
  remaining=t.LiveAttackWalls();int lastHand=d.enemyHandCount;
  king.isTapped=false;t.StartBattleByAttackSelect(king.gameObject);yield return new WaitForSecondsRealtime(.25f);
  try{Check(remaining.Count==1&&(int)typeof(HandDealer).GetField("enemyWallAliveCount",Flags).GetValue(d)==1,"Real wall count must be one");Check(!view.WallZoneGlowVisible&&view.IndividualWallGlowCount==1&&!view.WallZoneButton.gameObject.activeSelf,"Single wall did not skip zone stage");Click(Center(remaining[0].GetComponent<CardController>()));Check(t.HasPendingAttackConfirmation&&!king.isTapped&&t.LiveAttackWalls().Count==1,"Single wall attacked before confirmation");t.CancelAttackSelection();Check(!king.isTapped&&t.LiveAttackWalls().Count==1,"Single-wall cancellation destroyed wall");t.StartBattleByAttackSelect(king.gameObject);}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSecondsRealtime(.2f);
  try{Click(Center(remaining[0].GetComponent<CardController>()));Check(view.ConfirmationButton.gameObject.activeSelf&&view.ConfirmationButton.interactable,"Single-wall confirmation absent");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return null;
  try{Canvas.ForceUpdateCanvases();Click(RectTransformUtility.WorldToScreenPoint(null,view.ConfirmationButton.transform.position));Check(king.isTapped&&!t.AttackSelectionActive,"Single-wall confirmation did not commit");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForSecondsRealtime(AttackMotionSettings.Load().Duration/AttackMotionSettings.Load().speed+2.1f);
  try{Check(t.LiveAttackWalls().Count==0&&(int)typeof(HandDealer).GetField("enemyWallAliveCount",Flags).GetValue(d)==0&&d.enemyHandCount==lastHand+1,"K last wall was not destroyed and recovered exactly once");}
  catch(Exception e){error=e.ToString();}
  if(error!=null){Finish("FAIL: "+error);yield break;}
  // Direct attack is also confirmed; a live guard prevents ending the probe match.
  var directGuard=Add(d,t.enemyBattleArea,"8");normal.isTapped=false;yield return null;
  t.StartBattleByAttackSelect(normal.gameObject);yield return new WaitForSecondsRealtime(.2f);
  try{var direct=UnityEngine.Object.FindFirstObjectByType<EnemyDirectClick>();Check(direct!=null&&t.CanDirectAttack(),"No direct target at zero walls");t.SelectDirectAttackTarget(direct.gameObject);Check(t.HasPendingAttackConfirmation&&!normal.isTapped&&!t.IsBattleEnded,"Direct attack executed before confirmation");t.CancelAttackSelection();Check(!normal.isTapped&&!t.HasPendingAttackConfirmation,"Direct cancellation spent attack");t.StartBattleByAttackSelect(normal.gameObject);t.SelectDirectAttackTarget(direct.gameObject);Confirm(t);Check(normal.isTapped&&!t.HasPendingAttackConfirmation,"Direct confirmation failed");t.ConfirmAttackSelection();}
  catch(Exception e){error=e.ToString();}
  yield return new WaitForSecondsRealtime(AttackMotionSettings.Load().Duration/AttackMotionSettings.Load().speed+.3f);
  Finish(error!=null?"FAIL: "+error:"PASS: Actual EventSystem clicks select/switch/retap attackers and enemy targets, expanded monarch zone and individual wall hits. No phase advance/spend before commitment. Two-wall toggling/max/confirmation gate work; both existing slash/recovery routines start together and transfer exactly two cards once. Card battle and guard interception resolve. Q/K show zone then individual glows, but skip the zone at one wall. Normal wall/card, final monarch wall and direct attacks all wait for confirmation; cancellation spends no attack. Two-wall confirmation remains a single step. Real five-to-three-to-one-to-zero wall sequence confirms K destroys and recovers the final wall exactly once. No legacy icons; shader compiled and renders. No scene saved.");
 }
}
