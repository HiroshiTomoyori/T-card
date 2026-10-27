using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public sealed class FieldHighlightGraphic : MaskableGraphic
{
    Material owned;
    public void Show(FieldHighlightSettings settings,Vector2 size,float scale,float opacity,float time)
    {
        if(owned==null)
        {
            var shader=Shader.Find("T-card/UI/Field Drop Glow");if(shader==null)shader=Resources.Load<Shader>("TCardField/FieldDropGlow");
            if(shader==null)return;owned=new Material(shader){hideFlags=HideFlags.HideAndDontSave};material=owned;
        }
        owned.SetVector("_PanelSize",rectTransform.rect.size);
        owned.SetVector("_Shape",new Vector4(size.x,size.y,settings.cornerRadius*scale,Mathf.Max(1,settings.blur*scale)));
        owned.SetVector("_Appearance",new Vector4(opacity,settings.brightness,settings.interiorGlow,time));
        owned.SetVector("_Motion",new Vector4(Mathf.Max(.2f,settings.driftPeriod),settings.outwardSpread*scale,Mathf.Max(.1f,settings.lineWidth*scale),settings.outerGlow));
        owned.SetColor("_FieldColor",settings.highlightColor);raycastTarget=false;maskable=false;
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;vh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),color,Vector2.up);vh.AddVert(new Vector3(r.xMax,r.yMax),color,Vector2.one);vh.AddVert(new Vector3(r.xMax,r.yMin),color,Vector2.right);vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
    }
    protected override void OnDestroy(){if(owned!=null){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}base.OnDestroy();}
}
