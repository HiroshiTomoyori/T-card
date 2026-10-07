using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HandDealer : MonoBehaviour
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

    [Header("Card Size")]
    public Vector2 handCardSize = new Vector2(75, 112);
    public Vector2 wallCardSize = new Vector2(70, 105);

    [Header("Prefab")]
    public GameObject cardPrefab;

    [Header("Card Controller Pro / Player Hand")]
    public GameObject proHandCardPrefab;
    public CCP.CardGroup proHandGroup;


    [Header("Card Data")]
    public List<CardData> cardList = new List<CardData>();

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
    public CardData HandLimitDrawnCardData { get; private set; }
    public GameObject HandLimitDrawnCardObject { get; private set; }
    public GameObject SelectedHandLimitDiscardCard { get; private set; }
    readonly Dictionary<EventTrigger, List<EventTrigger.Entry>>
        handLimitSavedTriggers =
            new Dictionary<EventTrigger, List<EventTrigger.Entry>>();
    readonly List<MonoBehaviour> handLimitDisabledClickBehaviours =
        new List<MonoBehaviour>();
    string savedRedrawButtonText;
    string savedConfirmButtonText;

    List<CardData> currentDeck = new List<CardData>();
    List<CardData> enemyDeck = new List<CardData>();
    public List<CardData> enemyHandCards =
    new List<CardData>();

    [Header("Enemy Hand Visual")]
    public Vector2 enemyHandCardSize = new Vector2(35, 52);

    [Header("Opening Lock")]
    public Button endTurnButton;



public Transform playerWallArea;
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

        if (enemyDeckImage != null)
            enemyDeckImage.gameObject.SetActive(true);

        StartCoroutine(DealRoutine(true));
