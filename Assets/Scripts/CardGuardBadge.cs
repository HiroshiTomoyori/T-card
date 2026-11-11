using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent, DefaultExecutionOrder(520)]
public sealed class CardGuardBadge : MonoBehaviour
{
    const string ResourcePrefix = "TCardField/GuardBadge-";
    static Texture2D shieldTexture, silhouetteTexture;
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
        PlaceBadge(visual.Root, source, settings);
        bool focused = focusFrame == Time.frameCount && !card.isTapped && !AttackMotionPresentation.IsPlaying;
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
        PlaceBadge(copy.Root, artwork, settings);
        copy.Apply(settings, BadgeOpacity, 0f, 0f);
    }

    bool IsDisplayZone()
    {
        if (turn == null) turn = FindFirstObjectByType<TurnManager>();
        if (turn == null) return false;
        var parent = transform.parent;
        if (parent == null) return false;
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

    static void PlaceBadge(RectTransform target, Image source, CardActionGlowSettings settings)
    {
        Rect area = AttackMotionPresentation.ArtworkRect(source);
        target.anchorMin = target.anchorMax = source.rectTransform.pivot;
        target.anchoredPosition = area.min + Vector2.Scale(area.size, settings.guardBadgeInset);
        target.sizeDelta = Vector2.one * (area.width * settings.guardBadgeSize);
    }

    static Visual CreateVisual(Image source)
    {
        if (shieldTexture == null) shieldTexture = Resources.Load<Texture2D>(ResourcePrefix + "Shield");
        if (silhouetteTexture == null) silhouetteTexture = Resources.Load<Texture2D>(ResourcePrefix + "Rim");
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
        RawImage ripple, outline;
        Material rippleMaterial, outlineMaterial;

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

        internal void Apply(CardActionGlowSettings settings, float artworkAlpha, float focus, float age)
        {
            group.alpha = artworkAlpha;
            if (focus > 0f && silhouetteTexture != null)
            {
                if (outline == null) outline = CreateEffect("Shield contour glow", silhouetteTexture, out outlineMaterial);
                if (outline != null)
                {
                    outline.enabled = true;
                    float phase = .5f - .5f * Mathf.Cos(Time.unscaledTime / Mathf.Max(.2f, settings.guardBreathPeriod) * Mathf.PI * 2f);
                    float pulse = Mathf.Lerp(settings.guardIdleMinimum, 1f, phase);
                    outlineMaterial.SetColor("_RippleColor", settings.guardRippleColor);
                    outlineMaterial.SetVector("_Ripple", new Vector4(0f, (.55f + .45f * pulse) * focus, 1f, 0f));
                }
            }
            else if (outline != null) outline.enabled = false;

            if (focus > 0f && settings.guardRippleStrength > 0f)
            {
                if (ripple == null)
                {
                    ripple = CreateEffect("Guard ripple", Texture2D.whiteTexture, out rippleMaterial);
                    if (ripple != null) ripple.transform.SetAsFirstSibling();
                }
                if (ripple == null) return;
                ripple.enabled = true;
                var rect = ripple.rectTransform;
                float half = 1.16f * settings.guardRippleSize;
                rect.anchorMin = Vector2.one * (.5f - half);
                rect.anchorMax = Vector2.one * (.5f + half);
                float normalizedAge = Mathf.Repeat(Mathf.Max(0f, age), Mathf.Max(.2f, settings.guardRipplePeriod))
                    * 1.65f / Mathf.Max(.2f, settings.guardRipplePeriod);
                rippleMaterial.SetColor("_RippleColor", settings.guardRippleColor);
                rippleMaterial.SetVector("_Ripple", new Vector4(normalizedAge, settings.guardRippleStrength * focus, 0f, 0f));
            }
            else if (ripple != null) ripple.enabled = false;
        }

        RawImage CreateEffect(string name, Texture2D texture, out Material material)
        {
            material = null;
            if (rippleShader == null) rippleShader = Resources.Load<Shader>("TCardField/GuardBadgeRipple");
            if (rippleShader == null) return null;
            var effect = AddLayer(name, texture);
            effect.maskable = false;
            material = new Material(rippleShader) { hideFlags = HideFlags.HideAndDontSave };
            effect.material = material;
            return effect;
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
