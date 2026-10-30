using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public partial class HandDealer : MonoBehaviour
{
    [Header("Hand")]
    public Transform handArea;
    public RectTransform deckPosition;

    [Header("配布始点の調整")]
    public Vector2 dealStartOffset = Vector2.zero;

    [Header("Wall Cards")]
    public Transform wallArea;
    public Sprite cardBackSprite;
    public int wallCount = 5;

    [Header("King Wall Addition Limit")]
    [Tooltip("各陣営が1対戦中にキングで追加できるウォールの累計枚数。初期ウォールは含まず、破壊されても使用済み枚数は戻りません。0で追加なし。")]
    [Min(0)] public int maxKingWallAdditions = 4;
    int playerKingWallsAdded;
    int enemyKingWallsAdded;
    public int EnemyKingRecoveryRemaining => Mathf.Min(
        Mathf.Max(0, maxKingWallAdditions - enemyKingWallsAdded), enemyDeck != null ? enemyDeck.Count : 0);

    [Header("Card Size")]
    public Vector2 handCardSize = new Vector2(75, 112);
    [Header("Turn draw reveal")]
    public Vector2 turnDrawRevealSize = new Vector2(100f, 150f);
    public Vector2 turnDrawRevealOffset = Vector2.zero;
    [Min(0.01f)] public float turnDrawTravelTime = 0.24f;
    [Min(0.01f)] public float turnDrawFlipTime = 0.22f;
    [Tooltip("フリップ完了後、表面を表示してから手札へ移動するまでの秒数。0で待ち時間なし。")]
    [Min(0f)] public float turnDrawRevealHoldTime = 0.5f;
    [Min(0.01f)] public float turnDrawHandTime = 0.2f;
    [Header("Turn draw presentation / 表示位置・拡大・ハイライト")]
    [Min(.1f)] public float turnDrawRevealScale = 1.3f;
    public float turnDrawRevealLift = 90f;
    [Range(0,1)] public float turnDrawHighlightStrength = .5f;
    [Range(0,1.4f)] public float turnDrawOuterGlowStrength = .8f;
    [Range(0,60)] public float turnDrawGlowSpread = 32f;
    public Color turnDrawHighlightColor = new Color(1f,.97f,.84f,1f);
    [Header("Turn draw field shadow / フィールドに落ちる影")]
    [Range(0,1),Tooltip("0で影なし。大きいほど濃い影。")]
    public float turnDrawFieldShadowStrength = .24f;
    [Min(1)] public float turnDrawFieldShadowBlur = 22f;
    public Vector2 turnDrawFieldShadowOffset = new Vector2(18f,-34f);
    public Color turnDrawFieldShadowColor = new Color(.176f,.114f,.067f,1f);
    public bool IsTurnDrawAnimating { get; private set; }
    [HideInInspector] public Vector2 wallCardSize = new Vector2(70, 105);

    [Header("Prefab")]
    public GameObject cardPrefab;

    [Header("Card Controller Pro / Player Hand")]
    public GameObject proHandCardPrefab;
    public CCP.CardGroup proHandGroup;


    [Header("Card Data")]
    public List<CardData> cardList = new List<CardData>();

    [Header("Opening Animation / 初期配布の設定")]
    public OpeningShuffleSettings openingShuffleSettings;

    [Header("Deal Settings")]
    public int dealCount = 5;
    public int maxHandSize = 10;
    public float dealInterval = 0.15f;
    public float flyTime = 0.25f;

    [Header("UI")]
    public GameObject redrawButton;
    public GameObject confirmButton;
    public GameObject redrawChoicePanel;

    [Header("Opening Hand Display")]
    public HandController handController;
    public float openingHandScale = 0.85f;
    public float openingHandYOffset = 180f;

    [Header("Hand Limit Display")]
    public Vector2 handLimitDisplayPosition = new Vector2(0f, 180f);
    public Vector2 handLimitCardSize = new Vector2(100f, 150f);
    [Min(0f)] public float handLimitCardOverlap = 30f;
    public Vector2 handLimitDrawnCardPosition = new Vector2(0f, -40f);
    public Vector2 handLimitDrawnCardSize = new Vector2(120f, 180f);
    [Min(1f)] public float handLimitSelectedScale = 1.12f;
    public string handLimitDiscardConfirmText = "廃棄する";
    public string handLimitDiscardCancelText = "選び直す";

    [Header("Enemy")]
    public TMPro.TextMeshProUGUI enemyHandCountText;
    public Transform enemyHandArea;
    public Transform enemyWallArea;
    public int enemyWallCount = 5;
    public RectTransform enemyDeckPosition;
    public Image enemyDeckImage;
    public int enemyHandCount = 0;

    int playerWallAliveCount = 0;
    int enemyWallAliveCount = 0;

    [Header("CPU Draw Strength")]
    public bool assistEnemyDraws = true;
    [Range(0f, 1f)] public float enemyDrawAssistChance = 0.55f;
    [Min(1)] public int enemyDrawCandidateCount = 12;
    int openingEnemyDrawsRemaining;

    int SelectEnemyHandDrawIndex()
    {
        int fallback = Random.Range(0, enemyDeck.Count);
        if (openingEnemyDrawsRemaining > 0) { openingEnemyDrawsRemaining--; return fallback; }
        if(!assistEnemyDraws || enemyDrawAssistChance <= 0f || (enemyDrawAssistChance < 1f && Random.value >= enemyDrawAssistChance)) return fallback;
        var brain = turnManager != null ? turnManager.enemyAIBrain : TCardEnemyAIBrain.I;
        if(brain == null) return fallback;
        var indices = new List<int>();
        for(int i = 0; i < enemyDeck.Count; i++) indices.Add(i);
        int count = Mathf.Min(Mathf.Max(1, enemyDrawCandidateCount), indices.Count);
        for(int i = 0; i < count; i++)
        {
            int swap = Random.Range(i, indices.Count);
            int saved = indices[i]; indices[i] = indices[swap]; indices[swap] = saved;
        }
        if(count < indices.Count) indices.RemoveRange(count, indices.Count - count);
        brain.KingRecoveriesAvailable = EnemyKingRecoveryRemaining;
        int selected = brain.SelectAssistedDrawIndex(enemyDeck, enemyHandCards,
            turnManager != null && turnManager.enemyResourceManager != null ? turnManager.enemyResourceManager.maxResource : 0,
            turnManager != null ? turnManager.EnemyWallCountForAI : enemyWallAliveCount,
            turnManager != null ? turnManager.PlayerWallCountForAI : playerWallAliveCount,
            turnManager != null ? TCardAIUnityBridge.GetCards(turnManager.playerBattleArea) : null,
            turnManager != null ? TCardAIUnityBridge.GetCards(turnManager.enemyBattleArea) : null, indices,
            turnManager != null && turnManager.enemyResourceManager != null ? turnManager.enemyResourceManager.currentResource : 0);
        return selected >= 0 && selected < enemyDeck.Count ? selected : fallback;
    }
    [Header("Enemy Q Debug")]
    public bool debugForceEnemyQ = false;

    [Header("Turn")]
    public TurnManager turnManager;

    [Header("Effects")]
    public Sprite targetGlowSprite;
    public Sprite slashSprite;

    [Header("BGM")]
    public AudioSource bgmSource;

    public AudioClip normalBGM;
    public AudioClip advancedBGM;

    public AudioClip dangerBGM;

    [Header("SE")]
    public AudioSource seSource;
    public AudioClip slashSE;

    [Header("Player Danger")]
    public AudioClip playerDangerBGM;
    public AudioClip playerDangerSE;


    [Header("Voice By Difficulty")]
    public AudioClip easyWarningVoice;
    public AudioClip normalWarningVoice;
    public AudioClip hardWarningVoice;

    [Header("Victory")]
    public AudioClip easyEnemyDefeatSE;
    public AudioClip normalEnemyDefeatSE;
    public AudioClip hardEnemyDefeatSE;

    public AudioClip victoryFanfare;
    public GameObject victoryLogo;

    [Header("Defeat")]
    public AudioClip defeatSE;
    public GameObject defeatLogo;

    [Header("Enemy Resource Animation")]
    public RectTransform enemyResourcePosition;
    bool canRedraw = false;
    bool hasRedrawn = false;
    public bool IsHandLimitSelecting { get; private set; } = false;
    public bool IsHandLimitDecisionReady { get; private set; }
    public bool IsOpeningDecisionReady => IsRedrawSelecting && !IsHandLimitSelecting &&
        confirmButton != null && confirmButton.activeInHierarchy &&
        (confirmButton.GetComponent<Button>() == null || confirmButton.GetComponent<Button>().interactable) &&
        !IsWallPlacementRunning;

    public void ResolveHandLimitInactivity()
    {
        if (!IsHandLimitSelecting || !IsHandLimitDecisionReady) return;
        if (SelectedHandLimitDiscardCard == null)
            SelectHandLimitDiscardCard(HandLimitDrawnCardObject);
        ConfirmHandLimitDiscard();
    }
    public CardData HandLimitDrawnCardData { get; private set; }
    public GameObject HandLimitDrawnCardObject { get; private set; }
    public GameObject SelectedHandLimitDiscardCard { get; private set; }
    List<CardData> currentDeck = new List<CardData>();
    List<CardData> enemyDeck = new List<CardData>();
    public List<CardData> enemyHandCards =
    new List<CardData>();

    [Header("Enemy Hand Visual")]
    public Vector2 enemyHandCardSize = new Vector2(35, 52);

    [Header("Opening Lock")]
    public Button endTurnButton;



public Transform playerWallArea;
    OpeningDiscardPresentation openingDiscard;
    void OnEnable()
    {
        if(openingShuffleSettings==null)openingShuffleSettings=Resources.Load<OpeningShuffleSettings>("TCardOpening/OpeningShuffleSettings");
        if(turnManager!=null)
        {
            openingDiscard=GetComponent<OpeningDiscardPresentation>();if(openingDiscard==null)openingDiscard=gameObject.AddComponent<OpeningDiscardPresentation>();
            openingDiscard.Initialize(turnManager.playerGraveyard,turnManager.enemyGraveyard,openingShuffleSettings);
        }
    }
    public void DealStart()
    {
        IsRedrawSelecting = true;

        if(InputLockManager.I != null)
        {
            InputLockManager.I.LockInput();
        }
        if (cardPrefab == null)
        {
            Debug.LogError("HandDealer: CardPrefab が未設定です");
            return;
        }

        if (handArea == null)
        {
            Debug.LogError("HandDealer: HandArea が未設定です");
            return;
        }

        canRedraw = true;
        hasRedrawn = false;

        SetRedrawButtonInteractable(true);

        currentDeck = new List<CardData>(cardList);
        enemyDeck = new List<CardData>(cardList);
        enemyHandCount = 0;
        enemyHandCards.Clear();
        openingEnemyDrawsRemaining = dealCount;

        if (enemyDeckImage != null)
            enemyDeckImage.gameObject.SetActive(true);

        StartCoroutine(DealRoutine(true));
    }

    void Start()
    {
        ApplyRuleBGMClipOnly();

        if(enemyHandCountText != null)
        {
            enemyHandCountText.gameObject.SetActive(false);
        }

        if(enemyDeckImage != null)
        {
            enemyDeckImage.gameObject.SetActive(true);
        }

        // コイントス・初期手札確認が終わるまで非表示
        if(endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(false);
        }

        IsRedrawSelecting = true;

        if(redrawChoicePanel != null)
        {
            redrawChoicePanel.SetActive(false);
        }
    }
    
    void ApplyRuleBGMClipOnly()
    {
        if(bgmSource == null)
            return;

        AudioClip selectedBGM =
            GameSettings.IsAdvancedRule
            ? advancedBGM
            : normalBGM;

        if(selectedBGM == null)
            return;

        bgmSource.clip = selectedBGM;

        Debug.Log(
            "ルールBGM設定のみ：" +
            (GameSettings.IsAdvancedRule ? "Advanced" : "Normal")
        );
    }


    IEnumerator DealRoutine(bool showButtonsAfterDeal, bool includeEnemy=true)
    {
        SetOpeningLock(true);

        if(InputLockManager.I != null)
        {
            InputLockManager.I.LockInput();
        }

        // 1枚目を配る前から、初期手札用の位置とサイズにする
        ApplyOpeningHandLook();

        ClearHand();
        ClearWall();
        HideButtons();

        if(openingShuffleSettings==null)openingShuffleSettings=Resources.Load<OpeningShuffleSettings>("TCardOpening/OpeningShuffleSettings");
        // Finish placing both sides' walls before dealing the opening hand.
        CreateWallCards();
        if(includeEnemy)CreateEnemyWallCards();
        yield return new WaitUntil(() => !IsWallPlacementRunning && !IsHandLimitSelecting);

        if(openingShuffleSettings==null)openingShuffleSettings=Resources.Load<OpeningShuffleSettings>("TCardOpening/OpeningShuffleSettings");
        var opening=openingShuffleSettings;
        bool enemyFinished=!includeEnemy;
        if(includeEnemy)StartCoroutine(OpeningEnemyHand(opening,()=>enemyFinished=true));
        // Draw randomly, then reserve the final sorted layout before any card is shown.
        var drawn=new List<CardData>();
        for(int i=0;i<dealCount && currentDeck.Count>0;i++){var data=DrawRandomCardData();if(data!=null)drawn.Add(data);}
        drawn.Sort((a,b)=>{int rank=GetRankOrder(a.cardName??"").CompareTo(GetRankOrder(b.cardName??""));return rank!=0?rank:a.suit.CompareTo(b.suit);});
        var cards=new List<RectTransform>();
        foreach(var data in drawn)
        {
            var card=CreateHandCard(data);var rect=card.GetComponent<RectTransform>();
            var group=card.GetComponent<CanvasGroup>();if(group==null)group=card.AddComponent<CanvasGroup>();group.alpha=0;
            var layout=card.GetComponent<LayoutElement>();if(layout!=null)layout.ignoreLayout=false;
            cards.Add(rect);
        }
        SortPlayerHand();Canvas.ForceUpdateCanvases();ForceHandLayout();
        var targets=new List<Vector3>();var rotations=new List<Quaternion>();var scales=new List<Vector3>();
        foreach(var card in cards)
        {
            var pro=card.GetComponent<CCP.Card>();
            bool usePro=pro!=null && pro.parentCardGroup==proHandGroup;
            targets.Add(usePro?pro.destinationPosition:card.position);rotations.Add(usePro?pro.destinationRotation:card.rotation);
            scales.Add(usePro?Vector3.one*proHandGroup.GetCardScale():card.localScale);
            card.DOKill();if(pro!=null)pro.isBeingMovedManually=true;
            var layout=card.GetComponent<LayoutElement>();if(layout!=null)layout.ignoreLayout=true;
        }
        int inFlight=0;
        for(int i=0;i<cards.Count;i++)
        {
            inFlight++;
            StartCoroutine(OpeningDealCard(cards[i],targets[i],rotations[i],scales[i],opening,()=>inFlight--));
            if(opening!=null)yield return OpeningWait(opening,opening.handDealInterval);else yield return new WaitForSeconds(dealInterval);
        }
        yield return new WaitUntil(()=>inFlight<=0 && enemyFinished);
        if(opening!=null)yield return OpeningWait(opening,opening.afterDealHold);


        if(showButtonsAfterDeal)
        {
            ShowButtons();
        }
        else
        {
            HideButtons();
        }

        // 初期手札を確定するまで入力ロックを維持する
    }

    IEnumerator OpeningDealCard(RectTransform card,Vector3 target,Quaternion rotation,Vector3 scale,OpeningShuffleSettings settings,System.Action completed)
    {
        bool revealed=settings==null || !settings.revealDuringDeal;
        var pro=card!=null?card.GetComponent<CCP.Card>():null;
        try
        {
            if(card==null)yield break;
            using(var layer=OpeningDealLayer.Begin(card.gameObject,deckPosition,settings))
            {
                card.GetComponent<CanvasGroup>().alpha=1;
                card.rotation=Quaternion.identity;card.localScale=scale*.75f;
                if(deckPosition!=null)
                {
                    var front=pro!=null && pro.front!=null?pro.front.GetComponent<RectTransform>():card;
                    Vector3 center=front!=null?front.TransformPoint(front.rect.center):card.position;
                    card.position+=OpeningShuffleView.DealPosition(deckPosition)+deckPosition.TransformVector(dealStartOffset)-center;
                }
                var start=card.position;
                if(!revealed)StartCoroutine(OpeningReveal(card,settings,()=>revealed=true));
                float elapsed=0,duration=Mathf.Max(.02f,settings!=null?settings.Seconds(settings.handTravelDuration):flyTime);
                while(elapsed<duration && card!=null)
                {
                    float p=Mathf.Clamp01(elapsed/duration),e=1-Mathf.Pow(1-p,3);
                    card.position=Vector3.Lerp(start,target,e);card.rotation=Quaternion.Slerp(Quaternion.identity,rotation,e);card.localScale=Vector3.Lerp(scale*.75f,scale,e);
                    yield return null;elapsed+=settings!=null && settings.useUnscaledTime?Time.unscaledDeltaTime:Time.deltaTime;
                }
                if(card!=null){card.position=target;card.rotation=rotation;card.localScale=scale;}
                yield return new WaitUntil(()=>revealed||card==null);
            }
        }
        finally{if(pro!=null)pro.isBeingMovedManually=false;completed?.Invoke();}
    }
    IEnumerator OpeningReveal(RectTransform card,OpeningShuffleSettings settings,System.Action completed)
    {
        var controller=card!=null?card.GetComponent<CardController>():null;
        var art=controller!=null?controller.artworkImage:null;
        if(art==null||cardBackSprite==null){completed?.Invoke();yield break;}
        Sprite front=controller.data!=null && controller.data.artwork!=null?controller.data.artwork:art.sprite;
        Vector3 original=art.rectTransform.localScale;
        float elapsed=0,delay=settings.Seconds(settings.handFlipDelay),duration=Mathf.Max(.02f,settings.Seconds(settings.handFlipDuration));
        try
        {
            while(elapsed<delay+duration && card!=null && art!=null)
            {
                float p=Mathf.Clamp01((elapsed-delay)/duration),flip=Mathf.Cos(Mathf.PI*Mathf.SmoothStep(0,1,p));
                art.sprite=flip>=0?cardBackSprite:front;
                art.rectTransform.localScale=new Vector3(original.x*Mathf.Max(.028f,Mathf.Abs(flip)),original.y,original.z);
                yield return null;elapsed+=settings.useUnscaledTime?Time.unscaledDeltaTime:Time.deltaTime;
            }
        }
        finally{if(art!=null){art.sprite=front;art.rectTransform.localScale=original;}completed?.Invoke();}
    }
    static IEnumerator OpeningWait(OpeningShuffleSettings settings,float seconds)
    {
        float remaining=settings.Seconds(seconds);
        while(remaining>0){yield return null;remaining-=settings.useUnscaledTime?Time.unscaledDeltaTime:Time.deltaTime;}
    }

    CardData DrawRandomCardData()
    {
        if (currentDeck == null || currentDeck.Count <= 0)
            return null;

        int randomIndex = Random.Range(0, currentDeck.Count);
        CardData selectedCard = currentDeck[randomIndex];
        currentDeck.RemoveAt(randomIndex);

        return selectedCard;
    }
    CardData DrawEnemyRandomCardData()
    {
        if (enemyDeck == null || enemyDeck.Count <= 0)
            return null;

        int randomIndex = Random.Range(0, enemyDeck.Count);

        CardData selectedCard =
            enemyDeck[randomIndex];

        enemyDeck.RemoveAt(randomIndex);

        return selectedCard;
    }

/*    GameObject CreateHandCard(CardData selectedCard)
    {
        GameObject cardObj = Instantiate(cardPrefab, handArea);
        cardObj.name = "HandCard_" + selectedCard.cardName;

        SetupCardSize(cardObj, handCardSize);

        CardController card = cardObj.GetComponent<CardController>();

        if (card != null)
        {
            card.SetData(selectedCard);
        }
        else
        {
            Debug.LogWarning("CardPrefab に CardController がありません");
        }

        return cardObj;
    }*/

    GameObject CreateHandCard(CardData selectedCard)
        {
            bool usePro =
                proHandCardPrefab != null &&
                proHandGroup != null &&
                proHandGroup.transform == handArea;

            GameObject prefab = usePro
                ? proHandCardPrefab
                : cardPrefab;

            GameObject cardObj = Instantiate(prefab, handArea);
            cardObj.name = "HandCard_" + selectedCard.cardName;

            SetupCardSize(cardObj, handCardSize);

            CardController controller =
                cardObj.GetComponent<CardController>();

            if (controller != null)
            {
                controller.SetData(selectedCard);
            }
            else
            {
                Debug.LogError(
                    "手札PrefabにCardControllerがありません",
                    cardObj
                );
            }

            if (usePro)
            {
                CCP.Card proCard = cardObj.GetComponent<CCP.Card>();

                if (proCard != null)
                {
                    // 今回は表示だけ接続。入力は次の段階で有効化する。
                    //proCard.enabled = false;
                    proCard.enabled = true;
                    proHandGroup.AddBack(proCard);
                }
                else
                {
                    Debug.LogError(
                        "Pro用PrefabにCCP.Cardがありません",
                        cardObj
                    );
                }
            }

            CheckHandLimit(cardObj);
            return cardObj;
        }

    void SetupCardSize(GameObject cardObj, Vector2 size)
    {
        RectTransform rt = cardObj.GetComponent<RectTransform>();

        if (rt != null)
        {
            rt.sizeDelta = size;
        }

        LayoutElement layout = cardObj.GetComponent<LayoutElement>();

        if (layout == null)
            layout = cardObj.AddComponent<LayoutElement>();

        layout.ignoreLayout = true;
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        layout.minWidth = size.x;
        layout.minHeight = size.y;
    }
/*IEnumerator AnimateCardToHand(RectTransform cardRect,float durationOverride=-1)
{
    if (cardRect == null)
        yield break;

        CCP.Card proCard = cardRect.GetComponent<CCP.Card>();

    if (proCard != null &&
        proHandGroup != null &&
        proCard.parentCardGroup == proHandGroup)
    {
        // 配布元の位置から、Proが計算した配置へ移動する。
        proHandGroup.RecalculateAndAnimatePositions();

        yield return new WaitForSeconds(
            Mathf.Max(0f, flyTime)
        );

        yield break;
    }

    Vector2 start = cardRect.anchoredPosition;
    Vector2 end = Vector2.zero;

    float t = 0f;

    while (t < flyTime)
    {
        if (cardRect == null)
            yield break;

        t += Time.deltaTime;
        float rate = Mathf.Clamp01(t / flyTime);

        cardRect.anchoredPosition =
            Vector2.Lerp(start, end, rate);

        yield return null;
    }

    if (cardRect != null)
        cardRect.anchoredPosition = end;
}*/

IEnumerator AnimateCardToHand(RectTransform cardRect,float durationOverride=-1)
{
    if (cardRect == null)
        yield break;

    using(var layer=OpeningDealLayer.Begin(cardRect.gameObject,deckPosition,openingShuffleSettings))
    {
    CCP.Card proCard = cardRect.GetComponent<CCP.Card>();

    if (proCard != null &&
        proHandGroup != null &&
        proCard.parentCardGroup == proHandGroup)
    {
        // 手札の最終配置を計算する。
        if (handController != null &&
            handController.proHandGroup == proHandGroup)
        {
            handController.RefreshProLayout();
        }
        else
        {
            proHandGroup.RecalculateAndAnimatePositions();
        }

        Vector3 targetPosition = proCard.destinationPosition;
        Quaternion targetRotation = proCard.destinationRotation;
        float targetScale = proHandGroup.GetCardScale();

        // 出発時の角度・サイズ。
        cardRect.localRotation = Quaternion.identity;
        cardRect.localScale =
            Vector3.one * targetScale * 0.75f;

        if (deckPosition != null)
        {
            // 山札画像の中心。
            Vector2 startCenter =
                deckPosition.rect.center + dealStartOffset;

            Vector3 deckCenter = OpeningShuffleView.DealPosition(deckPosition)+deckPosition.TransformVector(dealStartOffset);

            RectTransform frontRect = proCard.front != null
                ? proCard.front.GetComponent<RectTransform>()
                : null;

            // カード表面画像の中心。
            Vector3 cardCenter = frontRect != null
                ? frontRect.TransformPoint(frontRect.rect.center)
                : cardRect.TransformPoint(cardRect.rect.center);

            // 画像同士の中心を合わせる。
            cardRect.position += deckCenter - cardCenter;
        }

        float duration = Mathf.Max(0.01f, durationOverride>=0?durationOverride:flyTime);

        // CCPで位置・角度・サイズをアニメーション。
        proCard.MoveToPosition(
            targetPosition,
            targetRotation,
            DG.Tweening.Ease.OutCubic,
            duration,
            targetScale
        );

        yield return new WaitForSeconds(duration);
        yield break;
    }

    // 旧Prefabを使う場合の配布処理。
    Vector2 start = cardRect.anchoredPosition;
    Vector2 end = Vector2.zero;

    float elapsed = 0f;
    float legacyDuration = Mathf.Max(0.01f, durationOverride>=0?durationOverride:flyTime);

    while (elapsed < legacyDuration)
    {
        if (cardRect == null)
            yield break;

        elapsed += Time.deltaTime;

        cardRect.anchoredPosition = Vector2.Lerp(
            start,
            end,
            Mathf.Clamp01(elapsed / legacyDuration)
        );

        yield return null;
    }

    if (cardRect != null)
        cardRect.anchoredPosition = end;
    }
}

    Vector2 WorldToLocalPosition(RectTransform parent, Vector3 worldPosition)
    {
        if (parent == null)
            return Vector2.zero;

        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(null, worldPosition);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            screenPoint,
            null,
            out Vector2 localPoint
        );

        return localPoint;
    }

    public bool IsWallPlacementRunning =>
        (wallArea != null && wallArea.GetComponent<WallAreaLayout>() != null &&
         wallArea.GetComponent<WallAreaLayout>().IsAnimating) ||
        (enemyWallArea != null && enemyWallArea.GetComponent<WallAreaLayout>() != null &&
         enemyWallArea.GetComponent<WallAreaLayout>().IsAnimating);

    WallAreaLayout GetWallLayout(Transform area)
    {
        if (area == null) return null;
        WallAreaLayout layout = area.GetComponent<WallAreaLayout>();
        return layout != null ? layout : area.gameObject.AddComponent<WallAreaLayout>();
    }

    void CreateWallCards()
    {
        playerWallAliveCount = 0;
        playerKingWallsAdded = 0;
        WallAreaLayout layout = GetWallLayout(wallArea);
        if (layout == null || cardPrefab == null || cardBackSprite == null) return;
        layout.ClearWalls();
        layout.ConfigureOpeningDeal(deckPosition,openingShuffleSettings);
        layout.BeginOpeningPlacement();
        for (int i = 0; i < wallCount && layout.HasSpace; i++)
        {
            CardData data = DrawRandomCardData();
            if (data == null) break;
            if (CreateWallCard(data, false, false)) playerWallAliveCount++;
            else currentDeck.Add(data);
        }
        layout.EndOpeningPlacement();
    }

    void CreateEnemyWallCards()
    {
        enemyWallAliveCount = 0;
        enemyKingWallsAdded = 0;
        WallAreaLayout layout = GetWallLayout(enemyWallArea);
        if (layout == null || cardPrefab == null || cardBackSprite == null) return;
        layout.ClearWalls();
        layout.ConfigureOpeningDeal(enemyDeckPosition,openingShuffleSettings);
        layout.BeginOpeningPlacement();
        for (int i = 0; i < enemyWallCount && layout.HasSpace; i++)
        {
            CardData data = DrawEnemyRandomCardData();
            if (data == null) break;
            if (CreateWallCard(data, true, false)) enemyWallAliveCount++;
            else enemyDeck.Add(data);
        }
        layout.EndOpeningPlacement();
    }

    bool CreateWallCard(CardData data, bool enemy, bool fromKing)
    {
        Transform area = enemy ? enemyWallArea : wallArea;
        WallAreaLayout layout = GetWallLayout(area);
        if (data == null || layout == null || !layout.HasSpace ||
            cardPrefab == null || cardBackSprite == null) return false;
        GameObject wall = Instantiate(cardPrefab, area);
        wall.name = (enemy ? "EnemyWallCard_" : "PlayerWallCard_") + data.cardName;
        SetupCardSize(wall, layout.CardSize);
        DisableWallInput(wall);
        CardController controller = wall.GetComponent<CardController>();
        if (controller == null)
        {
            Destroy(wall);
            Debug.LogError("Wall prefab requires CardController");
            return false;
        }
        controller.SetData(data);
        if (controller.artworkImage != null) controller.artworkImage.sprite = cardBackSprite;
        if (controller.costText != null) controller.costText.gameObject.SetActive(false);
        if (controller.attackText != null) controller.attackText.gameObject.SetActive(false);
        if (controller.hpText != null) controller.hpText.gameObject.SetActive(false);
        controller.enabled = false;

        GameObject slash = new GameObject("SlashEffect", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        slash.layer = wall.layer;
        slash.transform.SetParent(wall.transform, false);
        RectTransform slashRect = slash.GetComponent<RectTransform>();
        slashRect.anchorMin = Vector2.zero;
        slashRect.anchorMax = Vector2.one;
        slashRect.offsetMin = new Vector2(-120f, -120f);
        slashRect.offsetMax = new Vector2(120f, 120f);
        slashRect.localRotation = Quaternion.Euler(0f, 0f, -35f);
        Image slashImage = slash.GetComponent<Image>();
        slashImage.sprite = slashSprite;
        slashImage.preserveAspect = true;
        slashImage.raycastTarget = false;
        slash.GetComponent<CanvasGroup>().alpha = 0f;
        if (enemy)
        {
            if (wall.GetComponent<EnemyWallClick>() == null) wall.AddComponent<EnemyWallClick>();
            GameObject glow = new GameObject("TargetGlow", typeof(RectTransform), typeof(Image));
            glow.layer = wall.layer;
            glow.transform.SetParent(wall.transform, false);
            RectTransform rt = glow.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-16f, -16f);
            rt.offsetMax = new Vector2(16f, 16f);
            Image image = glow.GetComponent<Image>();
            image.sprite = targetGlowSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.65f, 1f, 0.35f, 1f);
            image.raycastTarget = false;
            glow.SetActive(false);
        }
        if (layout.AddWall(wall, fromKing)) return true;
        wall.SetActive(false);
        Destroy(wall);
        return false;
    }
  /*  void ClearHand()
    {
        if (handArea == null) return;

        for (int i = handArea.childCount - 1; i >= 0; i--)
        {
            Destroy(handArea.GetChild(i).gameObject);
        }
    }*/
    void ClearHand()
    {
        if (handArea == null)
            return;

        if (proHandGroup != null &&
            proHandGroup.transform == handArea)
        {
            for (int i = proHandGroup.cards.Count - 1; i >= 0; i--)
            {
                CCP.Card card = proHandGroup.cards[i];

                if (card != null)
                    proHandGroup.RemoveCard(card);
                else
                    proHandGroup.cards.RemoveAt(i);
            }
        }

        for (int i = handArea.childCount - 1; i >= 0; i--)
        {
            GameObject cardObj = handArea.GetChild(i).gameObject;
            // Removing CCP cards relayouts the remainder; stop those tweens before destruction.
            foreach(Transform part in cardObj.GetComponentsInChildren<Transform>(true))part.DOKill();
            cardObj.SetActive(false);
            cardObj.transform.SetParent(null, false);
            Destroy(cardObj);
        }
    }
    void ClearWall()
    {
        GetWallLayout(wallArea)?.ClearWalls();
    }

    void ForceHandLayout()
    {
        RectTransform handRect = handArea as RectTransform;

        if (handRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(handRect);
        }
    }