/*
        if (enemyHandCountText != null)
            enemyHandCountText.gameObject.SetActive(true);
*/
        if(debugForceEnemyQ)
{
    CardData qCard = enemyDeck.Find(
        card => card != null &&
        card.cardName.Contains("Q")
    );

    if(qCard != null)
    {
        enemyDeck.Remove(qCard);

        enemyHandCards.Add(qCard);
        enemyHandCount++;

        RefreshEnemyHandVisual();
        UpdateEnemyHandCountText();

        Debug.Log(
            "デバッグ：敵の初期手札にQを追加 → " +
            qCard.cardName
        );

        // Q以外の初期手札4枚を通常ドロー
        EnemyDraw(4);
    }
    else
    {
        Debug.LogWarning(
            "デバッグ：敵山札にQが見つかりません"
        );

        EnemyDraw(5);
    }
}
else
{
    EnemyDraw(5);
}
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


    IEnumerator DealRoutine(bool showButtonsAfterDeal)
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

        for (int i = 0; i < dealCount; i++)
        {
            if (currentDeck.Count <= 0)
            {
                Debug.LogWarning("山札が足りません");
                break;
            }

            CardData selectedCard = DrawRandomCardData();

            if (selectedCard == null)
                continue;

            GameObject cardObj = CreateHandCard(selectedCard);

            RectTransform cardRect = cardObj.GetComponent<RectTransform>();

            if (cardRect == null)
            {
                Debug.LogError("CardPrefab に RectTransform がありません");
                yield break;
            }

            Vector2 startPos = Vector2.zero;

            if (deckPosition != null)
            {
                startPos = WorldToLocalPosition(
                    handArea as RectTransform,
                    deckPosition.position
                );
            }

            cardRect.anchoredPosition = startPos;

            yield return AnimateCardToHand(cardRect);

            LayoutElement layout = cardObj.GetComponent<LayoutElement>();

            if (layout != null)
            {
                layout.ignoreLayout = false;
            }

            //ForceHandLayout();
            SortPlayerHand();

            yield return new WaitForSeconds(dealInterval);
        }

        CreateWallCards();
        CreateEnemyWallCards();
        yield return new WaitUntil(() => !IsWallPlacementRunning && !IsHandLimitSelecting);

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
/*IEnumerator AnimateCardToHand(RectTransform cardRect)
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

IEnumerator AnimateCardToHand(RectTransform cardRect)
{
    if (cardRect == null)
        yield break;

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

            Vector3 deckCenter = deckPosition.TransformPoint(
                new Vector3(startCenter.x, startCenter.y, 0f)
            );

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

        float duration = Mathf.Max(0.01f, flyTime);

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
    float legacyDuration = Mathf.Max(0.01f, flyTime);

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
        for (int i = 0; i < wallCount && layout.HasSpace; i++)
        {
            CardData data = DrawRandomCardData();
            if (data == null) break;
            if (CreateWallCard(data, false, false)) playerWallAliveCount++;
            else currentDeck.Add(data);
        }
    }

    void CreateEnemyWallCards()
    {
        enemyWallAliveCount = 0;
        enemyKingWallsAdded = 0;
        WallAreaLayout layout = GetWallLayout(enemyWallArea);
        if (layout == null || cardPrefab == null || cardBackSprite == null) return;
        layout.ClearWalls();
        for (int i = 0; i < enemyWallCount && layout.HasSpace; i++)
        {
            CardData data = DrawEnemyRandomCardData();
            if (data == null) break;
            if (CreateWallCard(data, true, false)) enemyWallAliveCount++;
            else enemyDeck.Add(data);
        }
    }

    bool CreateWallCard(CardData data, bool enemy, bool fromKing)
    {
        Transform area = enemy ? enemyWallArea : wallArea;
        WallAreaLayout layout = GetWallLayout(area);
        if (data == null || layout == null || !layout.HasSpace ||
            cardPrefab == null || cardBackSprite == null) return false;
        GameObject wall = Instantiate(cardPrefab, area);
        wall.name = (enemy ? "EnemyWallCard_" : "PlayerWallCard_") + data.cardName;
        SetupCardSize(wall, wallCardSize);
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
        if (layout.AddWall(wall, fromKing, wallCardSize)) return true;
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
        if(handArea == null)
        {
            Debug.LogError(
                "RedrawRoutine: HandArea が未設定です"
            );
            yield break;
        }

        if(cardPrefab == null)
        {
            Debug.LogError(
                "RedrawRoutine: CardPrefab が未設定です"
            );
            yield break;
        }

        if(currentDeck == null)
        {
            currentDeck =
                new List<CardData>();
        }

        // 現在の手札を山札へ戻す
        List<GameObject> oldHandCards =
            new List<GameObject>();

        for(int i = 0;
            i < handArea.childCount;
            i++)
        {
            Transform child =
                handArea.GetChild(i);

            if(child == null)
                continue;

            CardController controller =
                child.GetComponent<CardController>();

            if(controller != null &&
               controller.data != null)
            {
                currentDeck.Add(
                    controller.data
                );
            }

            oldHandCards.Add(
                child.gameObject
            );
        }

        // パネルを閉じる
        HideButtons();

        // 古い手札を消す
        foreach(GameObject oldCard
            in oldHandCards)
        {
            /*if(oldCard != null)
            {
                oldCard.SetActive(false);
                Destroy(oldCard);
            }*/

            if (oldCard != null)
            {
                CCP.Card proCard = oldCard.GetComponent<CCP.Card>();

                if (proCard != null &&
                    proCard.parentCardGroup != null)
                {
                    proCard.parentCardGroup.RemoveCard(proCard);
                }

                oldCard.SetActive(false);
                Destroy(oldCard);
            }
        }

        // Destroy反映待ち
        yield return null;

        ApplyOpeningHandLook();

        // 新しい手札を配布アニメーション付きで生成
        for(int i = 0;
            i < dealCount;
            i++)
        {
            if(currentDeck.Count <= 0)
            {
                Debug.LogWarning(
                    "RedrawRoutine: 山札が足りません"
                );
                break;
            }

            CardData selectedCard =
                DrawRandomCardData();

            if(selectedCard == null)
                continue;

            GameObject cardObj =
                CreateHandCard(
                    selectedCard
                );

            RectTransform cardRect =
                cardObj.GetComponent
                <RectTransform>();

            if(cardRect == null)
            {
                Debug.LogError(
                    "RedrawRoutine: " +
                    "CardPrefab に RectTransform がありません"
                );
                yield break;
            }

            Vector2 startPos =
                Vector2.zero;

            if(deckPosition != null)
            {
                startPos =
                    WorldToLocalPosition(
                        handArea as RectTransform,
                        deckPosition.position
                    );
            }

            cardRect.anchoredPosition =
                startPos;

            yield return
                AnimateCardToHand(
                    cardRect
                );

            LayoutElement layout =
                cardObj.GetComponent
                <LayoutElement>();

            if(layout != null)
            {
                layout.ignoreLayout =
                    false;
            }

            SortPlayerHand();

            yield return
                new WaitForSeconds(
                    dealInterval
                );
        }
        // 引き直した手札を1秒見せる
        yield return new WaitForSeconds(1f);
        CompleteOpeningSelection();
    }

    public void ConfirmHand()
    {
        if (IsHandLimitSelecting)
        {
            ConfirmHandLimitDiscard();
            return;
        }

        CompleteOpeningSelection();
    }

    void CompleteOpeningSelection()
    {
        RestoreOpeningHandLook();

        IsRedrawSelecting = false;
        HideButtons();
        canRedraw = false;

        SetOpeningLock(false);

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

    public void DrawOneCard()
    {
        if (currentDeck == null || currentDeck.Count <= 0)
        {
            Debug.Log("山札なし");
            return;
        }

        if (handArea != null && handArea.childCount >= maxHandSize)
        {
            BeginHandLimitSelection();
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

    void BeginHandLimitSelection()
    {
        if (IsHandLimitSelecting)
            return;

        IsHandLimitSelecting = true;
        SelectedHandLimitDiscardCard = null;

        SetOpeningLock(true);

        if (InputLockManager.I != null)
        {
            InputLockManager.I.LockInput();
        }

        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(false);
        }

        if (handController != null)
        {
            handController.transform.SetAsLastSibling();
        }

        StartCoroutine(ApplyHandLimitLookRoutine());

        Debug.Log(
            "手札上限選択開始：既存の" +
            maxHandSize +
            "枚を中央に拡大表示"
        );
    }

    IEnumerator ApplyHandLimitLookRoutine()
    {
        // 初期手札確認と同じ中央表示を適用してから、
        // 手札上限選択専用のInspector設定で上書きする。
        ApplyOpeningHandLook();
        yield return null;

        if (handController != null)
        {
            handController.enabled = false;
        }

        RectTransform handRect = handArea as RectTransform;

        if (handRect != null)
        {
            handRect.anchoredPosition = handLimitDisplayPosition;
        }

        int cardCount = handArea != null ? handArea.childCount : 0;
        float cardInterval =
            Mathf.Max(0f, handLimitCardSize.x - handLimitCardOverlap);
        float startX = -cardInterval * (cardCount - 1) * 0.5f;

        for (int i = 0; i < cardCount; i++)
        {
            Transform child = handArea.GetChild(i);
            RectTransform cardRect = child as RectTransform;

            if (cardRect == null)
                continue;

            LayoutElement layout = child.GetComponent<LayoutElement>();

            if (layout != null)
            {
                layout.ignoreLayout = true;
                layout.preferredWidth = handLimitCardSize.x;
                layout.preferredHeight = handLimitCardSize.y;
            }

            cardRect.sizeDelta = handLimitCardSize;
            cardRect.localScale = Vector3.one;
            cardRect.anchoredPosition = new Vector2(
                startX + cardInterval * i,
                0f
            );

            ConfigureHandLimitDiscardSelection(child.gameObject);
        }

        yield return ShowHandLimitDrawnCard();
    }

    IEnumerator ShowHandLimitDrawnCard()
    {
        HandLimitDrawnCardData = DrawRandomCardData();

        if (HandLimitDrawnCardData == null)
        {
            Debug.LogWarning("11枚目として表示するカードを引けませんでした");
            yield break;
        }

        Transform drawnCardParent =
            handArea != null && handArea.parent != null
            ? handArea.parent
            : transform;

        HandLimitDrawnCardObject =
            Instantiate(cardPrefab, drawnCardParent);
        HandLimitDrawnCardObject.name =
            "HandLimitDrawnCard_" + HandLimitDrawnCardData.cardName;

        SetupCardSize(
            HandLimitDrawnCardObject,
            handLimitDrawnCardSize
        );

        CardController card =
            HandLimitDrawnCardObject.GetComponent<CardController>();

        if (card != null)
        {
            card.SetData(HandLimitDrawnCardData);
            card.enabled = false;
        }

        ConfigureHandLimitDiscardSelection(HandLimitDrawnCardObject);

        RectTransform cardRect =
            HandLimitDrawnCardObject.GetComponent<RectTransform>();

        if (cardRect == null)
            yield break;

        HandLimitDrawnCardObject.transform.SetAsLastSibling();

        Vector2 startPosition = handLimitDrawnCardPosition;
        RectTransform parentRect = drawnCardParent as RectTransform;

        if (deckPosition != null && parentRect != null)
        {
            startPosition = WorldToLocalPosition(
                parentRect,
                deckPosition.position
            );
        }

        cardRect.anchoredPosition = startPosition;

        float elapsed = 0f;

        while (elapsed < flyTime)
        {
            elapsed += Time.deltaTime;
            float rate = flyTime > 0f
                ? Mathf.Clamp01(elapsed / flyTime)
                : 1f;

            cardRect.anchoredPosition = Vector2.Lerp(
                startPosition,
                handLimitDrawnCardPosition,
                rate
            );

            yield return null;
        }

        cardRect.anchoredPosition = handLimitDrawnCardPosition;

        Debug.Log(
            "11枚目を表示：" +
            HandLimitDrawnCardData.cardName
        );
    }

    void ConfigureHandLimitDiscardSelection(GameObject cardObject)
    {
        if (cardObject == null)
            return;

        CardController cardController =
            cardObject.GetComponent<CardController>();

        if (cardController != null)
        {
            cardController.enabled = false;
        }

        CanvasGroup canvasGroup =
            cardObject.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = cardObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.ignoreParentGroups = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        EventTrigger trigger = cardObject.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = cardObject.AddComponent<EventTrigger>();
        }

        if (trigger.triggers == null)
        {
            trigger.triggers = new List<EventTrigger.Entry>();
        }
        else
        {
            if (!handLimitSavedTriggers.ContainsKey(trigger))
            {
                handLimitSavedTriggers.Add(
                    trigger,
                    new List<EventTrigger.Entry>(trigger.triggers)
                );
            }

            // Prefab側に登録されている通常クリック処理を選択中だけ外す。
            trigger.triggers.Clear();
        }

        DisableNormalCardClickHandlers(cardObject, trigger);

        EventTrigger.Entry clickEntry = new EventTrigger.Entry();
        clickEntry.eventID = EventTriggerType.PointerClick;
        clickEntry.callback.AddListener(
            _ => SelectHandLimitDiscardCard(cardObject)
        );
        trigger.triggers.Add(clickEntry);
    }

    void DisableNormalCardClickHandlers(
        GameObject cardObject,
        EventTrigger selectionTrigger
    )
    {
        MonoBehaviour[] behaviours =
            cardObject.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == selectionTrigger)
                continue;

            bool receivesCardClick =
                behaviour is IPointerClickHandler ||
                behaviour is IPointerDownHandler ||
                behaviour is IPointerUpHandler ||
                behaviour is ISubmitHandler;

            if (receivesCardClick)
            {
                if (behaviour.enabled &&
                    !handLimitDisabledClickBehaviours.Contains(behaviour))
                {
                    handLimitDisabledClickBehaviours.Add(behaviour);
                }

                behaviour.enabled = false;
            }
        }
    }

    void SelectHandLimitDiscardCard(GameObject cardObject)
    {
        if (!IsHandLimitSelecting || cardObject == null)
            return;

        if (SelectedHandLimitDiscardCard != null &&
            SelectedHandLimitDiscardCard != cardObject)
        {
            SelectedHandLimitDiscardCard.transform.localScale =
                Vector3.one;
        }

        SelectedHandLimitDiscardCard = cardObject;
        SelectedHandLimitDiscardCard.transform.localScale =
            Vector3.one * handLimitSelectedScale;

        bool isDrawnCard =
            SelectedHandLimitDiscardCard == HandLimitDrawnCardObject;

        Debug.Log(
            isDrawnCard
            ? "廃棄候補に11枚目を選択"
            : "廃棄候補に既存手札を選択：" +
                SelectedHandLimitDiscardCard.name
        );

        ShowHandLimitDiscardUI();
    }

    void ShowHandLimitDiscardUI()
    {
        if (redrawChoicePanel != null)
        {
            redrawChoicePanel.SetActive(true);
            redrawChoicePanel.transform.SetAsLastSibling();
        }

        SetHandLimitButton(
            redrawButton,
            handLimitDiscardCancelText,
            ref savedRedrawButtonText
        );
        SetHandLimitButton(
            confirmButton,
            handLimitDiscardConfirmText,
            ref savedConfirmButtonText
        );
    }

    void SetHandLimitButton(
        GameObject buttonObject,
        string label,
        ref string savedLabel
    )
    {
        if (buttonObject == null)
            return;

        buttonObject.SetActive(true);

        CanvasGroup canvasGroup =
            buttonObject.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = buttonObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        Button button = buttonObject.GetComponent<Button>();

        if (button != null)
            button.interactable = true;

        TMPro.TMP_Text text =
            buttonObject.GetComponentInChildren<TMPro.TMP_Text>(true);

        if (text != null)
        {
            if (savedLabel == null)
                savedLabel = text.text;

            text.text = label;
        }
    }

    void CancelHandLimitDiscardSelection()
    {
        if (SelectedHandLimitDiscardCard != null)
        {
            SelectedHandLimitDiscardCard.transform.localScale =
                Vector3.one;
        }

        SelectedHandLimitDiscardCard = null;
        HideButtons();

        Debug.Log("廃棄候補の選択を解除");
    }

    void ConfirmHandLimitDiscard()
    {
        if (SelectedHandLimitDiscardCard == null)
            return;

        bool discardDrawnCard =
            SelectedHandLimitDiscardCard == HandLimitDrawnCardObject;

        GameObject discardedCard = SelectedHandLimitDiscardCard;
        CardController discardedController =
            discardedCard.GetComponent<CardController>();

        if (discardedController != null)
            discardedController.enabled = true;

        if (!discardDrawnCard && HandLimitDrawnCardObject != null)
        {
            HandLimitDrawnCardObject.transform.SetParent(handArea, false);
            HandLimitDrawnCardObject.name =
                "HandCard_" + HandLimitDrawnCardData.cardName;
            SetupCardSize(HandLimitDrawnCardObject, handCardSize);
        }

        if (turnManager != null && discardedController != null)
        {
            turnManager.SendCardToOwnGraveyard(discardedController);
        }
        else
        {
            Destroy(discardedCard);
        }

        Debug.Log(
            discardDrawnCard
            ? "11枚目を廃棄して元の手札を維持"
            : "既存手札を廃棄して11枚目を手札へ追加"
        );

        CompleteHandLimitSelection();
    }

    void CompleteHandLimitSelection()
    {
        HideButtons();
        RestoreHandLimitButtonLabels();

        foreach (
            KeyValuePair<EventTrigger, List<EventTrigger.Entry>> saved
            in handLimitSavedTriggers
        )
        {
            if (saved.Key != null)
            {
                saved.Key.triggers =
                    new List<EventTrigger.Entry>(saved.Value);
            }
        }

        foreach (MonoBehaviour behaviour in
            handLimitDisabledClickBehaviours)
        {
            if (behaviour != null)
                behaviour.enabled = true;
        }

        handLimitSavedTriggers.Clear();
        handLimitDisabledClickBehaviours.Clear();

        if (handArea != null)
        {
            for (int i = 0; i < handArea.childCount; i++)
            {
                Transform child = handArea.GetChild(i);
                child.localScale = Vector3.one;

                SetupCardSize(child.gameObject, handCardSize);

                LayoutElement layout =
                    child.GetComponent<LayoutElement>();

                if (layout != null)
                    layout.ignoreLayout = false;

                CanvasGroup canvasGroup =
                    child.GetComponent<CanvasGroup>();

                if (canvasGroup != null)
                {
                    canvasGroup.ignoreParentGroups = false;
                    canvasGroup.blocksRaycasts = true;
                    canvasGroup.interactable = true;
                }

                CardController controller =
                    child.GetComponent<CardController>();

                if (controller != null)
                    controller.enabled = true;
            }
        }

        if (handController != null)
        {
            handController.enabled = true;
        }

        RestoreOpeningHandLook();
        SortPlayerHand();

        IsHandLimitSelecting = false;
        SelectedHandLimitDiscardCard = null;
        HandLimitDrawnCardData = null;
        HandLimitDrawnCardObject = null;

        SetOpeningLock(false);

        if (InputLockManager.I != null)
            InputLockManager.I.UnlockInput();

        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(true);
    }

    void RestoreHandLimitButtonLabels()
    {
        RestoreButtonLabel(redrawButton, savedRedrawButtonText);
        RestoreButtonLabel(confirmButton, savedConfirmButtonText);
        savedRedrawButtonText = null;
        savedConfirmButtonText = null;
    }

    void RestoreButtonLabel(GameObject buttonObject, string label)
    {
        if (buttonObject == null || label == null)
            return;

        TMPro.TMP_Text text =
            buttonObject.GetComponentInChildren<TMPro.TMP_Text>(true);

        if (text != null)
            text.text = label;
    }

    public void DrawOneCardToPlayerHand()
    {
        DrawOneCard();
    }


    IEnumerator EnemyDrawAnimation(int amount)
    {
        /*DeckShuffle shuffle =
        enemyDeckImage.GetComponent<DeckShuffle>();

        if (shuffle != null)
        {
            shuffle.StartShuffle();
        }*/
        int remainingCapacity = Mathf.Max(0, maxHandSize - enemyHandCount);
        int drawAmount = Mathf.Min(amount, enemyDeck.Count, remainingCapacity);

        if (drawAmount <= 0)
        {
            Debug.Log("敵の手札が上限のためドローできません：" + maxHandSize + "枚");
            yield break;
        }

        for (int i = 0; i < drawAmount; i++)
        {
            GameObject moveCard =
                Instantiate(cardPrefab, transform.root);

            RectTransform rt =
                moveCard.GetComponent<RectTransform>();

            SetupCardSize(moveCard, wallCardSize);

            CardController card =
                moveCard.GetComponent<CardController>();

            if (card != null)
            {
                if (card.artworkImage != null)
                    card.artworkImage.sprite = cardBackSprite;

                if (card.costText != null)
                    card.costText.gameObject.SetActive(false);

                if (card.attackText != null)
                    card.attackText.gameObject.SetActive(false);

                if (card.hpText != null)
                    card.hpText.gameObject.SetActive(false);
            }

            rt.position = enemyDeckPosition.position;

            Vector3 target =
                enemyHandCountText.transform.position;

            float t=0;

            while(t<0.25f)
            {
                t += Time.deltaTime;

                rt.position =
                    Vector3.Lerp(
                        enemyDeckPosition.position,
                        target,
                        t/0.25f
                    );

                yield return null;
            }

            Destroy(moveCard);

            int randomIndex =
                Random.Range(0, enemyDeck.Count);

            CardData drawn =
                enemyDeck[randomIndex];

            enemyDeck.RemoveAt(randomIndex);

            enemyHandCards.Add(drawn);

            enemyHandCount++;

            RefreshEnemyHandVisual();

            if (enemyHandCountText != null)
            {
                UpdateEnemyHandCountText();

                if (enemyHandCount >= 10)
                    enemyHandCountText.fontSize = 17;
                else
                    enemyHandCountText.fontSize = 30;
            }

            yield return new WaitForSeconds(0.08f);
        }
    }

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
    public IEnumerator PlayWallSlash(
    GameObject targetWall
    )
    {
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

        yield return new WaitForSeconds(0.3f);

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

        yield return new WaitForSeconds(1.2f);

        if(seSource != null && victoryFanfare != null)
        {
            seSource.PlayOneShot(victoryFanfare);
        }

        yield return new WaitForSeconds(0.3f);

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
    Debug.Log("敵リソースアニメ開始");

    if(enemyHandCards.Count <= 0)
    {
        Debug.Log("敵手札なし");
        yield break;
    }

    int randomIndex =
        Random.Range(
            0,
            enemyHandCards.Count
        );

    CardData selectedCard =
        enemyHandCards[randomIndex];

    enemyHandCards.RemoveAt(randomIndex);

    GameObject moveCard =
        Instantiate(
            cardPrefab,
            transform.root
        );

    RectTransform rt =
        moveCard.GetComponent<RectTransform>();

    SetupCardSize(
        moveCard,
        handCardSize
    );

    CardController card =
        moveCard.GetComponent<CardController>();

    if(card != null)
    {
        card.SetData(selectedCard);
    }

    rt.position =
        enemyHandCountText.transform.position;

    Vector3 start =
        rt.position;

    Vector3 end =
        enemyResourcePosition.position;

    //=====================
    // ゆっくり移動
    //=====================

    float t = 0f;
    float moveTime = 0.6f;

    while(t < moveTime)
    {
        t += Time.deltaTime;

        float rate =
            Mathf.Clamp01(
                t / moveTime
            );

        rt.position =
            Vector3.Lerp(
                start,
                end,
                rate
            );

        yield return null;
    }

    rt.position = end;

    //=====================
    // 到着して停止
    //=====================

    yield return new WaitForSeconds(0.4f);

    //=====================
    // 消える演出
    //=====================

    CanvasGroup cg =
        moveCard.GetComponent<CanvasGroup>();

    if(cg == null)
    {
        cg =
            moveCard.AddComponent
            <CanvasGroup>();
    }

    t = 0f;

    float vanishTime = 0.25f;

    Vector3 startScale =
        Vector3.one;

    Vector3 endScale =
        Vector3.one * 0.6f;

    while(t < vanishTime)
    {
        t += Time.deltaTime;

        float rate =
            Mathf.Clamp01(
                t / vanishTime
            );

        cg.alpha =
            Mathf.Lerp(
                1f,
                0f,
                rate
            );

        rt.localScale =
            Vector3.Lerp(
                startScale,
                endScale,
                rate
            );

        yield return null;
    }

    enemyHandCount--;

    UpdateEnemyHandCountText();

    Destroy(moveCard);

    Debug.Log(
        "敵が手札を1枚リソースへ送った"
    );
}

