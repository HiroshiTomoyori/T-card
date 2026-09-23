using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Instant animation implementation. Snaps card to position immediately
/// while killing any existing tweens to prevent conflicts.
/// </summary>
[Serializable]
[AddTypeMenu("Instant", 101)]
public class InstantPlaceAnimation : IPlaceAnimation
{
    public void Place(Card card, Vector3 target, Action onComplete)
    {
        // Kill any existing tweens to prevent conflicts
        card.transform.DOKill();

        // Snap to position immediately
        card.transform.position = card.destinationPosition;
        card.transform.rotation = card.destinationRotation;

        card.Unhover();

        // CRITICAL: Always invoke callback to prevent game logic stalls
        onComplete?.Invoke();
    }
}

}
