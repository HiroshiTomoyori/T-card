using System;
using UnityEngine;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// No-op use animation. Card is destroyed immediately without visual effect.
/// </summary>
[Serializable]
[AddTypeMenu("None", 0)]
public class NoneUseAnimation : IUseAnimation
{
    public void Use(Card card, Transform target, Action onComplete)
    {
        onComplete?.Invoke();
    }
}

}
