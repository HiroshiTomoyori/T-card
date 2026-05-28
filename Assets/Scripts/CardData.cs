using UnityEngine;

public enum EffectType
{
    None,

    ChargeTopDeck,
    Draw1,
    DiscardEnemyHand,
    RecoverWall,
    JokerClearBattleArea,
    BlockOnly,

    BreakableJoker,
    BreakableFace,
    BreakableJack,

    InstantDestroySelf,
    TapAllEnemyBattle,
    DestroyOneEnemyBattle,
    SpecialSummonK,

    NoSummonSickness,
    CannotAttack,
    DoubleWallBreak,
    ShieldTrigger,
    JokerExtraTurn
}

public enum Suit
{
    Spade,
    Heart,
    Club,
    Diamond,
    Joker
}



[CreateAssetMenu(fileName = "CardData", menuName = "T-card/Card")]
public class CardData : ScriptableObject
{
    public string cardName;

    public Suit suit;
    public Sprite artwork;

    public int cost;

    // 非表示内部値
    public int power;

    [Header("Special Effects")]
    public EffectType[] effectTypes;

    public void SetPowerFromName()
    {
        string value = cardName;

        if(string.IsNullOrEmpty(value))
            value = name;

        if(value.Contains("A"))
        {
            power = 1;
            return;
        }

        if(value.Contains("J"))
        {
            power = 11;
            return;
        }

        if(value.Contains("Q"))
        {
            power = 12;
            return;
        }

        if(value.Contains("K"))
        {
            power = 13;
            return;
        }

        for(int i = 10; i >= 2; i--)
        {
            if(value.Contains(i.ToString()))
            {
                power = i;
                return;
            }
        }

        power = 0;

        Debug.LogWarning(
            "Power取得失敗：" + value
        );
    }

    public void SetCostFromName()
    {
        string n = cardName;

        if(n.Contains("Joker"))
        {
            cost = 13;
            return;
        }

        if(n.Contains("A"))
        {
            cost = 4;
            return;
        }

        if(n.Contains("K"))
        {
            cost = 13;
            return;
        }

        if(n.Contains("Q"))
        {
            cost = 12;
            return;
        }

        if(n.Contains("J"))
        {
            cost = 11;
            return;
        }

        if(n.Contains("10"))
        {
            cost = 10;
            return;
        }

        for(int i = 2; i <= 9; i++)
        {
            if(n.Contains(i.ToString()))
            {
                cost = i;
                return;
            }
        }

        cost = 0;
    }
}