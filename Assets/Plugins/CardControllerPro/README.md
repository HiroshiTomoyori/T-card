# Card Controller Pro

A drag-and-drop card system for Unity UI. Build TCGs, deckbuilders, poker games, or anything with cards. Handles hand layout, animations, drag-and-drop, targeting, flipping, and more -- all inside a Canvas.

## Requirements

- Unity 6+
- DOTween or DOTweenPro
- Unity Input System

## Quick Start

### 1. Set Up a Card Prefab

Use the included `Card.prefab` or create your own. The easiest way would be to duplicate the Card.prefab and then modify it to your liking. The card needs this GameObject hierarchy:

```
Card                   <- RectTransform + CanvasGroup + Card component
  cardTilter           <- idle/hover tilt
    cardDragRotator    <- spring wobble during drag
      cardFlipper      <- Y-axis flip rotation
        cardPickuper   <- pickup animation target
          cardHoverer  <- hover animation target
            cardVisuals
              front    <- front face (Image)
              back     <- back face (Image)
              border   <- targeting highlight (hidden by default)
          selectionMark <- selection indicator (hidden by default)
        DynamicArrow   <- dotted arrow for targeting (optional)
```

Each layer in the hierarchy is animated independently, so they don't interfere with each other.

Assign each GameObject to the matching field on the `Card` component in the Inspector.

### 2. Create a CardData Asset

Right-click in the Project window: **Create > CardControllerPro > Card Data**.

