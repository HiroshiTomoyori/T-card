using UnityEngine;
using UnityEngine.EventSystems;

public class BattleDropZone : MonoBehaviour, IDropHandler
{
    public Transform battleArea;

    [Header("Cost Debug")]
    public bool useCostCheck = true;

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("BattleDropZone OnDrop 呼ばれた");

        if(eventData.pointerDrag == null)
        {
            Debug.Log("ドラッグ中のオブジェクトがない");
            return;
        }

        CardDrag cardDrag =
            eventData.pointerDrag.GetComponent<CardDrag>();

        if(cardDrag == null)
        {
            Debug.Log("CardDrag が見つからない");
            return;
        }

        CardController card =
            eventData.pointerDrag.GetComponent<CardController>();

        if(card == null || card.data == null)
        {
            Debug.Log("CardController または CardData がない");
            return;
        }

        if(battleArea == null)
        {
            battleArea = transform;
        }

        card.data.SetPowerFromName();

        if(card.data.name.Contains("Joker"))
        {
            card.data.cost = 13;
        }
        else if(card.data.power == 1)
        {
            card.data.cost = 4;
        }
        else
        {
            card.data.cost = card.data.power;
        }

        int summonCost = card.data.cost;
        CardController selectedBaseCard = null;

        if(useCostCheck)
        {
            ResourceManager resourceManager =
                FindFirstObjectByType<ResourceManager>();

            if(resourceManager == null)
            {
                Debug.LogWarning(
                    "ResourceManager が見つからない"
                );

                return;
            }

            CardController baseCard =
                FindKSpecialSummonBase();

            if(IsSpecialSummonK(card) && baseCard != null)
            {
                selectedBaseCard = baseCard;

                baseCard.data.SetPowerFromName();

                int basePower =
                    baseCard.data.power;

                summonCost =
                    card.data.cost - basePower;

                if(summonCost < 0)
                {
                    summonCost = 0;
                }

                Debug.Log(
                    "K特殊召喚：土台 " +
                    baseCard.data.name +
                    " / 差分コスト " +
                    summonCost
                );
            }

            if(resourceManager.currentResource < summonCost)
            {
                Debug.Log(
                    "リソース不足：必要 " +
                    summonCost +
                    " / 現在 " +
                    resourceManager.currentResource
                );

                return;
            }

            resourceManager.UseResource(summonCost);

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

        // カードをバトルエリアへ移動
        cardDrag.DropToBattleArea(battleArea);

        // 手札の扇形回転をリセット
        RectTransform rt =
            card.GetComponent<RectTransform>();

        if(rt != null)
        {
            rt.localRotation =
                Quaternion.identity;
        }

        // K特殊召喚時、土台カードをKの下へ重ねる
        if(selectedBaseCard != null)
        {
            StackBaseCardUnderK(
                card,
                selectedBaseCard
            );
        }

        // バトルエリアを整列
        BattleAreaLayout layout =
            battleArea.GetComponent<BattleAreaLayout>();

        if(layout != null)
        {
            layout.Refresh();
        }

        Debug.Log(
            "カードをバトルエリアに出した"
        );

        // 召喚酔い判定
        bool noSummonSickness =
            card.data.effectTypes != null &&
            System.Array.Exists(
                card.data.effectTypes,
                x => x ==
                    EffectType.NoSummonSickness
            );

        if(noSummonSickness)
        {
            card.SetSummonSickness(false);
        }
        else
        {
            card.SetSummonSickness(true);
            card.SetAttackable(false);
        }

        // 召喚時効果
        if(CardEffectManager.I != null)
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
                "CardEffectManager が未配置"
            );
        }

        // 召喚完了後にリソースフェイズ終了
        ResourcePhaseManager resourcePhaseManager =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(resourcePhaseManager != null)
        {
            resourcePhaseManager
                .EndResourcePhase();
        }

        // メインフェイズ開始
        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if(turnManager != null)
        {
            turnManager
                .OnResourcePhaseComplete();
        }
    }

    bool IsSpecialSummonK(CardController card)
    {
        if(card == null || card.data == null)
            return false;

        if(card.data.effectTypes == null)
            return false;

        return System.Array.Exists(
            card.data.effectTypes,
            x => x == EffectType.SpecialSummonK
        );
    }

    CardController FindKSpecialSummonBase()
    {
        if(battleArea == null)
            return null;

        for(int i = 0; i < battleArea.childCount; i++)
        {
            CardController card =
                battleArea.GetChild(i)
                .GetComponent<CardController>();

            if(card == null || card.data == null)
                continue;

            card.data.SetPowerFromName();

            if(card.data.power == 7 ||
               card.data.power == 8 ||
               card.data.power == 10)
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
    if(kCard == null || baseCard == null)
        return;

    StackedCard stacked =
        kCard.GetComponent<StackedCard>();

    if(stacked == null)
        stacked =
            kCard.gameObject.AddComponent<StackedCard>();

    stacked.baseCard =
        baseCard.gameObject;
    // 追加
    CanvasGroup kCg =
        kCard.GetComponent<CanvasGroup>();

    if(kCg == null)
        kCg =
            kCard.gameObject.AddComponent<CanvasGroup>();

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

    if(baseRt != null)
    {
        baseRt.anchorMin = new Vector2(0.5f, 0.5f);
        baseRt.anchorMax = new Vector2(0.5f, 0.5f);
        baseRt.pivot = new Vector2(0.5f, 0.5f);

        baseRt.anchoredPosition =
            new Vector2(0f, -12f);

        baseRt.localScale =
            Vector3.one * 0.95f;
    }

    CanvasGroup cg =
        baseCard.GetComponent<CanvasGroup>();

    if(cg == null)
        cg =
            baseCard.gameObject.AddComponent<CanvasGroup>();

    cg.alpha = 0.45f;
    cg.blocksRaycasts = false;
    cg.interactable = false;

    CardDrag drag =
        baseCard.GetComponent<CardDrag>();

    if(drag != null)
        drag.enabled = false;

    BattleCardClick click =
        baseCard.GetComponent<BattleCardClick>();

    if(click != null)
        click.enabled = false;

    CardActionIcon icon =
        baseCard.GetComponent<CardActionIcon>();

    if(icon != null)
        icon.HideAll();

    Debug.Log(
        "K特殊召喚：土台カードを重ねた → " +
        baseCard.data.name
    );
}
}