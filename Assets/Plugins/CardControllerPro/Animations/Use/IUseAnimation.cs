using System;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Defines how a card animates when used (before destruction).
/// Implementations must be [Serializable] for Inspector dropdown.
/// </summary>
public interface IUseAnimation
{
    /// <summary>
    /// Animate the card's use effect.
    /// </summary>
    /// <param name="card">The card being used</param>
    /// <param name="target">Transform of the targeted card (null for non-targeted Use)</param>
    /// <param name="onComplete">Callback when animation finishes (must always be invoked)</param>
    void Use(Card card, Transform target, Action onComplete);
}

}
