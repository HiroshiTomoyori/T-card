using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class ActResolutionDiagnostics
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static bool started;
 static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardActResolutionValidation");
 static ActResolutionDiagnostics(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/T-card/Validate Act Resolution (Play)")]
 static void Begin(){SessionState.SetBool("TCardActResolutionProbe",true);started=false;EditorApplication.isPlaying=true;}
 static void Tick(){if(!SessionState.GetBool("TCardActResolutionProbe",false)||started||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;var d=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(d!=null&&d.IsOpeningDecisionReady){started=true;d.StartCoroutine(Probe(d));}}
 static void Finish(string s){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),s);Debug.Log(s);SessionState.SetBool("TCardActResolutionProbe",false);EditorApplication.isPlaying=false;}
 static void Check(bool v,string m){if(!v)throw new Exception(m);}
 static void Call(TurnManager t,string name,params object[] args){typeof(TurnManager).GetMethod(name,Flags).Invoke(t,args);}
 static CardController Add(HandDealer d,Transform area,string rank){var go=UnityEngine.Object.Instantiate(d.proHandCardPrefab,area);var c=go.GetComponent<CardController>();c.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_"+rank+".asset"));if(BattleAreaLayout.IsSpell(c.data))c.GetComponent<CardDrag>().PlaceFromShield(area,false);return c;}
 static IEnumerator Run(IEnumerator routine,Action done){yield return routine;done();}
 static IEnumerator Probe(HandDealer d)
 {
  var t=d.turnManager;d.turnManager=null;typeof(HandDealer).GetMethod("CompleteOpeningSelection",Flags).Invoke(d,null);d.turnManager=t;typeof(TurnManager).GetField("inactivityTimersEnabled",Flags).SetValue(t,false);yield return new WaitForSecondsRealtime(.4f);
  CardController low=null,mid=null,high=null,ace=null;
  try{
   Call(t,"SetEndTurnButton",true);low=Add(d,t.enemyBattleArea,"3");mid=Add(d,t.enemyBattleArea,"Q");high=Add(d,t.enemyBattleArea,"K");ace=Add(d,t.playerBattleArea,"A");t.StartSelectEnemyBattleToDestroy(ace);Call(t,"SyncEndTurnButtonVisibility");
   Check(t.IsResolvingActCard&&!t.endTurnButton.interactable,"Act selection did not disable end turn");t.EndPlayerTurn();Check(!t.IsPlayerDecisionOpen&&!(bool)typeof(TurnManager).GetField("isEndingTurn",Flags).GetValue(t),"End turn accepted during act");
   Call(t,"ResolveActTargetTimeout");Check(high.transform.parent==t.enemyGraveyard&&mid.transform.parent==t.enemyBattleArea&&low.transform.parent==t.enemyBattleArea,"Timeout did not select maximum power");Check(ace.transform.parent==t.playerGraveyard&&!t.IsSelectingDestroyTarget(),"Act not discarded after automatic selection");Call(t,"SyncEndTurnButtonVisibility");Check(t.endTurnButton.interactable,"End turn not restored after discard");
   ace=Add(d,t.playerBattleArea,"A");t.StartSelectEnemyBattleToDestroy(ace);t.TrySelectDestroyTarget(low);Check(t.IsPlayerDecisionOpen,"Target confirmation absent");var kind=typeof(TurnManager).GetNestedType("TimedDecision",BindingFlags.NonPublic);Call(t,"ResolveDecisionTimeout",Enum.Parse(kind,"Confirmation"));Check(mid.transform.parent==t.enemyGraveyard&&low.transform.parent==t.enemyBattleArea&&ace.transform.parent==t.playerGraveyard&&!t.IsPlayerDecisionOpen,"Confirmation timeout failed to choose strongest target");
  }catch(Exception e){Finish("FAIL: "+e);yield break;}
  var nine=Add(d,t.playerBattleArea,"9");CardEffectManager.I.ActivateOnSummon(nine);float deadline=Time.realtimeSinceStartup+8;
  while(t.IsResolvingActCard&&Time.realtimeSinceStartup<deadline){try{Call(t,"SyncEndTurnButtonVisibility");Check(!t.endTurnButton.interactable,"End turn enabled during Nine resolution");}catch(Exception e){Finish("FAIL: "+e);yield break;}yield return null;}
  try{Check(!t.IsResolvingActCard&&nine.transform.parent==t.playerGraveyard&&low.isTapped,"Nine did not resolve and discard");Call(t,"SyncEndTurnButtonVisibility");Check(t.endTurnButton.interactable,"Nine end-turn lock leaked");t.SendCardToOwnGraveyard(low);ace=Add(d,t.playerBattleArea,"A");t.StartSelectEnemyBattleToDestroy(ace);Check(ace.transform.parent==t.playerGraveyard&&!t.IsResolvingActCard,"No-target act lock leaked");}catch(Exception e){Finish("FAIL: "+e);yield break;}
  bool done=false,saw=false;d.StartCoroutine(Run(CardPlayPresentation.Play(ace.data,t.playerBattleArea),()=>done=true));deadline=Time.realtimeSinceStartup+8;
  while(!done&&Time.realtimeSinceStartup<deadline){try{if(CardPlayPresentation.IsPlaying){saw=true;Call(t,"SyncEndTurnButtonVisibility");Check(CardPlayPresentation.IsActPlaying&&t.IsResolvingActCard&&!t.endTurnButton.interactable,"Act reveal not protected");}}catch(Exception e){Finish("FAIL: "+e);yield break;}yield return null;}
  try{Call(t,"SyncEndTurnButtonVisibility");Check(done&&saw&&!t.IsResolvingActCard&&t.endTurnButton.interactable,"Reveal lock did not restore");}catch(Exception e){Finish("FAIL: "+e);yield break;}
  Finish("PASS: End-turn button and direct call blocked during Act reveal, Ace target selection/confirmation and Nine effect until discard. Target timeout and confirmation timeout select highest-power valid opponent without another confirmation; no-target and completed effects release lock. No scene saved.");
 }
}
