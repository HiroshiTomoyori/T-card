using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class CardActionGlowDiagnostics
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static bool started;static double deadline;static CardActionGlowSettings settings;static string json;
 static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardActionGlowValidation");
 static CardActionGlowDiagnostics(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/T-card/Validate Card Action Glow (Play)")]
 static void Begin(){CardActionGlowSettingsEditor.Ensure();SessionState.SetBool("TCardActionGlowProbe",true);started=false;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.isPlaying=true;}
 static void Tick(){if(!SessionState.GetBool("TCardActionGlowProbe",false)||started||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL: opening timeout");return;}var d=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(d!=null&&d.IsOpeningDecisionReady){started=true;d.StartCoroutine(Probe(d));}}
 static void Finish(string text){if(settings!=null&&json!=null)EditorJsonUtility.FromJsonOverwrite(json,settings);Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),text);Debug.Log(text);SessionState.SetBool("TCardActionGlowProbe",false);EditorApplication.isPlaying=false;}
 static void Check(bool value,string text){if(!value)throw new Exception(text);}
 static void Set(TurnManager turn,string key,bool value){typeof(TurnManager).GetField(key,Flags).SetValue(turn,value);}
 static CardController Add(HandDealer dealer,Transform area,CardData template,string rank,bool block=false,bool sick=false){var data=UnityEngine.Object.Instantiate(template);data.cardName=rank;data.effectTypes=block?new[]{EffectType.BlockOnly}:new EffectType[0];data.SetPowerFromName();var go=UnityEngine.Object.Instantiate(dealer.proHandCardPrefab!=null?dealer.proHandCardPrefab:dealer.cardPrefab,area);var c=go.GetComponent<CardController>();c.SetData(data);c.hasSummonSickness=sick;return c;}
 static Texture2D Capture(string name){var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name+".png"),image.EncodeToPNG());return image;}
 static IEnumerator Probe(HandDealer dealer)
 {
  Directory.CreateDirectory(Folder);settings=CardActionGlowSettings.Load();json=EditorJsonUtility.ToJson(settings);
  var turn=dealer.turnManager;dealer.turnManager=null;typeof(HandDealer).GetMethod("CompleteOpeningSelection",Flags).Invoke(dealer,null);dealer.turnManager=turn;yield return new WaitForSeconds(.5f);
  var data=dealer.handArea.GetComponentsInChildren<CardController>().First(c=>c.data!=null).data;
  var normal=Add(dealer,turn.playerBattleArea,data,"8");var sick=Add(dealer,turn.playerBattleArea,data,"10",false,true);var blocker=Add(dealer,turn.playerBattleArea,data,"8",true);blocker.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_8.asset"));var tapped=Add(dealer,turn.playerBattleArea,data,"K");tapped.isTapped=true;
  var enemyNormal=Add(dealer,turn.enemyBattleArea,data,"7");var enemyBlocker=Add(dealer,turn.enemyBattleArea,data,"8",true);enemyBlocker.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Club_8.asset"));
  typeof(TurnManager).GetField("actionGlowTurn",Flags).SetValue(turn,1);
  var layout=turn.playerBattleArea.GetComponent<BattleAreaLayout>();yield return null;layout.Refresh();foreach(var c in new[]{normal,sick,blocker,tapped})layout.PlaceImmediately(c);
  Set(turn,"isPlayerTurnActive",true);Set(turn,"isPlayerMainPhase",false);Set(turn,"isBattlePhase",false);Set(turn,"isEndingTurn",false);Set(turn,"battleIconsEnded",false);Set(turn,"enemyBattleIconsActive",false);
  settings.enabledEffect=true;settings.brightness=1;settings.minimumBrightness=.25f;settings.pulsePeriod=4.4f;
  yield return new WaitForSecondsRealtime(.35f);string error=null;
  try{
   Check(!UnityEditor.ShaderUtil.ShaderHasError(Shader.Find("T-card/UI/Card Action Glow")),"Shader compilation failed");
   Check(normal.GetComponent<CardActionGlow>().Opacity>.99f,"Attack-ready card not glowing during main phase");
   Check(enemyBlocker.GetComponent<CardActionGlow>().Opacity>.99f,"Enemy eight not glowing for defense during player main phase");
   Check(blocker.GetComponent<CardActionGlow>()==null||blocker.GetComponent<CardActionGlow>().Opacity<.001f,"Eight incorrectly glows for attack");
   Check(enemyNormal.GetComponent<CardActionGlow>()==null||enemyNormal.GetComponent<CardActionGlow>().Opacity<.001f,"Enemy attacker glows out of turn");
   Check(sick.GetComponent<CardActionGlow>()==null||sick.GetComponent<CardActionGlow>().Opacity<.001f,"Summon sickness glows");
   Check(tapped.GetComponent<CardActionGlow>()==null||tapped.GetComponent<CardActionGlow>().Opacity<.001f,"Tapped card glows");
   var graphic=normal.GetComponent<CardActionGlow>().Graphic;Check(graphic!=null&&!graphic.raycastTarget&&graphic.GetComponent<Canvas>()==null,"Glow intercepts input or creates foreground canvas");Check(graphic.transform.GetSiblingIndex()<normal.artworkImage.transform.GetSiblingIndex(),"Artwork behind glow");
   Check(Mathf.Abs(settings.EvaluatePulse(0)-.25f)<.001f&&Mathf.Abs(settings.EvaluatePulse(2.2f)-1)<.001f&&Mathf.Abs(settings.EvaluatePulse(4.4f)-.25f)<.001f,"Breathing period incorrect");
  }catch(Exception e){error=e.ToString();}if(error!=null){Finish("FAIL: "+error);yield break;}
  yield return new WaitForEndOfFrame();var lit=Capture("attack");settings.brightness=0;yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();var plain=Capture("without-glow");settings.brightness=1;
  try{var a=lit.GetPixels32();var b=plain.GetPixels32();int changed=0;for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>8)changed++;Check(changed>200,"Actual glow not visible");}catch(Exception e){error=e.ToString();}UnityEngine.Object.Destroy(lit);UnityEngine.Object.Destroy(plain);if(error!=null){Finish("FAIL: "+error);yield break;}
  typeof(TurnManager).GetField("actionGlowTurn",Flags).SetValue(turn,2);normal.isTapped=true;Set(turn,"isPlayerTurnActive",false);Set(turn,"isBattlePhase",false);Set(turn,"enemyBattleIconsActive",true);yield return new WaitForSecondsRealtime(.4f);
  try{Check(normal.GetComponent<CardActionGlow>().Opacity<.001f,"Attack glow remains on opponent turn");Check(blocker.GetComponent<CardActionGlow>().Opacity>.99f,"Actual eight defender not glowing during enemy main phase");Check(enemyNormal.GetComponent<CardActionGlow>().Opacity>.99f,"Enemy attacker not glowing during enemy main phase");Check(enemyBlocker.GetComponent<CardActionGlow>().Opacity<.001f,"Enemy eight incorrectly glows for attack");}catch(Exception e){error=e.ToString();}if(error!=null){Finish("FAIL: "+error);yield break;}
  yield return new WaitForEndOfFrame();var defense=Capture("defense");UnityEngine.Object.Destroy(defense);blocker.isTapped=true;yield return new WaitForSecondsRealtime(.4f);
  try{Check(blocker.GetComponent<CardActionGlow>().Opacity<.001f,"Used defender still glowing");}catch(Exception e){error=e.ToString();}
  blocker.isTapped=false;yield return new WaitForSecondsRealtime(.3f);blocker.transform.SetParent(dealer.handArea,false);yield return null;yield return null;
  try{Check(blocker.GetComponent<CardActionGlow>().Opacity==0&&!blocker.GetComponent<CardActionGlow>().Graphic.gameObject.activeSelf,"Card leaving field retains glow");}catch(Exception e){error=e.ToString();}
  Finish(error!=null?"FAIL: "+error:"PASS: Both fields glow for attack/defense throughout main phase, independent of battle-only icons. Actual Spade/Club eight assets correctly glow for defense, never attack; sick/tapped cards stay unlit; used/moved cards clear glow. Shader visibly renders behind artwork, no raycast or canvas override. 4.4s stationary breathing and brightness control validated. Settings restored; scene not saved.");
 }
}
