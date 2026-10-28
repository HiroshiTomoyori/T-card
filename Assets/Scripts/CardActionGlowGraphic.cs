using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CardActionGlowGraphic : MaskableGraphic
{
    Material owned;
    public void Configure(CardActionGlowSettings s,Vector2 size,float opacity,bool defense)
    {
        if(owned==null)
        {
            var shader=Resources.Load<Shader>("TCardField/CardActionGlow");if(shader==null)return;
            owned=new Material(shader){hideFlags=HideFlags.HideAndDontSave};material=owned;
        }
        float scale=size.x/118f;
        owned.SetVector("_PanelSize",rectTransform.rect.size);
        owned.SetVector("_Shape",new Vector4(size.x+2*s.edgeOffset*scale,size.y+2*s.edgeOffset*scale,s.cornerRadius*scale,Mathf.Max(1,s.blur*scale)));
        owned.SetVector("_Light",new Vector4(opacity*s.brightness*s.EvaluatePulse(Time.unscaledTime),Mathf.Max(.1f,s.lineWidth*scale),0,0));
        owned.SetColor("_GlowColor",defense?s.defenseColor:s.attackColor);raycastTarget=false;maskable=false;
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;vh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),color,Vector2.up);vh.AddVert(new Vector3(r.xMax,r.yMax),color,Vector2.one);vh.AddVert(new Vector3(r.xMax,r.yMin),color,Vector2.right);vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
    }
    protected override void OnDestroy(){if(owned!=null){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}base.OnDestroy();}
}
