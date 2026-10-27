using UnityEngine;
using UnityEngine.UI;
// A soft annulus and sparse drifting lights, with no texture or input handlers.
public sealed class CardPlayAtmosphereGraphic : MaskableGraphic
{
    CardPlaySettings settings;Vector2 cardSize;float elapsed,scale;bool isEnemy;
    public float RippleAlpha {get;private set;}
    public void Configure(CardPlaySettings value,Vector2 size,float time,float factor,bool enemy=false)
    {settings=value;isEnemy=enemy;cardSize=size;elapsed=time;scale=factor;SetVerticesDirty();}
    float Smooth(float a,float b,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
    void Vertex(VertexHelper vh,Vector2 p,Color c,float alpha)
    {var v=UIVertex.simpleVert;v.position=p;v.color=new Color(c.r,c.g,c.b,Mathf.Clamp01(c.a*alpha));vh.AddVert(v);}
    void Band(VertexHelper vh,float inner,float outer,Color color,float a,float b)
    {
        const int n=112;int start=vh.currentVertCount;
        for(int i=0;i<=n;i++){float angle=i*Mathf.PI*2/n;Vector2 d=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));Vertex(vh,d*inner,color,a);Vertex(vh,d*outer,color,b);}
        for(int i=0;i<n;i++){int v=start+i*2;vh.AddTriangle(v,v+2,v+1);vh.AddTriangle(v+1,v+2,v+3);}
    }
    void Spot(VertexHelper vh,Vector2 center,float radius,Color color,float alpha)
    {
        const int n=16;int start=vh.currentVertCount;Vertex(vh,center,color,alpha);
        for(int i=0;i<=n;i++){float angle=i*Mathf.PI*2/n;Vertex(vh,center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,color,0);}
        for(int i=0;i<n;i++)vh.AddTriangle(start,start+i+1,start+i+2);
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();RippleAlpha=0;if(settings==null)return;
        float turn=settings.RotateTime,arrival=turn+settings.HoldTime,total=settings.TotalTime;
        float strength=Smooth(turn*.08f,turn*.84f,elapsed)*(1-Smooth(arrival,total,elapsed));
        const int bands=28;float radius=285*scale;
        for(int i=0;i<bands;i++){float p=i/(float)bands,q=(i+1)/(float)bands;Band(vh,p*radius,q*radius,settings.Aura(isEnemy),strength*settings.auraStrength*Mathf.Pow(1-p,2),strength*settings.auraStrength*Mathf.Pow(1-q,2));}
        for(int i=0;i<Mathf.Clamp(settings.particleCount,0,60);i++)
        {
            float u=Mathf.Repeat(i*.618f+elapsed*.13f,1),theta=i*2.399f+elapsed*.07f,r=(145+u*110)*scale;
            var point=new Vector2(Mathf.Cos(theta),Mathf.Sin(theta)*1.1f)*r;
            float a=strength*settings.particleStrength*Mathf.Sin(Mathf.PI*u);
            Spot(vh,point,(6+i%3)*scale,settings.Highlight(isEnemy),a*.22f);Spot(vh,point,(1.2f+i%3*.4f)*scale,settings.Highlight(isEnemy),a);
        }
        float duration=Mathf.Max(.01f,settings.rippleDuration),progress=Mathf.Clamp01((elapsed-turn)/duration);
        RippleAlpha=elapsed<turn||elapsed>=turn+duration?0:settings.rippleStrength*Smooth(turn,turn+Mathf.Min(.03f,duration*.2f),elapsed)*Mathf.Pow(1-progress,1.5f);
        if(RippleAlpha<=0)return;
        float ring=cardSize.magnitude*.5f+(11+settings.rippleExpansion*(1-Mathf.Pow(1-progress,3)))*scale;
        float softness=Mathf.Max(1,settings.rippleSoftness)*scale;
        for(int i=0;i<20;i++)
        {
            float a=-3+6*i/20f,b=-3+6*(i+1)/20f;
            Band(vh,ring+a*softness,ring+b*softness,settings.Ripple(isEnemy),RippleAlpha*.48f*Mathf.Exp(-a*a*.8f),RippleAlpha*.48f*Mathf.Exp(-b*b*.8f));
        }
    }
}
