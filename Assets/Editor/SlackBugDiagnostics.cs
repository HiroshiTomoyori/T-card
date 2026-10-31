using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Runs only on an explicit menu request; never saves the scene.
[InitializeOnLoad]
public static class SlackBugDiagnostics
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static string Folder => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/TCardSlackBugValidation");
    static bool launched, started;
    static double deadline;
    static readonly List<string> results = new List<string>();
    static Transform archive;
    static SlackBugDiagnostics() { EditorApplication.update += Tick; }
    [MenuItem("Tools/T-card/Validate Slack Bug Fixes (Play)")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity") return;
        launched=started=false;
        Directory.CreateDirectory(Folder);
        SessionState.SetBool("TCardSlackBugProbe", true);
        deadline = EditorApplication.timeSinceStartup + 180;
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!SessionState.GetBool("TCardSlackBugProbe", false) || !EditorApplication.isPlaying || started) return;
        if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 180;
        if (EditorApplication.timeSinceStartup > deadline) { Finish("FAIL: opening timeout"); return; }
        var title = UnityEngine.Object.FindFirstObjectByType<TitleManager>();
        if (!launched && title != null && title.difficultyScreen != null && title.difficultyScreen.activeInHierarchy)
        { launched = true; title.SelectNormal(); }
        var dealer = UnityEngine.Object.FindFirstObjectByType<HandDealer>();
        if (dealer == null || !dealer.IsOpeningDecisionReady) return;
        started = true; dealer.StartCoroutine(Guarded(Probe(dealer)));
    }
    static void Finish(string message)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder,"result.txt"), message + "\n" + string.Join("\n",results));
        Debug.Log(message); SessionState.SetBool("TCardSlackBugProbe",false);
        EditorApplication.isPlaying = false;
    }
    static IEnumerator Guarded(IEnumerator probe)
    {
        while (true)
        {
            object current;
            try { if (!probe.MoveNext()) break; current = probe.Current; }
            catch (Exception e) { Finish("FAIL: " + e); yield break; }
            yield return current;
        }
        Finish("PASS: " + results.Count + " Unity runtime checks. No scene saved.");
    }
    static object Call(object obj,string method,params object[] args) => obj.GetType().GetMethod(method,Flags).Invoke(obj,args);
    static void Set(object obj,string field,object value) => obj.GetType().GetField(field,Flags).SetValue(obj,value);
    static void Check(bool value,string name) { if (!value) throw new Exception(name); results.Add("PASS: " + name); }
    static void Clear(Transform area)
    {
        foreach (Transform child in area.Cast<Transform>().ToArray())
        { child.gameObject.SetActive(false); child.SetParent(archive,false); }
    }
    static CardController Add(HandDealer d,Transform parent,string asset,bool prefab=false)
    {
        var go = prefab ? UnityEngine.Object.Instantiate(d.proHandCardPrefab,parent) : new GameObject(asset,typeof(RectTransform),typeof(CanvasGroup),typeof(CardController));
        go.transform.SetParent(parent,false);
        var c = go.GetComponent<CardController>();
        c.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/" + asset + ".asset"));
        if (c.data == null) throw new Exception("Missing card " + asset);
        c.data.SetPowerFromName(); return c;
    }
    static IEnumerator Probe(HandDealer d)
    {
        results.Clear();
        var t = d.turnManager;
        d.turnManager = null; Call(d,"CompleteOpeningSelection"); d.turnManager = t;
        Set(t,"inactivityTimersEnabled",false); Set(t,"isPlayerTurnActive",true); Set(t,"isPlayerMainPhase",true);
        GameSettings.IsAdvancedRule = false;
        archive = new GameObject("Slack probe archive").transform; archive.gameObject.SetActive(false);
        Clear(t.playerBattleArea); Clear(t.enemyBattleArea); Clear(d.handArea);
        yield return null;
        var effects = CardEffectManager.I;
        Check(effects != null,"effect manager exists");
        foreach (bool enemy in new[]{false,true}) foreach (bool reverse in new[]{false,true})
        {
            Clear(t.playerBattleArea); Clear(t.enemyBattleArea);
            var own = enemy ? t.enemyBattleArea : t.playerBattleArea;
            var foe = enemy ? t.playerBattleArea : t.enemyBattleArea;
            var ally = Add(d,own,"Spade_2"); var victim = Add(d,foe,"Heart_2");
            var otherJoker=Add(d,own,"Joker_1");
            var joker = Add(d,own,"Joker_1");
            if(reverse) joker.MarkPlayedFromReverse();
            effects.ActivateOnSummon(joker,reverse,enemy);
            Check(ally.transform.parent == (enemy?t.enemyGraveyard:t.playerGraveyard) && joker.transform.parent == own,"Joker clears allied field but retains source: enemy="+enemy+" reverse="+reverse);
            Check(victim.transform.parent == (enemy?t.playerGraveyard:t.enemyGraveyard),"Joker clears opposing field: enemy="+enemy+" reverse="+reverse);
            Check(otherJoker.transform.parent == (enemy?t.enemyGraveyard:t.playerGraveyard),"Other Joker is also cleared: enemy="+enemy+" reverse="+reverse);
            Check(CardActionIcon.CanAttack(joker),"Joker can attack: enemy="+enemy+" reverse="+reverse);
        }
        Clear(t.playerBattleArea); Clear(t.enemyBattleArea);
        var jack = Add(d,t.enemyBattleArea,"Spade_J"); var ten = Add(d,t.playerBattleArea,"Spade_10"); ten.Tap();
        Check((bool)Call(t,"HasSpecialWin",ten,jack),"10 wins against Jack using real Spade_J asset name");
        Call(t,"ResolveCardBattle",jack,ten);
        Check(jack.transform.parent==t.enemyGraveyard&&ten.transform.parent==t.playerBattleArea,"rested 10 defeats attacking Jack");
        var six = Add(d,t.playerBattleArea,"Spade_6");six.BeginNormalPlay();six.SetSummonSickness(true);
        Check(!CardActionIcon.CanAttack(six),"newly played 6 cannot attack");
        var eight = Add(d,t.playerBattleArea,"Spade_8"); var weak = Add(d,t.enemyBattleArea,"Heart_2");
        Check(CardActionIcon.CanBlock(eight),"8 can block before resting");
        Call(t,"ResolveCardBattle",weak,eight);
        Check(eight.isTapped&&!CardActionIcon.CanBlock(eight),"surviving blocker rests and cannot block twice");
        Clear(t.playerBattleArea);Clear(t.enemyBattleArea);
        for(int i=0;i<5;i++)Add(d,t.playerBattleArea,"Spade_2");
        var tenth=Add(d,t.playerBattleArea,"Spade_10");
        d.enemyHandCards.Clear();d.enemyHandCards.Add(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Heart_2.asset"));
        effects.ActivateOnSummon(tenth);
        Check(d.enemyHandCards.Count==0,"10 discards the sole enemy hand card when entering as sixth field card");
        Check(!BattleAreaLayout.CanAccept(t.playerBattleArea),"seventh ordinary field card is rejected");
        var spell = Add(d,t.playerBattleArea,"Spade_A");
        var layout=t.playerBattleArea.GetComponent<BattleAreaLayout>();layout.Refresh();
        Check(layout.CardCount==6,"spell does not change field layout/card count");
        Check(!TCardAIUnityBridge.GetCards(t.playerBattleArea).Contains(spell),"spell excluded from combat/AI field list");
        Clear(t.playerBattleArea);
        var zone=UnityEngine.Object.FindObjectsByType<BattleDropZone>(FindObjectsSortMode.None).First(z=>z.battleArea==t.playerBattleArea);
        var resource=UnityEngine.Object.FindFirstObjectByType<ResourceManager>();
        Clear(d.handArea);resource.currentResource=14;
        var two=Add(d,d.handArea,"Spade_2",true);var drag=two.GetComponent<CardDrag>();
        var hand=d.handArea.GetComponent<HandController>();Set(hand,"isIdle",false);Set(hand,"isOpeningLook",false);
        t.resourcePhaseManager.StartResourcePhase();
        Check(t.CanDragPlayerHand&&zone.CanPlayCardByRules(two),"resource phase permits affordable play before charging");
        Canvas.ForceUpdateCanvases();
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,t.playerBattleArea.position)};
        pointer.pointerDrag=two.gameObject;
        drag.OnBeginDrag(pointer);
        Check(drag.IsDragging,"begin hand drag in uncharged resource phase");
        Check(t.resourcePhaseManager.resourceDropArea==null||!t.resourcePhaseManager.resourceDropArea.blocksRaycasts,"resource raycast does not cover field when pointer is outside resource target");
        drag.OnEndDrag(pointer);
        t.resourcePhaseManager.EndResourcePhase();
        Clear(t.playerBattleArea);Clear(d.handArea);
        var ace=Add(d,d.handArea,"Spade_A",true);var aceDrag=ace.GetComponent<CardDrag>();
        aceDrag.DropToBattleArea(t.playerBattleArea,false);
        Check(ace.GetComponent<CanvasGroup>().alpha==0,"Ace source invisible before effect resolves");
        t.StartSelectEnemyBattleToDestroy(ace);
        Check(!t.IsSelectingDestroyTarget()&&ace.transform.parent==t.playerGraveyard,"targetless Ace resolves without locking turn forever");
        // Real early-turn availability rules, including boundary resources and K discount.
        Set(t,"turnCount",5);resource.currentResource=4;var four=Add(d,d.handArea,"Spade_4");
        Check(!(bool)Call(t,"ShouldAutoEndEarlyTurn"),"resource 4 / card 4 never auto-skips turn five");
        Clear(d.handArea);resource.currentResource=3;var basis=Add(d,t.playerBattleArea,"Spade_10");basis.Tap();Add(d,d.handArea,"Spade_K");
        Check(!(bool)Call(t,"ShouldAutoEndEarlyTurn"),"discounted K remains an available action");
        yield return new WaitForEndOfFrame();
        var screenshot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,"runtime.png"),screenshot.EncodeToPNG());UnityEngine.Object.Destroy(screenshot);
    }
}
