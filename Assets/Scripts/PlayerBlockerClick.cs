using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerBlockerClick :
    MonoBehaviour,
    IPointerClickHandler
{
public void OnPointerClick(
    PointerEventData eventData
)
{
    TurnManager turnManager =
        FindFirstObjectByType<TurnManager>();

    if(turnManager == null)
        return;

    // 攻撃対象選択中以外は無効
    if(!turnManager.IsSelectingTarget())
        return;

    turnManager.SelectAttackTarget(
        gameObject
    );
}
}