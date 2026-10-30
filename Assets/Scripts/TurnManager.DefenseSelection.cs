using UnityEngine;
using System.Collections.Generic;
public partial class TurnManager
{
    readonly List<GameObject> pendingEnemyWallTargets=new List<GameObject>();
    System.Action pendingDefenseConfirmation;
    CardController defenseContextAttacker;
    CardController selectedDefenseBlocker;
    bool defenseConfirmationApproved;
    DefenseSelectionPresentation defenseSelectionPresentation;
    public bool DefenseSelectionActive=>!IsBattleEnded&&waitingBlockSelect&&!resolvingEnemyBlock&&pendingEnemyAttacker!=null;
    public CardController IncomingAttacker=>pendingEnemyAttacker;
    public CardController IncomingFieldTarget=>pendingEnemyRestedTarget;
    public bool IncomingMonarchAttack=>pendingEnemyAttacker!=null&&IsMonarchCard(pendingEnemyAttacker.data);
    public bool HasPendingDefenseConfirmation=>pendingDefenseConfirmation!=null;
    public CardController SelectedDefenseBlocker=>selectedDefenseBlocker;
    public string DefenseConfirmationLabel=>selectedDefenseBlocker!=null?GetCardName(selectedDefenseBlocker.data)+"で防御しますか？":"攻撃を通しますか？";
    public bool CanChooseDefenseCard(CardController card)=>!PresentationSequenceLock.IsLocked&&DefenseSelectionActive&&card!=null&&card.gameObject.activeInHierarchy&&card.transform.parent==playerBattleArea&&CardActionIcon.CanBlock(card);
    public List<GameObject> IncomingWallTargets()
    {
        var result=new List<GameObject>();
        if(pendingEnemyTargetWall==null)return result;
        foreach(var wall in pendingEnemyWallTargets)if(IsLivePlayerWall(wall))result.Add(wall);
        if(result.Count==0&&IsLivePlayerWall(pendingEnemyTargetWall))result.Add(pendingEnemyTargetWall);
        return result;
    }
    bool IsLivePlayerWall(GameObject wall){if(wall==null||!wall.activeInHierarchy||wall.transform.parent!=playerWallArea)return false;var cg=wall.GetComponent<CanvasGroup>();return wall.GetComponent<CardController>()!=null&&(cg==null||cg.alpha>.01f);}
    void PrepareIncomingWallTargets(CardController attacker,GameObject primary,List<RectTransform> candidates)
    {
        pendingEnemyWallTargets.Clear();if(IsLivePlayerWall(primary))pendingEnemyWallTargets.Add(primary);
        if(attacker!=null&&IsMonarchCard(attacker.data))foreach(var candidate in candidates)if(candidate!=null&&candidate.gameObject!=primary&&IsLivePlayerWall(candidate.gameObject)){pendingEnemyWallTargets.Add(candidate.gameObject);break;}
    }
    bool RequestDefenseConfirmation(CardController blocker,System.Action execute)
    {
        // Existing inactivity resolution may pass automatically; manual decisions always confirm.
        if(defenseConfirmationApproved||automaticDecisionDepth>0)return false;
        pendingDefenseConfirmation=execute;selectedDefenseBlocker=blocker;defenseContextAttacker=pendingEnemyAttacker;
        SyncDefenseSelectionPresentation();return true;
    }
    public void CancelDefenseChoice(){pendingDefenseConfirmation=null;selectedDefenseBlocker=null;if(defenseSelectionPresentation!=null)defenseSelectionPresentation.Refresh();}
    public void ConfirmDefenseChoice()
    {
        if(!DefenseSelectionActive||pendingDefenseConfirmation==null)return;
        if(defenseContextAttacker!=pendingEnemyAttacker||(selectedDefenseBlocker!=null&&!CanChooseDefenseCard(selectedDefenseBlocker))){CancelDefenseChoice();return;}
        var execute=pendingDefenseConfirmation;pendingDefenseConfirmation=null;selectedDefenseBlocker=null;defenseConfirmationApproved=true;
        try{execute();}finally{defenseConfirmationApproved=false;}
        if(defenseSelectionPresentation!=null)defenseSelectionPresentation.Refresh();
    }
    void SyncDefenseSelectionPresentation()
    {
        if(DefenseSelectionActive&&!HasUntappedPlayerBlocker())RunAutomaticDecision(OnNoBlockButtonClicked);
        if(defenseContextAttacker!=pendingEnemyAttacker||!DefenseSelectionActive){pendingDefenseConfirmation=null;selectedDefenseBlocker=null;defenseContextAttacker=pendingEnemyAttacker;}
        if(defenseSelectionPresentation==null){defenseSelectionPresentation=GetComponent<DefenseSelectionPresentation>();if(defenseSelectionPresentation==null)defenseSelectionPresentation=gameObject.AddComponent<DefenseSelectionPresentation>();defenseSelectionPresentation.Initialize(this);}
    }
}
