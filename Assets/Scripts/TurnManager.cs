using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    int turnCount = 1;

    int wallBreakCount = 0;
    int maxWallBreakCount = 1;
    bool isPlayerFirst = true;
    bool isBattlePhase = false;

    bool enemyExtraTurnRequested = false;

    public bool IsBattlePhase
    {
        get { return isBattlePhase; }
    }
    public enum FirstPhase
    {
        Draw,
        Resource
    }

    [Header("First Turn Order")]
    public FirstPhase firstPhase = FirstPhase.Draw;

    [Header("Turn Settings")]
    public float enemyTurnWaitTime = 0.8f;

    public Transform playerBattleArea;
    public EnemyResourceManager enemyResourceManager;

    [Header("Managers")]
    public GameStartLogoAnimator gameStartLogoAnimator;
    public ResourcePhaseManager resourcePhaseManager;
    public MainPhaseManager mainPhaseManager;
    public BattlePhaseManager battlePhaseManager;
    public HandDealer handDealer;
    public AttackArrowManager attackArrowManager;

    [Header("T-card AI 1.0")]
    public TCardEnemyAIBrain enemyAIBrain;

    [Header("Turn Logo")]
    public GameObject playerTurnLogo;
    public GameObject enemyTurnLogo;

    [Header("Turn SE")]
    public AudioSource seSource;

    public AudioClip playerTurnSE;
    public AudioClip enemyTurnSE;


    [Header("Enemy Main Phase")]
    public Transform enemyBattleArea;
    public GameObject cardPrefab;
    //public EnemyResourceManager enemyResourceManager;
    GameObject currentAttacker;
    bool selectingTarget = false;

    [Header("Enemy Battle Phase")]
    public Transform playerWallArea;

    public GameObject slashEffectPrefab;
    public AudioClip slashSE;

    [Header("Enemy Layout")]
    public BattleAreaLayout enemyBattleLayout;

    [Header("No Block Button")]
    public GameObject noBlockButton;
    public Vector2 noBlockButtonOffset = new Vector2(0f, -90f);
    
    [Header("UI")]
    public Button endTurnButton;



    [Header("Graveyard")]
    public Transform playerGraveyard;
    public Transform enemyGraveyard;

    [Header("Blocker Slash")]
    public GameObject blockerSlashPrefab;

    [Header("Battle Result UI")]
    public GameObject resultPanel;
    public GameObject retryButton;
    public GameObject exitButton;

    [Header("Hide Objects On Result")]
    public GameObject gameStartLogo;
    //public Transform playerWallArea;
    public Transform enemyWallArea;

    bool isEndingTurn = false;

    bool playerExtraTurnRequested = false;

    List<GameObject> pendingWallTargets =
    new List<GameObject>();

    [Header("Joker Effect Select")]
    public GameObject jokerEffectPanel;
    public Button jokerClearButton;
    public Button jokerExtraTurnButton;

    CardController pendingJokerEffectCard;
    //bool playerExtraTurnRequested = false;

    public float resultWaitTime = 1.5f;

    void Awake()
    {
        if(enemyAIBrain == null)
        {
            enemyAIBrain =
                FindFirstObjectByType<TCardEnemyAIBrain>();
        }

        if(enemyAIBrain == null)
        {
            Debug.LogWarning(
                "TCardEnemyAIBrain が見つかりません"
            );
        }
        else
        {
            Debug.Log(
                "T-card AI 1.0 接続完了"
            );
        }
    }

    public void StartFirstTurn(bool playerFirst)
    {
        isPlayerFirst = playerFirst;
        turnCount = 1;

        StartCoroutine(FirstTurnRoutine());
    }

    bool waitingBlockSelect = false;
    bool enemyAttackBlocked = false;
    CardController pendingEnemyAttacker;
    GameObject pendingEnemyTargetWall;

IEnumerator FirstTurnRoutine()
{
    if(gameStartLogoAnimator != null)
    {
        gameStartLogoAnimator.Play();

        yield return new WaitForSeconds(2.4f);
    }

    // プレイヤー先攻
    if(isPlayerFirst)
    {
        Debug.Log("プレイヤー先攻1ターン目");

        StartPlayerTurn(true);

        yield break;
    }

    // 敵先攻
    else
    {
        Debug.Log("敵先攻1ターン目");

        yield return StartCoroutine(
            EnemyTurnRoutine()
        );

        yield return ShowTurnLogo(
            playerTurnLogo,
            playerTurnSE
        );

        StartPlayerTurn();

        yield break;
    }
}

    void StartDrawPhase()
    {
        Debug.Log("Draw Phase");

        if(handDealer != null)
        {
            handDealer.DrawOneCard();
        }
    }

    void StartResourcePhase()
    {
        Debug.Log("Resource Phase");

        if(resourcePhaseManager != null)
        {
            resourcePhaseManager.StartResourcePhase();
        }
    }

    void StartMainPhase()
    {
        Debug.Log("Main Phase");

        if (mainPhaseManager != null)
        {
            mainPhaseManager.StartMainPhase();
        }
        else
        {
            Debug.LogError("MainPhaseManager が未設定");
        }
    }

public void StartBattleByAttackSelect(GameObject attacker)
{
    if(attacker == null)
        return;

    CardController attackerCard =
        attacker.GetComponent<CardController>();

    if(attackerCard == null)
        return;

    if(attackerCard.isTapped)
    {
        Debug.Log("タップ済みなので攻撃不可");
        return;
    }

    if(attackerCard.hasSummonSickness)
    {
        Debug.Log("召喚酔い中なので攻撃不可");
        return;
    }

    if(attackerCard.data != null &&
       attackerCard.data.effectTypes != null &&
       System.Array.Exists(
           attackerCard.data.effectTypes,
           x => x == EffectType.CannotAttack
       ))
    {
        Debug.Log("攻撃不可カード");
        return;
    }

    // すでに攻撃対象選択中なら、攻撃カードを切り替える
    if(selectingTarget)
    {
        if(currentAttacker == attacker)
            return;

        currentAttacker = attacker;
        wallBreakCount = 0;
        pendingWallTargets.Clear();

        maxWallBreakCount = 1;

        if(attackerCard.data != null &&
           attackerCard.data.effectTypes != null &&
           System.Array.Exists(
               attackerCard.data.effectTypes,
               x => x == EffectType.DoubleWallBreak
           ))
        {
            maxWallBreakCount = 2;
        }

        if(maxWallBreakCount >= 2 &&
           attackArrowManager != null)
        {
            attackArrowManager.SetPlayerArrowColor(
                new Color(0.8f, 0.2f, 1f)
            );
        }
        else if(attackArrowManager != null)
        {
            attackArrowManager.SetPlayerArrowColor(
                new Color(0.55f, 1f, 0.55f, 1f)
            );
        }

        HideAttackArrow();

        Debug.Log("攻撃カード切り替え：" + attacker.name);
        Debug.Log("攻撃対象を選択してください");

        return;
    }

    currentAttacker = attacker;
    wallBreakCount = 0;
    maxWallBreakCount = 1;
    pendingWallTargets.Clear();

    if(attackerCard.data != null &&
       attackerCard.data.effectTypes != null &&
       System.Array.Exists(
           attackerCard.data.effectTypes,
           x => x == EffectType.DoubleWallBreak
       ))
    {
        maxWallBreakCount = 2;
    }

    selectingTarget = true;

    if(maxWallBreakCount >= 2 &&
       attackArrowManager != null)
    {
        attackArrowManager.SetPlayerArrowColor(
            new Color(0.8f, 0.2f, 1f)
        );
    }
    else if(attackArrowManager != null)
    {
        attackArrowManager.SetPlayerArrowColor(
            new Color(0.55f, 1f, 0.55f, 1f)
        );
    }

    SetEnemyWallClickable(true);

    if(!isBattlePhase)
    {
        if(mainPhaseManager != null)
        {
            mainPhaseManager.EndMainPhase();
        }

        if(battlePhaseManager != null)
        {
            battlePhaseManager.StartBattlePhase();
        }

        isBattlePhase = true;

        LockPlayerHand(true);

        ShowAttackableCards();
    }

    Debug.Log("攻撃カード選択：" + attacker.name);
    Debug.Log("攻撃対象を選択してください");
}

