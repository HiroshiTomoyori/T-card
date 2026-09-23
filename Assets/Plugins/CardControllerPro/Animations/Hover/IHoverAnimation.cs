namespace CCP
{

/// <summary>
/// Defines how a card animates on hover and unhover.
/// Implementations must be [Serializable] for Inspector dropdown.
/// </summary>
public interface IHoverAnimation
{
    void Hover(Card card);
    void Unhover(Card card);
}

}
