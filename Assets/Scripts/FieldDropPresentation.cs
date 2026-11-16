using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[DisallowMultipleComponent]
public sealed class FieldDropPresentation : MonoBehaviour
{
    static FieldDropPresentation current;
    BattleDropZone owner;HandDealer dealer;TurnManager manager;RectTransform root;
    Image target;FieldHighlightGraphic glow,raiseGlow,raiseKingGlow;CardDrag drag;Vector2 pointer;Camera pointerCamera;
    float opacity,start,targetAlpha,transitionTime=1,clock;bool activeDrag;
    internal static readonly Color RaiseViolet = new Color(.41f, .095f, .84f);
    internal const float RaiseKingGlowStrength = .60f;
    static readonly Color RaiseRim = new Color(.78f, .43f, 1f);
    public Rect ScreenRectangle {get;private set;}
    public float Opacity=>opacity;
    public bool IsTargeted=>targetAlpha>0;
    public RectTransform TargetRect=>root;
    public FieldHighlightGraphic Glow=>glow;
    public void Initialize(BattleDropZone zone){owner=zone;current=this;}
    public static void UpdateDrag(CardDrag card,Vector2 point,Camera camera)
    {
        if(current==null)current=Object.FindFirstObjectByType<FieldDropPresentation>();
        if(current==null)return;current.drag=card;current.pointer=point;current.pointerCamera=camera;current.activeDrag=true;current.RefreshTarget();
    }
    public static void EndDrag(){if(current!=null){current.activeDrag=false;current.drag=null;current.SetTarget(false);if(current.target!=null)current.target.raycastTarget=false;current.HideRaiseGlows();}}
    internal static void ForwardFieldCardDrop(Transform area, PointerEventData data)
    {
        if (current != null && current.owner != null && current.owner.battleArea == area)
            current.owner.OnDrop(data);
    }
    public bool Contains(Vector2 screenPoint){RefreshGeometry();return root!=null&&ScreenRectangle.Contains(screenPoint);}
    void Cache()
    {
        if(owner==null)owner=GetComponent<BattleDropZone>();
        if(owner!=null&&owner.battleArea==null)owner.battleArea=owner.transform;
        if(dealer==null)dealer=Object.FindFirstObjectByType<HandDealer>();if(manager==null)manager=Object.FindFirstObjectByType<TurnManager>();
    }
    void EnsureRoot()
    {
        if(root!=null)return;Cache();if(owner==null||owner.battleArea==null)return;
        Transform parent=owner.battleArea.parent;if(parent==null)return;
        if(dealer!=null)while(parent.parent!=null&&dealer.handArea!=null&&!dealer.handArea.IsChildOf(parent))parent=parent.parent;
        var go=new GameObject("Field drop highlight and target",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(FieldDropForwarder));go.layer=gameObject.layer;go.transform.SetParent(parent,false);
        root=(RectTransform)go.transform;root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);
        target=go.GetComponent<Image>();target.color=Color.clear;target.raycastTarget=false;target.maskable=false;go.GetComponent<FieldDropForwarder>().owner=owner;
        var visual=new GameObject("Soft field glow (behind cards)",typeof(RectTransform),typeof(CanvasRenderer),typeof(FieldHighlightGraphic));visual.layer=gameObject.layer;visual.transform.SetParent(root,false);
        glow=visual.GetComponent<FieldHighlightGraphic>();glow.raycastTarget=false;glow.maskable=false;
        int index=parent.childCount-1;
        var branches=dealer!=null?new Transform[]{owner.battleArea,dealer.handArea,dealer.enemyHandArea,dealer.wallArea,dealer.enemyWallArea,dealer.deckPosition,dealer.enemyDeckPosition,manager!=null?manager.enemyBattleArea:null}:new[]{owner.battleArea};
        foreach(var child in branches)
        {
            if(child==null||!child.IsChildOf(parent))continue;var branch=child;while(branch.parent!=parent&&branch.parent!=null)branch=branch.parent;index=Mathf.Min(index,branch.GetSiblingIndex());
        }
        root.SetSiblingIndex(index);
    }
    static Rect Bounds(RectTransform rect,Camera camera)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;
        foreach(var p in corners){Vector2 v=RectTransformUtility.WorldToScreenPoint(camera,p);min=Vector2.Min(min,v);max=Vector2.Max(max,v);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    public void RefreshGeometry()
    {
        Cache();EnsureRoot();if(root==null)return;var s=FieldHighlightSettings.Load();
        var canvas=owner.GetComponentInParent<Canvas>();var camera=canvas!=null&&canvas.rootCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.rootCanvas.worldCamera:null;
        float factor=Screen.width/720f;Rect player=Bounds(owner.battleArea as RectTransform,camera);
        float top=player.yMax,bottom=player.yMin;
        if(manager!=null&&manager.enemyBattleArea is RectTransform enemy)top=Bounds(enemy,camera).yMin-s.enemyFieldGap*factor;
        if(dealer!=null&&dealer.wallArea is RectTransform wall)
        {
            float wallTop=Bounds(wall,camera).yMax;
            foreach(var card in wall.GetComponentsInChildren<CardController>())
            {
                var group=card.GetComponentInParent<CanvasGroup>();if(group!=null&&group.alpha<=.01f)continue;
                var art=card.artworkImage!=null?card.artworkImage.rectTransform:card.transform as RectTransform;
                if(art!=null&&art.gameObject.activeInHierarchy)wallTop=Mathf.Max(wallTop,Bounds(art,camera).yMax);
            }
            bottom=wallTop+s.wallClearance*factor;
        }
        if(top<bottom+20*factor)top=Mathf.Max(player.yMax,bottom+80*factor);
        float margin=Mathf.Clamp(s.screenSideMargin*factor,0,Screen.width*.45f);
        var size=new Vector2(Mathf.Max(20,Screen.width-2*margin+s.sizeAdjustment.x*factor),Mathf.Max(20,top-bottom+s.sizeAdjustment.y*factor));
        var center=new Vector2(Screen.width*.5f,(top+bottom)*.5f)+s.positionOffset*factor;
        ScreenRectangle=new Rect(center-size*.5f,size);
        var parent=root.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,ScreenRectangle.min,camera,out Vector2 lower);RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,ScreenRectangle.max,camera,out Vector2 upper);
        root.localPosition=(lower+upper)*.5f;root.sizeDelta=upper-lower;root.localScale=Vector3.one;root.localRotation=Quaternion.identity;
        float units=root.rect.width/Mathf.Max(1,ScreenRectangle.width)*factor;
        float pad=(s.outwardSpread+s.blur*4+30)*units;glow.rectTransform.sizeDelta=root.sizeDelta+Vector2.one*pad*2;
        glow.Show(s,root.rect.size,units,opacity,clock);glow.enabled=opacity>.0001f;
    }
    void SetTarget(bool value)
    {
        float next=value?1:0;if(next==targetAlpha)return;start=opacity;targetAlpha=next;transitionTime=0;
    }
    void RefreshTarget()
    {
        RefreshGeometry();bool allowed=activeDrag&&drag!=null&&drag.IsDragging&&manager!=null&&manager.CanDragPlayerHand;
        bool playable=allowed&&owner.CanHighlightCard(drag.GetComponent<CardController>());
        if(target!=null)target.raycastTarget=playable;
        bool inside=playable&&ScreenRectangle.Contains(pointer);
        var draggedCard = playable ? drag.GetComponent<CardController>() : null;
        var raiseTarget = playable ? owner.FindRaiseTarget(draggedCard, pointer, pointerCamera) : null;
        RefreshRaiseGlows(raiseTarget, draggedCard);
        SetTarget(inside && raiseTarget == null && owner.CanPlayNormally(drag.GetComponent<CardController>()) && FieldHighlightSettings.Load().highlightEnabled);
    }
    void HideRaiseGlows()
    {
        if (raiseGlow != null) raiseGlow.enabled = false;
        if (raiseKingGlow != null) raiseKingGlow.enabled = false;
    }

    void RefreshRaiseGlows(CardController targetCard, CardController king)
    {
        if (targetCard == null || king == null || root == null)
        {
            HideRaiseGlows();
            return;
        }

        var settings = FieldHighlightSettings.Load();
        float unit = root.rect.width / Mathf.Max(1, ScreenRectangle.width) * Screen.width / 720f * .42f;
        float pulseTime = Time.unscaledTime;
        ShowRaiseGlow(ref raiseGlow, targetCard, "Raise target violet glow", settings, unit, pulseTime, false);
        ShowRaiseGlow(ref raiseKingGlow, king, "Dragged King violet glow", settings, unit, pulseTime, true);
    }

    void ShowRaiseGlow(ref FieldHighlightGraphic graphic, CardController card, string name,
        FieldHighlightSettings settings, float unit, float pulseTime, bool followsDraggedCard)
    {
        var artwork = card.artworkImage != null ? card.artworkImage.rectTransform : card.transform as RectTransform;
        if (artwork == null)
        {
            if (graphic != null) graphic.enabled = false;
            return;
        }
        if (graphic == null)
        {
            var visual = new GameObject(name, typeof(RectTransform), typeof(FieldHighlightGraphic));
            visual.layer = root.gameObject.layer;
            graphic = visual.GetComponent<FieldHighlightGraphic>();
        }

        var rect = graphic.rectTransform;
        RectTransform parent = followsDraggedCard ? artwork.parent as RectTransform : root;
        if (parent == null) parent = root;
        if (rect.parent != parent) rect.SetParent(parent, false);
        if (parent != root)
        {
            int artworkIndex = artwork.GetSiblingIndex();
            if (rect.GetSiblingIndex() > artworkIndex) rect.SetSiblingIndex(artworkIndex);
        }
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);

        var area = Bounds(artwork, pointerCamera);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, area.min, pointerCamera, out Vector2 a);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, area.max, pointerCamera, out Vector2 b);
        float localUnit = unit * root.TransformVector(Vector3.right).magnitude /
            Mathf.Max(.001f, parent.TransformVector(Vector3.right).magnitude);
        rect.localPosition = (a + b) * .5f;
        rect.sizeDelta = b - a + Vector2.one * (settings.outwardSpread + settings.blur * 4 + 30) * localUnit * 2f;
        float strength = followsDraggedCard ? RaiseKingGlowStrength : 1f;
        graphic.Show(settings, b - a, localUnit, strength, pulseTime, RaiseViolet, RaiseRim, 1.36f);
        graphic.enabled = true;
    }

    void LateUpdate()
    {
        RefreshTarget();var settings=FieldHighlightSettings.Load();float duration=targetAlpha>0?settings.fadeIn:settings.fadeOut;
        float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);transitionTime+=dt;opacity=Mathf.Lerp(start,targetAlpha,duration<=0?1:Mathf.SmoothStep(0,1,Mathf.Clamp01(transitionTime/duration)));
        if(opacity>0)clock+=dt;RefreshGeometry();
    }
    void OnDisable(){activeDrag=false;drag=null;HideRaiseGlows();opacity=start=targetAlpha=0;if(root!=null)root.gameObject.SetActive(false);}
    void OnEnable(){if(root!=null)root.gameObject.SetActive(true);}
    void OnDestroy(){if(raiseKingGlow!=null)Destroy(raiseKingGlow.gameObject);if(root!=null)Destroy(root.gameObject);if(current==this)current=null;}
}