public void SelectAttackTarget(GameObject target)
{
    if(!selectingTarget)
        return;

    if(currentAttacker == null)
        return;

    if(target == null)
        return;

    CardController attackerCard =
        currentAttacker.GetComponent<CardController>();

    if(attackerCard == null)
        return;

    Debug.Log(
        currentAttacker.name +
        " が " +
        target.name +
        " を攻撃"
    );

    CardController blocker =
        SelectEnemyBlocker(attackerCard);

    if(blocker != null)
    {
        Debug.Log(
            "CPUがブロック：" +
            blocker.data.cardName
        );

        ResolveCardBattle(
            attackerCard,
            blocker
        );

        attackerCard.Tap();
        attackerCard.SetAttackable(false);

        CardActionIcon icon =
            attackerCard.GetComponent<CardActionIcon>();

        if(icon != null)
            icon.HideAll();

        wallBreakCount = 0;
        maxWallBreakCount = 1;

        selectingTarget = false;
        currentAttacker = null;

        SetEnemyWallClickable(false);
        HideAttackArrow();

        return;
    }

    pendingWallTargets.Add(target);

    wallBreakCount++;

    Debug.Log(
        "Wall選択：" +
        wallBreakCount +
        "/" +
        maxWallBreakCount
    );

    // まだ2枚目選択が必要
    if(wallBreakCount < maxWallBreakCount &&
    handDealer != null &&
    !handDealer.IsEnemyWallZero())
    {
        Debug.Log("2枚目のWallを選択してください");

        return;
    }

    //=========================
    // ここで同時破壊
    //=========================

    foreach(GameObject wall in pendingWallTargets)
    {
        if(handDealer != null)
        {
            handDealer.DamageEnemyWallFromAttack(
                wall
            );
        }
    }

    pendingWallTargets.Clear();

    attackerCard.Tap();
    attackerCard.SetAttackable(false);

    CardActionIcon attackerIcon =
        attackerCard.GetComponent<CardActionIcon>();

    if(attackerIcon != null)
    {
        attackerIcon.HideAll();
    }

    wallBreakCount = 0;
    maxWallBreakCount = 1;

    selectingTarget = false;
    currentAttacker = null;

    SetEnemyWallClickable(false);

    HideAttackArrow();
    pendingWallTargets.Clear();
}

CardController SelectEnemyBlocker(
    CardController attacker
)
{
    if(attacker == null ||
       attacker.data == null)
    {
        return null;
    }

    int enemyWallCount =
        GetAliveEnemyWallCount();

    List<CardController> blockers =
        new List<CardController>();

    if(enemyBattleArea != null)
    {
        for(int i = 0;
            i < enemyBattleArea.childCount;
            i++)
        {
            CardController card =
                enemyBattleArea
                .GetChild(i)
                .GetComponent<CardController>();

            if(card == null ||
               card.data == null)
            {
                continue;
            }

            if(card.isTapped)
                continue;

            if(!IsGuardCard(card.data))
                continue;

            blockers.Add(card);
        }
    }

    if(enemyAIBrain != null)
    {
        return enemyAIBrain
            .SelectBestBlocker(
                attacker,
                blockers,
                enemyWallCount
            );
    }

    //=========================
    // AI Brainがない場合の旧処理
    //=========================

    bool dangerousAttack =
        IsMonarchCard(attacker.data) ||
        attacker.data.name.Contains("K") ||
        attacker.data.name.Contains("Joker");

    if(enemyWallCount >= 5 &&
       !dangerousAttack &&
       attacker.data.power <= 4)
    {
        return null;
    }

    CardController bestBlocker = null;
    int bestScore = -999;

    foreach(CardController card in blockers)
    {
        card.data.SetPowerFromName();

        int score = 0;

        if(card.data.power == 2)
            score += 40;

        if(card.data.power == 3)
            score += 40;

        if(card.data.power == 4)
            score += 35;

        if(dangerousAttack)
            score += 80;
        else
            score += 25;

        score -= card.data.power;

        if(score > bestScore)
        {
            bestScore = score;
            bestBlocker = card;
        }
    }

    return bestBlocker;
}

int GetAliveEnemyWallCount()
{
    if(handDealer == null)
        return 0;

    if(handDealer.enemyWallArea == null)
        return 0;

    int count = 0;

    for(int i = 0; i < handDealer.enemyWallArea.childCount; i++)
    {
        CanvasGroup cg =
            handDealer.enemyWallArea.GetChild(i)
            .GetComponent<CanvasGroup>();

        if(cg != null && cg.alpha <= 0.01f)
            continue;

        count++;
    }

    return count;
}
    public void SelectEnemyBattleCardTarget(GameObject target)
    {
        if(!selectingTarget)
            return;

        if(currentAttacker == null)
            return;

        if(target == null)
            return;

        CardController attackerCard =
            currentAttacker.GetComponent<CardController>();

        if(attackerCard == null)
            return;

        CardController targetCard =
            target.GetComponent<CardController>();

        if(targetCard == null)
            return;

        if(!targetCard.isTapped)
            return;

        StartCoroutine(
            BattleCardVsCardRoutine(
                attackerCard,
                targetCard
            )
        );

        selectingTarget = false;
    }
    IEnumerator DestroyEnemyBattleCardRoutine(
    CardController attackerCard,
    CardController targetCard
    )
    {
        if(targetCard == null)
            yield break;

        RectTransform targetRt =
            targetCard.GetComponent<RectTransform>();

        if(slashEffectPrefab != null && targetRt != null)
        {
            GameObject slash =
                Instantiate(
                    slashEffectPrefab,
                    targetRt.parent
                );

            RectTransform slashRt =
                slash.GetComponent<RectTransform>();

            if(slashRt != null)
            {
                slashRt.position = targetRt.position;
                slashRt.localScale = Vector3.one;
            }

            Destroy(slash, 0.5f);
        }

        if(seSource != null && slashSE != null)
        {
            seSource.PlayOneShot(slashSE);
        }

        yield return new WaitForSeconds(0.25f);

        //Destroy(targetCard.gameObject);
        SendToGraveyard(targetCard.gameObject, enemyGraveyard);

        attackerCard.Tap();
        attackerCard.SetAttackable(false);

        CardActionIcon icon =
            attackerCard.GetComponent<CardActionIcon>();

        if(icon != null)
        {
            icon.HideAll();
        }

        currentAttacker = null;

        HideAttackArrow();
    }

    IEnumerator BattleCardVsCardRoutine(
    CardController attackerCard,
    CardController targetCard
)
{
    if(attackerCard == null || targetCard == null)
        yield break;

    int attackerPower = attackerCard.data.power;
    int targetPower = targetCard.data.power;

    Debug.Log(
        "バトル比較：" +
        attackerPower +
        " VS " +
        targetPower
    );

    yield return StartCoroutine(
        PlayBlockerSlash(targetCard.gameObject)
    );

    if(attackerPower > targetPower)
    {
        Debug.Log("攻撃側勝利");

        SendToGraveyard(
            targetCard.gameObject,
            enemyGraveyard
        );

        attackerCard.Tap();
        attackerCard.SetAttackable(false);

        CardActionIcon icon =
            attackerCard.GetComponent<CardActionIcon>();

        if(icon != null)
            icon.HideAll();
    }
    else if(attackerPower < targetPower)
    {
        Debug.Log("防御側勝利");

        SendToGraveyard(
            attackerCard.gameObject,
            playerGraveyard
        );
    }
    else
    {
        Debug.Log("相打ち");

        SendToGraveyard(
            targetCard.gameObject,
            enemyGraveyard
        );

        SendToGraveyard(
            attackerCard.gameObject,
            playerGraveyard
        );
    }

    currentAttacker = null;

    HideAttackArrow();
}
IEnumerator AttackRoutine(
    CardController attackerCard,
    GameObject target
)
{
    yield return new WaitForSeconds(0.4f);

    // タップ敵カードなら
    CardController targetCard =
        target.GetComponent<CardController>();

    if(targetCard != null)
    {
        Debug.Log(
            "敵バトルカード攻撃"
        );

Destroy(
    targetCard.gameObject
);
    }
    else
    {
        Debug.Log(
            "敵Wall攻撃"
        );

if(handDealer != null)
{
    handDealer.DamageEnemyWall(target);
}
    }

    attackerCard.Tap();

    attackerCard.SetAttackable(false);

    if(attackArrowManager != null)
    {
        attackArrowManager.Hide();
    }

    currentAttacker = null;
}

    public void OnResourcePhaseComplete()
    {
        Debug.Log("Resource完了");

        if (ShouldAutoEndEarlyTurn())
        {
            Debug.Log("最初の5ターン・行動不能のため自動ターン終了");
            EndPlayerTurn();
            return;
        }

        StartMainPhase();
    }
    public bool IsSelectingTarget()
    {
        return selectingTarget;
    }

public GameObject GetCurrentAttacker()
{
    return currentAttacker;
}
public bool CanDirectAttack()
{
    if(handDealer == null)
        return false;

    return handDealer.IsEnemyWallZero();
}
public void ShowAttackArrowTo(GameObject target)
{
    Debug.Log("ShowAttackArrowTo 呼ばれた");

    if(!selectingTarget)
        return;

    if(currentAttacker == null)
        return;

    if(target == null)
        return;

    if(attackArrowManager == null)
        return;

    RectTransform from =
        currentAttacker.GetComponent<RectTransform>();

    RectTransform to =
        target.GetComponent<RectTransform>();

    attackArrowManager.ShowPlayerArrow(
        from,
        to
    );
}

