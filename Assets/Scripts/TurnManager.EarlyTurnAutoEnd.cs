using UnityEngine;

public partial class TurnManager
{
    const int EarlyAutoEndLastTurn = 3;
    // Once an affordable card has been available, or the player has used End Turn,
    // leave the rest of this turn under the player's control.
    bool earlyTurnAutoEndSuppressed;

    ResourceManager PlayerTurnResources()
    {
        if (resourcePhaseManager != null && resourcePhaseManager.resourceManager != null)
            return resourcePhaseManager.resourceManager;
        return FindFirstObjectByType<ResourceManager>();
    }

    void ObserveEarlyTurnHand()
    {
        if (earlyTurnAutoEndSuppressed || turnCount < 1 || turnCount > EarlyAutoEndLastTurn ||
            !isPlayerTurnActive || isEndingTurn || IsBattleEnded || handDealer == null || handDealer.handArea == null)
            return;
        var resources = PlayerTurnResources();
        if (resources == null) return; // Missing state never proves that no action is available.
        foreach (Transform child in handDealer.handArea)
        {
            if (!child.gameObject.activeInHierarchy) continue;
            var card = child.GetComponent<CardController>();
            if (card != null && card.data != null && card.data.cost <= resources.MaxResource)
            {
                earlyTurnAutoEndSuppressed = true;
                return;
            }
        }
    }

    bool HasMovingPlayerCard()
    {
        // A dragged card can temporarily live outside the hand hierarchy.
        foreach (var drag in FindObjectsByType<CardDrag>(FindObjectsSortMode.None))
            if (drag != null && (drag.IsDragging || drag.IsDropSnapPlaying)) return true;
        return false;
    }

    bool CanBeginPlayerTurnEnd()
    {
        return isPlayerTurnActive && !isEndingTurn && !isFirstTurnStarting && !IsBattleEnded &&
            !PresentationSequenceLock.IsLocked && !TurnSignalPresentation.IsPlaying &&
            !CardPlayPresentation.IsPlaying && !AttackMotionPresentation.IsPlaying &&
            !IsResolvingActCard && !resolvingPlayerCardAttack && !resolvingEnemyBlock &&
            !waitingBlockSelect && !IsChoosingEnemyHand && !IsPlayerDecisionOpen &&
            !HandDealer.IsRedrawSelecting &&
            (ReverseChoiceManager.I == null || !ReverseChoiceManager.I.IsChoosing) &&
            (resourcePhaseManager == null || !resourcePhaseManager.IsResolvingCharge) &&
            (CardEffectManager.I == null || (!CardEffectManager.I.IsResolvingNine && !CardEffectManager.I.IsResolvingSummon)) &&
            (handDealer == null || (!handDealer.IsTurnDrawAnimating && !handDealer.IsHandLimitSelecting &&
                !handDealer.IsWallPlacementRunning && !handDealer.IsResolvingWallAttack && !handDealer.IsResolvingEnemyWallAttack));
    }

    bool ShouldAutoEndEarlyTurn()
    {
        ObserveEarlyTurnHand();
        if (turnCount < 1 || turnCount > EarlyAutoEndLastTurn || earlyTurnAutoEndSuppressed || !CanBeginPlayerTurnEnd())
            return false;
        if ((!isPlayerMainPhase && !isBattlePhase) ||
            handDealer == null || handDealer.handArea == null || playerBattleArea == null || PlayerTurnResources() == null ||
            (resourcePhaseManager != null && resourcePhaseManager.IsRunning()) ||
            IsSelectingTarget() || IsSelectingDestroyTarget() || HasPendingAttackConfirmation || pendingJokerEffectCard != null ||
            HasMovingPlayerCard()) return false;
        // Keep actual legal actions, including discounted K/free-play and attacks,
        // in addition to the unconditional maximum-resource hand protection above.
        return !CanPlayerSummonAnything() && !CanPlayerAttackAnything();
    }

    bool TryAutoEndEarlyTurn()
    {
        if (!ShouldAutoEndEarlyTurn()) return false;
        Debug.Log("最初の3ターン・実行可能な行動がないため自動ターン終了");
        return CommitPlayerTurnEnd();
    }

    bool CommitPlayerTurnEnd()
    {
        if (!CanBeginPlayerTurnEnd() || HasMovingPlayerCard()) return false;
        // Claim the transition before any phase callback/coroutine can re-enter.
        isPlayerTurnActive = false;
        isEndingTurn = true;
        lowerPlayerHandForOpponent = !playerExtraTurnRequested;
        SyncPlayerHandTurnPosition();
        isPlayerMainPhase = false;
        mainTurnCountdown.Reset();
        decisionCountdown.Reset();
        if (decisionTimerPanel != null) decisionTimerPanel.gameObject.SetActive(false);
        if (resourcePhaseManager != null) resourcePhaseManager.EndResourcePhase();
        if (mainPhaseManager != null) mainPhaseManager.EndMainPhase();
        SetEndTurnButton(false);
        StartCoroutine(EndPlayerTurnRoutine());
        return true;
    }
}
