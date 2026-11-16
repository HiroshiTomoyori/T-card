using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent, DefaultExecutionOrder(520)]
public sealed class CardGuardBadge : MonoBehaviour
{
    const string ResourcePrefix = "TCardField/GuardBadge-";
    static Texture2D shieldTexture;
    static Shader rippleShader;
    static TurnManager turn;

    CardController card;
    CardDrag drag;
    Visual visual;
    Transform displayZone;
    float appearedAt = -1f;
    int focusFrame = -2;
    float rippleStarted;
    Canvas attackCanvas;
    float focusWeight;

    public bool IsVisible => visual != null && visual.Root.gameObject.activeInHierarchy;
    public float BadgeOpacity => IsVisible ? visual.Opacity : 0f;
    public bool IsRippling => IsVisible && visual.IsRippling;
    internal bool SuppressCardOutline => AttackMotionPresentation.IsPlaying || (turn != null && (turn.IncomingSwordVisible ||
        turn.DefenseSelectionActive || (turn.AttackSelectionActive &&
        transform.parent == turn.enemyBattleArea && !card.isTapped)));

    void Awake()
    {
        card = GetComponent<CardController>();
        drag = GetComponent<CardDrag>();
    }

    internal void FocusForAttack(Canvas canvas, float weight)
    {
        if (weight <= 0f) return;
        if (focusFrame < Time.frameCount - 1) rippleStarted = Time.unscaledTime;
        focusFrame = Time.frameCount;
        attackCanvas = canvas;
        focusWeight = weight;
    }