public void HideAttackArrow()
{
    if(attackArrowManager != null)
        attackArrowManager.Hide();
}

    public void EndPlayerTurn()
    {    
    
    if(HandDealer.IsRedrawSelecting)
    {
        Debug.Log("引き直し選択中のためターン終了不可");
        return;
    }


    ResourcePhaseManager resourcePhase =
    FindFirstObjectByType<ResourcePhaseManager>();

    if(resourcePhase != null)
    {
        resourcePhase.EndResourcePhase();
    }
        if(isEndingTurn)
            return;

        isEndingTurn = true;

        SetEndTurnButton(false);

        StartCoroutine(EndPlayerTurnRoutine());
    }

    IEnumerator EndPlayerTurnRoutine()
    {
        Debug.Log("自ターン終了");

        selectingTarget = false;
        currentAttacker = null;
        isBattlePhase = false;
        HideAttackArrow();
        HideAllCardIcons();

        LockPlayerHand(false);

    if(playerExtraTurnRequested)
    {
        playerExtraTurnRequested = false;

        Debug.Log("Joker効果：エクストラターン開始");

        yield return ShowTurnLogo(
            playerTurnLogo,
            playerTurnSE
    );

    StartPlayerTurn();

    isEndingTurn = false;
    yield break;
    }

if(playerExtraTurnRequested)
{
    playerExtraTurnRequested = false;

    Debug.Log("JOKER効果：追加ターン開始");

    yield return ShowTurnLogo(
        playerTurnLogo,
        playerTurnSE
    );

    StartPlayerTurn();

    isEndingTurn = false;

    yield break;
}

yield return StartCoroutine(EnemyTurnRoutine());

yield return ShowTurnLogo(
    playerTurnLogo,
    playerTurnSE
);

StartPlayerTurn();

isEndingTurn = false;
    }

void StartPlayerTurn(bool firstTurn = false)
{
    if(!firstTurn)
    {
        turnCount++;
    }

    Debug.Log("自ターン開始：" + turnCount);

    ClearPlayerSummonSickness();

    UntapPlayerBattleCards();

    ResourceManager rm =
        FindFirstObjectByType<ResourceManager>();

    if(rm != null)
    {
        rm.RecoverResource();
    }

    // 初ターン先攻だけドローしない
    if(!(isPlayerFirst && turnCount == 1))
    {
        StartDrawPhase();
    }

    SetEndTurnButton(true);

    StartResourcePhase();
}

void UntapPlayerBattleCards()
{
    if(playerBattleArea == null)
    {
        Debug.LogWarning("playerBattleArea が未設定");
        return;
    }

    for(int i = 0; i < playerBattleArea.childCount; i++)
    {
        CardController card =
            playerBattleArea.GetChild(i)
            .GetComponent<CardController>();

        if(card != null)
        {
            card.Untap();
        }
    }

    Debug.Log("自軍バトルカードをアンタップ");
}

    public void SelectDirectAttackTarget(GameObject target)
    {
        if(!selectingTarget)
            return;

        if(currentAttacker == null)
            return;

        if(target == null)
            return;

        if(!CanDirectAttack())
        {
            Debug.Log("敵ウォールが残っているため直接攻撃不可");
            return;
        }

        Debug.Log(
            currentAttacker.name +
            " が敵本体へ直接攻撃"
        );

        CardController attackerCard =
            currentAttacker.GetComponent<CardController>();

        if(attackerCard != null)
        {
            attackerCard.Tap();
            // 攻撃済みなので消す
            attackerCard.SetAttackable(false);

        }

        selectingTarget = false;
        currentAttacker = null;
        HideAttackArrow();

        if(handDealer != null)
        {
            handDealer.StartVictorySequence();
        }

        StartBattleEndResult();
    }

void ShowAttackableCards()
{
    if(playerBattleArea == null)
        return;

    for(int i = 0; i < playerBattleArea.childCount; i++)
    {
        CardController card =
            playerBattleArea.GetChild(i)
            .GetComponent<CardController>();

        if(card == null)
            continue;

        // 一旦全解除
        card.SetAttackable(false);

        CardActionIcon icon =
            card.GetComponent<CardActionIcon>();

        if(icon != null)
        {
            icon.HideAll();
        }

        // タップ済み
        if(card.isTapped)
            continue;

        // 召喚酔い
        if(card.hasSummonSickness)
            continue;

        // 攻撃不可効果
        if(card.data != null &&
        card.data.effectTypes != null &&
        System.Array.Exists(
            card.data.effectTypes,
            x => x == EffectType.CannotAttack
        ))
        {
            continue;
        }

        // 攻撃可能
        card.SetAttackable(true);

        if(icon != null)
        {
            icon.ShowAttackIcon();
        }
    }

    Debug.Log("攻撃可能カード表示");
}

void SetEnemyWallClickable(bool value)
{
    if(handDealer == null)
        return;

    if(handDealer.enemyWallArea == null)
        return;

    for(int i = 0; i < handDealer.enemyWallArea.childCount; i++)
    {
        GameObject wall =
            handDealer.enemyWallArea.GetChild(i).gameObject;

        CanvasGroup cg =
            wall.GetComponent<CanvasGroup>();

        if(cg == null)
            cg = wall.AddComponent<CanvasGroup>();

        if(cg.alpha <= 0.01f)
            continue;

        cg.blocksRaycasts = value;
        cg.interactable = value;
    }

    Debug.Log(
        value ?
        "敵WallクリックON" :
        "敵WallクリックOFF"
    );
}

    void LockPlayerHand(bool locked)
{
    if(handDealer == null)
        return;

    if(handDealer.handArea == null)
        return;

    for(int i = 0; i < handDealer.handArea.childCount; i++)
    {
        Transform child =
            handDealer.handArea.GetChild(i);

        CanvasGroup cg =
            child.GetComponent<CanvasGroup>();

        if(cg == null)
            cg = child.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = locked ? 0.45f : 1f;
        cg.blocksRaycasts = !locked;
        cg.interactable = !locked;

        CardDrag drag =
            child.GetComponent<CardDrag>();

        if(drag != null)
        {
            drag.enabled = !locked;
        }
    }

    Debug.Log(
        locked ?
        "手札をロックしました" :
        "手札ロック解除"
    );
}

        IEnumerator EnemyTurnRoutine()
        {
            Debug.Log("=== 敵ターン開始 ===");

            ClearEnemySummonSickness();
            if(enemyResourceManager != null)
            {
                enemyResourceManager.RecoverResource();
            }
            yield return ShowTurnLogo(
                enemyTurnLogo,
                enemyTurnSE
            );

            if(handDealer != null)
            {
                handDealer.EnemyDraw(1);

                yield return new WaitForSeconds(0.5f);
            }

            UntapEnemyBattleCards();

            yield return StartCoroutine(
                EnemyResourcePhase()
            );

            yield return StartCoroutine(
                EnemyMainPhase()
            );

            yield return StartCoroutine(
                EnemyBattlePhase()
            );

            Debug.Log("=== 敵ターン終了 ===");

            if(enemyExtraTurnRequested)
            {
                enemyExtraTurnRequested = false;

                Debug.Log("敵エクストラターン発動");

                yield return new WaitForSeconds(0.5f);

                yield return StartCoroutine(
                    EnemyTurnRoutine()
                );

                yield break;
            }
        }

CardData SelectEnemyResourceChargeCard()
{
    if(handDealer == null)
        return null;

    if(handDealer.enemyHandCards == null)
        return null;

    if(handDealer.enemyHandCards.Count == 0)
        return null;

    // AI Brainがある場合は、新AIで選択
    if(enemyAIBrain != null)
    {
        int enemyWallCount =
            GetAliveEnemyWallCount();

        int playerFieldCount =
            playerBattleArea != null
            ? playerBattleArea.childCount
            : 0;

        CardData aiSelectedCard =
            enemyAIBrain.SelectResourceCard(
                handDealer.enemyHandCards,
                enemyResourceManager != null
                    ? enemyResourceManager.maxResource
                    : 0,
                enemyWallCount,
                playerFieldCount
            );

        if(aiSelectedCard != null)
        {
            Debug.Log(
                "AI 1.0 リソース選択：" +
                GetCardName(aiSelectedCard)
            );
        }
        else
        {
            Debug.Log(
                "AI 1.0 リソースチャージ見送り"
            );
        }

        return aiSelectedCard;
    }

    //=========================
    // AI Brainがない場合の旧処理
    //=========================

    bool lowPriorityCharge = false;

    if(enemyResourceManager != null &&
       enemyResourceManager.currentResource >= 13)
    {
        lowPriorityCharge = true;
    }

    CardData bestCard = null;
    int bestScore = -999;

    foreach(CardData card in handDealer.enemyHandCards)
    {
        if(card == null)
            continue;

        card.SetPowerFromName();

        int score = 0;

        if(turnCount <= 10)
        {
            if(card.power == 2)
                score += 40;

            if(card.power == 3)
                score += 40;

            if(card.power == 9)
                score += 35;

            if(GetCardName(card).Contains("Q"))
                score += 35;
        }

        if(GetCardName(card).Contains("K"))
            score -= 80;

        if(card.power == 1)
            score -= 80;

        if(GetCardName(card).Contains("Joker"))
            score -= 100;

        if(card.power >= 2 &&
           card.power <= 6)
        {
            score += 10;
        }

        if(score > bestScore)
        {
            bestScore = score;
            bestCard = card;
        }
    }

    // リソース13未満なら最低1枚チャージ
    if(!lowPriorityCharge)
    {
        if(bestCard != null)
        {
            Debug.Log(
                "旧CPUリソースチャージ：" +
                GetCardName(bestCard)
            );

            return bestCard;
        }

        return handDealer.enemyHandCards[
            Random.Range(
                0,
                handDealer.enemyHandCards.Count
            )
        ];
    }

    // 13以降は慎重
    if(bestScore < 20)
    {
        Debug.Log("旧CPUリソース温存");
        return null;
    }

    return bestCard;
}

