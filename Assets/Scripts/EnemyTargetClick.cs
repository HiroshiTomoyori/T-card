using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyTargetClick : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if(turnManager == null)
            return;

        turnManager.SelectAttackTarget(gameObject);
    }
}