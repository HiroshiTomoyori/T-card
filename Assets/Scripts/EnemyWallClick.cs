using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyWallClick :
    MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public void OnPointerEnter(
        PointerEventData eventData
    )
    {
        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if(turnManager == null)
            return;

        if(!turnManager.IsSelectingTarget())
            return;

        turnManager.ShowAttackArrowTo(
            gameObject
        );
    }

    public void OnPointerExit(
        PointerEventData eventData
    )
    {
        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if(turnManager == null)
            return;

        if(!turnManager.IsSelectingTarget())
            return;

        turnManager.HideAttackArrow();
    }

    public void OnPointerClick(
        PointerEventData eventData
    )
    {
        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if(turnManager == null)
            return;

        if(!turnManager.IsSelectingTarget())
        {
            Debug.Log(
                "攻撃対象選択中ではないのでWall無効"
            );

            return;
        }

        turnManager.SelectAttackTarget(
            gameObject
        );
    }
}