using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[InitializeOnLoad]
public static class FieldHighlightDiagnostics
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardFieldHighlightValidation");
    static bool started;static double deadline;
    static FieldHighlightSettings savedAsset;static string savedJson;static bool skip;
    static FieldHighlightDiagnostics(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/T-card/Validate Field Highlight (Play)")]
    static void Begin(){FieldHighlightSettingsEditor.Ensure();SessionState.SetBool("TCardFieldProbe",true);started=false;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(!SessionState.GetBool("TCardFieldProbe",false)||started||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
        if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL: initial hand timeout");return;}
        var dealer=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(dealer==null||!dealer.IsOpeningDecisionReady)return;
        started=true;dealer.StartCoroutine(Probe(dealer));
    }
    static void Check(bool value,string text){if(!value)throw new Exception(text);}
    static void Finish(string text)
    {
        if(savedAsset!=null&&!string.IsNullOrEmpty(savedJson))EditorJsonUtility.FromJsonOverwrite(savedJson,savedAsset);
        typeof(TitleManager).GetProperty("SkipActionConfirmations").SetValue(null,skip);
        Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),text);Debug.Log(text);SessionState.SetBool("TCardFieldProbe",false);EditorApplication.isPlaying=false;
    }
    static int Count(Transform area)=>area.GetComponentsInChildren<CardController>().Count(c=>c.transform.parent==area&&c.data!=null);
    static Texture2D Capture(string name){var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name+".png"),image.EncodeToPNG());return image;}
    static void Set(TurnManager turn,string name,bool value){typeof(TurnManager).GetField(name,Flags).SetValue(turn,value);}
    static CardController Add(HandDealer dealer,Transform area,CardData data)
    {
        var go=UnityEngine.Object.Instantiate(dealer.proHandCardPrefab!=null?dealer.proHandCardPrefab:dealer.cardPrefab,area);
        var card=go.GetComponent<CardController>();card.SetData(data);return card;
    }
    static float VisualWidth(CardController card)
    {
        var rect=card.artworkImage!=null?card.artworkImage.rectTransform:card.transform as RectTransform;var corners=new Vector3[4];rect.GetWorldCorners(corners);return Vector3.Distance(corners[0],corners[3]);
    }
    static IEnumerator Probe(HandDealer dealer)
    {
        Directory.CreateDirectory(Folder);savedAsset=FieldHighlightSettings.Load();savedJson=EditorJsonUtility.ToJson(savedAsset);skip=TitleManager.SkipActionConfirmations;
        var turn=dealer.turnManager;dealer.turnManager=null;typeof(HandDealer).GetMethod("CompleteOpeningSelection",Flags).Invoke(dealer,null);dealer.turnManager=turn;
        yield return new WaitForSeconds(.5f);
        var data=UnityEngine.Object.Instantiate(dealer.handArea.GetComponentsInChildren<CardController>().First(c=>c.data!=null).data);data.cardName="2";data.power=2;data.effectTypes=new EffectType[0];
        string failure=null;
        foreach(var area in new[]{turn.playerBattleArea,turn.enemyBattleArea})
        {
            var layout=area.GetComponent<BattleAreaLayout>();var cards=new List<CardController>();cards.Add(Add(dealer,area,data));yield return null;layout.Refresh();layout.PlaceImmediately(cards[0]);float one=VisualWidth(cards[0]);
            for(int i=0;i<3;i++)cards.Add(Add(dealer,area,data));yield return null;layout.Refresh();foreach(var c in cards)layout.PlaceImmediately(c);
            try{Check(Mathf.Abs(one-VisualWidth(cards[0]))<.1f,"1-card and 4-card sizes differ on "+area.name);var original5=layout.layouts[4];var configured5=(BattleAreaLayout.LayoutSetting)typeof(BattleAreaLayout).GetMethod("GetSetting",Flags).Invoke(layout,new object[]{5});Check(configured5.cardWidth==original5.cardWidth&&configured5.gap==original5.gap,"Five-card configuration changed");}
            catch(Exception e){failure=e.ToString();break;}
        }
        if(failure!=null){Finish("FAIL: "+failure);yield break;}
        Set(turn,"isPlayerTurnActive",true);Set(turn,"isPlayerMainPhase",true);Set(turn,"isBattlePhase",false);Set(turn,"isEndingTurn",false);
        typeof(TitleManager).GetProperty("SkipActionConfirmations").SetValue(null,true);
        var hand=dealer.handArea.GetComponent<HandController>();hand.ChangeState(false);yield return new WaitForSeconds(.4f);
        var zone=turn.playerBattleArea.GetComponent<BattleDropZone>();var view=zone.FieldPresentation;zone.useCostCheck=false;
        var source=dealer.handArea.GetComponentsInChildren<CardController>().First(c=>c.data!=null);source.SetData(data);var drag=source.GetComponent<CardDrag>();
        var ev=new PointerEventData(EventSystem.current){pointerDrag=source.gameObject,position=RectTransformUtility.WorldToScreenPoint(null,source.transform.position)};
        drag.OnBeginDrag(ev);view.RefreshGeometry();Rect bounds=view.ScreenRectangle;
        try{Check(drag.IsDragging&&turn.CanDragPlayerHand,"Could not begin normal hand drag");Check(bounds.width>Screen.width*.85f,"Drop field not expanded toward screen edges");Check(!view.Contains(RectTransformUtility.WorldToScreenPoint(null,turn.enemyBattleArea.position)),"Player target overlaps enemy field center");Check(view.Glow.GetComponent<Canvas>()==null&&!view.Glow.raycastTarget,"Highlight overrides card layer/input");Check(!ShaderUtil.ShaderHasError(Shader.Find("T-card/UI/Field Drop Glow")),"Field shader failed");}
        catch(Exception e){Finish("FAIL: "+e);yield break;}
        // Validate the real hover path at the affordability boundary without spending resources.
        var eligibilityResource=UnityEngine.Object.FindFirstObjectByType<ResourceManager>();int savedResource=eligibilityResource.currentResource;
        zone.useCostCheck=true;eligibilityResource.currentResource=1;
        ev.position=bounds.center;drag.OnDrag(ev);yield return new WaitForSecondsRealtime(savedAsset.fadeOut+.1f);
        try{Check(!zone.CanHighlightCard(source)&&!view.IsTargeted&&view.Opacity<.001f,"Unaffordable card triggered highlight");Check(!view.TargetRect.GetComponent<Image>().raycastTarget,"Unaffordable card enabled expanded drop target");}
        catch(Exception e){failure=e.ToString();}
        eligibilityResource.currentResource=2;drag.OnDrag(ev);yield return null;
        try{Check(zone.CanHighlightCard(source)&&view.IsTargeted,"Affordable card did not trigger highlight");}
        catch(Exception e){failure=e.ToString();}
        // Capacity still blocks creatures, while spells and discounted K use the same drop rules.
        var fieldLayout=turn.playerBattleArea.GetComponent<BattleAreaLayout>();int oldLimit=fieldLayout.maxCards;fieldLayout.maxCards=4;
        try
        {
            Check(!zone.CanHighlightCard(source),"Full field accepted a creature highlight");
            data.cardName="A";data.effectTypes=new[]{EffectType.DestroyOneEnemyBattle};eligibilityResource.currentResource=3;Check(!zone.CanHighlightCard(source),"Unaffordable spell highlighted");
            eligibilityResource.currentResource=4;Check(zone.CanHighlightCard(source),"Affordable spell failed on full field");
            data.cardName="K";data.effectTypes=new[]{EffectType.SpecialSummonK};var baseCard=turn.playerBattleArea.GetComponentsInChildren<CardController>().First();
            var kingBase=UnityEngine.Object.Instantiate(data);kingBase.cardName="10";kingBase.effectTypes=new EffectType[0];baseCard.SetData(kingBase);
            eligibilityResource.currentResource=2;Check(!zone.CanHighlightCard(source),"Unaffordable K highlighted");eligibilityResource.currentResource=3;Check(zone.CanHighlightCard(source),"Discounted K failed on full field");baseCard.SetData(data);UnityEngine.Object.Destroy(kingBase);
        }
        catch(Exception e){failure=e.ToString();}
        fieldLayout.maxCards=oldLimit;data.cardName="2";data.effectTypes=new EffectType[0];data.SetPowerFromName();eligibilityResource.currentResource=savedResource;zone.useCostCheck=false;
        if(failure!=null){Finish("FAIL: "+failure);yield break;}
        ev.position=new Vector2(-10,-10);drag.OnDrag(ev);yield return new WaitForSecondsRealtime(savedAsset.fadeOut+.1f);
        ev.position=new Vector2(bounds.center.x+bounds.width*.24f,bounds.center.y);drag.OnDrag(ev);
        yield return new WaitForSecondsRealtime(savedAsset.fadeIn*.5f);
        try{Check(view.Opacity>0&&view.Opacity<1,"No fade-in transition");}catch(Exception e){failure=e.ToString();}
        yield return new WaitForSecondsRealtime(savedAsset.fadeIn+.05f);yield return new WaitForEndOfFrame();var lit=Capture("highlight");
        float brightness=savedAsset.brightness;savedAsset.brightness=0;yield return new WaitForEndOfFrame();var plain=Capture("without-highlight");savedAsset.brightness=brightness;
        try
        {
            if(failure!=null)throw new Exception(failure);Check(view.Opacity>.99f,"Highlight did not reach full opacity");
            var a=lit.GetPixels32();var b=plain.GetPixels32();int changed=0;for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>8)changed++;Check(changed>200,"Glow not visible in actual render");
            Transform branch=turn.playerBattleArea;while(branch.parent!=view.TargetRect.parent&&branch.parent!=null)branch=branch.parent;Check(view.TargetRect.GetSiblingIndex()<branch.GetSiblingIndex(),"Field cards render behind glow");
            var old=view.ScreenRectangle;savedAsset.positionOffset=new Vector2(12,8);view.RefreshGeometry();Check(Vector2.Distance(view.ScreenRectangle.center-old.center,new Vector2(12,8)*Screen.width/720f)<1,"Position adjustment not applied");savedAsset.positionOffset=Vector2.zero;view.RefreshGeometry();
        }catch(Exception e){failure=e.ToString();}
        UnityEngine.Object.Destroy(lit);UnityEngine.Object.Destroy(plain);if(failure!=null){Finish("FAIL: "+failure);yield break;}
        yield return null;yield return null;
        ev.position=new Vector2(-10,-10);drag.OnDrag(ev);yield return new WaitForSecondsRealtime(savedAsset.fadeOut*.5f);
        try{Check(view.Opacity>0&&view.Opacity<1,"No fade-out transition");}catch(Exception e){failure=e.ToString();}
        yield return new WaitForSecondsRealtime(savedAsset.fadeOut+.04f);drag.OnEndDrag(ev);yield return new WaitForSeconds(.35f);
        try{Check(view.Opacity<.001f,"Glow remains outside target");Check(source.transform.parent==dealer.handArea,"Invalid drop did not return to hand");}catch(Exception e){failure=e.ToString();}
        if(failure!=null){Finish("FAIL: "+failure);yield break;}
        int fieldBefore=Count(turn.playerBattleArea),handBefore=Count(dealer.handArea);var resource=UnityEngine.Object.FindFirstObjectByType<ResourceManager>();int resources=resource.currentResource;
        ev.position=RectTransformUtility.WorldToScreenPoint(null,source.transform.position);drag.OnBeginDrag(ev);view.RefreshGeometry();bounds=view.ScreenRectangle;ev.position=new Vector2(bounds.xMin+10,bounds.center.y);drag.OnDrag(ev);yield return null;
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(ev,hits);
        try{Check(hits.Any(h=>h.gameObject==view.TargetRect.gameObject),"Expanded target missing from real UI raycast");}catch(Exception e){Finish("FAIL: "+e);yield break;}
        ev.pointerCurrentRaycast=hits.First(h=>h.gameObject==view.TargetRect.gameObject);view.TargetRect.GetComponent<FieldDropForwarder>().OnDrop(ev);drag.OnEndDrag(ev);
        yield return new WaitForSecondsRealtime(CardPlaySettings.Load().TotalTime/Mathf.Max(.05f,CardPlaySettings.Load().speed)+.5f);
        try{Check(Count(turn.playerBattleArea)==fieldBefore+1&&Count(dealer.handArea)==handBefore-1,"Expanded-area drop did not play exactly one card");Check(resource.currentResource==resources,"Free validation play unexpectedly spent resource");Check(!CardPlayPresentation.IsPlaying&&view.Opacity<.001f,"Placement did not clean up");}
        catch(Exception e){Finish("FAIL: "+e);yield break;}
        yield return new WaitForEndOfFrame();var final=Capture("placed");UnityEngine.Object.Destroy(final);
        Finish("PASS: Unaffordable creatures/spells/K never highlight or enable the expanded target; affordable cards react immediately, capacity and K discount match actual drop rules. Both fields render one card at the four-card size; five-card settings/gaps preserved. Actual drag begins from expanded hand, highlights only inside expanded target, fades in/out, returns rejected drops to hand. Shader visibly renders and brightness/position edits apply; glow stays below cards and blocks no input. Real EventSystem raycast reaches expanded target; forwarded drop plays exactly one card with normal showcase, then clears glow. No scene saved; settings restored.");
    }
}
