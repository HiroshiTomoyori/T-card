using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class BattleDropZone :
    MonoBehaviour,
    IDropHandler
{
    public Transform battleArea;

    [Header("Cost Debug")]
    public bool useCostCheck = true;

    public void OnDrop(
        PointerEventData eventData
    )
    {
        Debug.Log(
            "BattleDropZone OnDrop 呼ばれた"
        );

        if (eventData.pointerDrag == null)
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

        card.data.SetPowerFromName();

        SetCardCost(card);

        int summonCost = card.data.cost;

        CardController selectedBaseCard = null;

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

            CardController baseCard =
                FindKSpecialSummonBase();

            if (
                IsSpecialSummonK(card) &&
                baseCard != null
            )
            {
                selectedBaseCard = baseCard;

                baseCard.data.SetPowerFromName();

                summonCost =
                    card.data.cost -
                    baseCard.data.power;

                if (summonCost < 0)
                    summonCost = 0;

                Debug.Log(
                    "K特殊召喚：土台 " +
                    baseCard.data.name +
                    " / 差分コスト " +
                    summonCost
                );
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
        cardDrag.DropToBattleArea(
            battleArea
        );

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
            StackBaseCardUnderK(
                card,
                selectedBaseCard
            );
        }

        RefreshBattleLayout();

        Debug.Log(
            "カードをバトルエリアに出した"
        );

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

        /*
         * DropToBattleAreaのスナップ演出が
         * CanvasGroup.alphaを戻す可能性があるため、
         * 対象選択中は毎フレーム非表示を維持する。
         */
        StartCoroutine(
            HideAceWhileSelecting(card)
        );

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

    IEnumerator HideAceWhileSelecting(
        CardController aceCard
    )
    {
        if (aceCard == null)
            yield break;

        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if (turnManager == null)
        {
            Debug.LogWarning(
                "A非表示処理：" +
                "TurnManagerが見つかりません"
            );
            yield break;
        }

        CanvasGroup canvasGroup =
            aceCard.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                aceCard.gameObject
                    .AddComponent<CanvasGroup>();
        }

        while (
            aceCard != null &&
            battleArea != null &&
            turnManager
                .IsSelectingDestroyTarget() &&
            aceCard.transform
                .IsChildOf(battleArea)
        )
        {
            SetAceHidden(canvasGroup);

            /*
             * CardDragやAnimatorによる
             * 表示更新の後に再度非表示にする。
             */
            yield return new WaitForEndOfFrame();

            if (
                aceCard != null &&
                battleArea != null &&
                turnManager
                    .IsSelectingDestroyTarget() &&
                aceCard.transform
                    .IsChildOf(battleArea)
            )
            {
                SetAceHidden(canvasGroup);
            }
        }

        /*
         * Aが墓地へ移動した場合、
         * TurnManager.SendToGraveyard()が
         * alphaを0.45へ設定する。
         *
         * ここではalphaを変更しない。
         */
        Debug.Log(
            "Aの対象選択終了：" +
            "非表示維持を終了"
        );
    }

    void SetAceHidden(
        CanvasGroup canvasGroup
    )
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
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

        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if (turnManager != null)
        {
            turnManager
                .SendCardToOwnGraveyard(card);

            Debug.Log(
                "9を墓地へ移動：" +
                card.data.name
            );
        }
        else
        {
            Debug.LogWarning(
                "9を墓地へ送れません：" +
                "TurnManagerが見つかりません"
            );
        }

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

    IEnumerator CompleteSummonAfterSnap(
        CardDrag cardDrag
    )
    {
        while (
            cardDrag != null &&
            cardDrag.IsDropSnapPlaying
        )
        {
            yield return null;
        }

        ResourcePhaseManager
            resourcePhaseManager =
                FindFirstObjectByType
                    <ResourcePhaseManager>();

        if (resourcePhaseManager != null)
        {
            resourcePhaseManager
                .EndResourcePhase();
        }

        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if (turnManager != null)
        {
            turnManager
                .OnResourcePhaseComplete();
        }
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

    CardController FindKSpecialSummonBase()
    {
        if (battleArea == null)
            return null;

        for (
            int i = 0;
            i < battleArea.childCount;
            i++
        )
        {
            CardController card =
                battleArea
                    .GetChild(i)
                    .GetComponent<CardController>();

            if (
                card == null ||
                card.data == null
            )
            {
                continue;
            }

            card.data.SetPowerFromName();

            if (
                card.data.power == 7 ||
                card.data.power == 8 ||
                card.data.power == 10
            )
            {
                return card;
            }
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

        baseCard.transform.SetParent(
            kCard.transform,
            false
        );

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