using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CardShadowGraphic : MaskableGraphic
{
    Material owned;
    public void Configure(CardShadowSettings s,Vector2 size)
    {
        if(s==null || s.shadowShader==null)return;
        if(owned==null || owned.shader!=s.shadowShader)
        {
            Release();owned=new Material(s.shadowShader){hideFlags=HideFlags.HideAndDontSave};material=owned;
        }
        float scale=size.x/Mathf.Max(1,s.referenceWidth);
        owned.SetVector("_RectSize",rectTransform.rect.size);
        owned.SetVector("_Card",new Vector4(size.x,size.y,size.x*s.cornerRadiusRatio,scale));
        owned.SetVector("_Shadow",new Vector4(s.strength,s.blur,s.contactStrength,s.contactBlur));
        owned.SetVector("_Offset",s.offset*scale);
        owned.SetColor("_ShadowColor",QualitySettings.activeColorSpace==ColorSpace.Linear?s.shadowColor.linear:s.shadowColor);
        raycastTarget=false;
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;
        vh.AddVert(new Vector3(r.xMin,r.yMin),color,new Vector2(0,0));
        vh.AddVert(new Vector3(r.xMin,r.yMax),color,new Vector2(0,1));
        vh.AddVert(new Vector3(r.xMax,r.yMax),color,new Vector2(1,1));
        vh.AddVert(new Vector3(r.xMax,r.yMin),color,new Vector2(1,0));
        vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
    }
    void Release(){if(owned!=null){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);owned=null;}}
    protected override void OnDestroy(){Release();base.OnDestroy();}
}
