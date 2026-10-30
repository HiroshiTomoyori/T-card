using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
[DefaultExecutionOrder(300),DisallowMultipleComponent]
public sealed class CardReadinessVisual:MonoBehaviour
{
    CardController card;CardReadinessIconGraphic icon;bool restInitialized,lastTapped;float lastRestDarkness;Image lastArtwork;readonly Vector3[] corners=new Vector3[4];
    bool preparationLanded,preparingObserved;float preparationStarted=-1;PresentationSequenceLock.Lease preparationLock;
    public bool IconVisible=>icon!=null&&icon.gameObject.activeInHierarchy;
    public bool PreparationLanded=>preparationLanded;
    public float PreparationBlend
    {
        get{if(card==null||!card.hasSummonSickness||!preparationLanded)return 0;float duration=CardReadinessSettings.Load().preparationTransitionDuration;return duration<=0?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.unscaledTime-preparationStarted)/duration));}
    }
    public bool IsPreparationTransitioning=>preparationLanded&&PreparationBlend<1;
    public float PreparationTimeRemaining=>Mathf.Max(0,CardReadinessSettings.Load().preparationTransitionDuration-(Time.unscaledTime-preparationStarted));
    public void ResetPreparationTransition(){ReleasePreparationLock();preparationLanded=false;preparationStarted=-1;preparingObserved=card!=null&&card.hasSummonSickness;if(preparingObserved&&isActiveAndEnabled)preparationLock=PresentationSequenceLock.Acquire(this);Hide();}
    void ReleasePreparationLock(){preparationLock?.Dispose();preparationLock=null;}
    public void ClearPreparationTransition(){ReleasePreparationLock();preparationLanded=false;preparationStarted=-1;preparingObserved=false;Hide();}
    public void NotifyFieldLanded()
    {
        if(card==null)card=GetComponent<CardController>();
        if(card==null||!card.hasSummonSickness||preparationLanded||transform.parent==null||transform.parent.GetComponent<BattleAreaLayout>()==null)return;
        preparationLanded=true;preparingObserved=true;preparationStarted=Time.unscaledTime;if(preparationLock==null)preparationLock=PresentationSequenceLock.Acquire(this);preparationLock.PauseGame();transform.parent.GetComponent<BattleAreaLayout>().MarkDirty();
    }
    void Awake(){card=GetComponent<CardController>();}
    public void RefreshVisual()
    {
        if(card==null)return;
        if(card.hasSummonSickness&&(card.PlayedFromReverse||!CardController.CanReceiveSummonSickness(card.data)))card.SetSummonSickness(false);
        var layout=transform.parent!=null?transform.parent.GetComponent<BattleAreaLayout>():null;
        if(layout==null){restInitialized=false;ClearPreparationTransition();return;}
        if(card.hasSummonSickness&&!preparingObserved)ResetPreparationTransition();
        if(!card.hasSummonSickness)ClearPreparationTransition();
        if(preparationLanded&&PreparationBlend>=1)ReleasePreparationLock();
        if(card.hasSummonSickness&&!preparationLanded)
        {
            var drag=GetComponent<CardDrag>();
            if((drag==null||!drag.IsDropSnapPlaying)&&!CardPlayPresentation.IsPlaying&&!DOTween.IsTweening(transform))NotifyFieldLanded();
        }
        float darkness=CardReadinessSettings.Load().restDarkness;
        if(!restInitialized||lastTapped!=card.isTapped||lastRestDarkness!=darkness||lastArtwork!=card.artworkImage){card.RefreshStateVisuals();restInitialized=true;lastTapped=card.isTapped;lastRestDarkness=darkness;lastArtwork=card.artworkImage;}
        if(!card.hasSummonSickness||!preparationLanded||PreparationBlend<=0||layout==null||card.artworkImage==null||!card.artworkImage.gameObject.activeInHierarchy){Hide();return;}
        if(icon==null){var go=new GameObject("Deployment preparation",typeof(RectTransform),typeof(CanvasRenderer));go.layer=gameObject.layer;go.transform.SetParent(transform,false);icon=go.AddComponent<CardReadinessIconGraphic>();icon.raycastTarget=false;}
        var source=card.artworkImage.rectTransform;source.GetWorldCorners(corners);var topLeft=transform.InverseTransformPoint(corners[1]);var topRight=transform.InverseTransformPoint(corners[2]);float width=(topRight-topLeft).magnitude;
        var settings=CardReadinessSettings.Load();float scale=width/110f;var rt=icon.rectTransform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);rt.localRotation=Quaternion.identity;rt.localScale=Vector3.one;rt.sizeDelta=Vector2.one*settings.iconSize*scale;rt.localPosition=topRight+(Vector3)(settings.iconOffset*scale);rt.SetAsLastSibling();
        icon.Configure(settings,PreparationBlend);icon.gameObject.SetActive(true);
    }
    void LateUpdate(){RefreshVisual();}
    void Hide(){if(icon!=null)icon.gameObject.SetActive(false);}
    void OnDisable(){ClearPreparationTransition();}
    void OnDestroy(){ReleasePreparationLock();if(icon!=null)Destroy(icon.gameObject);}
}
public sealed class CardReadinessIconGraphic:MaskableGraphic
{
    CardReadinessSettings settings;float opacity=1;
    public void Configure(CardReadinessSettings s,float transitionOpacity=1){settings=s;opacity=Mathf.Clamp01(transitionOpacity);raycastTarget=false;maskable=false;SetVerticesDirty();}
    void Quad(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint){var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;int i=vh.currentVertCount;vh.AddVert(a+n,tint,Vector2.zero);vh.AddVert(b+n,tint,Vector2.zero);vh.AddVert(b-n,tint,Vector2.zero);vh.AddVert(a-n,tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
    void Disc(VertexHelper vh,float radius,Color tint){int i=vh.currentVertCount;vh.AddVert(Vector2.zero,tint,Vector2.zero);for(int n=0;n<=40;n++){float a=n*Mathf.PI*2/40;vh.AddVert(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,tint,Vector2.zero);if(n>0)vh.AddTriangle(i,i+n,i+n+1);}}
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();if(settings==null)return;float r=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;var tint=settings.iconColor;tint.a*=opacity;var background=settings.iconBackground;background.a*=opacity;Disc(vh,r,tint);Disc(vh,r*.91f,background);
        float unit=r/15f,line=Mathf.Max(.7f,1.25f*unit);Vector2 tl=new Vector2(-5,7)*unit,tr=new Vector2(5,7)*unit,bl=new Vector2(-5,-7)*unit,br=new Vector2(5,-7)*unit;
        Quad(vh,tl,tr,line,tint);Quad(vh,bl,br,line,tint);Quad(vh,tl,new Vector2(-4,3)*unit,line,tint);Quad(vh,tr,new Vector2(4,3)*unit,line,tint);Quad(vh,new Vector2(-4,3)*unit,new Vector2(4,-3)*unit,line,tint);Quad(vh,new Vector2(4,3)*unit,new Vector2(-4,-3)*unit,line,tint);Quad(vh,new Vector2(-4,-3)*unit,bl,line,tint);Quad(vh,new Vector2(4,-3)*unit,br,line,tint);
        tint.a*=.8f+.2f*(1-Mathf.Cos(Time.unscaledTime/Mathf.Max(.2f,settings.pulsePeriod)*Mathf.PI*2))*.5f;Quad(vh,new Vector2(-2.8f,-5)*unit,new Vector2(2.8f,-5)*unit,2*unit,tint);
    }
}
