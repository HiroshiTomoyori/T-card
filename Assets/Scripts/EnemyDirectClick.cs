using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyDirectClick : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    TurnManager turnManager;

    void Start()
    {
        turnManager =
            FindFirstObjectByType<TurnManager>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(turnManager == null)
            return;

        if(!turnManager.IsSelectingTarget())
            return;

        if(!turnManager.CanDirectAttack())
            return;

        turnManager.ShowAttackArrowTo(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(turnManager == null)
            return;

        turnManager.HideAttackArrow();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(turnManager == null)
            return;

        if(!turnManager.CanDirectAttack())
        {
            Debug.Log("敵ウォールが残っているため直接攻撃不可");
            return;
        }

        turnManager.SelectDirectAttackTarget(gameObject);
    }
}