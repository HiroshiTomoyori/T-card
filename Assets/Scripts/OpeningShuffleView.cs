using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public sealed class OpeningShuffleView : MonoBehaviour
{
    readonly List<Image> cards=new List<Image>();
    RectTransform root,source;
    public RectTransform Source=>source;
    public RectTransform AspectReference;
    public Vector2 RenderedCardSize {get;private set;}
    public Vector3 TopPosition=>cards.Count>0 && cards[cards.Count-1]!=null?cards[cards.Count-1].rectTransform.TransformPoint(cards[cards.Count-1].rectTransform.rect.center):source.TransformPoint(source.rect.center);
    public static OpeningShuffleView ForDeck(RectTransform deck)
    {
        if(deck==null)return null;
        foreach(var view in FindObjectsByType<OpeningShuffleView>(FindObjectsSortMode.None))
            if(view.source!=null && (view.source==deck || view.source.IsChildOf(deck)))return view;
        return null;
    }
    public static Vector3 DealPosition(RectTransform deck){var view=ForDeck(deck);return view!=null && view.settled?view.TopPosition:deck.TransformPoint(deck.rect.center);}
    OpeningShuffleSettings liveSettings;
    bool settled;
    public void KeepStack(OpeningShuffleSettings settings){liveSettings=settings;settled=true;PlaceBehindHands();Sample(settings,settings.ShuffleDuration);}
    void PlaceBehindHands()
    {
        if(root==null || source==null)return;
        var canvas=source.GetComponentInParent<Canvas>().rootCanvas;
        var dealer=canvas.GetComponentInChildren<HandDealer>(true);
        if(dealer==null || dealer.handArea==null)return;
        Transform common=source.parent;
        while(common!=null && (!dealer.handArea.IsChildOf(common) || (dealer.enemyHandArea!=null && !dealer.enemyHandArea.IsChildOf(common))))common=common.parent;
        if(common==null)return;
        root.SetParent(common,false);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        int before=common.childCount-1;
        foreach(var hand in new[]{dealer.handArea,dealer.enemyHandArea})
        {
            if(hand==null)continue;Transform branch=hand;while(branch.parent!=common && branch.parent!=null)branch=branch.parent;
            before=Mathf.Min(before,branch.GetSiblingIndex());
        }
        root.SetSiblingIndex(before);
        OverlayCanvas.sortingOrder=canvas.sortingOrder;OverlayCanvas.sortingLayerID=canvas.sortingLayerID;OverlayCanvas.overrideSorting=false;
    }
    void LateUpdate(){if(settled && liveSettings!=null)Sample(liveSettings,liveSettings.ShuffleDuration);}
    public Canvas OverlayCanvas {get;private set;}
    public int VisibleCardCount {get;private set;}
    public void Initialize(RectTransform anchor,Sprite back,OpeningShuffleSettings settings)
    {
        source=anchor;
        var canvas=anchor.GetComponentInParent<Canvas>().rootCanvas;
        var go=new GameObject("Opening waterfall cards",typeof(RectTransform),typeof(Canvas));go.layer=anchor.gameObject.layer;go.transform.SetParent(canvas.transform,false);root=(RectTransform)go.transform;
        root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        OverlayCanvas=go.GetComponent<Canvas>();OverlayCanvas.overrideSorting=true;OverlayCanvas.sortingLayerID=canvas.sortingLayerID;OverlayCanvas.sortingOrder=canvas.sortingOrder+settings.sortingOffset;
        for(int i=0;i<Mathf.Clamp(settings.visualCardCount,1,60);i++)
        {
            var card=new GameObject("Falling card "+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Shadow));card.layer=anchor.gameObject.layer;card.transform.SetParent(root,false);
            var image=card.GetComponent<Image>();image.sprite=back;image.raycastTarget=false;image.preserveAspect=false;
            var shadow=card.GetComponent<Shadow>();shadow.effectColor=new Color(.1f,.065f,.04f,.19f);shadow.effectDistance=new Vector2(1.2f,-2);cards.Add(image);card.SetActive(false);
        }
        Sample(settings,0);
    }
    public void Sample(OpeningShuffleSettings settings,float time)
    {
        if(root==null||source==null)return;
        Vector2 center=root.InverseTransformPoint(source.TransformPoint(source.rect.center));
        Vector3 a=root.InverseTransformPoint(source.TransformPoint(new Vector3(source.rect.xMin,source.rect.yMin))),b=root.InverseTransformPoint(source.TransformPoint(new Vector3(source.rect.xMax,source.rect.yMax)));
        Vector2 size=new Vector2(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y));
        if(AspectReference!=null)
        {
            Vector3 w=root.InverseTransformVector(AspectReference.TransformVector(Vector3.right*AspectReference.rect.width));
            Vector3 h=root.InverseTransformVector(AspectReference.TransformVector(Vector3.up*AspectReference.rect.height));
            size.x=size.y*w.magnitude/Mathf.Max(.001f,h.magnitude);
        }
        RenderedCardSize=size;
        VisibleCardCount=0;
        for(int i=0;i<cards.Count;i++)
        {
            var pose=OpeningShuffleMotion.Sample(settings,i,time,center,size,root.rect.xMin);var image=cards[i];image.gameObject.SetActive(pose.visible);if(!pose.visible)continue;VisibleCardCount++;
            var r=image.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pose.position;r.localScale=Vector3.one*pose.scale;r.localRotation=Quaternion.Euler(0,0,pose.angle);image.color=new Color(1,1,1,pose.alpha);
        }
    }
    public void Hide(){if(root!=null)root.gameObject.SetActive(false);}
    void OnDestroy(){if(root!=null){if(Application.isPlaying)Destroy(root.gameObject);else DestroyImmediate(root.gameObject);}}
}