IEnumerator EnemyResourcePhase()
{
    Debug.Log("敵 Resource Phase");

    CardData chargeCard =
        SelectEnemyResourceChargeCard();

    if(chargeCard == null)
    {
        Debug.Log("敵はリソースチャージを見送り");
        yield return new WaitForSeconds(0.3f);
        yield break;
    }

    if(handDealer != null)
    {
        yield return StartCoroutine(
            handDealer.EnemyChargeSpecificResourceAnimation(
                chargeCard
            )
        );
    }

    if(enemyResourceManager != null)
    {
        enemyResourceManager.AddResource();
    }

    yield return new WaitForSeconds(0.5f);
}

IEnumerator EnemyMainPhase()
{
    Debug.Log("=== Enemy Main Phase 開始 ===");

    yield return new WaitForSeconds(0.5f);

    if(handDealer == null)
    {
        Debug.LogWarning("HandDealer が未設定");
        yield break;
    }

    if(enemyResourceManager == null)
    {
        Debug.LogWarning(
            "EnemyResourceManager が未設定"
        );

        yield break;
    }

    if(enemyBattleArea == null)
    {
        Debug.LogWarning(
            "enemyBattleArea が未設定"
        );

        yield break;
    }

    if(cardPrefab == null)
    {
        Debug.LogWarning(
            "cardPrefab が未設定"
        );

        yield break;
    }

    int maxSummons = 1;

    if(enemyAIBrain != null)
    {
        maxSummons =
            enemyAIBrain.GetMaxSummonsPerTurn();
    }

    int summonCount = 0;

    while(summonCount < maxSummons)
    {
        if(handDealer.enemyHandCards == null ||
           handDealer.enemyHandCards.Count == 0)
        {
            Debug.Log(
                "敵手札なし：メインフェイズ終了"
            );

            yield break;
        }

        List<CardData> summonableCards =
            new List<CardData>();

        foreach(CardData card
            in handDealer.enemyHandCards)
        {
            if(card == null)
                continue;

            card.SetPowerFromName();
            card.SetCostFromName();

            Debug.Log(
                "敵手札コスト確認：" +
                GetCardName(card) +
                " cost=" +
                card.cost +
                " / enemyResource=" +
                enemyResourceManager.currentResource
            );

            if(card.cost <=
               enemyResourceManager.currentResource)
            {
                summonableCards.Add(card);
            }
        }

        if(summonableCards.Count == 0)
        {
            Debug.Log(
                "召喚可能カードなし：敵メイン終了"
            );

            yield break;
        }

        CardData selectedCard =
            SelectEnemySummonCard(
                summonableCards
            );

        if(selectedCard == null)
        {
            Debug.Log(
                "AI判断：これ以上召喚しない"
            );

            yield break;
        }

        bool usedResource =
            enemyResourceManager.UseResource(
                selectedCard.cost
            );

        if(!usedResource)
        {
            Debug.LogWarning(
                "敵リソース不足：" +
                GetCardName(selectedCard)
            );

            yield break;
        }

        handDealer.enemyHandCards.Remove(
            selectedCard
        );

        handDealer.enemyHandCount--;

        GameObject obj =
            Instantiate(
                cardPrefab,
                enemyBattleArea
            );

        CardController controller =
            obj.GetComponent<CardController>();

        if(controller != null)
        {
            controller.SetData(
                selectedCard
            );

            bool noSummonSickness =
                controller.data.effectTypes != null &&
                System.Array.Exists(
                    controller.data.effectTypes,
                    x =>
                        x ==
                        EffectType.NoSummonSickness
                );

            controller.SetSummonSickness(
                !noSummonSickness
            );

            if(!noSummonSickness)
            {
                controller.SetAttackable(false);
            }

            if(CardEffectManager.I != null)
            {
                CardEffectManager.I
                    .ActivateOnSummon(
                        controller,
                        false,
                        true
                    );
            }
            else
            {
                Debug.LogWarning(
                    "CardEffectManager が未配置"
                );
            }
        }

        if(obj.GetComponent<
            EnemyBattleCardTargetClick>() == null)
        {
            obj.AddComponent<
                EnemyBattleCardTargetClick>();
        }

        RectTransform rt =
            obj.GetComponent<RectTransform>();

        if(rt != null)
        {
            rt.localScale =
                Vector3.one * 0.7f;

            rt.sizeDelta =
                new Vector2(
                    160f,
                    230f
                );

            rt.anchoredPosition =
                Vector2.zero;
        }

        handDealer.UpdateEnemyHandCountText();

        if(enemyBattleLayout != null)
        {
            enemyBattleLayout.Refresh();
        }

        summonCount++;

        Debug.Log(
            "敵が召喚：" +
            selectedCard.cardName +
            " (" +
            summonCount +
            "/" +
            maxSummons +
            ")"
        );

        yield return new WaitForSeconds(0.5f);

        /*
         * A・9・Jokerなどの効果でゲーム状態や
         * 手札・リソースが変わるため、次のwhileで
         * 召喚候補を最初から作り直す。
         */
    }

    Debug.Log(
        "敵の召喚回数上限：" +
        summonCount
    );
}

CardData SelectEnemySummonCard(
    List<CardData> summonableCards
)
{
        if(summonableCards == null ||
       summonableCards.Count == 0)
    {
        return null;
    }

    if(enemyAIBrain != null)
    {
        List<CardController> playerCards =
            TCardAIUnityBridge.GetCards(
                playerBattleArea
            );

        List<CardController> enemyCards =
            TCardAIUnityBridge.GetCards(
                enemyBattleArea
            );

        int playerWallCount =
            GetAlivePlayerWallCount();

        int enemyWallCount =
            GetAliveEnemyWallCount();

        CardData aiSelectedCard =
            enemyAIBrain.SelectSummonCard(
                summonableCards,
                enemyWallCount,
                playerWallCount,
                playerCards,
                enemyCards
            );

        if(aiSelectedCard != null)
        {
            Debug.Log(
                "AI 1.0 召喚選択：" +
                GetCardName(aiSelectedCard)
            );
        }
        else
        {
            Debug.Log(
                "AI 1.0 召喚候補なし"
            );
        }

        return aiSelectedCard;
    }
    CardData bestCard = null;
    int bestScore = -999;

    foreach(CardData card in summonableCards)
    {
        if(card == null)
            continue;

        card.SetPowerFromName();

        int score = 0;

        string name = GetCardName(card);

// Aceは破壊対象がいないなら召喚候補から除外
if(card.power == 1 && !HasPlayerFaceCard())
{
    Debug.Log("CPU召喚除外：Ace空撃ち " + name);
    continue;
}

// Reverseは有効対象がないなら召喚候補から除外
if(IsReverseCard(card) && !HasUsefulReverseTarget())
{
    Debug.Log("CPU召喚除外：Reverse空撃ち " + name);
    continue;
}

        // Reverseカードはメリットがないなら出さない
if(IsReverseCard(card))
{
    if(HasUsefulReverseTarget())
    {
        score += 50;
    }
    else
    {
        score -= 100;
    }
}

    // Aceは破壊対象がいないなら空撃ちしない
    if(card.power == 1)
    {
        if(HasPlayerFaceCard())
        {
            score += 80;
        }
        else
        {
            score -= 100;
        }
    }

        // 前半は5を積極的にプレイ
        if(turnCount <= 10 && card.power == 5)
            score += 50;

        // 後半はKing優先
        if(turnCount > 10 &&
        GetCardName(card).Contains("K"))
            score += 45;

        // Guardカードは状況次第
        if(IsGuardCard(card))
        {
            if(HasPlayerAttackableCard())
                score += 30;
            else
                score -= 40;
        }

        // 高パワーは少し優先
        score += card.power;

        if(score > bestScore)
        {
            bestScore = score;
            bestCard = card;
        }
    }

    if(bestCard == null)
    {
        return summonableCards[
            Random.Range(
                0,
                summonableCards.Count
            )
        ];
    }

    if(bestCard == null)
    {
        Debug.Log("CPU召喚選択：候補なし");
        return summonableCards[0];
    }

    Debug.Log(
        "CPU召喚選択：" +
        GetCardName(bestCard) +
        " score=" +
        bestScore
    );

    // どれか選ばれていれば、スコアが低くても召喚する
    if(bestCard != null)
    {
        Debug.Log(
            "CPU召喚選択：" +
            GetCardName(bestCard) +
            " score=" +
            bestScore
        );

        return bestCard;
    }

    // 本当に何も選べなかった時だけランダム
    Debug.Log("CPU召喚：強制ランダム選択");

    return summonableCards[
        Random.Range(
            0,
            summonableCards.Count
        )
    ];
}

string GetCardName(CardData data)
{
    if(data == null)
        return "";

    if(!string.IsNullOrEmpty(data.cardName))
        return data.cardName;

    return data.name;
}