    void LateUpdate()
    {
        var source = card.artworkImage;
        if (!CardActionIcon.IsGuard(card.data) || source == null || !source.isActiveAndEnabled ||
            source.overrideSprite == null || source.overrideSprite != card.data.artwork || !IsDisplayZone())
        {
            Hide();
            return;
        }

        if (visual == null) visual = CreateVisual(source);
        if (visual == null) return;
        if (visual.Root.parent != source.transform) visual.Root.SetParent(source.transform, false);
        visual.Root.gameObject.SetActive(true);
        var settings = CardActionGlowSettings.Load();
        bool focused = focusFrame == Time.frameCount && !card.isTapped && !AttackMotionPresentation.IsPlaying;
        PlaceBadge(visual.Root, source, settings, focused ? settings.guardBadgeSize : settings.IdleGuardBadgeSize);
        visual.SortAbove(focused ? attackCanvas : null);
        if (appearedAt < 0f && source.canvasRenderer.GetInheritedAlpha() > .01f && source.color.a > .01f)
            appearedAt = Time.unscaledTime;
        float fade = settings.guardBadgeFadeIn <= 0f ? 1f :
            appearedAt < 0f ? 0f : Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - appearedAt) / settings.guardBadgeFadeIn);
        visual.Apply(settings, source.color.a * fade, focused ? focusWeight : 0f, Time.unscaledTime - rippleStarted);
    }

    internal void CopyToAttackVisual(Image artwork)
    {
        if (!IsVisible || !CardActionIcon.IsGuard(card.data)) return;
        var copy = CreateVisual(artwork);
        if (copy == null) return;
        var settings = CardActionGlowSettings.Load();
        PlaceBadge(copy.Root, artwork, settings, settings.IdleGuardBadgeSize);
        copy.SetOpacity(BadgeOpacity);
    }

    bool IsDisplayZone()
    {
        if (turn == null) turn = FindFirstObjectByType<TurnManager>();
        if (turn == null) return false;
        var parent = transform.parent;
        if (parent == null) return false;
        if (HandDealer.IsRedrawSelecting && parent == turn.handDealer?.handArea) return false;
        if (parent == turn.playerBattleArea || parent == turn.enemyBattleArea || parent == turn.handDealer?.handArea)
        {
            if (parent != displayZone)
            {
                displayZone = parent;
                appearedAt = -1f;
            }
            return true;
        }

        return IsVisible && drag != null && drag.IsDragging;
    }

    static void PlaceBadge(RectTransform target, Image source, CardActionGlowSettings settings, float size)
    {
        Rect area = AttackMotionPresentation.ArtworkRect(source);
        target.anchorMin = target.anchorMax = source.rectTransform.pivot;
        target.anchoredPosition = area.min + Vector2.Scale(area.size, settings.guardBadgeInset);
        target.sizeDelta = Vector2.one * (area.width * size);
    }

    static Visual CreateVisual(Image source)
    {
        if (shieldTexture == null) shieldTexture = Resources.Load<Texture2D>(ResourcePrefix + "Shield");
        if (shieldTexture == null) return null;
        return new Visual(source.transform);
    }

    internal sealed class Visual
    {
        internal RectTransform Root { get; }
        internal float Opacity => group.alpha;
        internal bool IsRippling => ripple != null && ripple.enabled;
        readonly CanvasGroup group;
        Canvas canvas;
        RawImage ambient, ripple, outline;
        Material ambientMaterial, rippleMaterial, outlineMaterial;

        internal Visual(Transform parent)
        {
            Root = NewRect(parent, "Guard badge");
            Root.pivot = Vector2.zero;
            group = Root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            AddLayer("Photoshop shield", shieldTexture);
        }

        internal void SortAbove(Canvas overlay)
        {
            if (overlay != null)
            {
                if (canvas == null) canvas = Root.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingLayerID = overlay.sortingLayerID;
                canvas.sortingOrder = overlay.sortingOrder + 1;
            }
            else if (canvas != null) canvas.overrideSorting = false;
        }

        internal void SetOpacity(float alpha) => group.alpha = alpha;

        internal void Apply(CardActionGlowSettings settings, float artworkAlpha, float focus, float age)
        {
            SetOpacity(artworkAlpha);
            float breathPhase = Time.unscaledTime / Mathf.Max(.2f, settings.guardBreathPeriod);
            float breath = .5f - .5f * Mathf.Cos(breathPhase * Mathf.PI * 2f);
            float pulse = Mathf.Lerp(settings.guardIdleMinimum, 1f, breath);
            float wavePhase = Mathf.Repeat(age / Mathf.Max(.2f, settings.guardRipplePeriod), 1f);
            float focusGlow = settings.guardFocusGlowStrength * focus;

            float idleGlow = settings.guardIdleGlowStrength * pulse * (1f - focus);
            float ambientStrength = idleGlow + focusGlow * Mathf.Lerp(.65f, 1f, breath);
            Color ambientColor = Color.Lerp(settings.guardIdleGlowColor, settings.guardRippleColor,
                Mathf.Clamp01(focus));
            if (ambient == null && ambientStrength > 0f)
                ambient = CreateEffect("Breathing shield glow", 1.65f, out ambientMaterial, true);
            UpdateEffect(ambient, ambientMaterial, ambientColor, ambientStrength,
                new Vector4(breath, 0f, settings.guardIdleGlowSpread, 0f));

            if (outline == null && focusGlow > 0f)
                outline = CreateEffect("Shield contour glow", 1.65f, out outlineMaterial);
            UpdateEffect(outline, outlineMaterial, settings.guardRippleColor, focusGlow,
                new Vector4(wavePhase, 1f, 1f, pulse));

            float rippleStrength = settings.guardRippleStrength * focus;
            float sparkleStrength = settings.guardSparkleStrength * focus;
            if (ripple == null && (rippleStrength > 0f || sparkleStrength > 0f))
                ripple = CreateEffect("Guard ripple", 1.65f, out rippleMaterial, true);
            if (ripple != null)
                SetEffectExtent(ripple.rectTransform, 1.65f * settings.guardRippleSize / 1.4f);
            UpdateEffect(ripple, rippleMaterial, settings.guardRippleColor, rippleStrength,
                new Vector4(wavePhase, 2f, 1f, sparkleStrength));
        }

        static void UpdateEffect(RawImage effect, Material material, Color color, float strength, Vector4 parameters)
        {
            if (effect == null) return;
            effect.enabled = strength > 0f || (parameters.y == 2f && parameters.w > 0f);
            if (!effect.enabled) return;
            material.SetColor("_RippleColor", color);
            material.SetFloat("_Strength", strength);
            material.SetVector("_Ripple", parameters);
        }

        RawImage CreateEffect(string name, float extent, out Material material, bool behindShield = false)
        {
            material = null;
            if (rippleShader == null) rippleShader = Resources.Load<Shader>("TCardField/GuardBadgeRipple");
            if (rippleShader == null) return null;
            var effect = AddLayer(name, Texture2D.whiteTexture);
            if (behindShield) effect.transform.SetAsFirstSibling();
            SetEffectExtent(effect.rectTransform, extent);
            effect.maskable = false;
            material = new Material(rippleShader) { hideFlags = HideFlags.HideAndDontSave };
            effect.material = material;
            return effect;
        }

        static void SetEffectExtent(RectTransform rect, float extent)
        {
            rect.anchorMin = Vector2.one * (.5f - extent);
            rect.anchorMax = Vector2.one * (.5f + extent);
        }

        RawImage AddLayer(string name, Texture2D texture)
        {
            var rect = NewRect(Root, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        internal void Dispose()
        {
            if (ambientMaterial != null) Destroy(ambientMaterial);
            if (rippleMaterial != null) Destroy(rippleMaterial);
            if (outlineMaterial != null) Destroy(outlineMaterial);
            if (Root != null) Destroy(Root.gameObject);
        }
    }

    static RectTransform NewRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    void Hide()
    {
        if (visual != null) visual.Root.gameObject.SetActive(false);
        displayZone = null;
        appearedAt = -1f;
    }

    void OnDisable() => Hide();
    void OnDestroy() => visual?.Dispose();
}
