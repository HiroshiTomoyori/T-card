using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
[DefaultExecutionOrder(500)]
public sealed class AttackSelectionPresentation : MonoBehaviour
{
    TurnManager owner;RectTransform root;Canvas canvas;AttackSelectionGraphic graphic;Image zone;Button zoneButton;ConfirmationWindow confirmationWindow;
    readonly List<Button> wallButtons=new List<Button>();
    readonly List<RawImage> wallOrnaments=new List<RawImage>(2);
    RectTransform ornamentLayer;
    Texture2D wallOrnamentArt;
    Material wallOrnamentMaterial;
    readonly Vector4[] boxes=new Vector4[32],colors=new Vector4[32],flags=new Vector4[32];int count,retiringCount;float opacity;
    readonly Vector3[] corners=new Vector3[4];
    readonly List<Rect> confirmationObstacles = new List<Rect>(5);
    ConfirmationSwordOverlay swordOverlay;
    public float Opacity=>opacity;
    public AttackSelectionGraphic Graphic=>graphic;
    public Button WallZoneButton=>zoneButton;
    public Button ConfirmationButton=>confirmationWindow?.Accept;
    public int EntryCount=>count-retiringCount;
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
        ornamentLayer = Rect(root, "Monarch wall selection ornaments");
        ornamentLayer.anchorMin = Vector2.zero; ornamentLayer.anchorMax = Vector2.one;
        ornamentLayer.offsetMin = ornamentLayer.offsetMax = Vector2.zero;
        wallOrnamentArt = Resources.Load<Texture2D>("TCardField/MonarchWallOrnament");
        var ornamentShader = Resources.Load<Shader>("TCardField/MonarchWallOrnamentGlow");
        if (ornamentShader != null && ornamentShader.isSupported)
            wallOrnamentMaterial = new Material(ornamentShader) { hideFlags = HideFlags.HideAndDontSave };
        swordOverlay = new ConfirmationSwordOverlay(Rect(root, "Attack confirmation swords"));
        var zr=Rect(root,"Monarch wall zone target");zone=zr.gameObject.AddComponent<Image>();zone.color=Color.clear;zoneButton=zr.gameObject.AddComponent<Button>();zoneButton.transition=Selectable.Transition.None;zoneButton.onClick.AddListener(()=>owner.BeginMonarchWallChoice());
        confirmationWindow = new ConfirmationWindow(root, "Confirm attack choice", "Confirm Attack",
            "Return to attack selection", "Back", owner.CancelAttackSelection,
            "Confirm selected walls", "Attack", owner.ConfirmAttackSelection);
        confirmationWindow.Rect.gameObject.SetActive(false);
    }
    Camera EventCamera{get{var c=root!=null?root.GetComponentInParent<Canvas>().rootCanvas:null;return c!=null&&c.renderMode!=RenderMode.ScreenSpaceOverlay?c.worldCamera:null;}}
    Rect Bounds(RectTransform rt)
    {
        rt.GetWorldCorners(corners);Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;
        foreach(var c in corners){var p=RectTransformUtility.WorldToScreenPoint(EventCamera,c);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return UnityEngine.Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    Rect CardBounds(CardController card){return Bounds(card.artworkImage!=null?card.artworkImage.rectTransform:(RectTransform)card.transform);}
    void Add(Rect bounds,Color tint,float power,bool keepLit,float highlight=0,bool wallZone=false,float weight=1)
    {
        if(count>=32||weight<=0)return;var s=AttackSelectionSettings.Load();float scale=Screen.width/720f;bounds.center+=s.glowOffset*scale;bounds.size+=s.glowSizeAdjustment*scale;
        if(weight<1)retiringCount++;
        boxes[count]=new Vector4(bounds.center.x,bounds.center.y,bounds.width,bounds.height);colors[count]=new Vector4(tint.r,tint.g,tint.b,power*tint.a*weight);flags[count]=new Vector4(keepLit?weight:0,highlight*weight,wallZone?1:0,0);count++;
    }
    void Place(RectTransform rect,Rect screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen.min,EventCamera,out var a);RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen.max,EventCamera,out var b);rect.localPosition=(a+b)*.5f;rect.sizeDelta=b-a;
    }

    void ShowConfirmation(AttackSelectionSettings settings, Rect attackerBounds)
    {
        Rect safeArea = Screen.safeArea;
        confirmationObstacles.Clear();
        confirmationObstacles.Add(attackerBounds);
        int swordCount = 0;

        foreach (var target in owner.AttackConfirmationTargets)
        {
            if (target == null || !target.activeInHierarchy || !(target.transform is RectTransform targetRect)) continue;
            var card = target.GetComponent<CardController>();
            Rect bounds = card != null ? CardBounds(card) : Bounds(targetRect);
            confirmationObstacles.Add(bounds);
            if (card == null) continue;
            bool fieldCard = card.transform.parent == owner.enemyBattleArea;
            if (swordOverlay.Show(swordCount, bounds, fieldCard, owner.AttackConfirmationElapsed, settings, EventCamera, out var swordBounds))
            {
                swordCount++;
                confirmationObstacles.Add(swordBounds);
            }
        }

        swordOverlay.HideFrom(swordCount);
        float unit = Screen.width / 720f;
        var theme = ConfirmationTheme.Load();
        Vector2 size = theme.WindowSize * unit;
        Vector2 preferred = Bounds(root).center + (settings.confirmationOffset + theme.AttackOffset(owner.AttackConfirmationType)) * unit;
        Place(confirmationWindow.Rect, confirmationWindow.FindPlacement(safeArea, size, preferred,
            settings.confirmationClearance * unit, confirmationObstacles, theme.avoidCardsAndSwords));
        confirmationWindow.Message.text = owner.AttackConfirmationLabel;
        confirmationWindow.Layout();
        confirmationWindow.Rect.gameObject.SetActive(true);
    }

    Button WallButton(int index)
    {
        while(wallButtons.Count<=index){var rt=Rect(root,"Individual wall attack target");var image=rt.gameObject.AddComponent<Image>();image.color=Color.clear;var b=rt.gameObject.AddComponent<Button>();b.transition=Selectable.Transition.None;wallButtons.Add(b);}return wallButtons[index];
    }
    void ShowWallOrnament(int index, Rect bounds, AttackSelectionSettings settings, float breath)
    {
        if (wallOrnamentArt == null) return;
        while (wallOrnaments.Count <= index)
        {
            var image = Rect(ornamentLayer, "Selected wall filigree").gameObject.AddComponent<RawImage>();
            image.texture = wallOrnamentArt;
            image.raycastTarget = false;
            image.material = wallOrnamentMaterial;
            wallOrnaments.Add(image);
        }
        var mark = wallOrnaments[index];
        Vector2 center = bounds.center;
        bounds.size *= settings.monarchWallOrnamentScale;
        bounds.center = center;
        bounds = Expand(bounds, settings.monarchWallOrnamentGap * Screen.width / 720f);
        Place(mark.rectTransform, bounds);
        Color tint = Color.white;
        tint.a = Mathf.Clamp01(settings.monarchWallOrnamentTint.a * opacity * settings.monarchWallOrnamentStrength);
        if (wallOrnamentMaterial != null)
        {
            wallOrnamentMaterial.SetFloat("_Breath", breath);
            // White is neutral. Keep existing Inspector tint adjustments relative to the approved purple.
            wallOrnamentMaterial.SetColor("_InkTint", new Color(settings.monarchWallOrnamentTint.r / .86f,
                settings.monarchWallOrnamentTint.g / .68f, settings.monarchWallOrnamentTint.b));
        }
        mark.color = tint;
        if (!mark.gameObject.activeSelf) mark.gameObject.SetActive(true);
    }
    public void Refresh()
    {
        EnsureRoot();if(root==null)return;var s=AttackSelectionSettings.Load();bool active=owner.AttackSelectionActive;bool confirming=owner.AttackExecutionConfirmationActive;
        float frameTime = Mathf.Min(Time.unscaledDeltaTime,.05f);
        opacity=Mathf.MoveTowards(opacity,active?1:0,s.fadeDuration<=0?1:frameTime/s.fadeDuration);
        // Selection is already dimmed. As the sword fades in, blend its existing
        // glows and lowlight cutouts into the narrower confirmation focus.
        float confirmation = active&&confirming ? (s.fadeDuration<=0 ? 1 : Mathf.Clamp01((owner.AttackConfirmationElapsed+frameTime)/s.fadeDuration)) : 0;
        float SelectionWeight(GameObject target) => !confirming||owner.IsAttackConfirmationTarget(target) ? 1 : 1-confirmation;
        swordOverlay.Opacity = opacity*confirmation;
        count=retiringCount=0;IndividualWallGlowCount=0;WallZoneGlowVisible=false;int wallIndex=0;graphic.passThrough.Clear();bool wallsMode=owner.ChoosingMonarchWalls;
        int ornamentCount = 0;
        float breath = .5f - .5f * Mathf.Cos(Time.unscaledTime / Mathf.Max(.2f, s.pulsePeriod) * Mathf.PI * 2f);
        float wave = Mathf.Lerp(s.minimumPulse, 1f, breath);
        var targetColor=owner.IsMonarchAttacker?s.monarchTargetColor:s.normalTargetColor;
        if(active)
        {
            var attacker=owner.GetCurrentAttacker().GetComponent<CardController>();var attackerBounds=CardBounds(attacker);Add(attackerBounds,s.attackerColor,1,true,s.attackerHighlight);
            foreach(Transform t in owner.playerBattleArea)if(owner.CanChooseAttackCard(t.gameObject)){var c=t.GetComponent<CardController>();if(c!=null)graphic.passThrough.Add(CardBounds(c));}
            if(owner.enemyBattleArea!=null)foreach(Transform t in owner.enemyBattleArea)
            {
                var c=t.GetComponent<CardController>();if(c==null||c.data==null||!c.gameObject.activeInHierarchy)continue;
                if(owner.CanChooseEnemyAttackTarget(c)){var bounds=CardBounds(c);float highlight=owner.IsAttackConfirmationTarget(c.gameObject)?s.attackerHighlight*confirmation:0;Add(bounds,targetColor,wave,true,highlight,weight:SelectionWeight(c.gameObject));graphic.passThrough.Add(bounds);}
                else if (confirmation < 1f && CardActionIcon.CanBlock(c))
                    c.GetComponent<CardGuardBadge>()?.FocusForAttack(canvas, opacity * (1f - confirmation));
            }
            var walls=owner.LiveAttackWalls();Rect wallBounds=new Rect();bool first=true;
            bool individualWalls=!owner.IsMonarchAttacker||wallsMode||walls.Count==1;
            foreach(var w in walls)
            {
                var c=w.GetComponent<CardController>();var bounds=CardBounds(c);bool selected=owner.IsAttackWallSelected(w);
                if(individualWalls)
                {
                    var button=WallButton(wallIndex++);if(!button.gameObject.activeSelf)button.gameObject.SetActive(true);Place((RectTransform)button.transform,bounds);button.onClick.RemoveAllListeners();var targetWall=w;button.onClick.AddListener(()=>owner.SelectAttackTarget(targetWall));
                    bool adorned = owner.IsMonarchAttacker && (selected || owner.IsAttackConfirmationTarget(w));
                    // The filigree identifies the selection. Keep the card's artwork
                    // clear of any pale fill or competing bright outline underneath it.
                    float highlight = !adorned && owner.IsAttackConfirmationTarget(w) ? s.attackerHighlight * confirmation : 0f;
                    Add(bounds, targetColor, adorned ? wave * .4f : wave, true, highlight, weight:SelectionWeight(w));
                    if (adorned) ShowWallOrnament(ornamentCount++, bounds, s, breath);
                    if(!confirming||owner.IsAttackConfirmationTarget(w))IndividualWallGlowCount++;
                    graphic.passThrough.Add(bounds);
                }
                wallBounds=first?bounds:UnityEngine.Rect.MinMaxRect(Mathf.Min(wallBounds.xMin,bounds.xMin),Mathf.Min(wallBounds.yMin,bounds.yMin),Mathf.Max(wallBounds.xMax,bounds.xMax),Mathf.Max(wallBounds.yMax,bounds.yMax));first=false;
            }
            bool zoneAvailable=owner.IsMonarchAttacker&&walls.Count>1&&!wallsMode;
            if (zoneAvailable)
            {
                float unit = Screen.width / 720f;
                wallBounds = Expand(wallBounds, s.wallZonePadding * unit);
                Place(zone.rectTransform, wallBounds);
                Rect haloBounds = Expand(wallBounds, s.wallZoneHighlightPadding * unit);
                float strength = Mathf.Lerp(1f - s.wallZoneBreathDepth, 1f, breath);
                Add(haloBounds, targetColor, strength, true, wallZone: true, weight: 1f - confirmation);
                WallZoneGlowVisible = !confirming;
            }
            zone.gameObject.SetActive(zoneAvailable&&!confirming);
            if(owner.CanDirectAttack()&&!wallsMode)foreach(var direct in Object.FindObjectsByType<EnemyDirectClick>(FindObjectsSortMode.None)){if(direct.transform is RectTransform dr){var bounds=Bounds(dr);Add(bounds,targetColor,wave,true,weight:SelectionWeight(direct.gameObject));graphic.passThrough.Add(bounds);}}
            if (confirming) ShowConfirmation(s, attackerBounds);
        }
        else zone.gameObject.SetActive(false);
        if (!active || !confirming)
        {
            confirmationWindow.Rect.gameObject.SetActive(false);
            swordOverlay.HideFrom(0);
        }
        // Keep live targets enabled across pointer-down/up frames; retire only unused buttons.
        for(int i=wallIndex;i<wallButtons.Count;i++)if(wallButtons[i].gameObject.activeSelf)wallButtons[i].gameObject.SetActive(false);
        for (int i = ornamentCount; i < wallOrnaments.Count; i++)
            if (wallOrnaments[i].gameObject.activeSelf) wallOrnaments[i].gameObject.SetActive(false);
        graphic.Configure(owner,s,boxes,colors,flags,count,opacity,confirmation:confirmation);graphic.gameObject.SetActive(opacity>.0001f);
    }
    static Rect Expand(Rect bounds, float padding)
    {
        var margin = Vector2.one * padding;
        return new Rect(bounds.position - margin, bounds.size + margin * 2f);
    }
    void LateUpdate(){Refresh();}
    void OnDisable(){opacity=0;if(root!=null)root.gameObject.SetActive(false);}
    void OnEnable(){if(root!=null)root.gameObject.SetActive(true);}
    void OnDestroy()
    {
        swordOverlay?.Dispose();
        if (wallOrnamentMaterial != null) Destroy(wallOrnamentMaterial);
        if (root != null) Destroy(root.gameObject);
    }
}
