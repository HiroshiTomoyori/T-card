using System;
using System.Collections.Generic;
using UnityEngine;

public class TCardEnemyAIBrain : MonoBehaviour
{
    public static TCardEnemyAIBrain I { get; private set; }

    [Header("Debug")]
    public bool showDecisionLog = true;

    TCardAIConfig Config
    {
        get
        {
            if(TCardAIConfigLoader.I == null)
                return null;

            return TCardAIConfigLoader.I.Config;
        }
    }

    void Awake()
    {
        I = this;
    }

    public CardData SelectResourceCard(
        List<CardData> hand,
        int maxResource,
        int currentEnemyWall,
        int playerFieldCount
    )
    {
        if(hand == null || hand.Count == 0)
            return null;

        CardData best = null;
        float bestScore = float.MinValue;

        foreach(CardData card in hand)
        {
            if(card == null)
                continue;

            float score = EvaluateChargeCandidate(
                card,
                hand,
                maxResource,
                currentEnemyWall,
                playerFieldCount
            );

            if(showDecisionLog)
            {
                Debug.Log(
                    "[AI Resource] " +
                    card.cardName +
                    " = " +
                    score
                );
            }

            if(score > bestScore)
            {
                bestScore = score;
                best = card;
            }
        }

        // 13以上では、評価が低いカードを無理にチャージしない。
        if(maxResource >= 13 && bestScore < 20f)
            return null;

        return best;
    }

    public CardData SelectSummonCard(
        List<CardData> summonable,
        int enemyWallCount,
        int playerWallCount,
        IReadOnlyList<CardController> playerField,
        IReadOnlyList<CardController> enemyField
    )
    {
        if(summonable == null || summonable.Count == 0)
            return null;

        CardData best = null;
        float bestScore = float.MinValue;

        foreach(CardData card in summonable)
        {
            if(card == null)
                continue;

            float score = EvaluateSummonCandidate(
                card,
                enemyWallCount,
                playerWallCount,
                playerField,
                enemyField
            );

            if(showDecisionLog)
            {
                Debug.Log(
                    "[AI Summon] " +
                    card.cardName +
                    " = " +
                    score
                );
            }

            if(score > bestScore)
            {
                bestScore = score;
                best = card;
            }
        }

        if(bestScore <= -999000f)
        {
            if(showDecisionLog)
            {
                Debug.Log(
                    "AI 1.0：有効な召喚候補がないため召喚見送り"
                );
            }

            return null;
        }

        return best;
    }