bool IsReverseCard(CardData card)
{
    if(card == null || card.effectTypes == null)
        return false;

    return System.Array.Exists(
        card.effectTypes,
        x => x == EffectType.TapAllEnemyBattle
    );
}

bool HasUsefulReverseTarget()
{
    // まずは仮で「プレイヤー場にカードがある時だけ有効」にする
    if(playerBattleArea == null)
        return false;

    return playerBattleArea.childCount > 0;
}

bool HasPlayerFaceCard()
{
    if(playerBattleArea == null)
        return false;

    for(int i = 0; i < playerBattleArea.childCount; i++)
    {
        CardController card =
            playerBattleArea.GetChild(i)
            .GetComponent<CardController>();

        if(card == null || card.data == null)
            continue;

        string name = GetCardName(card.data);

        if(name.Contains("J") ||
           name.Contains("Q") ||
           name.Contains("K") ||
           name.Contains("Joker"))
        {
            return true;
        }
    }

    return false;
}
bool IsGuardCard(CardData card)
{
    if(card == null)
        return false;

    if(card.effectTypes == null)
        return false;

    return System.Array.Exists(
        card.effectTypes,
        x => x == EffectType.BlockOnly
    );
}

bool HasPlayerAttackableCard()
{
    if(playerBattleArea == null)
        return false;

    for(int i = 0; i < playerBattleArea.childCount; i++)
    {
        CardController card =
            playerBattleArea.GetChild(i)
            .GetComponent<CardController>();

        if(card == null)
            continue;

        if(card.isTapped)
            continue;

        if(card.hasSummonSickness)
            continue;

        if(card.data != null &&
        card.data.effectTypes != null &&
        System.Array.Exists(
            card.data.effectTypes,
            x => x == EffectType.CannotAttack
        ))
        {
            continue;
        }

        return true;
    }

    return false;
}

IEnumerator EnemyBattlePhase()
{
    Debug.Log("=== Enemy Battle Phase ===");

    if(enemyBattleArea == null ||
       playerWallArea == null)
    {
        yield break;
    }

    // 現在の敵バトルカードをリスト化
    List<CardController> attackers =
        TCardAIUnityBridge.GetCards(
            enemyBattleArea
        );

    int playerWallCount =
        GetAlivePlayerWallCount();

    // AIが攻撃順を決定
    if(enemyAIBrain != null)
    {
        attackers =
            enemyAIBrain.OrderAttackers(
                attackers,
                playerWallCount
            );
    }

    foreach(CardController attacker in attackers)
    {
        // 攻撃途中で墓地へ送られた場合
        if(attacker == null)
            continue;

        if(attacker.gameObject == null)
            continue;

        // すでに敵場から離れている場合
        if(attacker.transform.parent != enemyBattleArea)
            continue;

        if(attacker.isTapped)
            continue;

        if(attacker.data == null)
            continue;

        if(attacker.data.effectTypes != null &&
           System.Array.Exists(
               attacker.data.effectTypes,
               x => x == EffectType.CannotAttack
           ))
        {
            Debug.Log(
                "敵の攻撃不可カードなので攻撃しない：" +
                attacker.name
            );

            continue;
        }

        if(attacker.hasSummonSickness)
        {
            Debug.Log(
                "敵カードは召喚酔い中なので攻撃不可：" +
                attacker.name
            );

            continue;
        }

        if(!ShouldEnemyAttack(attacker))
        {
            Debug.Log(
                "CPU判断：攻撃見送り：" +
                attacker.data.cardName
            );

            continue;
        }

        List<RectTransform> targets =
            new List<RectTransform>();

        for(int w = 0;
            w < playerWallArea.childCount;
            w++)
        {
            RectTransform wall =
                playerWallArea.GetChild(w)
                .GetComponent<RectTransform>();

            if(wall == null)
                continue;

            CanvasGroup cg =
                wall.GetComponent<CanvasGroup>();

            if(cg != null &&
               cg.alpha <= 0.01f)
            {
                continue;
            }

            targets.Add(wall);
        }

        // Wallがない場合は直接攻撃
        if(targets.Count == 0)
        {
            Debug.Log(
                "プレイヤーWallなし。敵が直接攻撃"
            );

            RectTransform attackerRt =
                attacker.GetComponent<RectTransform>();

            RectTransform targetRt =
                playerBattleArea != null
                ? playerBattleArea
                    .GetComponent<RectTransform>()
                : null;

            if(attackArrowManager != null &&
               attackerRt != null &&
               targetRt != null)
            {
                attackArrowManager.ShowEnemyArrow(
                    attackerRt,
                    targetRt
                );
            }

            yield return new WaitForSeconds(1.2f);

            HideAttackArrow();

            if(attacker != null)
            {
                attacker.Tap();
            }

            if(handDealer != null)
            {
                handDealer.StartDefeatSequence();
            }

            StartBattleEndResult();

            yield break;
        }

        // 現状は攻撃対象Wallをランダム選択
        RectTransform targetWall =
            targets[
                Random.Range(
                    0,
                    targets.Count
                )
            ];

        if(attackArrowManager != null)
        {
            attackArrowManager.ShowEnemyArrow(
                attacker.GetComponent<RectTransform>(),
                targetWall
            );

            yield return new WaitForSeconds(0.6f);
        }

        pendingEnemyAttacker = attacker;
        pendingEnemyTargetWall =
            targetWall.gameObject;

        enemyAttackBlocked = false;

        if(HasUntappedPlayerBlocker())
        {
            waitingBlockSelect = true;

            ShowBlockableCards();

            ShowNoBlockButton(
                attacker.GetComponent<RectTransform>()
            );

            Debug.Log(
                "ブロックするカードを選んでください"
            );

            while(waitingBlockSelect)
            {
                yield return null;
            }

            HideNoBlockButton();
            HideAttackArrow();

            if(enemyAttackBlocked)
            {
                // 戦闘解決で攻撃カードが
                // 墓地へ送られている可能性がある
                if(attacker != null &&
                   attacker.transform.parent ==
                   enemyBattleArea)
                {
                    attacker.Tap();
                }

                enemyAttackBlocked = false;

                yield return new WaitForSeconds(
                    0.4f
                );

                continue;
            }
        }
        else
        {
            Debug.Log(
                "ブロッカーなし：Wallを自動破壊"
            );

            HideNoBlockButton();
            HideAttackArrow();
        }

        if(handDealer != null &&
           pendingEnemyTargetWall != null)
        {
            yield return StartCoroutine(
                handDealer
                .DamagePlayerWallAndWait(
                    pendingEnemyTargetWall
                )
            );
        }

        // Shield Triggerなどで攻撃カードが
        // 墓地へ送られた可能性を考慮
        if(attacker != null &&
           attacker.transform.parent ==
           enemyBattleArea)
        {
            attacker.Tap();
        }

        pendingEnemyAttacker = null;
        pendingEnemyTargetWall = null;
        enemyAttackBlocked = false;
        waitingBlockSelect = false;

        yield return new WaitForSeconds(0.4f);
    }
}

bool ShouldEnemyAttack(
    CardController attacker
)
{
    if(attacker == null ||
       attacker.data == null)
    {
        return false;
    }

    int playerWallCount =
        GetAlivePlayerWallCount();

    if(enemyAIBrain != null)
    {
        List<CardController> playerCards =
            TCardAIUnityBridge.GetCards(
                playerBattleArea
            );

        List<CardController> enemyCards =
            TCardAIUnityBridge.GetCards(
                enemyBattleArea
            );

        return enemyAIBrain.ShouldAttack(
            attacker,
            playerWallCount,
            playerCards,
            enemyCards
        );
    }

    //=========================
    // AI Brainがない場合の旧処理
    //=========================

    attacker.data.SetPowerFromName();

    if(playerWallCount <= 0)
        return true;

    if(playerWallCount <= 1)
        return true;

    if(IsMonarchCard(attacker.data))
        return true;

    if(attacker.data.name.Contains("K"))
        return true;

    if(attacker.data.name.Contains("Joker"))
        return true;

    if(playerWallCount >= 4 &&
       attacker.data.power < 9)
    {
        return false;
    }

    if(attacker.data.power >= 9)
        return true;

    return false;
}

int GetAlivePlayerWallCount()
{
    if(playerWallArea == null)
        return 0;

    int count = 0;

    for(int i = 0; i < playerWallArea.childCount; i++)
    {
        CanvasGroup cg =
            playerWallArea.GetChild(i)
            .GetComponent<CanvasGroup>();

        if(cg != null && cg.alpha <= 0.01f)
            continue;

        count++;
    }

    return count;
}

