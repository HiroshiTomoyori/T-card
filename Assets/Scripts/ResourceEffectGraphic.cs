using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ResourceEffectGraphic : MaskableGraphic
{
    Material owned;
    public void Draw(LiquidResourceSettings s,float clock,float chargeAge,float hoverAge,int mode)
    {
        if(s==null || s.chargeEffectsShader==null)return;
        if(owned==null||owned.shader!=s.chargeEffectsShader){Release();owned=new Material(s.chargeEffectsShader){hideFlags=HideFlags.HideAndDontSave};material=owned;}
        owned.SetVector("_RectSize",rectTransform.rect.size);
        owned.SetVector("_Panel",new Vector4(s.panelSize.x,s.panelSize.y,s.cornerRadius,mode));
        owned.SetVector("_Motion",new Vector4(clock,chargeAge/Mathf.Max(.1f,s.beamDuration),hoverAge,0));
        owned.SetVector("_Shape",new Vector4(Mathf.Max(1,s.beamWidth),Mathf.Max(1,s.beamHeight),s.rootGlowWidth,s.rootGlowHeight));
        owned.SetVector("_Beam",new Vector4(s.beamIntensity,s.beamCoreWidth,s.beamSoftness,s.beamRiseFraction));
        owned.SetVector("_Timing",new Vector4(s.beamFadeStart,s.beamFadePower,s.beamCurveAmount,s.beamFilamentStrength));
        owned.SetVector("_Root",new Vector4(s.rootGlowStrength,s.rootGlowTail/Mathf.Max(.1f,s.beamDuration),0,0));
        owned.SetVector("_Target",new Vector4(s.targetRipplePeriod,s.targetRippleStrength,s.targetRippleWidth,s.targetRippleSpread));
        owned.SetFloat("_TargetCount",Mathf.Clamp(s.targetRippleCount,0,3));
        owned.SetFloat("_TargetGlow",s.targetGlowStrength);
        owned.SetVector("_TargetPulse",new Vector4(s.targetPulsePeriod,s.targetMinimumGlow,s.targetEnterFlashStrength,s.targetEnterFlashDuration));
        owned.SetFloat("_TargetFill",s.targetFillStrength);
        owned.SetColor("_TargetColor",ToLinear(s.targetHighlightColor));
        owned.SetColor("_BeamColor",ToLinear(s.beamColor));owned.SetColor("_RootColor",ToLinear(s.rootGlowColor));
        owned.SetColor("_GlowColor",ToLinear(s.glowColor));owned.SetColor("_WaveColor",ToLinear(s.wave2Color));
        raycastTarget=false;maskable=false;
    }
    static Color ToLinear(Color c)=>QualitySettings.activeColorSpace==ColorSpace.Linear?c.linear:c;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;
        vh.AddVert(new Vector3(r.xMin,r.yMin),color,new Vector2(0,0));vh.AddVert(new Vector3(r.xMin,r.yMax),color,new Vector2(0,1));vh.AddVert(new Vector3(r.xMax,r.yMax),color,new Vector2(1,1));vh.AddVert(new Vector3(r.xMax,r.yMin),color,new Vector2(1,0));vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
    }
    void Release(){if(owned!=null){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);owned=null;}}
    protected override void OnDestroy(){Release();base.OnDestroy();}
}
