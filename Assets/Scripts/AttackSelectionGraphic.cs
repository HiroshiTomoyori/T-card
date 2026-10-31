using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
[RequireComponent(typeof(CanvasRenderer))]
public sealed class AttackSelectionGraphic : MaskableGraphic,IPointerClickHandler,ICanvasRaycastFilter
{
    Material owned;TurnManager owner;System.Action cancelAction;
    public readonly List<Rect> passThrough=new List<Rect>();
    public void Configure(TurnManager turn,AttackSelectionSettings s,Vector4[] boxes,Vector4[] colors,Vector4[] flags,int count,float opacity,bool? captureInput=null,System.Action cancel=null)
    {
        owner=turn;cancelAction=cancel;if(owned==null){var shader=Resources.Load<Shader>("TCardField/AttackSelection");if(shader==null)return;owned=new Material(shader){hideFlags=HideFlags.HideAndDontSave};material=owned;}
        float scale=Screen.width/720f;owned.SetVectorArray("_Entries",boxes);owned.SetVectorArray("_Colors",colors);owned.SetVectorArray("_Flags",flags);owned.SetInt("_Count",count);
        owned.SetVector("_ViewSize",new Vector4(Screen.width,Screen.height,scale,0));owned.SetVector("_Tune",new Vector4(s.blur*scale,s.lineWidth*scale,s.glowStrength,0));
        owned.SetVector("_WallTune",new Vector4(s.wallZoneCornerRadius*scale,s.wallZoneGlowBlur*scale,0,0));
        owned.SetColor("_DimColor",s.lowlightColor);owned.SetColor("_HighlightColor",s.attackerHighlightColor);owned.SetFloat("_Opacity",opacity);maskable=false;raycastTarget=captureInput??turn.AttackSelectionActive;
    }
    public bool IsRaycastLocationValid(Vector2 screenPoint,Camera camera){foreach(var r in passThrough)if(r.Contains(screenPoint))return false;return true;}
    public void OnPointerClick(PointerEventData data){if(data.button!=PointerEventData.InputButton.Left)return;if(cancelAction!=null)cancelAction();else if(owner!=null)owner.CancelAttackSelection();}
    protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;vh.AddVert(new Vector3(r.xMin,r.yMin),Color.white,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),Color.white,Vector2.up);vh.AddVert(new Vector3(r.xMax,r.yMax),Color.white,Vector2.one);vh.AddVert(new Vector3(r.xMax,r.yMin),Color.white,Vector2.right);vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);}
    protected override void OnDestroy(){if(owned!=null){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}base.OnDestroy();}
}
