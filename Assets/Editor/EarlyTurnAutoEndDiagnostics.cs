using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EarlyTurnAutoEndDiagnostics
{
    const string Key = "TCard.EarlyTurnAutoEndProbe";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static bool started, opened, savedSkip;
    static double deadline;
    static Transform archive;
    static readonly List<string> results = new List<string>();
    static string Folder => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/TCardEarlyTurnAutoEndValidation");
    static EarlyTurnAutoEndDiagnostics() { EditorApplication.update += Tick; }
    [MenuItem("Tools/T-card/Validate Early Turn Auto End (Play)", false, -3)]
    static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        started = opened = false; results.Clear();
        SessionState.SetBool(Key, true);
        deadline = EditorApplication.timeSinceStartup + 150;
        EditorApplication.isPaused = false;
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 150;
        if (EditorApplication.timeSinceStartup > deadline) { Finish("FAIL: opening timeout"); return; }
        if (!EditorApplication.isPlaying || started) return;
        if (!opened)
        {
            var title = UnityEngine.Object.FindFirstObjectByType<TitleManager>();
            if (title != null) { title.SelectNormal(); opened = true; }
        }
        var dealer = UnityEngine.Object.FindFirstObjectByType<HandDealer>();
        if (dealer == null || !dealer.IsOpeningDecisionReady) return;
        started = true; savedSkip = TitleManager.SkipActionConfirmations;
        dealer.StartCoroutine(Guarded(Probe(dealer)));
    }
    static void Finish(string text)
    {
        typeof(TitleManager).GetField("<SkipActionConfirmations>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, savedSkip);
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder, "result.txt"), text + "\n" + string.Join("\n", results));
        Debug.Log(text); SessionState.SetBool(Key, false); EditorApplication.isPlaying = false;
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
        Finish("PASS: " + results.Count + " early-turn auto-end runtime checks. No scene/settings saved.");
    }
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Flags).Invoke(obj, args);
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
    static bool Get(TurnManager turn, string name) => (bool)typeof(TurnManager).GetField(name, Flags).GetValue(turn);
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); results.Add("PASS: " + message); }
    static void Clear(Transform parent)
    {
        foreach (Transform child in parent.Cast<Transform>().ToArray()) { child.gameObject.SetActive(false); child.SetParent(archive, false); }
    }
    static CardController Card(Transform area, string rank)
    {
        var go = new GameObject("Auto-end probe " + rank, typeof(RectTransform), typeof(CanvasGroup), typeof(CardController));
        go.transform.SetParent(area, false);
        var card = go.GetComponent<CardController>();
        card.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_" + rank + ".asset"));
        if (card.data == null) throw new Exception("Missing card " + rank);
        return card;
    }
    static void Ready(TurnManager turn, ResourceManager resources, int number, int maximum, int current)
    {
        turn.StopAllCoroutines();
        foreach (var fx in UnityEngine.Object.FindObjectsByType<TurnSignalPresentation>(FindObjectsSortMode.None))
        { fx.gameObject.SetActive(false); UnityEngine.Object.Destroy(fx.gameObject); }
        Call(turn, "RejectPlayerDecision");
        turn.resourcePhaseManager.EndResourcePhase();
        Clear(turn.handDealer.handArea); Clear(turn.playerBattleArea); Clear(turn.enemyBattleArea);
        Set(resources, "maxResource", maximum); resources.currentResource = current;
        Set(turn, "turnCount", number); Set(turn, "isPlayerTurnActive", true); Set(turn, "isEndingTurn", false);
        Set(turn, "isFirstTurnStarting", false); Set(turn, "isPlayerMainPhase", true); Set(turn, "isBattlePhase", false);
        Set(turn, "earlyTurnAutoEndSuppressed", false); Set(turn, "inactivityTimersEnabled", false);
        Set(turn, "selectingTarget", false); Set(turn, "waitingBlockSelect", false); Set(turn, "resolvingPlayerCardAttack", false);
        Set(turn, "playerExtraTurnRequested", true); // Keep transition tests away from enemy AI/deck mutations.
        Call(turn, "SyncEndTurnButtonVisibility");
    }
    static bool Auto(TurnManager turn) => (bool)Call(turn, "ShouldAutoEndEarlyTurn");
    static void ExpireMain(TurnManager turn)
    {
        var timer = (DecisionCountdown)typeof(TurnManager).GetField("mainTurnCountdown", Flags).GetValue(turn);
        timer.Reset(); timer.Advance("player-turn", 25f, 0f, false, false);
        typeof(DecisionCountdown).GetField("<Remaining>k__BackingField", Flags).SetValue(timer, 0f);
        Set(turn, "inactivityTimersEnabled", true); Call(turn, "TickDecisionTimer"); Set(turn, "inactivityTimersEnabled", false);
    }
    static void BattleTimeout(TurnManager turn)
    {
        var kind = typeof(TurnManager).GetNestedType("TimedDecision", BindingFlags.NonPublic);
        Call(turn, "ResolveDecisionTimeout", Enum.Parse(kind, "Battle"));
    }
    static IEnumerator Probe(HandDealer dealer)
    {
        var turn = dealer.turnManager;
        dealer.turnManager = null; Call(dealer, "CompleteOpeningSelection"); dealer.turnManager = turn;
        yield return new WaitForSecondsRealtime(.5f);
        Set(turn, "inactivityTimersEnabled", false);
        archive = new GameObject("Auto-end probe archive").transform; archive.gameObject.SetActive(false);
        var resources = UnityEngine.Object.FindFirstObjectByType<ResourceManager>();
        typeof(TitleManager).GetField("<SkipActionConfirmations>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
        Check(resources != null && turn.resourcePhaseManager.resourceManager == resources, "correct player resource owner");
        for (int n = 1; n <= 3; n++)
        {
            Ready(turn, resources, n, n, n); Card(dealer.handArea, "7");
            Check(Auto(turn), "turn " + n + " with no affordable hand/attack can auto-end");
        }
        foreach (int n in new[] { 4, 5, 6 })
        {
            Ready(turn, resources, n, 3, 0); Card(dealer.handArea, "7");
            Check(!Auto(turn), "turn " + n + " never uses early auto-end");
            ExpireMain(turn); BattleTimeout(turn); turn.RunAutomaticDecision(turn.EndPlayerTurn);
            Check(Get(turn, "isPlayerTurnActive") && !Get(turn, "isEndingTurn"), "turn " + n + " also survives main/battle timeout and automatic callers");
        }
        Ready(turn, resources, 3, 3, 0); var cheap = Card(dealer.handArea, "3");
        Check(!Auto(turn), "cost equals MAX 3 prevents auto-end even when CURRENT is 0");
        cheap.transform.SetParent(archive); cheap.gameObject.SetActive(false);
        Check(!Auto(turn), "affordable-card protection remains after card leaves hand");
        ExpireMain(turn); BattleTimeout(turn);
        Check(Get(turn, "isPlayerTurnActive") && !Get(turn, "isEndingTurn"), "protected turn survives timer expiry");
        Ready(turn, resources, 3, 3, 3); Card(dealer.handArea, "7");
        var attacker = Card(turn.playerBattleArea, "4");
        Check(!Auto(turn), "available field attacker prevents auto-end");
        attacker.Tap(); Check(Auto(turn), "last attacker resting leaves no action and permits immediate auto-end");
        Ready(turn, resources, 3, 3, 3); Card(dealer.handArea, "K"); var basis = Card(turn.playerBattleArea, "10"); basis.Tap();
        Check(!Auto(turn), "legally discounted K still prevents premature auto-end");
        Ready(turn, resources, 3, 3, 3); Card(dealer.handArea, "7");
        turn.resourcePhaseManager.StartResourcePhase();
        Check(!Auto(turn), "uncharged resource phase is not skipped");
        Set(turn.resourcePhaseManager, "hasCharged", true);
        Check(!Auto(turn), "charge animation is protected");
        turn.EndPlayerTurn(); Check(!turn.IsPlayerDecisionOpen && !Get(turn, "isEndingTurn"), "manual end cannot interrupt resource charge");
        turn.resourcePhaseManager.EndResourcePhase();
        Set(dealer, "<IsTurnDrawAnimating>k__BackingField", true);
        Check(!Auto(turn), "turn-start draw is protected");
        Set(dealer, "<IsTurnDrawAnimating>k__BackingField", false);
        using (var lease = PresentationSequenceLock.Acquire(turn))
        {
            Check(!Auto(turn), "effect/input lock prevents auto-end");
            turn.EndPlayerTurn(); Check(!turn.IsPlayerDecisionOpen && !Get(turn, "isEndingTurn"), "manual end also respects effect lock");
        }
        Ready(turn, resources, 3, 3, 3); Card(dealer.handArea, "7");
        var moving = UnityEngine.Object.Instantiate(dealer.proHandCardPrefab, dealer.handArea);
        moving.GetComponent<CardController>().SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_7.asset"));
        var drag = moving.GetComponent<CardDrag>(); Set(drag, "playPresentationRunning", true);
        Check(!Auto(turn), "moving card prevents auto-end even outside presentation lock");
        Set(drag, "playPresentationRunning", false); moving.SetActive(false); moving.transform.SetParent(archive);
        Set(turn, "isSelectingDestroyTarget", true);
        Check(!Auto(turn), "unresolved effect target prevents auto-end"); Set(turn, "isSelectingDestroyTarget", false);
        Ready(turn, resources, 3, 3, 3); Card(dealer.handArea, "7");
        Check(turn.endTurnButton.interactable, "end-turn button available in idle main phase");
        turn.endTurnButton.onClick.Invoke();
        Check(turn.IsPlayerDecisionOpen && !Get(turn, "isEndingTurn"), "real end-turn button opens its own confirmation");
        ExpireMain(turn); BattleTimeout(turn); Call(turn, "TryAutoEndEarlyTurn"); turn.EndPlayerTurn();
        Check(turn.IsPlayerDecisionOpen && !Get(turn, "isEndingTurn"), "timers/auto-end/repeated click cannot dismiss or bypass manual confirmation");
        Call(turn, "RejectPlayerDecision");
        Check(!Auto(turn) && Get(turn, "isPlayerTurnActive"), "cancelling manual end retains player control for the turn");
        int ended = 0;
        Application.LogCallback counter = (message, stack, type) => { if (message == "自ターン終了") ended++; };
        Application.logMessageReceived += counter;
        turn.EndPlayerTurn(); Call(turn, "AcceptPlayerDecision"); turn.EndPlayerTurn(); Call(turn, "TryAutoEndEarlyTurn");
        Check(ended == 1 && !Get(turn, "isPlayerTurnActive") && Get(turn, "isEndingTurn"), "approved manual end commits exactly once despite repeated/automatic calls");
        Application.logMessageReceived -= counter;
        turn.OnResourcePhaseComplete();
        Check(!Get(turn, "isPlayerMainPhase"), "late resource callback cannot restart a manually ended turn");
        Ready(turn, resources, 3, 3, 3); Card(dealer.handArea, "7"); ended = 0;
        Application.logMessageReceived += counter;
        Check((bool)Call(turn, "TryAutoEndEarlyTurn"), "no-action auto-end commits immediately");
        turn.EndPlayerTurn(); Call(turn, "TryAutoEndEarlyTurn"); BattleTimeout(turn);
        Check(ended == 1 && !turn.IsPlayerDecisionOpen && Get(turn, "isEndingTurn"), "automatic end advances once without a confirmation or button race");
        Application.logMessageReceived -= counter;
        Ready(turn, resources, 3, 3, 3); Set(turn, "earlyTurnAutoEndSuppressed", true);
        Set(turn, "isPlayerTurnActive", false); Set(turn, "isPlayerFirst", true);
        var savedDealer = turn.handDealer; turn.handDealer = null;
        Call(turn, "StartPlayerTurn", true); turn.handDealer = savedDealer;
        Check(!Get(turn, "earlyTurnAutoEndSuppressed"), "real next-turn entry resets per-turn protection");
        // Let the actual LateUpdate path finish a fresh no-action main phase.
        Ready(turn, resources, 3, 3, 3); Card(dealer.handArea, "7"); ended = 0;
        Application.logMessageReceived += counter;
        yield return null; yield return null;
        Check(ended == 1 && Get(turn, "isEndingTurn"), "LateUpdate notices no actions without waiting for inactivity timeout");
        Application.logMessageReceived -= counter;
        turn.StopAllCoroutines();
        foreach (var fx in UnityEngine.Object.FindObjectsByType<TurnSignalPresentation>(FindObjectsSortMode.None))
        { fx.gameObject.SetActive(false); UnityEngine.Object.Destroy(fx.gameObject); }
        Set(turn, "isPlayerTurnActive", false);
        yield return null;
        Check(!PresentationSequenceLock.IsLocked && Time.timeScale > 0f, "test transitions release presentation lock/clock");
    }
}