bool IsMonarchCard(CardData card)
{
    if(card == null)
        return false;

    if(card.effectTypes == null)
        return false;

    return System.Array.Exists(
        card.effectTypes,
        x => x == EffectType.DoubleWallBreak
    );
}    void LayoutEnemyBattleCards()
    {
        if(enemyBattleArea == null)
            return;

        int count = enemyBattleArea.childCount;

        if(count == 0)
            return;

        float spacing = 140f;

        float startX = -spacing * (count - 1) / 2f;

        for(int i = 0; i < count; i++)
        {
            RectTransform rt =
                enemyBattleArea.GetChild(i)
                .GetComponent<RectTransform>();

            if(rt == null)
                continue;

            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(120f, 170f);
            rt.anchoredPosition =
                new Vector2(startX + spacing * i, 0f);
        }
    }

    GameObject GetRandomPlayerWall()
    {
        if(playerWallArea == null)
            return null;

        List<GameObject> wallSlots = new List<GameObject>();

        for(int i = 0; i < playerWallArea.childCount; i++)
        {
            Transform slot = playerWallArea.GetChild(i);

            CardController card =
                slot.GetComponent<CardController>();

            if(card == null)
                card = slot.GetComponentInChildren<CardController>();

            if(card != null)
            {
                wallSlots.Add(slot.gameObject);
            }
        }

        if(wallSlots.Count == 0)
            return null;

        int index = Random.Range(0, wallSlots.Count);
        return wallSlots[index];
    }

void ShowEnemyAttackArrow(GameObject attacker, GameObject target)
{
    Debug.Log("敵赤矢印表示処理");

    if(attackArrowManager == null)
    {
        attackArrowManager =
            FindFirstObjectByType<AttackArrowManager>();
    }

    if(attackArrowManager == null)
    {
        Debug.LogWarning("AttackArrowManager が見つからない");
        return;
    }

    Debug.Log("使用中の矢印 = " + attackArrowManager.name);

    attackArrowManager.SetArrowColor(Color.red);

    RectTransform from =
        attacker.GetComponent<RectTransform>();

    RectTransform to =
        target.GetComponent<RectTransform>();

    attackArrowManager.ShowArrow(from, to);
}

    void UntapEnemyBattleCards()
    {
        if(enemyBattleArea == null)
        {
            Debug.LogWarning("enemyBattleArea が未設定");
            return;
        }

        for(int i = 0; i < enemyBattleArea.childCount; i++)
        {
            CardController card =
                enemyBattleArea.GetChild(i)
                .GetComponent<CardController>();

            if(card != null)
            {
                card.Untap();
            }
        }

        Debug.Log("敵バトルカードをアンタップ");
    }

        IEnumerator ShowTurnLogo(
        GameObject logo,
        AudioClip se
    )
    {
        if(logo == null)
            yield break;

        logo.SetActive(true);

        CanvasGroup cg =
            logo.GetComponent<CanvasGroup>();

        if(cg == null)
            cg =
                logo.AddComponent<CanvasGroup>();

        cg.alpha = 0f;

        if(seSource != null &&
        se != null)
        {
            seSource.PlayOneShot(se);
        }

        float t = 0f;

        // フェードイン
        while(t < 0.3f)
        {
            t += Time.deltaTime;

            cg.alpha =
                Mathf.Lerp(
                    0f,
                    1f,
                    t/0.3f
                );

            yield return null;
        }

        yield return new WaitForSeconds(0.7f);

        t = 0f;

        // フェードアウト
        while(t < 0.3f)
        {
            t += Time.deltaTime;

            cg.alpha =
                Mathf.Lerp(
                    1f,
                    0f,
                    t/0.3f
                );

            yield return null;
        }

        logo.SetActive(false);
    }

    GameObject GetRandomPlayerBlocker()
    {
        if(playerBattleArea == null)
            return null;

        List<GameObject> blockers =
            new List<GameObject>();

        for(int i = 0; i < playerBattleArea.childCount; i++)
        {
            CardController card =
                playerBattleArea.GetChild(i)
                .GetComponent<CardController>();

            if(card == null)
                continue;

            if(card.isTapped)
                continue;

            blockers.Add(card.gameObject);
        }

        if(blockers.Count == 0)
            return null;

        return blockers[
            Random.Range(0, blockers.Count)
        ];
    }

    public void SelectBlocker(GameObject blocker)
    {
        if(!waitingBlockSelect)
            return;

        if(blocker == null)
            return;

        CardController blockerCard =
            blocker.GetComponent<CardController>();

        if(blockerCard == null)
            return;

        if(blockerCard.isTapped)
        {
            Debug.Log("タップ済みカードはブロック不可");
            return;
        }

        enemyAttackBlocked = true;
        waitingBlockSelect = false;

        HideAllCardIcons();

        StartCoroutine(
            BlockRoutine(blocker)
        );
    }
    IEnumerator BlockRoutine(GameObject blocker)
    {
        HideAttackArrow();

        if(blocker != null)
        {
            yield return StartCoroutine(
                PlayBlockerSlash(blocker)
            );

            CardController blockerCard =
                blocker.GetComponent<CardController>();

            ResolveCardBattle(
                pendingEnemyAttacker,
                blockerCard
            );

            pendingEnemyAttacker = null;
            pendingEnemyTargetWall = null;

            yield break;
        }

        // ブロッカーなしなら既存Wall破壊処理
        if(pendingEnemyTargetWall != null)
        {
            if(handDealer != null)
            {
                handDealer.DamagePlayerWall(
                    pendingEnemyTargetWall
                );
            }
        }

        pendingEnemyAttacker = null;
        pendingEnemyTargetWall = null;
    }
    public bool IsWaitingBlockSelect()
    {
        return waitingBlockSelect;
    }

    IEnumerator PlayBlockerSlash(GameObject blocker)
    {
        if(blocker == null)
            yield break;

        RectTransform blockerRt =
            blocker.GetComponent<RectTransform>();

        if(blockerSlashPrefab != null &&
        blockerRt != null)
        {
            GameObject slash =
                Instantiate(
                    blockerSlashPrefab,
                    blockerRt
                );

            RectTransform slashRt =
                slash.GetComponent<RectTransform>();

            if(slashRt != null)
            {
                slashRt.anchorMin = new Vector2(0.5f, 0.5f);
                slashRt.anchorMax = new Vector2(0.5f, 0.5f);
                slashRt.pivot = new Vector2(0.5f, 0.5f);
                slashRt.anchoredPosition = Vector2.zero;
                slashRt.localScale = Vector3.one;
            }

            Destroy(slash, 0.35f);
        }

        if(seSource != null && slashSE != null)
            seSource.PlayOneShot(slashSE);

        yield return new WaitForSeconds(0.25f);
    }

bool HasUntappedPlayerBlocker()
{
    if(playerBattleArea == null)
        return false;

    for(int i = 0; i < playerBattleArea.childCount; i++)
    {
        CardController card =
            playerBattleArea.GetChild(i)
            .GetComponent<CardController>();

        if(card == null)
            continue;

        if(card.isTapped)
            continue;

        if(card.data == null)
            continue;

        bool canBlock =
            card.data.effectTypes != null &&
            System.Array.Exists(
                card.data.effectTypes,
                x => x == EffectType.BlockOnly
            );

        if(canBlock)
            return true;
    }

    return false;
}

    void ShowNoBlockButton(RectTransform attackerRt)
    {
        if(noBlockButton == null)
            return;

        if(attackerRt == null)
            return;

        noBlockButton.SetActive(true);

        RectTransform buttonRt =
            noBlockButton.GetComponent<RectTransform>();

        if(buttonRt != null)
        {
            buttonRt.position =
                attackerRt.position;

            buttonRt.anchoredPosition +=
                noBlockButtonOffset;
        }

        // 最前面へ
        noBlockButton.transform.SetAsLastSibling();
    }

    void HideNoBlockButton()
    {
        if(noBlockButton != null)
            noBlockButton.SetActive(false);
    }
