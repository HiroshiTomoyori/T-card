using System;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Defines how a card animates when hidden after being removed from a card group.
/// Implementations must be [Serializable] for Inspector dropdown.
/// </summary>
public interface IHideAnimation
{
    /// <summary>
    /// Animate hiding the card.
    /// </summary>
    /// <param name="card">The card being hidden</param>
    /// <param name="onComplete">Callback when animation finishes (may be null)</param>
    void Hide(Card card, Action onComplete);
}

}
