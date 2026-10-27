using UnityEngine;
using UnityEngine.UI;
[DisallowMultipleComponent]
public sealed class TurnDrawHighlightEffect : MonoBehaviour
{
    HandDealer dealer;
    RectTransform artwork;
    float shadowVisibility;
    public TurnDrawFieldShadowGraphic FieldShadow {get;private set;}
    public TurnDrawHighlightGraphic Halo {get;private set;}
    public TurnDrawHighlightGraphic Surface {get;private set;}
    public float Elapsed {get;private set;}
    public float Amount {get;private set;}
    public void Initialize(HandDealer owner)
    {
        dealer=owner;var controller=GetComponent<CardController>();
        var art=controller!=null?controller.artworkImage:null;
        var anchor=art!=null?art.rectTransform:GetComponent<RectTransform>();
        artwork=anchor;
        Halo=Create(anchor,"Draw outer highlight",false);Surface=Create(anchor,"Draw surface highlight",true);CreateFieldShadow();SetTime(0);
    }
    TurnDrawHighlightGraphic Create(RectTransform anchor,string label,bool surface)
    {
        var go=new GameObject(label,typeof(RectTransform),typeof(CanvasRenderer),typeof(TurnDrawHighlightGraphic));go.layer=gameObject.layer;
        go.transform.SetParent(anchor,false);var rt=(RectTransform)go.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
        var graphic=go.GetComponent<TurnDrawHighlightGraphic>();graphic.surface=surface;graphic.raycastTarget=false;graphic.maskable=false;return graphic;
    }
    public void SetTime(float time)
    {
        Elapsed=time;if(dealer==null)return;
        float total=Mathf.Max(.01f,dealer.turnDrawTravelTime)+Mathf.Max(.01f,dealer.turnDrawFlipTime)+Mathf.Max(0,dealer.turnDrawRevealHoldTime)+Mathf.Max(.01f,dealer.turnDrawHandTime);
        shadowVisibility=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,Mathf.Max(.01f,dealer.turnDrawTravelTime),time))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(total-Mathf.Max(.01f,dealer.turnDrawHandTime),total,time)));
        float travel=Mathf.Max(.01f,dealer.turnDrawTravelTime),flip=Mathf.Max(.01f,dealer.turnDrawFlipTime),hold=Mathf.Max(0,dealer.turnDrawRevealHoldTime),hand=Mathf.Max(.01f,dealer.turnDrawHandTime);
        float start=travel+flip*.5f,rise=travel+flip*1.05f,fade=travel+flip+hold*.84f,end=travel+flip+hold+hand*.8f;
        Amount=dealer.turnDrawHighlightStrength*Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,rise,time))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(fade,end,time)));
        float sweep=Mathf.InverseLerp(travel+flip*.68f,travel+flip+hold*.82f,time);
        if(Halo!=null)Halo.Configure(Amount*dealer.turnDrawOuterGlowStrength,dealer.turnDrawGlowSpread,sweep,dealer.turnDrawHighlightColor);
        if(Surface!=null)Surface.Configure(Amount,0,sweep,dealer.turnDrawHighlightColor);
        UpdateFieldShadow();
    }
    void CreateFieldShadow()
    {
        if(dealer==null || artwork==null)return;
        Transform parent=dealer.wallArea!=null?dealer.wallArea.parent:transform.parent;
        while(parent!=null && ((dealer.handArea!=null&&!dealer.handArea.IsChildOf(parent))||(dealer.deckPosition!=null&&!dealer.deckPosition.IsChildOf(parent))))parent=parent.parent;
        if(parent==null)parent=transform.parent;if(parent==null)return;
        var go=new GameObject("Turn draw field shadow",typeof(RectTransform),typeof(CanvasRenderer),typeof(TurnDrawFieldShadowGraphic));go.layer=gameObject.layer;go.transform.SetParent(parent,false);
        FieldShadow=go.GetComponent<TurnDrawFieldShadowGraphic>();FieldShadow.raycastTarget=false;FieldShadow.maskable=false;
        int before=parent.childCount-1;
        foreach(var target in new Transform[]{dealer.wallArea,dealer.enemyWallArea,dealer.handArea,dealer.enemyHandArea,dealer.deckPosition,dealer.enemyDeckPosition})
        {
            if(target==null || !target.IsChildOf(parent))continue;
            Transform branch=target;while(branch.parent!=parent && branch.parent!=null)branch=branch.parent;before=Mathf.Min(before,branch.GetSiblingIndex());
        }
        go.transform.SetSiblingIndex(before);
    }
    void UpdateFieldShadow()
    {
        if(FieldShadow==null || artwork==null || dealer==null)return;
        var rt=FieldShadow.rectTransform;var parent=rt.parent;
        Vector3 width=parent.InverseTransformVector(artwork.TransformVector(Vector3.right*artwork.rect.width));
        Vector3 height=parent.InverseTransformVector(artwork.TransformVector(Vector3.up*artwork.rect.height));
        Vector2 footprint=new Vector2(Mathf.Max(1,width.magnitude*.94f),Mathf.Max(1,height.magnitude*.9f));
        rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);rt.localScale=Vector3.one;rt.localRotation=Quaternion.identity;
        rt.localPosition=parent.InverseTransformPoint(artwork.TransformPoint(artwork.rect.center))+(Vector3)dealer.turnDrawFieldShadowOffset;
        float blur=Mathf.Max(1,dealer.turnDrawFieldShadowBlur);rt.sizeDelta=footprint+Vector2.one*blur*5;
        FieldShadow.Configure(footprint,blur,Mathf.Clamp01(dealer.turnDrawFieldShadowStrength)*shadowVisibility,dealer.turnDrawFieldShadowColor);
    }
    void LateUpdate(){UpdateFieldShadow();}
    void OnDisable(){if(FieldShadow!=null)FieldShadow.gameObject.SetActive(false);}
    void OnDestroy(){if(FieldShadow!=null){FieldShadow.gameObject.SetActive(false);if(Application.isPlaying)Destroy(FieldShadow.gameObject);else DestroyImmediate(FieldShadow.gameObject);}}
}
