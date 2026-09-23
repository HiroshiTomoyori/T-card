using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Reduces card alpha on pickup and restores it on putdown.
/// </summary>
[Serializable]
[AddTypeMenu("Transparency", 104)]
public class TransparencyPickupAnimation : IPickupAnimation
{
    [SerializeField] private float pickupAlpha = 0.6f;
    [SerializeField] private float duration = 0.15f;

    public void Pickup(Card card)
    {
        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(pickupAlpha, duration)
                .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
        }
    }

    public void PutDown(Card card)
    {
        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, duration)
                .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
        }
    }
}

}
