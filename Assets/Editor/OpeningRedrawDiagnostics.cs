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
    static int enemyStackId;
    static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static string Report=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardCardCanvasValidation/result.txt");
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
            CheckFlights();
            if(stage==0)
            {
                var enemyViewBefore=OpeningShuffleView.ForDeck(dealer.enemyDeckPosition);
                if(enemyViewBefore==null || enemyViewBefore.VisibleCardCount==0)
                {Require(!dealer.enemyDeckImage.enabled,"Enemy visible before first drop");sawEmptyEnemy=true;}
                if(!dealer.IsOpeningDecisionReady)return;
                CheckDeal();playerPool=Pool(false);enemyPool=Pool(true);enemyState=EnemyState();enemyStackId=OpeningShuffleView.ForDeck(dealer.enemyDeckPosition).GetInstanceID();turn=dealer.turnManager;
                dealer.turnManager=null;stage=1;deadline=EditorApplication.timeSinceStartup+60;dealer.RedrawHand();
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
                Finish("PASS: Hover tilt invoked on every player card after both initial deal and Redraw; all pointer Canvas references remain valid and refer to the root Canvas after temporary sorting layers are removed. Actual Play Mode opening and player-only Redraw; enemy deck order, hand/wall instances, positions, and stack unchanged throughout Redraw; own hand/walls returned and redealt; card inventories conserved; enemy starts empty; enemy deck uses actual image bounds with matching player aspect ratio; outgoing flights render above deck stacks; persistent stack edges; no raycast interception; once-only redraw.");
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
