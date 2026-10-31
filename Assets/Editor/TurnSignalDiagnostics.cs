using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class TurnSignalDiagnostics
{
    const string Key = "TCard.TurnSignalProbe";
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    static bool started, opened;
    static double deadline;
    static string Folder => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/TCardTurnSignalValidation");
    static TurnSignalDiagnostics() { EditorApplication.update += Tick; }
    [MenuItem("Tools/T-card/Validate Turn Signals (Play)", false, -4)]
    static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(Key, true);
        started = opened = false;
        deadline = EditorApplication.timeSinceStartup + 140;
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling) return;
        if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 140;
        if (EditorApplication.timeSinceStartup > deadline) { Finish("FAIL: Turn signal validation timed out"); return; }
        if (!EditorApplication.isPlaying || started) return;
        if (!opened)
        {
            var title = UnityEngine.Object.FindFirstObjectByType<TitleManager>();
            if (title != null) { title.SelectNormal(); opened = true; }
        }
        var dealer = UnityEngine.Object.FindFirstObjectByType<HandDealer>();
        if (dealer != null && dealer.IsOpeningDecisionReady)
        {
            started = true;
            dealer.StartCoroutine(Probe(dealer));
        }
    }
    static void Finish(string result)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder, "result.txt"), result);
        Debug.Log(result);
        SessionState.SetBool(Key, false);
        EditorApplication.isPlaying = false;
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static IEnumerator Run(IEnumerator routine, Action done) { yield return routine; done(); }
    static IEnumerator Logo(TurnManager turn, bool opponent)
    {
        return (IEnumerator)typeof(TurnManager).GetMethod("ShowTurnLogo", Flags).Invoke(turn,
            new object[] { opponent ? turn.enemyTurnLogo : turn.playerTurnLogo, null, opponent });
    }
    static void Capture(string name)
    {
        var im = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(Folder, name + ".png"), im.EncodeToPNG());
        UnityEngine.Object.Destroy(im);
    }
    static IEnumerator Probe(HandDealer dealer)
    {
        Directory.CreateDirectory(Folder);
        var turn = dealer.turnManager;
        dealer.turnManager = null;
        typeof(HandDealer).GetMethod("CompleteOpeningSelection", Flags).Invoke(dealer, null);
        dealer.turnManager = turn;
        yield return new WaitForSecondsRealtime(.5f);
        var original = TurnSignalSettings.Load();
        TurnSignalSettings config = null;
        string failure = null;
        var cache = typeof(TurnSignalSettings).GetField("cached", BindingFlags.NonPublic | BindingFlags.Static);
        float beforeScale = Time.timeScale;
        try
        {
            Check(original != null && original.enabledEffect, "Approved turn settings not installed/enabled");
            Check(original.playerFrames.Length == 72 && original.opponentFrames.Length == 72 && original.frameRate == 60, "AE frame timing changed");
            for (int side = 0; side < 2; side++)
            {
                var frames = original.Frames(side == 1);
                for (int i = 0; i < frames.Length; i++)
                {
                    if (i >= 56) Check(frames[i] == null, "Lingering light frames still present");
                    if (frames[i] == null) continue;
                    Check(frames[i].width == 1800 && frames[i].height == 400, "Frame downscaled or crop distorted");
                    var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(frames[i]));
                    Check(!importer.mipmapEnabled && !importer.isReadable && importer.alphaIsTransparency, "Unnecessary mip/read copy or missing transparency");
                    Check(importer.GetPlatformTextureSettings("Standalone").format == TextureImporterFormat.BC7, "HQ desktop compression absent");
                }
            }
            config = UnityEngine.Object.Instantiate(original);
            config.hideFlags = HideFlags.HideAndDontSave;
            cache.SetValue(null, config);
        }
        catch (Exception e) { failure = e.ToString(); }
        if (failure == null)
        {
            for (int side = 0; side < 2; side++)
            {
                bool opponent = side == 1;
                if (opponent)
                {
                    config.speed = 1.3f;
                    config.widthRatio = .86f;
                    config.heightScale = 1.12f;
                    config.centerOffset = new Vector2(12, 58);
                    config.opponentTint = new Color(.85f, .93f, 1f, 1f);
                    config.opacity = .94f;
                }
                bool done = false, captured = false, clear = false, saw = false;
                float begin = Time.realtimeSinceStartup;
                turn.StartCoroutine(Run(Logo(turn, opponent), () => done = true));
                float until = begin + 8;
                while (!done && Time.realtimeSinceStartup < until)
                {
                    var fx = UnityEngine.Object.FindFirstObjectByType<TurnSignalPresentation>();
                    if (fx != null)
                    {
                        saw = true;
                        try
                        {
                            Check(fx.IsOpponent == opponent, "Wrong side's AE frames");
                            Check(Time.timeScale == 0 && PresentationSequenceLock.IsLocked && !turn.CanDragPlayerHand, "Input/game clock not locked");
                            foreach (var system in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                                Check(!system.enabled, "Event system accepted input during turn signal");
                            Check((turn.playerTurnLogo == null || !turn.playerTurnLogo.activeSelf) && (turn.enemyTurnLogo == null || !turn.enemyTurnLogo.activeSelf), "Legacy logo duplicated");
                            Check(!fx.Artwork.raycastTarget && fx.Overlay.overrideSorting, "Overlay configured incorrectly");
                            Check(fx.Artwork.texture == config.Frames(opponent)[fx.FrameIndex], "Frame clock differs from AE");
                            if (fx.Elapsed > .96f) { Check(!fx.Artwork.enabled, "Afterlight still visible"); clear = true; }
                            if (opponent && fx.Elapsed > .2f)
                            {
                                Check(Vector2.Distance(fx.Artwork.rectTransform.anchoredPosition, config.centerOffset) < .01f, "Edited position ignored");
                                Check(Mathf.Abs(fx.Artwork.color.a - config.opacity) < .001f, "Edited opacity ignored");
                            }
                        }
                        catch (Exception e) { failure = e.ToString(); break; }
                        if (!captured && fx.Elapsed > .45f && fx.Elapsed < .8f)
                        {
                            yield return new WaitForEndOfFrame();
                            Capture(opponent ? "opponent-adjusted" : "your-turn");
                            captured = true;
                        }
                    }
                    yield return null;
                }
                if (failure != null) break;
                try
                {
                    Check(done && saw && captured && clear, "Missing cue, showcase or clear ending");
                    Check(!PresentationSequenceLock.IsLocked && !TurnSignalPresentation.IsPlaying && Mathf.Approximately(Time.timeScale, beforeScale), "Input/clock/overlay leaked");
                    float took = Time.realtimeSinceStartup - begin;
                    Check(took >= 1.2f / config.speed - .06f && took < 1.2f / config.speed + 1, "Speed ignored: " + took);
                }
                catch (Exception e) { failure = e.ToString(); break; }
            }
        }
        if (failure == null)
        {
            config.speed = 1;
            bool done = false;
            turn.StartCoroutine(Run(Logo(turn, false), () => done = true));
            yield return new WaitForSecondsRealtime(.18f);
            var fx = UnityEngine.Object.FindFirstObjectByType<TurnSignalPresentation>();
            try { Check(fx != null && PresentationSequenceLock.IsLocked, "Interrupt probe did not start"); }
            catch (Exception e) { failure = e.ToString(); }
            if (fx != null) fx.gameObject.SetActive(false);
            yield return null; yield return null;
            try { Check(done && !TurnSignalPresentation.IsPlaying && !PresentationSequenceLock.IsLocked && Mathf.Approximately(Time.timeScale, beforeScale), "Interrupted cue retained input/clock lock"); }
            catch (Exception e) { failure = e.ToString(); }
        }
        if (failure == null)
        {
            config.enabledEffect = false;
            bool done = false;
            turn.StartCoroutine(Run(Logo(turn, false), () => done = true));
            yield return null;
            try { Check(done && !TurnSignalPresentation.IsPlaying && !PresentationSequenceLock.IsLocked, "Disabled effect did not skip cleanly"); }
            catch (Exception e) { failure = e.ToString(); }
            config.enabledEffect = true;
        }
        if (failure == null)
        {
            // First player turn used to bypass ShowTurnLogo; verify the real entry point.
            turn.gameStartLogoAnimator = null;
            turn.StartFirstTurn(true);
            float until = Time.realtimeSinceStartup + 5;
            bool saw = false;
            while (Time.realtimeSinceStartup < until)
            {
                var fx = UnityEngine.Object.FindFirstObjectByType<TurnSignalPresentation>();
                if (fx != null && !fx.IsOpponent) saw = true;
                if (saw && !TurnSignalPresentation.IsPlaying) break;
                yield return null;
            }
            try { Check(saw && !TurnSignalPresentation.IsPlaying && !PresentationSequenceLock.IsLocked, "First player turn missing/stuck"); }
            catch (Exception e) { failure = e.ToString(); }
        }
        cache.SetValue(null, original);
        if (config != null) UnityEngine.Object.Destroy(config);
        Finish(failure == null
            ? "PASS: Native 60fps AE turn signals, full-resolution transparent cropped frames with HQ compression; both sides, updated size/position/tint/speed/opacity, first-player-turn routing, input/game-clock locks, no lingering light, disabled effect, and interrupted cleanup verified. Original settings retained. No scene saved."
            : "FAIL: " + failure);
    }
}
