using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class EnemyPresentationDiagnostics
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;static bool started;static double deadline;
 static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardEnemyPresentationValidation");
 static EnemyPresentationDiagnostics(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/T-card/Validate Enemy Presentation (Play)")]static void Begin(){EnemyPresentationSettingsEditor.Ensure();SessionState.SetBool("TCardEnemyFlowProbe",true);started=false;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.isPlaying=true;}
 static void Tick(){if(!SessionState.GetBool("TCardEnemyFlowProbe",false)||started||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL: timeout");return;}var d=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(d!=null&&d.IsOpeningDecisionReady){started=true;d.StartCoroutine(Probe(d));}}
 static void Finish(string s){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),s);Debug.Log(s);SessionState.SetBool("TCardEnemyFlowProbe",false);EditorApplication.isPlaying=false;}
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static void Capture(string name){var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name+".png"),im.EncodeToPNG());UnityEngine.Object.Destroy(im);}
 static IEnumerator Probe(HandDealer d){
  Directory.CreateDirectory(Folder);var turn=d.turnManager;d.turnManager=null;typeof(HandDealer).GetMethod("CompleteOpeningSelection",Flags).Invoke(d,null);d.turnManager=turn;typeof(TurnManager).GetField("inactivityTimersEnabled",Flags).SetValue(turn,false);yield return new WaitForSecondsRealtime(.4f);
  var manager=turn.enemyResourceManager;var view=manager.EnsureLiquidView();string failure=null;var config=EnemyPresentationSettings.Load();var own=Resources.Load<LiquidResourceSettings>("TCardResource/LiquidResourceSettings");
  try{Check(view!=null,"Enemy resource view absent");Check(view.Settings!=own&&view.Settings.panelSize.y/view.Settings.panelSize.x<own.panelSize.y/own.panelSize.x,"Enemy settings not independent/flatter");Check(view.Settings.sourceFont==own.sourceFont&&view.Settings.suitAtlas==own.suitAtlas,"Font/suit assets not reused");Check(!manager.enemyResourceText.enabled,"Legacy resource number still drawn");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  manager.maxResource=13;manager.currentResource=6;manager.RecoverResource();manager.UseResource(7);
  int before=d.enemyHandCards.Count;var deck=(System.Collections.Generic.List<CardData>)typeof(HandDealer).GetField("enemyDeck",Flags).GetValue(d);int beforeDeck=deck.Count;bool done=false,captured=false;
  d.StartCoroutine(Run(d.DrawEnemyTurnStartCard(),()=>done=true));float until=Time.realtimeSinceStartup+15;
  while(!done&&Time.realtimeSinceStartup<until){
   foreach(var fx in UnityEngine.Object.FindObjectsByType<TurnDrawHighlightEffect>(FindObjectsSortMode.None))if(fx.name=="Enemy turn draw (back only)"){
    var card=fx.GetComponent<CardController>();if(card.artworkImage.sprite!=d.cardBackSprite)failure="Enemy draw revealed face";
    if(card.data!=null)failure="Hidden draw presentation retained card data";
    if(!captured&&fx.Elapsed>config.drawTravel+config.drawSettle){yield return new WaitForEndOfFrame();Capture("draw-back");captured=true;}
   }yield return null;
  }
  try{Check(done&&failure==null&&captured,failure??"Draw timeout or missing capture");Check(d.enemyHandCards.Count==before+1&&deck.Count==beforeDeck-1,"Draw counts not exactly once");Check(!d.IsEnemyDrawAnimating,"Draw gate stuck");foreach(Transform child in d.enemyHandArea){var c=child.GetComponent<CardController>();if(c!=null)Check(c.artworkImage.sprite==d.cardBackSprite,"Final hand face leak");}}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  yield return new WaitForEndOfFrame();Capture("draw-arrived");
  var selected=d.enemyHandCards[0];done=false;captured=false;before=d.enemyHandCards.Count;float chargeStart=Time.realtimeSinceStartup;d.StartCoroutine(Run(d.EnemyChargeSpecificResourceAnimation(selected),()=>done=true));until=Time.realtimeSinceStartup+15;
  while(!done&&Time.realtimeSinceStartup<until){
   foreach(var c in UnityEngine.Object.FindObjectsByType<CardController>(FindObjectsSortMode.None))if(c!=null&&c.name=="Enemy resource charge (public face)"&&c.GetComponent<CanvasGroup>().alpha>.9f){if(c.artworkImage.sprite!=selected.artwork)failure="Charge card not public face";if(!captured&&Time.realtimeSinceStartup-chargeStart>config.chargeTravel+.05f){var corners=new Vector3[4];((RectTransform)c.transform).GetWorldCorners(corners);foreach(var corner in corners){var screen=RectTransformUtility.WorldToScreenPoint(null,corner);if(screen.x<0||screen.x>Screen.width||screen.y<0||screen.y>Screen.height)failure="Charge reveal is off-screen";}var overlay=c.GetComponent<Canvas>();if(overlay==null||!overlay.overrideSorting||overlay.sortingOrder<=view.ForegroundCanvas.sortingOrder)failure="Charge face below foreground";yield return new WaitForEndOfFrame();Capture("charge-face");captured=true;}}
   yield return null;
  }
  try{Check(done&&captured&&failure==null,failure??"Charge timeout");Check(d.enemyHandCards.Count==before-1&&d.enemyHandCount==d.enemyHandCards.Count,"Charge hand count incorrect");Check(manager.maxResource==13&&manager.currentResource==6,"Animation spent/granted resource prematurely");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  manager.AddResource();yield return new WaitForSecondsRealtime(.18f);yield return new WaitForEndOfFrame();Capture("charge-beam");
  try{Check(manager.maxResource==14&&manager.currentResource==14,"Charge accounting changed");var parent=view.transform.parent;Check(parent.name=="Enemy Resource UI Host","Enemy view reparented whole enemy area");Check(view.ForegroundCanvas.overrideSorting,"No charge foreground");foreach(var g in view.ForegroundCanvas.GetComponentsInChildren<Graphic>(true))Check(!g.raycastTarget,"Charge intercepts input");}
  catch(Exception e){Finish("FAIL: "+e);yield break;}
  manager.UseResource(8);yield return new WaitForSecondsRealtime(.8f);yield return new WaitForEndOfFrame();Capture("spent");
  Finish("PASS: enemy draw back-only at every sampled frame and final hand, hidden data absent, hand/deck counts exactly once, draw gate clears; on-screen foreground public-face charge removes exactly one hand card before resource increment; dedicated flatter liquid UI shares Lora/suit assets, charge foreground is non-interactive; charge and spend update correctly. Existing scene was not saved.");
 }
 static IEnumerator Run(IEnumerator routine,Action done){yield return routine;done();}
}
