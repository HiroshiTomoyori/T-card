using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Shakes the card rotation on the Z-axis on pickup for a wiggle effect.
/// </summary>
[Serializable]
[AddTypeMenu("Shake Rotation 2D", 101)]
public class ShakeRotation2DPickupAnimation : IPickupAnimation
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float strength = 10f;
    [SerializeField] private int vibrato = 6;
    [SerializeField] private float randomness = 45f;

    public void Pickup(Card card)
    {
        card.cardPickuper.transform.DOKill();
        card.cardPickuper.transform.localRotation = Quaternion.identity;
        card.cardPickuper.transform.DOShakeRotation(duration, new Vector3(0, 0, strength), vibrato, randomness)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }

    public void PutDown(Card card) { }
}

}
