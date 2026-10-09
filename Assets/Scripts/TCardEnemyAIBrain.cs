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
        int playerFieldCount,
        IReadOnlyList<CardController> playerField = null,
        IReadOnlyList<CardController> enemyField = null,
        int playerWallCount = 9
    )
    {
        if(hand == null || hand.Count == 0)
            return null;

        CardData best = null;
        float bestScore = float.MinValue;

        float retainedPlan = float.MinValue;
        if(maxResource >= 13)
        {
            var baseline = ReadStrategicState(hand, maxResource, currentEnemyWall,
                playerWallCount, playerField, enemyField);
            var baselineSearch = new StrategicSearch();
            SearchSummons(baseline, hand, baselineSearch, 4);
            retainedPlan = baselineSearch.best;
        }
        float bestChargePlan = float.MinValue;
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

            if(IsSpell(card))
            {
                bool useful = playerField != null
                    ? HasUsefulSpellTarget(card, playerField, enemyField, playerWallCount)
                    : playerFieldCount > 0;
                if(!useful) score += 10000f;
            }

            var remainingHand = new List<CardData>(hand);
            remainingHand.Remove(card);
            var state = ReadStrategicState(remainingHand, maxResource + 1, currentEnemyWall,
                playerWallCount, playerField, enemyField);
            var search = new StrategicSearch();
            SearchSummons(state, remainingHand, search, 4);
            score += search.best * 0.8f;
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
                bestChargePlan = search.best;
            }
        }

        // More than 13 mana can fund multiple summons; stop only when charging gains nothing.
        if(maxResource >= 13 && bestChargePlan <= retainedPlan + 50f)
            return null;

        return best;
    }

    public CardData SelectSummonCard(
        List<CardData> summonable,
        int enemyWallCount,
        int playerWallCount,
        IReadOnlyList<CardController> playerField,
        IReadOnlyList<CardController> enemyField,
        int availableResource = -1,
        int fieldSlots = 6
    )
    {
        if(summonable == null || summonable.Count == 0)
            return null;

        if(availableResource >= 0)
            return SelectStrategicSummon(summonable, availableResource, enemyWallCount,
                playerWallCount, playerField, enemyField, fieldSlots);
        CardData best = null;
        float bestScore = float.MinValue;

        foreach(CardData card in summonable)
        {
            if(card == null)
                continue;

            card.SetPowerFromName();
            card.SetCostFromName();
            float score = EvaluateSummonCandidate(
                card,
                enemyWallCount,
                playerWallCount,
                playerField,
                enemyField
            );

            if(score <= -999000f) continue;
            // Spend efficiently when another body can be summoned afterwards.
            card.SetCostFromName();
            card.SetPowerFromName();
            score /= Mathf.Sqrt(Mathf.Max(1, card.cost));
            if(CanAttack(card)) score += 180f;
            if(HasEffect(card, EffectType.NoSummonSickness) && CanAttack(card))
                score += playerWallCount <= 1 ? 1800f : 650f;

            score += EvaluateSummonTempo(card, enemyWallCount, playerWallCount, playerField, enemyField);
            // Two-ply spending: value a second summon that fits the remaining budget.
            if(availableResource >= 0)
            {
                float followup = 0f;
                int bodies = 0;
                if(enemyField != null)
                    foreach(var ally in enemyField)
                        if(ally != null && ally.data != null && !IsSpell(ally.data)) bodies++;
                for(int j = 0; j < summonable.Count; j++)
                {
                    CardData other = summonable[j];
                    if(other == null || other == card) continue;
                    other.SetCostFromName(); other.SetPowerFromName();
                    if(other.cost > availableResource - card.cost) continue;
                    if(!IsSpell(other) && bodies + (IsSpell(card) ? 0 : 1) >= 6) continue;
                    float value = EvaluateSummonCandidate(other, enemyWallCount, playerWallCount,
                        playerField, enemyField);
                    if(value <= -999000f) continue;
                    value /= Mathf.Sqrt(Mathf.Max(1, other.cost));
                    value += EvaluateSummonTempo(other, enemyWallCount, playerWallCount,
                        playerField, enemyField);
                    followup = Mathf.Max(followup, value);
                }
                score += followup * 0.65f;
            }
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

    // Search visible board only. Surviving blockers can block again in this game.
    sealed class AttackPlan
    {
        public readonly List<CardController> attackers = new List<CardController>();
        public readonly List<CardController> blockers = new List<CardController>();
        public readonly Dictionary<int, float> cache = new Dictionary<int, float>();
        public readonly int[,] outcomes = new int[6, 6];
        public readonly float[,] trades = new float[6, 6];
        public readonly int[] breaks = new int[6];
        public readonly List<CardData> attackData = new List<CardData>();
        public readonly List<CardData> blockData = new List<CardData>();
    }

    AttackPlan BuildAttackPlan(IReadOnlyList<CardController> attackers,
        IReadOnlyList<CardController> defenders)
    {
        var plan = new AttackPlan();
        if(attackers != null)
            foreach(var card in attackers)
                if(IsReadyAttacker(card) && plan.attackers.Count < 6)
                    plan.attackers.Add(card);
        if(defenders != null)
            foreach(var card in defenders)
                if(card != null && card.data != null && !card.isTapped &&
                   HasEffect(card.data, EffectType.BlockOnly) && plan.blockers.Count < 6)
                    plan.blockers.Add(card);
        foreach(var card in plan.attackers) plan.attackData.Add(card.data);
        foreach(var card in plan.blockers) plan.blockData.Add(card.data);
        PrepareCombat(plan);
        return plan;
    }

    void PrepareCombat(AttackPlan plan)
    {
        for(int a = 0; a < plan.attackData.Count; a++)
        {
            CardData attack = plan.attackData[a];
            plan.breaks[a] = HasEffect(attack, EffectType.DoubleWallBreak) ? 2 : 1;
            for(int b = 0; b < plan.blockData.Count; b++)
            {
                CardData defend = plan.blockData[b];
                int outcome = BattleOutcome(attack, defend);
                plan.outcomes[a, b] = outcome;
                plan.trades[a, b] = ((outcome >= 0 ? GetCardValue(defend) : 0f) -
                    (outcome <= 0 ? GetCardValue(attack) : 0f)) * 0.55f;
            }
        }
    }

    bool IsReadyAttacker(CardController card)
    {
        return card != null && card.data != null && !card.isTapped &&
            !card.hasSummonSickness && CanAttack(card.data) && !IsSpell(card.data);
    }

    bool IsSpell(CardData card)
    {
        return HasEffect(card, EffectType.DestroyOneEnemyBattle) ||
            HasEffect(card, EffectType.TapAllEnemyBattle);
    }

    int BattleOutcome(CardData attack, CardData defend)
    {
        if(CanSpecialBreak(attack, defend)) return 1;
        if(CanSpecialBreak(defend, attack)) return -1;
        attack.SetPowerFromName();
        defend.SetPowerFromName();
        int a = attack.power, b = defend.power;
        ApplySuitAdvantage(attack, defend, ref a, ref b);
        return a.CompareTo(b);
    }

    float SearchAttacks(AttackPlan plan, int remaining, int blockers, int walls)
    {
        if(remaining == 0) return 0f;
        int key = remaining | (blockers << 6) | (walls << 12);
        if(plan.cache.TryGetValue(key, out float cached)) return cached;
        float best = 0f; // Stop only when every continuation loses value.
        for(int i = 0; i < plan.attackData.Count; i++)
            if((remaining & (1 << i)) != 0)
                best = Mathf.Max(best, ScoreAttack(plan, i, remaining, blockers, walls));
        plan.cache[key] = best;
        return best;
    }

    float ScoreAttack(AttackPlan plan, int index, int remaining, int blockers, int walls)
    {
        // TurnManager resolves a direct attack immediately when no walls remain.
        if(walls == 0) return 1000000f;
        int next = remaining & ~(1 << index);
        int broken = Mathf.Min(walls, plan.breaks[index]);
        float worst = broken * (walls <= 2 ? 1400f : 1000f) +
            SearchAttacks(plan, next, blockers, walls - broken);
        // The player may pass or use any available blocker; assume the best response.
        for(int i = 0; i < plan.blockData.Count; i++)
        {
            if((blockers & (1 << i)) == 0) continue;
            int result = plan.outcomes[index, i];
            int nextBlockers = result >= 0 ? blockers & ~(1 << i) : blockers;
            float value = plan.trades[index, i] + SearchAttacks(plan, next, nextBlockers, walls);
            worst = Mathf.Min(worst, value);
        }
        return worst;
    }

    public List<CardController> OrderAttackers(List<CardController> attackers,
        int playerWallCount, IReadOnlyList<CardController> playerField = null)
    {
        var plan = BuildAttackPlan(attackers, playerField);
        int remaining = (1 << plan.attackers.Count) - 1;
        int blockers = (1 << plan.blockers.Count) - 1;
        var scores = new Dictionary<CardController, float>();
        for(int i = 0; i < plan.attackers.Count; i++)
            scores[plan.attackers[i]] = ScoreAttack(plan, i, remaining, blockers,
                Mathf.Max(0, playerWallCount));
        var ordered = new List<CardController>(plan.attackers);
        ordered.Sort((a, b) => {
            int compare = scores[b].CompareTo(scores[a]);
            return compare != 0 ? compare : EvaluateAttackOrder(a, playerWallCount)
                .CompareTo(EvaluateAttackOrder(b, playerWallCount));
        });
        return ordered;
    }

    public bool ShouldAttack(CardController attacker, int playerWallCount,
        IReadOnlyList<CardController> playerField, IReadOnlyList<CardController> enemyField)
    {
        if(!IsReadyAttacker(attacker)) return false;
        var candidates = enemyField != null ? new List<CardController>(enemyField) :
            new List<CardController>();
        if(!candidates.Contains(attacker)) candidates.Add(attacker);
        var plan = BuildAttackPlan(candidates, playerField);
        int index = plan.attackers.IndexOf(attacker);
        if(index < 0) return false;
        float score = ScoreAttack(plan, index, (1 << plan.attackers.Count) - 1,
            (1 << plan.blockers.Count) - 1, Mathf.Max(0, playerWallCount));
        if(showDecisionLog) Debug.Log("[AI Tactical Attack] " + attacker.data.cardName +
            " score=" + score);
        if(score < 900000f && HasEffect(attacker.data, EffectType.BlockOnly) && playerField != null)
        {
            var context = new StrategicSearch();
            var own = ReadUnits(enemyField);
            var foes = ReadUnits(playerField);
            float before = Pressure(foes, own, DefensiveWallCount, context, true);
            foreach(var guard in own)
                if(guard.source == attacker) guard.tapped = true;
            float after = Pressure(foes, own, DefensiveWallCount, context, true);
            if(after >= 900000f && before < 900000f) return false;
        }
        return score > 0f;
    }

    public int DefensiveWallCount { get; set; } = 5;

    public CardController SelectDestroyTarget(IReadOnlyList<CardController> playerField,
        IReadOnlyList<CardController> enemyField, int playerWalls, int enemyWalls)
    {
        if(playerField == null) return null;
        CardController best = null;
        float bestScore = float.MinValue;
        var plan = BuildAttackPlan(enemyField, playerField);
        int remaining = (1 << plan.attackers.Count) - 1;
        int blockers = (1 << plan.blockers.Count) - 1;
        float before = SearchAttacks(plan, remaining, blockers, Mathf.Max(0, playerWalls));
        foreach(var target in playerField)
        {
            if(target == null || target.data == null || IsSpell(target.data)) continue;
            float score = GetCardValue(target.data);
            int index = plan.blockers.IndexOf(target);
            if(index >= 0)
                score += SearchAttacks(plan, remaining, blockers & ~(1 << index),
                    Mathf.Max(0, playerWalls)) - before;
            if(CanAttack(target.data) && !IsSpell(target.data))
                score += (enemyWalls <= 2 ? 1800f : 350f) *
                    (HasEffect(target.data, EffectType.DoubleWallBreak) ? 2 : 1);
            if(score > bestScore) { bestScore = score; best = target; }
        }
        return best;
    }
public CardController SelectBestBlocker(
    CardController attacker,
    List<CardController> blockers,
    int enemyWallCount,
    IReadOnlyList<CardController> playerField = null
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

        // Keep a surviving, reusable blocker instead of spending it on a mutual trade.
        int outcome = BattleOutcome(attacker.data, blocker.data);
        if(outcome < 0) score += 3000f - GetCardValue(blocker.data) * 0.25f;
        if(playerField != null && outcome >= 0)
        {
            var counter = BuildAttackPlan(playerField, blockers);
            int guard = counter.blockers.IndexOf(blocker);
            int attack = counter.attackers.IndexOf(attacker);
            int mask = (1 << counter.attackers.Count) - 1;
            if(attack >= 0) mask &= ~(1 << attack);
            int guards = (1 << counter.blockers.Count) - 1;
            if(guard >= 0) guards &= ~(1 << guard);
            if(SearchAttacks(counter, mask, guards, enemyWallCount) >= 900000f)
                score -= 500000f;
        }

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

    int outcome = BattleOutcome(attackData, blockData);
    if(outcome <= 0) score += attackerValue;
    if(outcome >= 0) score -= blockerValue;
    // A direct hit with zero walls is defeat, regardless of the blocker's value.
    if(enemyWallCount == 0) score += 1000000f;
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
        return 10;

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
        IReadOnlyList<CardController> enemyField,
        int playerWallCount = 5, int enemyWallCount = 5
    )
    {
        var state = ReadStrategicState(new List<CardData>(), 0, enemyWallCount,
            playerWallCount, playerField, enemyField);
        // The just-cast Joker is consumed by both choices, so exclude it from board value.
        state.own.RemoveAll(unit => HasEffect(unit.data, EffectType.JokerClearBattleArea));
        return ChooseProjectedJokerClear(state);
    }
    float EvaluateSummonTempo(CardData card, int enemyWalls, int playerWalls,
        IReadOnlyList<CardController> playerField, IReadOnlyList<CardController> enemyField)
    {
        var plan = BuildAttackPlan(enemyField, playerField);
        int ready = plan.attackers.Count;
        float score = 0f;
        if(HasEffect(card, EffectType.DestroyOneEnemyBattle))
        {
            var target = SelectDestroyTarget(playerField, enemyField, playerWalls, enemyWalls);
            if(target != null && plan.blockers.Contains(target))
            {
                int all = (1 << plan.blockers.Count) - 1;
                int mask = (1 << ready) - 1;
                score += SearchAttacks(plan, mask, all & ~(1 << plan.blockers.IndexOf(target)), playerWalls) -
                    SearchAttacks(plan, mask, all, playerWalls);
            }
        }
        if(HasEffect(card, EffectType.TapAllEnemyBattle) && ready > 0)
        {
            int all = (1 << plan.blockers.Count) - 1;
            int mask = (1 << ready) - 1;
            score += SearchAttacks(plan, mask, 0, playerWalls) -
                SearchAttacks(plan, mask, all, playerWalls);
        }
        if(CanAttack(card) && !IsSpell(card))
        {
            score += 450f;
            if(HasEffect(card, EffectType.NoSummonSickness))
            {
                score += 900f;
                if(plan.blockers.Count == 0 && ready >= playerWalls)
                    score += 1000000f;
            }
        }
        if(HasEffect(card, EffectType.BlockOnly) && enemyWalls <= 2) score += 1400f;
        return score;
    }
    float EvaluateChargeCandidate(
        CardData card,
        List<CardData> hand,
        int maxResource,
        int enemyWallCount,
        int playerFieldCount
    )
    {
        card.SetCostFromName();
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

        // Keep early attackers instead of charging them while holding only expensive cards.
        if(CanAttack(card) && card.cost <= maxResource + 1)
            score -= 110f;
        if(card.cost > maxResource + 3)
            score += 90f;
        float nextSummon = 0f;
        foreach(var remaining in hand)
        {
            if(remaining == null || remaining == card) continue;
            remaining.SetCostFromName();
            remaining.SetPowerFromName();
            if(remaining.cost <= maxResource + 1)
                nextSummon = Mathf.Max(nextSummon, GetCardValue(remaining));
        }
        score += nextSummon * 0.12f;

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

    bool HasUsefulSpellTarget(CardData card,
        IReadOnlyList<CardController> playerField, IReadOnlyList<CardController> enemyField,
        int playerWalls)
    {
        if(HasEffect(card, EffectType.DestroyOneEnemyBattle))
        {
            if(playerField != null)
                foreach(var target in playerField)
                    if(target != null && target.data != null && !IsSpell(target.data)) return true;
            return false;
        }
        if(HasEffect(card, EffectType.TapAllEnemyBattle))
        {
            var plan = BuildAttackPlan(enemyField, playerField);
            if(plan.attackers.Count == 0 || plan.blockers.Count == 0) return false;
            // Tapping non-blockers expires at the next player turn and opens no attack lane.
            int attackers = (1 << plan.attackers.Count) - 1;
            int blockers = (1 << plan.blockers.Count) - 1;
            int walls = Mathf.Max(0, playerWalls);
            return SearchAttacks(plan, attackers, 0, walls) >
                SearchAttacks(plan, attackers, blockers, walls);
        }
        return true;
    }

    float EvaluateSummonCandidate(
        CardData card,
        int enemyWallCount,
        int playerWallCount,
        IReadOnlyList<CardController> playerField,
        IReadOnlyList<CardController> enemyField
    )
    {
        if(IsSpell(card) && !HasUsefulSpellTarget(card, playerField, enemyField, playerWallCount))
            return -999999f;

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
            int recovery = enemyWallCount < 9 ? 1 : 0;

            score += recovery * 850f;
        }

        if(HasEffect(card, EffectType.DoubleWallBreak))
        {
            score += Mathf.Min(2, playerWallCount) * 700f;
        }

        if(HasEffect(card, EffectType.NoSummonSickness))
            score += 250f;

        if(HasEffect(card, EffectType.Draw1)) score += 350f;
        if(HasEffect(card, EffectType.ChargeTopDeck)) score += 300f;
        if(HasEffect(card, EffectType.DiscardEnemyHand)) score += 300f;

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
            card != null && !HasEffect(card, EffectType.CannotAttack);
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
    public CardController PlannedKingBase { get; private set; }
    public CardController PlannedDestroyTarget { get; private set; }
    public CardController ConsumePlannedDestroyTarget()
    {
        CardController target = PlannedDestroyTarget;
        PlannedDestroyTarget = null;
        return target;
    }
    public int KingRecoveriesAvailable { get; set; } = 4;

    sealed class StrategicUnit
    {
        public CardData data;
        public bool tapped, sick;
        public CardController source;
        public StrategicUnit Copy() => (StrategicUnit)MemberwiseClone();
    }

    sealed class StrategicState
    {
        public List<StrategicUnit> own = new List<StrategicUnit>();
        public List<StrategicUnit> foe = new List<StrategicUnit>();
        public bool[] used;
        public int mana, ownWalls, foeWalls, first = -1, slots = 6, kingRecoveries;
        public CardController firstBase;
        public CardController firstTarget;
        public float effectValue;
        public bool extraTurn;
        public StrategicState Copy()
        {
            var result = (StrategicState)MemberwiseClone();
            result.own = own.ConvertAll(unit => unit.Copy());
            result.foe = foe.ConvertAll(unit => unit.Copy());
            result.used = (bool[])used.Clone();
            return result;
        }
    }

    sealed class StrategicSearch
    {
        public int nodes;
        public float best = float.MinValue;
        public StrategicState choice;
        public readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        public readonly Dictionary<string, float> combat = new Dictionary<string, float>();
    }

    List<StrategicUnit> ReadUnits(IReadOnlyList<CardController> cards)
    {
        var units = new List<StrategicUnit>();
        if(cards != null)
            foreach(var card in cards)
                if(card != null && card.data != null && !IsSpell(card.data))
                    units.Add(new StrategicUnit { data = card.data, tapped = card.isTapped,
                        sick = card.hasSummonSickness, source = card });
        return units;
    }

    AttackPlan ProjectCombat(List<StrategicUnit> attackers, List<StrategicUnit> defenders,
        bool nextTurn = false)
    {
        var plan = new AttackPlan();
        foreach(var unit in attackers)
            if(CanAttack(unit.data) && !IsSpell(unit.data) &&
                (nextTurn || (!unit.tapped && !unit.sick)) && plan.attackData.Count < 6)
                plan.attackData.Add(unit.data);
        foreach(var unit in defenders)
            if(!unit.tapped && HasEffect(unit.data, EffectType.BlockOnly) && plan.blockData.Count < 6)
                plan.blockData.Add(unit.data);
        PrepareCombat(plan);
        return plan;
    }

    float Pressure(List<StrategicUnit> attackers, List<StrategicUnit> defenders,
        int walls, StrategicSearch context, bool nextTurn = false)
    {
        // Cache repeated boards reached by different orders of the same summons.
        var key = new System.Text.StringBuilder(nextTurn ? "N" : "C");
        key.Append(walls);
        foreach(var unit in attackers)
            key.Append('|').Append(unit.data.cardName).Append(unit.tapped).Append(unit.sick);
        key.Append('/');
        foreach(var unit in defenders)
            key.Append('|').Append(unit.data.cardName).Append(unit.tapped);
        string id = key.ToString();
        if(context.combat.TryGetValue(id, out float cached)) return cached;
        var plan = ProjectCombat(attackers, defenders, nextTurn);
        float result = SearchAttacks(plan, (1 << plan.attackData.Count) - 1,
            (1 << plan.blockData.Count) - 1, Mathf.Max(0, walls));
        context.combat[id] = result;
        return result;
    }

    float EvaluateStrategicState(StrategicState state, StrategicSearch context)
    {
        float offense = Pressure(state.own, state.foe, state.foeWalls, context);
        if(offense >= 900000f) return 10000000f + offense + state.effectValue;
        float danger = Pressure(state.foe, state.own, state.ownWalls, context, true);
        float future = Pressure(state.own, state.foe, state.foeWalls, context, true);
        if(state.extraTurn && future >= 900000f)
            return 10000000f + future + state.effectValue;
        float score = offense * 1.3f + Mathf.Min(future, 16000f) * 0.45f;
        score -= danger >= 900000f ? (state.extraTurn ? 12000f : 2000000f) : danger * 0.9f;
        if(state.extraTurn)
            score += 2500f + Mathf.Min(future, 20000f) * 0.9f;
        foreach(var unit in state.own)
        {
            score += GetCardValue(unit.data) * 0.6f;
            if(CanAttack(unit.data)) score += 600f;
            if(HasEffect(unit.data, EffectType.BlockOnly)) score += 250f;
        }
        foreach(var unit in state.foe) score -= GetCardValue(unit.data) * 0.65f;
        score += state.ownWalls * 700f + state.effectValue;
        return score;
    }

    bool CanRaiseFrom(CardData king, CardData basis)
    {
        if(!HasEffect(king, EffectType.SpecialSummonK) || basis == null) return false;
        basis.SetPowerFromName();
        return basis.power == 7 || basis.power == 8 || basis.power == 10;
    }

    IEnumerable<StrategicState> SummonSuccessors(StrategicState state, List<CardData> hand,
        int index, StrategicSearch context)
    {
        CardData card = hand[index];
        if(card == null || state.used[index]) yield break;
        card.SetPowerFromName(); card.SetCostFromName();
        if(HasEffect(card, EffectType.DestroyOneEnemyBattle))
        {
            if(card.cost > state.mana) yield break;
            for(int target = 0; target < state.foe.Count; target++)
            {
                var next = StartSummon(state, index, card.cost);
                if(state.first < 0) next.firstTarget = state.foe[target].source;
                next.foe.RemoveAt(target);
                yield return next;
            }
            yield break;
        }
        if(HasEffect(card, EffectType.TapAllEnemyBattle))
        {
            if(card.cost > state.mana) yield break;
            var next = StartSummon(state, index, card.cost);
            foreach(var unit in next.foe) unit.tapped = true;
            // Preserve the no-empty-cast rule, including inside combo searches.
            if(Pressure(next.own, next.foe, next.foeWalls, context) >
               Pressure(state.own, state.foe, state.foeWalls, context)) yield return next;
            yield break;
        }
        if(HasEffect(card, EffectType.JokerClearBattleArea))
        {
            if(card.cost > state.mana || state.own.Count >= state.slots) yield break;
            bool clear = ChooseProjectedJokerClear(state);
            var next = StartSummon(state, index, card.cost);
            if(clear) { next.own.Clear(); next.foe.Clear(); }
            else next.extraTurn = true;
            yield return next;
            yield break;
        }
        // Normal summon and every legal King base are separate tactical actions.
        for(int basis = -1; basis < state.own.Count; basis++)
        {
            if(basis >= 0 && !CanRaiseFrom(card, state.own[basis].data)) continue;
            int cost = basis < 0 ? card.cost : Mathf.Max(0, card.cost - state.own[basis].data.power);
            if(cost > state.mana || (basis < 0 && state.own.Count >= state.slots)) continue;
            var next = StartSummon(state, index, cost);
            if(basis >= 0)
            {
                if(state.first < 0) next.firstBase = state.own[basis].source;
                next.own.RemoveAt(basis);
            }
            next.own.Add(new StrategicUnit {data = card,
                sick = basis < 0 && !HasEffect(card, EffectType.NoSummonSickness)});
            if(HasEffect(card, EffectType.Draw1)) next.effectValue += 650f;
            if(HasEffect(card, EffectType.ChargeTopDeck)) { next.mana++; next.effectValue += 900f; }
            if(HasEffect(card, EffectType.DiscardEnemyHand)) next.effectValue += 500f;
            if(HasEffect(card, EffectType.RecoverWall) && next.ownWalls < 9 && next.kingRecoveries > 0)
            { next.ownWalls++; next.kingRecoveries--; }
            yield return next;
        }
    }

    StrategicState StartSummon(StrategicState state, int index, int cost)
    {
        var next = state.Copy();
        next.used[index] = true;
        next.mana -= cost;
        if(next.first < 0) next.first = index;
        return next;
    }

    bool ChooseProjectedJokerClear(StrategicState state)
    {
        if(!GameSettings.IsAdvancedRule) return true;
        var context = new StrategicSearch();
        var extra = state.Copy(); extra.extraTurn = true;
        var clear = state.Copy(); clear.own.Clear(); clear.foe.Clear();
        return EvaluateStrategicState(clear, context) > EvaluateStrategicState(extra, context);
    }

    void SearchSummons(StrategicState state, List<CardData> hand, StrategicSearch context, int depth)
    {
        if(context.nodes >= 160 || context.clock.ElapsedMilliseconds > 100) return;
        context.nodes++;
        float value = EvaluateStrategicState(state, context);
        if(value > context.best) { context.best = value; context.choice = state; }
        if(depth == 0 || value > 10000000f) return;
        var candidates = new List<StrategicState>();
        for(int index = 0; index < hand.Count; index++)
            foreach(var next in SummonSuccessors(state, hand, index, context)) candidates.Add(next);
        if(state.first < 0)
        {
            // Every first move is examined before deeper branches consume the budget.
            // This prevents a high-ranked creature from hiding an immediate removal win.
            foreach(var candidate in candidates)
            {
                float immediate = EvaluateStrategicState(candidate, context);
                if(immediate > context.best) { context.best = immediate; context.choice = candidate; }
            }
            context.clock.Restart();
            candidates.Sort((a,b) => EvaluateStrategicState(b, context)
                .CompareTo(EvaluateStrategicState(a, context)));
        }
        else
        // Order promising actions first; bounded search also explores keeping the card.
        candidates.Sort((a,b) => QuickStateValue(b).CompareTo(QuickStateValue(a)));
        int limit = Mathf.Min(8, candidates.Count);
        for(int i = 0; i < limit; i++)
            SearchSummons(candidates[i], hand, context, depth - 1);
    }

    float QuickStateValue(StrategicState state)
    {
        float score = state.effectValue + state.ownWalls * 1200f - state.foe.Count * 500f;
        foreach(var unit in state.own)
            score += GetCardValue(unit.data) * 0.4f + (CanAttack(unit.data) ? 650f : 0f) +
                (!unit.sick && !unit.tapped && CanAttack(unit.data) ? 1800f : 0f);
        return score;
    }

    StrategicState ReadStrategicState(List<CardData> hand, int mana, int ownWalls,
        int foeWalls, IReadOnlyList<CardController> playerField,
        IReadOnlyList<CardController> enemyField, int slots = 6)
    {
        return new StrategicState { own = ReadUnits(enemyField), foe = ReadUnits(playerField),
            used = new bool[hand.Count], mana = mana, ownWalls = ownWalls, foeWalls = foeWalls,
            slots = slots, kingRecoveries = KingRecoveriesAvailable };
    }

    CardData SelectStrategicSummon(List<CardData> hand, int mana, int ownWalls, int foeWalls,
        IReadOnlyList<CardController> playerField, IReadOnlyList<CardController> enemyField, int slots)
    {
        PlannedKingBase = null;
        PlannedDestroyTarget = null;
        var state = ReadStrategicState(hand, mana, ownWalls, foeWalls, playerField, enemyField, slots);
        var context = new StrategicSearch();
        int depth = Config != null ? Mathf.Max(1, Mathf.Min(6, Config.lookaheadDepth)) : 5;
        SearchSummons(state, hand, context, depth);
        if(context.choice == null || context.choice.first < 0) return null;
        PlannedKingBase = context.choice.firstBase;
        PlannedDestroyTarget = context.choice.firstTarget;
        CardData selected = hand[context.choice.first];
        if(showDecisionLog) Debug.Log("[AI Main Search] " + selected.cardName +
            " score=" + context.best + " nodes=" + context.nodes);
        return selected;
    }

    public CardController SelectKingBase(CardData king, IReadOnlyList<CardController> field,
        int mana, int slots = 6)
    {
        if(field == null || !HasEffect(king, EffectType.SpecialSummonK)) return null;
        king.SetCostFromName();
        CardController best = null;
        float bestScore = float.MinValue;
        foreach(var unit in field)
        {
            if(unit == null || !CanRaiseFrom(king, unit.data)) continue;
            int cost = Mathf.Max(0, king.cost - unit.data.power);
            if(cost > mana) continue;
            float score = unit.data.power * 200f - GetCardValue(unit.data) * 0.2f;
            if(unit.isTapped || unit.hasSummonSickness) score += 2000f;
            if(score > bestScore) { bestScore = score; best = unit; }
        }
        return best;
    }

}
