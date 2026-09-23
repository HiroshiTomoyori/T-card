using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Punches the card scale on hover for a quick pop effect.
/// </summary>
[Serializable]
[AddTypeMenu("Punch Scale", 100)]
public class PunchScaleHoverAnimation : IHoverAnimation
{
    [SerializeField] private Vector3 punchScale = new Vector3(0.1f, 0.1f, 0f);
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private int vibrato = 6;
    [SerializeField] private float elasticity = 0.5f;
    
    public void Hover(Card card)
    {
        card.cardHoverer.transform.DOKill();
        card.cardHoverer.transform.localScale = Vector3.one;
        card.cardHoverer.transform.DOPunchScale(punchScale, duration, vibrato, elasticity)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }

    public void Unhover(Card card) { }
}

}
