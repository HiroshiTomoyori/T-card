using UnityEngine;
using UnityEngine.UI;
public sealed class TurnDrawHighlightGraphic : MaskableGraphic
{
    public bool surface;
    float amount,spread,sweep;Color tint;
    public void Configure(float strength,float width,float progress,Color value){amount=Mathf.Max(0,strength);spread=Mathf.Max(0,width);sweep=progress;tint=value;SetVerticesDirty();}
    Vector2 Corner(int index,Vector2 half,float radius)
    {
        const int per=8;int corner=index/per;float angle=(-90+corner*90+(index%per)*90f/per)*Mathf.Deg2Rad;
        Vector2 center=corner==0?new Vector2(half.x-radius,-half.y+radius):corner==1?new Vector2(half.x-radius,half.y-radius):corner==2?new Vector2(-half.x+radius,half.y-radius):new Vector2(-half.x+radius,-half.y+radius);
        return center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
    }
    void Vertex(VertexHelper vh,Vector2 point,float alpha){var v=UIVertex.simpleVert;v.position=point;v.color=new Color(tint.r,tint.g,tint.b,Mathf.Clamp01(alpha*tint.a));vh.AddVert(v);}
    void Ring(VertexHelper vh,Vector2 center,Vector2 half,float radius,float inner,float outer,float a,float b)
    {
        const int n=32;int start=vh.currentVertCount;
        for(int i=0;i<n;i++){Vertex(vh,center+Corner(i,half+Vector2.one*inner,radius+inner),a);Vertex(vh,center+Corner(i,half+Vector2.one*outer,radius+outer),b);}
        for(int i=0;i<n;i++){int j=(i+1)%n;vh.AddTriangle(start+i*2,start+j*2,start+i*2+1);vh.AddTriangle(start+i*2+1,start+j*2,start+j*2+1);}
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();if(amount<=.0001f)return;Rect r=rectTransform.rect;Vector2 half=r.size*.5f,center=r.center;float radius=Mathf.Min(4,Mathf.Min(half.x,half.y)*.15f);
        if(!surface)
        {
            if(spread<=0)return;
            const int bands=20;
            for(int i=0;i<bands;i++)
            {
                float p=i/(float)bands,q=(i+1)/(float)bands;
                Ring(vh,center,half,radius,p*spread,q*spread,amount*.75f*Mathf.Pow(1-p,2.5f),amount*.75f*Mathf.Pow(1-q,2.5f));
            }
            Ring(vh,center,half,radius,.2f,2.1f,amount*.9f,0);return;
        }
        Ring(vh,center,half-Vector2.one*1.1f,radius,0,1,amount*.65f,0);
        // A clipped diagonal light sweep leaves the card artwork readable.
        const int cells=16;
        for(int y=0;y<cells;y++)for(int x=0;x<cells;x++)
        {
            int start=vh.currentVertCount;
            for(int k=0;k<4;k++)
            {
                float u=(x+(k==1||k==2?1:0))/(float)cells,v=(y+(k>=2?1:0))/(float)cells;
                float d=(u+v)*.5f-Mathf.Lerp(-.3f,1.3f,sweep);
                float a=amount*.3f*Mathf.Exp(-d*d/.022f);
                Vertex(vh,new Vector2(Mathf.Lerp(r.xMin+2,r.xMax-2,u),Mathf.Lerp(r.yMin+2,r.yMax-2,v)),a);
            }
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
