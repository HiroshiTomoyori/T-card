using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public partial class CardEffectManager
{
    readonly HashSet<int> resolvingDeckCards=new HashSet<int>();
    public bool IsResolvingSummon=>resolvingDeckCards.Count>0;
    bool QueueDeckSummon(CardController card,bool shield,bool enemy)
    {
        if(card==null||card.data==null||card.data.effectTypes==null)return false;
        bool has=System.Array.Exists(card.data.effectTypes,x=>x==EffectType.ChargeTopDeck||x==EffectType.Draw1);if(!has)return false;
        if(resolvingDeckCards.Add(card.GetInstanceID()))StartCoroutine(DeckSummonSequence(card,shield,enemy));return true;
    }
    IEnumerator DeckSummonSequence(CardController card,bool shield,bool enemy)
    {
        int id=card.GetInstanceID();var lease=PresentationSequenceLock.Acquire(this);var settings=SummonEffectSettings.Load();
        try
        {
            yield return null;
            while(card!=null)
            {
                var ready=card.GetComponent<CardReadinessVisual>();var drag=card.GetComponent<CardDrag>();
                if(ready!=null&&ready.PreparationLanded)break;
                if((drag==null||!drag.IsDropSnapPlaying)&&!CardPlayPresentation.IsPlaying&&!DOTween.IsTweening(card.transform))break;
                yield return null;
            }
            if(card==null)yield break;
            lease.PauseGame();yield return new WaitForSecondsRealtime(settings.landingDelay/Mathf.Max(.05f,settings.speed));
            if(card==null)yield break;
            yield return SummonPulse(card,settings);
            if(card==null)yield break;
            var dealer=FindFirstObjectByType<HandDealer>();
            if(dealer!=null)foreach(var effect in card.data.effectTypes)
            {
                if(effect==EffectType.ChargeTopDeck)yield return dealer.PlaySummonDeckEffect(enemy,true,settings);
                else if(effect==EffectType.Draw1)yield return dealer.PlaySummonDeckEffect(enemy,false,settings);
            }
        }
        finally{lease.Dispose();resolvingDeckCards.Remove(id);}
        if(card!=null)ResolveOnSummon(card,shield,enemy,true);
    }
    IEnumerator SummonPulse(CardController card,SummonEffectSettings settings)
    {
        var source=card.GetComponentInParent<Canvas>();if(source==null||card.artworkImage==null)yield break;source=source.rootCanvas;
        var go=new GameObject("Summon ability orange pulse",typeof(RectTransform),typeof(CanvasRenderer),typeof(Canvas),typeof(SummonAbilityPulseGraphic));go.layer=card.gameObject.layer;go.transform.SetParent(source.transform,false);
        var canvas=go.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingLayerID=source.sortingLayerID;canvas.sortingOrder=Mathf.Min(32760,source.sortingOrder+240);
        var rt=(RectTransform)go.transform;var fx=go.GetComponent<SummonAbilityPulseGraphic>();float age=0;var corners=new Vector3[4];
        try{while(age<settings.pulseDuration&&card!=null){var art=card.artworkImage.rectTransform;art.GetWorldCorners(corners);var a=source.transform.InverseTransformPoint(corners[0]);var b=source.transform.InverseTransformPoint(corners[2]);rt.localPosition=(a+b)*.5f;var size=new Vector2(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y));rt.sizeDelta=size*settings.pulseExpansion*1.5f;fx.Configure(size,settings.pulseColor,age/Mathf.Max(.01f,settings.pulseDuration),settings.pulseStrength,settings.pulseExpansion);yield return null;age+=Time.unscaledDeltaTime*Mathf.Max(.05f,settings.speed);}}
        finally{if(go!=null){go.SetActive(false);Destroy(go);}}
    }
}
