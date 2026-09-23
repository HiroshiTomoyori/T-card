using System;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Defines how a card animates when attacking a target.
/// Implementations must be [Serializable] for Inspector dropdown.
/// </summary>
public interface IAttackAnimation
{
    /// <summary>
    /// Animate card attack to target and return to origin.
    /// </summary>
    /// <param name="attacker">The card performing the attack</param>
    /// <param name="target">Transform of the target (Card, zone, etc.)</param>
    /// <param name="onComplete">Callback when animation finishes (may be null)</param>
    void Attack(Card attacker, Transform target, Action onAttack, Action onComplete);
}

}
