using UnityEngine;
public static class OpeningShuffleMotion
{
    public struct Pose{public bool visible,landed;public Vector2 position;public float angle,scale,alpha;}
    public static Pose Sample(OpeningShuffleSettings s,int index,float time,Vector2 landing,Vector2 cardSize,float leftEdge)
    {
        float start=Mathf.Max(0,s.startDelay)+index*Mathf.Max(0,s.cardDropInterval);
        if(time<=start)return new Pose{visible=false,scale=1};
        float p=Mathf.Clamp01((time-start)/Mathf.Max(.02f,s.cardFallDuration)),e=.16f*p+.84f*p*p;
        float alignment=s.alignDuration<=0?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((time-s.DropEnd)/s.alignDuration));
        // Fully beyond the root Canvas's left edge, even at maximum approach scale.
        float sourceX=leftEdge-cardSize.x*(1+s.approachScale)*.5f-Mathf.Max(0,s.offscreenPadding)-(index%2)*cardSize.x*s.sourceSpread*.4f;
        float thickness=s.stackThickness*cardSize.x/146;
        Vector2 end=landing+new Vector2(index*cardSize.x*.00082f,index*thickness);
        float side=index%2==1?1:-1;
        return new Pose{visible=true,landed=p>=1,position=new Vector2(Mathf.Lerp(sourceX,end.x,Mathf.SmoothStep(0,1,Mathf.Clamp01(p/.96f))),Mathf.Lerp(landing.y+cardSize.y*Mathf.Max(.1f,s.fallHeightInCards),end.y,e)),angle=(side*s.startAngle+Mathf.Sin(index*2.4f)*.8f)*(1-p)+(1-alignment)*Mathf.Sin(index*1.7f)*.34f*p,scale=1+s.approachScale*(1-p),alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(p/.1f))};
    }
}
