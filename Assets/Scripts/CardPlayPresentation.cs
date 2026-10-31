using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public sealed class CardPlayPresentation : MonoBehaviour
{
    static int active,activeActs;
    bool isActPresentation;
    public static bool IsActPlaying=>activeActs>0;
    public static bool IsPlaying=>active>0;
    public float Elapsed {get;private set;}
    public bool IsEnemy {get;private set;}
    public Image Artwork {get;private set;}
    public CardPlayAtmosphereGraphic Atmosphere {get;private set;}
    void OnEnable(){active++;}
    void OnDisable(){active=Mathf.Max(0,active-1);if(isActPresentation)activeActs=Mathf.Max(0,activeActs-1);}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState(){active=activeActs=0;}
    static RectTransform Rect(Transform parent,string name)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);
        var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);return rt;
    }
    static T Graphic<T>(Transform parent,string name) where T:Graphic
    {var rt=Rect(parent,name);rt.gameObject.AddComponent<CanvasRenderer>();var graphic=rt.gameObject.AddComponent<T>();graphic.raycastTarget=false;return graphic;}
    static float Smooth(float a,float b,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
    static float Ease(float p)=>1-Mathf.Pow(1-Mathf.Clamp01(p),3);
    public static IEnumerator Play(CardData data,Transform area,CardController replaced=null,bool isEnemy=false)
    {
        var s=CardPlaySettings.Load();if(!s.enabledEffect||data==null||data.artwork==null||area==null)yield break;
        Canvas owner=area.GetComponentInParent<Canvas>();if(owner==null)yield break;owner=owner.rootCanvas;
        var root=Rect(owner.transform,"Card play presentation");root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        var layer=root.gameObject.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingLayerID=owner.sortingLayerID;int order=owner.sortingOrder;
        foreach(var c in owner.GetComponentsInChildren<Canvas>(true))if(c!=layer&&c.sortingLayerID==layer.sortingLayerID)order=Mathf.Max(order,c.sortingOrder);
        layer.sortingOrder=Mathf.Min(32760,order+Mathf.Max(1,s.sortingOffset));root.gameObject.AddComponent<GraphicRaycaster>();
        var fx=root.gameObject.AddComponent<CardPlayPresentation>();fx.IsEnemy=isEnemy;fx.isActPresentation=BattleAreaLayout.IsSpell(data);if(fx.isActPresentation)activeActs++;
        var backdrop=Graphic<Image>(root,"Card play backdrop");backdrop.raycastTarget=true;var br=backdrop.rectTransform;br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;
        float fit=Mathf.Max(.01f,Mathf.Min(((RectTransform)owner.transform).rect.width/720f,((RectTransform)owner.transform).rect.height/760f));
        Vector2 reveal=new Vector2(Mathf.Max(1,s.revealSize.x),Mathf.Max(1,s.revealSize.y))*fit,center=(s.centerOffset+(isEnemy?Vector2.up*s.enemyCenterYOffset:Vector2.zero))*fit;
        Vector3 landing=area.position;Vector2 localSize=new Vector2(98,147);
        var layout=area.GetComponent<BattleAreaLayout>();if(layout!=null)layout.GetIncomingPlacement(replaced,out landing,out localSize);
        Camera camera=owner.renderMode==RenderMode.ScreenSpaceOverlay?null:owner.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,RectTransformUtility.WorldToScreenPoint(camera,landing),camera,out Vector2 target);
        Vector2 finalSize=new Vector2(root.InverseTransformVector(area.TransformVector(Vector3.right*localSize.x)).magnitude,root.InverseTransformVector(area.TransformVector(Vector3.up*localSize.y)).magnitude);
        bool isAct=BattleAreaLayout.IsSpell(data);
        if(isAct){target=center;finalSize=reveal;}
        var shadow=Graphic<TurnDrawFieldShadowGraphic>(root,"Play field shadow");shadow.maskable=false;
        var atmosphere=Graphic<CardPlayAtmosphereGraphic>(root,"Play atmosphere and circular ripple");atmosphere.rectTransform.anchoredPosition=center;atmosphere.maskable=false;fx.Atmosphere=atmosphere;
        var holder=Rect(root,"Play card");var halo=Graphic<TurnDrawHighlightGraphic>(holder,"Play edge glow");halo.surface=false;halo.maskable=false;
        fx.Artwork=Graphic<Image>(holder,"Play artwork");fx.Artwork.maskable=false;
        var surface=Graphic<TurnDrawHighlightGraphic>(holder,"Play surface highlight");surface.surface=true;surface.maskable=false;
        foreach(var graphic in new Graphic[]{halo,fx.Artwork,surface}){var r=graphic.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        var dealer=Object.FindFirstObjectByType<HandDealer>();Sprite back=dealer!=null?dealer.cardBackSprite:null;if(back==null)back=data.artwork;
        float elapsed=0;
        try
        {
            while(elapsed<s.TotalTime)
            {
                fx.Elapsed=elapsed;float rotate=s.RotateTime,arrival=rotate+s.HoldTime,total=s.TotalTime;
                float u=Mathf.Clamp01(elapsed/rotate),zoom=Mathf.Lerp(s.initialSizeRatio,1,Ease(u))+s.zoomOvershoot*Mathf.Sin(Mathf.PI*u),yaw=Mathf.PI*(1-Smooth(0,1,u));
                float travel=Smooth(arrival,total,elapsed),fade=1-travel;
                holder.anchoredPosition=Vector2.Lerp(center,target,travel);holder.sizeDelta=Vector2.Lerp(reveal*zoom,finalSize,travel);
                holder.localScale=new Vector3(elapsed<rotate?Mathf.Max(.018f,Mathf.Abs(Mathf.Cos(yaw))):1,1,1);holder.localRotation=Quaternion.Euler(0,0,elapsed<rotate?s.initialTilt*(1-Ease(u)):0);
                fx.Artwork.color=new Color(1,1,1,isAct?fade:1);
                fx.Artwork.sprite=elapsed<rotate&&Mathf.Cos(yaw)<0?back:data.artwork;
                float light=s.highlightStrength*Smooth(rotate*.08f,rotate*.84f,elapsed)*fade,sweep=Mathf.InverseLerp(rotate*.64f,rotate*1.34f,elapsed);
                halo.Configure(light*s.outerGlowStrength,s.glowSpread*fit,sweep,s.Highlight(isEnemy));surface.Configure(light,0,sweep,s.Highlight(isEnemy));
                atmosphere.Configure(s,reveal,elapsed,fit,isEnemy);
                Color dim=s.Backdrop(isEnemy);dim.a*=s.backdropOpacity*Smooth(0,rotate*.32f,elapsed)*fade;backdrop.color=dim;
                Vector2 footprint=holder.sizeDelta*new Vector2(Mathf.Max(.2f,holder.localScale.x)*.94f,.9f);shadow.rectTransform.sizeDelta=footprint+Vector2.one*s.shadowBlur*fit*5;shadow.rectTransform.anchoredPosition=holder.anchoredPosition+s.shadowOffset*fit;
                shadow.Configure(footprint,Mathf.Max(1,s.shadowBlur)*fit,s.shadowStrength*Smooth(0,rotate*.46f,elapsed)*fade,s.Shadow(isEnemy));
                yield return null;elapsed+=Time.unscaledDeltaTime*Mathf.Max(.05f,s.speed);
            }
        }
        finally{if(root!=null){root.gameObject.SetActive(false);Object.Destroy(root.gameObject);}}
    }
}