    public List<CardController> OrderAttackers(
        List<CardController> attackers,
        int playerWallCount
    )
    {
        if(attackers == null)
            return new List<CardController>();

        List<CardController> ordered =
            new List<CardController>(attackers);

        ordered.Sort((a, b) =>
        {
            float aScore = EvaluateAttackOrder(a, playerWallCount);
            float bScore = EvaluateAttackOrder(b, playerWallCount);

            return aScore.CompareTo(bScore);
        });

        if(showDecisionLog)
        {
            string order = "";

            foreach(CardController card in ordered)
            {
                if(card == null || card.data == null)
                    continue;

                if(order.Length > 0)
                    order += " → ";

                order += card.data.cardName;
            }

            Debug.Log("[AI Attack Order] " + order);
        }

        return ordered;
    }

public bool ShouldAttack(
    CardController attacker,
    int playerWallCount,
    IReadOnlyList<CardController> playerField,
    IReadOnlyList<CardController> enemyField
)
{
    if(attacker == null || attacker.data == null)
        return false;

    CardData card = attacker.data;

    if(attacker.isTapped)
        return false;

    if(attacker.hasSummonSickness)
        return false;

    if(HasEffect(card, EffectType.CannotAttack))
        return false;

    float attackScore = 0f;

    // Wallを削る価値
    attackScore += 500f;

    if(HasEffect(card, EffectType.DoubleWallBreak))
        attackScore += 900f;

    if(playerWallCount <= 2)
    {
        attackScore +=
            Config != null
            ? Config.lowWallAttackBonus
            : 250f;
    }

    if(playerWallCount <= 1)
        attackScore += 2000f;

    // 攻撃できるカードは基本的に積極評価
    if(Config != null)
        attackScore += Config.aggressionBonus;

    // 相手ブロッカーが多い場合は損失リスクを計算
    float strongestBlocker = 0f;

    if(playerField != null)
    {
        for(int i = 0; i < playerField.Count; i++)
        {
            CardController blocker =
                playerField[i];

            if(blocker == null ||
               blocker.data == null ||
               blocker.isTapped)
            {
                continue;
            }

            if(!HasEffect(
                blocker.data,
                EffectType.BlockOnly
            ))
            {
                continue;
            }

            strongestBlocker =
                Mathf.Max(
                    strongestBlocker,
                    GetCardValue(blocker.data)
                );
        }
    }

    float attackerValue =
        GetCardValue(card);

    if(strongestBlocker > attackerValue)
    {
        // 弱いカードならブロッカー誘導として攻撃可能
        if(attackerValue <= 900f)
        {
            attackScore += 250f;
        }
        else
        {
            attackScore -= 700f;
        }
    }

    bool shouldAttack =
        attackScore > 0f;

    if(showDecisionLog)
    {
        Debug.Log(
            "[AI Attack Decision] " +
            card.cardName +
            " score=" +
            attackScore +
            " → " +
            (shouldAttack ? "攻撃" : "見送り")
        );
    }

    return shouldAttack;
}

public CardController SelectBestBlocker(
    CardController attacker,
    List<CardController> blockers,
    int enemyWallCount
)
{
    if(attacker == null ||
       attacker.data == null)
    {
        return null;
    }

    if(blockers == null ||
       blockers.Count == 0)
    {
        return null;
    }

    CardController bestBlocker = null;
    float bestScore = float.MinValue;

    foreach(CardController blocker in blockers)
    {
        if(blocker == null ||
           blocker.data == null)
        {
            continue;
        }

        if(blocker.isTapped)
            continue;

        if(!HasEffect(
            blocker.data,
            EffectType.BlockOnly
        ))
        {
            continue;
        }

        float score =
            EvaluateBlock(
                attacker,
                blocker,
                enemyWallCount
            );

        if(showDecisionLog)
        {
            Debug.Log(
                "[AI Block] " +
                blocker.data.cardName +
                " vs " +
                attacker.data.cardName +
                " score=" +
                score
            );
        }

        if(score > bestScore)
        {
            bestScore = score;
            bestBlocker = blocker;
        }
    }

    // ブロックする方が損なら通す
    if(bestScore <= 0f)
    {
        if(showDecisionLog)
        {
            Debug.Log(
                "[AI Block] ブロックを見送り"
            );
        }

        return null;
    }

    if(showDecisionLog &&
       bestBlocker != null)
    {
        Debug.Log(
            "[AI Block] 採用：" +
            bestBlocker.data.cardName
        );
    }

    return bestBlocker;
}

float EvaluateBlock(
    CardController attacker,
    CardController blocker,
    int enemyWallCount
)
{
    CardData attackData = attacker.data;
    CardData blockData = blocker.data;

    attackData.SetPowerFromName();
    blockData.SetPowerFromName();

    float score = 0f;

    int wallBreakCount =
        HasEffect(
            attackData,
            EffectType.DoubleWallBreak
        )
        ? 2
        : 1;

    // Wallを守る価値
    score += wallBreakCount * 850f;

    if(enemyWallCount <= 2)
        score += wallBreakCount * 700f;

    if(enemyWallCount <= 1)
        score += 2000f;

    int attackerPower = attackData.power;
    int blockerPower = blockData.power;

    // 属性有利を反映
    ApplySuitAdvantage(
        attackData,
        blockData,
        ref attackerPower,
        ref blockerPower
    );

    float attackerValue =
        GetCardValue(attackData);

    float blockerValue =
        GetCardValue(blockData);

    if(blockerPower > attackerPower)
    {
        // 相手だけ倒せる
        score += attackerValue;
    }
    else if(blockerPower == attackerPower)
    {
        // 相打ち
        score += attackerValue;
        score -= blockerValue;
    }
    else
    {
        // ブロッカーだけ失う
        score -= blockerValue;
    }

    // 特殊勝利効果
    if(CanSpecialBreak(
        blockData,
        attackData
    ))
    {
        score += attackerValue + 1000f;
    }

    if(CanSpecialBreak(
        attackData,
        blockData
    ))
    {
        score -= blockerValue;
    }

    // 貴重なカードを序盤で捨てすぎない
    if(enemyWallCount >= 4 &&
       blockerValue >= 1800f &&
       wallBreakCount == 1)
    {
        score -= 900f;
    }

    return score;
}

void ApplySuitAdvantage(
    CardData attacker,
    CardData defender,
    ref int attackerPower,
    ref int defenderPower
)
{
    if(!GameSettings.IsAdvancedRule)
        return;

    if(IsSuitAdvantage(
        attacker.suit,
        defender.suit
    ))
    {
        attackerPower += 2;
    }

    if(IsSuitAdvantage(
        defender.suit,
        attacker.suit
    ))
    {
        defenderPower += 2;
    }
}

bool IsSuitAdvantage(
    Suit attacker,
    Suit defender
)
{
    return
        (attacker == Suit.Spade &&
         defender == Suit.Heart) ||

        (attacker == Suit.Heart &&
         defender == Suit.Club) ||

        (attacker == Suit.Club &&
         defender == Suit.Diamond) ||

        (attacker == Suit.Diamond &&
         defender == Suit.Spade);
}

public int GetMaxSummonsPerTurn()
{
    if(Config == null)
        return 1;

    return Mathf.Max(
        1,
        Config.maxSummonsPerTurn
    );
}

bool CanSpecialBreak(
    CardData attacker,
    CardData defender
)
{
    if(attacker == null ||
       defender == null)
    {
        return false;
    }

    string defenderName =
        !string.IsNullOrEmpty(
            defender.cardName
        )
        ? defender.cardName
        : defender.name;

    if(HasEffect(
        attacker,
        EffectType.BreakableJoker
    ) &&
       defenderName.Contains("Joker"))
    {
        return true;
    }

    if(HasEffect(
        attacker,
        EffectType.BreakableFace
    ) &&
       (
           defenderName.Contains("J") ||
           defenderName.Contains("Q") ||
           defenderName.Contains("K") ||
           defenderName.Contains("Joker")
       ))
    {
        return true;
    }

    if(HasEffect(
        attacker,
        EffectType.BreakableJack
    ) &&
       defenderName.Contains("J") &&
       !defenderName.Contains("Joker"))
    {
        return true;
    }

    return false;
}

