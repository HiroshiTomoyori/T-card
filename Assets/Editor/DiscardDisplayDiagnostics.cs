using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class DiscardDisplayDiagnostics
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;static bool started;static double deadline;
 static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardDiscardDisplayValidation");
 static DiscardDisplayDiagnostics(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/T-card/Validate Discard and Enemy Hand (Play)")]static void Begin(){SessionState.SetBool("TCardDiscardDisplayProbe",true);started=false;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.isPlaying=true;}
 static void Tick(){if(!SessionState.GetBool("TCardDiscardDisplayProbe",false)||started||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL: timeout");return;}var d=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(d!=null&&d.IsOpeningDecisionReady){started=true;d.StartCoroutine(Probe(d));}}
 static void Finish(string s){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),s);Debug.Log(s);SessionState.SetBool("TCardDiscardDisplayProbe",false);EditorApplication.isPlaying=false;}
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static int ActiveBacks(HandDealer d){int count=0;foreach(Transform child in d.enemyHandArea)if(child.gameObject.activeSelf&&child.GetComponent<CardController>()!=null)count++;return count;}
 static void Capture(string name){var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name+".png"),im.EncodeToPNG());UnityEngine.Object.Destroy(im);}
 static Vector2 WorldSize(RectTransform r)=>new Vector2(r.TransformVector(Vector3.right*r.rect.width).magnitude,r.TransformVector(Vector3.up*r.rect.height).magnitude);
 static Vector2 VisibleDeckSize(RectTransform deck)
 {
  var stack=OpeningShuffleView.ForDeck(deck);if(stack!=null&&stack.OverlayCanvas!=null){RectTransform last=null;foreach(Transform child in stack.OverlayCanvas.transform)if(child.gameObject.activeSelf&&child.GetComponent<Image>()!=null)last=child as RectTransform;if(last!=null)return WorldSize(last);}
  return WorldSize(deck);
 }
 static IEnumerator Probe(HandDealer d)
 {
  Directory.CreateDirectory(Folder);var turn=d.turnManager;d.turnManager=null;typeof(HandDealer).GetMethod("CompleteOpeningSelection",Flags).Invoke(d,null);d.turnManager=turn;typeof(TurnManager).GetField("inactivityTimersEnabled",Flags).SetValue(turn,false);yield return new WaitForSecondsRealtime(.4f);
  try{
   d.RefreshEnemyHandVisual();Check(ActiveBacks(d)==d.enemyHandCards.Count,"Enemy hand refresh retained old visible backs");
   d.enemyHandCards.RemoveAt(0);d.enemyHandCount=d.enemyHandCards.Count;d.RefreshEnemyHandVisual();Check(ActiveBacks(d)==d.enemyHandCards.Count,"Enemy hand removal displayed an extra card in the same frame");
  }catch(Exception e){Finish("FAIL: "+e);yield break;}
  foreach(bool enemy in new[]{false,true})
  {
   Transform grave=enemy?turn.enemyGraveyard:turn.playerGraveyard;CardController newest=null;
   try{
    for(int i=0;i<3;i++){var go=UnityEngine.Object.Instantiate(d.proHandCardPrefab,enemy?turn.enemyBattleArea:turn.playerBattleArea);var c=go.GetComponent<CardController>();c.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_"+(i+2)+".asset"));typeof(TurnManager).GetMethod("SendToGraveyard",Flags).Invoke(turn,new object[]{go,grave});newest=c;}
    typeof(TurnManager).GetMethod("SyncGraveyardCardSizes",Flags,null,new[]{typeof(Transform)},null).Invoke(turn,new object[]{grave});
    int active=0,total=0;foreach(Transform child in grave){var c=child.GetComponent<CardController>();if(c==null)continue;total++;var cg=c.GetComponent<CanvasGroup>();Check(cg!=null,"Discard missing visibility group");if(child.gameObject.activeSelf){active++;Check(c==newest&&cg.alpha==1,"Visible discard is not opaque/newest");}else Check(cg.alpha==0,"Older discard is translucent rather than hidden");}
    Check(active==1&&total>=3,"Discard history lost or several cards rendered");
    var deck=enemy?d.enemyDeckPosition:d.deckPosition;Check(Mathf.Abs(newest.artworkImage.rectTransform.TransformPoint(newest.artworkImage.rectTransform.rect.center).y-OpeningShuffleView.DealPosition(deck).y)<.5f,"Discard Y differs from visible deck center");var expected=VisibleDeckSize(enemy?d.enemyDeckPosition:d.deckPosition);var actual=WorldSize(newest.artworkImage.rectTransform);Check(Vector2.Distance(expected,actual)<.5f,"Discard does not match its own side's visible deck: expected "+expected+" got "+actual);
   }catch(Exception e){Finish("FAIL: "+e);yield break;}
   yield return new WaitForEndOfFrame();Capture(enemy?"enemy-discard":"player-discard");yield return null;
  }
  Finish("PASS: both discard faces match their respective visible decks; only latest card is opaque/active, older cards fully hidden with data retained; enemy hand rebuild/removal shows exactly the data count immediately, without deferred-destroy duplicates. No scene saved.");
 }
}
