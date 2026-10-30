using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using DG.Tweening;
[InitializeOnLoad]
public static class FieldGeometryDiagnostics
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static bool started;
 static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardFieldGeometryValidation");
 static FieldGeometryDiagnostics(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/T-card/Validate Field Geometry (Play)")]
 static void Begin(){SessionState.SetBool("TCardGeometryProbe",true);started=false;EditorApplication.isPlaying=true;}
 static void Tick(){if(!SessionState.GetBool("TCardGeometryProbe",false)||started||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;var d=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(d!=null&&d.IsOpeningDecisionReady){started=true;d.StartCoroutine(Probe(d));}}
 static void Finish(string result){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),result);Debug.Log(result);SessionState.SetBool("TCardGeometryProbe",false);EditorApplication.isPlaying=false;}
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static float Width(CardController c){var r=c.artworkImage.rectTransform;return c.transform.parent.InverseTransformVector(r.TransformVector(Vector3.right*r.rect.width)).magnitude;}
 static IEnumerator Probe(HandDealer d)
 {
  var t=d.turnManager;d.turnManager=null;typeof(HandDealer).GetMethod("CompleteOpeningSelection",Flags).Invoke(d,null);d.turnManager=t;typeof(TurnManager).GetField("inactivityTimersEnabled",Flags).SetValue(t,false);yield return new WaitForSecondsRealtime(.4f);
  var widths=new System.Collections.Generic.Dictionary<Transform,float>();
  foreach(var suit in new[]{"Spade","Heart","Club","Diamond"})foreach(var rank in new[]{"2","3","4","5","6","7","8","10","J","Q","K"})foreach(var area in new[]{t.playerBattleArea,t.enemyBattleArea})
  {
   CardController card=null;float width=0;
   try{
    var go=UnityEngine.Object.Instantiate(d.proHandCardPrefab,area);card=go.GetComponent<CardController>();card.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/"+suit+"_"+rank+".asset"));Check(card.data!=null,"Missing card asset");
    var pro=go.GetComponent<CCP.Card>();if(pro.cardHoverer!=null){pro.cardHoverer.transform.localScale*=1.35f;pro.cardHoverer.transform.DOScale(Vector3.one,.18f).SetUpdate(true);}if(pro.cardPickuper!=null)pro.cardPickuper.transform.localScale*=1.2f;
    Vector2 dimensions=((RectTransform)go.transform).sizeDelta;var drag=card.GetComponent<CardDrag>();drag.BeginPlayPresentation();drag.EndPlayPresentation();drag.DropToBattleArea(area,false);Check(((RectTransform)go.transform).sizeDelta==dimensions,"Double-click lost card dimensions: "+card.data.name);var layout=area.GetComponent<BattleAreaLayout>();layout.Refresh();layout.PlaceImmediately(card);width=Width(card);if(!widths.ContainsKey(area))widths[area]=width;Check(Mathf.Abs(width-widths[area])<.1f,"Rank-dependent field size: "+card.data.name);Check(width>1,"Card collapsed on entry: "+card.data.name);
   }catch(Exception e){Finish("FAIL: "+e);yield break;}
   yield return new WaitForSecondsRealtime(.24f);
   try{Check(Mathf.Abs(Width(card)-width)<.1f,"Hand animation changed field width: "+card.data.name);var layout=area.GetComponent<BattleAreaLayout>();layout.Refresh();layout.PlaceImmediately(card);Check(Mathf.Abs(Width(card)-width)<.1f,"Refresh changed field width: "+card.data.name);}
   catch(Exception e){Finish("FAIL: "+e);yield break;}
   UnityEngine.Object.Destroy(card.gameObject);yield return null;
  }
  foreach(var rank in new[]{"A","9"})foreach(var area in new[]{t.playerBattleArea,t.enemyBattleArea})
  {
   CardController card=null;
   try{
    var go=UnityEngine.Object.Instantiate(d.proHandCardPrefab,area);card=go.GetComponent<CardController>();card.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_"+rank+".asset"));card.GetComponent<CardDrag>().PlaceFromShield(area,false);ActCardPresentation.HideOnField(card);
    Check(card.GetComponent<CanvasGroup>().alpha==0,"Act visible on entry");var layout=area.GetComponent<BattleAreaLayout>();layout.Refresh();Vector3 before=card.transform.localPosition;layout.PlaceImmediately(card);Check(before==card.transform.localPosition,"Act assigned a field slot");
    t.SendCardToOwnGraveyard(card);Check(card.transform.parent==(area==t.playerBattleArea?t.playerGraveyard:t.enemyGraveyard),"Act ownership lost on discard");
   }catch(Exception e){Finish("FAIL: "+e);yield break;}
   yield return new WaitForEndOfFrame();
   try{Check(Mathf.Approximately(card.GetComponent<CanvasGroup>().alpha,1),"Discarded act remained invisible");}catch(Exception e){Finish("FAIL: "+e);yield break;}
  }
  Finish("PASS: 88 real-prefab field entries across all four suits and both sides, including every Q, preserve double-click dimensions and identical artwork size across ranks after hover/pickup completion and layout refresh. A/9 never occupy visible field slots and restore visibility in the correct discard. No scene saved.");
 }
}
