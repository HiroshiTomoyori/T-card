using UnityEngine;

// Native AE alpha passes, independently tinted. No procedural replacement.
public sealed class AEAttackVisual
{
    public readonly RectTransform root;
    public readonly AEAttackClip clip;
    readonly AEAttackMaskGraphic[] graphics=new AEAttackMaskGraphic[3];
    public float SampleTime {get;private set;}
    public bool Cross {get;private set;}
    public Vector2 Center=>root.localPosition;
    public AEAttackMaskGraphic[] Graphics=>graphics;
    public AEAttackVisual(Transform parent,AEAttackClip sequence,bool cross)
    {
        clip=sequence;Cross=cross;
        var go=new GameObject(cross?"AE Monarch wall cross":"AE losing-card slash",typeof(RectTransform));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);root=(RectTransform)go.transform;
        for(int i=0;i<3;i++)
        {
            var child=new GameObject(i==0?"Bloom":i==1?"Fragments":"Blade",typeof(RectTransform),typeof(CanvasRenderer),typeof(AEAttackMaskGraphic));
            child.layer=go.layer;child.transform.SetParent(root,false);
            var rt=(RectTransform)child.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
            graphics[i]=child.GetComponent<AEAttackMaskGraphic>();
        }
    }
    public void Sample(Vector2 center,Vector2 size,float time,AEAttackSettings.Palette palette,AEAttackSettings settings,float strength)
    {
        root.localPosition=center;root.sizeDelta=size;root.localRotation=Quaternion.Euler(0,0,Cross?settings.wallAngle:settings.cardAngle);SampleTime=time;
        for(int i=0;i<3;i++){var color=palette.At(i);color.a*=settings.opacity*Mathf.Clamp01(strength)*(i==0?settings.glowOpacity:1);graphics[i].Sample(clip,i,time,color);}
    }
}
