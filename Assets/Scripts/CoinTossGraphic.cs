using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CoinTossGraphic : MaskableGraphic
{
    private Material ownedMaterial;
    private Texture activeTexture;
    public override Texture mainTexture => activeTexture != null ? activeTexture : Texture2D.whiteTexture;

    public void ShowFrame(CoinTossSettings settings, CoinTossPose pose)
    {
        if (ownedMaterial == null || ownedMaterial.shader != settings.coinShader)
        {
            ReleaseMaterial();
            ownedMaterial = new Material(settings.coinShader) { hideFlags = HideFlags.HideAndDontSave };
            material = ownedMaterial;
        }
        bool front = pose.cosine >= 0f;
        Texture texture = front ? settings.butterflyTexture : settings.moonTexture;
        if (texture != activeTexture) { activeTexture = texture; SetMaterialDirty(); }
        ownedMaterial.SetTexture("_MainTex", mainTexture);
        ownedMaterial.SetVector("_FaceRect", front ? settings.butterflyUV : settings.moonUV);
        ownedMaterial.SetVector("_PanelSize", rectTransform.rect.size);
        ownedMaterial.SetVector("_Pose", new Vector4(pose.center.x, pose.center.y, pose.diameter, pose.cosine));
        ownedMaterial.SetVector("_Spin", new Vector4(pose.sine, pose.rollDegrees * Mathf.Deg2Rad, pose.arc, pose.time));
        ownedMaterial.SetColor("_CoinTint", settings.coinTint);
        ownedMaterial.SetVector("_Appearance", new Vector4(settings.brightness, settings.saturation, settings.sheen, settings.backgroundDim));
        ownedMaterial.SetVector("_Edge", new Vector4(settings.thicknessRatio, settings.reedingSpacing, settings.reedingStrength, settings.shadowOpacity));
        var shadow=CoinTossMotion.EvaluateShadow(settings,pose);
        ownedMaterial.SetVector("_ShadowPose",new Vector4(shadow.center.x,shadow.center.y,shadow.radii.x,shadow.radii.y));
        ownedMaterial.SetVector("_ShadowAppearance",new Vector4(shadow.opacity,Mathf.Max(.2f,settings.shadowSoftness),settings.contactShadowStrength,shadow.elevation));
        ownedMaterial.SetColor("_ShadowColor",settings.shadowColor);
        ownedMaterial.SetColor("_EdgeDark", settings.edgeDark);
        ownedMaterial.SetColor("_EdgeLight", settings.edgeLight);
        ownedMaterial.SetColor("_EdgeHighlight", settings.edgeHighlight);
        raycastTarget = false;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        vh.AddVert(new Vector3(r.xMin, r.yMin), color, new Vector2(0, 0));
        vh.AddVert(new Vector3(r.xMin, r.yMax), color, new Vector2(0, 1));
        vh.AddVert(new Vector3(r.xMax, r.yMax), color, new Vector2(1, 1));
        vh.AddVert(new Vector3(r.xMax, r.yMin), color, new Vector2(1, 0));
        vh.AddTriangle(0, 1, 2); vh.AddTriangle(2, 3, 0);
    }

    protected override void OnDestroy() { ReleaseMaterial(); base.OnDestroy(); }
    private void ReleaseMaterial()
    {
        if (ownedMaterial == null) return;
        if (Application.isPlaying) Destroy(ownedMaterial); else DestroyImmediate(ownedMaterial);
        ownedMaterial = null;
    }
}
