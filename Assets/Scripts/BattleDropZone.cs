using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;

public class BattleDropZone :
    MonoBehaviour,
    IDropHandler
{
    const string RaiseConfirmation = "Raise this King?";
    public Transform battleArea;

    public FieldDropPresentation FieldPresentation {get;private set;}
    void Awake()
    {
        if(battleArea==null)battleArea=transform;
        FieldPresentation=GetComponent<FieldDropPresentation>();if(FieldPresentation==null)FieldPresentation=gameObject.AddComponent<FieldDropPresentation>();
        FieldPresentation.Initialize(this);
    }
    public bool CanHighlightCard(CardController card)
    {
        if (card == null || card.data == null || battleArea == null || CardPlayPresentation.IsPlaying) return false;
        var decisionManager = FindFirstObjectByType<TurnManager>();
        if (decisionManager != null && !decisionManager.CanDragPlayerHand) return false;
        var cardDrag = card.GetComponent<CardDrag>();
        if (cardDrag == null || cardDrag.IsDropSnapPlaying) return false;

        return CanPlayCardByRules(card);
    }

    // Rule availability must also work between Resource and Main, when input is locked.
    public bool CanPlayCardByRules(CardController card)
    {
        if (card == null || card.data == null || battleArea == null) return false;
        // Match the normal drop path, including the best eligible K base and its discount.
        card.data.SetPowerFromName();
        SetCardCost(card);
        var resources = FindFirstObjectByType<ResourceManager>();
        int mana = useCostCheck ? (resources != null ? resources.currentResource : -1) : int.MaxValue;
        if (mana >= card.data.cost && (BattleAreaLayout.IsSpell(card.data) || BattleAreaLayout.CanAccept(battleArea))) return true;
        if (!IsSpecialSummonK(card) || !HasRaisesRemaining()) return false;
        foreach (Transform child in battleArea)
            if (CanRaiseOn(card, child.GetComponent<CardController>(), mana)) return true;
        return false;
    }

    [Header("Cost Debug")]
    public bool useCostCheck = true;

    public void OnDrop(
        PointerEventData eventData
    )
    {
        Debug.Log(
            "BattleDropZone OnDrop 呼ばれた"
        );

        if (eventData == null || eventData.pointerDrag == null)
        {
            Debug.Log(
                "ドラッグ中のオブジェクトがない"
            );
            return;
        }

        CardDrag cardDrag =
            eventData.pointerDrag
                .GetComponent<CardDrag>();

        if (cardDrag == null)
        {
            Debug.Log(
                "CardDrag が見つからない"
            );
            return;
        }

        if (!cardDrag.IsDragging)
    return;

        CardController card =
            eventData.pointerDrag
                .GetComponent<CardController>();

        if (
            card == null ||
            card.data == null
        )
        {
            Debug.Log(
                "CardController またはCardDataがない"
            );
            return;
        }

        if (battleArea == null)
            battleArea = transform;

        CardController selectedBaseCard = FindRaiseTarget(card, eventData.position, eventData.pressEventCamera);
        if (selectedBaseCard == null && FieldPresentation != null && !FieldPresentation.Contains(eventData.position)) return;
        FieldDropPresentation.EndDrag();
        TryPlayConfirmedCard(cardDrag, selectedBaseCard);
    }

    void TryPlayConfirmedCard(CardDrag cardDrag, CardController selectedBaseCard)
    {
        if (cardDrag == null || CardPlayPresentation.IsPlaying || cardDrag.IsDropSnapPlaying) return;
        TurnManager decisionManager = FindFirstObjectByType<TurnManager>();
        if (decisionManager != null && !decisionManager.CanDragPlayerHand) return;
        CardController card = cardDrag.GetComponent<CardController>();
        if (card == null || card.data == null) return;
        if (decisionManager != null && decisionManager.handDealer != null &&
            !cardDrag.IsDragging && !cardDrag.IsAwaitingPlayConfirmation && !card.transform.IsChildOf(decisionManager.handDealer.handArea)) return;
        card.data.SetPowerFromName();
        SetCardCost(card);
        int summonCost = card.data.cost;
        if (selectedBaseCard != null)
        {
            if (decisionManager?.handDealer == null || !HasRaisesRemaining() ||
                !CanRaiseOn(card, selectedBaseCard, int.MaxValue)) return;
            summonCost = Mathf.Max(0, card.data.cost - selectedBaseCard.data.power);
        }
        if(!BattleAreaLayout.IsSpell(card.data) && !BattleAreaLayout.CanAccept(battleArea, selectedBaseCard))
        {
            Debug.Log("バトルエリア上限6枚：手札へ戻します");
            return; // OnEndDrag restores the rejected card; no resource is spent.
        }

        if (useCostCheck)
        {
            ResourceManager resourceManager =
                FindFirstObjectByType<ResourceManager>();

            if (resourceManager == null)
            {
                Debug.LogWarning(
                    "ResourceManagerが見つからない"
                );
                return;
            }

            if (
                resourceManager.currentResource <
                summonCost
            )
            {
                Debug.Log(
                    "リソース不足：必要 " +
                    summonCost +
                    " / 現在 " +
                    resourceManager.currentResource
                );
                return;
            }

            if (RequestPlayConfirmation(decisionManager, card, cardDrag, selectedBaseCard)) return;
            if (selectedBaseCard != null && !decisionManager.handDealer.SpendRaise(false)) return;
            resourceManager.UseResource(
                summonCost
            );

            Debug.Log(
                "召喚コスト支払い：" +
                summonCost
            );
        }
        else
        {
            Debug.Log(
                "コストチェックOFF：無料召喚"
            );
        }

        /*
         * 手札からカードを取り出し、
         * バトルエリアへ移動する。
         *
         * AはTurnManagerが親Transformから
         * 所属陣営を判定するため、
         * 対象選択完了までは
         * playerBattleAreaの子として維持する。
         */
        if (!useCostCheck)
        {
            if (RequestPlayConfirmation(decisionManager, card, cardDrag, selectedBaseCard)) return;
            if (selectedBaseCard != null && !decisionManager.handDealer.SpendRaise(false)) return;
        }
        if (decisionManager != null) decisionManager.NotifyPlayerActionCommitted();
        if(CardPlaySettings.Load().enabledEffect)
        {
            StartCoroutine(PlayCommittedCard(card,cardDrag,selectedBaseCard));
            return;
        }
        FinishPlayingCard(card,cardDrag,selectedBaseCard,true);
    }

    bool RequestPlayConfirmation(TurnManager manager, CardController card, CardDrag drag, CardController basis)
    {
        bool isRaise = basis != null;
        string message = isRaise ? RaiseConfirmation : "Play " + (card.data.cardName ?? "this card").Replace('_', ' ') + "?";
        return manager != null && manager.RequestPlayerDecision("summon:" + card.GetInstanceID(), message,
            () =>
            {
                if (drag == null) return;
                try
                {
                    if (!isRaise || basis != null) TryPlayConfirmedCard(drag, basis);
                }
                finally { if (drag != null) drag.CancelPlayConfirmation(); }
            },
            () => { if (drag != null) drag.CancelPlayConfirmation(); },
            drag.HoldForPlayConfirmation, focusCard: card, secondFocusCard: basis,
            focusColor: isRaise ? FieldDropPresentation.RaiseViolet : CardPlaySettings.Load().Highlight(false),
            focusStrength: isRaise ? FieldDropPresentation.RaiseKingGlowStrength : 1f);
    }

    IEnumerator PlayCommittedCard(CardController card,CardDrag cardDrag,CardController selectedBaseCard)
    {
        cardDrag.BeginPlayPresentation();
        try{yield return CardPlayPresentation.Play(card.data,battleArea,selectedBaseCard);}
        finally{if(cardDrag!=null)cardDrag.EndPlayPresentation();}
        if(card!=null&&cardDrag!=null&&battleArea!=null)FinishPlayingCard(card,cardDrag,selectedBaseCard,false);
    }

    void FinishPlayingCard(CardController card,CardDrag cardDrag,CardController selectedBaseCard,bool playSnap)
    {
        cardDrag.DropToBattleArea(battleArea,playSnap && selectedBaseCard == null);

        RectTransform rt =
            card.GetComponent<RectTransform>();

        if (rt != null)
        {
            rt.localRotation =
                Quaternion.identity;
        }

        /*
         * A：
         * 対象選択中は非表示。
         * 対象選択完了後にTurnManagerが
         * A自身を墓地へ送る。
         */
        if (IsAceActionCard(card))
        {
            ResolveAceAction(
                card,
                cardDrag
            );
            return;
        }

        /*
         * 9：
         * 敵全体タップ後に
         * 即座に墓地へ送る。
         */
        if (IsNineActionCard(card))
        {
            ResolveNineAction(
                card,
                cardDrag
            );
            return;
        }

        /*
         * 通常カードの召喚処理。
         */
        bool isRaisedKing =
            selectedBaseCard != null;

        if (isRaisedKing)
        {
            card.transform.SetSiblingIndex(selectedBaseCard.transform.GetSiblingIndex());
            StackBaseCardUnderK(
                card,
                selectedBaseCard
            );
        }

        RefreshBattleLayout();

        Debug.Log(
            "カードをバトルエリアに出した"
        );

        card.BeginNormalPlay();
        bool noSummonSickness =
            HasCardEffect(
                card,
                EffectType.NoSummonSickness
            );

        if (
            isRaisedKing ||
            noSummonSickness
        )
        {
            card.SetSummonSickness(false);
            card.Untap();

            CanvasGroup raisedCg =
                card.GetComponent<CanvasGroup>();

            if (raisedCg != null)
            {
                raisedCg.alpha = 1f;
                raisedCg.blocksRaycasts = true;
                raisedCg.interactable = true;
            }

            if (isRaisedKing)
            {
                Debug.Log(
                    "K特殊召喚：" +
                    "召喚酔いなし・透明度を復元"
                );
            }
        }
        else
        {
            card.SetSummonSickness(true);
            card.SetAttackable(false);
        }

        if(!playSnap || isRaisedKing)
        {
            var layout=battleArea.GetComponent<BattleAreaLayout>();if(layout!=null)layout.PlaceImmediately(card);
            cardDrag.EnablePlacedCardInput();
        }
        ActivateSummonEffect(card);

        StartCoroutine(
            CompleteSummonAfterSnap(
                cardDrag
            )
        );
    }

    void SetCardCost(
        CardController card
    )
    {
        if (
            card == null ||
            card.data == null
        )
        {
            return;
        }

        if (
            !string.IsNullOrEmpty(
                card.data.name
            ) &&
            card.data.name.Contains("Joker")
        )
        {
            card.data.cost = 13;
        }
        else if (card.data.power == 1)
        {
            card.data.cost = 4;
        }
        else
        {
            card.data.cost =
                card.data.power;
        }
    }

    void ResolveAceAction(
        CardController card,
        CardDrag cardDrag
    )
    {
        if (
            card == null ||
            card.data == null
        )
        {
            return;
        }

        Debug.Log(
            "Aをプレイ：" +
            "対象選択完了まで墓地移動を保留"
        );

        card.SetAttackable(false);

        /*
         * CardEffectManagerから
         * TurnManager.StartSelectEnemyBattleToDestroy()
         * が呼ばれ、対象選択状態になる。
         */
        ActivateSummonEffect(card);

        ActCardPresentation.HideOnField(card);

        RefreshBattleLayout();

        /*
         * Aの墓地移動は行わない。
         * TurnManager.TrySelectDestroyTarget()が
         * 対象破壊後にA自身を墓地へ送る。
         */
        StartCoroutine(
            CompleteSummonAfterSnap(
                cardDrag
            )
        );
    }

    void ResolveNineAction(
        CardController card,
        CardDrag cardDrag
    )
    {
        if (
            card == null ||
            card.data == null
        )
        {
            return;
        }

        Debug.Log(
            "9をプレイ：" +
            "敵全体タップ後に墓地へ移動"
        );

        /*
         * 9がplayerBattleAreaの子である間に
         * 効果を発動することで、
         * TurnManagerが敵側を正しく判定できる。
         */
        ActivateSummonEffect(card);
        // The shared Nine sequence sends the spell to the graveyard after its cue.
        RefreshBattleLayout();

        StartCoroutine(
            CompleteSummonAfterSnap(
                cardDrag
            )
        );
    }

    void ActivateSummonEffect(
        CardController card
    )
    {
        if (
            card == null ||
            card.data == null
        )
        {
            return;
        }

        if (CardEffectManager.I != null)
        {
            CardEffectManager.I
                .ActivateOnSummon(card);

            Debug.Log(
                "召喚時効果発動：" +
                card.data.name
            );
        }
        else
        {
            Debug.LogWarning(
                "CardEffectManagerが未配置"
            );
        }
    }

    bool IsAceActionCard(
        CardController card
    )
    {
        return HasCardEffect(
            card,
            EffectType.DestroyOneEnemyBattle
        );
    }

    bool IsNineActionCard(
        CardController card
    )
    {
        return HasCardEffect(
            card,
            EffectType.TapAllEnemyBattle
        );
    }

    bool HasCardEffect(
        CardController card,
        EffectType effectType
    )
    {
        if (
            card == null ||
            card.data == null ||
            card.data.effectTypes == null
        )
        {
            return false;
        }

        return System.Array.Exists(
            card.data.effectTypes,
            effect => effect == effectType
        );
    }

    void RefreshBattleLayout()
    {
        if (battleArea == null)
            return;

        BattleAreaLayout layout =
            battleArea
                .GetComponent<BattleAreaLayout>();

        if (layout != null)
            layout.Refresh();
    }

IEnumerator CompleteSummonAfterSnap(CardDrag cardDrag)
{
    ResourcePhaseManager rpm =
        FindFirstObjectByType<ResourcePhaseManager>();

    // Mainでの召喚なら、フェイズを開始し直さない。
    bool summonedDuringResource =
        rpm != null && rpm.IsRunning();

    while (cardDrag != null && cardDrag.IsDropSnapPlaying)
    {
        yield return null;
    }

    if (!summonedDuringResource ||
        rpm == null ||
        !rpm.IsRunning())
    {
        yield break;
    }

    while(PresentationSequenceLock.IsLocked || (CardEffectManager.I != null && (CardEffectManager.I.IsResolvingNine || CardEffectManager.I.IsResolvingSummon))) yield return null;
    rpm.EndResourcePhase();

    TurnManager turnManager =
        FindFirstObjectByType<TurnManager>();

    if (turnManager != null)
        turnManager.OnResourcePhaseComplete();
}

    bool IsSpecialSummonK(
        CardController card
    )
    {
        return HasCardEffect(
            card,
            EffectType.SpecialSummonK
        );
    }

    bool HasRaisesRemaining()
    {
        var manager = FindFirstObjectByType<TurnManager>();
        return manager != null && manager.handDealer != null && manager.handDealer.PlayerRaisesRemaining > 0;
    }

    internal bool CanPlayNormally(CardController card)
    {
        if (card == null || card.data == null ||
            (!BattleAreaLayout.IsSpell(card.data) && !BattleAreaLayout.CanAccept(battleArea))) return false;
        var resources = FindFirstObjectByType<ResourceManager>();
        return !useCostCheck || (resources != null && resources.currentResource >= card.data.cost);
    }

    bool CanRaiseOn(CardController king, CardController basis, int mana)
    {
        if (!IsSpecialSummonK(king) || basis == null || basis.data == null ||
            basis.transform.parent != battleArea || !basis.gameObject.activeInHierarchy || BattleAreaLayout.IsSpell(basis.data))
            return false;
        basis.data.SetPowerFromName();
        int power = basis.data.power;
        return (power == 7 || power == 8 || power == 10) && mana >= Mathf.Max(0, king.data.cost - power)
            && BattleAreaLayout.CanAccept(battleArea, basis);
    }

    internal CardController FindRaiseTarget(CardController king, Vector2 pointer, Camera camera)
    {
        if (battleArea == null || !IsSpecialSummonK(king) || !HasRaisesRemaining()) return null;
        king.data.SetPowerFromName();
        SetCardCost(king);
        var resources = FindFirstObjectByType<ResourceManager>();
        int mana = useCostCheck ? (resources != null ? resources.currentResource : -1) : int.MaxValue;
        for (int i = battleArea.childCount - 1; i >= 0; i--)
        {
            var basis = battleArea.GetChild(i).GetComponent<CardController>();
            if (!CanRaiseOn(king, basis, mana)) continue;
            var artwork = basis.artworkImage != null ? basis.artworkImage.rectTransform : basis.transform as RectTransform;
            if (artwork != null && RectTransformUtility.RectangleContainsScreenPoint(artwork, pointer, camera))
                return basis;
        }
        return null;
    }
    void StackBaseCardUnderK(
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
}
