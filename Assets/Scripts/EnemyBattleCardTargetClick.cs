using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyBattleCardTargetClick :
    MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    CardController card;
    TurnManager turnManager;

    void Start()
    {
        card = GetComponent<CardController>();

        turnManager =
            FindFirstObjectByType<TurnManager>();
    }

    public void OnPointerClick(
        PointerEventData eventData
    )
    {
        if(turnManager == null)
            return;

        if(card == null)
            return;

        // Aの破壊対象選択中なら最優先
        if(turnManager.TrySelectDestroyTarget(card))
            return;

        if(!turnManager.IsSelectingTarget())
            return;

        if(!card.isTapped)
            return;

        turnManager.SelectEnemyBattleCardTarget(
            gameObject
        );
    }

    public void OnPointerEnter(
        PointerEventData eventData
    )
    {
        if(turnManager == null)
            return;

        if(card == null)
            return;

        if(!turnManager.IsSelectingTarget())
            return;

        if(!card.isTapped)
            return;

        turnManager.ShowAttackArrowTo(
            gameObject
        );
    }

    public void OnPointerExit(
        PointerEventData eventData
    )
    {
        if(turnManager == null)
            return;

        if(!turnManager.IsSelectingTarget())
            return;

        turnManager.HideAttackArrow();
    }
}