using UnityEngine;
using System.Collections.Generic;
public partial class TurnManager
{
    bool choosingMonarchWalls;
    System.Action pendingAttackConfirmation;
    readonly List<GameObject> pendingAttackTargets=new List<GameObject>();
    float attackConfirmationStartedAt;
    public bool AttackExecutionConfirmationActive => HasPendingAttackConfirmation;
    public bool IsAttackConfirmationTarget(GameObject target) => pendingAttackTargets.Contains(target);
    public IReadOnlyList<GameObject> AttackConfirmationTargets => pendingAttackTargets;
    public float AttackConfirmationElapsed => Time.unscaledTime - attackConfirmationStartedAt;
    bool attackConfirmationApproved;
    public bool HasPendingAttackConfirmation=>pendingAttackConfirmation!=null;
    public string AttackConfirmationLabel {get;private set;}
    internal AttackConfirmationKind AttackConfirmationType { get; private set; }
    void ClearPendingAttackConfirmation(){pendingAttackConfirmation=null;pendingAttackTargets.Clear();AttackConfirmationLabel=null;}
    bool RequestAttackConfirmation(AttackConfirmationKind kind, string label,System.Action execute,params GameObject[] targets)
    {
        if(attackConfirmationApproved)return false;
        pendingAttackConfirmation=execute;pendingAttackTargets.Clear();pendingAttackTargets.AddRange(targets);AttackConfirmationLabel=label;
        AttackConfirmationType = kind;
        attackConfirmationStartedAt = Time.unscaledTime;
        if(attackSelectionPresentation!=null)attackSelectionPresentation.Refresh();
        return true;
    }
    public void ConfirmAttackSelection()
    {
        if(!AttackSelectionActive || !HasPendingAttackConfirmation)return;
        var execute=pendingAttackConfirmation;ClearPendingAttackConfirmation();
        attackConfirmationApproved=true;
        try { execute(); }
        finally
        {
            attackConfirmationApproved = false;
            if (attackSelectionPresentation != null) attackSelectionPresentation.Refresh();
        }
    }
    AttackSelectionPresentation attackSelectionPresentation;
    public bool AttackSelectionActive => selectingTarget && CanChooseAttackCard(currentAttacker);
    public bool ChoosingMonarchWalls => AttackSelectionActive && choosingMonarchWalls;
    public bool IsMonarchAttacker
    {
        get {var c=currentAttacker!=null?currentAttacker.GetComponent<CardController>():null;if(c==null||c.data==null)return false;c.data.SetPowerFromName();return c.data.suit!=Suit.Joker&&(c.data.power==12||c.data.power==13);}
    }
    public bool CanChooseAttackCard(GameObject card)
    {
        return !PresentationSequenceLock.IsLocked && !IsBattleEnded && !IsChoosingEnemyHand && !IsPlayerDecisionOpen && isPlayerTurnActive && !isEndingTurn && !resolvingPlayerCardAttack && !waitingBlockSelect && !CardPlayPresentation.IsPlaying && !AttackMotionPresentation.IsPlaying &&
            (handDealer==null||(!HandDealer.IsRedrawSelecting&&!handDealer.IsHandLimitSelecting&&!handDealer.IsTurnDrawAnimating&&!handDealer.IsWallPlacementRunning&&!handDealer.IsResolvingWallAttack&&!handDealer.IsResolvingEnemyWallAttack)) &&
            (CardEffectManager.I==null||!CardEffectManager.I.IsResolvingNine) && !IsSelectingDestroyTarget() && card!=null && playerBattleArea!=null && card.transform.parent==playerBattleArea && card.activeInHierarchy && CardActionIcon.CanAttack(card.GetComponent<CardController>());
    }
    public List<GameObject> LiveAttackWalls()
    {
        var result=new List<GameObject>();if(handDealer==null||handDealer.enemyWallArea==null)return result;
        foreach(Transform t in handDealer.enemyWallArea)if(IsLiveAttackWall(t.gameObject))result.Add(t.gameObject);return result;
    }
    public bool IsLiveAttackWall(GameObject wall)
    {
        if(wall==null||!wall.activeInHierarchy||handDealer==null||handDealer.enemyWallArea==null||wall.transform.parent!=handDealer.enemyWallArea)return false;
        var card=wall.GetComponent<CardController>();var cg=wall.GetComponent<CanvasGroup>();return card!=null&&card.data!=null&&(cg==null||cg.alpha>.01f);
    }
    public bool CanChooseEnemyAttackTarget(CardController card)
    {
        return AttackSelectionActive&&!choosingMonarchWalls&&card!=null&&card.data!=null&&card.gameObject.activeInHierarchy&&card.transform.parent==enemyBattleArea&&card.isTapped;
    }
    public int RequiredAttackWallCount=>Mathf.Min(IsMonarchAttacker?2:1,LiveAttackWalls().Count);
    public bool IsAttackWallSelected(GameObject card)=>pendingWallTargets.Contains(card);
    public int SelectedAttackWallCount=>pendingWallTargets.Count;
    public void BeginMonarchWallChoice()
    {
        if(!AttackSelectionActive||!IsMonarchAttacker||LiveAttackWalls().Count<2)return;
        choosingMonarchWalls=true;pendingWallTargets.Clear();wallBreakCount=0;
        if(attackSelectionPresentation!=null)attackSelectionPresentation.Refresh();
    }
    public void CancelAttackSelection(){ClearPendingAttackConfirmation();choosingMonarchWalls=false;CancelTimedAttackSelection();if(attackSelectionPresentation!=null)attackSelectionPresentation.Refresh();}
    public void ConfirmMonarchWallChoice()
    {
        if (ChoosingMonarchWalls) ConfirmAttackSelection();
    }
    void ToggleMonarchWallTarget(GameObject target)
    {
        pendingWallTargets.RemoveAll(wall => !IsLiveAttackWall(wall));
        int required = RequiredAttackWallCount;
        if (!pendingWallTargets.Remove(target))
        {
            if (pendingWallTargets.Count >= required) return;
            pendingWallTargets.Add(target);
        }

        ClearPendingAttackConfirmation();
        wallBreakCount = pendingWallTargets.Count;
        if (wallBreakCount == required)
            CommitSelectedWallAttack(new List<GameObject>(pendingWallTargets));
        else if (attackSelectionPresentation != null)
            attackSelectionPresentation.Refresh();
    }
    void CommitSelectedWallAttack(List<GameObject> walls)
    {
        if(!AttackSelectionActive||handDealer==null||walls.Count==0||walls.Count!=RequiredAttackWallCount)return;
        var unique=new HashSet<GameObject>();foreach(var wall in walls)if(!IsLiveAttackWall(wall)||!unique.Add(wall))return;
        var kind = IsMonarchAttacker ? AttackConfirmationKind.MonarchWall : AttackConfirmationKind.Wall;
        string label = walls.Count == 1 ? "Attack this Wall card?" : $"Attack these {walls.Count} Wall cards?";
        if(RequestAttackConfirmation(kind,label,()=>CommitSelectedWallAttack(walls),walls.ToArray()))return;
        var attacker=currentAttacker.GetComponent<CardController>();var blocker=SelectEnemyBlocker(attacker);
        StartPlayerBattlePhase();NotifyPlayerActionCommitted();CancelAttackSelection();
        attacker.Tap();attacker.SetAttackable(false);
        StartCoroutine(PlayerWallAttackMotion(attacker,walls,blocker));
    }
    void SyncAttackSelectionPresentation()
    {
        if(selectingTarget&&!AttackSelectionActive&&!IsPlayerDecisionOpen)CancelAttackSelection();
        if(!selectingTarget){ClearPendingAttackConfirmation();choosingMonarchWalls=false;pendingWallTargets.Clear();}
        if(attackSelectionPresentation==null){attackSelectionPresentation=GetComponent<AttackSelectionPresentation>();if(attackSelectionPresentation==null)attackSelectionPresentation=gameObject.AddComponent<AttackSelectionPresentation>();attackSelectionPresentation.Initialize(this);}
    }
}