public IEnumerator EnemyChargeSpecificResourceAnimation(
    CardData selectedCard
)
{
    Debug.Log("敵指定リソースアニメ開始");

    if(selectedCard == null)
        yield break;

    if(enemyHandCards == null)
        yield break;

    if(!enemyHandCards.Contains(selectedCard))
        yield break;

    enemyHandCards.Remove(selectedCard);

    RefreshEnemyHandVisual();

    GameObject moveCard =
        Instantiate(
            cardPrefab,
            transform.root
        );

    RectTransform rt =
        moveCard.GetComponent<RectTransform>();

    SetupCardSize(
        moveCard,
        handCardSize
    );

    CardController card =
        moveCard.GetComponent<CardController>();

    if(card != null)
    {
        card.SetData(selectedCard);
    }

    rt.position =
        enemyHandCountText.transform.position;

    Vector3 start =
        rt.position;

    Vector3 end =
        enemyResourcePosition.position;

    float t = 0f;
    float moveTime = 0.6f;

    while(t < moveTime)
    {
        t += Time.deltaTime;

        float rate =
            Mathf.Clamp01(
                t / moveTime
            );

        rt.position =
            Vector3.Lerp(
                start,
                end,
                rate
            );

        yield return null;
    }

    rt.position = end;

    yield return new WaitForSeconds(0.4f);

    CanvasGroup cg =
        moveCard.GetComponent<CanvasGroup>();

    if(cg == null)
    {
        cg =
            moveCard.AddComponent
            <CanvasGroup>();
    }

    t = 0f;

    float vanishTime = 0.25f;

    Vector3 startScale =
        Vector3.one;

    Vector3 endScale =
        Vector3.one * 0.6f;

    while(t < vanishTime)
    {
        t += Time.deltaTime;

        float rate =
            Mathf.Clamp01(
                t / vanishTime
            );

        cg.alpha =
            Mathf.Lerp(
                1f,
                0f,
                rate
            );

        rt.localScale =
            Vector3.Lerp(
                startScale,
                endScale,
                rate
            );

        yield return null;
    }

    enemyHandCount--;

    UpdateEnemyHandCountText();

    Destroy(moveCard);

    Debug.Log(
        "敵が指定カードをリソースへ送った：" +
        selectedCard.cardName
    );
}

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
            var cg = wall.GetComponent<CanvasGroup>() ?? wall.AddComponent<CanvasGroup>();
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
            triggerController.SetSummonSickness(false);
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
                triggerDrag.PlaceFromShield(turnManager.playerBattleArea);
                layout.Refresh();
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
            else if(isNine || isJoker)
            {
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

    public void ChargeTopDeckToResource()
    {
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
            return false;
        List<CardData> deck = enemy ? enemyDeck : currentDeck;
        WallAreaLayout layout = GetWallLayout(enemy ? enemyWallArea : wallArea);
        if (deck == null || deck.Count == 0 || layout == null || !layout.HasSpace) return false;
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
        return true;
    }
    public void DiscardRandomEnemyHand()
    {
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
    public void RecoverWallByKing()
    {
        RecoverWallByKing(false);
    }

    public void RecoverWallByKing(bool enemy)
    {
        int remaining = Mathf.Max(0, maxKingWallAdditions - (enemy ? enemyKingWallsAdded : playerKingWallsAdded));
        if(remaining == 0) return;
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

    void SetOpeningLock(bool locked)
    {
        CanvasGroup[] groups =
            FindObjectsByType<CanvasGroup>(
                FindObjectsSortMode.None
            );

        foreach(CanvasGroup cg in groups)
        {
            if(cg == null)
                continue;

            // Redrawボタンだけ除外
            if(redrawButton != null &&
            cg.gameObject == redrawButton)
                continue;

            // 確定ボタンだけ除外
            if(confirmButton != null &&
            cg.gameObject == confirmButton)
                continue;

            cg.blocksRaycasts = !locked;
            cg.interactable = !locked;
        }

        Debug.Log(
            locked ?
            "初期手札選択ロックON" :
            "初期手札選択ロック解除"
        );
    }

    void RefreshEnemyHandVisual()
    {
        if(enemyHandArea == null)
            return;

        for(int i = enemyHandArea.childCount - 1; i >= 0; i--)
        {
            Destroy(enemyHandArea.GetChild(i).gameObject);
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
