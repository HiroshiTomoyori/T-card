using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class ConfirmationGlass : MaskableGraphic
{
    Material ownedMaterial;
    Text buttonLabel;
    Button button;
    bool isButton;
    bool primary;
    bool isEndTurn;

    internal void ConfigureEndTurnLabel(Text label, Button owner)
    {
        // Use the window's glass (including backdrop blur and reflection).
        buttonLabel = label;
        button = owner;
        isEndTurn = true;
        SetVerticesDirty();
        ApplyTheme();
    }

    internal void ConfigureButton(bool emphasized, Text label, Button owner)
    {
        isButton = true;
        primary = emphasized;
        buttonLabel = label;
        button = owner;
        SetVerticesDirty();
        ApplyTheme();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ApplyTheme();
    }

    void LateUpdate() => ApplyTheme();

    void ApplyTheme()
    {
        if (ownedMaterial == null)
        {
            var shader = Resources.Load<Shader>("UI/ConfirmationGlass");
            if (shader == null) return;
            ownedMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            material = ownedMaterial;
        }

        var theme = ConfirmationTheme.Load();
        Rect rect = rectTransform.rect;
        if (buttonLabel != null)
        {
            Color labelTint = isEndTurn ? new Color32(237, 233, 226, 255) : theme.buttonTextColor;
            labelTint.a *= button.interactable ? 1f : .53f;
            buttonLabel.color = labelTint;
            buttonLabel.resizeTextMaxSize = Mathf.Max(1, Mathf.RoundToInt(rect.height * (isEndTurn ? .309f : .36f)));
            buttonLabel.resizeTextMinSize = Mathf.Max(1, Mathf.RoundToInt(rect.height * .22f));
        }
        float units = isButton || isEndTurn
            ? Mathf.Min(rect.width / 134f, rect.height / 44f)
            : Mathf.Min(rect.width / 366f, rect.height / 208f);
        float radius = (isButton || isEndTurn ? theme.buttonCornerRadius : theme.cornerRadius) * Mathf.Max(.01f, units);
        Color fill = isButton ? (primary ? theme.primaryButtonColor : theme.secondaryButtonColor) : theme.glassColor;
        if (!isButton) fill.a *= theme.glassOpacity;
        Color lower = theme.lowerLight;
        Color frame = theme.frameColor;
        if (isButton) frame.a *= primary ? 1.18f : .67f;
        if (isEndTurn)
        {
            var states = button.colors;
            float activeBrightness = Mathf.Min(states.normalColor.r, states.highlightedColor.r, states.selectedColor.r);
            float tint = canvasRenderer.GetColor().r / Mathf.Max(.01f, states.colorMultiplier);
            float idle = button.IsInteractable() ? Mathf.InverseLerp(states.pressedColor.r, activeBrightness, tint) : 0f;
            fill = Color.Lerp(new Color32(29, 30, 43, 245), new Color32(39, 42, 57, 245), idle);
            lower = Color.Lerp(new Color32(58, 56, 69, 255), new Color32(92, 93, 112, 255), idle);
            frame = new Color32(180, 171, 184, 148);
        }
        ownedMaterial.SetColor("_Fill", fill);
        ownedMaterial.SetColor("_Lower", lower);
        ownedMaterial.SetColor("_Frame", frame);
        ownedMaterial.SetVector("_Shape", new Vector4(rect.width, rect.height, radius, Padding));
        ownedMaterial.SetVector("_Look", new Vector4(isButton ? 1f : 0f, theme.backdropBlur * Screen.width / 720f,
            isButton ? 0f : theme.reflectionStrength, Mathf.Max(.45f, units)));
    }

    float Padding => isButton ? 5f : 13f;

    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        Rect rect = GetPixelAdjustedRect();
        float pad = Padding;
        vertices.Clear();
        vertices.AddVert(new Vector3(rect.xMin - pad, rect.yMin - pad), color, Vector2.zero);
        vertices.AddVert(new Vector3(rect.xMin - pad, rect.yMax + pad), color, Vector2.up);
        vertices.AddVert(new Vector3(rect.xMax + pad, rect.yMax + pad), color, Vector2.one);
        vertices.AddVert(new Vector3(rect.xMax + pad, rect.yMin - pad), color, Vector2.right);
        vertices.AddTriangle(0, 1, 2);
        vertices.AddTriangle(2, 3, 0);
    }

    protected override void OnDestroy()
    {
        if (ownedMaterial != null) Destroy(ownedMaterial);
        base.OnDestroy();
    }
}