void ShowBlockableCards()
{
    if(playerBattleArea == null)
        return;

    for(int i = 0; i < playerBattleArea.childCount; i++)
    {
        CardController card =
            playerBattleArea.GetChild(i)
            .GetComponent<CardController>();

        if(card == null)
            continue;

        if(card.isTapped)
            continue;

        if(card.data == null)
            continue;

        bool canBlock =
            card.data.effectTypes != null &&
            System.Array.Exists(
                card.data.effectTypes,
                x => x == EffectType.BlockOnly
            );

        if(!canBlock)
            continue;

        CardActionIcon icon =
            card.GetComponent<CardActionIcon>();

        if(icon != null)
        {
            icon.ShowBlockIcon();
        }
    }

    Debug.Log("ブロック可能カード表示");
}

    void HideAllCardIcons()
    {
        CardController[] cards =
            FindObjectsByType<CardController>(
                FindObjectsSortMode.None
            );

        foreach(CardController card in cards)
        {
            CardActionIcon icon =
                card.GetComponent<CardActionIcon>();

            if(icon != null)
            {
                icon.HideAll();
            }
        }
    }
    public void OnNoBlockButtonClicked()
    {
        if(!waitingBlockSelect)
            return;

        Debug.Log("ブロックしない");

        enemyAttackBlocked = false;
        waitingBlockSelect = false;

        HideNoBlockButton();
    }

    void SendToGraveyard(GameObject cardObj, Transform graveyard)
    {
        Debug.Log("SendToGraveyard 実行：" + cardObj.name);
        if(cardObj == null)
            return;

        if(graveyard == null)
        {
            Destroy(cardObj);
            return;
        }

        StackedCard stacked =
            cardObj.GetComponent<StackedCard>();

        if(stacked != null &&
        stacked.baseCard != null)
        {
            GameObject baseCard =
                stacked.baseCard;

            stacked.baseCard = null;

            SendToGraveyard(
                baseCard,
                graveyard
            );
        }

        cardObj.transform.SetParent(graveyard, false);

RectTransform rt =
    cardObj.GetComponent<RectTransform>();

if(rt != null)
{
    rt.anchorMin = new Vector2(0.5f, 0.5f);
    rt.anchorMax = new Vector2(0.5f, 0.5f);
    rt.pivot = new Vector2(0.5f, 0.5f);
    rt.anchoredPosition = Vector2.zero;

    rt.sizeDelta = new Vector2(45f, 65f);
    rt.localScale = Vector3.one;
}

LayoutElement layout =
    cardObj.GetComponent<LayoutElement>();

if(layout == null)
{
    layout = cardObj.AddComponent<LayoutElement>();
}

layout.ignoreLayout = false;
layout.preferredWidth = 45f;
layout.preferredHeight = 65f;
layout.minWidth = 45f;
layout.minHeight = 65f;
layout.flexibleWidth = 0f;
layout.flexibleHeight = 0f;

       /* LayoutElement layout =
    cardObj.GetComponent<LayoutElement>();*/

        if(layout != null)
        {
            layout.ignoreLayout = false;
            layout.preferredWidth = 120f;
            layout.preferredHeight = 170f;
        }
                CardController card =
            cardObj.GetComponent<CardController>();

        if(card != null)
        {
            card.Untap();
            card.SetAttackable(false);
        }

        CanvasGroup cg =
            cardObj.GetComponent<CanvasGroup>();

        if(cg == null)
            cg = cardObj.AddComponent<CanvasGroup>();

        cg.alpha = 0.45f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        CardDrag drag =
            cardObj.GetComponent<CardDrag>();

        if(drag != null)
            drag.enabled = false;

        BattleCardClick click =
            cardObj.GetComponent<BattleCardClick>();

        if(click != null)
            click.enabled = false;

        CardActionIcon icon =
            cardObj.GetComponent<CardActionIcon>();

        if(icon != null)
            icon.HideAll();

        cardObj.transform.SetAsLastSibling();
    }

void ResolveCardBattle(CardController attacker, CardController defender)
{
    Debug.Log("ResolveCardBattle 呼ばれた");

    if(attacker == null || defender == null)
        return;

    if(attacker.data == null || defender.data == null)
    {
        Debug.LogWarning("data が null");
        return;
    }

    attacker.data.SetPowerFromName();
    defender.data.SetPowerFromName();

    int attackerPower = attacker.data.power;
    int defenderPower = defender.data.power;

    if(GameSettings.IsAdvancedRule)
    {
        int attackerBonus =
            GetSuitBattleBonus(
                attacker.data.suit,
                defender.data.suit
            );

        int defenderBonus =
            GetSuitBattleBonus(
                defender.data.suit,
                attacker.data.suit
            );

        attackerPower += attackerBonus;
        defenderPower += defenderBonus;

        Debug.Log(
            "SECRETスート補正：" +
            attacker.data.suit +
            " +" + attackerBonus +
            " / " +
            defender.data.suit +
            " +" + defenderBonus
        );
    }

    Transform attackerGrave =
        attacker.transform.IsChildOf(playerBattleArea)
        ? playerGraveyard
        : enemyGraveyard;

    Transform defenderGrave =
        defender.transform.IsChildOf(playerBattleArea)
        ? playerGraveyard
        : enemyGraveyard;

    if(HasSpecialWin(attacker, defender))
    {
        Debug.Log("攻撃側 特殊勝利");

        SendToGraveyard(
            defender.gameObject,
            defenderGrave
        );

        return;
    }

    if(HasSpecialWin(defender, attacker))
    {
        Debug.Log("防御側 特殊勝利");

        SendToGraveyard(
            attacker.gameObject,
            attackerGrave
        );

        return;
    }

    Debug.Log(
        "バトル判定: " +
        attacker.data.name +
        "(" + attackerPower + ") vs " +
        defender.data.name +
        "(" + defenderPower + ")"
    );

    if(attackerPower > defenderPower)
    {
        SendToGraveyard(
            defender.gameObject,
            defenderGrave
        );
    }
    else if(attackerPower < defenderPower)
    {
        SendToGraveyard(
            attacker.gameObject,
            attackerGrave
        );
    }
    else
    {
        SendToGraveyard(
            attacker.gameObject,
            attackerGrave
        );

        SendToGraveyard(
            defender.gameObject,
            defenderGrave
        );
    }
}

int GetSuitBattleBonus(Suit mySuit, Suit enemySuit)
{
    if(mySuit == Suit.Spade &&
       enemySuit == Suit.Heart)
        return 2;

    if(mySuit == Suit.Heart &&
       enemySuit == Suit.Club)
        return 2;

    if(mySuit == Suit.Club &&
       enemySuit == Suit.Diamond)
        return 2;

    if(mySuit == Suit.Diamond &&
       enemySuit == Suit.Spade)
        return 2;

    return 0;
}

string GetSuitFromCardName(string cardName)
{
    if(string.IsNullOrEmpty(cardName))
        return "";

    if(cardName.Contains("Spade"))
        return "Spade";

    if(cardName.Contains("Heart"))
        return "Heart";

    if(cardName.Contains("Club"))
        return "Club";

    if(cardName.Contains("Diamond"))
        return "Diamond";

    return "";
}
    CardController pendingDestroySelfCard;
    bool isSelectingDestroyTarget = false;

public void StartSelectEnemyBattleToDestroy(
    CardController selfCard
)
{
    pendingDestroySelfCard = selfCard;

    if(selfCard == null)
        return;

    Debug.Log(
        "破壊効果発動カード：" +
        GetCardName(selfCard.data)
    );

    bool isEnemyCard =
        enemyBattleArea != null &&
        selfCard.transform.IsChildOf(enemyBattleArea);

    Debug.Log("A効果 isEnemyCard = " + isEnemyCard);

    if(isEnemyCard)
    {
        CardController target =
            GetCpuDestroyTargetFromPlayerBattle();

        if(target == null)
        {
            Debug.Log("CPU A効果：破壊対象なし");

            SendCardToOwnGraveyard(selfCard);

            pendingDestroySelfCard = null;
            isSelectingDestroyTarget = false;
            return;
        }

        Debug.Log(
            "CPU A効果：破壊対象 = " +
            GetCardName(target.data)
        );

        TrySelectDestroyTarget(target);
        return;
    }

    isSelectingDestroyTarget = true;

    Debug.Log("破壊する敵カードを選択してください");
}

CardController GetCpuDestroyTargetFromPlayerBattle()
{
    if(playerBattleArea == null)
        return null;

    CardController bestTarget = null;
    int bestScore = -999;

    for(int i = 0; i < playerBattleArea.childCount; i++)
    {
        CardController card =
            playerBattleArea.GetChild(i)
            .GetComponent<CardController>();

        if(card == null || card.data == null)
            continue;

        card.data.SetPowerFromName();

        int score = card.data.power;

        string name = GetCardName(card.data);

        if(name.Contains("Joker"))
            score += 100;

        if(name.Contains("K"))
            score += 80;

        if(name.Contains("Q"))
            score += 60;

        if(name.Contains("J"))
            score += 50;

        if(score > bestScore)
        {
            bestScore = score;
            bestTarget = card;
        }
    }

    return bestTarget;
}

public bool TrySelectDestroyTarget(
    CardController target
)
{
    if(target == null)
        return false;

    Debug.Log(
        "選択破壊：" +
        GetCardName(target.data)
    );

    Debug.Log(
        "A自身：" +
        (
            pendingDestroySelfCard != null
            ? GetCardName(pendingDestroySelfCard.data)
            : "NULL"
        )
    );

    SendCardToOwnGraveyard(target);

    if(pendingDestroySelfCard != null)
    {
        if(pendingDestroySelfCard.transform.IsChildOf(enemyBattleArea))
        {
            SendToGraveyard(
                pendingDestroySelfCard.gameObject,
                enemyGraveyard
            );
        }
        else
        {
            SendToGraveyard(
                pendingDestroySelfCard.gameObject,
                playerGraveyard
            );
        }
    }

    pendingDestroySelfCard = null;
    isSelectingDestroyTarget = false;

    return true;
}

