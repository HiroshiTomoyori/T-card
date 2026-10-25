using UnityEngine;
using UnityEngine.UI;
[DisallowMultipleComponent]
public sealed class CardBoundaryShadow : MonoBehaviour
{
    public CardShadowSettings settings;
    CardController controller;
    Image artwork;
    CardShadowGraphic graphic;
    public void RefreshShadow()
    {
        if(settings==null)settings=Resources.Load<CardShadowSettings>("TCardCards/CardShadowSettings");
        if(controller==null)controller=GetComponent<CardController>();
        var source=controller!=null?controller.artworkImage:null;
        if(source==null)source=GetComponent<Image>();
        if(source==null || settings==null || settings.shadowShader==null || source.transform==transform){Hide();return;}
        if(!settings.shadowsEnabled || !source.enabled || source.sprite==null || !source.gameObject.activeInHierarchy){Hide();return;}
        artwork=source;
        if(graphic==null)
        {
            var go=new GameObject("Card boundary shadow",typeof(RectTransform),typeof(CanvasRenderer),typeof(CardShadowGraphic));
            go.layer=source.gameObject.layer;graphic=go.GetComponent<CardShadowGraphic>();
        }
        var rt=graphic.rectTransform;var sr=source.rectTransform;
        // The artwork's previous sibling is behind its face, but remains inside the card.
        if(rt.parent!=sr.parent)rt.SetParent(sr.parent,false);
        int index=sr.GetSiblingIndex();if(rt.GetSiblingIndex()<index)index--;
        if(rt.GetSiblingIndex()!=index)rt.SetSiblingIndex(index);
        rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);
        rt.localRotation=sr.localRotation;rt.localScale=sr.localScale;
        var size=sr.rect.size;float scale=size.x/Mathf.Max(1,settings.referenceWidth);
        float padding=(Mathf.Max(settings.blur,settings.contactBlur)*3+settings.offset.magnitude+2)*scale;
        rt.sizeDelta=size+Vector2.one*padding*2;
        rt.localPosition=sr.localPosition+sr.localRotation*Vector3.Scale(new Vector3((.5f-sr.pivot.x)*size.x,(.5f-sr.pivot.y)*size.y),sr.localScale);
        graphic.gameObject.SetActive(true);graphic.color=new Color(1,1,1,source.color.a);graphic.Configure(settings,size);
    }
    void LateUpdate(){RefreshShadow();}
    void Hide(){if(graphic!=null)graphic.gameObject.SetActive(false);}
    void OnDisable(){Hide();}
    void OnDestroy(){if(graphic!=null){if(Application.isPlaying)Destroy(graphic.gameObject);else DestroyImmediate(graphic.gameObject);}}
}