    public bool ChooseJokerClear(
        IReadOnlyList<CardController> playerField,
        IReadOnlyList<CardController> enemyField
    )
    {
        float clearValue =
            SumFieldValue(playerField) -
            SumFieldValue(enemyField);

        if(Config != null)
            clearValue += Config.jokerClearBias;

        // true = 一掃、false = 追加ターン
        return clearValue > 0f;
    }

    float EvaluateChargeCandidate(
        CardData card,
        List<CardData> hand,
        int maxResource,
        int enemyWallCount,
        int playerFieldCount
    )
    {
        string rank = GetRank(card);
        TCardAIRankWeight weight = GetWeight(rank);

        float score = 100f;

        if(card.cost <= 6)
            score += 20f;

        int duplicates = 0;

        foreach(CardData handCard in hand)
        {
            if(handCard != null && GetRank(handCard) == rank)
                duplicates++;
        }

        score += Mathf.Max(0, duplicates - 1) * 12f;

        if(weight != null)
            score -= weight.chargePenalty;

        if(HasEffect(card, EffectType.DestroyOneEnemyBattle) &&
           playerFieldCount > 0)
        {
            score -= 100f;
        }

        if(HasEffect(card, EffectType.TapAllEnemyBattle) &&
           playerFieldCount >= 2)
        {
            score -= 100f;
        }

        if(HasEffect(card, EffectType.RecoverWall) &&
           enemyWallCount < 5)
        {
            score -= 130f;
        }

        if(HasEffect(card, EffectType.JokerExtraTurn))
            score -= 160f;

        if(maxResource >= 13)
            score -= 30f;

        return score;
    }

    float EvaluateSummonCandidate(
        CardData card,
        int enemyWallCount,
        int playerWallCount,
        IReadOnlyList<CardController> playerField,
        IReadOnlyList<CardController> enemyField
    )
    {
        string rank = GetRank(card);
        TCardAIRankWeight weight = GetWeight(rank);

        float score =
            weight != null
            ? weight.value
            : card.power * 100f;

        if(HasEffect(card, EffectType.DestroyOneEnemyBattle))
        {
            if(playerField == null || playerField.Count == 0)
                return -999999f;

            score += GetHighestFieldValue(playerField) * 0.7f;
        }

        if(HasEffect(card, EffectType.TapAllEnemyBattle))
        {
            int activeTargets = CountUntapped(playerField);

            if(activeTargets == 0)
                return -999999f;

            score += activeTargets * 450f;
        }

        if(HasEffect(card, EffectType.RecoverWall))
        {
            int recovery =
                enemyWallCount < 5
                ? 5 - enemyWallCount
                : enemyWallCount < 9 ? 1 : 0;

            score += recovery * 850f;
        }

        if(HasEffect(card, EffectType.DoubleWallBreak))
        {
            score += Mathf.Min(2, playerWallCount) * 700f;
        }

        if(HasEffect(card, EffectType.NoSummonSickness))
            score += 250f;

        if(HasEffect(card, EffectType.BlockOnly))
            score += enemyWallCount <= 2 ? 400f : 180f;

        if(HasEffect(card, EffectType.JokerClearBattleArea))
        {
            float clearValue =
                SumFieldValue(playerField) -
                SumFieldValue(enemyField);

            score += Mathf.Max(0f, clearValue);

            if(Config != null)
                score += Mathf.Abs(Config.jokerClearBias);
        }

        if(playerWallCount <= 2 && CanAttack(card))
        {
            score +=
                Config != null
                ? Config.lowWallAttackBonus
                : 200f;
        }

        return score;
    }