void ShowButtons()
{
    if(redrawChoicePanel != null)
    {
        redrawChoicePanel.SetActive(true);
        redrawChoicePanel.transform.SetAsLastSibling();
    }

    if(handController != null)
    {
        handController.transform.SetAsLastSibling();
    }

    if(redrawButton != null)
    {
        redrawButton.SetActive(true);

        CanvasGroup cg =
            redrawButton.GetComponent<CanvasGroup>();

        if(cg == null)
            cg = redrawButton.AddComponent<CanvasGroup>();

        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;
    }

    if(confirmButton != null)
    {
        confirmButton.SetActive(true);

        CanvasGroup cg =
            confirmButton.GetComponent<CanvasGroup>();

        if(cg == null)
            cg = confirmButton.AddComponent<CanvasGroup>();

        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;
    }
}

    void HideButtons()
    {
        
        if(redrawChoicePanel != null)
        {
            redrawChoicePanel.SetActive(false);
        }

if (redrawButton != null)
            redrawButton.SetActive(false);

        if (confirmButton != null)
            confirmButton.SetActive(false);
    }


    void ApplyOpeningHandLook()
    {
        if(handController == null)
        {
            Debug.LogWarning(
                "HandDealer: HandController が未設定です"
            );
            return;
        }

        handController.ApplyOpeningIdleLook(
            openingHandScale,
            openingHandYOffset
        );
    }

    void RestoreOpeningHandLook()
    {
        if(handController == null)
            return;

        handController.RestoreNormalIdleLook();
    }

    void SetRedrawButtonInteractable(bool value)
    {
        if (redrawButton == null) return;

        Button btn = redrawButton.GetComponent<Button>();

        if (btn != null)
            btn.interactable = value;
    }

    public static bool IsRedrawSelecting = false;

    public void RedrawHand()
    {
        if (IsHandLimitSelecting)
        {
            CancelHandLimitDiscardSelection();
            return;
        }

        if(!canRedraw)
            return;

        if(hasRedrawn)
            return;

        if (turnManager != null && turnManager.RequestPlayerDecision("redraw-hand",
            "初期手札を引き直します。\nよろしいですか？", RedrawHand)) return;

        IsRedrawSelecting = true;
        hasRedrawn = true;
        canRedraw = false;

        SetRedrawButtonInteractable(false);

        if(confirmButton != null)
        {
            Button confirmBtn =
                confirmButton.GetComponent<Button>();

            if(confirmBtn != null)
                confirmBtn.interactable = false;
        }

        if(endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(false);
        }

        if(InputLockManager.I != null)
        {
            InputLockManager.I.LockInput();
        }

        SetOpeningLock(true);

        StartCoroutine(RedrawRoutine());
    }

    IEnumerator RedrawRoutine()
    {
        if(handArea==null || cardPrefab==null)yield break;
        HideButtons();
        ReturnOpeningCards(handArea,currentDeck);
        ReturnOpeningCards(wallArea,currentDeck);
        ClearHand();ClearWall();
        yield return null;
        var shuffle=deckPosition!=null?deckPosition.GetComponent<DeckShuffle>():null;
        if(shuffle==null)shuffle=FindFirstObjectByType<DeckShuffle>();
        if(shuffle!=null)yield return shuffle.ReplayShuffle(false);
        yield return DealRoutine(false,false);
        CompleteOpeningSelection();
    }
    static void ReturnOpeningCards(Transform area,List<CardData> deck)
    {
        if(area==null || deck==null)return;
        foreach(Transform child in area)
        {
            var card=child.GetComponent<CardController>();
            if(card!=null && card.data!=null && child.gameObject.activeSelf)deck.Add(card.data);
        }
    }

    IEnumerator OpeningEnemyHand(OpeningShuffleSettings settings,System.Action completed)
    {
        var layouts=enemyHandArea!=null?enemyHandArea.GetComponents<LayoutGroup>():new LayoutGroup[0];
        var wasEnabled=new bool[layouts.Length];
        var dealLayers=new List<OpeningDealLayer>();
        for(int i=0;i<layouts.Length;i++)wasEnabled[i]=layouts[i].enabled;
        try
        {
            for(int i=0;i<dealCount && enemyDeck.Count>0 && enemyHandCards.Count<maxHandSize;i++)
            {
                int index=SelectEnemyHandDrawIndex();
                if(i==0 && debugForceEnemyQ){int q=enemyDeck.FindIndex(c=>c!=null && c.cardName.Contains("Q"));if(q>=0)index=q;}
                enemyHandCards.Add(enemyDeck[index]);enemyDeck.RemoveAt(index);
            }
            enemyHandCount=enemyHandCards.Count;RefreshEnemyHandVisual();UpdateEnemyHandCountText();
            if(enemyHandArea==null)yield break;
            Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(enemyHandArea as RectTransform);
            var cards=new List<RectTransform>();var targets=new List<Vector3>();
            foreach(Transform child in enemyHandArea)
            {
                if(!child.gameObject.activeSelf || child.GetComponent<CardController>()==null)continue;
                cards.Add((RectTransform)child);targets.Add(child.position);child.GetComponent<CanvasGroup>().alpha=0;
                dealLayers.Add(OpeningDealLayer.Begin(child.gameObject,enemyDeckPosition,settings));
            }
            for(int i=0;i<layouts.Length;i++){wasEnabled[i]=layouts[i].enabled;layouts[i].enabled=false;}
            float travel=settings!=null?settings.Seconds(settings.handTravelDuration):flyTime;
            float interval=settings!=null?settings.Seconds(settings.handDealInterval):dealInterval;
            Vector3 start=enemyDeckPosition!=null?OpeningShuffleView.DealPosition(enemyDeckPosition):enemyHandArea.position;
            float elapsed=0,total=Mathf.Max(0,cards.Count-1)*interval+Mathf.Max(.02f,travel);
            while(elapsed<total)
            {
                for(int i=0;i<cards.Count;i++)
                {
                    if(cards[i]==null)continue;
                    float p=Mathf.Clamp01((elapsed-i*interval)/Mathf.Max(.02f,travel));
                    cards[i].GetComponent<CanvasGroup>().alpha=elapsed>=i*interval?1:0;
                    cards[i].position=Vector3.Lerp(start,targets[i],Mathf.SmoothStep(0,1,p));
                    cards[i].localScale=Vector3.one*Mathf.Lerp(.92f,1,p);
                }
                yield return null;elapsed+=settings!=null && settings.useUnscaledTime?Time.unscaledDeltaTime:Time.deltaTime;
            }
            for(int i=0;i<cards.Count;i++)if(cards[i]!=null){cards[i].position=targets[i];cards[i].localScale=Vector3.one;cards[i].GetComponent<CanvasGroup>().alpha=1;}
        }
        finally{foreach(var layer in dealLayers)layer?.Dispose();for(int i=0;i<layouts.Length;i++)if(layouts[i]!=null)layouts[i].enabled=wasEnabled[i];completed?.Invoke();}
    }

    public void ConfirmHand()
    {
        if (IsHandLimitSelecting)
        {
            ConfirmHandLimitDiscard();
            return;
        }

        if (turnManager != null && turnManager.RequestPlayerDecision("confirm-hand",
            "この手札でゲームを開始します。\nよろしいですか？", ConfirmHand)) return;
        CompleteOpeningSelection();
    }

    void CompleteOpeningSelection()
    {
        RestoreOpeningHandLook();

        IsRedrawSelecting = false;
        HideButtons();
        canRedraw = false;

        SetOpeningLock(false);
        if(openingDiscard!=null)openingDiscard.Reveal();

        if(InputLockManager.I != null)
        {
            InputLockManager.I.UnlockInput();
        }

        if(endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(true);
        }

        if(turnManager != null)
        {
            GameFlowManager flow =
                FindFirstObjectByType<GameFlowManager>();

            bool playerFirst = true;

            if(flow != null)
                playerFirst = flow.PlayerFirst;

            Debug.Log("PlayerFirst = " + playerFirst);

            turnManager.StartFirstTurn(playerFirst);

            Debug.Log(
                "Opening selection completed. PlayerFirst = " +
                playerFirst
            );
        }
    }

    public IEnumerator DrawTurnStartCard()
    {
        if (IsTurnDrawAnimating || currentDeck == null || currentDeck.Count == 0 || handArea == null)
            yield break;
        CardData data = DrawRandomCardData();
        if (data == null) yield break;
        if (cardPrefab == null) { currentDeck.Add(data); yield break; }
        IsTurnDrawAnimating = true;
        GameObject preview = null;
        CanvasGroup handVisibility = null;
        float savedAlpha = 1f;
        OpeningDealLayer drawLayer=null;
        try
        {
            preview = Instantiate(cardPrefab, handArea.parent != null ? handArea.parent : transform);
            DisableWallInput(preview);
            drawLayer=OpeningDealLayer.Begin(preview,deckPosition,openingShuffleSettings);
            preview.AddComponent<TurnDrawHighlightEffect>().Initialize(this);
            yield return AnimateTurnDrawReveal(preview, data);

            GameObject handCard = CreateHandCard(data);
            handVisibility = handCard.GetComponent<CanvasGroup>();
            if (handVisibility == null) handVisibility = handCard.AddComponent<CanvasGroup>();
            savedAlpha = handVisibility.alpha;
            handVisibility.alpha = 0f;
            SortPlayerHand();
            // Calculate the real hand destination while the temporary card stays visible.
            yield return null;
            RectTransform rect = preview.GetComponent<RectTransform>();
            CardController controller = handCard.GetComponent<CardController>();
            RectTransform destination = controller != null && controller.artworkImage != null
                ? controller.artworkImage.rectTransform : handCard.GetComponent<RectTransform>();
            if (rect != null && destination != null)
            {
                Vector3 start = rect.position;
                Vector3 startScale = rect.localScale;
                float elapsed = 0f;
                float duration = Mathf.Max(0.01f, turnDrawHandTime);
                while (elapsed < duration && destination != null)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    rect.position = Vector3.Lerp(start, destination.TransformPoint(destination.rect.center), eased);
                    rect.rotation = Quaternion.Slerp(Quaternion.identity, destination.rotation, eased);
                    float width = destination.rect.width * destination.lossyScale.x;
                    float height = destination.rect.height * destination.lossyScale.y;
                    Vector3 sizeScale = new Vector3(width / Mathf.Max(1f, rect.rect.width * rect.parent.lossyScale.x),
                        height / Mathf.Max(1f, rect.rect.height * rect.parent.lossyScale.y), 1f);
                    rect.localScale = Vector3.Lerp(startScale, sizeScale, eased);
                    preview.GetComponent<TurnDrawHighlightEffect>().SetTime(turnDrawTravelTime+turnDrawFlipTime+turnDrawRevealHoldTime+elapsed);
                    yield return null;
                }
            }
            LayoutElement layout = handCard.GetComponent<LayoutElement>();
            if (layout != null) layout.ignoreLayout = false;
            SortPlayerHand();
        }
        finally
        {
            if (handVisibility != null) handVisibility.alpha = savedAlpha;
            drawLayer?.Dispose();
            if (preview != null) Destroy(preview);
            IsTurnDrawAnimating = false;
        }
        yield return new WaitUntil(() => !IsHandLimitSelecting);
    }

    IEnumerator AnimateTurnDrawReveal(GameObject preview, CardData data)
    {
        SetupCardSize(preview, turnDrawRevealSize*Mathf.Max(.1f,turnDrawRevealScale));
        var highlight=preview.GetComponent<TurnDrawHighlightEffect>();
        CardController controller = preview.GetComponent<CardController>();
        if (controller != null)
        {
            controller.SetData(data);
            controller.enabled = false;
        }
        RectTransform rect = preview.GetComponent<RectTransform>();
        if (rect == null) yield break;
        Image artwork = controller != null ? controller.artworkImage : null;
        if (artwork != null) artwork.sprite = cardBackSprite;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        preview.transform.SetAsLastSibling();
        Vector3 start = deckPosition != null ? OpeningShuffleView.DealPosition(deckPosition) : rect.position;
        RectTransform wallRect = wallArea as RectTransform;
        Vector3 center = wallRect != null ? wallRect.TransformPoint(wallRect.rect.center)
            : wallArea != null ? wallArea.position : handArea.position;
        Vector3 end = center + rect.parent.TransformVector(turnDrawRevealOffset+Vector2.up*turnDrawRevealLift);
        rect.position = start;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, turnDrawTravelTime);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.position = Vector3.Lerp(start, end, 1f - Mathf.Pow(1f - t, 3f));
            if(highlight!=null)highlight.SetTime(elapsed);
            yield return null;
        }
        rect.position = end;
        elapsed = 0f;
        duration = Mathf.Max(0.01f, turnDrawFlipTime);
        bool revealed = false;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (!revealed && t >= 0.5f)
            {
                if (artwork != null) artwork.sprite = data.artwork;
                revealed = true;
            }
            rect.localScale = new Vector3(Mathf.Max(.018f,Mathf.Abs(Mathf.Cos(t * Mathf.PI))), 1f, 1f);
            if(highlight!=null)highlight.SetTime(turnDrawTravelTime+elapsed);
            yield return null;
        }
        if (artwork != null) artwork.sprite = data.artwork;
        rect.localScale = Vector3.one;
        float holdTime = Mathf.Max(0f, turnDrawRevealHoldTime);
        elapsed=0;
        while(elapsed<holdTime)
        {
            if(highlight!=null)highlight.SetTime(turnDrawTravelTime+turnDrawFlipTime+elapsed);
            yield return null;elapsed+=Time.deltaTime;
        }
    }

    public void DrawOneCard()
    {
        if (currentDeck == null || currentDeck.Count <= 0)
        {
            Debug.Log("山札なし");
            return;
        }


        CardData selectedCard =
            DrawRandomCardData();

        if (selectedCard == null)
            return;

        GameObject cardObj =
            CreateHandCard(selectedCard);

        LayoutElement layout =
            cardObj.GetComponent<LayoutElement>();

        if(layout != null)
        {
            layout.ignoreLayout = false;
        }

        //ForceHandLayout();
        SortPlayerHand();
    }

    public int HandLimitChoiceRevision { get; private set; }
    readonly List<GameObject> handLimitAdditions = new List<GameObject>();

    void CheckHandLimit(GameObject addedCard)
    {
        if (turnManager != null && turnManager.IsBattleEnded) return;
        if (addedCard != null && !handLimitAdditions.Contains(addedCard)) handLimitAdditions.Add(addedCard);
        if (IsHandLimitSelecting)
        {
            HandLimitDrawnCardObject = addedCard;
            if (IsHandLimitDecisionReady)
            {
                IsHandLimitDecisionReady = false;
                SelectedHandLimitDiscardCard = null;
                if (turnManager != null) turnManager.CloseHandLimitChoice();
            }
            return;
        }
        if (TCardAIUnityBridge.GetCards(handArea).Count <= Mathf.Max(0, maxHandSize))
        {
            handLimitAdditions.Clear();
            return;
        }
        IsHandLimitSelecting = true;
        IsHandLimitDecisionReady = false;
        HandLimitDrawnCardObject = addedCard;
        StartCoroutine(ResolveHandOverflow());
    }

    IEnumerator ResolveHandOverflow()
    {
        // Coalesce simultaneous additions and finish the normal turn-draw reveal.
        yield return null;
        while (IsTurnDrawAnimating) yield return null;
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        while (turnManager != null && !turnManager.IsBattleEnded)
        {
            var cards = TCardAIUnityBridge.GetCards(handArea);
            if (cards.Count <= Mathf.Max(0, maxHandSize)) break;
            // Other mandatory choices finish first; never stack interactive modals.
            while (turnManager.IsChoosingEnemyHand || turnManager.IsPlayerDecisionOpen ||
                (ReverseChoiceManager.I != null && ReverseChoiceManager.I.IsChoosing))
            {
                if (turnManager.IsBattleEnded) break;
                yield return null;
            }
            if (turnManager.IsBattleEnded) break;
            cards = TCardAIUnityBridge.GetCards(handArea);
            if (cards.Count <= Mathf.Max(0, maxHandSize)) break;
            if (HandLimitDrawnCardObject == null || !HandLimitDrawnCardObject.transform.IsChildOf(handArea))
            {
                HandLimitDrawnCardObject = null;
                for (int i = handLimitAdditions.Count - 1; i >= 0; i--)
                    if (handLimitAdditions[i] != null && handLimitAdditions[i].transform.IsChildOf(handArea))
                    { HandLimitDrawnCardObject = handLimitAdditions[i]; break; }
                if (HandLimitDrawnCardObject == null) HandLimitDrawnCardObject = cards[cards.Count - 1].gameObject;
            }
            HandLimitDrawnCardData = HandLimitDrawnCardObject.GetComponent<CardController>().data;
            SelectedHandLimitDiscardCard = null;
            HandLimitChoiceRevision++;
            turnManager.ShowHandLimitChoice(cards);
            IsHandLimitDecisionReady = true;
            yield return new WaitUntil(() => !IsHandLimitDecisionReady || turnManager == null || turnManager.IsBattleEnded);
        }
        CancelHandLimitForBattleEnd();
        SortPlayerHand();
    }

    public void SelectHandLimitDiscardCard(GameObject cardObject)
    {
        if (!IsHandLimitSelecting || !IsHandLimitDecisionReady || cardObject == null ||
            !cardObject.transform.IsChildOf(handArea) || cardObject.GetComponent<CardController>() == null) return;
        SelectedHandLimitDiscardCard = cardObject;
        if (turnManager != null) turnManager.UpdateHandLimitChoiceSelection(cardObject);
    }

    public void CancelHandLimitDiscardSelection()
    {
        if (!IsHandLimitDecisionReady) return;
        SelectedHandLimitDiscardCard = null;
        if (turnManager != null) turnManager.UpdateHandLimitChoiceSelection(null);
    }

    public void ConfirmHandLimitDiscard()
    {
        if (!IsHandLimitSelecting || !IsHandLimitDecisionReady || SelectedHandLimitDiscardCard == null ||
            turnManager == null || turnManager.IsBattleEnded ||
            !SelectedHandLimitDiscardCard.transform.IsChildOf(handArea)) return;
        // Atomically disable this choice before mutating the real hand.
        IsHandLimitDecisionReady = false;
        GameObject discarded = SelectedHandLimitDiscardCard;
        turnManager.CloseHandLimitChoice();
        turnManager.DiscardPlayerHandCard(discarded.GetComponent<CardController>());
        // A missing graveyard uses deferred Destroy; detach immediately for counting.
        if (discarded != null && discarded.transform.IsChildOf(handArea))
        {
            discarded.transform.SetParent(null, false);
            discarded.SetActive(false);
        }
        SelectedHandLimitDiscardCard = null;
        handLimitAdditions.Remove(discarded);
        if (HandLimitDrawnCardObject == discarded)
        {
            HandLimitDrawnCardObject = null;
            HandLimitDrawnCardData = null;
        }
    }

    public void CancelHandLimitForBattleEnd()
    {
        handLimitAdditions.Clear();
        IsHandLimitDecisionReady = false;
        IsHandLimitSelecting = false;
        SelectedHandLimitDiscardCard = null;
        HandLimitDrawnCardObject = null;
        HandLimitDrawnCardData = null;
        if (turnManager != null) turnManager.CloseHandLimitChoice();
    }

    public void DrawOneCardToPlayerHand()
    {
        DrawOneCard();
    }


    IEnumerator EnemyDrawAnimation(int amount) { yield return EnemyFlowDraw(amount); }

    public void UpdateEnemyHandCountText()
    {
        if(enemyHandCountText != null)
        {
            enemyHandCountText.gameObject.SetActive(false);
        }
        if (enemyHandCountText == null)
            return;

        enemyHandCountText.enableAutoSizing = false;

        enemyHandCountText.text =
            enemyHandCount.ToString();

        if (enemyHandCount >= 10)
            enemyHandCountText.fontSize = 17;
        else
            enemyHandCountText.fontSize = 30;
    }
    public void EnemyDraw(int amount)
    {
        if(enemyDeck==null)
            return;

        if(enemyDeck.Count<=0)
            return;

        StartCoroutine(
            EnemyDrawAnimation(amount)
        );
    }

