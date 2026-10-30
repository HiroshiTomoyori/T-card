using UnityEngine;
using UnityEngine.UI;
// Execution-only material; selection/confirmation shaders and settings stay independent.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class AttackMotionFocusGraphic : MaskableGraphic
{
    Material owned;
    public int EntryCount {get;private set;}
    public float Opacity {get;private set;}
    public void Configure(AttackMotionSettings settings,Vector4[] boxes,Vector4[] colors,Vector4[] flags,int count,float opacity)
    {
        if(owned==null){var shader=Resources.Load<Shader>("TCardField/AttackMotionFocus");if(shader==null)return;owned=new Material(shader){hideFlags=HideFlags.HideAndDontSave};material=owned;}
        EntryCount=count;Opacity=opacity;
        owned.SetVectorArray("_Entries",boxes);owned.SetVectorArray("_Colors",colors);owned.SetVectorArray("_Flags",flags);owned.SetInt("_Count",count);
        float unit=Screen.width/720f;
        owned.SetVector("_ViewSize",new Vector4(Screen.width,Screen.height,unit,0));
        owned.SetVector("_Tune",new Vector4(settings.glowBlur*unit,settings.glowLineWidth*unit,1,0));
        owned.SetColor("_DimColor",settings.lowlightColor);owned.SetFloat("_Opacity",opacity);
        maskable=false;raycastTarget=false;color=Color.white;canvasRenderer.cull=false;SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;
        vh.AddVert(new Vector3(r.xMin,r.yMin),Color.white,Vector2.zero);
        vh.AddVert(new Vector3(r.xMin,r.yMax),Color.white,Vector2.up);
        vh.AddVert(new Vector3(r.xMax,r.yMax),Color.white,Vector2.one);
        vh.AddVert(new Vector3(r.xMax,r.yMin),Color.white,Vector2.right);
        vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
    }
    protected override void OnDestroy(){if(owned!=null){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}base.OnDestroy();}
}
