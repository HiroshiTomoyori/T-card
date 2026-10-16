using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public partial class TurnManager : MonoBehaviour
{
    public int PlayerWallCountForAI => GetAlivePlayerWallCount();
    public int EnemyWallCountForAI => GetAliveEnemyWallCount();
    [Header("Ace spell presentation")]
    public AudioClip aceCastSE;
    public AudioClip aceWaitingSE;
    [Range(0f, 1f)] public float aceSEVolume = 0.45f;
    [Min(0f)] public float enemyAceSelectionDuration = 1.2f;
    AceSelectionEffect aceSelectionEffect;
    int turnCount = 1;

    int wallBreakCount = 0;
    int maxWallBreakCount = 1;
    bool isPlayerFirst = true;
    bool isBattlePhase = false;
    bool enemyBattleIconsActive;
    bool battleIconsEnded;

    bool enemyExtraTurnRequested = false;
private bool isPlayerMainPhase = false;

public bool CanDragPlayerHand
{
    get
    {
        bool isResource =
            resourcePhaseManager != null &&
            resourcePhaseManager.IsRunning();

        return !IsPlayerDecisionOpen && (CardEffectManager.I == null || !CardEffectManager.I.IsResolvingNine) && (handDealer == null || (!handDealer.IsTurnDrawAnimating && !handDealer.IsWallPlacementRunning && !handDealer.IsResolvingWallAttack)) && isPlayerTurnActive &&
               !isEndingTurn &&
               !isBattlePhase &&
               !HandDealer.IsRedrawSelecting &&
               !IsSelectingTarget() &&
               !IsSelectingDestroyTarget() &&
               (isResource || isPlayerMainPhase);
    }
}

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
    [Tooltip("ボタンの背景などを含む表示ルート。未設定なら endTurnButton 本体を使用")]
    public GameObject endTurnButtonRoot;

    [Header("Debug Mode")]
    public bool debugForceEnemyQ = false;

    [Header("Graveyard")]
    public Transform playerGraveyard;
    public Transform enemyGraveyard;

    [Header("Graveyard Card Size")]
    [Tooltip("墓地に置かれるカードの固定サイズ（ピクセル）")]
    [SerializeField]
    Vector2 graveyardCardSize = new Vector2(65f, 94f);

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
    public bool IsBattleEnded { get; private set; }
    bool battleFrozen;
    float timeScaleBeforeResult = 1f;
    readonly List<Tween> frozenTweens = new List<Tween>();
    readonly List<Animator> frozenAnimators = new List<Animator>();
    readonly List<ParticleSystem> frozenParticles = new List<ParticleSystem>();

    // プレイヤーターンの唯一の状態フラグ。
    // ボタン表示・入力可否・二重ターン開始の防止に共通利用する。
    bool isPlayerTurnActive = false;
    bool isFirstTurnStarting = false;

    bool playerExtraTurnRequested = false;

    List<GameObject> pendingWallTargets =
    new List<GameObject>();

    [Header("Joker Effect Select")]
    public GameObject jokerEffectPanel;
    public Button jokerClearButton;
    public Button jokerExtraTurnButton;

    CardController pendingJokerEffectCard;
    //bool playerExtraTurnRequested = false;

    public float resultWaitTime = 3f;

    void Awake()
    {
        // シーン上でボタンが有効になっていても、マッチ開始前は必ず隠す。
        SetEndTurnButton(false);

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

    void LateUpdate()
    {
        PositionPlayerDecisionPanel();
        TickDecisionTimer();
        // Animatorや別UI処理に再表示されても、自ターン以外では必ず隠す。
        SyncEndTurnButtonVisibility();

        // Inspectorでサイズを変更した場合や、別のUI処理に上書きされた場合も
        // 墓地内のカードへ毎フレーム固定サイズを再適用する。
        SyncGraveyardCardSizes();
        SyncBattleActionIcons();
    }

    public void StartFirstTurn(bool playerFirst)
    {
        // StartFirstTurn が複数回呼ばれても、進行中のターンを重ねない。
        if(isFirstTurnStarting || isPlayerTurnActive || isEndingTurn)
        {
            Debug.LogWarning("ターン進行中のため、先攻開始要求を無視しました");
            return;
        }

        battleIconsEnded = false;
        isPlayerFirst = playerFirst;
        turnCount = 1;
        isFirstTurnStarting = true;

        SetEndTurnButton(false);

        StartCoroutine(FirstTurnRoutine());
    }

    bool resolvingEnemyBlock = false;
    bool waitingBlockSelect = false;
    bool enemyAttackBlocked = false;
    CardController pendingEnemyAttacker;
    GameObject pendingEnemyTargetWall;
    CardController pendingEnemyRestedTarget;
    bool resolvingPlayerCardAttack;

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

        isFirstTurnStarting = false;
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

        isFirstTurnStarting = false;
        StartPlayerTurn();

        yield break;
    }
}

    void StartDrawPhase()
    {
        isPlayerMainPhase = false;
        Debug.Log("Draw Phase");

        if(handDealer != null)
        {
            handDealer.DrawOneCard();
        }
    }

    void StartResourcePhase()
    {
        isPlayerMainPhase = false;
        Debug.Log("Resource Phase");

        if(resourcePhaseManager != null)
        {
            resourcePhaseManager.StartResourcePhase();
        }
    }

    void StartMainPhase()
    {
        isPlayerMainPhase = true;
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

    void StartPlayerBattlePhase()
    {
        if (IsBattleEnded || !isPlayerTurnActive || isEndingTurn || isBattlePhase) return;
        if (mainPhaseManager != null) mainPhaseManager.EndMainPhase();
        isPlayerMainPhase = false;
        isBattlePhase = true;
        if (battlePhaseManager != null) battlePhaseManager.StartBattlePhase();
        LockPlayerHand(true);
        ShowAttackableCards();
    }

public void StartBattleByAttackSelect(GameObject attacker)
{
    if (IsBattleEnded || IsPlayerDecisionOpen || !isPlayerTurnActive || isEndingTurn || resolvingPlayerCardAttack) return;
    if (handDealer != null && (handDealer.IsWallPlacementRunning || handDealer.IsResolvingWallAttack)) return;
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
        StartPlayerBattlePhase();
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

    if(target == null || pendingWallTargets.Contains(target))
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

    if (RequestPlayerDecision("attack-wall:" + target.GetInstanceID(),
        GetCardName(attackerCard.data) + "でウォールを攻撃します。\nよろしいですか？", () => SelectAttackTarget(target))) return;

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
    if(wallBreakCount < Mathf.Min(maxWallBreakCount, GetAliveEnemyWallCount()))
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
    CardController attacker,
    CardController protectedTarget = null
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

            if(card.isTapped || !card.gameObject.activeInHierarchy)
                continue;

            if(!IsGuardCard(card.data))
                continue;

            blockers.Add(card);
        }
    }

    if (protectedTarget != null)
    {
        // Protect a rested ally when blocking wins, trades, or saves a more valuable card.
        if (CompareBattleCards(attacker, protectedTarget) < 0) return null;
        CardController guard = null;
        int bestProtection = int.MinValue;
        foreach (CardController candidate in blockers)
        {
            int outcome = CompareBattleCards(attacker, candidate);
            candidate.data.SetPowerFromName();
            protectedTarget.data.SetPowerFromName();
            int savedValue = protectedTarget.data.power - candidate.data.power;
            if (outcome > 0 && savedValue <= 0) continue;
            int score = (outcome < 0 ? 10000 : outcome == 0 ? 5000 : 0) + savedValue;
            if (score > bestProtection) { bestProtection = score; guard = candidate; }
        }
        return guard;
    }

    if(enemyAIBrain != null)
    {
        return enemyAIBrain
            .SelectBestBlocker(
                attacker,
                blockers,
                enemyWallCount,
                TCardAIUnityBridge.GetCards(playerBattleArea),
                TCardAIUnityBridge.GetCards(enemyBattleArea),
                GetAlivePlayerWallCount()
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
        if (handDealer.enemyWallArea.GetChild(i).GetComponentInChildren<CardController>() == null) continue;
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
        if (IsBattleEnded || !isPlayerTurnActive || resolvingPlayerCardAttack) return;
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

        if (enemyBattleArea == null || !targetCard.transform.IsChildOf(enemyBattleArea) ||
            playerBattleArea == null || !attackerCard.transform.IsChildOf(playerBattleArea) || attackerCard.isTapped)
            return;

        if (RequestPlayerDecision("attack-card:" + target.GetInstanceID(),
            GetCardName(attackerCard.data) + "で" + GetCardName(targetCard.data) + "を攻撃します。\nよろしいですか？",
            () => SelectEnemyBattleCardTarget(target))) return;

        selectingTarget = false;
        pendingWallTargets.Clear();
        wallBreakCount = 0;
        maxWallBreakCount = 1;
        SetEnemyWallClickable(false);

        StartCoroutine(
            BattleCardVsCardRoutine(
                attackerCard,
                targetCard
            )
        );

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

    IEnumerator BattleCardVsCardRoutine(CardController attackerCard, CardController targetCard)
    {
        if (attackerCard == null || targetCard == null) yield break;
        resolvingPlayerCardAttack = true;
        attackerCard.Tap();
        attackerCard.SetAttackable(false);
        try
        {
            CardController blocker = SelectEnemyBlocker(attackerCard, targetCard);
            CardController defender = blocker != null ? blocker : targetCard;
            if (attackArrowManager != null)
                attackArrowManager.ShowPlayerArrow(attackerCard.GetComponent<RectTransform>(), defender.GetComponent<RectTransform>());
            yield return PlayBlockerSlash(defender.gameObject);
            if (!IsBattleEnded && attackerCard != null && defender != null)
                ResolveCardBattle(attackerCard, defender);
        }
        finally
        {
            resolvingPlayerCardAttack = false;
            currentAttacker = null;
            HideAttackArrow();
            if (!IsBattleEnded) ShowAttackableCards();
        }
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
            RunAutomaticDecision(EndPlayerTurn);
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

    if(target == null || pendingWallTargets.Contains(target))
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
        if (resolvingPlayerCardAttack) return;
        // マッチ開始前・敵ターン中・二重クリックからの呼び出しを拒否する。
        if(!isPlayerTurnActive || isEndingTurn || (CardEffectManager.I != null && CardEffectManager.I.IsResolvingNine) || (handDealer != null && (handDealer.IsWallPlacementRunning || handDealer.IsResolvingWallAttack)))
        {
            Debug.Log("自分のターンではないためターン終了不可");
            return;
        }
    
    if(HandDealer.IsRedrawSelecting)
    {
        Debug.Log("引き直し選択中のためターン終了不可");
        return;
    }


    if (RequestPlayerDecision("end-turn", "ターンを終了します。\nよろしいですか？", EndPlayerTurn)) return;

    ResourcePhaseManager resourcePhase =
    FindFirstObjectByType<ResourcePhaseManager>();

    if(resourcePhase != null)
    {
        resourcePhase.EndResourcePhase();
    }
        // コルーチン開始前に無効化し、同一フレームの連打も遮断する。
        isPlayerTurnActive = false;
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

        ClearAttackableCards();

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
    if (IsBattleEnded) return;
    // 同じターン開始イベントが重複しても、ドロー等を二重実行しない。
    if(isPlayerTurnActive)
    {
        Debug.LogWarning("自ターンは既に開始済みです。重複開始を無視しました");
        return;
    }

    isPlayerTurnActive = true;

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

    StartCoroutine(StartPlayerTurnDrawRoutine());
}

IEnumerator StartPlayerTurnDrawRoutine()
{
    SetEndTurnButton(false);
    isPlayerMainPhase = false;
    if (!(isPlayerFirst && turnCount == 1) && handDealer != null)
        yield return handDealer.DrawTurnStartCard();
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

        if (RequestPlayerDecision("direct-attack", "相手に直接攻撃します。\nよろしいですか？",
            () => SelectDirectAttackTarget(target))) return;

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

        // バトル中は操作だけをロックし、手札の見た目は透過させない。
        // 以前は locked 時に 0.45f にしていたため、ターン終了まで
        // 手札が透けた状態になっていた。
        cg.alpha = 1f;
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

        enemyAIBrain.KingRecoveriesAvailable = handDealer.EnemyKingRecoveryRemaining;
        CardData aiSelectedCard =
            enemyAIBrain.SelectResourceCard(
                handDealer.enemyHandCards,
                enemyResourceManager != null
                    ? enemyResourceManager.maxResource
                    : 0,
                enemyWallCount,
                playerFieldCount,
                TCardAIUnityBridge.GetCards(playerBattleArea),
                TCardAIUnityBridge.GetCards(enemyBattleArea),
                GetAlivePlayerWallCount()
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
        Debug.LogWarning("EnemyResourceManager が未設定");
        yield break;
    }

    if(enemyBattleArea == null)
    {
        Debug.LogWarning("enemyBattleArea が未設定");
        yield break;
    }

    if(cardPrefab == null)
    {
        Debug.LogWarning("cardPrefab が未設定");
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
            Debug.Log("敵手札なし：メインフェイズ終了");
            yield break;
        }

        List<CardData> summonableCards =
            new List<CardData>();

        // =========================
        // 召喚可能カードを取得
        // =========================

        foreach(CardData card in handDealer.enemyHandCards)
        {
            if(card == null)
                continue;

            // A and Nine can be cast even when all six creature slots are occupied.
            CardController availableKingBase = enemyAIBrain != null
                ? enemyAIBrain.SelectKingBase(card, TCardAIUnityBridge.GetCards(enemyBattleArea), enemyResourceManager.currentResource) : null;
            if(!BattleAreaLayout.IsSpell(card) && !BattleAreaLayout.CanAccept(enemyBattleArea, availableKingBase))
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

            // デバッグONならコストを無視
            if(debugForceEnemyQ ||
               (availableKingBase != null || card.cost <= enemyResourceManager.currentResource))
            {
                summonableCards.Add(card);
            }
        }

        if(summonableCards.Count == 0)
        {
            Debug.Log("召喚可能カードなし：敵メイン終了");
            yield break;
        }

        // =========================
        // 召喚カードを選択
        // =========================

        CardData selectedCard = null;

        if(debugForceEnemyQ)
        {
            // デバッグ時はQを優先して選択
            selectedCard = summonableCards.Find(
                card => GetCardName(card).Contains("Q")
            );

            if(selectedCard == null)
            {
                Debug.LogWarning(
                    "DEBUG：召喚可能なQが敵手札にありません"
                );

                yield break;
            }

            Debug.Log(
                "DEBUG：コスト無視でQを召喚：" +
                GetCardName(selectedCard)
            );
        }
        else
        {
            // 通常時は既存AIに選択させる
            selectedCard =
                SelectEnemySummonCard(handDealer.enemyHandCards);
        }

        if(selectedCard == null)
        {
            Debug.Log("AI判断：これ以上召喚しない");
            yield break;
        }

        // =========================
        // リソース消費
        // =========================

        CardController kingBase = !debugForceEnemyQ && enemyAIBrain != null ? enemyAIBrain.PlannedKingBase : null;
        if(kingBase != null && kingBase.transform.parent != enemyBattleArea) kingBase = null;
        int summonCost = selectedCard.cost;
        if(kingBase != null)
        {
            kingBase.data.SetPowerFromName();
            summonCost = Mathf.Max(0, selectedCard.cost - kingBase.data.power);
        }
        if(!debugForceEnemyQ)
        {
            bool usedResource =
                enemyResourceManager.UseResource(
                    summonCost
                );

            if(!usedResource)
            {
                Debug.LogWarning(
                    "敵リソース不足：" +
                    GetCardName(selectedCard)
                );

                yield break;
            }
        }
        else
        {
            Debug.Log(
                "DEBUG：リソース消費をスキップ"
            );
        }

        // =========================
        // 敵手札から削除
        // =========================

        handDealer.enemyHandCards.Remove(selectedCard);

        handDealer.enemyHandCount--;

        // =========================
        // カードを場に召喚
        // =========================

        GameObject obj =
            Instantiate(
                cardPrefab,
                enemyBattleArea
            );

        CardController controller =
            obj.GetComponent<CardController>();

        if(controller != null)
        {
            controller.SetData(selectedCard);
            if(kingBase != null) StackEnemyKingBase(controller, kingBase);

            bool noSummonSickness =
                controller.data.effectTypes != null &&
                System.Array.Exists(
                    controller.data.effectTypes,
                    x => x == EffectType.NoSummonSickness
                );

            noSummonSickness = noSummonSickness || kingBase != null;
            controller.SetSummonSickness(!noSummonSickness);

            if(!noSummonSickness)
            {
                controller.SetAttackable(false);
            }

            // 召喚時効果
            if(CardEffectManager.I != null)
            {
                CardEffectManager.I.ActivateOnSummon(
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

        // =========================
        // 敵カードのクリック処理
        // =========================

        if(obj.GetComponent<EnemyBattleCardTargetClick>() == null)
        {
            obj.AddComponent<EnemyBattleCardTargetClick>();
        }

        // =========================
        // カードサイズ・配置
        // =========================

        RectTransform rt =
            obj.GetComponent<RectTransform>();

        if(rt != null)
        {
            rt.localScale = Vector3.one * 0.7f;

            rt.sizeDelta = new Vector2(160f, 230f);

            rt.anchoredPosition = Vector2.zero;
        }

        // =========================
        // UI更新
        // =========================

        handDealer.UpdateEnemyHandCountText();

        if(enemyBattleLayout != null)
        {
            enemyBattleLayout.Refresh();
        }

        while(IsSelectingDestroyTarget() || (CardEffectManager.I != null && CardEffectManager.I.IsResolvingNine)) yield return null;

        yield return new WaitUntil(() => handDealer == null || !handDealer.IsWallPlacementRunning);
        if(!BattleAreaLayout.IsSpell(selectedCard)) summonCount++;

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
         * A・9・Jokerなどの効果で
         * 手札・リソースが変化する可能性があるため、
         * 次のループで候補を再取得する。
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

        enemyAIBrain.KingRecoveriesAvailable = handDealer.EnemyKingRecoveryRemaining;
        CardData aiSelectedCard =
            enemyAIBrain.SelectSummonCard(
                summonableCards,
                enemyWallCount,
                playerWallCount,
                playerCards,
                enemyCards,
                enemyResourceManager.currentResource,
                enemyBattleArea.GetComponent<BattleAreaLayout>() != null
                    ? Mathf.Clamp(enemyBattleArea.GetComponent<BattleAreaLayout>().maxCards, 1, 6) : 6
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

    if(enemyAIBrain != null) enemyAIBrain.ConfigureBattleReserve(
        handDealer != null ? handDealer.enemyHandCards : null,
        enemyResourceManager != null ? enemyResourceManager.maxResource : 0);
    enemyBattleIconsActive = true;
    SyncBattleActionIcons();
    try
    {
    // 現在の敵バトルカードをリスト化
    List<CardController> attackers =
        TCardAIUnityBridge.GetCards(
            enemyBattleArea
        );

    int playerWallCount =
        GetAlivePlayerWallCount();

    while(attackers.Count > 0)
    {
        // Replan after each battle and shield trigger using the current visible board.
        attackers.RemoveAll(card => card == null || card.transform.parent != enemyBattleArea);
        if(enemyAIBrain != null)
            attackers = enemyAIBrain.OrderAttackers(attackers, GetAlivePlayerWallCount(),
                TCardAIUnityBridge.GetCards(playerBattleArea), GetAliveEnemyWallCount(),
                TCardAIUnityBridge.GetCards(enemyBattleArea));
        if(attackers.Count == 0) break;
        CardController attacker = attackers[0];
        attackers.RemoveAt(0);
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

        CardController restedTarget = GetEnemyRestedAttackTarget(attacker);
        if (restedTarget != null)
        {
            yield return EnemyAttackRestedCardRoutine(attacker, restedTarget);
            if (IsBattleEnded) yield break;
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

            if(wall == null || wall.GetComponentInChildren<CardController>() == null)
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

            StartBattleEndResult(true);

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

            while(waitingBlockSelect || resolvingEnemyBlock)
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

        if (handDealer != null && pendingEnemyTargetWall != null)
        {
            var wallTargets = new List<GameObject> { pendingEnemyTargetWall };
            if (IsMonarchCard(attacker.data))
                foreach (var candidate in targets)
                    if (candidate != null && candidate.gameObject != pendingEnemyTargetWall)
                    { wallTargets.Add(candidate.gameObject); break; }
            attacker.Tap();
            yield return handDealer.DamagePlayerWallsAndWait(wallTargets);
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
    finally
    {
        enemyBattleIconsActive = false;
        SyncBattleActionIcons();
    }
}

bool ShouldEnemyAttack(
    CardController attacker
)
{
    // DEBUG：召喚したQは必ず攻撃する
    if(debugForceEnemyQ &&
    attacker != null &&
    attacker.data != null &&
    GetCardName(attacker.data).Contains("Q"))
    {
        Debug.Log("DEBUG：Qの攻撃を強制実行");
        return true;
    }

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

        enemyAIBrain.DefensiveWallCount = GetAliveEnemyWallCount();
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
        if (playerWallArea.GetChild(i).GetComponentInChildren<CardController>() == null) continue;
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

        if (playerBattleArea == null || !blockerCard.transform.IsChildOf(playerBattleArea) ||
            !CardActionIcon.CanBlock(blockerCard)) return;

        if(blockerCard.isTapped)
        {
            Debug.Log("タップ済みカードはブロック不可");
            return;
        }

        if (RequestPlayerDecision("block:" + blocker.GetInstanceID(),
            GetCardName(blockerCard.data) + "でブロックします。\nよろしいですか？", () => SelectBlocker(blocker))) return;

        enemyAttackBlocked = true;
        waitingBlockSelect = false;

        HideAllCardIcons();

        resolvingEnemyBlock = true;
        StartCoroutine(
            BlockRoutine(blocker)
        );
    }
    IEnumerator BlockRoutine(GameObject blocker)
    {
        try
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
            pendingEnemyRestedTarget = null;

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
        pendingEnemyRestedTarget = null;
            }
        finally { resolvingEnemyBlock = false; }
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

        if (RequestPlayerDecision("no-block", "ブロックせずに攻撃を受けます。\nよろしいですか？", OnNoBlockButtonClicked)) return;

        Debug.Log("ブロックしない");

        enemyAttackBlocked = false;
        waitingBlockSelect = false;

        HideNoBlockButton();
    }

    void SendToGraveyard(GameObject cardObj, Transform graveyard)
    {
        if(cardObj == null)
            return;

        Debug.Log("SendToGraveyard 実行：" + cardObj.name);

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

        // Cancel both field movement and CCP's child hover/scale animations.
        foreach(Transform visual in cardObj.GetComponentsInChildren<Transform>(true))
            visual.DOKill();
        CCP.Card proCard = cardObj.GetComponent<CCP.Card>();
        if(proCard != null)
        {
            proCard.enabled = false;
            if(proCard.parentCardGroup != null) proCard.parentCardGroup.RemoveCard(proCard);
            proCard.isBeingMovedManually = true;
        }
        CardDrag graveDrag = cardObj.GetComponent<CardDrag>();
        if(graveDrag != null) graveDrag.enabled = false;

        cardObj.transform.SetParent(graveyard, false);
        cardObj.transform.SetAsLastSibling();

        RectTransform rt =
            cardObj.GetComponent<RectTransform>();

        if(rt != null)
        {
            FitGraveyardCard(rt, graveyard);
        }

        LayoutElement layout =
            cardObj.GetComponent<LayoutElement>();

        if(layout == null)
            layout = cardObj.AddComponent<LayoutElement>();

        // 墓地ではカードを同じ位置に重ね、親のLayout Groupによる
        // サイズ上書きを受けないようにする。
        layout.ignoreLayout = true;

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

    }

    void FitGraveyardCard(RectTransform cardRect, Transform graveyard)
    {
        RectTransform background = graveyard.Find("GraveBackground") as RectTransform;
        Vector2 size = new Vector2(Mathf.Max(1f, graveyardCardSize.x), Mathf.Max(1f, graveyardCardSize.y));
        if(background != null)
        {
            background.localRotation = Quaternion.identity;
            background.localScale = Vector3.one;
            background.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + 10f);
            background.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y + 16f);
        }
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.localRotation = Quaternion.identity;
        cardRect.localScale = Vector3.one;
        cardRect.sizeDelta = size;
        cardRect.anchoredPosition3D = Vector3.zero;
        if(background != null) cardRect.position = background.TransformPoint(background.rect.center);

        CardController card = cardRect.GetComponent<CardController>();
        if(card == null || card.artworkImage == null) return;
        // Normalize the complete image path, including CCP's scaled visual container.
        // The image then occupies exactly the shared graveyard size for either prefab.
        card.artworkImage.preserveAspect = false;
        Transform visual = card.artworkImage.transform;
        while(visual != null)
        {
            AspectRatioFitter aspect = visual.GetComponent<AspectRatioFitter>();
            if(aspect != null) aspect.enabled = false;
            ContentSizeFitter fitter = visual.GetComponent<ContentSizeFitter>();
            if(fitter != null) fitter.enabled = false;
            LayoutGroup layoutGroup = visual.GetComponent<LayoutGroup>();
            if(layoutGroup != null) layoutGroup.enabled = false;
            if(visual == cardRect) break;
            RectTransform imageRect = visual as RectTransform;
            if(imageRect != null)
            {
                imageRect.anchorMin = Vector2.zero;
                imageRect.anchorMax = Vector2.one;
                imageRect.pivot = new Vector2(0.5f, 0.5f);
                imageRect.offsetMin = imageRect.offsetMax = Vector2.zero;
                imageRect.localRotation = Quaternion.identity;
                imageRect.localScale = Vector3.one;
            }
            visual = visual.parent;
        }
    }

    void SyncGraveyardCardSizes()
    {
        SyncGraveyardCardSizes(playerGraveyard);
        SyncGraveyardCardSizes(enemyGraveyard);
    }

    void SyncGraveyardCardSizes(Transform graveyard)
    {
        if(graveyard == null) return;
        for(int i = 0; i < graveyard.childCount; i++)
        {
            Transform child = graveyard.GetChild(i);
            if(child.GetComponent<CardController>() == null) continue;
            RectTransform rt = child as RectTransform;
            if(rt == null) continue;
            LayoutElement layout = child.GetComponent<LayoutElement>();
            if(layout != null) layout.ignoreLayout = true;
            FitGraveyardCard(rt, graveyard);
        }
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
    bool resolvingEnemyAce;

public void StartSelectEnemyBattleToDestroy(
    CardController selfCard
)
{
    if(selfCard == null || selfCard.data == null)
        return;

    // 破壊効果を持たないカードから選択モードを開始させない。
    bool hasDestroyEffect =
        selfCard.data.effectTypes != null &&
        System.Array.Exists(
            selfCard.data.effectTypes,
            x => x == EffectType.DestroyOneEnemyBattle
        );

    if(!hasDestroyEffect)
    {
        Debug.LogWarning("選択破壊効果を持たないカードからの開始要求を拒否");
        return;
    }

    pendingDestroySelfCard = selfCard;
    isSelectingDestroyTarget = true;

    Debug.Log(
        "破壊効果発動カード：" +
        GetCardName(selfCard.data)
    );

    bool isEnemyCard =
        enemyBattleArea != null &&
        selfCard.transform.IsChildOf(enemyBattleArea);

    Debug.Log("A効果 isEnemyCard = " + isEnemyCard);

    if(aceSelectionEffect != null) Destroy(aceSelectionEffect.gameObject);
    Canvas spellCanvas = selfCard.GetComponentInParent<Canvas>();
    if(spellCanvas != null)
    {
        GameObject fx = new GameObject("Ace Selection Effect", typeof(RectTransform));
        fx.layer = spellCanvas.rootCanvas.gameObject.layer;
        fx.transform.SetParent(spellCanvas.rootCanvas.transform, false);
        aceSelectionEffect = fx.AddComponent<AceSelectionEffect>();
        aceSelectionEffect.Initialize(this, isEnemyCard ? playerBattleArea : enemyBattleArea,
            isEnemyCard, aceCastSE, aceWaitingSE, aceSEVolume);
    }

    if(isEnemyCard)
    {
        StartCoroutine(ResolveEnemyAceAfterCue(selfCard));
        return;
    }

    Debug.Log("破壊する敵カードを選択してください");
}

IEnumerator ResolveEnemyAceAfterCue(CardController selfCard)
{
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, enemyAceSelectionDuration));
        if(!isSelectingDestroyTarget || pendingDestroySelfCard != selfCard) yield break;
        CardController target =
            GetCpuDestroyTargetFromPlayerBattle();

        if(target == null)
        {
            Debug.Log("CPU A効果：破壊対象なし");

            SendCardToOwnGraveyard(selfCard);

            pendingDestroySelfCard = null;
            isSelectingDestroyTarget = false;
            yield break;
        }

        Debug.Log(
            "CPU A効果：破壊対象 = " +
            GetCardName(target.data)
        );

        resolvingEnemyAce = true;
        try { TrySelectDestroyTarget(target); }
        finally { resolvingEnemyAce = false; }
}

CardController GetCpuDestroyTargetFromPlayerBattle()
{
    if(playerBattleArea == null)
        return null;

    CardController plannedTarget = enemyAIBrain != null ? enemyAIBrain.ConsumePlannedDestroyTarget() : null;
    if(plannedTarget != null && plannedTarget.transform.parent == playerBattleArea)
        return plannedTarget;

    if(enemyAIBrain != null)
        return enemyAIBrain.SelectDestroyTarget(
            TCardAIUnityBridge.GetCards(playerBattleArea),
            TCardAIUnityBridge.GetCards(enemyBattleArea),
            GetAlivePlayerWallCount(), GetAliveEnemyWallCount());
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
    // 通常のカードタップから直接呼ばれても破壊しない。
    if(!isSelectingDestroyTarget)
    {
        Debug.Log("選択破壊モードではないため、カードタップを無視");
        return false;
    }

    if(target == null || target.data == null)
        return false;

    if(pendingDestroySelfCard == null ||
       pendingDestroySelfCard.data == null)
    {
        Debug.LogWarning("選択破壊の発動元がないため処理を中止");
        isSelectingDestroyTarget = false;
        pendingDestroySelfCard = null;
        return false;
    }

    bool sourceIsEnemy =
        enemyBattleArea != null &&
        pendingDestroySelfCard.transform.IsChildOf(enemyBattleArea);

    bool sourceIsPlayer =
        playerBattleArea != null &&
        pendingDestroySelfCard.transform.IsChildOf(playerBattleArea);

    if(sourceIsEnemy && !resolvingEnemyAce) return false;

    bool targetIsValidOpponent =
        (sourceIsEnemy &&
         playerBattleArea != null &&
         target.transform.IsChildOf(playerBattleArea)) ||
        (sourceIsPlayer &&
         enemyBattleArea != null &&
         target.transform.IsChildOf(enemyBattleArea));

    if(!targetIsValidOpponent || target == pendingDestroySelfCard)
    {
        Debug.Log("選択破壊の対象外カードをタップしたため無視");
        return false;
    }

    if (sourceIsPlayer && RequestPlayerDecision("destroy-target:" + target.GetInstanceID(),
        GetCardName(target.data) + "を効果で破壊します。\nよろしいですか？", () => TrySelectDestroyTarget(target))) return true;

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

    // 再入や二重タップを防ぐため、破壊処理より先に選択状態を閉じる。
    isSelectingDestroyTarget = false;

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

    public void DiscardPlayerHandCard(CardController card)
    {
        if(card != null && handDealer != null && handDealer.handArea != null &&
           card.transform.IsChildOf(handDealer.handArea))
            SendToGraveyard(card.gameObject, playerGraveyard);
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

    public void StartBattleEndResult() { StartBattleEndResult(false); }

    void StartBattleEndResult(bool playerDefeated)
    {
        if (IsBattleEnded) return;
        IsBattleEnded = true;
        DismissPlayerDecision();
        resultPlayerDefeated = playerDefeated;
        PrepareResultChoicePanel();
        if (resultPanel != null) resultPanel.SetActive(false);
        // Cancel the whole turn chain, including the parent enemy-turn coroutine.
        StopAllCoroutines();
        battleIconsEnded = true;
        enemyBattleIconsActive = false;
        HideAllCardIcons();
        //Debug.LogError("★★ StartBattleEndResult 呼ばれた ★★");

        isPlayerTurnActive = false;
        isPlayerMainPhase = false;
        isBattlePhase = false;
        isEndingTurn = true;
        SetEndTurnButton(false);
        HideAttackArrow();
        HideNoBlockButton();
        if (jokerEffectPanel != null) jokerEffectPanel.SetActive(false);

        FreezeBattleAtResult();
        if (handDealer != null)
        {
            if (playerDefeated) handDealer.StartDefeatSequence();
            else handDealer.StartVictorySequence();
        }

        StartCoroutine(
            BattleEndResultRoutine()
        );
    }

    void SetEndTurnButton(bool value)
    {
        value = value && !IsBattleEnded;
        if(endTurnButton == null)
            return;

        isPlayerTurnActive = value;
        SyncEndTurnButtonVisibility();
    }

    void SyncEndTurnButtonVisibility()
    {
        if(endTurnButton == null)
            return;

        bool shouldShow = isPlayerTurnActive && !isEndingTurn && (handDealer == null || !handDealer.IsWallPlacementRunning);
        GameObject target = endTurnButtonRoot != null
            ? endTurnButtonRoot
            : endTurnButton.gameObject;

        if(target.activeSelf != shouldShow)
            target.SetActive(shouldShow);

        endTurnButton.interactable = shouldShow;
    }

IEnumerator BattleEndResultRoutine()
{
    Debug.Log("BattleEndResultRoutine 開始");

    yield return new WaitForSecondsRealtime(Mathf.Max(0f, resultWaitTime));

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

    resultChoiceVisible = true;
    Debug.Log("戦闘終了UI表示");
}

bool IsResultUi(Transform target)
{
    return (resultPanel != null && target.IsChildOf(resultPanel.transform)) ||
        (retryButton != null && target.IsChildOf(retryButton.transform)) ||
        (exitButton != null && target.IsChildOf(exitButton.transform)) ||
        (handDealer != null && handDealer.defeatLogo != null && target.IsChildOf(handDealer.defeatLogo.transform)) ||
        (handDealer != null && handDealer.victoryLogo != null && target.IsChildOf(handDealer.victoryLogo.transform));
}

void FreezeBattleAtResult()
{
    battleFrozen = true;
    timeScaleBeforeResult = Time.timeScale;
    // Stop gameplay coroutines even when they use unscaled time or WaitUntil.
    foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
    {
        if (behaviour == null || behaviour == this || behaviour.gameObject.scene != gameObject.scene ||
            behaviour.GetType().Assembly != typeof(TurnManager).Assembly || IsResultUi(behaviour.transform)) continue;
        behaviour.StopAllCoroutines();
    }
    List<Tween> playing = DOTween.PlayingTweens();
    if (playing != null)
    {
        foreach (Tween tween in playing)
        {
            // Existing board tweens may use unscaled time. Result UI starts separately.
            if (tween.target is Component target && IsResultUi(target.transform)) continue;
            tween.Pause();
            frozenTweens.Add(tween);
        }
    }
    foreach (Animator animator in FindObjectsByType<Animator>(FindObjectsSortMode.None))
    {
        if (!animator.enabled || animator.gameObject.scene != gameObject.scene || IsResultUi(animator.transform)) continue;
        animator.enabled = false;
        frozenAnimators.Add(animator);
    }
    foreach (ParticleSystem particles in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
    {
        if (!particles.isPlaying || particles.gameObject.scene != gameObject.scene || IsResultUi(particles.transform)) continue;
        particles.Pause(false);
        frozenParticles.Add(particles);
    }
    foreach (CardController card in FindObjectsByType<CardController>(FindObjectsSortMode.None))
    {
        if (card.gameObject.scene != gameObject.scene) continue;
        CanvasGroup group = card.GetComponent<CanvasGroup>();
        if (group == null) group = card.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
    }
    Time.timeScale = 0f;
}

void OnDestroy()
{
    if (playerDecisionOverlay != null) Destroy(playerDecisionOverlay.gameObject);
    if (ownsResultChoiceFont && resultChoiceFont != null) Destroy(resultChoiceFont);
        if (decisionTimerPanel != null) Destroy(decisionTimerPanel.gameObject);
    if (!battleFrozen) return;
    // A scene reload/retry must not inherit the frozen clock.
    Time.timeScale = timeScaleBeforeResult;
    foreach (Tween tween in frozenTweens)
        if (tween != null && tween.IsActive()) tween.Play();
    foreach (Animator animator in frozenAnimators)
        if (animator != null) animator.enabled = true;
    foreach (ParticleSystem particles in frozenParticles)
        if (particles != null) particles.Play(false);
}

void ClearArea(Transform area)
{
    if(area == null)
        return;
    WallAreaLayout wallLayout = area.GetComponent<WallAreaLayout>();
    if (wallLayout != null) { wallLayout.ClearWalls(false); return; }

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
    if (RequestPlayerDecision("joker-clear", "Jokerの効果で場のカードを一掃します。\nよろしいですか？", OnSelectJokerClear)) return;
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
        if (RequestPlayerDecision("joker-extra", "Jokerの効果で追加ターンを選びます。\nよろしいですか？", OnSelectJokerExtraTurn)) return;
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

    void ClearAttackableCards()
    {
        if(playerBattleArea == null)
            return;

        for(int i = 0; i < playerBattleArea.childCount; i++)
        {
            CardController card =
                playerBattleArea.GetChild(i)
                .GetComponent<CardController>();

            if(card != null)
            {
                card.SetAttackable(false);
            }
        }
    }
    void StackEnemyKingBase(
        CardController kCard,
        CardController baseCard
    )
    {
        if (
            kCard == null ||
            baseCard == null
        )
        {
            return;
        }

        StackedCard stacked =
            kCard.GetComponent<StackedCard>();

        if (stacked == null)
        {
            stacked =
                kCard.gameObject
                    .AddComponent<StackedCard>();
        }

        stacked.baseCard =
            baseCard.gameObject;

        CanvasGroup kCg =
            kCard.GetComponent<CanvasGroup>();

        if (kCg == null)
        {
            kCg =
                kCard.gameObject
                    .AddComponent<CanvasGroup>();
        }

        kCg.alpha = 1f;
        kCg.blocksRaycasts = true;
        kCg.interactable = true;

        baseCard.SetAttackable(false);
        baseCard.Untap();

        // Stop the field placement tween before the card becomes part of the stack.
        baseCard.transform.DOKill();
        CCP.Card proBase = baseCard.GetComponent<CCP.Card>();
        if (proBase != null)
        {
            proBase.enabled = false;
            if (proBase.parentCardGroup != null)
                proBase.parentCardGroup.RemoveCard(proBase);
            proBase.isBeingMovedManually = true;
        }

        baseCard.transform.SetParent(
            kCard.transform,
            false
        );

        // CCP's Front must remain above the base card.
        baseCard.transform.SetAsFirstSibling();

        RectTransform baseRt =
            baseCard.GetComponent<RectTransform>();

        if (baseRt != null)
        {
            baseRt.anchorMin =
                new Vector2(0.5f, 0.5f);

            baseRt.anchorMax =
                new Vector2(0.5f, 0.5f);

            baseRt.pivot =
                new Vector2(0.5f, 0.5f);

            baseRt.anchoredPosition =
                new Vector2(0f, -12f);

            baseRt.localScale =
                Vector3.one * 0.95f;
            baseRt.localRotation = Quaternion.identity;
        }

        CanvasGroup cg =
            baseCard.GetComponent<CanvasGroup>();

        if (cg == null)
        {
            cg =
                baseCard.gameObject
                    .AddComponent<CanvasGroup>();
        }

        cg.alpha = 1f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        CardDrag drag =
            baseCard.GetComponent<CardDrag>();

        if (drag != null)
            drag.enabled = false;

        // CardDrag.OnDisable restores raycasts; a stacked base must stay inert.
        cg.blocksRaycasts = false;
        cg.interactable = false;

        BattleCardClick click =
            baseCard.GetComponent<BattleCardClick>();

        if (click != null)
            click.enabled = false;

        CardActionIcon icon =
            baseCard.GetComponent<CardActionIcon>();

        if (icon != null)
            icon.HideAll();

        Debug.Log(
            "K特殊召喚：" +
            "土台カードを重ねた → " +
            baseCard.data.name
        );
    }
    void SyncBattleActionIcons()
    {
        bool playerAttack = !battleIconsEnded && isBattlePhase && isPlayerTurnActive && !isEndingTurn;
        bool enemyAttack = !battleIconsEnded && enemyBattleIconsActive;
        CardActionIcon template = cardPrefab != null ? cardPrefab.GetComponent<CardActionIcon>() : null;
        SyncFieldActionIcons(playerBattleArea, playerAttack, enemyAttack, template);
        SyncFieldActionIcons(enemyBattleArea, enemyAttack, playerAttack, template);
    }

    void SyncFieldActionIcons(Transform area, bool attackMode, bool defenseMode, CardActionIcon template)
    {
        if(area == null) return;
        foreach(Transform child in area)
        {
            CardController controller = child.GetComponent<CardController>();
            if(controller == null) continue;
            CardActionIcon icon = child.GetComponent<CardActionIcon>();
            bool attack = child.gameObject.activeInHierarchy && attackMode && CardActionIcon.CanAttack(controller);
            bool block = child.gameObject.activeInHierarchy && defenseMode && CardActionIcon.CanBlock(controller);
            if(icon == null && (attack || block)) icon = child.gameObject.AddComponent<CardActionIcon>();
            if(icon == null) continue;
            if(attack || block) icon.EnsureIcons(template);
            icon.SetAvailability(attack, block);
        }
    }
}

[System.Serializable]
public class InactivityTimerRule
{
    [Tooltip("無操作の制限秒数。0でこのタイマーを無効にします。")]
    [Min(0f)] public float seconds;
    [Tooltip("タイマーを表示する位置。未設定なら対象のエリア／パネルを使います。")]
    public RectTransform anchor;
    public Vector2 offset = new Vector2(0f, 64f);
    public InactivityTimerRule(float duration) { seconds = duration; }
}

// Pure countdown state: only a new decision or actual input resets inactivity.
public sealed class DecisionCountdown
{
    public string Context { get; private set; }
    public float Remaining { get; private set; }
    public float Duration { get; private set; }
    public float DelayRemaining { get; private set; }
    float displayDelay;
    bool expired;
    public void Reset() { Context = null; Remaining = Duration = DelayRemaining = displayDelay = 0f; expired = false; }
    public bool Advance(string context, float seconds, float delta, bool activity, bool paused, float delay = 0f)
    {
        if (string.IsNullOrEmpty(context) || seconds <= 0f) { Reset(); return false; }
        delay = System.Math.Max(0f, delay);
        if (Context != context || Duration != seconds || displayDelay != delay)
        {
            Context = context; Duration = Remaining = seconds; expired = false;
            displayDelay = DelayRemaining = delay;
            return false;
        }
        if (expired || paused) return false;
        if (activity) { Remaining = Duration; DelayRemaining = displayDelay; return false; }
        delta = System.Math.Max(0f, delta);
        if (DelayRemaining > 0f)
        {
            float consumed = System.Math.Min(DelayRemaining, delta);
            DelayRemaining -= consumed;
            delta -= consumed;
        }
        Remaining = System.Math.Max(0f, Remaining - delta);
        if (Remaining > 0f) return false;
        expired = true;
        return true;
    }
}

public partial class TurnManager
{
    [Header("Inactivity timers / 0 seconds disables a position")]
    [SerializeField] bool inactivityTimersEnabled = true;
    [SerializeField] InactivityTimerRule confirmationTimer = new InactivityTimerRule(15f) { offset = new Vector2(0f, -360f) };
    [SerializeField] InactivityTimerRule openingTimer = new InactivityTimerRule(30f);
    [SerializeField] InactivityTimerRule resourceTimer = new InactivityTimerRule(20f);
    [SerializeField] InactivityTimerRule mainTimer = new InactivityTimerRule(45f);
    [SerializeField] InactivityTimerRule battleTimer = new InactivityTimerRule(30f);
    [SerializeField] InactivityTimerRule attackTargetTimer = new InactivityTimerRule(15f);
    [SerializeField] InactivityTimerRule blockTimer = new InactivityTimerRule(15f);
    [SerializeField] InactivityTimerRule shieldTimer = new InactivityTimerRule(15f);
    [SerializeField] InactivityTimerRule effectTimer = new InactivityTimerRule(20f);
    [SerializeField] InactivityTimerRule discardTimer = new InactivityTimerRule(25f);
    [Header("Inactivity timer display")]
    [SerializeField] Vector2 inactivityTimerSize = new Vector2(172f, 44f);
    [SerializeField, Min(0.5f)] float inactivityTimerScale = 1.7f;
    [Tooltip("全セクション共通。無操作がこの秒数続くまで何も表示せず、その後にカウントを開始します。")]
    [SerializeField, Min(0f)] float inactivityTimerDisplayDelay = 5f;
    [SerializeField] Sprite inactivityTimerPanelSprite;
    [SerializeField] Color inactivityTimerColor = new Color(0.95f, 0.77f, 0.35f, 1f);
    [SerializeField] Color inactivityTimerWarningColor = new Color(1f, 0.34f, 0.22f, 1f);
    readonly DecisionCountdown decisionCountdown = new DecisionCountdown();
    RectTransform decisionTimerPanel;
    RectTransform decisionTimerFill;
    TMPro.TextMeshProUGUI decisionTimerText;
    Image decisionTimerFillImage;
    Canvas decisionTimerCanvas;
    Vector3 previousDecisionPointer;
    float nextDragRefresh;
    CardDrag[] decisionDrags = new CardDrag[0];

    enum TimedDecision { Confirmation, Opening, Resource, Main, Battle, Attack, Block, Shield, Destroy, Joker, Discard }
    struct PendingDecision
    {
        public TimedDecision kind;
        public InactivityTimerRule rule;
        public Transform anchor;
        public string key;
        public string label;
        public PendingDecision(TimedDecision value, InactivityTimerRule settings, Transform position, string identity, string title)
        { kind = value; rule = settings; anchor = position; key = identity; label = title; }
    }

    bool TryGetTimedDecision(out PendingDecision decision)
    {
        decision = default;
        if (IsPlayerDecisionOpen)
        {
            decision = new PendingDecision(TimedDecision.Confirmation, confirmationTimer, playerDecisionBody,
                "confirmation:" + playerDecisionRevision, "CONFIRM / CANCEL");
            return true;
        }
        if (handDealer != null && handDealer.IsHandLimitSelecting)
        {
            if (!handDealer.IsHandLimitDecisionReady) return false;
            decision = new PendingDecision(TimedDecision.Discard, discardTimer,
                handDealer.HandLimitDrawnCardObject != null ? handDealer.HandLimitDrawnCardObject.transform : handDealer.handArea,
                "discard:" + (handDealer.HandLimitDrawnCardObject != null ? handDealer.HandLimitDrawnCardObject.GetInstanceID() : 0), "DISCARD");
            return true;
        }
        ReverseChoiceManager reverse = ReverseChoiceManager.I;
        if (reverse != null && reverse.IsChoosing)
        {
            GameObject panel = reverse.IsChoosingOrder ? reverse.orderPanel : reverse.choicePanel;
            decision = new PendingDecision(TimedDecision.Shield, shieldTimer, panel != null ? panel.transform : playerWallArea,
                "shield:" + reverse.ChoiceRevision, reverse.IsChoosingOrder ? "SHIELD ORDER" : "SHIELD CHOICE");
            return true;
        }
        if (handDealer != null && handDealer.IsOpeningDecisionReady)
        {
            decision = new PendingDecision(TimedDecision.Opening, openingTimer, handDealer.handArea, "opening", "CONFIRM HAND");
            return true;
        }
        if (isSelectingDestroyTarget)
        {
            // CPU selections are automatic and must not be timed as player decisions.
            if (pendingDestroySelfCard == null || playerBattleArea == null ||
                !pendingDestroySelfCard.transform.IsChildOf(playerBattleArea)) return false;
            decision = new PendingDecision(TimedDecision.Destroy, effectTimer, enemyBattleArea,
                "effect:" + pendingDestroySelfCard.GetInstanceID(), "EFFECT TARGET");
            return true;
        }
        if (jokerEffectPanel != null && jokerEffectPanel.activeInHierarchy && pendingJokerEffectCard != null &&
            playerBattleArea != null && pendingJokerEffectCard.transform.IsChildOf(playerBattleArea))
        {
            decision = new PendingDecision(TimedDecision.Joker, effectTimer, jokerEffectPanel.transform,
                "joker:" + pendingJokerEffectCard.GetInstanceID(), "JOKER CHOICE");
            return true;
        }
        if (waitingBlockSelect && !resolvingEnemyBlock)
        {
            decision = new PendingDecision(TimedDecision.Block, blockTimer, playerBattleArea,
                "block:" + (pendingEnemyAttacker != null ? pendingEnemyAttacker.GetInstanceID() : 0), "BLOCK / PASS");
            return true;
        }
        if (!isPlayerTurnActive || isEndingTurn || isFirstTurnStarting || resolvingEnemyBlock || resolvingPlayerCardAttack ||
            (handDealer != null && (HandDealer.IsRedrawSelecting || handDealer.IsTurnDrawAnimating ||
                handDealer.IsWallPlacementRunning || handDealer.IsResolvingWallAttack)) ||
            (CardEffectManager.I != null && CardEffectManager.I.IsResolvingNine)) return false;
        // Do not time out in the middle of a committed summon or resource effect.
        if (resourcePhaseManager != null && resourcePhaseManager.IsResolvingCharge) return false;
        RefreshDecisionDrags();
        foreach (CardDrag drag in decisionDrags)
            if (drag != null && drag.IsDropSnapPlaying) return false;
        if (selectingTarget)
        {
            decision = new PendingDecision(TimedDecision.Attack, attackTargetTimer,
                enemyWallArea != null ? enemyWallArea : enemyBattleArea,
                "attack:" + (currentAttacker != null ? currentAttacker.GetInstanceID() : 0) + ":" + pendingWallTargets.Count, "ATTACK TARGET");
            return true;
        }
        if (resourcePhaseManager != null && resourcePhaseManager.IsRunning())
        {
            decision = new PendingDecision(TimedDecision.Resource, resourceTimer, resourcePhaseManager.resourceArea,
                "resource:" + turnCount, "RESOURCE / SKIP");
            return true;
        }
        if (isBattlePhase || isPlayerMainPhase)
        {
            decision = new PendingDecision(isBattlePhase ? TimedDecision.Battle : TimedDecision.Main,
                isBattlePhase ? battleTimer : mainTimer, playerBattleArea,
                (isBattlePhase ? "battle:" : "main:") + turnCount, isBattlePhase ? "BATTLE / END TURN" : "MAIN / NEXT PHASE");
            return true;
        }
        return false;
    }

    void RefreshDecisionDrags()
    {
        if (Time.unscaledTime < nextDragRefresh) return;
        nextDragRefresh = Time.unscaledTime + 0.25f;
        decisionDrags = FindObjectsByType<CardDrag>(FindObjectsSortMode.None);
    }

    bool HasDecisionInput()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        Vector3 pointer = Input.mousePosition;
        bool movingDrag = Input.GetMouseButton(0) && (pointer - previousDecisionPointer).sqrMagnitude > 4f;
        previousDecisionPointer = pointer;
        return Input.anyKeyDown || Input.GetMouseButtonUp(0) || movingDrag;
#elif ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        var touch = UnityEngine.InputSystem.Touchscreen.current;
        return (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.leftButton.wasReleasedThisFrame ||
            (mouse.leftButton.isPressed && mouse.delta.ReadValue().sqrMagnitude > 4f))) ||
            (keyboard != null && keyboard.anyKey.wasPressedThisFrame) ||
            (touch != null && (touch.primaryTouch.press.wasPressedThisFrame || touch.primaryTouch.press.wasReleasedThisFrame ||
                (touch.primaryTouch.press.isPressed && touch.primaryTouch.delta.ReadValue().sqrMagnitude > 4f)));
#else
        return false;
#endif
    }

    void TickDecisionTimer()
    {
        if (!inactivityTimersEnabled || IsBattleEnded || Time.timeScale <= 0f)
        {
            if (decisionTimerPanel != null) decisionTimerPanel.gameObject.SetActive(false);
            if (!inactivityTimersEnabled || IsBattleEnded) decisionCountdown.Reset();
            return;
        }
        bool input = HasDecisionInput();
        if (!TryGetTimedDecision(out PendingDecision decision))
        {
            // A committed animation pauses the timer, but the action that started it
            // still counts as activity when the same phase resumes.
            if (input && decisionCountdown.Context != null)
                decisionCountdown.Advance(decisionCountdown.Context, decisionCountdown.Duration, 0f, true, false, inactivityTimerDisplayDelay);
            if (decisionTimerPanel != null) decisionTimerPanel.gameObject.SetActive(false);
            return;
        }
        if (decision.rule == null || decision.rule.seconds <= 0f)
        {
            decisionCountdown.Reset();
            if (decisionTimerPanel != null) decisionTimerPanel.gameObject.SetActive(false);
            return;
        }
        bool timeout = decisionCountdown.Advance(decision.key, decision.rule.seconds, Time.unscaledDeltaTime, input, false, inactivityTimerDisplayDelay);
        if (decisionCountdown.DelayRemaining > 0f)
        {
            if (decisionTimerPanel != null) decisionTimerPanel.gameObject.SetActive(false);
        }
        else ShowDecisionTimer(decision);
        if (!timeout) return;
        if (decisionTimerPanel != null) decisionTimerPanel.gameObject.SetActive(false);
        // Return held cards before the next phase locks the hand.
        decisionDrags = FindObjectsByType<CardDrag>(FindObjectsSortMode.None);
        foreach (CardDrag drag in decisionDrags)
            if (drag != null && drag.IsDragging) drag.OnEndDrag(null);
        Debug.Log("Inactivity timeout: " + decision.label);
        RunAutomaticDecision(() => ResolveDecisionTimeout(decision.kind));
        decisionCountdown.Reset();
    }

    void ResolveDecisionTimeout(TimedDecision kind)
    {
        if (IsBattleEnded) return;
        switch (kind)
        {
            case TimedDecision.Confirmation: RejectPlayerDecision(); break;
            case TimedDecision.Opening: handDealer.ConfirmHand(); break;
            case TimedDecision.Resource: resourcePhaseManager.CompleteResourcePhase(); break;
            case TimedDecision.Main: StartPlayerBattlePhase(); break;
            case TimedDecision.Block: OnNoBlockButtonClicked(); break;
            case TimedDecision.Shield:
                if (ReverseChoiceManager.I != null) ReverseChoiceManager.I.ResolveInactivity();
                break;
            case TimedDecision.Discard: handDealer.ResolveHandLimitInactivity(); break;
            case TimedDecision.Destroy:
                CardController source = pendingDestroySelfCard;
                pendingDestroySelfCard = null;
                isSelectingDestroyTarget = false;
                if (aceSelectionEffect != null) Destroy(aceSelectionEffect.gameObject);
                if (source != null) SendCardToOwnGraveyard(source);
                break;
            case TimedDecision.Joker:
                jokerEffectPanel.SetActive(false);
                CardController joker = pendingJokerEffectCard;
                pendingJokerEffectCard = null;
                if (joker != null) SendCardToOwnGraveyard(joker);
                break;
            case TimedDecision.Attack:
                CancelTimedAttackSelection();
                break;
            case TimedDecision.Battle:
                CancelTimedAttackSelection();
                RunAutomaticDecision(EndPlayerTurn);
                break;
        }
    }

    void CancelTimedAttackSelection()
    {
        selectingTarget = false;
        pendingWallTargets.Clear();
        wallBreakCount = 0;
        maxWallBreakCount = 1;
        currentAttacker = null;
        SetEnemyWallClickable(false);
        HideAttackArrow();
        // Only cancel this target choice; the battle phase gets its own timer.
        if (isBattlePhase) ShowAttackableCards();
    }

    void EnsureDecisionTimerUi()
    {
        if (decisionTimerPanel != null) return;
        Canvas source = handDealer != null && handDealer.handArea != null
            ? handDealer.handArea.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
        if (source == null) source = FindFirstObjectByType<Canvas>();
        if (source == null) return;
        source = source.rootCanvas;
        GameObject panel = new GameObject("Decision Timer", typeof(RectTransform), typeof(Canvas), typeof(Image), typeof(Outline));
        panel.layer = source.gameObject.layer;
        decisionTimerPanel = panel.GetComponent<RectTransform>();
        decisionTimerPanel.SetParent(source.transform, false);
        decisionTimerPanel.anchorMin = decisionTimerPanel.anchorMax = new Vector2(0.5f, 0.5f);
        decisionTimerCanvas = panel.GetComponent<Canvas>();
        decisionTimerCanvas.overrideSorting = true;
        decisionTimerCanvas.sortingLayerID = source.sortingLayerID;
        decisionTimerCanvas.sortingOrder = source.sortingOrder + 150;
        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.12f, 0.09f, 0.06f, 0.94f);
        background.raycastTarget = false;
        Image existingPanel = resultPanel != null ? resultPanel.GetComponent<Image>() : null;
        background.sprite = inactivityTimerPanelSprite != null ? inactivityTimerPanelSprite
            : existingPanel != null ? existingPanel.sprite : null;
        background.type = Image.Type.Sliced;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0.82f, 0.64f, 0.3f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        GameObject label = new GameObject("Time Remaining", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        label.layer = panel.layer;
        label.transform.SetParent(panel.transform, false);
        decisionTimerText = label.GetComponent<TMPro.TextMeshProUGUI>();
        TMPro.TMP_Text template = source.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (template != null && template != decisionTimerText && template.font != null) decisionTimerText.font = template.font;
        decisionTimerText.fontSize = 13f;
        decisionTimerText.enableAutoSizing = true;
        decisionTimerText.fontSizeMin = 9f;
        decisionTimerText.fontSizeMax = 13f;
        decisionTimerText.alignment = TMPro.TextAlignmentOptions.Center;
        decisionTimerText.raycastTarget = false;
        RectTransform textRect = label.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6f, 8f); textRect.offsetMax = new Vector2(-6f, -2f);
        GameObject track = new GameObject("Time Track", typeof(RectTransform), typeof(Image));
        track.layer = panel.layer;
        track.transform.SetParent(panel.transform, false);
        Image trackImage = track.GetComponent<Image>();
        trackImage.color = new Color(1f, 1f, 1f, 0.15f); trackImage.raycastTarget = false;
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0f); trackRect.anchorMax = new Vector2(1f, 0f);
        trackRect.pivot = new Vector2(0.5f, 0f);
        trackRect.offsetMin = new Vector2(7f, 5f); trackRect.offsetMax = new Vector2(-7f, 8f);
        GameObject fill = new GameObject("Time Fill", typeof(RectTransform), typeof(Image));
        fill.layer = panel.layer;
        fill.transform.SetParent(track.transform, false);
        decisionTimerFill = fill.GetComponent<RectTransform>();
        decisionTimerFill.anchorMin = Vector2.zero; decisionTimerFill.anchorMax = Vector2.one;
        decisionTimerFill.offsetMin = decisionTimerFill.offsetMax = Vector2.zero;
        decisionTimerFillImage = fill.GetComponent<Image>(); decisionTimerFillImage.raycastTarget = false;
    }

    void ShowDecisionTimer(PendingDecision decision)
    {
        EnsureDecisionTimerUi();
        if (decisionTimerPanel == null) return;
        decisionTimerPanel.gameObject.SetActive(true);
        float displayScale = Mathf.Max(0.5f, inactivityTimerScale);
        decisionTimerPanel.sizeDelta = new Vector2(Mathf.Max(100f, inactivityTimerSize.x), Mathf.Max(30f, inactivityTimerSize.y)) * displayScale;
        decisionTimerText.fontSizeMin = 9f * displayScale;
        decisionTimerText.fontSizeMax = 13f * displayScale;
        decisionTimerPanel.SetAsLastSibling();
        Transform anchor = decision.rule.anchor != null ? decision.rule.anchor : decision.anchor;
        RectTransform parent = decisionTimerPanel.parent as RectTransform;
        RectTransform anchorRect = anchor as RectTransform;
        Vector3 point = anchorRect != null ? anchorRect.TransformPoint(anchorRect.rect.center)
            : anchor != null ? anchor.position : parent.TransformPoint(parent.rect.center);
        Vector3 local = parent.InverseTransformPoint(point);
        local += (Vector3)decision.rule.offset;
        float halfWidth = decisionTimerPanel.sizeDelta.x * 0.5f;
        float halfHeight = decisionTimerPanel.sizeDelta.y * 0.5f;
        local.x = Mathf.Clamp(local.x, parent.rect.xMin + halfWidth + 6f, parent.rect.xMax - halfWidth - 6f);
        local.y = Mathf.Clamp(local.y, parent.rect.yMin + halfHeight + 6f, parent.rect.yMax - halfHeight - 6f);
        local.z = 0f;
        decisionTimerPanel.localPosition = local;
        Color color = decisionCountdown.Remaining <= 5f ? inactivityTimerWarningColor : inactivityTimerColor;
        decisionTimerText.color = color;
        decisionTimerText.text = decision.label + "  " + Mathf.CeilToInt(decisionCountdown.Remaining) + "s";
        decisionTimerFill.anchorMax = new Vector2(Mathf.Clamp01(decisionCountdown.Remaining / Mathf.Max(0.01f, decisionCountdown.Duration)), 1f);
        decisionTimerFillImage.color = color;
    }
}

public partial class TurnManager
{
    bool resultPlayerDefeated;
    [Header("Result choice panel size")]
    [SerializeField, Min(0.5f)] float resultChoiceScale = 2f;
    bool resultChoiceVisible;
    bool resultSceneReloadRequested;
    Font resultChoiceFont;
    bool ownsResultChoiceFont;

    GameObject FindResultObject(string objectName)
    {
        foreach (RectTransform rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (rect.gameObject.scene == gameObject.scene && rect.gameObject.name == objectName) return rect.gameObject;
        return null;
    }

    void PrepareResultChoicePanel()
    {
        Canvas source = handDealer != null && handDealer.handArea != null
            ? handDealer.handArea.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
        if (source == null) source = FindFirstObjectByType<Canvas>();
        if (source == null) { Debug.LogError("Result panel needs a Canvas"); return; }
        source = source.rootCanvas;
        if (resultPanel == null) resultPanel = FindResultObject("ResultPanel");
        if (retryButton == null) retryButton = FindResultObject("RetryButton");
        if (exitButton == null) exitButton = FindResultObject("ExitButton");
        if (resultPanel == null) resultPanel = new GameObject("ResultPanel", typeof(RectTransform));
        resultPanel.SetActive(false);
        resultPanel.layer = source.gameObject.layer;
        RectTransform overlay = resultPanel.GetComponent<RectTransform>();
        overlay.SetParent(source.transform, false);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        overlay.localScale = Vector3.one;
        Canvas canvas = resultPanel.GetComponent<Canvas>();
        if (canvas == null) canvas = resultPanel.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingLayerID = source.sortingLayerID;
        canvas.sortingOrder = source.sortingOrder + 300;
        if (resultPanel.GetComponent<GraphicRaycaster>() == null) resultPanel.AddComponent<GraphicRaycaster>();
        Image shade = resultPanel.GetComponent<Image>();
        if (shade == null) shade = resultPanel.AddComponent<Image>();
        shade.sprite = null;
        shade.color = new Color(0f, 0f, 0f, 0.58f);
        shade.raycastTarget = true;
        CanvasGroup group = resultPanel.GetComponent<CanvasGroup>();
        if (group == null) group = resultPanel.AddComponent<CanvasGroup>();
        group.alpha = 1f; group.interactable = group.blocksRaycasts = true; group.ignoreParentGroups = true;

        if (resultChoiceFont == null)
        {
        resultChoiceFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Noto Sans CJK JP", "Arial" }, 28);
        ownsResultChoiceFont = resultChoiceFont != null;
        if (resultChoiceFont == null) resultChoiceFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        GameObject body = new GameObject("Result Choices", typeof(RectTransform), typeof(Image), typeof(Outline));
        body.layer = resultPanel.layer;
        body.transform.SetParent(overlay, false);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.sizeDelta = new Vector2(330f, 280f);
        bodyRect.localScale = Vector3.one * Mathf.Max(0.5f, resultChoiceScale);
        body.GetComponent<Image>().color = new Color(0.16f, 0.12f, 0.08f, 0.98f);
        Outline border = body.GetComponent<Outline>();
        border.effectColor = new Color(0.86f, 0.69f, 0.36f, 1f);
        border.effectDistance = new Vector2(2f, -2f);
        CreateResultLabel(bodyRect, "Outcome", resultPlayerDefeated ? "敗北" : "勝利", new Vector2(0f, 98f), new Vector2(300f, 46f), 32,
            new Color(1f, 0.85f, 0.52f, 1f));
        CreateResultLabel(bodyRect, "Prompt", "もう一度対戦しますか？", new Vector2(0f, 53f), new Vector2(300f, 34f), 20, Color.white);
        retryButton = ConfigureResultButton(retryButton, bodyRect, "RetryButton", "再戦", new Vector2(0f, -8f), RetryMatchFromBeginning);
        exitButton = ConfigureResultButton(exitButton, bodyRect, "ExitButton", "タイトルに戻る", new Vector2(0f, -79f), ReturnToTitle);
    }

    Text CreateResultLabel(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        GameObject label = new GameObject(name, typeof(RectTransform), typeof(Text));
        label.layer = parent.gameObject.layer;
        label.transform.SetParent(parent, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        Text value = label.GetComponent<Text>();
        value.font = resultChoiceFont; value.text = text; value.fontSize = fontSize;
        value.alignment = TextAnchor.MiddleCenter; value.color = color;
        value.raycastTarget = false;
        return value;
    }

    GameObject ConfigureResultButton(GameObject existing, RectTransform parent, string objectName, string caption,
        Vector2 position, UnityEngine.Events.UnityAction action)
    {
        // Result buttons have their own geometry; old sprites and controls can
        // compress the frame or bring overlapping labels into the modal.
        if (existing != null) existing.SetActive(false);
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform));
        buttonObject.layer = parent.gameObject.layer;
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = new Vector2(278f, 62f); rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        Image image = buttonObject.GetComponent<Image>();
        if (image == null) image = buttonObject.AddComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = new Color(1f, 0.92f, 0.73f, 1f); image.raycastTarget = true;
        Outline border = buttonObject.AddComponent<Outline>();
        border.effectColor = new Color(0.86f, 0.65f, 0.27f, 1f);
        border.effectDistance = new Vector2(1f, -1f);
        Button button = buttonObject.GetComponent<Button>();
        if (button == null) button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image; button.interactable = true;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
        CanvasGroup group = buttonObject.GetComponent<CanvasGroup>();
        if (group != null) { group.alpha = 1f; group.interactable = group.blocksRaycasts = true; }
        Text label = CreateResultLabel(rect, "Choice Label", caption, Vector2.zero, new Vector2(254f, 54f), 26,
            new Color(0.18f, 0.12f, 0.06f, 1f));
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 18;
        label.resizeTextMaxSize = 26;
        buttonObject.SetActive(true);
        return buttonObject;
    }

    public void RetryMatchFromBeginning() { ReloadAfterResult(true); }
    public void ReturnToTitle() { ReloadAfterResult(false); }

    void ReloadAfterResult(bool rematch)
    {
        if (!IsBattleEnded || !resultChoiceVisible || resultSceneReloadRequested) return;
        resultSceneReloadRequested = true;
        if (retryButton != null) retryButton.GetComponent<Button>().interactable = false;
        if (exitButton != null) exitButton.GetComponent<Button>().interactable = false;
        GameFlowManager flow = FindFirstObjectByType<GameFlowManager>();
        TitleManager.SetResultDestination(rematch, flow != null ? flow.selectedDifficulty : GameFlowManager.Difficulty.Normal);
        // Retire frozen animations; do not resume the old match during scene teardown.
        foreach (Tween tween in frozenTweens)
            if (tween != null && tween.IsActive()) tween.Kill(false);
        frozenTweens.Clear(); frozenAnimators.Clear(); frozenParticles.Clear();
        battleFrozen = false;
        Time.timeScale = 1f;
        HandDealer.IsRedrawSelecting = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.path);
    }
}

public partial class TurnManager
{
    int CompareBattleCards(CardController attacker, CardController defender)
    {
        if (attacker == null || defender == null || attacker.data == null || defender.data == null) return -1;
        attacker.data.SetPowerFromName(); defender.data.SetPowerFromName();
        if (HasSpecialWin(attacker, defender)) return 1;
        if (HasSpecialWin(defender, attacker)) return -1;
        int attack = attacker.data.power;
        int defense = defender.data.power;
        if (GameSettings.IsAdvancedRule)
        {
            attack += GetSuitBattleBonus(attacker.data.suit, defender.data.suit);
            defense += GetSuitBattleBonus(defender.data.suit, attacker.data.suit);
        }
        return attack.CompareTo(defense);
    }

    CardController GetEnemyRestedAttackTarget(CardController attacker)
    {
        // Keep an available direct finishing attack; otherwise consider a rested unit.
        if (playerBattleArea == null || GetAlivePlayerWallCount() == 0) return null;
        CardController best = null;
        int bestValue = int.MinValue;
        foreach (CardController candidate in TCardAIUnityBridge.GetCards(playerBattleArea))
        {
            if (candidate == null || candidate.data == null || !candidate.isTapped ||
                !candidate.gameObject.activeInHierarchy || CompareBattleCards(attacker, candidate) <= 0) continue;
            int value = candidate.data.power + (IsMonarchCard(candidate.data) ? 20 : 0);
            if (value > bestValue) { bestValue = value; best = candidate; }
        }
        return best;
    }

    IEnumerator EnemyAttackRestedCardRoutine(CardController attacker, CardController target)
    {
        if (attacker == null || target == null) yield break;
        pendingEnemyAttacker = attacker;
        pendingEnemyRestedTarget = target;
        pendingEnemyTargetWall = null;
        enemyAttackBlocked = false;
        attacker.Tap();
        if (attackArrowManager != null)
            attackArrowManager.ShowEnemyArrow(attacker.GetComponent<RectTransform>(), target.GetComponent<RectTransform>());
        yield return new WaitForSeconds(0.6f);
        if (HasUntappedPlayerBlocker())
        {
            waitingBlockSelect = true;
            ShowBlockableCards();
            ShowNoBlockButton(attacker.GetComponent<RectTransform>());
            while (waitingBlockSelect || resolvingEnemyBlock) yield return null;
        }
        HideNoBlockButton();
        HideAllCardIcons();
        HideAttackArrow();
        if (!enemyAttackBlocked && !IsBattleEnded && attacker != null && target != null &&
            attacker.transform.IsChildOf(enemyBattleArea) && target.transform.IsChildOf(playerBattleArea))
        {
            yield return PlayBlockerSlash(target.gameObject);
            if (attacker != null && target != null) ResolveCardBattle(attacker, target);
        }
        pendingEnemyAttacker = null;
        pendingEnemyRestedTarget = null;
        pendingEnemyTargetWall = null;
        waitingBlockSelect = false;
        enemyAttackBlocked = false;
        yield return new WaitForSeconds(0.4f);
    }
}

public partial class TurnManager
{
    [Header("Player decision panel")]
    [SerializeField, Min(0.5f)] float playerDecisionPanelScale = 2f;
    RectTransform playerDecisionOverlay;
    RectTransform playerDecisionBody;
    RectTransform playerDecisionBoard;
    Text playerDecisionMessage;
    System.Action pendingPlayerDecision;
    System.Action pendingPlayerCancellation;
    string pendingPlayerDecisionKey;
    string approvedPlayerDecisionKey;
    int automaticDecisionDepth;
    int playerDecisionRevision;
    public bool IsPlayerDecisionOpen => pendingPlayerDecision != null;

    // Returns true when the caller must defer. Only the exact approved action may pass.
    public bool RequestPlayerDecision(string key, string message, System.Action accept, System.Action cancel = null)
    {
        if (IsBattleEnded) return true;
        if (automaticDecisionDepth > 0 || approvedPlayerDecisionKey == key) return false;
        if (IsPlayerDecisionOpen) return true;
        EnsurePlayerDecisionUi();
        if (playerDecisionOverlay == null) return true;
        pendingPlayerDecisionKey = key;
        pendingPlayerDecision = accept;
        pendingPlayerCancellation = cancel;
        playerDecisionRevision++;
        playerDecisionMessage.text = message;
        playerDecisionOverlay.gameObject.SetActive(true);
        PositionPlayerDecisionPanel();
        decisionCountdown.Reset();
        return true;
    }

    public void RunAutomaticDecision(System.Action action)
    {
        automaticDecisionDepth++;
        try { action?.Invoke(); }
        finally { automaticDecisionDepth--; }
    }

    void AcceptPlayerDecision()
    {
        if (!IsPlayerDecisionOpen || IsBattleEnded) return;
        System.Action action = pendingPlayerDecision;
        string key = pendingPlayerDecisionKey;
        DismissPlayerDecision();
        string previous = approvedPlayerDecisionKey;
        approvedPlayerDecisionKey = key;
        try { action?.Invoke(); }
        finally { approvedPlayerDecisionKey = previous; }
    }

    void RejectPlayerDecision()
    {
        if (!IsPlayerDecisionOpen) return;
        System.Action cancel = pendingPlayerCancellation;
        DismissPlayerDecision();
        cancel?.Invoke();
    }

    void DismissPlayerDecision()
    {
        pendingPlayerDecision = pendingPlayerCancellation = null;
        pendingPlayerDecisionKey = null;
        if (playerDecisionOverlay != null) playerDecisionOverlay.gameObject.SetActive(false);
        decisionCountdown.Reset();
    }

    void EnsurePlayerDecisionUi()
    {
        if (playerDecisionOverlay != null) return;
        Canvas source = handDealer != null && handDealer.handArea != null
            ? handDealer.handArea.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
        if (source == null) source = FindFirstObjectByType<Canvas>();
        if (source == null) { Debug.LogError("Player decisions need a Canvas"); return; }
        source = source.rootCanvas;
        if (resultChoiceFont == null)
        {
            resultChoiceFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Noto Sans CJK JP", "Arial" }, 28);
            ownsResultChoiceFont = resultChoiceFont != null;
            if (resultChoiceFont == null) resultChoiceFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        TitleManager titleManager = FindFirstObjectByType<TitleManager>();
        playerDecisionBoard = titleManager != null && titleManager.gameScreen != null
            ? titleManager.gameScreen.transform as RectTransform : null;
        GameObject overlay = new GameObject("Player Decision Panel", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image), typeof(CanvasGroup));
        overlay.layer = source.gameObject.layer;
        playerDecisionOverlay = overlay.GetComponent<RectTransform>();
        playerDecisionOverlay.SetParent(source.transform, false);
        playerDecisionOverlay.anchorMin = Vector2.zero; playerDecisionOverlay.anchorMax = Vector2.one;
        playerDecisionOverlay.offsetMin = playerDecisionOverlay.offsetMax = Vector2.zero;
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.overrideSorting = true; canvas.sortingLayerID = source.sortingLayerID;
        canvas.sortingOrder = source.sortingOrder + 220;
        Image shade = overlay.GetComponent<Image>();
        shade.color = new Color(0f, 0f, 0f, 0.4f); shade.raycastTarget = true;
        CanvasGroup group = overlay.GetComponent<CanvasGroup>();
        group.ignoreParentGroups = true; group.interactable = group.blocksRaycasts = true;
        GameObject body = new GameObject("Decision Content", typeof(RectTransform), typeof(Image), typeof(Outline));
        body.layer = overlay.layer; body.transform.SetParent(overlay.transform, false);
        playerDecisionBody = body.GetComponent<RectTransform>();
        playerDecisionBody.anchorMin = playerDecisionBody.anchorMax = new Vector2(0.5f, 0.5f);
        playerDecisionBody.sizeDelta = new Vector2(330f, 320f);
        body.GetComponent<Image>().color = new Color(0.16f, 0.12f, 0.08f, 0.98f);
        Outline border = body.GetComponent<Outline>();
        border.effectColor = new Color(0.86f, 0.69f, 0.36f, 1f); border.effectDistance = new Vector2(2f, -2f);
        CreateResultLabel(body.transform, "Decision Title", "行動の確認", new Vector2(0f, 125f), new Vector2(300f, 44f), 28,
            new Color(1f, 0.85f, 0.52f, 1f));
        playerDecisionMessage = CreateResultLabel(body.transform, "Decision Message", "", new Vector2(0f, 55f), new Vector2(298f, 96f), 22, Color.white);
        playerDecisionMessage.resizeTextForBestFit = true;
        playerDecisionMessage.resizeTextMinSize = 17; playerDecisionMessage.resizeTextMaxSize = 22;
        ConfigureResultButton(null, playerDecisionBody, "Confirm Action", "はい", new Vector2(0f, -28f), AcceptPlayerDecision);
        ConfigureResultButton(null, playerDecisionBody, "Cancel Action", "いいえ", new Vector2(0f, -103f), RejectPlayerDecision);
        overlay.SetActive(false);
    }

    void PositionPlayerDecisionPanel()
    {
        if (playerDecisionBody == null || !IsPlayerDecisionOpen) return;
        playerDecisionBody.localScale = Vector3.one * Mathf.Max(0.5f, playerDecisionPanelScale);
        // Use the game board's center, independent of the chosen card or pointer.
        RectTransform board = playerDecisionBoard;
        if (board != null) playerDecisionBody.position = board.TransformPoint(board.rect.center);
        else playerDecisionBody.anchoredPosition = Vector2.zero;
        playerDecisionOverlay.SetAsLastSibling();
    }
}
