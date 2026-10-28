using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
[DefaultExecutionOrder(510)]
public sealed class DefenseSelectionPresentation:MonoBehaviour
{
    TurnManager owner;RectTransform root,panel;AttackSelectionGraphic graphic;Font font;bool ownsFont;Button pass,yes,back;Text question;
    readonly List<Button> guards=new List<Button>();
    readonly Vector4[] boxes=new Vector4[32],colors=new Vector4[32],flags=new Vector4[32];readonly Vector3[] corners=new Vector3[4];int count;float opacity;
    public Button PassButton=>pass;public Button ConfirmButton=>yes;public Button BackButton=>back;public AttackSelectionGraphic Graphic=>graphic;
    public int HighlightedGuardCount{get;private set;}public int HighlightedWallCount{get;private set;}public Color CurrentTargetColor{get;private set;}public Color CurrentAttackerColor{get;private set;}
    public void Initialize(TurnManager turn){owner=turn;}
    static RectTransform Rect(Transform parent,string name){var g=new GameObject(name,typeof(RectTransform));g.layer=parent.gameObject.layer;g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);return r;}
    Text Label(RectTransform parent,string text){var rt=Rect(parent,"Label");rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(8,4);rt.offsetMax=new Vector2(-8,-4);var t=rt.gameObject.AddComponent<Text>();t.font=font;t.fontSize=25;t.alignment=TextAnchor.MiddleCenter;t.text=text;t.raycastTarget=false;return t;}
    Button Button(RectTransform parent,string name,string text,System.Action click){var rt=Rect(parent,name);var image=rt.gameObject.AddComponent<Image>();var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;button.onClick.AddListener(()=>click());Label(rt,text);return button;}
    void EnsureRoot()
    {
        if(root!=null||owner==null||owner.playerBattleArea==null)return;var source=owner.playerBattleArea.GetComponentInParent<Canvas>();if(source==null)return;source=source.rootCanvas;
        root=Rect(source.transform,"Defense target selection");root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        var canvas=root.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingLayerID=source.sortingLayerID;canvas.sortingOrder=source.sortingOrder+110;root.gameObject.AddComponent<GraphicRaycaster>();
        font=Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic","Meiryo","Noto Sans CJK JP","Arial"},25);ownsFont=font!=null;if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var surface=Rect(root,"Defense glows and lowlight");surface.anchorMin=Vector2.zero;surface.anchorMax=Vector2.one;surface.offsetMin=surface.offsetMax=Vector2.zero;graphic=surface.gameObject.AddComponent<AttackSelectionGraphic>();
        pass=Button(root,"Pass incoming attack","攻撃を通す",()=>owner.OnNoBlockButtonClicked());
        panel=Rect(root,"Confirm defense choice");panel.gameObject.AddComponent<Image>();
        var title=Rect(panel,"Question");question=Label(title,"");title.anchoredPosition=new Vector2(0,38);title.sizeDelta=new Vector2(320,62);
        back=Button(panel,"Return to defense selection","戻る",()=>owner.CancelDefenseChoice());yes=Button(panel,"Confirm defense action","実行する",()=>owner.ConfirmDefenseChoice());
        var br=(RectTransform)back.transform;br.anchoredPosition=new Vector2(-80,-34);br.sizeDelta=new Vector2(140,58);var yr=(RectTransform)yes.transform;yr.anchoredPosition=new Vector2(80,-34);yr.sizeDelta=new Vector2(140,58);
    }
    Camera EventCamera{get{var c=root.GetComponentInParent<Canvas>().rootCanvas;return c.renderMode==RenderMode.ScreenSpaceOverlay?null:c.worldCamera;}}
    Rect Bounds(RectTransform rt){rt.GetWorldCorners(corners);Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;foreach(var c in corners){var p=RectTransformUtility.WorldToScreenPoint(EventCamera,c);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return UnityEngine.Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
    Rect CardBounds(CardController c)=>Bounds(c.artworkImage!=null?c.artworkImage.rectTransform:(RectTransform)c.transform);
    void Place(RectTransform rt,Rect screen){RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen.min,EventCamera,out var a);RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen.max,EventCamera,out var b);rt.localPosition=(a+b)*.5f;rt.sizeDelta=b-a;}
    void Add(Rect bounds,Color color,float power,float highlight){if(count>=32)return;var s=AttackSelectionSettings.Load();float unit=Screen.width/720f;bounds.center+=s.glowOffset*unit;bounds.size+=s.glowSizeAdjustment*unit;boxes[count]=new Vector4(bounds.center.x,bounds.center.y,bounds.width,bounds.height);colors[count]=new Vector4(color.r,color.g,color.b,power*color.a);flags[count]=new Vector4(1,highlight,0,0);count++;}
    Button Guard(int index){while(guards.Count<=index){var rt=Rect(root,"Choose defender");var im=rt.gameObject.AddComponent<Image>();im.color=Color.clear;var b=rt.gameObject.AddComponent<Button>();b.transition=Selectable.Transition.None;guards.Add(b);}return guards[index];}
    public void Refresh()
    {
        EnsureRoot();if(root==null)return;var s=DefenseSelectionSettings.Load();var shared=AttackSelectionSettings.Load();bool active=owner.DefenseSelectionActive,pending=owner.HasPendingDefenseConfirmation;count=0;HighlightedGuardCount=HighlightedWallCount=0;graphic.passThrough.Clear();
        opacity=Mathf.MoveTowards(opacity,active?1:0,shared.fadeDuration<=0?1:Mathf.Min(Time.unscaledDeltaTime,.05f)/shared.fadeDuration);
        float wave=shared.minimumPulse+(1-shared.minimumPulse)*(1-Mathf.Cos(Time.unscaledTime/Mathf.Max(.2f,shared.pulsePeriod)*Mathf.PI*2))*.5f;
        CurrentTargetColor=owner.IncomingMonarchAttack?shared.monarchTargetColor:shared.normalTargetColor;CurrentAttackerColor=shared.attackerColor;int index=0;
        if(active)
        {
            Add(CardBounds(owner.IncomingAttacker),shared.attackerColor,1,shared.attackerHighlight);
            if((!pending||owner.SelectedDefenseBlocker==null)&&owner.IncomingFieldTarget!=null)Add(CardBounds(owner.IncomingFieldTarget),CurrentTargetColor,wave,shared.attackerHighlight);
            foreach(var wall in (!pending||owner.SelectedDefenseBlocker==null)?owner.IncomingWallTargets():new List<GameObject>()){var card=wall.GetComponent<CardController>();Add(CardBounds(card),CurrentTargetColor,wave,shared.attackerHighlight);HighlightedWallCount++;}
            if((!pending||owner.SelectedDefenseBlocker==null)&&owner.IncomingFieldTarget==null&&owner.IncomingWallTargets().Count==0&&owner.playerBattleArea is RectTransform direct)Add(Bounds(direct),CurrentTargetColor,wave,shared.attackerHighlight);
            foreach(Transform child in owner.playerBattleArea){var c=child.GetComponent<CardController>();if(!owner.CanChooseDefenseCard(c))continue;bool selected=owner.SelectedDefenseBlocker==c;if(!pending||selected){Add(CardBounds(c),shared.attackerColor,wave*(selected?1.5f:1),shared.attackerHighlight);HighlightedGuardCount++;}
                var b=Guard(index++);b.gameObject.SetActive(!pending);Place((RectTransform)b.transform,CardBounds(c));b.onClick.RemoveAllListeners();var blocker=c;b.onClick.AddListener(()=>owner.SelectBlocker(blocker.gameObject));}
        }
        for(int i=index;i<guards.Count;i++)guards[i].gameObject.SetActive(false);
        float scale=Screen.width/720f;Vector2 anchor=new Vector2(Screen.width*.5f,Screen.height*.12f);
        if(owner.handDealer!=null&&owner.handDealer.handArea is RectTransform hand)
        {
            var bounds=Bounds(hand);float top=float.NegativeInfinity;
            foreach(var card in hand.GetComponentsInChildren<CardController>())if(card.gameObject.activeInHierarchy){var cg=card.GetComponent<CanvasGroup>();if(cg==null||cg.alpha>.01f)top=Mathf.Max(top,CardBounds(card).yMax);}
            anchor=new Vector2(bounds.center.x,float.IsNegativeInfinity(top)?Mathf.Min(bounds.yMax,Screen.height*.12f):top);
        }
        Vector2 passPoint=anchor+s.passButtonOffset*scale;Vector2 passSize=s.passButtonSize*scale;passPoint.x=Mathf.Clamp(passPoint.x,passSize.x*.5f,Screen.width-passSize.x*.5f);passPoint.y=Mathf.Clamp(passPoint.y,passSize.y*.5f,Screen.height-passSize.y*.5f);
        Place((RectTransform)pass.transform,new Rect(passPoint-passSize*.5f,passSize));pass.gameObject.SetActive(active&&!pending);pass.interactable=active&&!pending;
        Vector2 panelSize=s.confirmationSize*scale,panelPoint=passPoint+s.confirmationOffset*scale;panelPoint.x=Mathf.Clamp(panelPoint.x,panelSize.x*.5f,Screen.width-panelSize.x*.5f);panelPoint.y=Mathf.Clamp(panelPoint.y,panelSize.y*.5f,Screen.height-panelSize.y*.5f);Place(panel,new Rect(panelPoint-panelSize*.5f,panelSize));
        // Scale panel contents with its adjustable dimensions.
        question.rectTransform.parent.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,panel.rect.height*.25f);question.rectTransform.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(panel.rect.width-20,panel.rect.height*.4f);
        foreach(var b in new[]{back,yes}){var r=(RectTransform)b.transform;r.sizeDelta=new Vector2(panel.rect.width*.41f,panel.rect.height*.37f);r.anchoredPosition=new Vector2((b==back?-1:1)*panel.rect.width*.235f,-panel.rect.height*.22f);}
        panel.gameObject.SetActive(active&&pending);question.text=owner.DefenseConfirmationLabel;
        foreach(var image in new[]{pass.GetComponent<Image>(),panel.GetComponent<Image>(),back.GetComponent<Image>(),yes.GetComponent<Image>()})image.color=s.buttonColor;
        foreach(var t in root.GetComponentsInChildren<Text>(true))t.color=s.buttonTextColor;
        graphic.Configure(owner,shared,boxes,colors,flags,count,opacity,active,()=>{if(owner.HasPendingDefenseConfirmation)owner.CancelDefenseChoice();});graphic.gameObject.SetActive(opacity>.0001f);
        // Confirmation and pass controls must stay above the full-screen input shield.
        pass.transform.SetAsLastSibling();panel.SetAsLastSibling();
    }
    void LateUpdate(){Refresh();}
    void OnDisable(){opacity=0;if(root!=null)root.gameObject.SetActive(false);}
    void OnEnable(){if(root!=null)root.gameObject.SetActive(true);}
    void OnDestroy(){if(root!=null)Destroy(root.gameObject);if(ownsFont&&font!=null)Destroy(font);}
}
