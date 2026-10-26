using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class LiquidResourceGraphic : MaskableGraphic
{
    Material owned;
    public void Draw(LiquidResourceSettings s,float time,float level,float ripple,float beamAge,bool beam)
    {
        if(owned==null || owned.shader!=s.liquidShader)
        {
            Release(); owned=new Material(s.liquidShader){hideFlags=HideFlags.HideAndDontSave}; material=owned;
        }
        owned.SetVector("_RectSize",rectTransform.rect.size);
        owned.SetVector("_Panel",new Vector4(s.panelSize.x,s.panelSize.y,s.cornerRadius,s.auraStrength));
        owned.SetVector("_Motion",new Vector4(time,level,ripple,beamAge/Mathf.Max(.1f,s.beamDuration)));
        owned.SetVector("_Waves",new Vector4(s.waveAmplitude,s.waveSpeed,s.impactWaveAmplitude,beam?1:0));
        owned.SetColor("_DryColor",QualitySettings.activeColorSpace==ColorSpace.Linear?s.dryColor.linear:s.dryColor); owned.SetColor("_LiquidColor",QualitySettings.activeColorSpace==ColorSpace.Linear?s.liquidColor.linear:s.liquidColor);
        owned.SetColor("_DepthColor",QualitySettings.activeColorSpace==ColorSpace.Linear?s.depthColor.linear:s.depthColor); owned.SetColor("_WaveColor",QualitySettings.activeColorSpace==ColorSpace.Linear?s.waveColor.linear:s.waveColor);
        owned.SetColor("_BeamColor",QualitySettings.activeColorSpace==ColorSpace.Linear?s.beamColor.linear:s.beamColor);
        owned.SetColor("_Wave2Color",QualitySettings.activeColorSpace==ColorSpace.Linear?s.wave2Color.linear:s.wave2Color);owned.SetColor("_GlowColor",QualitySettings.activeColorSpace==ColorSpace.Linear?s.glowColor.linear:s.glowColor);
        owned.SetVector("_Light",new Vector4(s.liquidGlow,s.lightFlicker,s.lightFlickerSpeed,s.auraPadding));
        owned.SetTexture("_SuitTex",s.suitAtlas!=null?s.suitAtlas:Texture2D.blackTexture);
        owned.SetVector("_Suits",new Vector4(s.suitCount,s.suitScale,s.suitWarp,s.suitSpeed));
        owned.SetVector("_SuitLight",new Vector4(s.suitOpacity,s.suitGlow,0,0));
        owned.SetColor("_SuitColor",QualitySettings.activeColorSpace==ColorSpace.Linear?s.suitColor.linear:s.suitColor);
        raycastTarget=false; maskable=false;
    }
    protected override void OnPopulateMesh(VertexHelper v)
    {
        v.Clear(); Rect r=rectTransform.rect;
        v.AddVert(new Vector3(r.xMin,r.yMin),color,new Vector2(0,0));
        v.AddVert(new Vector3(r.xMin,r.yMax),color,new Vector2(0,1));
        v.AddVert(new Vector3(r.xMax,r.yMax),color,new Vector2(1,1));
        v.AddVert(new Vector3(r.xMax,r.yMin),color,new Vector2(1,0));
        v.AddTriangle(0,1,2);v.AddTriangle(2,3,0);
    }
    void Release(){if(owned!=null){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);owned=null;}}
    protected override void OnDestroy(){Release();base.OnDestroy();}
}
