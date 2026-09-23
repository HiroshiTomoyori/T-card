namespace CCP
{

/// <summary>
/// Defines how a card animates on select and deselect.
/// Implementations must be [Serializable] for Inspector dropdown.
/// </summary>
public interface ISelectAnimation
{
    void Select(Card card);
    void Deselect(Card card);
}

}
