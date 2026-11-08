using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

internal sealed class ConfirmationWindow
{
    internal RectTransform Rect { get; }
    internal Button Accept { get; }
    internal Button Back { get; }
    internal Text Message { get; }
    readonly Text heading;
    readonly List<float> candidateX = new List<float>(16);
    readonly List<float> candidateY = new List<float>(16);
    static Font font;

    internal ConfirmationWindow(Transform parent, string name, string caption,
        string backName, string backText, UnityAction back, string acceptName, string acceptText, UnityAction accept)
    {
        Rect = CreateRect(parent, name);
        Rect.gameObject.AddComponent<ConfirmationGlass>();
        heading = CreateLabel(Rect, "Decision Title", caption);
        Message = CreateLabel(Rect, "Decision Message", "");
        Back = CreateChoice(Rect, backName, backText, false, back);
        Accept = CreateChoice(Rect, acceptName, acceptText, true, accept);
    }

    internal void Layout()
    {
        var theme = ConfirmationTheme.Load();
        Vector2 size = Rect.rect.size;
        float units = Mathf.Max(.01f, Mathf.Min(size.x / 366f, size.y / 208f));
        Vector2 buttonSize = theme.buttonSize * units;
        buttonSize.x = Mathf.Clamp(buttonSize.x, size.x * .15f, size.x * .44f);
        buttonSize.y = Mathf.Clamp(buttonSize.y, size.y * .13f, size.y * .30f);
        float gap = size.x * .026f;
        float bottom = -size.y * .5f + 16f * units;
        float buttonY = bottom + buttonSize.y * .5f;
        SetRect((RectTransform)Back.transform, new Vector2(-(buttonSize.x + gap) * .5f, buttonY), buttonSize);
        SetRect((RectTransform)Accept.transform, new Vector2((buttonSize.x + gap) * .5f, buttonY), buttonSize);

        float top = size.y * .5f - 16f * units;
        float titleHeight = 26f * units;
        float copyBottom = bottom + buttonSize.y + 10f * units;
        float copyTop = top - titleHeight;
        PlaceLabel(heading, top - titleHeight * .5f, new Vector2(size.x * .9f, titleHeight), 15f * units, theme.textColor);
        PlaceLabel(Message, (copyBottom + copyTop) * .5f,
            new Vector2(size.x * .9f, Mathf.Max(1, copyTop - copyBottom)), 25f * units, theme.textColor);
    }

