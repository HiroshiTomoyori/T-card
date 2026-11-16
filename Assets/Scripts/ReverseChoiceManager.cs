using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReverseChoiceManager : MonoBehaviour
{
    public static ReverseChoiceManager I;
    const string ResourcesPath = "TCardReverseCheck/";
    const int FrameCount = 78, FramesPerPage = 40, Columns = 5, Rows = 8;

    [SerializeField] GameObject choicePanel;
    [SerializeField] GameObject orderPanel;
    [Header("Card Check")]
    [SerializeField] Vector2 windowSize = new Vector2(575, 456);
    [SerializeField] Vector2 windowOffset;
    [SerializeField, Min(0)] float windowFade = 0f;
    [SerializeField, Min(0)] float reverseCueDuration = .815f;
    [SerializeField, Min(0)] float glowSpeed = 1f;
    [SerializeField] Color glowTint = Color.white;
    [SerializeField] Color cueTint = new Color(1f, .86f, .55f);

    readonly List<int> selected = new List<int>(2);
    readonly Texture2D[] glowPages = new Texture2D[2];
    readonly Texture2D[] orderArt = new Texture2D[2];
    Texture2D ringArt;
    RectTransform root, panel;
    CanvasGroup panelGroup;
    Button play, take;
    Text title, playLabel;
    CardSlot[] slots;
    bool committed;

    public bool IsChoosing { get; private set; }
    public int ChoiceRevision { get; private set; }
    public Transform TimerAnchor => panel;
    public bool IsPlayingCue { get; private set; }

    sealed class CardSlot
    {
        public Button Button;
        public RectTransform Face;
        public Image Art;
        public RawImage Glow, Order;
        public Text Tag;
        public bool IsReverse;
    }

    void Awake()
    {
        I = this;
        if (choicePanel != null) choicePanel.SetActive(false);
        if (orderPanel != null) orderPanel.SetActive(false);
    }

    void OnDisable()
    {
        IsChoosing = IsPlayingCue = false;
        committed = true;
        selected.Clear();
        if (root != null) root.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (I == this) I = null;
        if (root != null) Destroy(root.gameObject);
    }

    public void ResolveInactivity()
    {
        if (IsChoosing) Commit(false);
    }

    internal IEnumerator Check(IReadOnlyList<CardData> cards, Canvas source, List<int> result)
    {
        result.Clear();
        if (!EnsureRoot(source) || !isActiveAndEnabled || cards.Count == 0) yield break;

        float fit = Fit();
        panelGroup.alpha = 0;
        panelGroup.interactable = false;
        selected.Clear();
        committed = false;
        foreach (var slot in slots) slot.Button.gameObject.SetActive(false);
        int count = Mathf.Min(slots.Length, cards.Count);
        int reverseCount = 0;
        int lastReverse = -1;
        for (int i = 0; i < count; i++)
        {
            var slot = slots[i];
            slot.IsReverse = HasReverse(cards[i]);
            if (slot.IsReverse)
            {
                reverseCount++;
                lastReverse = i;
            }
            slot.Art.sprite = cards[i].artwork;
            slot.Glow.enabled = slot.IsReverse;
            if (slot.IsReverse) SampleGlow(slot.Glow, 0f);
            slot.Tag.gameObject.SetActive(slot.IsReverse);
            slot.Tag.color = cueTint;
            slot.Button.interactable = slot.IsReverse;
            slot.Button.gameObject.SetActive(true);
        }
        if (reverseCount == 1)
        {
            // Offer the only Reverse directly; Play Card still requires confirmation.
            selected.Add(lastReverse);
            slots[lastReverse].Button.interactable = false;
        }
        play.gameObject.SetActive(reverseCount > 0);
        Layout(fit, count, reverseCount > 0);
        RefreshSelection();

        root.gameObject.SetActive(true);
        panel.gameObject.SetActive(true);
        float started = Time.unscaledTime;
        try
        {
            while (!committed && isActiveAndEnabled)
            {
                float age = Time.unscaledTime - started;
                panelGroup.alpha = windowFade <= 0f ? 1f : Ease(age / windowFade);
                for (int i = 0; i < count; i++)
                {
                    var slot = slots[i];
                    if (slot.IsReverse) SampleGlow(slot.Glow, age);
                }
                if (!IsChoosing && age >= windowFade)
                {
                    IsChoosing = true;
                    ChoiceRevision++;
                    panelGroup.interactable = true;
                    if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(take.gameObject);
                }
                yield return null;
            }
            result.AddRange(selected);
        }
        finally
        {
            IsChoosing = false;
            root.gameObject.SetActive(false);
        }
    }

    static bool HasReverse(CardData data) =>
        data != null && data.effectTypes != null && Array.IndexOf(data.effectTypes, EffectType.ShieldTrigger) >= 0;

    void Toggle(int index)
    {
        if (!IsChoosing || committed || !slots[index].Button.interactable) return;
        if (!selected.Remove(index) && selected.Count < 2) selected.Add(index);
        RefreshSelection();
    }

    void Commit(bool use)
    {
        if (!IsChoosing || committed || (use && selected.Count == 0)) return;
        if (!use) selected.Clear();
        committed = true;
        IsChoosing = false;
        panelGroup.interactable = false;
    }

    void RefreshSelection()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].Button.gameObject.activeSelf) continue;
            int index = selected.IndexOf(i);
            slots[i].Order.enabled = index >= 0 && slots[i].Button.interactable;
            if (slots[i].Order.enabled) slots[i].Order.texture = orderArt[index];
        }
        play.interactable = selected.Count > 0;
        playLabel.text = selected.Count == 2 ? "Play 2 Cards" :
            selected.Count == 1 ? "Play Card" : "Play";
    }

    internal IEnumerator PlayCue(CardData card, int index, int count, Canvas source)
    {
        if (reverseCueDuration <= 0 || card.artwork == null || !EnsureRoot(source)) yield break;
        float fit = Fit();
        Vector2 size = new Vector2(178, 267) * fit;
        var content = NewRect(root, "Reverse Play");
        var group = content.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        var outer = Graphic<RawImage>(content, "Platinum Ring");
        var inner = Graphic<RawImage>(content, "Inner Ring");
        outer.texture = inner.texture = ringArt;
        var art = Graphic<Image>(content, "Reverse Artwork");
        art.sprite = card.artwork;
        art.rectTransform.sizeDelta = size;
        var halo = Graphic<RawImage>(content, "Platinum Orbit");
        halo.rectTransform.sizeDelta = Vector2.Scale(size, new Vector2(512f / 300f, 640f / 450f));
        var sweep = Graphic<TurnDrawHighlightGraphic>(content, "Platinum Sheen");
        sweep.surface = true;
        sweep.rectTransform.sizeDelta = size;
        Label(content, "Reverse Order", $"{index + 1} / {count}", 164 * fit, new Vector2(170, 24) * fit, 14 * fit).color = cueTint;
        Label(content, "Reverse Title", "Reverse Play", -164 * fit, new Vector2(260, 35) * fit, 27 * fit);

        root.gameObject.SetActive(true);
        panel.gameObject.SetActive(false);
        IsPlayingCue = true;
        var input = PresentationSequenceLock.Acquire(this);
        float start = Time.unscaledTime;
        try
        {
            while (isActiveAndEnabled && Time.unscaledTime - start < reverseCueDuration)
            {
                float age = Time.unscaledTime - start;
                float t = Mathf.Clamp01(age / reverseCueDuration);
                float fade = Ease(t / .21f) * (1 - Ease((t - .73f) / .27f));
                group.alpha = fade;
                content.anchoredPosition = Vector2.up * Mathf.Lerp(-19, 12, Ease(t)) * fit;
                content.localScale = Vector3.one * Mathf.Lerp(.91f, 1.045f, Ease(t));
                SetRing(outer, t, 320 * fit, .76f);
                SetRing(inner, (t - .09f) / .91f, 258 * fit, .45f);
                SampleGlow(halo, age);
                sweep.Configure(.9f * fade, 0, Mathf.Clamp01((t - .10f) / .66f), cueTint);
                yield return null;
            }
        }
        finally
        {
            input.Dispose();
            IsPlayingCue = false;
            content.gameObject.SetActive(false);
            Destroy(content.gameObject);
            root.gameObject.SetActive(false);
        }
    }

    void SetRing(RawImage graphic, float t, float diameter, float strength)
    {
        graphic.rectTransform.sizeDelta = Vector2.one * diameter * Mathf.Lerp(.57f, 1.3f, Ease(t));
        Color tint = cueTint;
        tint.a *= strength * Ease(t / .22f) * (1 - Ease((t - .23f) / .77f));
        graphic.color = tint;
    }

    static float Ease(float value) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(value));

    void SampleGlow(RawImage image, float age)
    {
        int frame = (int)Mathf.Repeat(age * 30 * glowSpeed, FrameCount);
        int cell = frame % FramesPerPage;
        image.texture = glowPages[frame / FramesPerPage];
        image.uvRect = new Rect((cell % Columns) / (float)Columns, (Rows - 1 - cell / Columns) / (float)Rows,
            1f / Columns, 1f / Rows);
        image.color = glowTint;
    }

    bool EnsureRoot(Canvas source)
    {
        if (root != null) return true;
        if (source == null) return false;
        source = source.rootCanvas;
        root = NewRect(source.transform, "Reverse Card Check Overlay");
        Stretch(root);
        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingLayerID = source.sortingLayerID;
        canvas.sortingOrder = source.sortingOrder + 146;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var backdrop = Graphic<Image>(root, "Card Check Lowlight");
        backdrop.raycastTarget = true;
        backdrop.color = new Color(.025f, .04f, .065f, .62f);
        Stretch(backdrop.rectTransform);

        panel = NewRect(root, "Card Check");
        ConfirmationWindow.AddSurface(panel);
        panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
        title = Label(panel, "Card Check Title", "Card Check", 0, Vector2.one, 35);
        take = ConfirmationWindow.CreateChoice(panel, "Add to Hand", "Add to Hand", false, () => Commit(false));
        play = ConfirmationWindow.CreateChoice(panel, "Play Selected", "Play", true, () => Commit(true));
        playLabel = play.GetComponentInChildren<Text>(true);

        slots = new CardSlot[2];
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            var rect = NewRect(panel, "Revealed Wall " + (i + 1));
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Toggle(index));
            var face = NewRect(rect, "Flip");
            var art = Graphic<Image>(face, "Card Artwork");
            Stretch(art.rectTransform);
            var glow = Graphic<RawImage>(face, "AE Platinum Orbit");
            var stamp = Graphic<RawImage>(face, "Reverse Order Stamp");
            Stretch(stamp.rectTransform);
            var tag = Label(rect, "Card Type", "Reverse", 0, Vector2.one, 17);
            slots[i] = new CardSlot { Button = button, Face = face, Art = art, Glow = glow, Order = stamp, Tag = tag };
            glowPages[i] = Resources.Load<Texture2D>(ResourcesPath + "Glow" + i);
            orderArt[i] = Resources.Load<Texture2D>(ResourcesPath + "Order" + (i + 1));
        }
        ringArt = Resources.Load<Texture2D>(ResourcesPath + "Ring");
        root.gameObject.SetActive(false);
        return true;
    }

    float Fit()
    {
        var bounds = ((RectTransform)root.parent).rect;
        return Mathf.Max(.01f, Mathf.Min(bounds.width / 720f, bounds.height / 540f,
            (bounds.width - 24f) / Mathf.Max(1, windowSize.x), (bounds.height - 24f) / Mathf.Max(1, windowSize.y)));
    }

    void Layout(float fit, int count, bool canPlay)
    {
        ConfirmationWindow.SetRect(panel, windowOffset * fit, windowSize * fit);
        float layout = Mathf.Min(windowSize.x / 575f, windowSize.y / 456f) * fit;
        PlaceLabel(title, 181, 480, 44, 36, layout);
        for (int i = 0; i < count; i++)
        {
            var slot = slots[i];
            var cardSize = new Vector2(134, 201) * layout;
            ConfirmationWindow.SetRect((RectTransform)slot.Button.transform,
                new Vector2(count == 1 ? 0 : (i == 0 ? -91 : 91), 17) * layout, cardSize);
            slot.Face.sizeDelta = cardSize;
            slot.Glow.rectTransform.sizeDelta = Vector2.Scale(cardSize, new Vector2(512f / 300f, 640f / 450f));
            PlaceLabel(slot.Tag, -119, 134, 26, 17, layout);
        }
        ConfirmationWindow.SetRect((RectTransform)take.transform, new Vector2(canPlay ? -86 : 0, -178) * layout,
            new Vector2(158, 42) * layout);
        ConfirmationWindow.SetRect((RectTransform)play.transform, new Vector2(86, -178) * layout, new Vector2(158, 42) * layout);
    }

    static RectTransform NewRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        return rect;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static T Graphic<T>(Transform parent, string name) where T : Graphic
    {
        var graphic = NewRect(parent, name).gameObject.AddComponent<T>();
        graphic.raycastTarget = false;
        return graphic;
    }

    static Text Label(Transform parent, string name, string value, float y, Vector2 size, float fontSize)
    {
        var label = Graphic<Text>(parent, name);
        label.text = value;
        label.font = ConfirmationWindow.WindowFont;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = ConfirmationTheme.Load().textColor;
        label.resizeTextForBestFit = true;
        PlaceLabel(label, y, size.x, size.y, fontSize, 1);
        return label;
    }

    static void PlaceLabel(Text label, float y, float width, float height, float fontSize, float fit)
    {
        ConfirmationWindow.SetRect(label.rectTransform, Vector2.up * y * fit, new Vector2(width, height) * fit);
        label.resizeTextMaxSize = Mathf.Max(1, Mathf.RoundToInt(fontSize * fit));
        label.resizeTextMinSize = Mathf.Max(1, Mathf.RoundToInt(fontSize * fit * .62f));
    }
}
