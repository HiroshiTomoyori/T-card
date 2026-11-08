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
            buttonLabel.color = theme.buttonTextColor * (button.interactable ? Color.white : new Color(1, 1, 1, .53f));
            buttonLabel.resizeTextMaxSize = Mathf.Max(1, Mathf.RoundToInt(rect.height * .36f));
            buttonLabel.resizeTextMinSize = Mathf.Max(1, Mathf.RoundToInt(rect.height * .22f));
        }
        float units = isButton
            ? Mathf.Min(rect.width / 134f, rect.height / 44f)
            : Mathf.Min(rect.width / 366f, rect.height / 208f);
        float radius = (isButton ? theme.buttonCornerRadius : theme.cornerRadius) * Mathf.Max(.01f, units);
        Color fill = isButton ? (primary ? theme.primaryButtonColor : theme.secondaryButtonColor) : theme.glassColor;
        if (!isButton) fill.a *= theme.glassOpacity;
        Color frame = theme.frameColor;
        if (isButton) frame.a *= primary ? 1.18f : .67f;
        ownedMaterial.SetColor("_Fill", fill);
        ownedMaterial.SetColor("_Lower", theme.lowerLight);
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
