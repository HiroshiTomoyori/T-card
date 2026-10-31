using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class AEAttackMaskGraphic : MaskableGraphic
{
    Texture2D frame;
    Rect crop;
    Material owned;
    public override Texture mainTexture=>frame!=null?frame:Texture2D.whiteTexture;
    public int FrameIndex {get;private set;}=-1;
    protected override void Awake()
    {
        base.Awake();
        var shader=Resources.Load<Shader>("TCardAttackEffects/AEAttackMask");
        if(shader!=null){owned=new Material(shader){hideFlags=HideFlags.HideAndDontSave};material=owned;}
        raycastTarget=false;maskable=false;
    }
    public void Sample(AEAttackClip clip,int pass,float time,Color tint)
    {
        int index=time>=0&&time<clip.Duration?Mathf.Clamp(Mathf.FloorToInt(time*clip.frameRate),0,clip.frameCount-1):-1;
        FrameIndex=index;
        var p=clip.passes[pass];
        var next=index>=0&&p.frames!=null&&index<p.frames.Length?p.frames[index]:null;
        crop=next!=null&&p.rectangles!=null&&index<p.rectangles.Length?p.rectangles[index]:default;
        if(frame!=next){frame=next;SetMaterialDirty();}
        color=tint;SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();if(frame==null||crop.width<=0||crop.height<=0)return;
        Rect full=GetPixelAdjustedRect();
        float x=full.x+crop.x*full.width,y=full.y+crop.y*full.height,w=crop.width*full.width,h=crop.height*full.height;
        vh.AddVert(new Vector3(x,y),color,new Vector2(0,0));
        vh.AddVert(new Vector3(x,y+h),color,new Vector2(0,1));
        vh.AddVert(new Vector3(x+w,y+h),color,new Vector2(1,1));
        vh.AddVert(new Vector3(x+w,y),color,new Vector2(1,0));
        vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
    }
    protected override void OnDestroy(){if(owned!=null)Destroy(owned);base.OnDestroy();}
}
