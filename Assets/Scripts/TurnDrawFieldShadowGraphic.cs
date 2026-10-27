using UnityEngine;
using UnityEngine.UI;
// A soft projection rendered at field level, independently of the floating card Canvas.
public sealed class TurnDrawFieldShadowGraphic : MaskableGraphic
{
    Vector2 footprint;float blur=22,strength;Color tint;
    public float Strength=>strength;
    public void Configure(Vector2 size,float softness,float opacity,Color value)
    {
        if(footprint==size && Mathf.Approximately(blur,softness)&&Mathf.Approximately(strength,opacity)&&tint==value)return;
        footprint=size;blur=Mathf.Max(1,softness);strength=Mathf.Clamp01(opacity);tint=value;SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();if(strength<=.0001f)return;
        const int columns=24,rows=32;Rect rect=rectTransform.rect;Vector2 half=footprint*.5f;float radius=Mathf.Min(10,Mathf.Min(half.x,half.y)*.25f);
        for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
        {
            Vector2 p=new Vector2(Mathf.Lerp(rect.xMin,rect.xMax,x/(float)columns),Mathf.Lerp(rect.yMin,rect.yMax,y/(float)rows));
            // Slight projection skew; smooth distance falloff softens the entire silhouette.
            Vector2 sample=new Vector2(p.x+p.y*.06f,p.y);
            Vector2 q=new Vector2(Mathf.Abs(sample.x)-half.x+radius,Mathf.Abs(sample.y)-half.y+radius);
            float distance=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;
            float falloff=1/(1+Mathf.Exp(Mathf.Clamp(distance/blur*3,-30,30)));
            if(distance>blur*2.3f)falloff=0;
            var v=UIVertex.simpleVert;v.position=p;v.color=new Color(tint.r,tint.g,tint.b,strength*falloff*tint.a);vh.AddVert(v);
        }
        for(int y=0;y<rows;y++)for(int x=0;x<columns;x++){int i=y*(columns+1)+x;vh.AddTriangle(i,i+columns+1,i+1);vh.AddTriangle(i+1,i+columns+1,i+columns+2);}
    }
}
