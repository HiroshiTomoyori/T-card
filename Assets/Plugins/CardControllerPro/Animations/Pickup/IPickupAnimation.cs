namespace CCP
{

/// <summary>
/// Defines how a card animates on pickup (drag start) and putdown (drag end).
/// Implementations must be [Serializable] for Inspector dropdown.
/// </summary>
public interface IPickupAnimation
{
    void Pickup(Card card);
    void PutDown(Card card);
}

}
