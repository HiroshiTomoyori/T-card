using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Fade-in animation. Snaps card to target position then fades it in from alpha 0.
/// </summary>
[Serializable]
[AddTypeMenu("FadeIn", 103)]
public class FadeInPlaceAnimation : IPlaceAnimation
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease easing = Ease.InQuad;

    public void Place(Card card, Vector3 target, Action onComplete)
    {
        card.transform.position = target;

        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        if (canvasGroup == null) {
            canvasGroup = card.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.DOKill();
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, duration)
            .SetEase(easing)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime)
            .SetLink(card.gameObject)
            .OnComplete(() => onComplete?.Invoke());
    }
}

}
