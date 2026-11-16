using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
[RequireComponent(typeof(CanvasRenderer))]
public sealed class AttackSelectionGraphic : MaskableGraphic,IPointerClickHandler,ICanvasRaycastFilter
{
    Material owned;TurnManager owner;System.Action cancelAction;
    public readonly List<Rect> passThrough=new List<Rect>();
    public void Configure(TurnManager turn,AttackSelectionSettings s,Vector4[] boxes,Vector4[] colors,Vector4[] flags,int count,float opacity,bool? captureInput=null,System.Action cancel=null,float confirmation=0f,Color? lowlight=null)
    {
        owner=turn;cancelAction=cancel;if(owned==null){var shader=Resources.Load<Shader>("TCardField/AttackSelection");if(shader==null)return;owned=new Material(shader){hideFlags=HideFlags.HideAndDontSave};material=owned;}
        float scale=Screen.width/720f;owned.SetVectorArray("_Entries",boxes);owned.SetVectorArray("_Colors",colors);owned.SetVectorArray("_Flags",flags);owned.SetInt("_Count",count);
        owned.SetVector("_ViewSize",new Vector4(Screen.width,Screen.height,scale,0));owned.SetVector("_Tune",new Vector4(s.blur*scale,s.lineWidth*scale,s.glowStrength,0));
        owned.SetVector("_WallTune",new Vector4(s.wallZoneCornerRadius*scale,s.wallZoneGlowBlur*scale,0,0));
        owned.SetColor("_DimColor",ConfirmationTheme.Load().StrengthenLowlight(lowlight??s.lowlightColor,confirmation));owned.SetColor("_HighlightColor",s.attackerHighlightColor);owned.SetFloat("_Opacity",opacity);maskable=false;raycastTarget=captureInput??turn.AttackSelectionActive;
    }
    internal void ConfigureIncomingRipple(Rect bounds, Color tint, float phase, float strength, float spread)
    {
        if (owned == null) return;
        owned.SetVector("_IncomingCard", new Vector4(bounds.center.x, bounds.center.y, bounds.width, bounds.height));
        owned.SetVector("_IncomingRipple", new Vector4(phase, strength, spread, 0f));
        owned.SetColor("_IncomingRippleColor", tint);
    }
    internal void ConfigureGuardBadgeCutouts(Vector4[] bounds)
    {
        if (owned != null) owned.SetVectorArray("_GuardBadgeCutouts", bounds);
    }
    internal void ConfigureDefenseGuardGlow(Vector4[] artworkBounds, CardActionGlowSettings settings)
    {
        if (owned == null) return;
        owned.SetVectorArray("_DefenseGuardArtwork", artworkBounds);
        owned.SetVector("_DefenseGuardShape", new Vector4(settings.positionOffset.x, settings.positionOffset.y,
            settings.edgeOffset, settings.cornerRadius));
        float strength = settings.enabledEffect ? settings.brightness * settings.EvaluatePulse(Time.unscaledTime) : 0f;
        owned.SetVector("_DefenseGuardLight", new Vector4(settings.blur, settings.lineWidth, strength, 0f));
        owned.SetColor("_DefenseGuardColor", settings.defenseColor);
    }
    public bool IsRaycastLocationValid(Vector2 screenPoint,Camera camera){foreach(var r in passThrough)if(r.Contains(screenPoint))return false;return true;}
    public void OnPointerClick(PointerEventData data){if(data.button!=PointerEventData.InputButton.Left)return;if(cancelAction!=null)cancelAction();else if(owner!=null)owner.CancelAttackSelection();}
    protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;vh.AddVert(new Vector3(r.xMin,r.yMin),Color.white,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),Color.white,Vector2.up);vh.AddVert(new Vector3(r.xMax,r.yMax),Color.white,Vector2.one);vh.AddVert(new Vector3(r.xMax,r.yMin),Color.white,Vector2.right);vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);}
    protected override void OnDestroy(){if(owned!=null){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}base.OnDestroy();}
}
