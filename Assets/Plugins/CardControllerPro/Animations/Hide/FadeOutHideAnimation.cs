using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Fade-out hide animation. Fades the card out to alpha 0.
/// </summary>
[Serializable]
[AddTypeMenu("FadeOut", 100)]
public class FadeOutHideAnimation : IHideAnimation
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease easing = Ease.OutQuad;

    public void Hide(Card card, Action onComplete)
    {
        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        if (canvasGroup == null) {
            canvasGroup = card.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.DOKill();
        canvasGroup.alpha = 1f;
        canvasGroup.DOFade(0f, duration)
            .SetEase(easing)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime)
            .SetLink(card.gameObject)
            .OnComplete(() => onComplete?.Invoke());
    }
}

}
