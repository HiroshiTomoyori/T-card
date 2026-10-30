using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SummonAbilityPulseGraphic:MaskableGraphic
{
    Vector2 cardSize;Color tint;float phase,strength,expansion;
    public void Configure(Vector2 size,Color c,float progress,float power,float expand){cardSize=size;tint=c;phase=Mathf.Clamp01(progress);strength=power;expansion=expand;raycastTarget=false;maskable=false;SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();float envelope=Mathf.Sin(Mathf.PI*phase)*strength;
        for(int layer=0;layer<9;layer++)
        {
            float fade=1-layer/9f;float radius=Mathf.Lerp(.54f,expansion*.58f,phase);float blur=1+layer*.024f;
            var c=tint;c.a*=envelope*fade*.13f;
            const int steps=80;for(int i=0;i<steps;i++)
            {
                float a=i*Mathf.PI*2/steps,b=(i+1)*Mathf.PI*2/steps;var outer=cardSize*radius*blur;var inner=outer*new Vector2(.97f,.97f);int n=vh.currentVertCount;
                vh.AddVert(new Vector3(Mathf.Cos(a)*outer.x,Mathf.Sin(a)*outer.y),c,Vector2.zero);vh.AddVert(new Vector3(Mathf.Cos(b)*outer.x,Mathf.Sin(b)*outer.y),c,Vector2.zero);vh.AddVert(new Vector3(Mathf.Cos(b)*inner.x,Mathf.Sin(b)*inner.y),c,Vector2.zero);vh.AddVert(new Vector3(Mathf.Cos(a)*inner.x,Mathf.Sin(a)*inner.y),c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
        var light=tint;light.a*=envelope*.24f;int start=vh.currentVertCount;Vector2 half=cardSize*.5f;
        vh.AddVert(new Vector3(-half.x,-half.y),light,Vector2.zero);vh.AddVert(new Vector3(-half.x,half.y),light,Vector2.zero);vh.AddVert(new Vector3(half.x,half.y),light,Vector2.zero);vh.AddVert(new Vector3(half.x,-half.y),light,Vector2.zero);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
    }
}
