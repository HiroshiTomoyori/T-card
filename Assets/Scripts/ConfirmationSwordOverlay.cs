using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Shared presentation for a sword embedded in a selected card. Neither visual
// participates in the raycasts used for targeting or confirmation.
internal sealed class ConfirmationSwordOverlay
{
    const int Frames = 120;
    const float Fps = 25f;
    const float ExposedBladeHeight = 247f;
    static readonly Vector2 FrameSize = new Vector2(256f, 352f);
    static readonly Vector2 Insertion = new Vector2(84.8f, 65.6f);

    readonly RectTransform layer;
    readonly CanvasGroup visibility;
    readonly List<RawImage> swords = new List<RawImage>(2);
    readonly List<Image> shadows = new List<Image>(2);
    Texture2D[] sheets;
    Texture2D gradient;
    Sprite gradientSprite;

    public ConfirmationSwordOverlay(RectTransform layer)
    {
        this.layer = layer;
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
        visibility = layer.gameObject.AddComponent<CanvasGroup>();
        visibility.interactable = false;
        visibility.blocksRaycasts = false;
    }

    public float Opacity { set => visibility.alpha = value; }

    public bool Show(int index, Rect card, bool fieldCard, float elapsed, AttackSelectionSettings settings, Camera camera, out Rect bounds)
    {
        Rect safe = Screen.safeArea;
        Vector2 point = new Vector2(
            Mathf.Lerp(card.xMin, card.xMax, settings.confirmationSwordPoint.x),
            Mathf.Lerp(card.yMin, card.yMax, settings.confirmationSwordPoint.y));
        float swordScale = settings.confirmationSwordScale;
        if (fieldCard) swordScale *= settings.confirmationFieldSwordScale;
        float scale = card.height * swordScale / ExposedBladeHeight;
        scale = Mathf.Min(scale, (point.x - safe.xMin) / Insertion.x);
        scale = Mathf.Min(scale, (safe.xMax - point.x) / (FrameSize.x - Insertion.x));
        scale = Mathf.Min(scale, (point.y - safe.yMin) / Insertion.y);
        scale = Mathf.Min(scale, (safe.yMax - point.y) / (FrameSize.y - Insertion.y));
        bounds = new Rect(point - Insertion * scale, FrameSize * scale);
        if (scale <= 0f) return false;

        if (sheets == null)
        {
            sheets = new Texture2D[3];
            for (int i = 0; i < sheets.Length; i++)
                sheets[i] = Resources.Load<Texture2D>("TCardSwordConfirmation/Sword" + i);
        }

        int frame = Mathf.Min(Frames - 1, Mathf.FloorToInt(Mathf.Repeat(
            elapsed * Mathf.Max(.1f, settings.confirmationSwordSpeed), Frames / Fps) * Fps));
        var sheet = sheets[frame / 40];
        if (sheet == null) return false;

        if (index == swords.Count)
        {
            var shadow = MakeGraphic<Image>("Sword gradient shadow");
            shadow.rectTransform.SetAsFirstSibling();
            shadows.Add(shadow);
            swords.Add(MakeGraphic<RawImage>("Embedded sword"));
        }

        var sword = swords[index];
        int tile = frame % 40;
        sword.texture = sheet;
        sword.uvRect = new Rect((tile % 8) / 8f, (4 - tile / 8) / 5f, 1f / 8f, 1f / 5f);
        Place(sword.rectTransform, bounds, camera);
        sword.gameObject.SetActive(true);

        var backdrop = shadows[index];
        float strength = Mathf.Clamp01(settings.confirmationSwordShadowStrength);
        if (strength > 0f)
        {
            if (gradientSprite == null) CreateGradient();
            backdrop.sprite = gradientSprite;
            backdrop.color = new Color(.015f, .02f, .035f, strength);
            Place(backdrop.rectTransform, bounds, camera);
        }
        backdrop.gameObject.SetActive(strength > 0f);
        return true;
    }

    T MakeGraphic<T>(string name) where T : MaskableGraphic
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = layer.gameObject.layer;
        obj.transform.SetParent(layer, false);
        var rect = (RectTransform)obj.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        var graphic = obj.AddComponent<T>();
        graphic.raycastTarget = false;
        graphic.maskable = false;
        return graphic;
    }

    void Place(RectTransform rect, Rect screen, Camera camera)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen.min, camera, out var min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen.max, camera, out var max);
        rect.localPosition = (min + max) * .5f;
        rect.sizeDelta = max - min;
    }

    void CreateGradient()
    {
        const int width = 128, height = 176;
        var pixels = new Color32[width * height];
        // Use the AE shadow's center and tilt in the cropped frame. Concentrate
        // the added darkness toward the center and keep both ends transparent.
        var center = new Vector2(129.6f, 182.4f);
        var axis = new Vector2(.35f, .937f);
        var across = new Vector2(axis.y, -axis.x);
        for (int y = 1; y < height - 1; y++)
        for (int x = 1; x < width - 1; x++)
        {
            var offset = new Vector2((x + .5f) * 2f, (y + .5f) * 2f) - center;
            float along = Vector2.Dot(offset, axis) / 155f;
            float side = Vector2.Dot(offset, across) / 87f;
            float radius = Mathf.Sqrt(along * along + side * side);
            float fade = 1f - Mathf.SmoothStep(0f, 1f, radius);
            pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * fade * fade));
        }
        gradient = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
        {
            name = "Sword gradient shadow",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        gradient.SetPixels32(pixels);
        gradient.Apply(false, true);
        gradientSprite = Sprite.Create(gradient, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
        gradientSprite.hideFlags = HideFlags.HideAndDontSave;
    }

    public void HideFrom(int firstUnused)
    {
        for (int i = firstUnused; i < swords.Count; i++)
        {
            swords[i].gameObject.SetActive(false);
            shadows[i].gameObject.SetActive(false);
        }
    }

    public void Dispose()
    {
        if (gradientSprite != null) Object.Destroy(gradientSprite);
        if (gradient != null) Object.Destroy(gradient);
    }
}
