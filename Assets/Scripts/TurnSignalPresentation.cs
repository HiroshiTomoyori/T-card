using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Plays the approved AE frames without rebuilding the typography or warp in Unity.
public sealed class TurnSignalPresentation : MonoBehaviour
{
    public static bool IsPlaying => active > 0;
    public float Elapsed { get; private set; }
    public int FrameIndex { get; private set; }
    public bool IsOpponent { get; private set; }
    public RawImage Artwork { get; private set; }
    public Canvas Overlay { get; private set; }
    public Image Lowlight { get; private set; }
    float visualEndTime;
    static int active;
    TurnManager owner;
    Canvas canvas;
    RectTransform root;
    TurnSignalSettings settings;
    PresentationSequenceLock.Lease inputLock;

    void OnEnable() { active++; }
    void OnDisable()
    {
        active = Mathf.Max(0, active - 1);
        inputLock?.Dispose();
        inputLock = null;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { active = 0; }

    static Canvas FindCanvas(TurnManager turn)
    {
        Transform[] candidates = {
            turn.playerTurnLogo != null ? turn.playerTurnLogo.transform : null,
            turn.enemyTurnLogo != null ? turn.enemyTurnLogo.transform : null,
            turn.handDealer != null ? turn.handDealer.handArea : null,
            turn.playerBattleArea, turn.enemyBattleArea, turn.transform
        };
        foreach (var candidate in candidates)
        {
            if (candidate == null) continue;
            var found = candidate.GetComponentInParent<Canvas>(true);
            if (found != null && found.rootCanvas.isActiveAndEnabled) return found.rootCanvas;
        }
        return null;
    }
    public static bool CanPlay(TurnManager turn, bool opponent)
    {
        var config = TurnSignalSettings.Load();
        return turn != null && config != null && config.enabledEffect && config.HasFrames(opponent) && FindCanvas(turn) != null;
    }
    void LateUpdate()
    {
        if (owner == null || !owner.isActiveAndEnabled || owner.IsBattleEnded)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        ApplyLayout();
    }
    void ApplyLayout()
    {
        if (canvas == null || settings == null || Artwork == null) return;
        float width = ((RectTransform)canvas.transform).rect.width * Mathf.Max(.1f, settings.widthRatio);
        var rect = Artwork.rectTransform;
        rect.anchoredPosition = settings.centerOffset;
        rect.sizeDelta = new Vector2(width, width * settings.frameSize.y / Mathf.Max(1f, settings.frameSize.x) * Mathf.Max(.05f, settings.heightScale));
        Color tint = IsOpponent ? settings.opponentTint : settings.playerTint;
        tint.a *= settings.opacity;
        Artwork.color = tint;
        if (Lowlight != null)
        {
            float fadeIn = settings.lowlightFadeIn <= 0 ? 1f : Mathf.SmoothStep(0,1,Mathf.Clamp01(Elapsed / settings.lowlightFadeIn));
            float fadeOut = settings.lowlightFadeOut <= 0 ? (Elapsed < visualEndTime ? 1f : 0f)
                : Mathf.SmoothStep(0,1,Mathf.Clamp01((visualEndTime - Elapsed) / settings.lowlightFadeOut));
            Color dim = settings.lowlightColor;
            dim.a *= settings.lowlightOpacity * settings.opacity * fadeIn * fadeOut;
            Lowlight.color = dim;
        }
    }
    public static IEnumerator Play(TurnManager turn, bool opponent, AudioClip sound)
    {
        if (!CanPlay(turn, opponent)) yield break;
        var config = TurnSignalSettings.Load();
        var canvas = FindCanvas(turn);
        if (turn.playerTurnLogo != null) turn.playerTurnLogo.SetActive(false);
        if (turn.enemyTurnLogo != null) turn.enemyTurnLogo.SetActive(false);
        var go = new GameObject(opponent ? "Opponent Turn (AE)" : "Your Turn (AE)", typeof(RectTransform));
        go.layer = canvas.gameObject.layer;
        var root = (RectTransform)go.transform;
        root.SetParent(canvas.transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        var fx = go.AddComponent<TurnSignalPresentation>();
        try
        {
            fx.owner = turn;
            fx.canvas = canvas;
            fx.root = root;
            fx.settings = config;
            fx.IsOpponent = opponent;
            var layer = go.AddComponent<Canvas>();
            layer.overrideSorting = true;
            layer.sortingLayerID = canvas.sortingLayerID;
            int order = canvas.sortingOrder;
            foreach (var child in canvas.GetComponentsInChildren<Canvas>(true))
                if (child != layer && child.sortingLayerID == layer.sortingLayerID) order = Mathf.Max(order, child.sortingOrder);
            layer.sortingOrder = Mathf.Min(32760, order + Mathf.Max(1, config.sortingOffset));
            fx.Overlay = layer;
            var dimObject = new GameObject("Background lowlight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dimObject.layer = go.layer;
            dimObject.transform.SetParent(root, false);
            var dimRect = (RectTransform)dimObject.transform;
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
            fx.Lowlight = dimObject.GetComponent<Image>();
            fx.Lowlight.color = Color.clear;
            fx.Lowlight.raycastTarget = false;
            fx.Lowlight.maskable = false;
            var image = new GameObject("AE artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            image.layer = go.layer;
            image.transform.SetParent(root, false);
            var imageRect = (RectTransform)image.transform;
            imageRect.anchorMin = imageRect.anchorMax = imageRect.pivot = new Vector2(.5f, .5f);
            fx.Artwork = image.GetComponent<RawImage>();
            fx.Artwork.raycastTarget = false;
            fx.Artwork.maskable = false;
            fx.inputLock = PresentationSequenceLock.Acquire(fx);
            fx.inputLock.PauseGame();
            if (turn.seSource != null && sound != null) turn.seSource.PlayOneShot(sound);
            var frames = config.Frames(opponent);
            float duration = frames.Length / config.frameRate;
            // Fade the background with the final visible artwork, not the blank tail.
            for (int i = frames.Length - 1; i >= 0; i--)
                if (frames[i] != null) { fx.visualEndTime = (i + 1f) / config.frameRate; break; }
            while (fx != null && fx.isActiveAndEnabled && turn != null && !turn.IsBattleEnded && fx.Elapsed < duration)
            {
                fx.FrameIndex = Mathf.Clamp(Mathf.FloorToInt(fx.Elapsed * config.frameRate), 0, frames.Length - 1);
                var frame = frames[fx.FrameIndex];
                fx.Artwork.enabled = frame != null;
                fx.Artwork.texture = frame;
                fx.ApplyLayout();
                yield return null;
                if (fx != null) fx.Elapsed += Time.unscaledDeltaTime * Mathf.Max(.05f, config.speed);
            }
        }
        finally
        {
            if (fx != null)
            {
                fx.inputLock?.Dispose();
                fx.inputLock = null;
            }
            if (root != null)
            {
                root.gameObject.SetActive(false);
                Destroy(root.gameObject);
            }
        }
    }
}
