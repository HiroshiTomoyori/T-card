using System;
using UnityEngine;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Fade Teleport attack animation - card fades out, appears at target, then returns.
/// Creates a magical/mystical teleportation effect for spell-casting cards.
/// </summary>
[Serializable]
[AddTypeMenu("Fade Teleport", 102)]
public class FadeTeleportAttackAnimation : IAttackAnimation
{
    [SerializeField] private float fadeOutDuration = 0.15f;
    [SerializeField] private float holdAtTargetDuration = 0.2f;
    [SerializeField] private float fadeInDuration = 0.15f;

    public void Attack(Card attacker, Transform target, Action onAttack, Action onComplete)
    {
        if (attacker == null || target == null) return;

        // Kill existing tweens to prevent conflicts
        attacker.cardVisuals.transform.DOKill();
        attacker.SetAttackingState(true);

        // Get or add CanvasGroup for alpha control
        CanvasGroup canvasGroup = attacker.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = attacker.gameObject.AddComponent<CanvasGroup>();
        }

        // Store original position and alpha
        Vector3 originalPosition = attacker.cardVisuals.transform.position;
        Vector3 originalLocalPosition = attacker.cardVisuals.transform.localPosition;
        float originalAlpha = canvasGroup.alpha;

        // Create teleport sequence
        Sequence teleportSequence = DOTween.Sequence();

        // 1. Fade out at origin
        teleportSequence.Append(
            canvasGroup.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.OutQuad)
        );

        // 2. Move to target instantly (while invisible)
        teleportSequence.AppendCallback(() => {
            attacker.cardVisuals.transform.position = target.position;
        });

        // 3. Fade in at target
        teleportSequence.Append(
            canvasGroup.DOFade(1f, fadeInDuration * 0.5f)
                .SetEase(Ease.InQuad)
        );

        // 4. Hold at target briefly
        teleportSequence.AppendCallback(() => onAttack?.Invoke());
        teleportSequence.AppendInterval(holdAtTargetDuration);

        // 5. Fade out at target
        teleportSequence.Append(
            canvasGroup.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.OutQuad)
        );

        // 6. Move back to origin instantly (while invisible)
        teleportSequence.AppendCallback(() => {
            attacker.cardVisuals.transform.localPosition = originalLocalPosition;
        });

        // 7. Fade in at origin
        teleportSequence.Append(
            canvasGroup.DOFade(originalAlpha, fadeInDuration)
                .SetEase(Ease.InQuad)
        );

        // Invoke callback when complete
        teleportSequence.AppendCallback(() => {
            attacker.SetAttackingState(false);
            onComplete?.Invoke();
        });

        teleportSequence.SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
}

}