bool allowEnemyWallDamage = false;
readonly HashSet<GameObject> resolvingEnemyWalls = new HashSet<GameObject>();
public bool IsResolvingEnemyWallAttack => resolvingEnemyWalls.Count > 0;

public void DamageEnemyWallFromAttack(
    GameObject targetWall
)
{
    allowEnemyWallDamage = true;

    DamageEnemyWall(targetWall);
}

public void DamageEnemyWall(
    GameObject targetWall
)
{
    if(!allowEnemyWallDamage)
    {
        Debug.Log("正規攻撃ではないので敵Wall破壊しない");
        return;
    }

    allowEnemyWallDamage = false;

    StartCoroutine(
        DamageEnemyWallRoutine(
            targetWall
        )
    );
}
    public bool IsEnemyWallZero()
    {
        return enemyWallAliveCount <= 0;
    }
    IEnumerator DamageEnemyWallRoutine(
        GameObject targetWall
    )
    {
        if(targetWall == null || enemyWallArea == null ||
           targetWall.transform.parent != enemyWallArea ||
           !targetWall.activeInHierarchy || !resolvingEnemyWalls.Add(targetWall))
            yield break;

        try
        {
            yield return DamageEnemyWallCore(targetWall);
        }
        finally
        {
            resolvingEnemyWalls.Remove(targetWall);
        }
    }

    IEnumerator DamageEnemyWallCore(GameObject targetWall)
    {
        if(targetWall == null)
            yield break;

        CardController wallCard = targetWall.GetComponent<CardController>();
        if(wallCard == null || wallCard.data == null)
        {
            Debug.LogWarning("敵ウォールのカードデータがありません", targetWall);
            yield break;
        }
        CardData brokenCard = wallCard.data;

        CanvasGroup cg =
            targetWall.GetComponent<CanvasGroup>();

        if(cg != null && cg.alpha <= 0)
            yield break;

        // 切り裂き演出
        yield return PlayWallSlash(
            targetWall
        );

        if(targetWall == null)
            yield break;

        enemyHandCards.Add(brokenCard);
        enemyHandCount = enemyHandCards.Count;
        RefreshEnemyHandVisual();
        UpdateEnemyHandCountText();

        if(cg == null)
        {
            cg =
                targetWall.AddComponent
                <CanvasGroup>();
        }

        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        enemyWallAliveCount--;

        Debug.Log(
            "敵ウォール残り：" +
            enemyWallAliveCount
        );

        if(enemyWallAliveCount <= 0)
        {
            // 今のBGM停止
            if(bgmSource != null)
            {
                bgmSource.Stop();
            }

            // 静寂
            yield return new WaitForSeconds(0.5f);

            AudioClip voice =
                GetWarningVoiceByDifficulty();

            if(seSource != null &&
            voice != null)
            {
                seSource.PlayOneShot(
                    voice
                );
            }

            // 待機
            yield return new WaitForSeconds(1f);

            // BGM変更
            ChangeDangerBGM();
        }
    }
    public bool SuppressAttackWallSlash {get;set;}
    public IEnumerator PlayWallSlash(
    GameObject targetWall
    )
    {
        if(SuppressAttackWallSlash)yield break;
        Debug.Log("Slash再生開始");
            if(seSource != null && slashSE != null)
    {
        seSource.PlayOneShot(slashSE);
    }
        Transform slash =
            targetWall.transform.Find(
                "SlashEffect"
            );

        if (slash == null)
            yield break;

        CanvasGroup cg =
            slash.GetComponent<CanvasGroup>();

        RectTransform rt =
            slash.GetComponent<RectTransform>();

        cg.alpha = 1f;

        rt.localScale =
            Vector3.zero;

        float t=0;

    while(t<0.35f)
    {
        t += Time.deltaTime;

        float rate =
            t/0.35f;

        // 大きく
        rt.localScale =
            Vector3.Lerp(
                Vector3.one*0.4f,
                Vector3.one*2.8f,
                rate
            );

        // 最初ほぼ消さない
        if(rate < 0.7f)
            cg.alpha = 1f;
        else
            cg.alpha =
                Mathf.Lerp(
                    1f,
                    0f,
                    (rate-0.7f)/0.3f
                );

        yield return null;
    }

        cg.alpha = 0f;
    }

    void ChangeDangerBGM()
    {
        if(bgmSource == null)
            return;

        if(dangerBGM == null)
            return;

        bgmSource.clip = dangerBGM;
        bgmSource.Play();

        Debug.Log("BGM変更");
    }

    AudioClip GetWarningVoiceByDifficulty()
    {
        GameFlowManager flow =
            FindFirstObjectByType<GameFlowManager>();

        if(flow == null)
            return normalWarningVoice;

        if(flow.selectedDifficulty == GameFlowManager.Difficulty.Easy)
            return easyWarningVoice;

        if(flow.selectedDifficulty == GameFlowManager.Difficulty.Hard)
            return hardWarningVoice;

        return normalWarningVoice;
    }

    public void StartVictorySequence()
    {
        StartCoroutine(VictorySequenceRoutine());
    }
    public void StartDefeatSequence()
    {
        StartCoroutine(DefeatSequenceRoutine());
    }

    IEnumerator DefeatSequenceRoutine()
    {
        if(bgmSource != null)
        {
            bgmSource.Stop();
        }

        if(seSource != null && defeatSE != null)
        {
            seSource.PlayOneShot(defeatSE);
        }

        yield return new WaitForSecondsRealtime(0.3f);

        if(defeatLogo != null)
        {
            defeatLogo.SetActive(true);
        }

        Debug.Log("敗北演出開始");
    }
    IEnumerator VictorySequenceRoutine()
    {
        if(bgmSource != null)
        {
            bgmSource.Stop();
        }

        AudioClip defeatSE =
            GetEnemyDefeatSEByDifficulty();

        if(seSource != null && defeatSE != null)
        {
            seSource.PlayOneShot(defeatSE);
        }

        yield return new WaitForSecondsRealtime(1.2f);

        if(seSource != null && victoryFanfare != null)
        {
            seSource.PlayOneShot(victoryFanfare);
        }

        yield return new WaitForSecondsRealtime(0.3f);

        if(victoryLogo != null)
        {
            victoryLogo.SetActive(true);
        }

        Debug.Log("勝利演出開始");
    }

    AudioClip GetEnemyDefeatSEByDifficulty()
    {
        GameFlowManager flow =
            FindFirstObjectByType<GameFlowManager>();

        if(flow == null)
            return normalEnemyDefeatSE;

        if(flow.selectedDifficulty == GameFlowManager.Difficulty.Easy)
            return easyEnemyDefeatSE;

        if(flow.selectedDifficulty == GameFlowManager.Difficulty.Hard)
            return hardEnemyDefeatSE;

        return normalEnemyDefeatSE;
    }

    public void SortPlayerHand()
    {
        if (proHandGroup != null &&
    proHandGroup.transform == handArea &&
    proHandCardPrefab != null)
    {
        // 手札から出たカード・削除済みカードを登録から外す。
        for (int i = proHandGroup.cards.Count - 1; i >= 0; i--)
        {
            CCP.Card card = proHandGroup.cards[i];

            if (card == null)
            {
                proHandGroup.cards.RemoveAt(i);
            }
            else if (card.transform.parent != handArea ||
                    !card.gameObject.activeSelf)
            {
                proHandGroup.RemoveCard(card);
            }
        }

        proHandGroup.cards.Sort((a, b) =>
        {
            CardController ca = a.GetComponent<CardController>();
            CardController cb = b.GetComponent<CardController>();

            CardData da = ca != null ? ca.data : null;
            CardData db = cb != null ? cb.data : null;

            if (da == null)
                return db == null ? 0 : 1;

            if (db == null)
                return -1;

            int rankCompare =
                GetRankOrder(da.cardName ?? "").CompareTo(
                    GetRankOrder(db.cardName ?? "")
                );

            if (rankCompare != 0)
                return rankCompare;

            // Suitの定義順：Spade → Heart → Club → Diamond
            return da.suit.CompareTo(db.suit);
        });

        for (int i = 0; i < proHandGroup.cards.Count; i++)
        {
            proHandGroup.cards[i].transform.SetSiblingIndex(i);
        }

        if (handController != null)
        {
            handController.RefreshProLayout();
        }
        else
        {
            proHandGroup.RecalculateAndAnimatePositions();
        }

        return;
    }
        List<Transform> cards = new List<Transform>();

        for(int i=0; i<handArea.childCount; i++)
        {
            Transform child = handArea.GetChild(i);

            CardController card =
                child.GetComponent<CardController>();

            if(card != null && card.data != null)
            {
                cards.Add(child);
            }
        }
        foreach(Transform child in cards)
        {
            CardController c = child.GetComponent<CardController>();
            Debug.Log("手札カード名 = " + c.data.cardName);
        }
        cards.Sort((a,b)=>
        {
            CardData da =
                a.GetComponent<CardController>().data;

            CardData db =
                b.GetComponent<CardController>().data;

            int rankCompare =
                GetRankOrder(da.cardName)
                .CompareTo(
                    GetRankOrder(db.cardName)
                );

            if(rankCompare != 0)
                return rankCompare;

            return GetSuitOrder(da.cardName)
                .CompareTo(
                    GetSuitOrder(db.cardName)
                );
        });

        for(int i=0;i<cards.Count;i++)
        {
            cards[i].SetSiblingIndex(i);
        }

        if(HandExpandManager.I != null &&
        HandExpandManager.I.IsExpanded)
        {
            HandExpandManager.I.RefreshLayout();
        }
        else
        {
            ForceHandLayout();
        }
    }

    int GetRankOrder(string cardName)
    {
        if(cardName.Contains("Joker") || cardName.Contains("ジョーカー"))
            return 99;

        if(cardName.Contains("10")) return 10;
        if(cardName.Contains("2")) return 2;
        if(cardName.Contains("3")) return 3;
        if(cardName.Contains("4")) return 4;
        if(cardName.Contains("5")) return 5;
        if(cardName.Contains("6")) return 6;
        if(cardName.Contains("7")) return 7;
        if(cardName.Contains("8")) return 8;
        if(cardName.Contains("9")) return 9;
        if(cardName.Contains("J")) return 11;
        if(cardName.Contains("Q")) return 12;
        if(cardName.Contains("K")) return 13;
        if(cardName.Contains("A")) return 14;

        return 999;
    }

    int GetSuitOrder(string cardName)
    {
        if(cardName.Contains("♠") || cardName.Contains("Spade") || cardName.Contains("スペード"))
            return 0;

        if(cardName.Contains("♥") || cardName.Contains("Heart") || cardName.Contains("ハート"))
            return 1;

        if(cardName.Contains("♣") || cardName.Contains("Club") || cardName.Contains("クラブ"))
            return 2;

        if(cardName.Contains("♦") || cardName.Contains("Diamond") || cardName.Contains("ダイヤ"))
            return 3;

        return 999;
    }

    public bool EnemyUseRandomHandCardAsResource()
    {
        if(enemyHandCount <= 0)
        {
            Debug.Log("敵手札なし：リソースチャージ不可");
            return false;
        }

        enemyHandCount--;

        UpdateEnemyHandCountText();

        Debug.Log("敵が手札を1枚リソースへ送った。敵手札：" + enemyHandCount);

        return true;
    }

