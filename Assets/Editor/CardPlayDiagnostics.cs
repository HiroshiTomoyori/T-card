using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class CardPlayDiagnostics
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardCardPlaySideReverseValidation");
    static bool started;static double deadline;
    static CardPlayDiagnostics(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/T-card/Validate Card Play (Play)")]
    static void Begin(){SessionState.SetBool("TCardCardPlayProbe",true);started=false;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(!SessionState.GetBool("TCardCardPlayProbe",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||started)return;
        if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
        if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL: opening timeout");return;}
        var dealer=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(dealer==null||!dealer.IsOpeningDecisionReady)return;
        started=true;dealer.StartCoroutine(Probe(dealer));
    }
    static void Require(bool value,string text){if(!value)throw new Exception(text);}
    static int Count(Transform area)=>area.GetComponentsInChildren<CardController>().Count(c=>c.transform.parent==area&&c.data!=null);
    static void Finish(string message){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),message);Debug.Log(message);SessionState.SetBool("TCardCardPlayProbe",false);EditorApplication.isPlaying=false;}
    static IEnumerator Probe(HandDealer dealer)
    {
        var turn=dealer.turnManager;dealer.turnManager=null;typeof(HandDealer).GetMethod("CompleteOpeningSelection",Flags).Invoke(dealer,null);dealer.turnManager=turn;
        yield return new WaitForSeconds(.5f);
        Directory.CreateDirectory(Folder);var settings=CardPlaySettings.Load();string saved=EditorJsonUtility.ToJson(settings);string failure=null;
        var zone=UnityEngine.Object.FindObjectsByType<BattleDropZone>(FindObjectsSortMode.None).FirstOrDefault(z=>z.battleArea!=null&&z.battleArea.GetComponent<BattleAreaLayout>()!=null);
        if(zone==null){Finish("FAIL: BattleDropZone missing");yield break;}
        var method=typeof(BattleDropZone).GetMethod("PlayCommittedCard",Flags);int initial=Count(zone.battleArea);int beforeHand=Count(dealer.handArea);
        bool sawBack=false,sawFront=false,sawRipple=false,captured=false;float expectedDuration=settings.TotalTime/Mathf.Max(.05f,settings.speed);float began=Time.realtimeSinceStartup;
        var card=dealer.handArea.GetComponentsInChildren<CardController>().First(c=>c.data!=null);
        var data=UnityEngine.Object.Instantiate(card.data);data.cardName="2";data.power=2;data.effectTypes=new EffectType[0];card.SetData(data);
        var drag=card.GetComponent<CardDrag>();zone.StartCoroutine((IEnumerator)method.Invoke(zone,new object[]{card,drag,null}));
        float timeout=Time.realtimeSinceStartup+8;
        while((drag.IsDropSnapPlaying||CardPlayPresentation.IsPlaying)&&Time.realtimeSinceStartup<timeout)
        {
            var fx=UnityEngine.Object.FindFirstObjectByType<CardPlayPresentation>();
            if(fx!=null)
            {
                try
                {
                    Require(Count(zone.battleArea)==initial,"Card entered field before presentation finished");
                    Require(card.GetComponent<CanvasGroup>().alpha==0,"Real card duplicated during reveal");
                    Require(!turn.CanDragPlayerHand,"Hand still draggable during showcase");
                    Require(!fx.Artwork.raycastTarget&&!fx.Atmosphere.raycastTarget,"Effects block input individually");
                    if(fx.Elapsed<settings.RotateTime*.4f&&fx.Artwork.sprite==dealer.cardBackSprite)sawBack=true;
                    if(fx.Elapsed>=settings.RotateTime&&fx.Artwork.sprite==data.artwork)sawFront=true;
                    if(fx.Elapsed>settings.RotateTime+.04f&&fx.Elapsed<settings.RotateTime+.15f)sawRipple=true;
                }catch(Exception e){failure=e.ToString();break;}
                if(!captured&&fx.Elapsed>=settings.RotateTime+.05f)
                {
                    yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,"showcase.png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);captured=true;
                }
            }
            yield return null;
        }
        float duration=Time.realtimeSinceStartup-began;
        yield return new WaitForSeconds(.35f);
        try
        {
            if(failure!=null)throw new Exception(failure);
            Require(sawBack&&sawFront&&sawRipple&&captured,"Missing back/front/reveal ripple");
            Require(!CardPlayPresentation.IsPlaying&&!drag.IsDropSnapPlaying,"Animation did not complete");
            Require(Count(zone.battleArea)==initial+1,"Exactly one card was not added");Require(Count(dealer.handArea)==beforeHand-1,"Hand count mismatch");
            Require(card.hasSummonSickness,"Summon state lost");Require(card.GetComponent<CanvasGroup>().blocksRaycasts,"Placed card cannot receive input");
            Require(UnityEngine.Object.FindObjectsByType<CardPlayPresentation>(FindObjectsSortMode.None).Length==0,"Overlay leaked");
            Require(duration>=expectedDuration-.05f&&duration<expectedDuration+.65f,"Default animation timing mismatch: "+duration);
        }catch(Exception e){failure=e.ToString();}
        if(failure!=null){EditorJsonUtility.FromJsonOverwrite(saved,settings);Finish("FAIL: "+failure);yield break;}
        // Exercise the same shared presentation on the opponent's field, with edited settings.
        settings.speed=1.6f;settings.enemyHighlightColor=Color.cyan;settings.enemyCenterYOffset=110;settings.revealSize=new Vector2(250,360);settings.initialSizeRatio=.7f;settings.rippleStrength=.2f;
        bool complete=false,editedColor=false,enemyOffset=false;int enemyBefore=Count(turn.enemyBattleArea);float enemyStart=Time.realtimeSinceStartup;
        dealer.StartCoroutine(EnemyProbe(data,turn.enemyBattleArea,()=>complete=true));
        while(!complete&&Time.realtimeSinceStartup-enemyStart<5)
        {
            var fx=UnityEngine.Object.FindFirstObjectByType<CardPlayPresentation>();if(fx!=null&&fx.Elapsed>.1f)
            {
                Require(fx.IsEnemy,"Opponent presentation uses player palette");
                if(fx.Elapsed<settings.RotateTime+settings.HoldTime)
                {
                    var bounds=((RectTransform)fx.transform).rect;float fit=Mathf.Min(bounds.width/720,bounds.height/760);
                    var position=((RectTransform)fx.Artwork.transform.parent).anchoredPosition;
                    enemyOffset=Vector2.Distance(position,(settings.centerOffset+Vector2.up*110)*fit)<.1f;
                }
                var halo=fx.GetComponentsInChildren<TurnDrawHighlightGraphic>().First(g=>!g.surface);var mesh=halo.canvasRenderer.GetMesh();
                editedColor=editedColor||(mesh!=null&&mesh.colors32.Any(c=>c.g>200&&c.b>200&&c.r<10&&c.a>0));
            }
            yield return null;
        }
        try
        {
            Require(complete&&editedColor&&enemyOffset,"Edited settings / opponent presentation failed");Require(Count(turn.enemyBattleArea)==enemyBefore,"Opponent preview added an early field card");
            Require(Time.realtimeSinceStartup-enemyStart<1.2f,"Speed adjustment not applied");
        }catch(Exception e){failure=e.ToString();}
        EditorJsonUtility.FromJsonOverwrite(saved,settings);
        if(failure!=null){Finish("FAIL: "+failure);yield break;}
        // Resolve a real accepted reverse choice; do not bypass its gameplay path.
        var reverse=UnityEngine.Object.Instantiate(data);reverse.effectTypes=new[]{EffectType.ShieldTrigger};
        int reverseField=Count(turn.playerBattleArea),reverseHand=Count(dealer.handArea);
        var resources=UnityEngine.Object.FindFirstObjectByType<ResourceManager>();int resourceBefore=resources!=null?resources.currentResource:0;
        bool reverseDone=false,reverseSeen=false;float reverseStart=Time.realtimeSinceStartup;
        dealer.StartCoroutine(ReverseProbe(dealer,reverse,()=>reverseDone=true));
        while(ReverseChoiceManager.I!=null&&!ReverseChoiceManager.I.IsChoosing&&Time.realtimeSinceStartup-reverseStart<2)yield return null;
        if(ReverseChoiceManager.I==null||!ReverseChoiceManager.I.IsChoosing){Finish("FAIL: reverse choice did not open");yield break;}
        ReverseChoiceManager.I.useButton.onClick.Invoke();
        if(turn.IsPlayerDecisionOpen)typeof(TurnManager).GetMethod("AcceptPlayerDecision",Flags).Invoke(turn,null);
        while(!reverseDone&&Time.realtimeSinceStartup-reverseStart<8)
        {
            var fx=UnityEngine.Object.FindFirstObjectByType<CardPlayPresentation>();
            if(fx!=null)
            {
                reverseSeen=true;
                try{Require(!fx.IsEnemy,"Player reverse uses opponent palette");Require(Count(turn.playerBattleArea)==reverseField,"Reverse entered field before reveal completed");}
                catch(Exception e){failure=e.ToString();break;}
            }
            yield return null;
        }
        yield return new WaitForSeconds(.1f);
        try
        {
            Require(reverseDone&&reverseSeen,"Accepted reverse did not use showcase");
            Require(Count(turn.playerBattleArea)==reverseField+1,"Reverse did not add exactly one card");
            Require(Count(dealer.handArea)==reverseHand,"Used reverse unexpectedly changed hand");
            Require(resources==null||resources.currentResource==resourceBefore,"Reverse spent resources");
            var placed=turn.playerBattleArea.GetComponentsInChildren<CardController>().First(c=>c.data==reverse);
            Require(!placed.hasSummonSickness,"Reverse gained summon sickness");
            Require(placed.GetComponent<CardDrag>()==null||!placed.GetComponent<CardDrag>().IsDropSnapPlaying,"Old snap replayed after reverse showcase");
            Require(!CardPlayPresentation.IsPlaying,"Reverse overlay leaked");
        }catch(Exception e){failure=e.ToString();}
        if(failure!=null){Finish("FAIL: "+failure);yield break;}
        yield return new WaitForEndOfFrame();var final=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,"field.png"),final.EncodeToPNG());UnityEngine.Object.Destroy(final);
        Finish("PASS: Real committed player-card routine: back-to-front reveal, circular ripple cue, hidden source, no field entry until presentation finishes, exactly one card leaves hand and enters field, summon sickness/input preserved, default duration "+duration.ToString("F2")+"s, overlay cleanup. Opponent-specific cyan color, upward offset and 1.6x speed verified independently; settings restored. Accepted reverse choice used the same showcase before exactly one field entry, with no resource cost, hand change, summon sickness or duplicate snap. No scene saved.");
    }
    static IEnumerator ReverseProbe(HandDealer dealer,CardData data,Action done){yield return (IEnumerator)typeof(HandDealer).GetMethod("ResolveBrokenPlayerWall",Flags).Invoke(dealer,new object[]{data});done();}
    static IEnumerator EnemyProbe(CardData data,Transform area,Action done){yield return CardPlayPresentation.Play(data,area,isEnemy:true);done();}
}
