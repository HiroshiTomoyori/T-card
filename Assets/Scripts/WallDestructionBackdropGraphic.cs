using UnityEngine;
using UnityEngine.UI;

// Broad contrast behind the whole effect, independent of the animated fragment shadow.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class WallDestructionBackdropGraphic : MaskableGraphic
{
    Material owned;
    public void Configure(WallDestructionSettings settings)
    {
        if(owned==null)
        {
            var shader=Resources.Load<Shader>("TCardWallDestruction/WallDestructionBackdrop");
            if(shader==null)return;
            owned=new Material(shader){hideFlags=HideFlags.HideAndDontSave};material=owned;
        }
        var tint=settings.backdropColor;tint.a*=settings.backdropOpacity;color=tint;
        owned.SetFloat("_Falloff",Mathf.Max(.5f,settings.backdropFalloff));
        maskable=false;raycastTarget=false;SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=GetPixelAdjustedRect();
        vh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);
        vh.AddVert(new Vector3(r.xMin,r.yMax),color,Vector2.up);
        vh.AddVert(new Vector3(r.xMax,r.yMax),color,Vector2.one);
        vh.AddVert(new Vector3(r.xMax,r.yMin),color,Vector2.right);
        vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
    }
    protected override void OnDestroy(){if(owned!=null)Destroy(owned);base.OnDestroy();}
}
