using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Shakes the card rotation on all axes on pickup for a 3D wiggle effect.
/// </summary>
[Serializable]
[AddTypeMenu("Shake Rotation 3D", 102)]
public class ShakeRotation3DPickupAnimation : IPickupAnimation
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float strength = 10f;
    [SerializeField] private int vibrato = 6;
    [SerializeField] private float randomness = 45f;

    public void Pickup(Card card)
    {
        card.cardPickuper.transform.DOKill();
        card.cardPickuper.transform.localRotation = Quaternion.identity;
        card.cardPickuper.transform.DOShakeRotation(duration, strength, vibrato, randomness)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }

    public void PutDown(Card card) { }
}

}
