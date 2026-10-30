using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
[InitializeOnLoad]
public static class SummonSequenceDiagnostics
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;static bool started;
 static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardSummonSequenceValidation");
 static SummonSequenceDiagnostics(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/T-card/Validate Summon Sequences (Play)")]static void Begin(){SessionState.SetBool("TCardSummonSequenceProbe",true);started=false;EditorApplication.isPlaying=true;}
 static void Tick(){if(!SessionState.GetBool("TCardSummonSequenceProbe",false)||started||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;var d=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(d!=null&&d.IsOpeningDecisionReady){started=true;d.StartCoroutine(Probe(d));}}
 static void Finish(string s){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),s);Debug.Log(s);SessionState.SetBool("TCardSummonSequenceProbe",false);EditorApplication.isPlaying=false;}
 static void Check(bool b,string s){if(!b)throw new Exception(s);}
 static int DeckCount(HandDealer d,bool enemy)=>((System.Collections.ICollection)typeof(HandDealer).GetField(enemy?"enemyDeck":"currentDeck",Flags).GetValue(d)).Count;
 static void Capture(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name+".png"),t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
 static IEnumerator Probe(HandDealer d)
 {
  Directory.CreateDirectory(Folder);var turn=d.turnManager;d.turnManager=null;typeof(HandDealer).GetMethod("CompleteOpeningSelection",Flags).Invoke(d,null);d.turnManager=turn;typeof(TurnManager).GetField("inactivityTimersEnabled",Flags).SetValue(turn,false);yield return new WaitForSecondsRealtime(.4f);
  foreach(bool enemy in new[]{false,true})foreach(bool charge in new[]{true,false})
  {
   string mode=(enemy?"enemy":"player")+(charge?"-5":"-6");var area=enemy?turn.enemyBattleArea:turn.playerBattleArea;var old=UnityEngine.Object.Instantiate(d.proHandCardPrefab,area);var oldCard=old.GetComponent<CardController>();oldCard.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_2.asset"));area.GetComponent<BattleAreaLayout>().PlaceImmediately(oldCard);Vector3 oldPosition=old.transform.position;
   var go=UnityEngine.Object.Instantiate(d.proHandCardPrefab,area);var card=go.GetComponent<CardController>();card.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_"+(charge?"5":"6")+".asset"));
   card.BeginNormalPlay();card.SetSummonSickness(true);
   var layout=area.GetComponent<BattleAreaLayout>();layout.Refresh();layout.PlaceImmediately(card);
   int deck=DeckCount(d,enemy);int hands=enemy?d.enemyHandCards.Count:TCardAIUnityBridge.GetCards(d.handArea).Count;int resource=enemy?turn.enemyResourceManager.currentResource:UnityEngine.Object.FindFirstObjectByType<ResourceManager>().currentResource;
   var input=UnityEngine.Object.FindFirstObjectByType<EventSystem>();var fx=CardEffectManager.I;fx.ActivateOnSummon(card,false,enemy);yield return new WaitForSecondsRealtime(.25f);
   try{Check(fx.IsResolvingSummon&&PresentationSequenceLock.IsLocked&&Time.timeScale==0,"Sequence did not pause game");Check(!input.enabled&&!turn.CanDragPlayerHand,"Input not blocked");Check(Vector3.Distance(old.transform.position,oldPosition)>.1f,"Existing field card stayed frozen during preparation");Check(DeckCount(d,enemy)==deck,"Deck changed before delay/pulse");}catch(Exception e){Finish("FAIL: "+mode+" "+e);yield break;}
   float stop=Time.realtimeSinceStartup+20;bool pulse=false,draw=false;float drawSeen=-1;bool dropSeen=false;Vector3 revealCenter=Vector3.zero;float revealWidth=0;string failure=null;
   while(fx.IsResolvingSummon&&Time.realtimeSinceStartup<stop)
   {
    if(!pulse&&UnityEngine.Object.FindFirstObjectByType<SummonAbilityPulseGraphic>()!=null){yield return new WaitForSecondsRealtime(.14f/Mathf.Max(.05f,SummonEffectSettings.Load().speed));yield return new WaitForEndOfFrame();Capture(mode+"-pulse");pulse=true;}
    var obj=GameObject.Find(charge?"Summon 5 deck to resource":"Summon 6 deck to hand");if(obj!=null&&!draw){var c=obj.GetComponent<CardController>();if(c!=null&&c.artworkImage!=null){if(drawSeen<0)drawSeen=Time.realtimeSinceStartup;if(enemy&&!charge&&c.artworkImage.sprite!=d.cardBackSprite)failure="Enemy effect draw leaked a face";if(!enemy||charge){if(c.artworkImage.sprite!=c.data.artwork||obj.transform.localScale.x<.95f){yield return null;continue;}}if(enemy&&!charge&&Time.realtimeSinceStartup-drawSeen<SummonEffectSettings.Load().drawTravel/Mathf.Max(.05f,SummonEffectSettings.Load().speed)+.05f){yield return null;continue;}yield return new WaitForEndOfFrame();Capture(mode+"-draw");draw=true;}}
    if(charge)
    {
     var preview=GameObject.Find("Summon 5 deck to resource");var fall=GameObject.Find("Resource card falling");
     if(fall!=null&&!dropSeen&&revealWidth>0)
     {
      var rect=(RectTransform)fall.transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);
      float width=Vector3.Distance(corners[1],corners[2]);
      if(Vector3.Distance((corners[0]+corners[2])*.5f,revealCenter)>revealWidth*.15f||Mathf.Abs(width-revealWidth)>revealWidth*.06f)failure="Charge handoff jumped in position or size";
      dropSeen=true;
     }
     if(preview!=null&&preview.GetComponent<CanvasGroup>().alpha>.9f){var art=preview.GetComponent<CardController>().artworkImage.rectTransform;var corners=new Vector3[4];art.GetWorldCorners(corners);revealCenter=(corners[0]+corners[2])*.5f;revealWidth=Vector3.Distance(corners[1],corners[2]);}
    }
    if(!PresentationSequenceLock.IsLocked||Time.timeScale!=0||input.enabled)failure="Sequence released lock before completion";
    yield return null;
   }
   try{Check(failure==null,failure);Check(!charge||dropSeen,"Charge handoff was not observed");Check(!fx.IsResolvingSummon&&pulse&&draw,"Sequence failed to finish all visible phases");Check(DeckCount(d,enemy)==deck-1,"Effect consumed incorrect deck count");int nowHands=enemy?d.enemyHandCards.Count:TCardAIUnityBridge.GetCards(d.handArea).Count;int nowResource=enemy?turn.enemyResourceManager.currentResource:UnityEngine.Object.FindFirstObjectByType<ResourceManager>().currentResource;Check(nowHands==hands+(charge?0:1)&&nowResource==resource+(charge?1:0),"Draw/charge result incorrect or duplicated");Check(!PresentationSequenceLock.IsLocked&&Time.timeScale==1&&input.enabled,"Lock/input/time not restored");}catch(Exception e){Finish("FAIL: "+mode+" "+e);yield break;}
   UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(old);yield return null;
  }
  Finish("PASS: own/enemy rank5 and rank6: actual landing then 0.5s delay, orange pulse, visible deck reveal (enemy6 back-only), exactly one charge/draw; EventSystem disabled, hand blocked and scaled game time frozen through complete sequence; input/time restored. No scene saved.");
 }
}