public IEnumerator EnemyChargeResourceAnimation()
{
    if(enemyHandCards==null || enemyHandCards.Count==0)yield break;
    yield return EnemyFlowCharge(enemyHandCards[Random.Range(0,enemyHandCards.Count)]);
}

public IEnumerator EnemyChargeSpecificResourceAnimation(CardData selectedCard)
{ yield return EnemyFlowCharge(selectedCard); }

    public void DamagePlayerWall(GameObject wall)
    {
        StartCoroutine(
            DamagePlayerWallRoutine(wall)
        );
    }


public bool IsResolvingWallAttack { get; private set; }

IEnumerator DamagePlayerWallRoutine(GameObject wall)
{
    yield return DamagePlayerWallsAndWait(new[] { wall });
}

public IEnumerator DamagePlayerWallsAndWait(IEnumerable<GameObject> requested)
{
    if (IsResolvingWallAttack) yield break;
    var walls = new List<GameObject>();
    var cards = new List<CardData>();
    foreach (var wall in requested)
    {
        if (wall == null || walls.Contains(wall) || wall.transform.parent != wallArea) continue;
        var cg = wall.GetComponent<CanvasGroup>();
        var card = wall.GetComponentInChildren<CardController>();
        if ((cg != null && cg.alpha <= 0.01f) || card == null || card.data == null) continue;
        walls.Add(wall);
        cards.Add(card.data);
        if (walls.Count == 2) break;
    }
    if (walls.Count == 0) yield break;
    IsResolvingWallAttack = true;
    try
    {
        // Start all slash animations in the same frame, then commit both breaks
        // before any shield choice/effect can recover a wall or destroy the attacker.
        var animations = new List<Coroutine>();
        foreach (var wall in walls) animations.Add(StartCoroutine(PlayWallSlash(wall)));
        foreach (var animation in animations) yield return animation;
        foreach (var wall in walls)
        {
            var cg = wall.GetComponent<CanvasGroup>(); if (cg == null) cg = wall.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }
        playerWallAliveCount = Mathf.Max(0, playerWallAliveCount - walls.Count);
        var shields = cards.FindAll(IsShieldCard);
        if (shields.Count == 2 && ReverseChoiceManager.I != null)
        {
            int first = 0;
            yield return ReverseChoiceManager.I.ShowOrderRoutine(shields[0], shields[1], value => first = value);
            if (first == 1) { var swap = shields[0]; shields[0] = shields[1]; shields[1] = swap; }
        }
        foreach (var data in cards)
            if (!IsShieldCard(data)) yield return ResolveBrokenPlayerWall(data);
        foreach (var data in shields)
        {
            yield return ResolveBrokenPlayerWall(data);
            yield return new WaitUntil(() => !IsWallPlacementRunning && !IsHandLimitSelecting);
        }
        Debug.Log("ウォール攻撃完了：同時破壊 " + walls.Count + "枚 / 残り " + playerWallAliveCount);
    }
    finally { IsResolvingWallAttack = false; }
}

