using UnityEngine;
using UnityEngine.UI;

public sealed class WallDestructionGraphic : MaskableGraphic
{
    Texture2D frame;
    Rect crop;
    Material owned;
    bool shadow;
    Vector2 uvPadding,geometryPadding;
    public void ConfigureShadow()
    {
        shadow=true;
        var shader=Resources.Load<Shader>("TCardWallDestruction/WallDestructionShadow");
        if(owned!=null)Destroy(owned);
        owned=shader!=null?new Material(shader){hideFlags=HideFlags.HideAndDontSave}:null;material=owned;
    }
    public override Texture mainTexture=>frame!=null?frame:Texture2D.whiteTexture;
    public int FrameIndex {get;private set;}=-1;
    protected override void Awake()
    {
        base.Awake();
        var shader=Resources.Load<Shader>("TCardWallDestruction/WallDestructionLight");
        if(shader!=null){owned=new Material(shader){hideFlags=HideFlags.HideAndDontSave};material=owned;}
        raycastTarget=false;maskable=false;
    }
    public void Sample(WallDestructionSettings s,float age)
    {
        FrameIndex=Mathf.Clamp(s.impactFrame+Mathf.FloorToInt(age*s.frameRate),s.impactFrame,s.frames.Length-1);
        var next=s.frames[FrameIndex];crop=s.rectangles[FrameIndex];
        if(frame!=next){frame=next;SetMaterialDirty();}
        var tint=shadow?s.shadowColor:s.glowColor;tint.a*=s.opacity*(shadow?s.shadowOpacity:1);color=tint;
        if(shadow&&frame!=null)
        {
            float blur=Mathf.Max(1,s.shadowSoftness*s.sourceCardHeight),pad=blur*3;
            var uv=new Vector2(blur/frame.width,blur/frame.height);
            uvPadding=new Vector2(pad/frame.width,pad/frame.height);geometryPadding=new Vector2(pad/s.sourceSize.x,pad/s.sourceSize.y);
            if(owned!=null)owned.SetVector("_BlurUV",new Vector4(uv.x,uv.y,0,0));
        }
        if(!shadow&&owned!=null&&frame!=null)
        {
            owned.SetColor("_BottomColor",s.bottomColor);owned.SetColor("_TopColor",s.topColor);owned.SetColor("_RimColor",s.rimColor);
            owned.SetFloat("_GradientStrength",s.gradientStrength);owned.SetFloat("_SheenStrength",s.sheenStrength);owned.SetFloat("_RimStrength",s.rimStrength);
            owned.SetVector("_CropY",new Vector4(crop.y,crop.height,0,0));
            float edge=Mathf.Max(.5f,s.rimWidth*s.sourceCardHeight);
            owned.SetVector("_EdgeUV",new Vector4(edge/frame.width,edge/frame.height,0,0));
        }
        SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();if(frame==null||crop.width<=0||crop.height<=0)return;
        var r=GetPixelAdjustedRect();var c=crop;Vector2 uvMin=Vector2.zero,uvMax=Vector2.one;
        if(shadow){c.x-=geometryPadding.x;c.y-=geometryPadding.y;c.width+=geometryPadding.x*2;c.height+=geometryPadding.y*2;uvMin-=uvPadding;uvMax+=uvPadding;}
        float x=r.x+c.x*r.width,y=r.y+c.y*r.height,w=c.width*r.width,h=c.height*r.height;
        vh.AddVert(new Vector3(x,y),color,uvMin);vh.AddVert(new Vector3(x,y+h),color,new Vector2(uvMin.x,uvMax.y));
        vh.AddVert(new Vector3(x+w,y+h),color,uvMax);vh.AddVert(new Vector3(x+w,y),color,new Vector2(uvMax.x,uvMin.y));
        vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
    }
    protected override void OnDestroy(){if(owned!=null)Destroy(owned);base.OnDestroy();}
}

