using UnityEngine;

public class BattlePhaseManager : MonoBehaviour
{
    bool isBattlePhase = false;

    public bool IsBattlePhase
    {
        get { return isBattlePhase; }
    }

    public void StartBattlePhase()
    {
        CardController[] cards =
            FindObjectsByType<CardController>(
                FindObjectsSortMode.None
            );

        foreach(CardController card in cards)
        {
            if(card.isTapped)
                continue;

            card.SetAttackable(true);
        }

        Debug.Log("攻撃可能カードを光らせた");
    }

    public void EndBattlePhase()
    {
        isBattlePhase = false;

        Debug.Log("=== Battle Phase 終了 ===");
    }
}