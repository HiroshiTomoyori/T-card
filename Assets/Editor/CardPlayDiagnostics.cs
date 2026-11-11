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
        // Exercise the real two-wall break and select the second card first.
        var walls = dealer.wallArea.GetComponentsInChildren<CardController>()
            .Where(c => c.transform.parent == dealer.wallArea).Take(3).ToArray();
        if(walls.Length != 3){Finish("FAIL: expected three available Wall cards");yield break;}
        var first = UnityEngine.Object.Instantiate(data);
        var second = UnityEngine.Object.Instantiate(data);
        first.effectTypes = second.effectTypes = new[]{EffectType.ShieldTrigger};
        walls[0].SetData(first);
        walls[1].SetData(second);
        walls[0].artworkImage.sprite = walls[1].artworkImage.sprite = dealer.cardBackSprite;
        int reverseField = Count(turn.playerBattleArea), reverseHand = Count(dealer.handArea);
        var resources = UnityEngine.Object.FindFirstObjectByType<ResourceManager>();
        int resourceBefore = resources != null ? resources.currentResource : 0;
        var reverse = ReverseChoiceManager.I;
        bool completeCheck = false;
        dealer.StartCoroutine(WallProbe(dealer, new[]{walls[0].gameObject,walls[1].gameObject},()=>completeCheck=true));
        float reverseDeadline = Time.realtimeSinceStartup + 15;
        while(reverse != null && !reverse.IsChoosing && Time.realtimeSinceStartup < reverseDeadline) yield return null;
        try
        {
            Require(reverse != null && reverse.IsChoosing && dealer.IsResolvingWallAttack,"Card Check did not open");
            var panel = reverse.TimerAnchor;
            var firstButton = panel.Find("Revealed Wall 1").GetComponent<Button>();
            var secondButton = panel.Find("Revealed Wall 2").GetComponent<Button>();
            var firstGlow = firstButton.transform.Find("Flip/AE Platinum Orbit").GetComponent<RawImage>();
            Require(firstGlow.enabled && firstGlow.texture != null,"AE glow missing on a Reverse card");
            var playButton = panel.Find("Play Selected").GetComponent<Button>();
            Require(!playButton.interactable,"Play enabled with an empty selection");
            secondButton.onClick.Invoke();
            firstButton.onClick.Invoke();
            secondButton.onClick.Invoke();
            Require(firstButton.transform.Find("Flip/Reverse Order Stamp").GetComponent<RawImage>().texture.name=="Order1","Deselect did not renumber");
            firstButton.onClick.Invoke();
            secondButton.onClick.Invoke();
            firstButton.onClick.Invoke();
            Require(Count(turn.playerBattleArea)==reverseField,"A Reverse entered the field before commitment");
        
        }
        catch(Exception e){Finish("FAIL: "+e);yield break;}
        yield return new WaitForEndOfFrame();
        var checkImage=ScreenCapture.CaptureScreenshotAsTexture();
        if(checkImage!=null){File.WriteAllBytes(Path.Combine(Folder,"reverse-check.png"),checkImage.EncodeToPNG());UnityEngine.Object.Destroy(checkImage);}
        reverse.TimerAnchor.Find("Play Selected").GetComponent<Button>().onClick.Invoke();
        bool reverseSeen = false, correctFirst = false, sawSecondCue = false;
        while(!completeCheck && Time.realtimeSinceStartup < reverseDeadline)
        {
            if(reverse.IsPlayingCue)
            {
                reverseSeen = true;
                int fieldCount = Count(turn.playerBattleArea);
                if(fieldCount == reverseField+1)
                {
                    sawSecondCue = true;
                    correctFirst = turn.playerBattleArea.GetComponentsInChildren<CardController>().Any(c=>c.data==second);
                }
            }
            yield return null;
        }
        try
        {
            Require(completeCheck && reverseSeen && sawSecondCue && correctFirst,"Reverse cards did not play in the selected order");
            Require(Count(turn.playerBattleArea)==reverseField+2,"Two Reverse cards did not enter the field");
            Require(Count(dealer.handArea)==reverseHand,"Played Reverse cards unexpectedly changed hand");
            Require(resources==null || resources.currentResource==resourceBefore,"Reverse spent resources");
            foreach(var placed in turn.playerBattleArea.GetComponentsInChildren<CardController>().Where(c=>c.data==first||c.data==second))
                Require(!placed.hasSummonSickness,"Reverse gained summon sickness");
            Require(!reverse.IsPlayingCue && !PresentationSequenceLock.IsLocked,"Reverse presentation leaked");
        }
        catch(Exception e){Finish("FAIL: "+e);yield break;}

        var regular = UnityEngine.Object.Instantiate(data);
        regular.effectTypes = Array.Empty<EffectType>();
        walls[2].SetData(regular);
        walls[2].artworkImage.sprite = dealer.cardBackSprite;
        completeCheck = false;
        dealer.StartCoroutine(WallProbe(dealer,new[]{walls[2].gameObject},()=>completeCheck=true));
        reverseDeadline = Time.realtimeSinceStartup+12;
        while(!reverse.IsChoosing && Time.realtimeSinceStartup<reverseDeadline) yield return null;
        try
        {
            Require(reverse.IsChoosing,"Regular Wall did not open Card Check");
            Require(!reverse.TimerAnchor.Find("Play Selected").gameObject.activeSelf,"Regular Wall exposed a Play action");
            Require(!reverse.TimerAnchor.Find("Revealed Wall 1/Flip/AE Platinum Orbit").GetComponent<RawImage>().enabled,"Regular Wall acquired a Reverse glow");
            reverse.ResolveInactivity();
        }
        catch(Exception e){Finish("FAIL: "+e);yield break;}
        while(!completeCheck && Time.realtimeSinceStartup<reverseDeadline) yield return null;
        try { Require(completeCheck && Count(dealer.handArea)==reverseHand+1,"Regular Wall did not reach the hand after timeout"); }
        catch(Exception e){Finish("FAIL: "+e);yield break;}
        Finish("PASS: player and enemy card-play presentations, actual two-Wall Card Check, AE glow and order stamp renumbering, committed reverse play order and cue before field entry, no resource spend, regular Wall to hand on timeout. No scene saved.");
    }
    static IEnumerator WallProbe(HandDealer dealer, GameObject[] walls, Action done)
    {
        yield return dealer.DamagePlayerWallsAndWait(walls);
        done();
    }
    static IEnumerator EnemyProbe(CardData data,Transform area,Action done){yield return CardPlayPresentation.Play(data,area,isEnemy:true);done();}
}
