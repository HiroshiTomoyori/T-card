using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
[DefaultExecutionOrder(500)]
public sealed class AttackSelectionPresentation : MonoBehaviour
{
    TurnManager owner;RectTransform root;Canvas canvas;AttackSelectionGraphic graphic;Image zone;Button zoneButton,confirm;Text confirmLabel;
    readonly List<Button> wallButtons=new List<Button>();
    readonly Vector4[] boxes=new Vector4[32],colors=new Vector4[32],flags=new Vector4[32];int count;float opacity;
    readonly Vector3[] corners=new Vector3[4];
    public float Opacity=>opacity;
    public AttackSelectionGraphic Graphic=>graphic;
    public Button WallZoneButton=>zoneButton;
    public Button ConfirmationButton=>confirm;
    public int EntryCount=>count;
    public int IndividualWallGlowCount {get;private set;}
    public bool WallZoneGlowVisible {get;private set;}
    public void Initialize(TurnManager turn){owner=turn;}
    static RectTransform Rect(Transform parent,string name){var g=new GameObject(name,typeof(RectTransform));g.layer=parent.gameObject.layer;g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);return r;}
    void EnsureRoot()
    {
        if(root!=null||owner==null||owner.playerBattleArea==null)return;
        var source=owner.playerBattleArea.GetComponentInParent<Canvas>();if(source==null)return;source=source.rootCanvas;
        root=Rect(source.transform,"Attack target selection");root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        canvas=root.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingLayerID=source.sortingLayerID;canvas.sortingOrder=source.sortingOrder+100;root.gameObject.AddComponent<GraphicRaycaster>();
        var surface=Rect(root,"Target glows and lowlight");surface.anchorMin=Vector2.zero;surface.anchorMax=Vector2.one;surface.offsetMin=surface.offsetMax=Vector2.zero;surface.gameObject.AddComponent<CanvasRenderer>();graphic=surface.gameObject.AddComponent<AttackSelectionGraphic>();
        var zr=Rect(root,"Monarch wall zone target");zone=zr.gameObject.AddComponent<Image>();zone.color=Color.clear;zoneButton=zr.gameObject.AddComponent<Button>();zoneButton.transition=Selectable.Transition.None;zoneButton.onClick.AddListener(()=>owner.BeginMonarchWallChoice());
        var cr=Rect(root,"Confirm selected walls");var image=cr.gameObject.AddComponent<Image>();image.color=new Color(.25f,.12f,.38f,.98f);confirm=cr.gameObject.AddComponent<Button>();confirm.targetGraphic=image;confirm.onClick.AddListener(()=>owner.ConfirmAttackSelection());
        var label=Rect(cr,"Label");label.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;label.offsetMin=new Vector2(10,4);label.offsetMax=new Vector2(-10,-4);confirmLabel=label.gameObject.AddComponent<Text>();confirmLabel.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");confirmLabel.fontSize=24;confirmLabel.color=Color.white;confirmLabel.alignment=TextAnchor.MiddleCenter;confirmLabel.raycastTarget=false;
    }
    Camera EventCamera{get{var c=root!=null?root.GetComponentInParent<Canvas>().rootCanvas:null;return c!=null&&c.renderMode!=RenderMode.ScreenSpaceOverlay?c.worldCamera:null;}}
    Rect Bounds(RectTransform rt)
    {
        rt.GetWorldCorners(corners);Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;
        foreach(var c in corners){var p=RectTransformUtility.WorldToScreenPoint(EventCamera,c);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return UnityEngine.Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    Rect CardBounds(CardController card){return Bounds(card.artworkImage!=null?card.artworkImage.rectTransform:(RectTransform)card.transform);}
    void Add(Rect bounds,Color tint,float power,bool keepLit,float highlight=0)
    {
        if(count>=32)return;var s=AttackSelectionSettings.Load();float scale=Screen.width/720f;bounds.center+=s.glowOffset*scale;bounds.size+=s.glowSizeAdjustment*scale;
        boxes[count]=new Vector4(bounds.center.x,bounds.center.y,bounds.width,bounds.height);colors[count]=new Vector4(tint.r,tint.g,tint.b,power*tint.a);flags[count]=new Vector4(keepLit?1:0,highlight,0,0);count++;
    }
    void Place(RectTransform rect,Rect screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen.min,EventCamera,out var a);RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen.max,EventCamera,out var b);rect.localPosition=(a+b)*.5f;rect.sizeDelta=b-a;
    }
    Button WallButton(int index)
    {
        while(wallButtons.Count<=index){var rt=Rect(root,"Individual wall attack target");var image=rt.gameObject.AddComponent<Image>();image.color=Color.clear;var b=rt.gameObject.AddComponent<Button>();b.transition=Selectable.Transition.None;wallButtons.Add(b);}return wallButtons[index];
    }
    public void Refresh()
    {
        EnsureRoot();if(root==null)return;var s=AttackSelectionSettings.Load();bool active=owner.AttackSelectionActive;bool confirming=owner.AttackExecutionConfirmationActive;
        opacity=Mathf.MoveTowards(opacity,active?1:0,s.fadeDuration<=0?1:Mathf.Min(Time.unscaledDeltaTime,.05f)/s.fadeDuration);
        count=0;IndividualWallGlowCount=0;WallZoneGlowVisible=false;int wallIndex=0;graphic.passThrough.Clear();bool wallsMode=owner.ChoosingMonarchWalls;
        float wave=s.minimumPulse+(1-s.minimumPulse)*(1-Mathf.Cos(Time.unscaledTime/Mathf.Max(.2f,s.pulsePeriod)*Mathf.PI*2))*.5f;
        var targetColor=owner.IsMonarchAttacker?s.monarchTargetColor:s.normalTargetColor;
        if(active)
        {
            var attacker=owner.GetCurrentAttacker().GetComponent<CardController>();Add(CardBounds(attacker),s.attackerColor,1,true,s.attackerHighlight);
            foreach(Transform t in owner.playerBattleArea)if(owner.CanChooseAttackCard(t.gameObject)){var c=t.GetComponent<CardController>();if(c!=null)graphic.passThrough.Add(CardBounds(c));}
            if(owner.enemyBattleArea!=null)foreach(Transform t in owner.enemyBattleArea)
            {
                var c=t.GetComponent<CardController>();if(c==null||c.data==null||!c.gameObject.activeInHierarchy)continue;
                if(owner.CanChooseEnemyAttackTarget(c)){var bounds=CardBounds(c);if(!confirming||owner.IsAttackConfirmationTarget(c.gameObject))Add(bounds,targetColor,wave,true);graphic.passThrough.Add(bounds);}
                else if(!confirming&&CardActionIcon.CanBlock(c))Add(CardBounds(c),Color.clear,0,true);
            }
            var walls=owner.LiveAttackWalls();Rect wallBounds=new Rect();bool first=true;
            bool individualWalls=!owner.IsMonarchAttacker||wallsMode||walls.Count==1;
            foreach(var w in walls)
            {
                var c=w.GetComponent<CardController>();var bounds=CardBounds(c);bool selected=owner.IsAttackWallSelected(w);
                if(individualWalls)
                {
                    var button=WallButton(wallIndex++);if(!button.gameObject.activeSelf)button.gameObject.SetActive(true);Place((RectTransform)button.transform,bounds);button.onClick.RemoveAllListeners();var targetWall=w;button.onClick.AddListener(()=>owner.SelectAttackTarget(targetWall));
                    if(!confirming||owner.IsAttackConfirmationTarget(w)){Add(bounds,selected?s.selectedWallColor:targetColor,selected?1.5f:wave,true,selected?.08f:0);IndividualWallGlowCount++;}graphic.passThrough.Add(bounds);
                }
                wallBounds=first?bounds:UnityEngine.Rect.MinMaxRect(Mathf.Min(wallBounds.xMin,bounds.xMin),Mathf.Min(wallBounds.yMin,bounds.yMin),Mathf.Max(wallBounds.xMax,bounds.xMax),Mathf.Max(wallBounds.yMax,bounds.yMax));first=false;
            }
            bool zoneActive=owner.IsMonarchAttacker&&walls.Count>1&&!wallsMode&&!confirming;
            if(zoneActive){float pad=s.wallZonePadding*Screen.width/720f;wallBounds.xMin-=pad;wallBounds.xMax+=pad;wallBounds.yMin-=pad;wallBounds.yMax+=pad;Add(wallBounds,targetColor,wave,true);WallZoneGlowVisible=true;Place(zone.rectTransform,wallBounds);}
            zone.gameObject.SetActive(zoneActive&&!wallsMode);
            if(owner.CanDirectAttack()&&!wallsMode)foreach(var direct in Object.FindObjectsByType<EnemyDirectClick>(FindObjectsSortMode.None)){if(direct.transform is RectTransform dr){var bounds=Bounds(dr);if(!confirming||owner.IsAttackConfirmationTarget(direct.gameObject))Add(bounds,targetColor,wave,true);graphic.passThrough.Add(bounds);}}
            bool pending=owner.HasPendingAttackConfirmation;
            confirm.gameObject.SetActive(pending||(wallsMode&&owner.RequiredAttackWallCount>1));
            confirm.interactable=pending||(owner.SelectedAttackWallCount==owner.RequiredAttackWallCount&&owner.RequiredAttackWallCount>0);
            confirmLabel.text=pending?owner.AttackConfirmationLabel:"BREAK "+owner.SelectedAttackWallCount+" / "+owner.RequiredAttackWallCount+"  —  CONFIRM";
        }
        else{zone.gameObject.SetActive(false);confirm.gameObject.SetActive(false);}
        // Keep live targets enabled across pointer-down/up frames; retire only unused buttons.
        for(int i=wallIndex;i<wallButtons.Count;i++)if(wallButtons[i].gameObject.activeSelf)wallButtons[i].gameObject.SetActive(false);
        float unit=root.rect.width/720f;confirm.GetComponent<RectTransform>().sizeDelta=s.confirmationSize*unit;confirm.GetComponent<RectTransform>().anchoredPosition=s.confirmationOffset*unit;
        graphic.Configure(owner,s,boxes,colors,flags,count,opacity);graphic.gameObject.SetActive(opacity>.0001f);
    }
    void LateUpdate(){Refresh();}
    void OnDisable(){opacity=0;if(root!=null)root.gameObject.SetActive(false);}
    void OnEnable(){if(root!=null)root.gameObject.SetActive(true);}
    void OnDestroy(){if(root!=null)Destroy(root.gameObject);}
}
