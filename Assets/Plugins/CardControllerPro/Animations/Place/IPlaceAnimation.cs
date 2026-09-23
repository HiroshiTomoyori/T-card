using System;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Defines how a card animates when placed at a target position.
/// Implementations must be [Serializable] for Inspector dropdown.
/// </summary>
public interface IPlaceAnimation
{
    /// <summary>
    /// Animate card placement to target position.
    /// </summary>
    /// <param name="card">The card being placed</param>
    /// <param name="target">World position to place at</param>
    /// <param name="onComplete">Callback when animation finishes (may be null)</param>
    void Place(Card card, Vector3 target, Action onComplete);
}

}