    internal static Button CreateChoice(Transform parent, string name, string caption, bool primary, UnityAction click)
    {
        var rect = CreateRect(parent, name);
        var surface = rect.gameObject.AddComponent<ConfirmationGlass>();
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = surface;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.13f, 1.13f, 1.13f, 1);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.78f, .78f, .78f, 1);
        colors.disabledColor = new Color(.75f, .75f, .75f, .55f);
        button.colors = colors;
        button.onClick.AddListener(click);
        var label = CreateLabel(rect, "Choice Label", caption);
        var textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(3f, 2f);
        textRect.offsetMax = new Vector2(-3f, -2f);
        surface.ConfigureButton(primary, label, button);
        return button;
    }

    internal static ConfirmationGlass AddSurface(RectTransform rect)
    {
        var graphic = rect.gameObject.AddComponent<ConfirmationGlass>();
        graphic.raycastTarget = false;
        return graphic;
    }

    internal static Font WindowFont
    {
        get
        {
            if (font == null) font = Resources.Load<Font>("TCardResource/Lora-Regular");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

    static Text CreateLabel(RectTransform parent, string name, string caption)
    {
        var label = CreateRect(parent, name).gameObject.AddComponent<Text>();
        label.font = WindowFont;
        label.text = caption;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        return label;
    }

    static void PlaceLabel(Text label, float y, Vector2 size, float maximumFont, Color color)
    {
        SetRect(label.rectTransform, new Vector2(0, y), size);
        label.resizeTextMaxSize = Mathf.Max(1, Mathf.RoundToInt(maximumFont));
        label.resizeTextMinSize = Mathf.Max(1, Mathf.RoundToInt(maximumFont * .62f));
        label.color = color;
    }

    static RectTransform CreateRect(Transform parent, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
        var rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        return rect;
    }

    internal static void SetRect(RectTransform rect, Vector2 center, Vector2 size)
    {
        rect.anchoredPosition = center;
        rect.sizeDelta = size;
    }

    internal static void PositionOnScreen(RectTransform body, RectTransform parent, Vector3 anchor, Vector2 offset)
    {
        var canvas = parent.GetComponentInParent<Canvas>().rootCanvas;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, body.TransformPoint(body.rect.min));
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, body.TransformPoint(body.rect.max));
        Rect safe = Screen.safeArea;
        Vector2 size = Vector2.Min(topRight - bottomLeft, safe.size);
        Vector2 preferred = RectTransformUtility.WorldToScreenPoint(camera, anchor) + offset * (Screen.width / 720f);
        Vector2 center = ClampCenter(safe, size, preferred);
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, center, camera, out var world))
            body.position = world;
    }

    static Vector2 ClampCenter(Rect safe, Vector2 size, Vector2 preferred)
    {
        Vector2 half = size * .5f;
        return new Vector2(Mathf.Clamp(preferred.x, safe.xMin + half.x, safe.xMax - half.x),
            Mathf.Clamp(preferred.y, safe.yMin + half.y, safe.yMax - half.y));
    }

    internal Rect FindPlacement(Rect safe, Vector2 requestedSize, Vector2 preferred, float gap, List<Rect> obstacles, bool avoidObstacles)
    {
        Vector2 size = Vector2.Min(requestedSize, safe.size);
        Vector2 half = size * .5f;
        Vector2 clamped = ClampCenter(safe, size, preferred);
        if (!avoidObstacles || obstacles.Count == 0) return new Rect(clamped - half, size);
        Vector2 min = safe.min + half;
        Vector2 max = safe.max - half;
        candidateX.Clear();
        candidateY.Clear();
        candidateX.Add(clamped.x);
        candidateY.Add(clamped.y);
        candidateX.Add(min.x);
        candidateX.Add(max.x);
        candidateY.Add(min.y);
        candidateY.Add(max.y);
        foreach (var obstacle in obstacles)
        {
            candidateX.Add(Mathf.Clamp(obstacle.xMin - gap - half.x, min.x, max.x));
            candidateX.Add(Mathf.Clamp(obstacle.xMax + gap + half.x, min.x, max.x));
            candidateY.Add(Mathf.Clamp(obstacle.yMin - gap - half.y, min.y, max.y));
            candidateY.Add(Mathf.Clamp(obstacle.yMax + gap + half.y, min.y, max.y));
        }

        Rect best = default;
        float bestOverlap = float.PositiveInfinity;
        float bestDistance = float.PositiveInfinity;
        foreach (float x in candidateX)
        foreach (float y in candidateY)
        {
            Vector2 center = new Vector2(x, y);
            Rect candidate = new Rect(center - half, size);
            float overlap = 0;
            foreach (var obstacle in obstacles)
            {
                float width = Mathf.Min(candidate.xMax, obstacle.xMax + gap) - Mathf.Max(candidate.xMin, obstacle.xMin - gap);
                float height = Mathf.Min(candidate.yMax, obstacle.yMax + gap) - Mathf.Max(candidate.yMin, obstacle.yMin - gap);
                if (width > 0 && height > 0) overlap += width * height;
            }
            float distance = (center - preferred).sqrMagnitude;
            if (overlap > bestOverlap || (overlap == bestOverlap && distance >= bestDistance)) continue;
            best = candidate;
            bestOverlap = overlap;
            bestDistance = distance;
        }
        return best;
    }
}
