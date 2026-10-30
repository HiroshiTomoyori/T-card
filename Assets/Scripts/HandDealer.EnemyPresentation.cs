using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public partial class HandDealer
{
 public bool IsEnemyDrawAnimating{get;private set;}
 public IEnumerator DrawEnemyTurnStartCard(){yield return EnemyDrawAnimation(1);}
 IEnumerator EnemyFlowDraw(int amount)
 {
  while(IsEnemyDrawAnimating)yield return null;
  if(enemyDeck==null||enemyHandArea==null||cardPrefab==null)yield break;
  IsEnemyDrawAnimating=true;
  var s=EnemyPresentationSettings.Load();
  try{
   int count=Mathf.Min(amount,enemyDeck.Count,Mathf.Max(0,maxHandSize-enemyHandCards.Count));
   for(int i=0;i<count&&enemyDeck.Count>0;i++){
    int index=SelectEnemyHandDrawIndex();CardData data=enemyDeck[index];enemyDeck.RemoveAt(index);
    GameObject preview=null;CanvasGroup destinationVisibility=null;OpeningDealLayer layer=null;bool committed=false;
    try{
     preview=Instantiate(cardPrefab,enemyHandArea.parent);preview.name="Enemy turn draw (back only)";DisableWallInput(preview);
     var controller=preview.GetComponent<CardController>();if(controller!=null){controller.enabled=false;if(controller.artworkImage!=null)controller.artworkImage.sprite=cardBackSprite;}
     SetupCardSize(preview,s.drawSize);var rt=(RectTransform)preview.transform;rt.localScale=Vector3.one;rt.localRotation=Quaternion.identity;
     layer=OpeningDealLayer.Begin(preview,enemyDeckPosition,openingShuffleSettings);
     var fx=preview.AddComponent<TurnDrawHighlightEffect>();fx.Initialize(this,s);
     var start=enemyDeckPosition!=null?OpeningShuffleView.DealPosition(enemyDeckPosition):enemyHandArea.position;
     var wall=enemyWallArea as RectTransform;Vector3 end=(wall!=null?wall.TransformPoint(wall.rect.center):enemyHandArea.position)+rt.parent.TransformVector(s.drawOffset);
     rt.position=start;float elapsed=0;Vector2 startSize=enemyHandCardSize;
     var stack=enemyDeckPosition;if(stack!=null)startSize=new Vector2(stack.rect.width*Mathf.Abs(stack.lossyScale.x/rt.parent.lossyScale.x),stack.rect.height*Mathf.Abs(stack.lossyScale.y/rt.parent.lossyScale.y));
     while(elapsed<s.drawTravel){float t=Mathf.Clamp01(elapsed/Mathf.Max(.01f,s.drawTravel)),p=1-Mathf.Pow(1-t,3);rt.position=Vector3.Lerp(start,end,p);SetupCardSize(preview,Vector2.Lerp(startSize,s.drawSize,p));fx.SetTime(elapsed);yield return null;elapsed+=Time.unscaledDeltaTime;}
     rt.position=end;SetupCardSize(preview,s.drawSize);elapsed=0;
     while(elapsed<s.drawSettle+s.drawHold){fx.SetTime(s.drawTravel+elapsed);yield return null;elapsed+=Time.unscaledDeltaTime;}
     enemyHandCards.Add(data);enemyHandCount=enemyHandCards.Count;committed=true;RefreshEnemyHandVisual();UpdateEnemyHandCountText();
     // Wait for destroyed previous backs and the layout to settle before finding the new slot.
     foreach(Transform child in enemyHandArea)if(child.name=="EnemyHandBack"&&child.gameObject.activeSelf){var cg=child.GetComponent<CanvasGroup>();if(cg!=null)destinationVisibility=cg;}
     if(destinationVisibility!=null)destinationVisibility.alpha=0;
     yield return null;Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(enemyHandArea as RectTransform);
     var target=destinationVisibility!=null?destinationVisibility.GetComponent<RectTransform>():enemyHandArea as RectTransform;
     Vector3 from=rt.position;elapsed=0;
     while(elapsed<s.drawToHand&&target!=null){float u=Mathf.Clamp01(elapsed/Mathf.Max(.01f,s.drawToHand)),p=1-Mathf.Pow(1-u,3);rt.position=Vector3.Lerp(from,target.TransformPoint(target.rect.center),p);rt.rotation=Quaternion.Slerp(Quaternion.identity,target.rotation,p);
      Vector2 size=new Vector2(target.rect.width*Mathf.Abs(target.lossyScale.x/rt.parent.lossyScale.x),target.rect.height*Mathf.Abs(target.lossyScale.y/rt.parent.lossyScale.y));SetupCardSize(preview,Vector2.Lerp(s.drawSize,size,p));fx.SetTime(s.drawTravel+s.drawSettle+s.drawHold+elapsed);yield return null;elapsed+=Time.unscaledDeltaTime;}
    }finally{if(preview!=null)preview.SetActive(false);if(destinationVisibility!=null)destinationVisibility.alpha=1;layer?.Dispose();if(preview!=null)Destroy(preview);if(!committed)enemyDeck.Add(data);}
    if(s.drawInterval>0)yield return new WaitForSecondsRealtime(s.drawInterval);
   }
  }finally{IsEnemyDrawAnimating=false;}
 }
 IEnumerator EnemyFlowCharge(CardData data)
 {
  if(data==null||enemyHandCards==null||!enemyHandCards.Contains(data)||cardPrefab==null)yield break;
  var manager=turnManager!=null?turnManager.enemyResourceManager:FindFirstObjectByType<EnemyResourceManager>();
  var view=manager!=null?manager.EnsureLiquidView():null;
  if(view==null)yield break;
  var s=EnemyPresentationSettings.Load();var liquid=view.Settings;
  GameObject preview=null;OpeningDealLayer layer=null;
  try{
   preview=Instantiate(cardPrefab,enemyHandArea!=null?enemyHandArea.parent:transform);preview.name="Enemy resource charge (public face)";
   var card=preview.GetComponent<CardController>();if(card!=null){card.SetData(data);card.enabled=false;if(card.artworkImage!=null){card.artworkImage.sprite=data.artwork;card.artworkImage.color=Color.white;}}
   DisableWallInput(preview);SetupCardSize(preview,s.chargeRevealSize);
   foreach(var t in preview.GetComponentsInChildren<Transform>(true))t.DOKill();
   var pro=preview.GetComponent<CCP.Card>();if(pro!=null)pro.enabled=false;
   var visibility=preview.GetComponent<CanvasGroup>();if(visibility!=null)visibility.alpha=1;
   layer=OpeningDealLayer.Begin(preview,enemyDeckPosition,openingShuffleSettings);
   if(view.ForegroundCanvas!=null)foreach(var canvas in preview.GetComponentsInChildren<Canvas>(true)){canvas.overrideSorting=true;canvas.sortingLayerID=view.ForegroundCanvas.sortingLayerID;canvas.sortingOrder=Mathf.Min(32767,view.ForegroundCanvas.sortingOrder+Mathf.Max(1,s.chargeRevealSortingOffset));}
   var rt=(RectTransform)preview.transform;rt.localScale=Vector3.one;rt.localRotation=Quaternion.identity;
   Vector3 start=enemyHandArea!=null?enemyHandArea.position:enemyResourcePosition.position;
   enemyHandCards.Remove(data);enemyHandCount=enemyHandCards.Count;RefreshEnemyHandVisual();UpdateEnemyHandCountText();
   var root=(RectTransform)view.GetComponentInParent<Canvas>().rootCanvas.transform;
   var zone=(RectTransform)view.transform;var summon=SummonEffectSettings.Load();
   Vector3 point=root.InverseTransformPoint(zone.TransformPoint(zone.rect.center));
   if(enemyDeckPosition!=null)point.y=root.InverseTransformPoint(OpeningShuffleView.DealPosition(enemyDeckPosition)).y+summon.chargeRevealAboveDeck*root.rect.width/720f;
   Vector3 end=root.TransformPoint(point);
   end=ClampEnemyChargeReveal(rt,end,s.chargeScreenPadding);
   float elapsed=0;
   while(elapsed<s.chargeTravel){float p=Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.01f,s.chargeTravel));rt.position=Vector3.Lerp(start,end,p);yield return null;elapsed+=Time.unscaledDeltaTime;}
   rt.position=end;float hold=Mathf.Max(s.chargeHold,s.chargeMinimumVisibleDuration);if(hold>0)yield return new WaitForSecondsRealtime(hold);
   yield return view.DropCard(preview,true);
  }finally{layer?.Dispose();if(preview!=null){preview.SetActive(false);Destroy(preview);}}
 }
 Vector3 ClampEnemyChargeReveal(RectTransform card,Vector3 position,float padding)
 {
  var canvas=card.GetComponentInParent<Canvas>().rootCanvas;Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
  card.position=position;var corners=new Vector3[4];card.GetWorldCorners(corners);Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;
  foreach(var corner in corners){var p=RectTransformUtility.WorldToScreenPoint(camera,corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
  float pad=padding*Screen.width/720f;
  Vector2 offset=new Vector2(min.x<pad?pad-min.x:max.x>Screen.width-pad?Screen.width-pad-max.x:0,min.y<pad?pad-min.y:max.y>Screen.height-pad?Screen.height-pad-max.y:0);
  Vector2 screen=RectTransformUtility.WorldToScreenPoint(camera,position)+offset;
  return RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)card.parent,screen,camera,out var point)?point:position;
 }
}
