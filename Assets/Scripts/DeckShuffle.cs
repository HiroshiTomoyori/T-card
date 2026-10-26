using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class DeckShuffle : MonoBehaviour
{
    public OpeningShuffleSettings openingSettings;
    [Header("Legacy values / 旧設定")]
    public float moveAmount=20f;
    public float speed=.04f;
    public int shuffleCount=10;
    public bool IsShuffling {get;private set;}
    public bool HasCompleted {get;private set;}
    Image deckImage,enemyImage;
    OpeningShuffleView visual,enemyVisual;
    Coroutine routine;
    bool previousImageEnabled,previousEnemyEnabled,enemyCaptured;
    void Awake()
    {
        openingSettings=openingSettings!=null?openingSettings:Resources.Load<OpeningShuffleSettings>("TCardOpening/OpeningShuffleSettings");
        deckImage=GetComponent<Image>();
        if(deckImage!=null){previousImageEnabled=deckImage.enabled;deckImage.enabled=false;}
        PrepareEnemyDeck(FindDealer());
    }
    void OnEnable(){HasCompleted=false;if(deckImage!=null && openingSettings!=null)deckImage.enabled=false;PrepareEnemyDeck(FindDealer());}
    HandDealer FindDealer(){var canvas=GetComponentInParent<Canvas>();return canvas!=null?canvas.rootCanvas.GetComponentInChildren<HandDealer>(true):null;}
    void PrepareEnemyDeck(HandDealer dealer)
    {
        if(dealer==null || dealer.enemyDeckImage==null)return;
        enemyImage=dealer.enemyDeckImage;if(!enemyCaptured){previousEnemyEnabled=enemyImage.enabled;enemyCaptured=true;}enemyImage.enabled=false;
    }
    public void StartShuffle()
    {
        if(IsShuffling||HasCompleted)return;
        if(InputLockManager.I!=null)InputLockManager.I.LockInput();
        routine=StartCoroutine(Opening());
    }
    IEnumerator Opening()
    {
        yield return ReplayShuffle();
        HasCompleted=true;routine=null;
        var dealer=FindDealer();if(dealer!=null)dealer.DealStart();
    }
    // Shared by the first deal and Redraw. Does not start dealing or reset redraw eligibility.
    public IEnumerator ReplayShuffle(bool includeEnemy=true)
    {
        if(IsShuffling)yield break;
        IsShuffling=true;ClearOne(ref visual);if(includeEnemy)ClearOne(ref enemyVisual);bool complete=false;
        try
        {
            var dealer=FindDealer();
            if(openingSettings==null)openingSettings=Resources.Load<OpeningShuffleSettings>("TCardOpening/OpeningShuffleSettings");
            var rect=GetComponent<RectTransform>();
            if(openingSettings!=null && rect!=null && rect.GetComponentInParent<Canvas>()!=null)
            {
                if(deckImage!=null)deckImage.enabled=false;
                Sprite back=deckImage!=null?deckImage.sprite:null;if(back==null && dealer!=null)back=dealer.cardBackSprite;
                visual=gameObject.AddComponent<OpeningShuffleView>();visual.Initialize(rect,back,openingSettings);
                if(includeEnemy && dealer!=null && dealer.enemyDeckPosition!=null && dealer.enemyDeckPosition!=rect)
                {
                    PrepareEnemyDeck(dealer);
                    var enemyAnchor=enemyImage!=null?enemyImage.rectTransform:dealer.enemyDeckPosition;
                    enemyVisual=gameObject.AddComponent<OpeningShuffleView>();enemyVisual.AspectReference=rect;enemyVisual.Initialize(enemyAnchor,back,openingSettings);
                }
                float elapsed=0;
                while(elapsed<openingSettings.ShuffleDuration)
                {
                    visual.Sample(openingSettings,elapsed);if(includeEnemy && enemyVisual!=null)enemyVisual.Sample(openingSettings,elapsed);
                    yield return null;elapsed+=(openingSettings.useUnscaledTime?Time.unscaledDeltaTime:Time.deltaTime)*Mathf.Max(.05f,openingSettings.globalSpeed);
                }
                visual.KeepStack(openingSettings);if(includeEnemy && enemyVisual!=null)enemyVisual.KeepStack(openingSettings);
                complete=true;
            }
        }
        finally{IsShuffling=false;if(!complete){ClearVisuals();RestoreImages();}}
    }
    void RestoreImages(){if(deckImage!=null)deckImage.enabled=previousImageEnabled;if(enemyImage!=null&&enemyCaptured)enemyImage.enabled=previousEnemyEnabled;}
    void ClearOne(ref OpeningShuffleView view){if(view==null)return;view.Hide();if(Application.isPlaying)Destroy(view);else DestroyImmediate(view);view=null;}
    void ClearVisuals(){ClearOne(ref visual);ClearOne(ref enemyVisual);}
    void OnDisable(){StopAllCoroutines();routine=null;IsShuffling=false;HasCompleted=false;ClearVisuals();RestoreImages();}
}