bool IsShieldCard(CardData data)
{
    return (data != null && data.effectTypes != null &&
        System.Array.Exists(data.effectTypes, effect => effect == EffectType.ShieldTrigger));
}

IEnumerator ResolveBrokenPlayerWall(CardData data)
{
bool shieldTrigger =
    (
        data.effectTypes != null &&
        System.Array.Exists(
            data.effectTypes,
            x => x == EffectType.ShieldTrigger
        )
    );

    bool useReverse = false;

    if(shieldTrigger &&
       ReverseChoiceManager.I != null)
    {
        yield return StartCoroutine(
            ReverseChoiceManager.I.ShowChoiceRoutine(
                data,
                result =>
                {
                    useReverse = result;
                }
            )
        );
    }

    if(shieldTrigger && useReverse)
    {
        Debug.Log(
            "リバース使用：" +
            data.cardName
        );

        if(turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<TurnManager>();
        }

        if(turnManager == null ||
           turnManager.playerBattleArea == null)
        {
            Debug.LogWarning(
                "playerBattleArea が見つからない"
            );
            yield break;
        }

        if(!BattleAreaLayout.IsSpell(data) && !BattleAreaLayout.CanAccept(turnManager.playerBattleArea))
        {
            Debug.Log("シールドトリガー召喚不可：場が6枚のため手札へ追加");
            CreateHandCard(data);
            yield break;
        }

        // Show the accepted reverse before field entry and trigger resolution.
        yield return CardPlayPresentation.Play(data,turnManager.playerBattleArea);

        GameObject triggerCard =
            Instantiate(
                proHandCardPrefab != null ? proHandCardPrefab : cardPrefab,
                turnManager.playerBattleArea
            );

        SetupCardSize(
            triggerCard,
            handCardSize
        );

        triggerCard.name =
            "Reverse_" + data.cardName;

        CardController triggerController =
            triggerCard.GetComponent<CardController>();

        if(triggerController != null)
        {
            triggerController.SetData(data);
            if (BattleAreaLayout.IsSpell(data))
            {
                var spellGroup = triggerCard.GetComponent<CanvasGroup>(); if (spellGroup == null) spellGroup = triggerCard.AddComponent<CanvasGroup>();
                spellGroup.alpha = 0f; spellGroup.blocksRaycasts = spellGroup.interactable = false;
            }
            triggerController.MarkPlayedFromReverse();
            triggerController.SetAttackable(false);
        }

        LayoutElement triggerLayout =
            triggerCard.GetComponent<LayoutElement>();

        if(triggerLayout == null)
            triggerLayout =
                triggerCard.AddComponent<LayoutElement>();

        triggerLayout.ignoreLayout = true;
        triggerLayout.preferredWidth = handCardSize.x;
        triggerLayout.preferredHeight = handCardSize.y;
        triggerLayout.minWidth = handCardSize.x;
        triggerLayout.minHeight = handCardSize.y;

        RectTransform rt =
            triggerCard.GetComponent<RectTransform>();

        if(rt != null)
        {
            rt.localRotation =
                Quaternion.identity;

            rt.sizeDelta =
                handCardSize;
        }

        BattleAreaLayout layout =
            turnManager.playerBattleArea
            .GetComponent<BattleAreaLayout>();

        if(layout != null)
        {
            // The normal summon path registers CCP movement and field click input.
            CardDrag triggerDrag = triggerCard.GetComponent<CardDrag>();
            if(triggerDrag != null)
            {
                triggerDrag.enabled = true;
                bool showcase=CardPlaySettings.Load().enabledEffect;
                triggerDrag.PlaceFromShield(turnManager.playerBattleArea,!showcase);
                layout.Refresh();
                if(showcase)
                {
                    layout.PlaceImmediately(triggerController);
                    triggerDrag.EnablePlacedCardInput();
                }
                while(triggerDrag != null && triggerDrag.IsDropSnapPlaying)
                    yield return null;
            }
            else
            {
                BattleCardClick click = triggerCard.GetComponent<BattleCardClick>();
                if(click == null) click = triggerCard.AddComponent<BattleCardClick>();
                click.enabled = true;
                layout.Refresh();
            }
        }

        if (BattleAreaLayout.IsSpell(data))
        {
            var spellGroup = triggerCard.GetComponent<CanvasGroup>(); if (spellGroup == null) spellGroup = triggerCard.AddComponent<CanvasGroup>();
            spellGroup.alpha = 0f; spellGroup.blocksRaycasts = spellGroup.interactable = false;
        }

        if(CardEffectManager.I != null &&
           triggerController != null)
        {
            bool isAce =
                HasEffect(
                    triggerController,
                    EffectType.DestroyOneEnemyBattle
                );

            bool isNine =
                HasEffect(
                    triggerController,
                    EffectType.TapAllEnemyBattle
                );

            bool isJoker =
                HasEffect(
                    triggerController,
                    EffectType.JokerClearBattleArea
                );

            Debug.Log(
                "相手ターン・シールドトリガー発動：" +
                data.cardName +
                " / A=" + isAce +
                " / 9=" + isNine +
                " / Joker=" + isJoker
            );

            CardEffectManager.I.ActivateOnSummon(
                triggerController,
                true,
                false
            );

            // Finish reverse 10 before continuing this attack or the next wall.
            while(PresentationSequenceLock.IsLocked || (turnManager != null && turnManager.IsChoosingEnemyHand)) yield return null;

            if(isAce)
            {
                CanvasGroup aceCg =
                    triggerCard.GetComponent<CanvasGroup>();

                if(aceCg == null)
                {
                    aceCg =
                        triggerCard.AddComponent<CanvasGroup>();
                }

                aceCg.alpha = 0f;
                aceCg.blocksRaycasts = false;
                aceCg.interactable = false;

                Debug.Log(
                    "シールドトリガーA：" +
                    "敵カードの対象選択待機開始"
                );

                while(turnManager != null &&
                      turnManager.IsSelectingDestroyTarget())
                {
                    if(aceCg != null)
                    {
                        aceCg.alpha = 0f;
                        aceCg.blocksRaycasts = false;
                        aceCg.interactable = false;
                    }

                    yield return null;
                }

                Debug.Log(
                    "シールドトリガーA：" +
                    "対象選択・破壊完了"
                );
            }
            else if(isNine)
            {
                while(isNine && CardEffectManager.I != null && CardEffectManager.I.IsResolvingNine)
                    yield return null;
                if(triggerController != null &&
                   triggerController.transform.IsChildOf(
                       turnManager.playerBattleArea
                   ))
                {
                    turnManager.SendCardToOwnGraveyard(
                        triggerController
                    );
                }

                Debug.Log(
                    "シールドトリガー即時効果完了：" +
                    data.cardName
                );
            }
        }
    }
    else
    {
        Debug.Log(
            shieldTrigger
            ? "リバースを手札へ：" + data.cardName
            : "通常Wallを手札へ：" + data.cardName
        );

        // Use the same prefab and Pro hand registration as a normal draw.
        GameObject handCard = CreateHandCard(data);

        LayoutElement layout2 =
            handCard.GetComponent<LayoutElement>();

        if(layout2 != null)
        {
            layout2.ignoreLayout = false;
        }

        CanvasGroup handCg =
            handCard.GetComponent<CanvasGroup>();

        if(handCg == null)
            handCg =
                handCard.AddComponent<CanvasGroup>();

        handCg.alpha = 1f;
        handCg.blocksRaycasts = true;
        handCg.interactable = true;

        CardDrag drag =
            handCard.GetComponent<CardDrag>();

        if(drag != null)
            drag.enabled = true;

        SortPlayerHand();
    }

}

    public void ChargeTopDeckToResource() => ChargeTopDeckToResource(false);

    public void ChargeTopDeckToResource(bool enemy)
    {
        if(enemy)
        {
            if(enemyDeck == null || enemyDeck.Count == 0) return;
            EnemyResourceManager manager = FindFirstObjectByType<EnemyResourceManager>();
            if(manager == null) return;
            enemyDeck.RemoveAt(0);
            manager.AddResource(false);
            return;
        }
        if(currentDeck == null ||
        currentDeck.Count <= 0)
        {
            Debug.Log("山札なし");
            return;
        }

        CardData card = currentDeck[0];
        currentDeck.RemoveAt(0);

        ResourceManager rm =
            FindFirstObjectByType<ResourceManager>();

        if(rm != null)
        {
            rm.AddResource();
        }
        else
        {
            Debug.LogWarning("ResourceManager が見つからない");
        }

        Debug.Log(
            "効果発動：山札上をリソースへ → " +
            card.name +
            " / 残り山札：" +
            currentDeck.Count
        );
    }


    // 10：敵手札ランダム墓地
    public void RecoverWallFromDeck()
    {
        RecoverWallFromDeck(false, false);
    }

    bool RecoverWallFromDeck(bool enemy, bool fromKing)
    {
        if(fromKing && (enemy ? enemyKingWallsAdded : playerKingWallsAdded) >= Mathf.Max(0, maxKingWallAdditions))
        {
            Debug.Log($"ウォール追加不可：{(enemy ? "敵" : "味方")} キング累計上限 {maxKingWallAdditions}");
            return false;
        }
        List<CardData> deck = enemy ? enemyDeck : currentDeck;
        WallAreaLayout layout = GetWallLayout(enemy ? enemyWallArea : wallArea);
        if (deck == null || deck.Count == 0 || layout == null || !layout.HasSpace)
        {
            Debug.LogWarning($"ウォール追加不可：{(enemy ? "敵" : "味方")} 山札={deck?.Count ?? 0} 配置先={layout != null} 空き枠={layout != null && layout.HasSpace}");
            return false;
        }
        CardData data = deck[0];
        if (!CreateWallCard(data, enemy, fromKing)) return false;
        deck.RemoveAt(0);
        if(fromKing)
        {
            if(enemy) enemyKingWallsAdded++;
            else playerKingWallsAdded++;
        }
        if (enemy) enemyWallAliveCount++;
        else playerWallAliveCount++;
        Debug.Log($"ウォール追加成功：{(enemy ? "敵" : "味方")} 現在{(enemy ? enemyWallAliveCount : playerWallAliveCount)}枚 / キング累計{(enemy ? enemyKingWallsAdded : playerKingWallsAdded)}/{maxKingWallAdditions}");
        return true;
    }
    public void DiscardRandomEnemyHand() => DiscardRandomEnemyHand(false);

    public void DiscardRandomEnemyHand(bool sourceIsEnemy)
    {
        if(sourceIsEnemy)
        {
            var cards = TCardAIUnityBridge.GetCards(handArea);
            if(cards.Count == 0) return;
            CardController discarded = cards[Random.Range(0, cards.Count)];
            if(turnManager != null) turnManager.DiscardPlayerHandCard(discarded);
            else Destroy(discarded.gameObject);
            if(handController != null) handController.RefreshProLayout();
            return;
        }
        if(enemyHandCards == null ||
        enemyHandCards.Count <= 0)
        {
            Debug.Log("敵手札0枚");
            return;
        }

        int index =
            Random.Range(
                0,
                enemyHandCards.Count
            );

        CardData removed =
            enemyHandCards[index];

        enemyHandCards.RemoveAt(index);

        RefreshEnemyHandVisual();

        enemyHandCount--;

        UpdateEnemyHandCountText();

        Debug.Log(
            "効果発動：敵手札墓地 → " +
            removed.cardName
        );
    }
    public void SelectEnemyHandToDiscard(bool sourceIsEnemy)
    {
        // CPU effects still select automatically from the player's hidden hand.
        if (sourceIsEnemy) { DiscardRandomEnemyHand(true); return; }
        if (enemyHandCards == null || enemyHandCards.Count == 0) return;
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager != null) turnManager.BeginEnemyHandChoice();
    }

    public bool DiscardEnemyHandAt(int index)
    {
        if (enemyHandCards == null || index < 0 || index >= enemyHandCards.Count) return false;
        CardData removed = enemyHandCards[index];
        enemyHandCards.RemoveAt(index);
        enemyHandCount = enemyHandCards.Count;
        RefreshEnemyHandVisual();
        UpdateEnemyHandCountText();
        Debug.Log("効果発動：敵手札墓地 → " + (removed != null ? removed.cardName : "unknown"));
        return true;
    }

    public void RecoverWallByKing()
    {
        RecoverWallByKing(false);
    }

    public void RecoverWallByKing(bool enemy)
    {
        int remaining = Mathf.Max(0, maxKingWallAdditions - (enemy ? enemyKingWallsAdded : playerKingWallsAdded));
        // Each King restores exactly one wall, subject to space, deck and lifetime limit.
        RecoverWallFromDeck(enemy, true);
    }
    public IEnumerator DamagePlayerWallAndWait(GameObject wall)
    {
        yield return StartCoroutine(
            DamagePlayerWallRoutine(wall)
        );
    }

    bool HasEffect(
        CardController card,
        EffectType effectType
    )
    {
        if(card == null ||
           card.data == null ||
           card.data.effectTypes == null)
        {
            return false;
        }

        return System.Array.Exists(
            card.data.effectTypes,
            effect => effect == effectType
        );
    }

    struct OpeningInputState { public bool raycasts, interactable; }
    readonly Dictionary<CanvasGroup, OpeningInputState> openingInputStates = new Dictionary<CanvasGroup, OpeningInputState>();
    void SetOpeningLock(bool locked)
    {
        if (!locked)
        {
            foreach (var entry in openingInputStates)
                if (entry.Key != null)
                {
                    entry.Key.blocksRaycasts = entry.Value.raycasts;
                    entry.Key.interactable = entry.Value.interactable;
                }
            openingInputStates.Clear();
            return;
        }
        // Lock gameplay branches only. Global CanvasGroup edits also disabled modal
        // ancestors, and unlocking them re-enabled deliberately non-interactive effects.
        var areas = new List<Transform> { handArea, wallArea, enemyWallArea };
        if (turnManager != null)
        {
            areas.Add(turnManager.playerBattleArea); areas.Add(turnManager.enemyBattleArea);
            if (turnManager.resourcePhaseManager != null) areas.Add(turnManager.resourcePhaseManager.resourceArea);
        }
        foreach (var area in areas)
        {
            if (area == null) continue;
            var group = area.GetComponent<CanvasGroup>(); if (group == null) group = area.gameObject.AddComponent<CanvasGroup>();
            if (!openingInputStates.ContainsKey(group))
                openingInputStates[group] = new OpeningInputState { raycasts = group.blocksRaycasts, interactable = group.interactable };
            group.blocksRaycasts = group.interactable = false;
        }
        foreach (var button in new[] { redrawButton, confirmButton })
        {
            if (button == null) continue;
            var group = button.GetComponent<CanvasGroup>(); if (group == null) group = button.AddComponent<CanvasGroup>();
            group.ignoreParentGroups = true;
            group.blocksRaycasts = group.interactable = true;
        }
    }

    public void RefreshEnemyHandVisual()
    {
        if(enemyHandArea == null)
            return;

        for(int i = enemyHandArea.childCount - 1; i >= 0; i--)
        {
            // Destroy is deferred: retire rendering and layout immediately.
            var old=enemyHandArea.GetChild(i).gameObject;
            old.SetActive(false);
            Destroy(old);
        }

        for(int i = 0; i < enemyHandCards.Count; i++)
        {
            GameObject cardObj =
                Instantiate(cardPrefab, enemyHandArea);

            cardObj.name = "EnemyHandBack";

            SetupCardSize(cardObj, enemyHandCardSize);

            LayoutElement layout =
                cardObj.GetComponent<LayoutElement>();

            if(layout != null)
            {
                layout.ignoreLayout = false;
                layout.preferredWidth = enemyHandCardSize.x;
                layout.preferredHeight = enemyHandCardSize.y;
                layout.minWidth = enemyHandCardSize.x;
                layout.minHeight = enemyHandCardSize.y;
            }

            RectTransform rt =
                cardObj.GetComponent<RectTransform>();

            if(rt != null)
            {
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }

            CardController card =
                cardObj.GetComponent<CardController>();

            if(card != null)
            {
                if(card.artworkImage != null)
                    card.artworkImage.sprite = cardBackSprite;

                if(card.costText != null)
                    card.costText.gameObject.SetActive(false);

                if(card.attackText != null)
                    card.attackText.gameObject.SetActive(false);

                if(card.hpText != null)
                    card.hpText.gameObject.SetActive(false);

                card.enabled = false;
            }

            CardDrag drag =
                cardObj.GetComponent<CardDrag>();

            if(drag != null)
                drag.enabled = false;

            BattleCardClick click =
                cardObj.GetComponent<BattleCardClick>();

            if(click != null)
                click.enabled = false;

            CanvasGroup cg =
                cardObj.GetComponent<CanvasGroup>();

            if(cg == null)
                cg = cardObj.AddComponent<CanvasGroup>();

            cg.alpha = 1f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }
    }

    void DisableWallInput(GameObject wallCard)
    {
        if(wallCard == null)
            return;

        CanvasGroup cg =
            wallCard.GetComponent<CanvasGroup>();

        if(cg == null)
            cg = wallCard.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = false;
        cg.interactable = false;

        CardDrag drag =
            wallCard.GetComponent<CardDrag>();

        if(drag != null)
            drag.enabled = false;

        BattleCardClick battleClick =
            wallCard.GetComponent<BattleCardClick>();

        if(battleClick != null)
            battleClick.enabled = false;

        HandCardDoubleClick handDoubleClick =
            wallCard.GetComponent<HandCardDoubleClick>();

        if(handDoubleClick != null)
            handDoubleClick.enabled = false;
    }
}