Configure:
- **Card Name / Description** -- display info
- **Front Art / Back Art** -- sprites for card faces
- **Card Type** -- determines how the card behaves when dragged (see [Card Types](#card-types))
- **Animations** -- pick a Place, Attack, Pickup, and Use animation from the dropdowns

### 3. Set Up a CardGroup

Add the `CardGroup` component to an empty GameObject inside your Canvas. This is where cards live -- think of it as a "hand", "deck", "field zone", etc.

Key settings:
- **Card Spacing** -- horizontal distance between cards
- **Fan Radius** -- set to 0 for a straight line, or a value like 800 for a curved fan hand
- **Card Scale** -- scale of cards in this group
- **Allow Pickup** -- whether cards can be dragged from this group

### 4. Populate Cards

**Option A: CardGroupInitializer (Inspector-driven, recommended)**

Add `CardGroupInitializer` to any GameObject. In the Inspector, add entries specifying which prefab, CardData, and count to spawn into which CardGroup. Runs automatically on Start.

**Option B: Code**

```csharp
GameObject cardObj = Instantiate(cardPrefab);
Card card = cardObj.GetComponent<Card>();
card.Initialize(myCardData);
myCardGroup.AddBack(card);
```

### 5. Set Up Drop Zones (optional)

To let players drag cards from one group to another, add `CardDropZone` to a UI element (e.g. an Image). Assign the target `CardGroup`. The drop zone automatically appears when a card is being dragged, and hides otherwise.

---

## Card Types

Set the card type on the `CardData` asset. This controls what happens when the player drags the card.

| Type | Drag Behavior |
|------|---------------|
| **Place** | Card follows the mouse. Drop on a `CardDropZone` to move it to another group. |
| **Attack** | Card stays in place. A targeting arrow appears. Release over a valid target to trigger an attack animation. |
| **Use** | Card follows the mouse. Drop on a `CardDropZone` to play a use animation, then the card is destroyed. |
| **TargettedUse** | Targeting arrow (like Attack). Release over a target to play a use animation on that target, then the card is destroyed. |
| **Static** | Card cannot be dragged at all. |

### Placeable From Hand

The `placeableFromHand` toggle on CardData (default: true) changes how Attack cards behave in the player's hand. When enabled, Attack cards dragged from a group marked `isPlayerHand` will behave as Place cards instead -- allowing players to drag attack cards onto the board, where they then function as attack cards.

---

## Layout Options

All layout is configured per `CardGroup`:

**Straight line** (default): Set `fanRadius = 0`. Cards are spaced evenly with `cardSpacing`.

**Fan/arc**: Set `fanRadius` to a non-zero value (e.g. 800). Positive = curves down, negative = curves up. `cardSpacing` controls how spread out the fan is.

**Stacked/deck**: Set `cardSpacing = 0`, and use `yOffsetPerCard` and/or `zOffsetPerCard` for slight visual offsets. Enable `anchorToFirstCard` to stack from the first card position instead of centering.

---

## Animations

Animations are configured through Inspector dropdowns. Each dropdown shows all available options for that animation type.

### Place Animations

Configured on **CardData**. Plays when a card is placed onto a drop zone.

| Animation | Description |
|-----------|-------------|
| Default | No animation, instant snap |
| Instant | Kills active tweens, snaps to position |
| Swirl | 360-degree rotation with scale pulse |
| LinearSlide | Smooth slide to destination |
| FadeIn | Fades in from transparent |
| From Below | Scales up from zero with a shake |
| Custom Curve | Drive position and scale with your own AnimationCurves |
| Custom DOTween | Separate DOTween Ease settings for position and scale |

### Attack Animations

Configured on **CardData**. Plays when a card attacks a target.

| Animation | Description |
|-----------|-------------|
| Arc | Curved bezier path to target with scale punch on impact |
| Quick Dash | Fast linear dash to target and back |
| Fade Teleport | Fade out, appear at target, fade back in |
| Impact Strike | Tilt and scale punch in-place (no movement) |
| Custom Curve | AnimationCurve control for approach and return phases |
| Custom DOTween | DOTween Ease control for approach and return phases |

### Hover Animations

Configured on **CardGroup**. Plays when a card is hovered.

| Animation | Description |
|-----------|-------------|
| None | No visual effect |
| Scale Up | Scales the card up |
| Punch Scale | Quick punch scale pop |
| Shake Rotation 2D | Z-axis wiggle |
| Shake Rotation 3D | 3D rotation shake |

The CardGroup also handles the positional hover effect (lifting the card up and pushing neighbors aside). These are controlled by `hoverYOffset`, `hoverZOffset`, `makeRoomOnHover`, and `hoverSpacing`.

### Pickup Animations

Configured on **CardData**. Plays when a card is clicked/pressed.

| Animation | Description |
|-----------|-------------|
| Default | No visual effect |
| Scale Up | Scales the card up while held |
| Punch Scale | Quick punch pop on pickup |
| Shake Rotation 2D | Z-axis wiggle on pickup |
| Shake Rotation 3D | 3D rotation shake on pickup |
| Transparency | Fades card to semi-transparent while held |

### Select Animations

Configured on **CardGroup**. Plays when a card is selected (requires `onClickAction = Select`).

| Animation | Description |
|-----------|-------------|
| None | No visual effect |
| Scale | Scales the card up while selected |
| Lift | Lifts the card upward while selected |

### Use Animations

Configured on **CardData**. Plays when a Use or TargettedUse card is activated.

| Animation | Description |
|-----------|-------------|
| None | No animation, immediately completes |
| Dash | Dashes toward the target (or shrinks if no target) |
| Shake | Shakes before completing |

### Hide Animations

Not configured in the Inspector. Instead, pass them as a parameter when removing cards from a group:

```csharp
cardGroup.RemoveFront(new FadeOutHideAnimation(), (card) => {
    Destroy(card.gameObject);
});
```

| Animation | Description |
|-----------|-------------|
| FadeOut | Fades to transparent |
| Shrink | Scales to zero with a shake |

---

## Adding and Removing Cards

### Adding

```csharp
cardGroup.AddBack(card);                                    // add to end
cardGroup.AddFront(card);                                   // add to front
cardGroup.AddAtPosition(card, 2);                           // add at index

// With a place animation override:
cardGroup.AddBack(card, new LinearSlidePlaceAnimation());
```

### Removing

```csharp
Card card = cardGroup.RemoveFront();                        // remove first
Card card = cardGroup.RemoveBack();                         // remove last
Card card = cardGroup.RemoveAtPosition(2);                  // remove at index
cardGroup.RemoveCard(card);                                 // remove specific card

// With a hide animation:
cardGroup.RemoveFront(new FadeOutHideAnimation(), (removedCard) => {
    Destroy(removedCard.gameObject);
});
```

### Moving Between Groups

```csharp
card.MoveToGroup(otherGroup);
card.MoveToGroup(otherGroup, new SwirlPlaceAnimation());
card.MoveToGroupAtPosition(otherGroup, 2);

// Or with more control:
sourceGroup.RemoveCard(card);
targetGroup.AddBack(card, new SwirlPlaceAnimation());
targetGroup.AddAtPosition(card, 2, new LinearSlidePlaceAnimation());
```

### Shuffling

```csharp
cardGroup.Shuffle();
```

---

## Click Actions

Set `onClickAction` on the CardGroup:

| Action | Behavior |
|--------|----------|
| **Nothing** | No click response |
| **Flip** | Flips the card face-up/face-down |
| **Select** | Toggles card selection (see below) |
| **Custom** | Fires the `onClickCustom` UnityEvent |

---

## Card Selection

Set `onClickAction = Select` on the CardGroup.

```csharp
// Get selected/unselected cards
List<Card> selected = cardGroup.GetSelectedCards();
List<Card> remaining = cardGroup.GetNotSelectedCards();

// Check a specific card
bool isSelected = cardGroup.IsSelected(card);
```

Optionally enable `showSelectionMark` on the CardGroup to show/hide the `selectionMark` GameObject on each card.

---

## Card Flipping

```csharp
card.FlipCard();              // animated flip with Y-axis rotation
card.FlipCardNoAnimation();   // instant flip
```

Both toggle between front and back faces. The card automatically detects rotation-based flips during animations.

---

## Drag-and-Drop Between Groups

1. Set up a **source** `CardGroup` with `allowPickup = true`
2. Add a `CardDropZone` component to a UI element
3. Assign a **destination** `CardGroup` to the drop zone's `cardGroup` field

The drop zone automatically shows when a card is being dragged and hides when the drag ends. When a card is dropped on it:
- **Place/Attack cards**: removed from source group, added to destination group, place animation plays
- **Use cards**: use animation plays, card is destroyed (if `autoDestroyOnUse` is true)

Set `allowDroppingCardsFromDestinationGroup = true` if cards should be droppable back into the same group they came from.

---

## Reordering Cards During Drag

Enabled by default (`allowReorderingDuringDrag = true` on CardGroup). When a player drags a card horizontally within its group, other cards slide aside and the card snaps into its new position on drop.

If the player drags the card too far vertically (beyond `reorderYThreshold`), reordering stops -- useful so that dragging a card toward a drop zone doesn't accidentally reorder the hand.

---

## Targeting Arrow

For Attack and TargettedUse cards, a dotted arrow automatically appears during drag. Set up the `DynamicArrow` component on your card prefab and assign `nodePrefab` (dot) and `headPrefab` (arrow tip).

Configure on the DynamicArrow:
- **Distance Per Node** -- spacing between dots
- **Max Nodes** -- pool limit
- **First/Last Node Scale** -- dots grow from start to end
- **Head Scale** -- arrow tip size

---

## Responding to Card Events

Two ways to add game logic when cards are interacted with:

### Option 1: Subclass CardData

Best for logic that belongs to a specific card type (e.g. "Fireball deals 5 damage on use").

```csharp
[CreateAssetMenu(menuName = "MyGame/Fireball Card")]
public class FireballCardData : CardData
{
    public int damage = 5;

    public override void OnTargetedUse(Card card, Card target)
    {
        var health = target.GetComponent<CardHealth>();
        health?.TakeDamage(damage);
    }
}
```

Available overrides:

| Method | When It Fires |
|--------|---------------|
| `InitializeCard(Card)` | Card data assigned via `Initialize()` |
| `OnPickedUp(Card)` | Card clicked / pointer down |
| `OnDropped(Card)` | Drag ended without placing on a drop zone |
| `OnPlaced(Card)` | Card placed onto a drop zone |
| `OnTargetingStart(Card)` | Attack/TargettedUse drag started |
| `OnAttackHit(Card attacker, Card target)` | Attack animation reaches the target |
| `OnUsed(Card, CardGroup)` | Use animation completed |
| `OnTargetedUse(Card, Card target)` | Targeted use animation completed |

Always call `base.InitializeCard(card)` if you override `InitializeCard` -- the base sets front/back art sprites.

### Option 2: Subscribe to Events

Best for external systems (score tracking, sound effects, UI updates).

```csharp
card.onPickedUp += () => audioSource.PlayOneShot(pickupSound);
card.onPlaced += () => scoreManager.AddPoints(10);
card.onAttackHit += (target) => SpawnHitVFX(target.transform.position);
card.onUsed += (dropZone) => DrawCards(2);
card.onTargetedUse += (target) => ApplyBuff(target);
card.onDropped += () => Debug.Log("Card dropped");
card.onTargetingStarted += () => ShowTargetingUI();
card.onTargeted += () => HighlightCard();       // this card became a target
card.onUntargeted += () => RemoveHighlight();   // this card lost target status
```

Both mechanisms fire together -- CardData override runs first, then the Action event.

---

## Card Health

Add the `CardHealth` component alongside `Card` on your card prefab.

```csharp
CardHealth health = card.GetComponent<CardHealth>();
health.Initialize(20);       // set max health
health.TakeDamage(5);
health.Heal(3);
health.SetHealth(10);        // set to exact value
```

When health reaches 0, the card is automatically removed from its group and destroyed.

Wire up a UI `Text` element to the `healthDisplay` field to show current/max health. Call `health.StartDisplayingHealth()` to show it.

Events:

```csharp
health.OnHealthChanged += (int current, int max) => { };
health.OnDied += () => { };
CardHealth.OnCardDied += (CardHealth deadCard) => { };  // static, fires for any card
```

---

## Dealing Cards with Delay

Use `CardGroupInitializer` with the `delay` field set to a positive value (in seconds). Cards will be added one at a time with that delay between each.

Or do it manually:

```csharp
IEnumerator DealCards(CardGroup source, CardGroup dest, int count, float delay)
{
    for (int i = 0; i < count; i++)
    {
        Card card = source.Back();
        card.MoveToGroup(dest);
        card.FlipCard();
        yield return new WaitForSeconds(delay);
    }
}
```

---

## Tilt Effect

Cards gently wobble when idle and tilt toward the cursor on hover. Configured per CardGroup:

- **Enable Tilt** -- toggle the whole effect
- **Tilt Towards Cursor** -- tilt toward or away from the mouse on hover
- **Auto Tilt Amount** -- idle wobble amplitude (degrees)
- **Manual Tilt Amount** -- hover tilt amplitude (degrees)
- **Tilt Speed** -- how fast the tilt interpolates

---

## Drag Spring

When dragging a Place or Use card, it wobbles based on mouse movement (like a spring). Configured per Card:

- **Enable Drag Spring** -- toggle the effect
- **Drag Spring Input Scale** -- mouse delta sensitivity
- **Drag Spring Max Angle** -- max rotation angle
- **Stiffness** -- spring return speed (higher = snappier)
- **Damping** -- how fast oscillation settles

---

## Global Settings

Create a settings asset: **Create > CardControllerPro > Settings**. Place it in a `Resources` folder so it loads automatically.

| Setting | Default | Description |
|---------|---------|-------------|
| `useUnscaledTime` | false | When true, all card animations ignore `Time.timeScale` (keep animating while paused) |

---

## Creating Custom Animations

You can create your own animations for any category. Here's a Place animation example:

```csharp
using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

[Serializable]
[AddTypeMenu("Bounce", 110)]
public class BouncePlaceAnimation : IPlaceAnimation
{
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float bounceStrength = 1.3f;

    public void Place(Card card, Vector3 target, Action onComplete)
    {
        card.isBeingPlaced = true;
        card.transform.DOKill();

        DOTween.Sequence()
            .Append(card.transform.DOScale(
                Vector3.one * bounceStrength, duration * 0.5f)
                .SetEase(Ease.OutQuad))
            .Append(card.transform.DOScale(
                Vector3.one, duration * 0.5f)
                .SetEase(Ease.InQuad))
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime)
            .OnComplete(() =>
            {
                card.isBeingPlaced = false;
                onComplete?.Invoke();
            });
    }
}
```

The steps are the same for any animation type:

1. Create a `[Serializable]` class implementing the interface (`IPlaceAnimation`, `IAttackAnimation`, `IHoverAnimation`, `IPickupAnimation`, `ISelectAnimation`, `IUseAnimation`, or `IHideAnimation`)
2. Add `[AddTypeMenu("Name", order)]` so it appears in the Inspector dropdown
3. Add `[SerializeField]` fields for any configurable parameters
4. **Always call `onComplete`** when the animation finishes (for interfaces that have a callback)
5. Call `.SetUpdate(CardControllerSettings.Instance.useUnscaledTime)` on DOTween sequences/tweens

The animation will automatically appear in the Inspector dropdown -- no registration needed.

### Animation Interfaces

```csharp
// Place -- configured on CardData
void Place(Card card, Vector3 target, Action onComplete)

// Attack -- configured on CardData
void Attack(Card attacker, Transform target, Action onAttack, Action onComplete)

// Hover -- configured on CardGroup
void Hover(Card card)
void Unhover(Card card)

// Pickup -- configured on CardData
void Pickup(Card card)
void PutDown(Card card)

// Select -- configured on CardGroup
void Select(Card card)
void Deselect(Card card)

// Use -- configured on CardData
void Use(Card card, Transform target, Action onComplete)

// Hide -- passed as parameter to Remove methods
void Hide(Card card, Action onComplete)
```

---

## Included Prefabs

| Prefab | Description |
|--------|-------------|
| `Card.prefab` | Standard card with full hierarchy |
| `TCGCard.prefab` | TCG-styled variant |
| `BalatroCard.prefab` | Balatro-styled variant with `BalatroCardValue` component |
| `Node.prefab` | Dot for the targeting arrow |
| `Head.prefab` | Arrow tip for the targeting arrow |

---

## Demo Scenes

The included demo scenes show different use cases. The demo scripts (`DemoCardDealer`, `PokerDemoCardDealer`, `CardConfirmerDemo`, `CardTester`) are for demonstration only -- use `CardGroupInitializer` for production card spawning.

---

## Creating an Assembly Definition for CardControllerPro

If you want to create an Assembly Definition (`.asmdef`) for CardControllerPro:

1. Open the **DOTween Utility Panel** (Tools > Demigiant > DOTween Utility Panel) and click the **Create ASMDEF** button (located just below the Setup button).

2. In the `CardControllerPro` directory, create an **Assembly Definition Reference** asset and add the following assembly references to it:
   - `MackySoft.SerializeReferenceExtensions`
   - `MackySoft.SerializeReferenceExtensions.Editor`
   - `Unity.InputSystem`
   - The DOTween assembly created in step 1

3. Click **Apply**.
