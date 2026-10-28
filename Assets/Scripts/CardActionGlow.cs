using UnityEngine;
[DisallowMultipleComponent]
public sealed class CardActionGlow : MonoBehaviour
{
    CardController controller;CardActionGlowGraphic graphic;Transform field;bool attack,defense;float opacity;
    public float Opacity=>opacity;
    public CardActionGlowGraphic Graphic=>graphic;
    public void SetAvailability(Transform area,bool canAttack,bool canBlock){field=area;attack=canAttack;defense=canBlock;}
    void LateUpdate()
    {
        if(controller==null)controller=GetComponent<CardController>();
        var s=CardActionGlowSettings.Load();
        bool inField=field!=null&&transform.parent==field&&controller!=null&&controller.data!=null;
        bool available=inField&&s.enabledEffect&&((attack&&CardActionIcon.CanAttack(controller))||(defense&&CardActionIcon.CanBlock(controller)));
        // A card leaving the field must never retain an aura in the hand, graveyard or K stack.
        if(!inField){opacity=0;Hide();return;}
        opacity=Mathf.MoveTowards(opacity,available?1:0,s.fadeDuration<=0?1:Time.unscaledDeltaTime/s.fadeDuration);
        var source=controller.artworkImage;
        if(opacity<=.0001f||source==null||!source.enabled||source.sprite==null||!source.gameObject.activeInHierarchy||source.transform==transform){Hide();return;}
        if(graphic==null){var go=new GameObject("Action glow (behind artwork)",typeof(RectTransform),typeof(CanvasRenderer),typeof(CardActionGlowGraphic));go.layer=source.gameObject.layer;graphic=go.GetComponent<CardActionGlowGraphic>();}
        var rt=graphic.rectTransform;var sr=source.rectTransform;if(rt.parent!=sr.parent)rt.SetParent(sr.parent,false);
        int index=sr.GetSiblingIndex();if(rt.GetSiblingIndex()<index)index--;if(rt.GetSiblingIndex()!=index)rt.SetSiblingIndex(index);
        rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);rt.localRotation=sr.localRotation;rt.localScale=sr.localScale;
        var size=sr.rect.size;float scale=size.x/118f;float pad=(s.blur*4+Mathf.Abs(s.edgeOffset)+s.lineWidth+10)*scale;
        rt.sizeDelta=size+Vector2.one*pad*2;
        Vector3 center=new Vector3((.5f-sr.pivot.x)*size.x,(.5f-sr.pivot.y)*size.y)+(Vector3)(s.positionOffset*scale);
        rt.localPosition=sr.localPosition+sr.localRotation*Vector3.Scale(center,sr.localScale);
        graphic.gameObject.SetActive(true);graphic.color=new Color(1,1,1,source.color.a);graphic.Configure(s,size,opacity,defense);
    }
    void Hide(){if(graphic!=null)graphic.gameObject.SetActive(false);}
    void OnDisable(){opacity=0;attack=defense=false;Hide();}
    void OnDestroy(){if(graphic!=null){if(Application.isPlaying)Destroy(graphic.gameObject);else DestroyImmediate(graphic.gameObject);}}
}
