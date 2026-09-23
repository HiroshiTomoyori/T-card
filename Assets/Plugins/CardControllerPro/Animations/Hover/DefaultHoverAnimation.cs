using System;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Default no-op hover animation. Does nothing on hover/unhover.
/// </summary>
[Serializable]
[AddTypeMenu("None", 0)]
public class NoneHoverAnimation : IHoverAnimation
{
    public void Hover(Card card) { }
    public void Unhover(Card card) { }
}

}
