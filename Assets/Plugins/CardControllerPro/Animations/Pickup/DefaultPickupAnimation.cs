using System;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Default no-op pickup animation. Does nothing on pickup/putdown.
/// </summary>
[Serializable]
[AddTypeMenu("Default", 0)]
public class DefaultPickupAnimation : IPickupAnimation
{
    public void Pickup(Card card) { }
    public void PutDown(Card card) { }
}

}
