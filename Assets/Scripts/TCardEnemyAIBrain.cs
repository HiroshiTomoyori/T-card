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

    public int SelectAssistedDrawIndex(List<CardData> deck, List<CardData> hand,
        int resource, int ownWalls, int foeWalls, IReadOnlyList<CardController> playerField,
        IReadOnlyList<CardController> enemyField, IReadOnlyList<int> candidates, int availableResource = -1)
    {
        if(deck == null || candidates == null) return -1;
        var state = ReadStrategicState(hand ?? new List<CardData>(), availableResource >= 0 ? availableResource : resource,
            ownWalls, foeWalls, playerField, enemyField);
        var search = new StrategicSearch();
        float offense = Pressure(state.own, state.foe, foeWalls, search);
        float danger = Pressure(state.foe, state.own, ownWalls, search, true);
        int best = -1; float bestScore = float.MinValue;
        foreach(int index in candidates)
        {
            if(index < 0 || index >= deck.Count || deck[index] == null) continue;
            float score = EvaluateDrawCandidate(deck[index], hand, resource, state, search, offense, danger);
            if(score > bestScore) { bestScore = score; best = index; }
        }
        return best;
    }

    float EvaluateDrawCandidate(CardData card, List<CardData> hand, int resource,
        StrategicState state, StrategicSearch search, float offense, float danger)
    {
        card.SetPowerFromName(); card.SetCostFromName();
        int budget = Mathf.Max(1, resource + 1);
        float score = GetCardValue(card) * 0.18f;
        int cheap = 0, copies = 0;
        if(hand != null) foreach(var held in hand)
        {
            if(held == null) continue;
            held.SetCostFromName();
            if(!IsSpell(held) && held.cost <= budget + 2) cheap++;
            if(GetRank(held) == GetRank(card)) copies++;
        }
        score -= copies * 650f;
        if(!IsSpell(card))
        {
            score += card.cost <= budget + 2 ? (cheap < 2 ? 1400f : 350f) : 0f;
            score -= Mathf.Max(0, card.cost - budget - 2) * 160f;
            if(HasEffect(card, EffectType.ChargeTopDeck)) score += resource < 13 ? 1050f : 300f;
            if(HasEffect(card, EffectType.Draw1)) score += 650f;
            if(HasEffect(card, EffectType.NoSummonSickness)) score += 350f;
            if(HasEffect(card, EffectType.BlockOnly)) score += state.ownWalls <= 2 ? 600f : 200f;
            if(HasEffect(card, EffectType.DoubleWallBreak)) score += 550f;
        }
        else score -= 500f;
        int cost = card.cost;
        bool raised = false;
        foreach(var basis in state.own)
            if(CanRaiseFrom(card, basis.data) && card.cost - basis.data.power < cost)
            { cost = card.cost - basis.data.power; raised = true; }
        bool affordable = cost <= state.mana;
        if(HasEffect(card, EffectType.DestroyOneEnemyBattle))
        {
            if(state.foe.Count == 0) return score - 2500f;
            score += 600f;
            if(affordable)
                foreach(var target in state.foe)
                {
                    var removed = state.Copy(); removed.foe.RemoveAll(u => u.source == target.source);
                    score = Mathf.Max(score, 600f + DrawTacticalGain(removed, state, search, offense, danger));
                }
            return score;
        }
        if(HasEffect(card, EffectType.TapAllEnemyBattle))
        {
            var tapped = state.Copy(); foreach(var unit in tapped.foe) unit.tapped = true;
            float gain = Pressure(tapped.own, tapped.foe, tapped.foeWalls, search) - offense;
            if(gain <= 0f) return score - 2500f;
            return score + (affordable ? gain * 1.2f : 350f);
        }
        if(HasEffect(card, EffectType.JokerClearBattleArea))
        {
            if(!affordable) return score;
            var joker = state.Copy();
            if(ChooseProjectedJokerClear(state)) { joker.own.Clear(); joker.foe.Clear(); }
            else joker.extraTurn = true;
            return score + DrawTacticalGain(joker, state, search, offense, danger);
        }
        if(HasEffect(card, EffectType.RecoverWall) && state.ownWalls < 5 && KingRecoveriesAvailable > 0)
            score += 800f;
        if(!affordable || (state.own.Count >= state.slots && !raised)) return score;
        var developed = state.Copy();
        if(raised)
        {
            int basis = developed.own.FindIndex(u => CanRaiseFrom(card, u.data) && card.cost - u.data.power == cost);
            if(basis >= 0) developed.own.RemoveAt(basis);
        }
        developed.own.Add(new StrategicUnit { data = card,
            sick = !raised && !HasEffect(card, EffectType.NoSummonSickness) });
        if(HasEffect(card, EffectType.RecoverWall) && developed.ownWalls < 9 && KingRecoveriesAvailable > 0)
            developed.ownWalls++;
        return score + DrawTacticalGain(developed, state, search, offense, danger);
    }

    float DrawTacticalGain(StrategicState next, StrategicState before, StrategicSearch search,
        float offense, float danger)
    {
        float attack = Pressure(next.own, next.foe, next.foeWalls, search, next.extraTurn);
        if(attack >= 900000f && offense < 900000f) return 20000000f;
        float defense = Pressure(next.foe, next.own, next.ownWalls, search, true);
        if(danger >= 900000f && defense < 900000f) return 10000000f;
        return Mathf.Min(12000f, Mathf.Max(0f, attack - offense)) * 1.2f +
            Mathf.Min(12000f, Mathf.Max(0f, danger - defense)) * 0.8f;
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
        if(maxResource >= 0)
        {
            var baseline = ReadStrategicState(hand, maxResource, currentEnemyWall,
                playerWallCount, playerField, enemyField);
            var baselineSearch = new StrategicSearch();
            SearchSummons(baseline, hand, baselineSearch, 6);
            retainedPlan = baselineSearch.best;
            if(baselineSearch.choice != null && retainedPlan < 10000000f)
            {
                var forecast = new BattleForecast();
                retainedPlan = ForecastAttacks(baselineSearch.choice, forecast, 6) + baselineSearch.choice.effectValue + baselineSearch.choice.handValue;
            }
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
            SearchSummons(state, remainingHand, search, 6);
            float chargePlan = search.best;
            if(search.choice != null && chargePlan < 10000000f)
            {
                var forecast = new BattleForecast();
                chargePlan = ForecastAttacks(search.choice, forecast, 6) + search.choice.effectValue + search.choice.handValue;
            }
            score += chargePlan * 0.8f;
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
                bestChargePlan = chargePlan;
            }
        }

        // Never destroy an already available winning line just to gain one resource.
        if(retainedPlan >= 10000000f && bestChargePlan < 10000000f) return null;
        if(retainedPlan > -1000000f && bestChargePlan <= -1000000f) return null;
        if(maxResource >= 6 && hand.Count <= 2 && bestChargePlan < retainedPlan - 1600f) return null;
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
        int playerWallCount, IReadOnlyList<CardController> playerField = null, int enemyWallCount = 5,
        IReadOnlyList<CardController> enemyField = null)
    {
        DefensiveWallCount = enemyWallCount;
        PrepareBattleDecisions(enemyField ?? attackers, playerField, enemyWallCount, playerWallCount);
        var ordered = new List<CardController>();
        if(attackers != null)
            foreach(var card in attackers)
                if(IsReadyAttacker(card)) ordered.Add(card);
        ordered.Sort((a,b) => {
            float left = lastAttackScores.TryGetValue(a, out var x) ? x : float.MinValue;
            float right = lastAttackScores.TryGetValue(b, out var y) ? y : float.MinValue;
            int order = right.CompareTo(left);
            return order != 0 ? order : EvaluateAttackOrder(a, playerWallCount)
                .CompareTo(EvaluateAttackOrder(b, playerWallCount));
        });
        return ordered;
    }

    public bool ShouldAttack(CardController attacker, int playerWallCount,
        IReadOnlyList<CardController> playerField, IReadOnlyList<CardController> enemyField)
    {
        if(!IsReadyAttacker(attacker)) return false;
        var allies = enemyField != null ? new List<CardController>(enemyField) : new List<CardController>();
        if(!allies.Contains(attacker)) allies.Add(attacker);
        // Preserve the complete attack-only proof of a current-turn win.
        var immediate = BuildAttackPlan(allies, playerField);
        int index = immediate.attackers.IndexOf(attacker);
        if(index >= 0 && ScoreAttack(immediate, index, (1 << immediate.attackers.Count) - 1,
            (1 << immediate.blockers.Count) - 1, playerWallCount) >= 900000f) return true;
        PrepareBattleDecisions(allies, playerField, DefensiveWallCount, playerWallCount);
        bool attack = lastAttackScores.TryGetValue(attacker, out float score) && score > lastStopScore + 5f;
        if(showDecisionLog) Debug.Log("[AI Counterplay] " + attacker.data.cardName +
            " attack=" + score + " hold=" + lastStopScore);
        return attack;
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
    IReadOnlyList<CardController> playerField = null,
    IReadOnlyList<CardController> enemyField = null,
    int playerWallCount = 5
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

    if(playerField != null && enemyField != null)
        return SelectPlannedDefense(attacker, blockers, enemyWallCount, playerWallCount,
            playerField, enemyField);
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
    public bool PlannedWinningLine { get; private set; }
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
        public float effectValue, handValue;
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
        public readonly Dictionary<string, Tuple<float, StrategicState>> finalists = new Dictionary<string, Tuple<float, StrategicState>>();
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

    float GuardCoverage(List<StrategicUnit> guards, List<StrategicUnit> threats)
    {
        float coverage = 0f;
        foreach(var threat in threats)
        {
            if(!CanAttack(threat.data)) continue;
            float best = 0f;
            foreach(var guard in guards)
            {
                if(guard.tapped || !HasEffect(guard.data, EffectType.BlockOnly)) continue;
                int result = BattleOutcome(threat.data, guard.data);
                // A reusable counter matters much more than a one-use losing guard.
                if(result < 0) best = Mathf.Max(best, Mathf.Min(GetCardValue(threat.data) * 0.3f, 350f + Mathf.Min(800f, GetCardValue(threat.data) * 0.35f)));
                else if(result == 0) best = Mathf.Max(best, Mathf.Min(100f, GetCardValue(threat.data) * 0.12f));
            }
            coverage += best;
        }
        return coverage;
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
        score += GuardCoverage(state.own, state.foe) - GuardCoverage(state.foe, state.own) * 0.7f;
        score += state.ownWalls * 700f + state.effectValue + state.handValue;
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
                var next = StartSummon(state, index, card.cost, card);
                if(state.first < 0) next.firstTarget = state.foe[target].source;
                next.foe.RemoveAt(target);
                yield return next;
            }
            yield break;
        }
        if(HasEffect(card, EffectType.TapAllEnemyBattle))
        {
            if(card.cost > state.mana) yield break;
            var next = StartSummon(state, index, card.cost, card);
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
            var next = StartSummon(state, index, card.cost, card);
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
            var next = StartSummon(state, index, cost, card);
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

    StrategicState StartSummon(StrategicState state, int index, int cost, CardData card)
    {
        var next = state.Copy();
        next.used[index] = true;
        next.handValue -= GetCardValue(card) * 0.3f;
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
        var clearForecast = new BattleForecast();
        var extraForecast = new BattleForecast();
        return ForecastAttacks(clear, clearForecast, 6) > ForecastAttacks(extra, extraForecast, 6);
    }

    void SearchSummons(StrategicState state, List<CardData> hand, StrategicSearch context, int depth)
    {
        ConsiderSummon(state, EvaluateStrategicState(state, context), context);
        var frontier = new List<StrategicState> {state};
        var visited = new HashSet<string>();
        for(int level = 0; level < depth && frontier.Count > 0; level++)
        {
            var candidates = new List<Tuple<float, StrategicState>>();
            foreach(var parent in frontier)
            {
                for(int index = 0; index < hand.Count; index++)
                {
                    if(level > 0 && (context.nodes >= 2048 || context.clock.ElapsedMilliseconds > 300)) break;
                    foreach(var next in SummonSuccessors(parent, hand, index, context))
                    {
                        if(!visited.Add(PositionKey(next, true))) continue;
                        context.nodes++;
                        float value = EvaluateStrategicState(next, context);
                        ConsiderSummon(next, value, context);
                        candidates.Add(Tuple.Create(value, next));
                    }
                }
            }
            candidates.Sort((a,b) => b.Item1.CompareTo(a.Item1));
            frontier.Clear();
            // Retain different opening actions before filling the beam by raw score.
            var openings = new HashSet<int>();
            var selected = new HashSet<StrategicState>();
            foreach(var candidate in candidates)
            {
                if(frontier.Count >= 24) break;
                if(candidate.Item1 >= 10000000f || !openings.Add(candidate.Item2.first)) continue;
                frontier.Add(candidate.Item2); selected.Add(candidate.Item2);
            }
            foreach(var candidate in candidates)
            {
                if(frontier.Count >= 24) break;
                if(candidate.Item1 < 10000000f && selected.Add(candidate.Item2)) frontier.Add(candidate.Item2);
            }
            if(context.best > 10000000f || context.nodes >= 2048 || context.clock.ElapsedMilliseconds > 300) break;
        }
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
        float reserve = 0f;
        foreach(var card in hand) if(card != null) reserve += GetCardValue(card) * 0.3f;
        return new StrategicState { handValue = reserve, own = ReadUnits(enemyField), foe = ReadUnits(playerField),
            used = new bool[hand.Count], mana = mana, ownWalls = ownWalls, foeWalls = foeWalls,
            slots = slots, kingRecoveries = KingRecoveriesAvailable };
    }

    CardData SelectStrategicSummon(List<CardData> hand, int mana, int ownWalls, int foeWalls,
        IReadOnlyList<CardController> playerField, IReadOnlyList<CardController> enemyField, int slots)
    {
        PlannedKingBase = null;
        PlannedDestroyTarget = null;
        PlannedWinningLine = false;
        var state = ReadStrategicState(hand, mana, ownWalls, foeWalls, playerField, enemyField, slots);
        var context = new StrategicSearch();
        int depth = Config != null ? Mathf.Max(1, Mathf.Min(6, Config.lookaheadDepth)) : 6;
        SearchSummons(state, hand, context, depth);
        if(context.best < 10000000f)
        {
            var forecasts = new List<Tuple<float, StrategicState>>(context.finalists.Values);
            forecasts.Sort((a,b) => b.Item1.CompareTo(a.Item1));
            var hold = new BattleForecast();
            float bestBattle = ForecastAttacks(state, hold, 6) + state.effectValue + state.handValue;
            StrategicState battleChoice = state;
            for(int i = 0; i < Mathf.Min(6, forecasts.Count); i++)
            {
                var candidate = forecasts[i].Item2;
                var forecast = new BattleForecast();
                float score = ForecastAttacks(candidate, forecast, 6) + candidate.effectValue + candidate.handValue;
                if(score > bestBattle) { bestBattle = score; battleChoice = candidate; }
            }
            context.choice = battleChoice;
            context.best = bestBattle;
        }
        PlannedWinningLine = context.best >= 10000000f;
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

    sealed class DefenseForecast
    {
        public int nodes;
        public readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        public readonly StrategicSearch pressure = new StrategicSearch();
        public readonly Dictionary<string, float> memo = new Dictionary<string, float>();
    }

    CardController SelectPlannedDefense(CardController attacker, List<CardController> blockers,
        int ownWalls, int foeWalls, IReadOnlyList<CardController> playerField,
        IReadOnlyList<CardController> enemyField)
    {
        var state = ReadStrategicState(new List<CardData>(), 0, ownWalls, foeWalls, playerField, enemyField);
        int attack = state.foe.FindIndex(u => u.source == attacker);
        if(attack < 0 || ownWalls <= 0) return null;
        // The attacking card may already be tapped by the live battle animation.
        state.foe[attack].tapped = true;
        var shared = new DefenseForecast();
        float best = ForecastDefense(ResolveIncoming(state, attack, -1), shared, 6);
        CardController chosen = null;
        foreach(var blocker in blockers)
        {
            int guard = state.own.FindIndex(u => u.source == blocker);
            if(guard < 0 || state.own[guard].tapped || !HasEffect(blocker.data, EffectType.BlockOnly)) continue;
            // Give each root choice the same search budget.
            var search = new DefenseForecast();
            float score = ForecastDefense(ResolveIncoming(state, attack, guard), search, 6);
            if(score > best + 1f) { best = score; chosen = blocker; }
        }
        // A doomed projection must not turn into automatic surrender. Wall triggers and
        // future draws are unknown, so preserve chances with the best immediate defense.
        if(best <= -10000000f)
        {
            float fallback = 0f;
            foreach(var blocker in blockers)
            {
                if(blocker == null || blocker.data == null || blocker.isTapped || !HasEffect(blocker.data, EffectType.BlockOnly)) continue;
                float value = EvaluateBlock(attacker, blocker, ownWalls);
                if(BattleOutcome(attacker.data, blocker.data) < 0) value += 3000f;
                if(value > fallback) { fallback = value; chosen = blocker; }
            }
        }
        if(showDecisionLog) Debug.Log("[AI Defense plan] " + (chosen == null ? "pass" : chosen.data.cardName) + " score=" + best);
        return chosen;
    }

    StrategicState ResolveIncoming(StrategicState state, int attack, int guard)
    {
        var next = state.Copy();
        var data = next.foe[attack].data;
        next.foe[attack].tapped = true;
        if(guard < 0)
            next.ownWalls = Mathf.Max(0, next.ownWalls - (HasEffect(data, EffectType.DoubleWallBreak) ? 2 : 1));
        else
        {
            int outcome = BattleOutcome(data, next.own[guard].data);
            if(outcome >= 0) next.own.RemoveAt(guard);
            if(outcome <= 0) next.foe.RemoveAt(attack);
        }
        return next;
    }

    float DefenseTerminal(StrategicState state, DefenseForecast search)
    {
        // CPU untaps first on its next turn; the player's surviving attackers stay tapped.
        var counter = state.Copy();
        foreach(var unit in counter.own) { unit.tapped = false; unit.sick = false; }
        float pressure = Pressure(counter.own, counter.foe, counter.foeWalls, search.pressure, false);
        bool winningCounter = pressure >= 900000f;
        float value = counter.ownWalls * 1100f + Mathf.Min(pressure, 12000f) * 0.8f;
        foreach(var unit in counter.own) value += GetCardValue(unit.data);
        foreach(var unit in counter.foe) value -= GetCardValue(unit.data) * 0.9f;
        return value + (winningCounter ? 3000000f : 0f);
    }

    float ForecastDefense(StrategicState state, DefenseForecast search, int depth)
    {
        string key = depth + ":" + PositionKey(state);
        if(search.memo.TryGetValue(key, out float cached)) return cached;
        float worst = DefenseTerminal(state, search);
        bool limited = depth == 0 || search.nodes >= 768 || search.clock.ElapsedMilliseconds > 150;
        search.nodes++;
        for(int a = 0; a < state.foe.Count; a++)
        {
            var attacker = state.foe[a];
            if(attacker.tapped || attacker.sick || !CanAttack(attacker.data)) continue;
            if(state.ownWalls == 0) return -11000000f - depth * 10000f;
            if(limited)
            {
                // At the budget boundary still prove immediate lethal instead of valuing a doomed board.
                float danger = Pressure(state.foe, state.own, state.ownWalls, search.pressure, false);
                return danger >= 900000f ? -11000000f - depth * 10000f : worst - Mathf.Min(danger, 12000f);
            }
            float best = ForecastDefense(ResolveIncoming(state, a, -1), search, depth - 1);
            for(int b = 0; b < state.own.Count; b++)
            {
                var guard = state.own[b];
                if(guard.tapped || !HasEffect(guard.data, EffectType.BlockOnly)) continue;
                best = Mathf.Max(best, ForecastDefense(ResolveIncoming(state, a, b), search, depth - 1));
            }
            worst = Mathf.Min(worst, best);
        }
        search.memo[key] = worst;
        return worst;
    }
    sealed class BattleForecast
    {
        public int nodes;
        public bool useReserve;
        public readonly StrategicSearch position;
        public readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        public readonly Dictionary<string, float> memo = new Dictionary<string, float>();
        public BattleForecast(StrategicSearch sharedPosition = null)
        {
            position = sharedPosition ?? new StrategicSearch();
        }
    }

    float QuickAfterBattleScore(StrategicState state)
    {
        int attackers = 0, breaks = 0;
        bool reusableGuard = false;
        foreach(var enemy in state.foe)
            if(CanAttack(enemy.data))
            { attackers++; breaks += HasEffect(enemy.data, EffectType.DoubleWallBreak) ? 2 : 1; }
        int guards = 0;
        foreach(var guard in state.own)
        {
            if(guard.tapped || !HasEffect(guard.data, EffectType.BlockOnly)) continue;
            guards++;
            bool winsAll = true;
            foreach(var enemy in state.foe)
                if(CanAttack(enemy.data) && BattleOutcome(enemy.data, guard.data) >= 0) winsAll = false;
            if(winsAll) reusableGuard = true;
        }
        float score = (9 - state.foeWalls) * 1600f + state.ownWalls * 900f;
        foreach(var unit in state.own)
            score += GetCardValue(unit.data) * 0.7f + (CanAttack(unit.data) ? 500f : 0f);
        foreach(var unit in state.foe) score -= GetCardValue(unit.data) * 0.75f;
        if(!state.extraTurn && attackers > 0 && state.ownWalls == 0) score -= 2000000f;
        else if(!state.extraTurn && guards == 0 && attackers > 1 && breaks - 1 >= state.ownWalls)
            score -= 2000000f;
        else if(!reusableGuard) score -= Mathf.Max(0, breaks - guards) * 700f;
        if(state.extraTurn) score += 2500f;
        return score;
    }

    string PositionKey(StrategicState state, bool includeHand = false)
    {
        var key = new System.Text.StringBuilder();
        key.Append(state.ownWalls).Append('/').Append(state.foeWalls).Append('/').Append(state.extraTurn);
        var allies = new List<string>();
        var foes = new List<string>();
        foreach(var unit in state.own)
            allies.Add(unit.data.cardName + ':' + unit.tapped + ':' + unit.sick);
        foreach(var unit in state.foe)
            foes.Add(unit.data.cardName + ':' + unit.tapped + ':' + unit.sick);
        allies.Sort(StringComparer.Ordinal); foes.Sort(StringComparer.Ordinal);
        foreach(var unit in allies) key.Append('|').Append(unit);
        key.Append('/');
        foreach(var unit in foes) key.Append('|').Append(unit);
        if(includeHand)
        {
            key.Append('/').Append(state.mana).Append('/').Append(state.kingRecoveries).Append('/').Append(state.effectValue);
            foreach(bool used in state.used) key.Append(used ? '1' : '0');
        }
        return key.ToString();
    }

    float ScoreAfterBattle(StrategicState state, BattleForecast forecast)
    {
        float reply = Pressure(state.foe, state.own, state.ownWalls, forecast.position, true);
        float future = Pressure(state.own, state.foe, state.foeWalls, forecast.position, true);
        if(state.extraTurn && future >= 900000f) return 11000000f;
        float score = (9 - state.foeWalls) * 1600f + state.ownWalls * 900f;
        score -= reply >= 900000f ? (state.extraTurn ? 18000f : 2000000f) : reply * 1.1f;
        score += Mathf.Min(future, 12000f) * (state.extraTurn ? 0.7f : 0.12f);
        if(state.extraTurn) score += 2500f;
        foreach(var unit in state.own)
            score += GetCardValue(unit.data) * 0.7f + (CanAttack(unit.data) ? 500f : 0f);
        foreach(var unit in state.foe) score -= GetCardValue(unit.data) * 0.75f;
        score += GuardCoverage(state.own, state.foe) - GuardCoverage(state.foe, state.own) * 0.7f;
        if(forecast.useReserve) score += reserveDevelopment[Mathf.Max(0, Mathf.Min(6, state.slots - state.own.Count))];
        return score;
    }

    float ForecastAttacks(StrategicState state, BattleForecast forecast, int depth)
    {
        string key = depth + ":" + PositionKey(state);
        if(forecast.memo.TryGetValue(key, out float cached)) return cached;
        bool smallPosition = state.own.Count + state.foe.Count <= 6;
        int nodeLimit = smallPosition ? 256 : 48;
        int timeLimit = smallPosition ? 100 : 50;
        if(forecast.nodes >= nodeLimit || forecast.clock.ElapsedMilliseconds > timeLimit)
        {
            float limited = QuickAfterBattleScore(state);
            if(forecast.useReserve) limited += reserveDevelopment[Mathf.Max(0, Mathf.Min(6, state.slots - state.own.Count))];
            forecast.memo[key] = limited;
            return limited;
        }
        forecast.nodes++;
        float best = ScoreAfterBattle(state, forecast);
        if(depth == 0)
        {
            forecast.memo[key] = best;
            return best;
        }
        for(int i = 0; i < state.own.Count; i++)
        {
            if(forecast.nodes >= nodeLimit || forecast.clock.ElapsedMilliseconds > timeLimit) break;
            var unit = state.own[i];
            if(unit.tapped || unit.sick || !CanAttack(unit.data)) continue;
            best = Mathf.Max(best, ForecastAttack(state, i, forecast, depth));
        }
        forecast.memo[key] = best;
        return best;
    }

    float ForecastAttack(StrategicState state, int index, BattleForecast forecast, int depth)
    {
        if(state.foeWalls == 0) return 11000000f;
        CardData attack = state.own[index].data;
        var passed = state.Copy();
        passed.own[index].tapped = true;
        passed.foeWalls = Mathf.Max(0, passed.foeWalls -
            (HasEffect(attack, EffectType.DoubleWallBreak) ? 2 : 1));
        float worst = ForecastAttacks(passed, forecast, depth - 1);
        for(int i = 0; i < state.foe.Count; i++)
        {
            var blocker = state.foe[i];
            if(blocker.tapped || !HasEffect(blocker.data, EffectType.BlockOnly)) continue;
            var blocked = state.Copy();
            blocked.own[index].tapped = true;
            int result = BattleOutcome(attack, blocker.data);
            if(result >= 0) blocked.foe.RemoveAt(i);
            if(result <= 0) blocked.own.RemoveAt(index);
            // A surviving blocker remains untapped and may block the next attack.
            worst = Mathf.Min(worst, ForecastAttacks(blocked, forecast, depth - 1));
        }
        return worst;
    }

    readonly float[] reserveDevelopment = new float[7];
    public void ConfigureBattleReserve(List<CardData> hand, int nextTurnMana)
    {
        Array.Clear(reserveDevelopment, 0, reserveDevelopment.Length);
        int budget = Mathf.Max(0, Mathf.Min(128, nextTurnMana));
        var values = new float[7, budget + 1];
        if(hand != null)
            foreach(var card in hand)
            {
                if(card == null || IsSpell(card) || HasEffect(card, EffectType.JokerClearBattleArea)) continue;
                card.SetCostFromName(); card.SetPowerFromName();
                if(card.cost > budget) continue;
                float value = (GetCardValue(card) * 0.7f + (CanAttack(card) ? 500f : 0f)) * 0.75f;
                if(HasEffect(card, EffectType.Draw1)) value += 350f;
                if(HasEffect(card, EffectType.ChargeTopDeck)) value += 450f;
                for(int slots = 6; slots >= 1; slots--)
                    for(int mana = budget; mana >= card.cost; mana--)
                        values[slots, mana] = Mathf.Max(values[slots, mana], values[slots - 1, mana - card.cost] + value);
            }
        for(int slots = 0; slots <= 6; slots++) reserveDevelopment[slots] = values[slots, budget];
        // The same board can require a different attack after the hand changes.
        lastBattleKey = null;
    }
    string lastBattleKey;
    readonly Dictionary<CardController, float> lastAttackScores = new Dictionary<CardController, float>();
    float lastStopScore;

    void PrepareBattleDecisions(IReadOnlyList<CardController> own,
        IReadOnlyList<CardController> foe, int ownWalls, int foeWalls)
    {
        var state = ReadStrategicState(new List<CardData>(), 0, ownWalls, foeWalls, foe, own);
        string key = PositionKey(state);
        foreach(var unit in state.own)
            key += ":" + (unit.source != null ? unit.source.GetHashCode().ToString() : "-");
        if(key == lastBattleKey && lastAttackScores.Count > 0) return;
        lastBattleKey = key;
        lastAttackScores.Clear();
        var baseline = new BattleForecast { useReserve = true };
        lastStopScore = ScoreAfterBattle(state, baseline);
        for(int i = 0; i < state.own.Count; i++)
        {
            var unit = state.own[i];
            if(unit.source == null || unit.tapped || unit.sick || !CanAttack(unit.data)) continue;
            // Each first attack gets a fresh budget, so list order cannot starve a winning move.
            var forecast = new BattleForecast(baseline.position) { useReserve = true };
            lastAttackScores[unit.source] = ForecastAttack(state, i, forecast, 6);
        }
    }

    void ConsiderSummon(StrategicState state, float value, StrategicSearch context)
    {
        if(value > context.best) { context.best = value; context.choice = state; }
        if(state.first < 0) return;
        string action = state.first + ":" + (state.firstBase != null ? state.firstBase.data.cardName : "") +
            ":" + (state.firstTarget != null ? state.firstTarget.data.cardName : "");
        if(!context.finalists.TryGetValue(action, out var old) || value > old.Item1)
            context.finalists[action] = Tuple.Create(value, state);
    }

}
