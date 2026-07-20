using UnityEngine;
using UnityEngine.EventSystems;

public class BattleCardClick : MonoBehaviour, IPointerClickHandler
{
    CardController card;

    void Awake()
    {
        card = GetComponent<CardController>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 左クリック以外は無視
        if(eventData.button != PointerEventData.InputButton.Left)
            return;

        GameObject playerBattleArea =
            GameObject.Find("PlayerBattleArea");

        // プレイヤーのバトルエリアにないカードは無視
        if(playerBattleArea == null ||
           !transform.IsChildOf(playerBattleArea.transform))
        {
            return;
        }

        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if(turnManager == null)
            return;

        if(turnManager.IsWaitingBlockSelect())
        {
            turnManager.SelectBlocker(gameObject);
            return;
        }

        turnManager.StartBattleByAttackSelect(gameObject);
    }
}