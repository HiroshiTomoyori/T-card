using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public partial class HandDealer
{
    public IEnumerator PlaySummonDeckEffect(bool enemy,bool charge,SummonEffectSettings s)
    {
        var deck=enemy?enemyDeck:currentDeck;if(deck==null||deck.Count==0||cardPrefab==null)yield break;
        if(enemy&&!charge&&enemyHandCards.Count>=maxHandSize)yield break;
        int index=charge?0:enemy?SelectEnemyHandDrawIndex():Random.Range(0,deck.Count);var data=deck[index];deck.RemoveAt(index);
        GameObject preview=null;CanvasGroup destination=null;float destinationAlpha=1;bool committed=false;OpeningDealLayer layer=null;
        if(!enemy&&!charge)IsTurnDrawAnimating=true;
        try
        {
            var hand=enemy?enemyHandArea:handArea;var owner=hand.GetComponentInParent<Canvas>().rootCanvas;var parent=(RectTransform)owner.transform;
            preview=Instantiate(cardPrefab,parent);preview.name=charge?"Summon 5 deck to resource":"Summon 6 deck to hand";DisableWallInput(preview);
            var controller=preview.GetComponent<CardController>();if(controller!=null){controller.SetData(data);controller.enabled=false;}
            var art=controller!=null?controller.artworkImage:preview.GetComponentInChildren<Image>();
            var deckRect=enemy?enemyDeckPosition:deckPosition;layer=OpeningDealLayer.Begin(preview,deckRect,openingShuffleSettings);
            foreach(var c in preview.GetComponentsInChildren<Canvas>(true)){c.overrideSorting=true;c.sortingOrder=32740;}
            var rt=(RectTransform)preview.transform;rt.localScale=Vector3.one;rt.localRotation=Quaternion.identity;
            float unit=parent.rect.width/720f;Vector2 size=(enemy?s.enemyRevealSize:s.playerRevealSize)*unit;SetupCardSize(preview,size);
            var wall=(enemy?enemyWallArea:wallArea) as RectTransform;Vector3 start=deckRect!=null?OpeningShuffleView.DealPosition(deckRect):hand.position;
            Vector3 end=(wall!=null?wall.TransformPoint(wall.rect.center):hand.position)+parent.TransformVector((enemy?s.enemyRevealOffset:s.playerRevealOffset)*unit);
            if(charge)
            {
                var manager=enemy?null:FindFirstObjectByType<ResourceManager>();
                var enemyManager=enemy?(turnManager!=null?turnManager.enemyResourceManager:FindFirstObjectByType<EnemyResourceManager>()):null;
                var resourceView=enemy?enemyManager?.EnsureLiquidView():manager?.EnsureLiquidView();
                if(resourceView!=null)
                {
                    var zone=(RectTransform)resourceView.transform;
                    Vector3 point=parent.InverseTransformPoint(zone.TransformPoint(zone.rect.center));
                    point.y=parent.InverseTransformPoint(start).y+s.chargeRevealAboveDeck*unit;
                    end=parent.TransformPoint(point);
                }
            }
            Vector3 local=parent.InverseTransformPoint(end);local.x=Mathf.Clamp(local.x,parent.rect.xMin+size.x*.55f,parent.rect.xMax-size.x*.55f);local.y=Mathf.Clamp(local.y,parent.rect.yMin+size.y*.6f,parent.rect.yMax-size.y*.6f);end=parent.TransformPoint(local);
            if(art!=null)art.sprite=cardBackSprite;
            yield return TravelSummonCard(rt,start,end,s.drawTravel,s.speed);
            bool publicFace=charge||!enemy;
            if(publicFace){float age=0;while(age<s.drawFlip){float p=age/Mathf.Max(.01f,s.drawFlip);rt.localScale=new Vector3(Mathf.Max(.018f,Mathf.Abs(Mathf.Cos(p*Mathf.PI))),1,1);if(art!=null&&p>=.5f)art.sprite=data.artwork;yield return null;age+=Time.unscaledDeltaTime*Mathf.Max(.05f,s.speed);}rt.localScale=Vector3.one;if(art!=null)art.sprite=data.artwork;}
            yield return new WaitForSecondsRealtime(s.drawHold/Mathf.Max(.05f,s.speed));
            if(charge)
            {
                var resources=enemy?null:FindFirstObjectByType<ResourceManager>();var enemyResources=enemy?(turnManager!=null?turnManager.enemyResourceManager:FindFirstObjectByType<EnemyResourceManager>()):null;
                var view=enemy?enemyResources?.EnsureLiquidView():resources?.EnsureLiquidView();if(view==null)yield break;
                var liquid=view.Settings;
                // Keep one continuous trajectory from the revealed pose to the water.
                yield return view.DropCard(preview,true,s.chargeTravel/Mathf.Max(.05f,s.speed));if(enemy)enemyResources.AddResource(false);else resources.AddResource();committed=true;
                yield return new WaitForSecondsRealtime(Mathf.Max(0,liquid.beamDuration+liquid.rootGlowTail));
            }
            else if(enemy)
            {
                enemyHandCards.Add(data);enemyHandCount=enemyHandCards.Count;committed=true;RefreshEnemyHandVisual();UpdateEnemyHandCountText();
                foreach(Transform child in enemyHandArea)if(child.name=="EnemyHandBack"&&child.gameObject.activeSelf){destination=child.GetComponent<CanvasGroup>();}
                if(destination!=null){destinationAlpha=destination.alpha;destination.alpha=0;}
                yield return null;Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)enemyHandArea);
                var target=destination!=null?(RectTransform)destination.transform:(RectTransform)enemyHandArea;
                yield return TravelSummonCard(rt,rt.position,target.TransformPoint(target.rect.center),s.handTravel,s.speed,target);
            }
            else
            {
                var handCard=CreateHandCard(data);committed=true;destination=handCard.GetComponent<CanvasGroup>()??handCard.AddComponent<CanvasGroup>();destinationAlpha=destination.alpha;destination.alpha=0;SortPlayerHand();yield return null;Canvas.ForceUpdateCanvases();
                var card=handCard.GetComponent<CardController>();var target=card!=null&&card.artworkImage!=null?card.artworkImage.rectTransform:(RectTransform)handCard.transform;
                var pro=handCard.GetComponent<CCP.Card>();Vector3 point=target.TransformPoint(target.rect.center);if(pro!=null&&pro.destinationPosition!=Vector3.zero)point+=pro.destinationPosition-handCard.transform.position;
                yield return TravelSummonCard(rt,rt.position,point,s.handTravel,s.speed,target);
                var layout=handCard.GetComponent<LayoutElement>();if(layout!=null)layout.ignoreLayout=false;
                if(pro!=null&&pro.destinationPosition!=Vector3.zero)handCard.transform.position=pro.destinationPosition;SortPlayerHand();
            }
        }
        finally
        {
            if(preview!=null)preview.SetActive(false);if(destination!=null)destination.alpha=destinationAlpha;
            layer?.Dispose();if(preview!=null)Destroy(preview);if(!committed)deck.Insert(Mathf.Min(index,deck.Count),data);if(!enemy&&!charge)IsTurnDrawAnimating=false;
        }
    }
    IEnumerator TravelSummonCard(RectTransform rt,Vector3 from,Vector3 to,float duration,float speed,RectTransform resize=null)
    {
        Vector2 start=rt.sizeDelta;Vector3 scale=rt.localScale;float age=0;
        while(age<duration&&rt!=null){float t=Mathf.Clamp01(age/Mathf.Max(.01f,duration)),p=1-Mathf.Pow(1-t,3);rt.position=Vector3.Lerp(from,to,p);
            if(resize!=null){Vector2 size=new Vector2(resize.rect.width*Mathf.Abs(resize.lossyScale.x/rt.parent.lossyScale.x),resize.rect.height*Mathf.Abs(resize.lossyScale.y/rt.parent.lossyScale.y));rt.sizeDelta=Vector2.Lerp(start,size,p);rt.rotation=Quaternion.Slerp(Quaternion.identity,resize.rotation,p);}yield return null;age+=Time.unscaledDeltaTime*Mathf.Max(.05f,speed);}
        if(rt!=null)rt.position=to;
    }
}
