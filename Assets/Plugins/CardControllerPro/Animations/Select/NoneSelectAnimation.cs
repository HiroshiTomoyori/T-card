using System;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// No-op select animation. Tracks selection state without any visual feedback.
/// </summary>
[Serializable]
[AddTypeMenu("None", 0)]
public class NoneSelectAnimation : ISelectAnimation
{
    public void Select(Card card) { }
    public void Deselect(Card card) { }
}

}
