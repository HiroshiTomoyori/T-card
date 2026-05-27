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