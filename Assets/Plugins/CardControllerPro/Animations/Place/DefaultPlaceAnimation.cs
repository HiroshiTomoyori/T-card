using System;
using UnityEngine;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Default no-animation implementation. Snaps card to position immediately.
/// </summary>
[Serializable]
[AddTypeMenu("Default", 0)]  // Order 0 ensures first position in dropdown
public class DefaultPlaceAnimation : IPlaceAnimation
{
    public void Place(Card card, Vector3 target, Action onComplete)
    {
        // No animation - snap instantly to target
        card.transform.position = target;

        // CRITICAL: Always invoke callback to prevent game logic stalls
        onComplete?.Invoke();
    }
}

}
