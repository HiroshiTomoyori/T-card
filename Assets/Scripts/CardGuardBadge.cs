using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent, DefaultExecutionOrder(520)]
public sealed class CardGuardBadge : MonoBehaviour
{
    const string ResourcePrefix = "TCardField/GuardBadge-";
    static Texture2D shieldTexture, rimTexture, ambientTexture;
    static Shader rippleShader;
    static TurnManager turn;

    CardController card;
    CardDrag drag;
    Visual visual;
    int focusFrame = -2;
    float rippleStarted;

    public bool IsVisible => visual != null && visual.Root.gameObject.activeInHierarchy;
    public float BadgeOpacity => IsVisible ? visual.Opacity : 0f;
    public bool IsRippling => IsVisible && visual.IsRippling;
    internal bool SuppressCardOutline => AttackMotionPresentation.IsEnemyAttackPlaying ||
        (turn != null && turn.IncomingSwordVisible);

    void Awake()
    {
        card = GetComponent<CardController>();
        drag = GetComponent<CardDrag>();
    }

    internal RectTransform FocusForDefense()
    {
        if (focusFrame < Time.frameCount - 1) rippleStarted = Time.unscaledTime;
        focusFrame = Time.frameCount;
        return IsVisible ? visual.Root : null;
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
        bool focused = focusFrame == Time.frameCount;
        visual.Apply(settings, source.color.a, focused, Time.unscaledTime - rippleStarted);
    }

    internal Visual CopyToAttackVisual(Image artwork, bool focusBadge)
    {
        if (!IsVisible || !CardActionIcon.IsGuard(card.data)) return null;
        var copy = CreateVisual(artwork);
        if (copy == null) return null;
        var settings = CardActionGlowSettings.Load();
        PlaceBadge(copy.Root, artwork, settings);
        copy.StartTime = focusBadge && focusFrame >= Time.frameCount - 1 ? rippleStarted : Time.unscaledTime;
        copy.Apply(settings, 1f, focusBadge, Time.unscaledTime - copy.StartTime);
        return copy;
    }

    bool IsDisplayZone()
    {
        if (turn == null) turn = FindFirstObjectByType<TurnManager>();
        if (turn == null) return false;
        var parent = transform.parent;
        if (parent == null) return false;
        if (parent == turn.playerBattleArea || parent == turn.enemyBattleArea || parent == turn.handDealer?.handArea)
            return true;

        return IsVisible && drag != null && drag.IsDragging;
    }

    static void PlaceBadge(RectTransform target, Image source, CardActionGlowSettings settings)
    {
        Rect area = source.rectTransform.rect;
        if (source.preserveAspect)
        {
            float aspect = source.overrideSprite.rect.width / source.overrideSprite.rect.height;
            var size = new Vector2(Mathf.Min(area.width, area.height * aspect), Mathf.Min(area.height, area.width / aspect));
            area.position += Vector2.Scale(area.size - size, source.rectTransform.pivot);
            area.size = size;
        }
        target.anchorMin = target.anchorMax = source.rectTransform.pivot;
        target.anchoredPosition = area.min + Vector2.Scale(area.size, settings.guardBadgeInset);
        target.sizeDelta = Vector2.one * (area.width * settings.guardBadgeSize);
    }

    static Visual CreateVisual(Image source)
    {
        if (shieldTexture == null) shieldTexture = Resources.Load<Texture2D>(ResourcePrefix + "Shield");
        if (rimTexture == null) rimTexture = Resources.Load<Texture2D>(ResourcePrefix + "Rim");
        if (ambientTexture == null) ambientTexture = Resources.Load<Texture2D>(ResourcePrefix + "Ambient");
        if (shieldTexture == null || rimTexture == null || ambientTexture == null) return null;
        return new Visual(source.transform);
    }

    internal sealed class Visual
    {
        internal RectTransform Root { get; }
        internal float Opacity => group.alpha;
        internal bool IsRippling => ripple != null && ripple.enabled;
        internal float StartTime;
        readonly CanvasGroup group;
        readonly RawImage ambient, rim, shield;
        RawImage ripple;
        Material rippleMaterial;

        internal Visual(Transform parent)
        {
            Root = NewRect(parent, "Guard badge");
            Root.pivot = Vector2.zero;
            group = Root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            ambient = AddLayer("Blue diffusion", ambientTexture);
            rim = AddLayer("Cyan rim", rimTexture);
            shield = AddLayer("Original shield", shieldTexture);
        }

        internal void Apply(CardActionGlowSettings settings, float artworkAlpha, bool focused, float age)
        {
            group.alpha = artworkAlpha * (focused ? 1f : settings.guardIdleOpacity);
            float cycle = Mathf.Max(.2f, settings.guardBreathPeriod);
            float phase = .5f - .5f * Mathf.Cos(Time.unscaledTime / cycle * Mathf.PI * 2f);
            float breathing = Mathf.Lerp(settings.guardIdleMinimum, 1f, phase);
            ambient.color = new Color(1f, 1f, 1f, .04f + .96f * breathing);
            rim.color = new Color(1f, 1f, 1f, .10f + .90f * breathing);

            if (focused && settings.guardRippleStrength > 0f)
            {
                if (ripple == null) CreateRipple();
                if (ripple == null) return;
                ripple.enabled = true;
                var rect = ripple.rectTransform;
                float half = 1.16f * settings.guardRippleSize;
                rect.anchorMin = Vector2.one * (.5f - half);
                rect.anchorMax = Vector2.one * (.5f + half);
                float normalizedAge = Mathf.Repeat(Mathf.Max(0f, age), Mathf.Max(.2f, settings.guardRipplePeriod))
                    * 1.65f / Mathf.Max(.2f, settings.guardRipplePeriod);
                rippleMaterial.SetColor("_RippleColor", settings.guardRippleColor);
                rippleMaterial.SetVector("_Ripple", new Vector4(normalizedAge, settings.guardRippleStrength, 0f, 0f));
            }
            else if (ripple != null) ripple.enabled = false;
        }

        void CreateRipple()
        {
            if (rippleShader == null) rippleShader = Resources.Load<Shader>("TCardField/GuardBadgeRipple");
            if (rippleShader == null) return;
            ripple = AddLayer("Guard ripple", Texture2D.whiteTexture);
            ripple.transform.SetSiblingIndex(shield.transform.GetSiblingIndex());
            ripple.maskable = false;
            rippleMaterial = new Material(rippleShader) { hideFlags = HideFlags.HideAndDontSave };
            ripple.material = rippleMaterial;
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
    }

    void OnDisable() => Hide();
    void OnDestroy() => visual?.Dispose();
}
