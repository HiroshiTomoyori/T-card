using UnityEngine;
using UnityEngine.UI;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Base ScriptableObject for card configuration. Subclass this to add game-specific data and logic.
/// Create instances via Assets > Create > CardControllerPro > Card Data.
/// </summary>
[CreateAssetMenu(fileName = "NewCardData", menuName = "CardControllerPro/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardName;
    [TextArea] public string description;
    public Sprite frontArt;
    public Sprite backArt;

    [Header("Card Type")]
    public CardType cardType;
    public bool placeableFromHand = true;
    [ShowIf("cardType", (int)CardType.Use, (int)CardType.TargettedUse)]
    [Tooltip("If false, the card will not be destroyed after use — you must destroy it manually.")]
    public bool autoDestroyOnUse = true;

    [Header("Animations")]
    [ShowIf("cardType", (int)CardType.Place, OrField = "placeableFromHand")]
    [SerializeReference, SubclassSelector] public IPlaceAnimation placeAnimation = new DefaultPlaceAnimation();
    [ShowIf("cardType", (int)CardType.Attack)]
    [SerializeReference, SubclassSelector] public IAttackAnimation attackAnimation = new ArcAttackAnimation();
    [ShowIf("placeableFromHand")]
    [SerializeReference, SubclassSelector] public IPickupAnimation pickupAnimation = new DefaultPickupAnimation();
    [ShowIf("cardType", (int)CardType.Use, (int)CardType.TargettedUse)]
    [SerializeReference, SubclassSelector] public IUseAnimation useAnimation = new NoneUseAnimation();

    /// <summary>
    /// Null-safe accessor for placeAnimation. Returns DefaultPlaceAnimation if field is null.
    /// </summary>
    public IPlaceAnimation PlaceAnimationSafe =>
        placeAnimation ?? new DefaultPlaceAnimation();

    /// <summary>
    /// Null-safe accessor for attackAnimation. Returns ArcAttackAnimation if field is null.
    /// </summary>
    public IAttackAnimation AttackAnimationSafe =>
        attackAnimation ?? new ArcAttackAnimation();

    /// <summary>
    /// Null-safe accessor for pickupAnimation. Returns DefaultPickupAnimation if field is null.
    /// </summary>
    public IPickupAnimation PickupAnimationSafe =>
        pickupAnimation ?? new DefaultPickupAnimation();

    /// <summary>
    /// Null-safe accessor for useAnimation. Returns NoneUseAnimation if field is null.
    /// </summary>
    public IUseAnimation UseAnimationSafe =>
        useAnimation ?? new NoneUseAnimation();

    /// <summary>
    /// Called when asset is first created or Reset from context menu.
    /// Ensures default animation is always set.
    /// </summary>
    private void Reset() {
        placeAnimation = new DefaultPlaceAnimation();
        attackAnimation = new ArcAttackAnimation();
        pickupAnimation = new DefaultPickupAnimation();
        useAnimation = new NoneUseAnimation();
    }

    /// <summary>
    /// Called when ScriptableObject loads or Inspector values change.
    /// Ensures animation field is never null for existing assets.
    /// </summary>
    private void OnValidate() {
        if (placeAnimation == null) {
            placeAnimation = new DefaultPlaceAnimation();
        }

        if (attackAnimation == null) {
            attackAnimation = new ArcAttackAnimation();
        }

        if (pickupAnimation == null) {
            pickupAnimation = new DefaultPickupAnimation();
        }

        if (useAnimation == null) {
            useAnimation = new NoneUseAnimation();
        }

    }

    // --- Virtual lifecycle methods for subclasses ---

    /// <summary>
    /// Called when CardData is assigned to a Card via Initialize().
    /// Override to set up card visuals, stats, etc.
    /// </summary>
    public virtual void InitializeCard(Card card) {
        if (frontArt != null && card.front != null) {
            Image frontImage = card.front.GetComponent<Image>();
            if (frontImage != null) frontImage.sprite = frontArt;
        }

        if (backArt != null && card.back != null) {
            Image backImage = card.back.GetComponent<Image>();
            if (backImage != null) backImage.sprite = backArt;
        }
    }

    /// <summary>
    /// Called when the card is picked up (pointer down).
    /// </summary>
    public virtual void OnPickedUp(Card card) { }

    /// <summary>
    /// Called when a dragged card is dropped without being placed on a drop zone.
    /// </summary>
    public virtual void OnDropped(Card card) { }

    /// <summary>
    /// Called when the card is placed onto the field.
    /// </summary>
    public virtual void OnPlaced(Card card) { }

    /// <summary>
    /// Called when the card begins attack or targetted-use targeting (drag starts on an Attack/TargettedUse card).
    /// </summary>
    public virtual void OnTargetingStart(Card card) { }

    /// <summary>
    /// Called at the moment an attack hits the target.
    /// </summary>
    public virtual void OnAttackHit(Card attacker, Card target) { }
    
    /// <summary>
    /// Called when a Use card completes its use animation, just before destruction.
    /// </summary>
    /// <param name="card">The card being used</param>
    /// <param name="target">The CardGroup of the drop zone the card was used on</param>
    public virtual void OnUsed(Card card, CardGroup target) { }

    /// <summary>
    /// Called when a TargetedUse card completes its use animation, just before destruction.
    /// </summary>
    /// <param name="card">The card being used</param>
    /// <param name="target">The card that was targeted</param>
    public virtual void OnTargetedUse(Card card, Card target) { }
}

}
