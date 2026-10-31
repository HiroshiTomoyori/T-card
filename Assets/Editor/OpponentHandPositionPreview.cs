using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Explicit visual inspection utility; temporary play state is never saved.
[InitializeOnLoad]
public static class OpponentHandPositionPreview
{
    const string Key = "TCard.OpponentHandPositionPreview";
    static bool started, opened;
    static double deadline;
    static string Folder => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/TCardOpponentHandPositionPreview");
    static OpponentHandPositionPreview() { EditorApplication.update += Tick; }
    [MenuItem("Tools/T-card/Preview Opponent Hand Position (Play)", false, -2)]
    static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        started = opened = false; deadline = EditorApplication.timeSinceStartup + 120;
        SessionState.SetBool(Key, true); EditorApplication.isPaused = false; EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (SessionState.GetBool(Key + "Resource", false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            SessionState.SetBool(Key + "Resource", false);
            ResourceFinalDiagnostics.Validate();
        }
        if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling || !EditorApplication.isPlaying || started) return;
        if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 120;
        if (EditorApplication.timeSinceStartup > deadline) { SessionState.SetBool(Key, false); EditorApplication.isPlaying = false; return; }
        if (!opened) { var title = Object.FindFirstObjectByType<TitleManager>(); if (title != null) { title.SelectNormal(); opened = true; } }
        var dealer = Object.FindFirstObjectByType<HandDealer>();
        if (dealer != null && dealer.IsOpeningDecisionReady) { started = true; dealer.StartCoroutine(Capture(dealer)); }
    }
    static IEnumerator Capture(HandDealer dealer)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var turn = dealer.turnManager;
        dealer.turnManager = null; typeof(HandDealer).GetMethod("CompleteOpeningSelection", flags).Invoke(dealer, null); dealer.turnManager = turn;
        typeof(TurnManager).GetField("inactivityTimersEnabled", flags).SetValue(turn, false);
        var hand = dealer.handArea.GetComponent<HandController>();
        Directory.CreateDirectory(Folder); string report = "";
        foreach (int side in new[] { 1, 2, 1 })
        {
            typeof(TurnManager).GetField("actionGlowTurn", flags).SetValue(turn, side);
            if (hand.IsIdle) hand.ChangeState(false);
            yield return new WaitForSecondsRealtime(.65f); yield return new WaitForEndOfFrame();
            string label = side == 2 ? "opponent-expanded" : report.Length == 0 ? "player-expanded" : "player-restored";
            var image = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(Folder, label + ".png"), image.EncodeToPNG()); Object.Destroy(image);
            var corners = new Vector3[4]; float handTop = float.NegativeInfinity, wallBottom = float.PositiveInfinity;
            foreach (var card in dealer.handArea.GetComponentsInChildren<CardController>())
            {
                var rt = card.artworkImage != null ? card.artworkImage.rectTransform : card.transform as RectTransform;
                if (rt == null) continue; rt.GetWorldCorners(corners); foreach (var c in corners) handTop = Mathf.Max(handTop, RectTransformUtility.WorldToScreenPoint(null, c).y);
            }
            foreach (var card in dealer.wallArea.GetComponentsInChildren<CardController>())
            {
                var rt = card.artworkImage != null ? card.artworkImage.rectTransform : card.transform as RectTransform;
                if (rt == null) continue; rt.GetWorldCorners(corners); foreach (var c in corners) wallBottom = Mathf.Min(wallBottom, RectTransformUtility.WorldToScreenPoint(null, c).y);
            }
            report += label + ": localY=" + hand.transform.localPosition.y + ", baseY=" + hand.expandPosition.y + ", lowering=" + hand.opponentTurnExpandLowering + ", gapPixels=" + (wallBottom - handTop) + "\n";
        }
        // Inspect the actual turn-end commit while the AE banner pauses gameplay.
        var firstCard = hand.proHandGroup.cards[0].transform;
        float worldBefore = firstCard.position.y;
        typeof(TurnManager).GetField("isPlayerTurnActive", flags).SetValue(turn, true);
        typeof(TurnManager).GetField("isEndingTurn", flags).SetValue(turn, false);
        typeof(TurnManager).GetField("isFirstTurnStarting", flags).SetValue(turn, false);
        typeof(TurnManager).GetField("isPlayerMainPhase", flags).SetValue(turn, true);
        typeof(TurnManager).GetField("playerExtraTurnRequested", flags).SetValue(turn, false);
        typeof(TurnManager).GetField("turnCount", flags).SetValue(turn, 4);
        turn.resourcePhaseManager.EndResourcePhase();
        bool committed = (bool)typeof(TurnManager).GetMethod("CommitPlayerTurnEnd", flags).Invoke(turn, null);
        report += "turn-end-commit=" + committed + ", immediateHandTargetY=" + hand.transform.localPosition.y + "\n";
        yield return new WaitForSecondsRealtime(.18f);
        report += "during-banner: timeScale=" + Time.timeScale + ", handCardWorldDeltaY=" + (firstCard.position.y - worldBefore) + ", banner=" + TurnSignalPresentation.IsPlaying + "\n";
        var turnFx = Object.FindFirstObjectByType<TurnSignalPresentation>();
        if (turnFx != null && turnFx.Lowlight != null)
            report += "lowlightAlpha=" + turnFx.Lowlight.color.a + ", behindArtwork=" + (turnFx.Lowlight.transform.GetSiblingIndex() < turnFx.Artwork.transform.GetSiblingIndex()) + ", receivesRaycast=" + turnFx.Lowlight.raycastTarget + "\n";
        yield return new WaitForEndOfFrame();
        var bannerCapture = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(Folder, "turn-end-moving-during-banner.png"), bannerCapture.EncodeToPNG()); Object.Destroy(bannerCapture);
        float fadeDeadline = Time.realtimeSinceStartup + 3;
        while (turnFx != null && turnFx.Elapsed < 1f && Time.realtimeSinceStartup < fadeDeadline) yield return null;
        if (turnFx != null && turnFx.Lowlight != null) report += "blank-tail-lowlightAlpha=" + turnFx.Lowlight.color.a + "\n";
        turn.StopAllCoroutines();
        foreach (var fx in Object.FindObjectsByType<TurnSignalPresentation>(FindObjectsSortMode.None)) { fx.gameObject.SetActive(false); Object.Destroy(fx.gameObject); }
        typeof(TurnManager).GetField("isPlayerTurnActive", flags).SetValue(turn, false);
        typeof(TurnManager).GetField("isEndingTurn", flags).SetValue(turn, false);
        typeof(TurnManager).GetField("lowerPlayerHandForOpponent", flags).SetValue(turn, false);
        typeof(TurnManager).GetField("actionGlowTurn", flags).SetValue(turn, 1);
        hand.ChangeState(true); yield return new WaitForSecondsRealtime(.4f);
        report += "idleY=" + hand.transform.localPosition.y + ", configuredIdleY=" + hand.idlePosition.y + "\n";
        File.WriteAllText(Path.Combine(Folder, "layout.txt"), report); Debug.Log(report);
        SessionState.SetBool(Key + "Resource", true);
        SessionState.SetBool(Key, false); EditorApplication.isPlaying = false;
    }
}
