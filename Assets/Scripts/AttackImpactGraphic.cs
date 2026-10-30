using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public sealed class AttackImpactGraphic : MaskableGraphic
{
    public float Age=>age; public int VertexCount {get;private set;}
    AttackMotionSettings settings;IList<Vector2> centers;float age,unit;Color tint;
    public void Configure(AttackMotionSettings s,IList<Vector2> points,float elapsed,Color color,float scale){settings=s;centers=points;age=elapsed;tint=color;unit=scale;maskable=false;raycastTarget=false;canvasRenderer.cull=false;SetVerticesDirty();}
    static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color){Vector2 d=(b-a).normalized,n=new Vector2(-d.y,d.x)*width*.5f;int i=vh.currentVertCount;vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddVert(a-n,color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
    static void Aura(VertexHelper vh,Vector2 point,float radius,Color color){int i=vh.currentVertCount;vh.AddVert(point,color,Vector2.zero);Color edge=color;edge.a=0;for(int n=0;n<=40;n++){float a=n*Mathf.PI*2/40;vh.AddVert(point+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,edge,Vector2.zero);if(n>0)vh.AddTriangle(i,i+n,i+n+1);}}
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        VertexCount=0;vh.Clear();if(settings==null||age<0||age>settings.hitDuration||centers==null)return;
        float u=Mathf.Clamp01(age/settings.hitDuration),fade=(1-Mathf.SmoothStep(0,1,u))*settings.hitStrength,growth=1-Mathf.Pow(1-Mathf.Clamp01(u/.24f),3),scale=unit*settings.hitScale;
        Quaternion rotation=Quaternion.Euler(0,0,settings.slashAngle);
        foreach(var center in centers){var aura=tint;aura.a=.46f*fade;Aura(vh,center,124*scale,aura);
            for(int pass=0;pass<3;pass++){float width=(pass==0?18:pass==1?6:2)*scale;for(int n=0;n<24;n++){float a=n/24f,b=(n+1)/24f;Vector2 p=new Vector2(Mathf.Lerp(-76,76,a),Mathf.Lerp(-72,72,a))*growth*scale;Vector2 q=new Vector2(Mathf.Lerp(-76,76,b),Mathf.Lerp(-72,72,b))*growth*scale;p=rotation*p;q=rotation*q;Color c=pass==2?Color.white:tint;c.a=fade*Mathf.Sin(a*Mathf.PI)*(pass==0?.17f:pass==1?.65f:1);Line(vh,center+p,center+q,width,c);}}
            for(int n=0;n<9;n++){float a=n*2.399f+1;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Color c=n%3==0?Color.white:tint;c.a=fade*.65f;float r=(19+76*u)*scale;Line(vh,center+d*r,center+d*(r+(1-u)*(8+n%3*5)*scale),1.1f*scale,c);}
        }
        VertexCount=vh.currentVertCount;
    }
}