    float EvaluateAttackOrder(
        CardController attacker,
        int playerWallCount
    )
    {
        if(attacker == null || attacker.data == null)
            return float.MaxValue;

        CardData card = attacker.data;

        float score = GetCardValue(card);

        // 小型で先にブロッカーを誘い、2枚割りを後ろへ。
        if(HasEffect(card, EffectType.DoubleWallBreak))
            score += 5000f;

        // Wallが残り2以下なら勝ち筋カードを早める。
        if(playerWallCount <= 2 &&
           HasEffect(card, EffectType.DoubleWallBreak))
        {
            score -= 7000f;
        }

        return score;
    }

    public float GetCardValue(CardData card)
    {
        if(card == null)
            return 0f;

        TCardAIRankWeight weight =
            GetWeight(GetRank(card));

        float value =
            weight != null
            ? weight.value
            : card.power * 100f;

        if(HasEffect(card, EffectType.DoubleWallBreak))
            value += 450f;

        if(HasEffect(card, EffectType.DestroyOneEnemyBattle))
            value += 400f;

        if(HasEffect(card, EffectType.TapAllEnemyBattle))
            value += 350f;

        if(HasEffect(card, EffectType.RecoverWall))
            value += 400f;

        if(HasEffect(card, EffectType.JokerExtraTurn))
            value += 700f;

        return value;
    }

    TCardAIRankWeight GetWeight(string rank)
    {
        if(Config == null)
            return null;

        return Config.GetRankWeight(rank);
    }

    string GetRank(CardData card)
    {
        if(card == null)
            return "";

        string value =
            !string.IsNullOrEmpty(card.cardName)
            ? card.cardName
            : card.name;

        if(value.Contains("Joker"))
            return "Joker";

        if(value.Contains("10"))
            return "10";

        if(value.Contains("Jack") ||
           value.EndsWith("_J") ||
           value.Contains("_J_"))
        {
            return "J";
        }

        if(value.EndsWith("_Q") || value.Contains("_Q_"))
            return "Q";

        if(value.EndsWith("_K") || value.Contains("_K_"))
            return "K";

        if(value.EndsWith("_A") || value.Contains("_A_"))
            return "A";

        for(int i = 9; i >= 2; i--)
        {
            if(value.EndsWith("_" + i) ||
               value.Contains("_" + i + "_"))
            {
                return i.ToString();
            }
        }

        return "";
    }

    bool CanAttack(CardData card)
    {
        return
            !HasEffect(card, EffectType.CannotAttack) &&
            !HasEffect(card, EffectType.BlockOnly);
    }

    bool HasEffect(CardData card, EffectType effect)
    {
        if(card == null || card.effectTypes == null)
            return false;

        return Array.Exists(
            card.effectTypes,
            x => x == effect
        );
    }

    float SumFieldValue(
        IReadOnlyList<CardController> cards
    )
    {
        if(cards == null)
            return 0f;

        float total = 0f;

        for(int i = 0; i < cards.Count; i++)
        {
            CardController card = cards[i];

            if(card != null)
                total += GetCardValue(card.data);
        }

        return total;
    }

    float GetHighestFieldValue(
        IReadOnlyList<CardController> cards
    )
    {
        float highest = 0f;

        if(cards == null)
            return highest;

        for(int i = 0; i < cards.Count; i++)
        {
            CardController card = cards[i];

            if(card == null)
                continue;

            highest = Mathf.Max(
                highest,
                GetCardValue(card.data)
            );
        }

        return highest;
    }

    int CountUntapped(
        IReadOnlyList<CardController> cards
    )
    {
        if(cards == null)
            return 0;

        int count = 0;

        for(int i = 0; i < cards.Count; i++)
        {
            CardController card = cards[i];

            if(card != null && !card.isTapped)
                count++;
        }

        return count;
    }
}
