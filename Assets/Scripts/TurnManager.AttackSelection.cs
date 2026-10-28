using UnityEngine;
using System.Collections.Generic;
public partial class TurnManager
{
    bool choosingMonarchWalls;
    System.Action pendingAttackConfirmation;
    readonly List<GameObject> pendingAttackTargets=new List<GameObject>();
    public bool AttackExecutionConfirmationActive=>HasPendingAttackConfirmation||(ChoosingMonarchWalls&&RequiredAttackWallCount>0&&SelectedAttackWallCount==RequiredAttackWallCount);
    public bool IsAttackConfirmationTarget(GameObject target)=>pendingAttackTargets.Contains(target)||(ChoosingMonarchWalls&&SelectedAttackWallCount==RequiredAttackWallCount&&pendingWallTargets.Contains(target));
    bool attackConfirmationApproved;
    public bool HasPendingAttackConfirmation=>pendingAttackConfirmation!=null;
    public string AttackConfirmationLabel {get;private set;}
    void ClearPendingAttackConfirmation(){pendingAttackConfirmation=null;pendingAttackTargets.Clear();AttackConfirmationLabel=null;}
    bool RequestAttackConfirmation(string label,System.Action execute,params GameObject[] targets)
    {
        if(attackConfirmationApproved)return false;
        pendingAttackConfirmation=execute;pendingAttackTargets.Clear();pendingAttackTargets.AddRange(targets);AttackConfirmationLabel=label;
        if(attackSelectionPresentation!=null)attackSelectionPresentation.Refresh();
        return true;
    }
    public void ConfirmAttackSelection()
    {
        if(!AttackSelectionActive)return;
        if(pendingAttackConfirmation==null){ConfirmMonarchWallChoice();return;}
        var execute=pendingAttackConfirmation;ClearPendingAttackConfirmation();
        attackConfirmationApproved=true;
        try{execute();}finally{attackConfirmationApproved=false;}
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
        return !IsBattleEnded && !IsChoosingEnemyHand && !IsPlayerDecisionOpen && isPlayerTurnActive && !isEndingTurn && !resolvingPlayerCardAttack && !waitingBlockSelect && !CardPlayPresentation.IsPlaying &&
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
        if(!ChoosingMonarchWalls)return;
        pendingWallTargets.RemoveAll(w=>!IsLiveAttackWall(w));
        if(RequiredAttackWallCount==0||pendingWallTargets.Count!=RequiredAttackWallCount)return;
        // The two-wall confirmation button is already the final confirmation.
        attackConfirmationApproved=true;
        try{CommitSelectedWallAttack(new List<GameObject>(pendingWallTargets));}
        finally{attackConfirmationApproved=false;}
    }
    void CommitSelectedWallAttack(List<GameObject> walls)
    {
        if(!AttackSelectionActive||handDealer==null||walls.Count==0||walls.Count!=RequiredAttackWallCount)return;
        var unique=new HashSet<GameObject>();foreach(var wall in walls)if(!IsLiveAttackWall(wall)||!unique.Add(wall))return;
        if(RequestAttackConfirmation("BREAK "+walls.Count+"  —  CONFIRM",()=>CommitSelectedWallAttack(walls),walls.ToArray()))return;
        var attacker=currentAttacker.GetComponent<CardController>();var blocker=SelectEnemyBlocker(attacker);
        StartPlayerBattlePhase();NotifyPlayerActionCommitted();CancelAttackSelection();
        attacker.Tap();attacker.SetAttackable(false);
        if(blocker!=null){ResolveCardBattle(attacker,blocker);return;}
        // Start both existing wall slash/recovery routines in this frame, after one confirmation.
        foreach(var wall in walls)handDealer.DamageEnemyWallFromAttack(wall);
    }
    void SyncAttackSelectionPresentation()
    {
        if(selectingTarget&&!AttackSelectionActive&&!IsPlayerDecisionOpen)CancelAttackSelection();
        if(!selectingTarget){ClearPendingAttackConfirmation();choosingMonarchWalls=false;pendingWallTargets.Clear();}
        if(attackSelectionPresentation==null){attackSelectionPresentation=GetComponent<AttackSelectionPresentation>();if(attackSelectionPresentation==null)attackSelectionPresentation=gameObject.AddComponent<AttackSelectionPresentation>();attackSelectionPresentation.Initialize(this);}
    }
}
