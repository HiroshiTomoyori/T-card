using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandDealer : MonoBehaviour
{
    [Header("Hand")]
    public Transform handArea;
    public RectTransform deckPosition;

    [Header("Wall Cards")]
    public Transform wallArea;
    public Sprite cardBackSprite;
    public int wallCount = 5;

    [Header("Card Size")]
    public Vector2 handCardSize = new Vector2(75, 112);
    public Vector2 wallCardSize = new Vector2(70, 105);

    [Header("Prefab")]
    public GameObject cardPrefab;

    [Header("Card Data")]
    public List<CardData> cardList = new List<CardData>();

    [Header("Deal Settings")]
    public int dealCount = 5;
    public float dealInterval = 0.15f;
    public float flyTime = 0.25f;

    [Header("UI")]
    public GameObject redrawButton;
    public GameObject confirmButton;

    [Header("Enemy")]
    public TMPro.TextMeshProUGUI enemyHandCountText;
    public Transform enemyWallArea;
    public int enemyWallCount = 5;
    public RectTransform enemyDeckPosition;
    public Image enemyDeckImage;
    public int enemyHandCount = 0;

    int playerWallAliveCount = 0;
    int enemyWallAliveCount = 0;


    [Header("Turn")]
    public TurnManager turnManager;

    [Header("Effects")]
    public Sprite targetGlowSprite;
    public Sprite slashSprite;

    [Header("BGM")]
    public AudioSource bgmSource;
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

    List<CardData> currentDeck = new List<CardData>();
    List<CardData> enemyDeck = new List<CardData>();
    public List<CardData> enemyHandCards =
    new List<CardData>();

    public void DealStart()
    {
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

        StartCoroutine(DealRoutine());

        if (enemyHandCountText != null)
            enemyHandCountText.gameObject.SetActive(true);

        EnemyDraw(5);
    }

    void Start()
    {
        if (enemyHandCountText != null)
            enemyHandCountText.gameObject.SetActive(false);

        if (enemyDeckImage != null)
            enemyDeckImage.gameObject.SetActive(true);
    }
    IEnumerator DealRoutine()
    {
        SetOpeningLock(true);
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
        ShowButtons();

        SetOpeningLock(false);
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
    GameObject CreateHandCard(CardData selectedCard)
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
IEnumerator AnimateCardToHand(RectTransform cardRect)
{
    if (cardRect == null)
        yield break;

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

    void CreateWallCards()
    {
        playerWallAliveCount = wallCount;
        if(wallArea == null) return;
        if(cardPrefab == null) return;

        if(cardBackSprite == null)
        {
            Debug.LogWarning("CardBackSprite が未設定です");
            return;
        }

        ClearWall();

        for(int i = 0; i < wallCount; i++)
        {
            CardData selectedCard = DrawRandomCardData();

            if(selectedCard == null)
                break;

            GameObject wallCard =
                Instantiate(cardPrefab, wallArea);

            wallCard.name =
                "PlayerWallCard_" + selectedCard.cardName;

            SetupCardSize(wallCard, wallCardSize);

            CardController card =
                wallCard.GetComponent<CardController>();

            if(card != null)
            {
                card.SetData(selectedCard);

                if(card.artworkImage != null)
                    card.artworkImage.sprite = cardBackSprite;

                if(card.costText != null)
                    card.costText.gameObject.SetActive(false);

                if(card.attackText != null)
                    card.attackText.gameObject.SetActive(false);

                if(card.hpText != null)
                    card.hpText.gameObject.SetActive(false);

                // これが重要
                card.enabled = false;
            }
            else
            {
                Image img = wallCard.GetComponent<Image>();

                if(img != null)
                    img.sprite = cardBackSprite;
            }

            GameObject slashObj = new GameObject("SlashEffect");
            slashObj.transform.SetParent(wallCard.transform, false);

            RectTransform slashRt = slashObj.AddComponent<RectTransform>();
            slashRt.anchorMin = Vector2.zero;
            slashRt.anchorMax = Vector2.one;
            slashRt.offsetMin = new Vector2(-120, -120);
            slashRt.offsetMax = new Vector2(120, 120);

            Image slashImg = slashObj.AddComponent<Image>();
            slashImg.raycastTarget = false;

            if(slashSprite != null)
            {
                slashImg.sprite = slashSprite;
                slashImg.preserveAspect = true;
            }

            slashImg.color = Color.white;
            slashObj.transform.rotation = Quaternion.Euler(0, 0, -35f);

            CanvasGroup slashCG = slashObj.AddComponent<CanvasGroup>();
            slashCG.alpha = 0f;

            slashObj.transform.SetAsLastSibling();

            LayoutElement layout =
                wallCard.GetComponent<LayoutElement>();

            if(layout != null)
                layout.ignoreLayout = false;
        }
    }

    void CreateSinglePlayerWallCard(CardData selectedCard)
    {
        if(selectedCard == null) return;
        if(wallArea == null) return;
        if(cardPrefab == null) return;

        GameObject wallCard =
            Instantiate(cardPrefab, wallArea);

        wallCard.name =
            "RecoveredWall_" + selectedCard.cardName;

        SetupCardSize(wallCard, wallCardSize);

        CardController card =
            wallCard.GetComponent<CardController>();

        if(card != null)
        {
            card.SetData(selectedCard);

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

        GameObject slashObj = new GameObject("SlashEffect");
        slashObj.transform.SetParent(wallCard.transform, false);

        RectTransform slashRt = slashObj.AddComponent<RectTransform>();
        slashRt.anchorMin = Vector2.zero;
        slashRt.anchorMax = Vector2.one;
        slashRt.offsetMin = new Vector2(-120, -120);
        slashRt.offsetMax = new Vector2(120, 120);

        Image slashImg = slashObj.AddComponent<Image>();
        slashImg.raycastTarget = false;

        if(slashSprite != null)
        {
            slashImg.sprite = slashSprite;
            slashImg.preserveAspect = true;
        }

        slashImg.color = Color.white;
        slashObj.transform.rotation = Quaternion.Euler(0, 0, -35f);

        CanvasGroup slashCG = slashObj.AddComponent<CanvasGroup>();
        slashCG.alpha = 0f;

        slashObj.transform.SetAsLastSibling();

        LayoutElement layout =
            wallCard.GetComponent<LayoutElement>();

        if(layout != null)
            layout.ignoreLayout = false;
    }
    void CreateEnemyWallCards()
    {
        enemyWallAliveCount = enemyWallCount;
        if (enemyWallArea == null) return;
        if (cardPrefab == null) return;
        if (cardBackSprite == null) return;

        // 既存削除
        for (int i = enemyWallArea.childCount - 1; i >= 0; i--)
        {
            Destroy(enemyWallArea.GetChild(i).gameObject);
        }

        for (int i = 0; i < enemyWallCount; i++)
        {
            CardData selectedCard =
                DrawEnemyRandomCardData();

            if (selectedCard == null)
                break;

            GameObject wallCard =
                Instantiate(
                    cardPrefab,
                    enemyWallArea
                );

            wallCard.name =
                "EnemyWallCard_" +
                selectedCard.cardName;

            SetupCardSize(
                wallCard,
                wallCardSize
            );

            CanvasGroup wallCg =
            wallCard.GetComponent<CanvasGroup>();

            if(wallCg == null)
                wallCg = wallCard.AddComponent<CanvasGroup>();

            wallCg.blocksRaycasts = false;
            wallCg.interactable = false;
            //====================
            // SlashEffect追加
            //====================

            GameObject slashObj =
                new GameObject("SlashEffect");

            slashObj.transform.SetParent(
                wallCard.transform,
                false
            );

            RectTransform slashRt =
                slashObj.AddComponent<RectTransform>();

            slashRt.anchorMin = Vector2.zero;
            slashRt.anchorMax = Vector2.one;

            slashRt.offsetMin =
                new Vector2(-120,-120);

            slashRt.offsetMax =
                new Vector2(120,120);

            Image slashImg =
                slashObj.AddComponent<Image>();

            slashImg.raycastTarget = false;

            if(slashSprite != null)
            {
                slashImg.sprite = slashSprite;
                slashImg.preserveAspect = true;
            }
            else
            {
                Debug.LogWarning("slashSprite が未設定です");
            }

            slashImg.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    1f
                );

            slashObj.transform.rotation =
                Quaternion.Euler(
                    0,
                    0,
                    -35f
                );

            CanvasGroup slashCG =
                slashObj.AddComponent<CanvasGroup>();

            slashCG.alpha = 0f;
            slashObj.transform.SetAsLastSibling();
            // ウォールクリック追加
            if (wallCard.GetComponent<EnemyWallClick>() == null)
            {
                wallCard.AddComponent<EnemyWallClick>();
            }
    // ターゲットグロー追加
    GameObject glowObj =
        new GameObject("TargetGlow");

    glowObj.transform.SetParent(
        wallCard.transform,
        false
    );

    RectTransform glowRt =
        glowObj.AddComponent<RectTransform>();

    glowRt.anchorMin = Vector2.zero;
    glowRt.anchorMax = Vector2.one;

    glowRt.offsetMin = new Vector2(-16f,-16f);
    glowRt.offsetMax = new Vector2(16f,16f);

    Image glowImg =
        glowObj.AddComponent<Image>();

    if(targetGlowSprite != null)
    {
        glowImg.sprite = targetGlowSprite;
        glowImg.type = Image.Type.Sliced;
    }

    glowImg.color =
        new Color(
            0.65f,
            1f,
            0.35f,
            1f
        );

    glowImg.raycastTarget = false;

    glowObj.SetActive(false);

    glowObj.transform.SetAsLastSibling();
                CardController card =
                    wallCard.GetComponent<CardController>();

                if (card != null)
                {
                    // 内部データ保持
                    card.SetData(selectedCard);

                    // 見た目だけ裏面化
                    if (card.artworkImage != null)
                        card.artworkImage.sprite = cardBackSprite;

                    // 全情報を隠す
                    if (card.costText != null)
                        card.costText.gameObject.SetActive(false);

                    if (card.attackText != null)
                        card.attackText.gameObject.SetActive(false);

                    if (card.hpText != null)
                        card.hpText.gameObject.SetActive(false);

                    card.enabled = false;
                }

                LayoutElement layout =
                    wallCard.GetComponent<LayoutElement>();

                if (layout != null)
                    layout.ignoreLayout = false;
            }

            
        }
    void ClearHand()
    {
        if (handArea == null) return;

        for (int i = handArea.childCount - 1; i >= 0; i--)
        {
            Destroy(handArea.GetChild(i).gameObject);
        }
    }

    void ClearWall()
    {
        if (wallArea == null) return;

        for (int i = wallArea.childCount - 1; i >= 0; i--)
        {
            Destroy(wallArea.GetChild(i).gameObject);
        }
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
    if(redrawButton != null)
    {
        redrawButton.SetActive(true);

        CanvasGroup cg =
            redrawButton.GetComponent<CanvasGroup>();

        if(cg == null)
            cg =
                redrawButton.AddComponent<CanvasGroup>();

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
            cg =
                confirmButton.AddComponent<CanvasGroup>();

        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;
    }
}

    void HideButtons()
    {
        if (redrawButton != null)
            redrawButton.SetActive(false);

        if (confirmButton != null)
            confirmButton.SetActive(false);
    }

    void SetRedrawButtonInteractable(bool value)
    {
        if (redrawButton == null) return;

        Button btn = redrawButton.GetComponent<Button>();

        if (btn != null)
            btn.interactable = value;
    }

    public void RedrawHand()
    {
        if (!canRedraw) return;
        if (hasRedrawn) return;

        hasRedrawn = true;
        canRedraw = false;

        SetRedrawButtonInteractable(false);

        currentDeck = new List<CardData>(cardList);

        StartCoroutine(DealRoutine());
    }

    public void ConfirmHand()
    {
        HideButtons();
        canRedraw = false;

        if (turnManager != null)
        {
            GameFlowManager flow =
                FindFirstObjectByType<GameFlowManager>();

            bool playerFirst = true;

            if (flow != null)
                playerFirst = flow.PlayerFirst;

            Debug.Log("PlayerFirst = " + playerFirst);

            turnManager.StartFirstTurn(playerFirst);
            Debug.Log("ConfirmHand PlayerFirst = " + playerFirst);
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
        int drawAmount = Mathf.Min(amount, enemyDeck.Count);

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
        if(targetWall == null)
            yield break;

        CanvasGroup cg =
            targetWall.GetComponent<CanvasGroup>();

        if(cg != null && cg.alpha <= 0)
            yield break;

        // 切り裂き演出
        yield return PlayWallSlash(
            targetWall
        );

        enemyHandCount++;

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

        void SortPlayerHand()
    {
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

        ForceHandLayout();
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

IEnumerator DamagePlayerWallRoutine(GameObject wall)
{
    if(wall == null)
        yield break;

    CanvasGroup wallCg =
        wall.GetComponent<CanvasGroup>();

    if(wallCg != null && wallCg.alpha <= 0.01f)
        yield break;

    CardController wallCard =
        wall.GetComponent<CardController>();

    if(wallCard == null)
        wallCard =
            wall.GetComponentInChildren<CardController>();

    if(wallCard == null ||
    wallCard.data == null)
    {
        Debug.Log("破壊できるWallカードなし");
        yield break;
    }

    CardData data =
        wallCard.data;

    Debug.Log(
        "プレイヤーWall破壊 → " +
        data.cardName
    );

    yield return PlayWallSlash(wall);

    bool shieldTrigger =
        data.effectTypes != null &&
        System.Array.Exists(
            data.effectTypes,
            x => x == EffectType.ShieldTrigger
        );

    if(shieldTrigger)
    {
        Debug.Log(
            "シールドトリガー発動：" +
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
            Debug.LogWarning("playerBattleArea が見つからない");
            yield break;
        }

        GameObject triggerCard =
            Instantiate(
                cardPrefab,
                turnManager.playerBattleArea
            );

        triggerCard.name =
            "ShieldTrigger_" + data.cardName;

        CardController triggerController =
            triggerCard.GetComponent<CardController>();

        if(triggerController != null)
        {
            triggerController.SetData(data);
            triggerController.SetSummonSickness(false);

            if(CardEffectManager.I != null)
            {
                CardEffectManager.I.ActivateOnSummon(
                    triggerController
                );
            }
        }

        RectTransform triggerRt =
            triggerCard.GetComponent<RectTransform>();

        if(triggerRt != null)
        {
            triggerRt.localScale =
                Vector3.one * 0.7f;

            triggerRt.sizeDelta =
                new Vector2(160f, 230f);

            triggerRt.anchoredPosition =
                Vector2.zero;
        }

        LayoutElement triggerLayout =
            triggerCard.GetComponent<LayoutElement>();

        if(triggerLayout != null)
        {
            triggerLayout.ignoreLayout = true;
        }

        CanvasGroup triggerCg =
            triggerCard.GetComponent<CanvasGroup>();

        if(triggerCg == null)
            triggerCg =
                triggerCard.AddComponent<CanvasGroup>();

        triggerCg.alpha = 1f;
        triggerCg.blocksRaycasts = true;
        triggerCg.interactable = true;

        CardDrag triggerDrag =
            triggerCard.GetComponent<CardDrag>();

        if(triggerDrag != null)
            triggerDrag.enabled = false;

        if(wallCg == null)
            wallCg =
                wall.AddComponent<CanvasGroup>();

        wallCg.alpha = 0f;
        wallCg.blocksRaycasts = false;
        wallCg.interactable = false;

        playerWallAliveCount--;

        Debug.Log(
            "プレイヤーWall残り：" +
            playerWallAliveCount
        );

        BattleAreaLayout layout =
            turnManager.playerBattleArea
            .GetComponent<BattleAreaLayout>();

        if(layout != null)
        {
            layout.Refresh();
        }

        yield break;
    }

    GameObject handCard =
        Instantiate(
            cardPrefab,
            handArea
        );

    handCard.name =
        "HandCard_" + data.cardName;

    SetupCardSize(
        handCard,
        handCardSize
    );

    CardController handController =
        handCard.GetComponent<CardController>();

    if(handController != null)
    {
        handController.SetData(data);
    }

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

    if(wallCg == null)
        wallCg =
            wall.AddComponent<CanvasGroup>();

    wallCg.alpha = 0f;
    wallCg.blocksRaycasts = false;
    wallCg.interactable = false;

    playerWallAliveCount--;

    Debug.Log(
        "プレイヤーWall残り：" +
        playerWallAliveCount
    );

    SortPlayerHand();
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
        if(currentDeck == null ||
        currentDeck.Count <= 0)
        {
            Debug.Log("山札なし");
            return;
        }

        if(playerWallAliveCount >= 5)
        {
            Debug.Log("Wall最大");
            return;
        }

        CardData card =
            currentDeck[0];

        currentDeck.RemoveAt(0);

        CreateSinglePlayerWallCard(card);

        playerWallAliveCount++;

        Debug.Log(
            "効果発動：Wall回復 → " +
            card.cardName
        );
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

        enemyHandCount--;

        UpdateEnemyHandCountText();

        Debug.Log(
            "効果発動：敵手札墓地 → " +
            removed.cardName
        );
    }

    public IEnumerator DamagePlayerWallAndWait(GameObject wall)
    {
        yield return StartCoroutine(
            DamagePlayerWallRoutine(wall)
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
}