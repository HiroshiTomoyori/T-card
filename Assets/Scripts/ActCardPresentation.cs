using UnityEngine;

// Keep ownership for effect targeting without displaying an act as a field card.
[DefaultExecutionOrder(1000)]
public sealed class ActCardPresentation : MonoBehaviour
{
    public static void HideOnField(CardController card)
    {
        if (card == null) return;
        var view = card.GetComponent<ActCardPresentation>() ?? card.gameObject.AddComponent<ActCardPresentation>();
        view.Apply();
    }
    void LateUpdate() { Apply(); }
    void Apply()
    {
        if (transform.parent == null || transform.parent.GetComponent<BattleAreaLayout>() == null) return;
        var group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0;
        group.blocksRaycasts = group.interactable = false;
    }
}
