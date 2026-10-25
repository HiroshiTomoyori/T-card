using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public sealed class LiquidResourceSeparator : MaskableGraphic
{
    LiquidResourceSettings settings;
    public void Configure(LiquidResourceSettings value){settings=value;raycastTarget=false;rectTransform.sizeDelta=value.panelSize;color=value.numberColor;SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();if(settings==null)return;
        Vector2 a=new Vector2(-settings.panelSize.x*34/350,-settings.panelSize.y*82/218),b=-a;
        Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized;
        for(int pass=0;pass<3;pass++)
        {
            int start=vh.currentVertCount;
            for(int i=0;i<=48;i++)
            {
                float t=i/48f;Vector2 center=Vector2.Lerp(a,b,t);
                float w=(.12f+Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.8f)*.88f)*settings.separatorWidth*.5f;
                Color c=pass==2?color:settings.glowColor;c.a*=pass==2?1:.13f*settings.numberGlow;
                w+=pass==0?5:pass==1?2:0;
                if(pass==2)
                {
                    vh.AddVert(center-normal*w,c,Vector2.zero);vh.AddVert(center+normal*w,c,Vector2.zero);
                    if(i>0){int j=start+i*2;vh.AddTriangle(j-2,j-1,j);vh.AddTriangle(j,j-1,j+1);}
                }
                else
                {
                    Color edge=c;edge.a=0;
                    vh.AddVert(center-normal*w,edge,Vector2.zero);vh.AddVert(center,c,Vector2.zero);vh.AddVert(center+normal*w,edge,Vector2.zero);
                    if(i>0)for(int k=0;k<2;k++){int j=start+i*3+k;vh.AddTriangle(j-3,j-2,j);vh.AddTriangle(j,j-2,j+1);}
                }
            }
        }
    }
}
