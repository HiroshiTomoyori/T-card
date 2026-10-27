using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class OpeningRedrawDiagnostics
{
    static int stage;static double deadline;static HandDealer dealer;static TurnManager turn;
    static string playerPool,enemyPool,enemyState;static bool sawShuffle,sawWall,sawForeground,sawEmptyEnemy;
    static int enemyStackId,stableSamples,resourceBefore;
    static bool sawFade;
    static ResourcePhaseManager rpm;static GameObject charged;
    static string reservedHand;
    static readonly Dictionary<int,Vector3> landed=new Dictionary<int,Vector3>();
    static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static string Report=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardOpeningUIFlowValidation/result.txt");
    static OpeningRedrawDiagnostics(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/T-card/Validate Opening Deal and Redraw (Play)")]
    static void Begin(){SessionState.SetBool("TCardRedrawProbe",true);stage=0;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.isPlaying=true;}
    static List<CardData> Deck(string name)=>(List<CardData>)typeof(HandDealer).GetField(name,flags).GetValue(dealer);
    static IEnumerable<CardData> Cards(Transform area)=>area==null?Enumerable.Empty<CardData>():area.GetComponentsInChildren<CardController>().Where(c=>c.data!=null).Select(c=>c.data);
    static string Pool(bool enemy)
    {
        var data=new List<CardData>(Deck(enemy?"enemyDeck":"currentDeck"));
        if(enemy){data.AddRange(dealer.enemyHandCards);data.AddRange(Cards(dealer.enemyWallArea));}
        else{data.AddRange(Cards(dealer.handArea));data.AddRange(Cards(dealer.wallArea));}
        return string.Join(",",data.Select(c=>c.GetInstanceID()).OrderBy(i=>i));
    }
    static string EnemyState()
    {
        var ids=new List<string>();
        ids.Add("deck:"+string.Join(",",Deck("enemyDeck").Select(c=>c.GetInstanceID())));
        ids.Add("hand:"+string.Join(",",dealer.enemyHandCards.Select(c=>c.GetInstanceID())));
        foreach(var area in new[]{dealer.enemyHandArea,dealer.enemyWallArea})
            foreach(Transform c in area)if(c.gameObject.activeSelf && c.GetComponent<CardController>()!=null)
                ids.Add(c.GetInstanceID()+":"+c.localPosition.ToString("F3")+":"+c.localScale.ToString("F3"));
        return string.Join("|",ids);
    }
    static void CheckFlights()
    {
        foreach(var flight in UnityEngine.Object.FindObjectsByType<OpeningDealLayer>(FindObjectsSortMode.None))
        {
            var canvas=flight.GetComponent<Canvas>();if(canvas==null)continue;
            var stack=OpeningShuffleView.ForDeck(flight.transform.IsChildOf(dealer.enemyHandArea)||flight.transform.IsChildOf(dealer.enemyWallArea)?dealer.enemyDeckPosition:dealer.deckPosition);
            if(stack==null)continue;
            Require(canvas.overrideSorting && canvas.sortingOrder>stack.OverlayCanvas.sortingOrder,"Flight rendered below deck");sawForeground=true;
        }
    }
    static void Require(bool ok,string reason){if(!ok)throw new Exception(reason);}
    static void CheckSortedDeal()
    {
        if(!HandDealer.IsRedrawSelecting || dealer.proHandGroup==null)return;
        var cards=dealer.proHandGroup.cards.Where(c=>c!=null).ToList();if(cards.Count!=dealer.dealCount)return;
        string order=string.Join(",",cards.Select(c=>c.GetInstanceID()));
        if(reservedHand==null)reservedHand=order;else Require(order==reservedHand,"Opening hand reordered after dealing started");
        int previous=-1;bool gap=false;
        foreach(var card in cards)
        {
            var data=card.GetComponent<CardController>().data;
            int rank=(int)typeof(HandDealer).GetMethod("GetRankOrder",flags).Invoke(dealer,new object[]{data.cardName??""});
            int key=rank*10+(int)data.suit;Require(key>=previous,"Opening hand not sorted before first flight");previous=key;
            var alpha=card.GetComponent<CanvasGroup>().alpha;
            if(alpha<.5f){gap=true;continue;}Require(!gap,"Cards launched out of sorted order");
            if(card.GetComponent<OpeningDealLayer>()!=null)continue;
            int id=card.GetInstanceID();if(landed.TryGetValue(id,out var position)){Require(Vector3.Distance(position,card.transform.position)<.05f,"Landed opening card moved when later card arrived");stableSamples++;}else landed[id]=card.transform.position;
        }
    }
    static void CheckDiscard(bool hidden)
    {
        var tm=turn!=null?turn:UnityEngine.Object.FindFirstObjectByType<TurnManager>();
        foreach(var area in new[]{tm.playerGraveyard,tm.enemyGraveyard})
        {
            var group=area.GetComponent<CanvasGroup>();Require(group!=null,"Discard visibility group missing");
            if(hidden)Require(group.alpha==0 && !group.blocksRaycasts,"Discard visible or interactive before initial choice");
            else{if(group.alpha>0 && group.alpha<1)sawFade=true;}
        }
    }
    static void CheckHoverCanvas()
    {
        foreach(var card in dealer.handArea.GetComponentsInChildren<CCP.Card>())
        {
            var property=typeof(CCP.Card).GetProperty("parentCanvas",flags);
            var field=typeof(CCP.Card).GetField("parentCanvas",flags);
            var canvas=property!=null?(Canvas)property.GetValue(card):(Canvas)field.GetValue(card);
            Require(canvas!=null,"Card cached destroyed Canvas after deal");
            Require(canvas==card.GetComponentInParent<Canvas>().rootCanvas,"Card pointer Canvas must be root, not temporary sorting Canvas");
            if(UnityEngine.InputSystem.Mouse.current!=null && card.parentCardGroup!=null)
                typeof(CCP.Card).GetMethod("CalculateHoverTilt",flags).Invoke(card,null);
        }
    }
    static void CheckDeal()
    {
        CheckHoverCanvas();
        Require(Cards(dealer.handArea).Count()==dealer.dealCount,"Player hand count");
        Require(dealer.enemyHandCards.Count==dealer.dealCount,"Enemy hand data count");
        Require(Cards(dealer.wallArea).Count()==dealer.wallCount,"Player wall count");
        Require(Cards(dealer.enemyWallArea).Count()==dealer.enemyWallCount,"Enemy wall count");
        Require(!dealer.IsWallPlacementRunning,"Wall animation still running");
        foreach(var c in dealer.enemyHandArea.GetComponentsInChildren<CardController>())Require(c.artworkImage.sprite==dealer.cardBackSprite,"Enemy hand exposed");
        foreach(var area in new[]{dealer.wallArea,dealer.enemyWallArea})foreach(var c in area.GetComponentsInChildren<CardController>())
        {Require(c.artworkImage.sprite==dealer.cardBackSprite,"Wall exposed");Require(c.transform.localScale.sqrMagnitude>2.9f,"Wall invisible after flight");}
        var views=UnityEngine.Object.FindObjectsByType<OpeningShuffleView>(FindObjectsSortMode.None);
        Require(views.Length==2,"Expected persistent stack on both decks");
        var playerView=OpeningShuffleView.ForDeck(dealer.deckPosition);var enemyView=OpeningShuffleView.ForDeck(dealer.enemyDeckPosition);
        Require(playerView!=null && enemyView!=null,"Both deck views required");
        Require(Mathf.Abs(playerView.RenderedCardSize.x/playerView.RenderedCardSize.y-enemyView.RenderedCardSize.x/enemyView.RenderedCardSize.y)<.001f,"Deck aspect mismatch");
        Require(enemyView.Source==dealer.enemyDeckImage.rectTransform,"Enemy used container instead of image");
        Require(!dealer.enemyDeckImage.enabled,"Enemy static placeholder visible");
        foreach(var view in views)
        {
            Require(!view.OverlayCanvas.overrideSorting,"Settled deck still uses foreground sorting");
            foreach(var hand in new[]{dealer.handArea,dealer.enemyHandArea})
            {
                Transform branch=hand;var parent=view.OverlayCanvas.transform.parent;
                while(branch.parent!=parent && branch.parent!=null)branch=branch.parent;
                Require(branch.parent==parent && view.OverlayCanvas.transform.GetSiblingIndex()<branch.GetSiblingIndex(),"Deck not behind hand in Canvas hierarchy");
            }
            Require(view.VisibleCardCount==dealer.openingShuffleSettings.visualCardCount,"Persistent stack missing cards");
            var images=view.OverlayCanvas.GetComponentsInChildren<Image>();
            Require(images.Length<2 || Vector3.Distance(images[0].transform.position,images[images.Length-1].transform.position)>.01f,"Stack edges collapsed");
            foreach(var image in images)Require(!image.raycastTarget,"Stack intercepts input");
        }
    }
    static void Tick()
    {
        if(!SessionState.GetBool("TCardRedrawProbe",false))return;
        if(EditorApplication.isCompiling||!EditorApplication.isPlaying)return;
        try
        {
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Runtime validation timeout");
            if(dealer==null)dealer=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(dealer==null)return;
            CheckFlights();CheckSortedDeal();
            if(HandDealer.IsRedrawSelecting)CheckDiscard(true);
            if(stage==0)
            {
                var enemyViewBefore=OpeningShuffleView.ForDeck(dealer.enemyDeckPosition);
                if(enemyViewBefore==null || enemyViewBefore.VisibleCardCount==0)
                {Require(!dealer.enemyDeckImage.enabled,"Enemy visible before first drop");sawEmptyEnemy=true;}
                if(!dealer.IsOpeningDecisionReady)return;
                CheckDeal();playerPool=Pool(false);enemyPool=Pool(true);enemyState=EnemyState();enemyStackId=OpeningShuffleView.ForDeck(dealer.enemyDeckPosition).GetInstanceID();turn=dealer.turnManager;
                dealer.turnManager=null;stage=1;reservedHand=null;landed.Clear();deadline=EditorApplication.timeSinceStartup+60;dealer.RedrawHand();
            }
            else if(stage==1)
            {
                var shuffle=UnityEngine.Object.FindFirstObjectByType<DeckShuffle>();sawShuffle|=shuffle!=null && shuffle.IsShuffling;sawWall|=dealer.IsWallPlacementRunning;
                Require(EnemyState()==enemyState,"Enemy changed during player Redraw");
                Require(OpeningShuffleView.ForDeck(dealer.enemyDeckPosition).GetInstanceID()==enemyStackId,"Enemy stack recreated during player Redraw");
                if(HandDealer.IsRedrawSelecting)return;
                CheckDeal();Require(sawShuffle,"Redraw shuffle not observed");Require(sawWall,"Redraw wall deal not observed");
                Require(Pool(false)==playerPool,"Player cards lost or duplicated");Require(Pool(true)==enemyPool,"Enemy cards lost or duplicated");
                Require(!(bool)typeof(HandDealer).GetField("canRedraw",flags).GetValue(dealer),"Repeated redraw allowed");
                Require(sawForeground,"Foreground deal was not observed");Require(sawEmptyEnemy,"Empty enemy start not observed");
                Require(stableSamples>0,"No stable opening arrival observed");
                stage=2;
            }
            else if(stage==2)
            {
                CheckDiscard(false);
                var presentation=dealer.GetComponent<OpeningDiscardPresentation>();if(!presentation.IsRevealed)return;
                Require(dealer.openingShuffleSettings.discardFadeDuration<=0 || sawFade,"Discard did not fade in");
                foreach(var area in new[]{turn.playerGraveyard,turn.enemyGraveyard})Require(area.GetComponent<CanvasGroup>().alpha>.99f,"Discard still invisible after decision");
                rpm=UnityEngine.Object.FindFirstObjectByType<ResourcePhaseManager>();Require(rpm!=null,"Resource phase missing");rpm.StartResourcePhase();
                resourceBefore=rpm.resourceManager.MaxResource;
                var card=dealer.handArea.GetComponentsInChildren<CardDrag>().First();charged=card.gameObject;
                typeof(CardDrag).GetField("canDrag",flags).SetValue(card,true);
                var zone=UnityEngine.Object.FindObjectsByType<ResourceDropZone>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(z=>z.resourcePhaseManager==rpm);
                zone.OnDrop(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerDrag=card.gameObject});
                Require(rpm.IsResolvingCharge,"Valid drop was not accepted immediately");Require(!turn.IsPlayerDecisionOpen,"Charge opened confirmation window");
                rpm.TryChargeResource(card);stage=3;
            }
            else if(stage==3)
            {
                Require(!turn.IsPlayerDecisionOpen,"Charge opened delayed confirmation");
                if(charged!=null)return;
                Require(rpm.resourceManager.MaxResource==resourceBefore+1,"Charge did not add exactly one point");
                Finish("PASS: Actual Unity opening and player-only Redraw; both settled decks behind hands; cards drawn randomly then sorted before launch, reserved final slots and no movement of landed cards when later cards arrive; discards hidden and noninteractive before decision, then fade visible; actual ResourceDropZone.OnDrop immediately starts charge without confirmation and adds exactly one resource even on repeated request. Previous hover, inventory, enemy invariance, stack aspect and foreground flight checks passed.");
            }
        }
        catch(Exception e){Finish("FAIL: "+e);}
    }
    static void Finish(string result)
    {
        if(dealer!=null)dealer.turnManager=turn;SessionState.SetBool("TCardRedrawProbe",false);stage=0;deadline=0;
        Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllText(Report,result);Debug.Log(result);EditorApplication.isPlaying=false;
    }
}
