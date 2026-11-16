using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
[DefaultExecutionOrder(510)]
public sealed class DefenseSelectionPresentation:MonoBehaviour
{
    TurnManager owner;RectTransform root;AttackSelectionGraphic graphic;Button pass;ConfirmationWindow confirmationWindow;
    readonly List<Button> guards=new List<Button>();
    ConfirmationSwordOverlay swordOverlay;
    CardController rippleAttacker;
    float rippleStartedAt;
    readonly List<Rect> confirmationObstacles = new List<Rect>(6);
    readonly Vector4[] guardBadgeCutouts = new Vector4[32];
    readonly Vector4[] defenseGuardArtwork = new Vector4[32];
    readonly Vector4[] boxes=new Vector4[32],colors=new Vector4[32],flags=new Vector4[32];readonly Vector3[] corners=new Vector3[4];int count;float opacity,confirmationShade;
    public Button PassButton=>pass;public Button ConfirmButton=>confirmationWindow?.Accept;public Button BackButton=>confirmationWindow?.Back;public AttackSelectionGraphic Graphic=>graphic;
    public int HighlightedGuardCount{get;private set;}public int HighlightedWallCount{get;private set;}public Color CurrentTargetColor{get;private set;}public Color CurrentAttackerColor{get;private set;}
    public void Initialize(TurnManager turn){owner=turn;}
    static RectTransform Rect(Transform parent,string name){var g=new GameObject(name,typeof(RectTransform));g.layer=parent.gameObject.layer;g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);return r;}
    void EnsureRoot()
    {
        if(root!=null||owner==null||owner.playerBattleArea==null)return;var source=owner.playerBattleArea.GetComponentInParent<Canvas>();if(source==null)return;source=source.rootCanvas;
        root=Rect(source.transform,"Defense target selection");root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        var canvas=root.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingLayerID=source.sortingLayerID;canvas.sortingOrder=source.sortingOrder+110;root.gameObject.AddComponent<GraphicRaycaster>();
        var surface=Rect(root,"Defense glows and lowlight");surface.anchorMin=Vector2.zero;surface.anchorMax=Vector2.one;surface.offsetMin=surface.offsetMax=Vector2.zero;graphic=surface.gameObject.AddComponent<AttackSelectionGraphic>();
        swordOverlay = new ConfirmationSwordOverlay(Rect(root, "Incoming attack swords"));
        pass = ConfirmationWindow.CreateChoice(root, "Pass incoming attack", "Let Attack Through", true, owner.OnNoBlockButtonClicked);
        confirmationWindow = new ConfirmationWindow(root, "Confirm defense choice", "Confirm Defense",
            "Return to defense selection", "Back", owner.CancelDefenseChoice,
            "Confirm defense action", "Confirm", owner.ConfirmDefenseChoice);
        pass.gameObject.SetActive(false);
        confirmationWindow.Rect.gameObject.SetActive(false);
    }
    Camera EventCamera{get{var c=root.GetComponentInParent<Canvas>().rootCanvas;return c.renderMode==RenderMode.ScreenSpaceOverlay?null:c.worldCamera;}}
    Rect Bounds(RectTransform rt){rt.GetWorldCorners(corners);Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;foreach(var c in corners){var p=RectTransformUtility.WorldToScreenPoint(EventCamera,c);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return UnityEngine.Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
    Rect CardBounds(CardController c)=>Bounds(c.artworkImage!=null?c.artworkImage.rectTransform:(RectTransform)c.transform);
    void Place(RectTransform rt,Rect screen){RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen.min,EventCamera,out var a);RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen.max,EventCamera,out var b);rt.localPosition=(a+b)*.5f;rt.sizeDelta=b-a;}
    void Add(Rect bounds, Color color, float power, float highlight, CardController card = null, bool useDefenseGlow = false)
    {
        if (count >= boxes.Length) return;
        bool includeBadge = card != null && CardActionIcon.IsGuard(card.data);
        if (includeBadge)
        {
            var badge = CardActionGlowSettings.Load();
            float size = bounds.width * badge.IdleGuardBadgeSize;
            Vector2 center = bounds.min + Vector2.Scale(bounds.size, badge.guardBadgeInset) + Vector2.one * (size * .5f);
            guardBadgeCutouts[count] = new Vector4(center.x, center.y, size, badge.guardIdleGlowSpread);
        }
        var settings = AttackSelectionSettings.Load();
        if (useDefenseGlow)
            defenseGuardArtwork[count] = new Vector4(bounds.center.x, bounds.center.y, bounds.width, bounds.height);
        float unit = Screen.width / 720f;
        bounds.center += settings.glowOffset * unit;
        bounds.size += settings.glowSizeAdjustment * unit;
        boxes[count] = new Vector4(bounds.center.x, bounds.center.y, bounds.width, bounds.height);
        colors[count] = new Vector4(color.r, color.g, color.b, power * color.a);
        flags[count] = new Vector4(1f, highlight, 0f, useDefenseGlow ? 2f : includeBadge ? 1f : 0f);
        count++;
    }
    Button Guard(int index){while(guards.Count<=index){var rt=Rect(root,"Choose defender");var im=rt.gameObject.AddComponent<Image>();im.color=Color.clear;var b=rt.gameObject.AddComponent<Button>();b.transition=Selectable.Transition.None;guards.Add(b);}return guards[index];}
    void RefreshSwords(AttackSelectionSettings settings, bool visible, List<GameObject> walls)
    {
        int count = 0;
        if (visible)
        {
            var target = owner.IncomingFieldTarget;
            float elapsed = owner.IncomingSwordElapsed;
            var camera = EventCamera;
            if (target != null)
            {
                if (target.gameObject.activeInHierarchy)
                {
                    Rect bounds = CardBounds(target);
                    confirmationObstacles.Add(bounds);
                    if (swordOverlay.Show(count, bounds, true, elapsed, settings, camera, out var swordBounds))
                    {
                        confirmationObstacles.Add(swordBounds);
                        count++;
                    }
                }
            }
            else
            {
                foreach (var wall in walls)
                {
                    var card = wall.GetComponent<CardController>();
                    Rect bounds = CardBounds(card);
                    confirmationObstacles.Add(bounds);
                    if (swordOverlay.Show(count, bounds, false, elapsed, settings, camera, out var swordBounds))
                    {
                        confirmationObstacles.Add(swordBounds);
                        count++;
                    }
                }
            }
        }
        swordOverlay.HideFrom(count);
    }

    public void Refresh()
    {
        EnsureRoot();if(root==null)return;var s=DefenseSelectionSettings.Load();var shared=AttackSelectionSettings.Load();bool active=owner.DefenseSelectionActive,pending=owner.HasPendingDefenseConfirmation;count=0;HighlightedGuardCount=HighlightedWallCount=0;graphic.passThrough.Clear();
        bool swordsVisible = owner.IncomingSwordVisible;
        var wallTargets = active || swordsVisible ? owner.IncomingWallTargets() : null;
        bool incomingCardPreview = swordsVisible && (owner.IncomingFieldTarget != null || wallTargets.Count > 0);
        bool showLowlight = active || incomingCardPreview;
        opacity=Mathf.MoveTowards(opacity,showLowlight?1:0,shared.fadeDuration<=0?1:Mathf.Min(Time.unscaledDeltaTime,.05f)/shared.fadeDuration);
        confirmationShade = Mathf.MoveTowards(confirmationShade, active && pending ? 1f : 0f,
            shared.fadeDuration <= 0f ? 1f : Mathf.Min(Time.unscaledDeltaTime, .05f) / shared.fadeDuration);
        swordOverlay.Opacity = opacity;
        confirmationObstacles.Clear();
        RefreshSwords(shared, swordsVisible, wallTargets);
        float wave=shared.minimumPulse+(1-shared.minimumPulse)*(1-Mathf.Cos(Time.unscaledTime/Mathf.Max(.2f,shared.pulsePeriod)*Mathf.PI*2))*.5f;
        CurrentTargetColor=owner.IncomingMonarchAttack?shared.monarchTargetColor:shared.normalTargetColor;CurrentAttackerColor=s.incomingRippleColor;int index=0;
        if(showLowlight)
        {
            Add(CardBounds(owner.IncomingAttacker), CurrentAttackerColor, 1f, shared.attackerHighlight, owner.IncomingAttacker);
            if((!pending||owner.SelectedDefenseBlocker==null)&&owner.IncomingFieldTarget!=null)
                Add(CardBounds(owner.IncomingFieldTarget), CurrentTargetColor, wave, shared.attackerHighlight, owner.IncomingFieldTarget);
            if (!pending || owner.SelectedDefenseBlocker == null)
            {
                foreach(var wall in wallTargets){var card=wall.GetComponent<CardController>();Add(CardBounds(card),CurrentTargetColor,wave,shared.attackerHighlight);HighlightedWallCount++;}
                if(owner.IncomingFieldTarget==null&&wallTargets.Count==0&&owner.playerBattleArea is RectTransform direct)Add(Bounds(direct),CurrentTargetColor,wave,shared.attackerHighlight);
            }
            // Light the available guards during the sword preview as well.
            // Their buttons still wait until the defense decision opens.
            foreach (Transform child in owner.playerBattleArea)
            {
                var c=child.GetComponent<CardController>();if(!owner.IsEligibleDefenseGuard(c))continue;
                var bounds=CardBounds(c);bool selected=owner.SelectedDefenseBlocker==c;
                if (!pending || selected)
                {
                    Add(bounds, Color.clear, 0f, selected ? shared.attackerHighlight : 0f, c, useDefenseGlow: true);
                    HighlightedGuardCount++;
                }
                if(!active||pending)continue;
                var b=Guard(index++);b.gameObject.SetActive(true);Place((RectTransform)b.transform,bounds);b.onClick.RemoveAllListeners();var blocker=c;b.onClick.AddListener(()=>owner.SelectBlocker(blocker.gameObject));
            }
        }
        for(int i=index;i<guards.Count;i++)guards[i].gameObject.SetActive(false);
        float scale = Screen.width / 720f;
        var theme = ConfirmationTheme.Load();
        Rect safe = Screen.safeArea;
        bool showPass = active && !pending;
        if (showPass) PlacePassButton(s, scale, safe);
        pass.gameObject.SetActive(showPass);
        pass.interactable = showPass;
        var panel = confirmationWindow.Rect;
        if (active && pending)
        {
            if (owner.IncomingAttacker != null) confirmationObstacles.Add(CardBounds(owner.IncomingAttacker));
            if (owner.SelectedDefenseBlocker != null) confirmationObstacles.Add(CardBounds(owner.SelectedDefenseBlocker));
            var offsets = owner.SelectedDefenseBlocker != null ? theme.blockConfirmation : theme.passConfirmation;
            Vector2 caseOffset = offsets?.For(owner.IncomingFieldTarget != null, wallTargets.Count > 0, owner.IncomingMonarchAttack) ?? Vector2.zero;
            Vector2 preferred = ConfirmationAnchor() + (s.passButtonOffset + s.confirmationOffset + caseOffset) * scale;
            Place(panel, confirmationWindow.FindPlacement(safe, theme.WindowSize * scale, preferred,
                shared.confirmationClearance * scale, confirmationObstacles, theme.avoidCardsAndSwords));
            confirmationWindow.Message.text = owner.DefenseConfirmationLabel;
            confirmationWindow.Layout();
        }
        panel.gameObject.SetActive(active && pending);
        graphic.Configure(owner,shared,boxes,colors,flags,count,opacity,active,()=>{if(owner.HasPendingDefenseConfirmation)owner.CancelDefenseChoice();},confirmationShade);graphic.gameObject.SetActive(opacity>.0001f);
        if (showLowlight)
        {
            graphic.ConfigureGuardBadgeCutouts(guardBadgeCutouts);
            graphic.ConfigureDefenseGuardGlow(defenseGuardArtwork, CardActionGlowSettings.Load());
        }
        RefreshIncomingRipple(s, showLowlight && !pending);
        // Confirmation and pass controls must stay above the full-screen input shield.
        pass.transform.SetAsLastSibling();panel.SetAsLastSibling();
    }
    void PlacePassButton(DefenseSelectionSettings settings, float scale, Rect safe)
    {
        Vector2 size = Vector2.Min(settings.passButtonSize * scale, safe.size);
        Vector2 anchor = new Vector2(safe.center.x, safe.yMin + safe.height * .22f);
        if (owner.playerWallArea is RectTransform wall)
        {
            var layout = wall.GetComponent<WallAreaLayout>();
            if (layout != null)
                anchor = RectTransformUtility.WorldToScreenPoint(EventCamera, layout.PresentationBottomCenter);
            else
            {
                Rect bounds = Bounds(wall);
                anchor = new Vector2(bounds.center.x, bounds.yMin);
            }
        }
        Vector2 center = anchor - Vector2.up * (settings.passWallGap * scale + size.y * .5f);
        center.x = Mathf.Clamp(center.x, safe.xMin + size.x * .5f, safe.xMax - size.x * .5f);
        center.y = Mathf.Clamp(center.y, safe.yMin + size.y * .5f, safe.yMax - size.y * .5f);
        Place((RectTransform)pass.transform, new Rect(center - size * .5f, size));
    }
    Vector2 ConfirmationAnchor()
    {
        if (!(owner.handDealer?.handArea is RectTransform hand))
            return new Vector2(Screen.width * .5f, Screen.height * .12f);
        Rect bounds = Bounds(hand);
        float top = float.NegativeInfinity;
        foreach (var card in hand.GetComponentsInChildren<CardController>())
        {
            var group = card.GetComponent<CanvasGroup>();
            if (card.gameObject.activeInHierarchy && (group == null || group.alpha > .01f))
                top = Mathf.Max(top, CardBounds(card).yMax);
        }
        return new Vector2(bounds.center.x, float.IsNegativeInfinity(top) ? Mathf.Min(bounds.yMax, Screen.height * .12f) : top);
    }
    void LateUpdate(){Refresh();}
    void RefreshIncomingRipple(DefenseSelectionSettings settings, bool visible)
    {
        var attacker = visible ? owner.IncomingAttacker : null;
        if (attacker != rippleAttacker)
        {
            rippleAttacker = attacker;
            rippleStartedAt = Time.unscaledTime;
        }
        if (attacker == null || !attacker.gameObject.activeInHierarchy)
        {
            graphic.ConfigureIncomingRipple(default, Color.clear, 0f, 0f, 1f);
            return;
        }

        float elapsed = Time.unscaledTime - rippleStartedAt;
        float phase = Mathf.Repeat(elapsed, Mathf.Max(1.3f, settings.incomingRipplePeriod));
        graphic.ConfigureIncomingRipple(CardBounds(attacker), settings.incomingRippleColor,
            phase, settings.incomingRippleStrength * opacity, settings.incomingRippleSpread);
    }
    void OnDisable(){opacity=confirmationShade=0;rippleAttacker=null;if(root!=null)root.gameObject.SetActive(false);}
    void OnEnable(){if(root!=null)root.gameObject.SetActive(true);}
    void OnDestroy(){swordOverlay?.Dispose();if(root!=null)Destroy(root.gameObject);}
}
