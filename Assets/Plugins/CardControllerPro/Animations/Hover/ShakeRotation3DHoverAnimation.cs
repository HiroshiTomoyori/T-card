using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Shakes the card rotation on hover for a wiggle effect.
/// </summary>
[Serializable]
[AddTypeMenu("Shake Rotation 3D", 101)]
public class ShakeRotation3DHoverAnimation : IHoverAnimation
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float strength = 10f;
    [SerializeField] private int vibrato = 6;
    [SerializeField] private float randomness = 45f;

    public void Hover(Card card)
    {
        card.cardHoverer.transform.DOKill();
        card.cardHoverer.transform.localRotation = Quaternion.identity;
        card.cardHoverer.transform.DOShakeRotation(duration, strength, vibrato, randomness)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }

    public void Unhover(Card card) { }
}

}