public bool IsSelectingDestroyTarget()
{
    return isSelectingDestroyTarget;
}
    public void JokerClearBattleArea(CardController summonedJoker)
    {
        if(summonedJoker == null)
            return;

        List<GameObject> targets =
            new List<GameObject>();

        if(playerBattleArea != null)
        {
            for(int i = 0; i < playerBattleArea.childCount; i++)
            {
                CardController card =
                    playerBattleArea.GetChild(i)
                    .GetComponent<CardController>();

                if(card == null)
                    continue;

                if(card == summonedJoker)
                    continue;

                targets.Add(card.gameObject);
            }
        }

        if(enemyBattleArea != null)
        {
            for(int i = 0; i < enemyBattleArea.childCount; i++)
            {
                CardController card =
                    enemyBattleArea.GetChild(i)
                    .GetComponent<CardController>();

                if(card == null)
                    continue;

                if(card == summonedJoker)
                    continue;

                targets.Add(card.gameObject);
            }
        }

        foreach(GameObject obj in targets)
        {
            if(obj == null)
                continue;

            if(obj.transform.IsChildOf(playerBattleArea))
            {
                SendToGraveyard(obj, playerGraveyard);
            }
            else if(obj.transform.IsChildOf(enemyBattleArea))
            {
                SendToGraveyard(obj, enemyGraveyard);
            }
        }

        Debug.Log(
            "JOKER効果：バトルエリア全体墓地送り " +
            targets.Count +
            "枚"
        );
    }

    bool HasSpecialWin(
        CardController attacker,
        CardController target
    )
    {
        if(attacker == null ||
        attacker.data == null ||
        target == null ||
        target.data == null)
        {
            return false;
        }

        if(attacker.data.effectTypes == null)
            return false;

        string targetName =
            target.data.name;

        foreach(EffectType effect in attacker.data.effectTypes)
        {
            // Jokerに勝利
            if(effect == EffectType.BreakableJoker)
            {
                if(targetName.Contains("JOKER"))
                    return true;
            }

            // J,Q,K,JOKERに勝利
            if(effect == EffectType.BreakableFace)
            {
                if(targetName.Contains("JOKER") ||
                targetName.Contains("J") ||
                targetName.Contains("Q") ||
                targetName.Contains("K"))
                {
                    return true;
                }
            }

            // Jに勝利
            if(effect == EffectType.BreakableJack)
            {   //0529修正
                if(targetName.Contains("Jack"))
                    return true;
            }
        }

        return false;
    }

    public void SendCardToOwnGraveyard(CardController card)
    {
        if(card == null)
            return;

        if(card.transform.IsChildOf(playerBattleArea))
        {
            SendToGraveyard(
                card.gameObject,
                playerGraveyard
            );
        }
        else if(card.transform.IsChildOf(enemyBattleArea))
        {
            SendToGraveyard(
                card.gameObject,
                enemyGraveyard
            );
        }
    }

    public void TapAllEnemyBattle(CardController card)
    {
        Transform targetArea =
            card.transform.IsChildOf(playerBattleArea)
            ? enemyBattleArea
            : playerBattleArea;

        for(int i = 0; i < targetArea.childCount; i++)
        {
            CardController target =
                targetArea.GetChild(i)
                .GetComponent<CardController>();

            if(target != null)
            {
                target.Tap();
            }
        }
    }

    public void DestroyRandomEnemyBattle(CardController card)
    {
        Transform targetArea =
            card.transform.IsChildOf(playerBattleArea)
            ? enemyBattleArea
            : playerBattleArea;

        List<CardController> targets =
            new List<CardController>();

        for(int i = 0; i < targetArea.childCount; i++)
        {
            CardController target =
                targetArea.GetChild(i)
                .GetComponent<CardController>();

            if(target != null)
            {
                targets.Add(target);
            }
        }

        if(targets.Count == 0)
            return;

        CardController selected =
            targets[
                Random.Range(
                    0,
                    targets.Count
                )
            ];

        SendCardToOwnGraveyard(selected);
    }

    void ClearPlayerSummonSickness()
    {
        foreach(Transform child in playerBattleArea)
        {
            CardController card =
                child.GetComponent<CardController>();

            if(card == null)
                continue;

            card.SetSummonSickness(false);
        }
    }

    void ClearEnemySummonSickness()
    {
        if(enemyBattleArea == null)
            return;

        for(int i = 0; i < enemyBattleArea.childCount; i++)
        {
            CardController card =
                enemyBattleArea.GetChild(i)
                .GetComponent<CardController>();

            if(card == null)
                continue;

            card.SetSummonSickness(false);
        }

        Debug.Log("敵召喚酔い解除");
    }

    public void StartBattleEndResult()
    {
        //Debug.LogError("★★ StartBattleEndResult 呼ばれた ★★");

        StartCoroutine(
            BattleEndResultRoutine()
        );
    }

    void SetEndTurnButton(bool value)
    {
        if(endTurnButton == null)
            return;

        endTurnButton.gameObject.SetActive(true);
        endTurnButton.interactable = value;
    }

IEnumerator BattleEndResultRoutine()
{
    Debug.Log("BattleEndResultRoutine 開始");

    yield return new WaitForSeconds(
        resultWaitTime
    );

    if(playerBattleArea != null)
        ClearArea(playerBattleArea);

    if(enemyBattleArea != null)
        ClearArea(enemyBattleArea);

    if(playerWallArea != null)
        ClearArea(playerWallArea);

    if(handDealer != null &&
    handDealer.enemyWallArea != null)
    {
        ClearArea(
            handDealer.enemyWallArea
        );
    }

    HideAttackArrow();
    HideNoBlockButton();
    HideAllCardIcons();

    if(playerTurnLogo != null)
        playerTurnLogo.SetActive(false);

    if(enemyTurnLogo != null)
        enemyTurnLogo.SetActive(false);

    if(gameStartLogo != null)
        gameStartLogo.SetActive(false);

    Debug.Log("UI表示直前");

    if(resultPanel != null)
    {
        resultPanel.SetActive(true);

        CanvasGroup cg =
            resultPanel.GetComponent<CanvasGroup>();

        if(cg != null)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        resultPanel.transform.SetAsLastSibling();
    }

    if(retryButton != null)
        retryButton.SetActive(true);

    if(exitButton != null)
        exitButton.SetActive(true);

    Debug.Log("戦闘終了UI表示");
}

void ClearArea(Transform area)
{
    if(area == null)
        return;

    for(
        int i=area.childCount-1;
        i>=0;
        i--
    )
    {
        Destroy(
            area.GetChild(i).gameObject
        );
    }
}

public void RequestPlayerExtraTurn()
{
    playerExtraTurnRequested = true;

    Debug.Log("Joker効果：エクストラターン予約");
}

public void ShowJokerEffectSelectPanel(CardController jokerCard)
{
    if(jokerCard == null)
        return;

    pendingJokerEffectCard = jokerCard;

    if(jokerEffectPanel != null)
        jokerEffectPanel.SetActive(true);

    if(jokerClearButton != null)
    {
        jokerClearButton.onClick.RemoveAllListeners();
        jokerClearButton.onClick.AddListener(OnSelectJokerClear);
    }

    if(jokerExtraTurnButton != null)
    {
        jokerExtraTurnButton.onClick.RemoveAllListeners();
        jokerExtraTurnButton.onClick.AddListener(OnSelectJokerExtraTurn);
    }

    Debug.Log("JOKER効果選択パネル表示");
}

void OnSelectJokerClear()
{
    if(jokerEffectPanel != null)
        jokerEffectPanel.SetActive(false);

    if(pendingJokerEffectCard != null)
    {
        Debug.Log("JOKER効果選択：バトルエリア一掃");
        JokerClearBattleArea(pendingJokerEffectCard);
    }

    pendingJokerEffectCard = null;
}

    public void OnSelectJokerExtraTurn()
    {
        if(jokerEffectPanel != null)
            jokerEffectPanel.SetActive(false);

        RequestPlayerExtraTurn();

        if(pendingJokerEffectCard != null)
        {
            Debug.Log("JOKER追加ターン選択：JOKERを墓地へ");
            SendCardToOwnGraveyard(pendingJokerEffectCard);
        }

        pendingJokerEffectCard = null;
    }

    public void RequestEnemyExtraTurn()
    {
        Debug.Log("敵エクストラターン予約");
        enemyExtraTurnRequested = true;
    }

    bool ShouldAutoEndEarlyTurn()
    {
        // 最初の5ターンだけ
        if (turnCount > 5)
            return false;

        // 召喚できるカードがあるなら終了しない
        if (CanPlayerSummonAnything())
            return false;

        // 攻撃できるカードがあるなら終了しない
        if (CanPlayerAttackAnything())
            return false;

        return true;
    }

    bool CanPlayerSummonAnything()
    {
        ResourceManager rm =
            FindFirstObjectByType<ResourceManager>();

        if(rm == null)
            return false;

        if(handDealer == null)
            return false;

        if(handDealer.handArea == null)
            return false;

        for(int i = 0; i < handDealer.handArea.childCount; i++)
        {
            CardController card =
                handDealer.handArea
                .GetChild(i)
                .GetComponent<CardController>();

            if(card == null || card.data == null)
                continue;

            card.data.SetCostFromName();

            if(card.data.cost <= rm.currentResource)
                return true;
        }

        return false;
    }

    bool CanPlayerAttackAnything()
    {
        if(playerBattleArea == null)
            return false;

        for(int i = 0; i < playerBattleArea.childCount; i++)
        {
            CardController card =
                playerBattleArea
                .GetChild(i)
                .GetComponent<CardController>();

            if(card == null)
                continue;

            if(card.isTapped)
                continue;

            if(card.hasSummonSickness)
                continue;

            if(card.data != null &&
            card.data.effectTypes != null &&
            System.Array.Exists(
                card.data.effectTypes,
                x => x == EffectType.CannotAttack))
            {
                continue;
            }

            return true;
        }

        return false;
    }
}