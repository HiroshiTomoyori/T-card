using UnityEngine;

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager I;

    void Awake()
    {
        I = this;
    }

public void ActivateOnSummon(CardController card)
{
    if(card == null)
        return;

    if(card.data == null)
        return;

    if(card.data.effectTypes == null)
        return;

    HandDealer handDealer =
        FindFirstObjectByType<HandDealer>();

    TurnManager turnManager =
        FindFirstObjectByType<TurnManager>();

    bool instantDestroySelf = false;

    foreach(EffectType effect in card.data.effectTypes)
    {
        switch(effect)
        {
            case EffectType.None:
                break;

            case EffectType.ChargeTopDeck:
                if(handDealer != null)
                {
                    Debug.Log("効果発動：山札上をリソースへ");
                    handDealer.ChargeTopDeckToResource();
                }
                break;

            case EffectType.Draw1:
                if(handDealer != null)
                {
                    Debug.Log("効果発動：1ドロー");
                    handDealer.DrawOneCard();
                }
                break;

            case EffectType.DiscardEnemyHand:
                if(handDealer != null)
                {
                    Debug.Log("効果発動：敵手札ランダム墓地");
                    handDealer.DiscardRandomEnemyHand();
                }
                break;

            case EffectType.RecoverWall:
                if(handDealer != null)
                {
                    Debug.Log("効果発動：Wall回復");
                    handDealer.RecoverWallFromDeck();
                }
                break;

            case EffectType.JokerClearBattleArea:
                if(turnManager != null)
                {
                    Debug.Log("効果発動：JOKER 全体墓地送り");
                    turnManager.JokerClearBattleArea(card);
                }
                break;

            case EffectType.TapAllEnemyBattle:

                if(turnManager != null)
                {
                    Debug.Log("効果発動：敵全体タップ");

                    turnManager.TapAllEnemyBattle(card);
                }

                break;


            case EffectType.DestroyOneEnemyBattle:

            if(turnManager != null)
            {
                Debug.Log("効果発動：敵1体破壊選択開始");

                turnManager.StartSelectEnemyBattleToDestroy(card);
            }

                break;

            case EffectType.InstantDestroySelf:
                instantDestroySelf = true;
                break;
        }
    }

    if(instantDestroySelf)
    {
        if(turnManager != null)
        {
            Debug.Log("効果発動：インスタンスカード消滅");
            turnManager.SendCardToOwnGraveyard(card);
        }
    }
}

    public void ActivateOnAttack(CardController card)
    {
        if(card == null || card.data == null)
            return;

        Debug.Log("攻撃時効果チェック：" + card.data.name);
    }

    public void ActivateOnDestroy(CardController card)
    {
        if(card == null || card.data == null)
            return;

        Debug.Log("破壊時効果チェック：" + card.data.name);
    }

    public void ActivateOnBlock(CardController card)
    {
        if(card == null || card.data == null)
            return;

        Debug.Log("ブロック時効果チェック：" + card.data.name);
    }
}