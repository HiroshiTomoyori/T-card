# CardControllerPro

A Unity UI toolkit for card-based games (TCG, deckbuilder, poker, etc.). Provides animated card hands, fan/straight layouts, hover/pickup/select/attack/placement animations, drag-and-drop reordering, card flipping, targeting arrows, and a drop-zone system. All UI is Canvas/UGUI-based.

**Unity Version:** Unity 6
**Render Pipeline:** URP 17.3
**Dependencies:** DOTween/DOTweenPro (Demigiant), SerializeReferenceExtensions (MackySoft, vendored), Unity Input System 1.18, Unity UGUI 2.0

---

## Architecture

Component-based Unity architecture. Every animation category uses the **Strategy pattern**: animations are plain `[Serializable]` C# classes (not MonoBehaviours), selected at design-time via `[SubclassSelector]` Inspector dropdowns (powered by `SerializeReferenceExtensions`). Card data is separated from behavior via `CardData` ScriptableObject.

### Core Scripts

| Script | Type | Purpose |
|--------|------|---------|
| `Card.cs` | MonoBehaviour | Card controller: pointer events, drag, flip, tilt, attack targeting, spring physics |
| `CardGroup.cs` | MonoBehaviour | Hand/zone manager: fan/straight layout, hover/select routing, card ordering |
| `CardData.cs` | ScriptableObject | Data container + animation references + virtual lifecycle hooks |
| `CardDropZone.cs` | MonoBehaviour | UGUI drop target: moves/uses cards between groups on drop |
| `CardGroupInitializer.cs` | MonoBehaviour | Scene bootstrap: instantiates card prefabs into groups with optional delay |
| `CardControllerSettings.cs` | ScriptableObject | Global settings (singleton via Resources). Only field: `useUnscaledTime` (bool, default false) |
| `CardHealth.cs` | MonoBehaviour | Runtime health tracking, damage/heal, auto-destroy on death |
| `DynamicArrow.cs` | MonoBehaviour | Pooled dotted arrow for attack/targeting visuals (namespace: `CardControllerPro`) |
| `DampedSpring.cs` | [Serializable] | Spring-damper math for drag-rotation wobble |

### Enums

**CardType** (`CardType.cs`): `Place`, `Attack`, `Static`, `Use`, `TargettedUse`
**OnClickAction** (`OnClickAction.cs`): `Nothing`, `Flip`, `Custom`, `Select`

### Card Prefab Hierarchy

```
Card (RectTransform, CanvasGroup, Card.cs)
 cardTilter
    cardDragRotator
       cardFlipper
          cardPickuper
             cardHoverer
                cardVisuals
                   front
                   back
                   border
             selectionMark
          DynamicArrow
```

All card visual effects target specific hierarchy nodes: tilt -> `cardTilter`, drag spring rotation -> `cardDragRotator`, flip -> `cardFlipper`, pickup scale -> `cardPickuper`, hover scale -> `cardHoverer`.

### Namespaces

Most core scripts are in the **global namespace** (`Card`, `CardGroup`, `CardData`, all animation classes). Only `DynamicArrow` and `IPointable` use the `CardControllerPro` namespace.

---

## CardGroup API

`CardGroup` manages a list of `Card` objects with automatic layout and animation.

### Serialized Fields

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| `cardSpacing` | float | 1 | Horizontal spacing between cards |
| `fanRadius` | float | 0 | 0 = straight line; nonzero = arc/fan layout. Positive = curves down, negative = curves up |
| `yOffsetPerCard` | float | 0 | Vertical stacking offset per card index |
| `zOffsetPerCard` | float | 0 | Z stacking offset per card index (controls render order) |
| `cardScale` | float | 1 | Scale applied to each card in this group |
| `anchorToFirstCard` | bool | false | When true, positions start from card 0 instead of centering |
| `tweenEaseType` | Ease | OutQuad | DOTween ease for card movement |
| `tweenDuration` | float | 0.3 | Duration of position/rotation tweens |
| `hoverYOffset` | float | 0.5 | Y lift on hover |
| `hoverZOffset` | float | 0.2 | Z shift on hover |
| `makeRoomOnHover` | bool | true | Push neighboring cards aside on hover |
| `hoverSpacing` | float | 0.5 | How far neighbors shift when making room |
| `hoverAnimation` | IHoverAnimation | ScaleUpHoverAnimation | Visual effect on hover |
| `onClickAction` | OnClickAction | Nothing | What happens on card click |
| `onClickCustom` | UnityEvent\<Card\> | - | Fired when `onClickAction == Custom` |
| `selectAnimation` | ISelectAnimation | LiftSelectAnimation | Visual effect on selection |
| `showSelectionMark` | bool | false | Show/hide the selectionMark GameObject |
| `allowPickup` | bool | true | Allow dragging cards from this group |
| `resetRotationOnPickup` | bool | false | Reset card rotation when picked up |
| `allowReorderingDuringDrag` | bool | true | Allow card reordering within the group during drag |
| `reorderYThreshold` | float | 200 | Max Y distance (local) for reordering to apply |
| `isTargettable` | bool | true | Can cards in this group be attack targets |
| `isPlayerHand` | bool | false | Marks group as the player hand (affects CardType resolution) |
| `AttackingCardParent` | Transform | null | Reparent cards here during attack animations (for render order) |
| `enableTilt` | bool | true | Enable idle/hover tilt effect |
| `tiltTowardsCursor` | bool | true | Tilt direction on hover |
| `autoTiltAmount` | float | 5 | Idle tilt amplitude (degrees) |
| `manualTiltAmount` | float | 15 | Hover tilt amplitude (degrees) |
| `tiltSpeed` | float | 40 | Tilt lerp speed |

### Key Methods

```csharp
// Add cards (optional IPlaceAnimation overrides the add animation)
void AddFront(Card card, IPlaceAnimation placeAnimation = null)
void AddBack(Card card, IPlaceAnimation placeAnimation = null)
void AddAtPosition(Card card, int index, IPlaceAnimation placeAnimation = null)

// Access
Card Front()   // first card, or null
Card Back()    // last card, or null

// Remove cards (optional IHideAnimation plays before onComplete)
Card RemoveFront(IHideAnimation hideAnimation = null, Action<Card> onComplete = null)
Card RemoveBack(IHideAnimation hideAnimation = null, Action<Card> onComplete = null)
Card RemoveAtPosition(int index, IHideAnimation hideAnimation = null, Action<Card> onComplete = null)
void RemoveCard(Card card, IHideAnimation hideAnimation = null, Action<Card> onComplete = null)

// Selection
void SelectCard(Card card)          // internal
void DeselectCard(Card card)        // internal
bool IsSelected(Card card)
List<Card> GetSelectedCards()
List<Card> GetNotSelectedCards()

// Other
void Shuffle()
void RecalculateAndAnimatePositions(Card skipAnimating = null)
void ReorderCardDuringDrag(Card card, int newIndex)
```

### Runtime State

- `cards` (List\<Card\>) - public, the ordered list of cards in this group
- `hoveredCardIndex` (int) - index of currently hovered card, or -1

---

## Card API

`Card` is the core controller for a single card. Handles all Unity pointer events.

### Inspector Fields

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| `cardData` | CardData | null | ScriptableObject with card config + animations |
| `cardTilter` | GameObject | - | Hierarchy node for tilt rotation |
| `cardDragRotator` | GameObject | - | Hierarchy node for drag spring rotation |
| `cardFlipper` | GameObject | - | Hierarchy node for Y-axis flip |
| `cardHoverer` | GameObject | - | Hierarchy node for hover visual effects |
| `cardPickuper` | GameObject | - | Hierarchy node for pickup visual effects |
| `cardVisuals` | GameObject | - | Container for front/back/border |
| `front` | GameObject | - | Front face of the card |
| `back` | GameObject | - | Back face of the card |
| `border` | GameObject | - | Targeting border highlight |
| `selectionMark` | GameObject | - | Selection indicator |
| `dynamicArrow` | DynamicArrow | - | Targeting arrow (uses `IPointable` interface, namespace `CardControllerPro`) |
| `canTargetOwnCardGroup` | bool | false | Allow targeting cards in same group |
| `flipDuration` | float | 0.1 | Half-flip animation duration |
| `enableDragSpring` | bool | true | Enable spring physics during drag |
| `dragSpring` | DampedSpring | default | Spring settings (stiffness=150, damping=10) |
| `dragSpringInputScale` | float | 20 | Mouse delta multiplier for spring input |
| `dragSpringMaxAngle` | float | 25 | Max spring rotation angle |

### Static State (shared across all cards)

```csharp
static Action<bool> OnCardMoved;  // fired true on drag start, false on drag end
static Card movingCard;           // currently being dragged
static Card isTargetting;         // card in attack/targeting mode
static Card targetedCard;         // currently highlighted target
static bool stopOnHover;          // temporary hover suppression
```

### Instance Events (Action delegates)

```csharp
Action onPickedUp;                // pointer down
Action onDropped;                 // drag ended without placement
Action onPlaced;                  // placed on field via drop zone
Action onTargetingStarted;        // attack/targeting drag started
Action<Card> onTargetedUse;       // targeted use completed (param: target card)
Action<Card> onAttackHit;         // attack hit moment (param: target card)
Action<CardGroup> onUsed;         // use completed (param: drop zone group)
Action onTargeted;                // this card was targeted by another
Action onUntargeted;              // this card was untargeted
```

### Key Methods

```csharp
void Initialize(CardData data)                    // assign data, call InitializeCard
void MoveToPosition(Vector3 pos, Quaternion rot,
    Ease ease, float duration, float? scale)       // animated move (called by CardGroup)
void MoveToGroup(CardGroup newGroup, IPlaceAnimation placeAnimation = null) // remove from current, add to new
void MoveToGroupAtPosition(CardGroup newGroup, int index, IPlaceAnimation placeAnimation = null) // remove from current, insert at index
void SetCardGroup(CardGroup group)                 // set parent group reference
void Hover() / void Unhover()                      // hover state management
void Targeted() / void Untargeted()                // targeting highlight
void FlipCard()                                    // animated Y-axis flip
void FlipCardNoAnimation()                         // instant flip
void ExecuteAttack(Transform target, Action onComplete)    // play attack animation
void ExecuteUse(CardGroup dropZoneGroup)                   // play use animation + destroy
void ExecuteTargettedUse(Card target)                      // play targeted use + destroy
void PlayPlaceAnimation()                                  // play place animation
void PlayUseAnimation(CardGroup dropZoneGroup)             // alias for ExecuteUse
void StartTrackingMouse() / void StopTrackingMouse()       // manual mouse tracking
void DisallowOnHover()                                     // suppress hover briefly (0.3s)
void SetAttackingState(bool attacking)                     // set isAttacking flag
```

### Runtime Properties

- `parentCardGroup` (CardGroup) - the group this card belongs to
- `destinationPosition` / `destinationRotation` - target set by CardGroup layout
- `isBeingMovedManually` (bool) - true during drag, prevents tween overrides
- `isBeingPlaced` / `isAttacking` / `isBeingUsed` (bool) - animation state flags

### CardType Resolution Logic

The effective `CardType` is resolved at runtime, not just from `cardData.cardType`:
- If `cardData` is null, defaults to `Place`
- If `cardData.placeableFromHand` is true AND the card is in a group with `isPlayerHand == true` AND `cardType` is not `Use`/`TargettedUse`, the effective type becomes `Place`
- Otherwise uses `cardData.cardType`

This means a card configured as `Attack` in its CardData will behave as `Place` when dragged from the player hand (if `placeableFromHand` is true), allowing attack cards to be placed onto the board first.

### Drag Behavior by CardType

| Effective Type | Drag Start | During Drag | Drag End |
|----------------|-----------|-------------|----------|
| **Place** | Detaches from group, disables raycasts | Moves with mouse, reorders within group | Snaps back to group or drops on CardDropZone |
| **Use** | Same as Place | Same as Place | If dropped on CardDropZone, plays use animation + destroys |
| **Attack** | Starts targeting mode, shows arrow | Arrow follows mouse | Finds target via raycast, plays attack animation |
| **TargettedUse** | Same as Attack | Same as Attack | Finds target, plays use animation on target + destroys |
| **Static** | Blocked | Blocked | Blocked |

---

## CardData API

Base ScriptableObject. Subclass to add game-specific data and behavior. Create via Assets > Create > CardControllerPro > Card Data.

### Serialized Fields

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| `cardName` | string | - | Display name |
| `description` | string | - | Card description (TextArea) |
| `frontArt` | Sprite | null | Front face image (applied to front's Image component) |
| `backArt` | Sprite | null | Back face image (applied to back's Image component) |
| `cardType` | CardType | Place | Determines drag/interaction behavior |
| `placeableFromHand` | bool | true | Override type to Place when in player hand |
| `autoDestroyOnUse` | bool | true | Auto-destroy after Use/TargettedUse animation (shown only for Use/TargettedUse types) |
| `placeAnimation` | IPlaceAnimation | DefaultPlaceAnimation | Place animation strategy |
| `attackAnimation` | IAttackAnimation | ArcAttackAnimation | Attack animation strategy |
| `pickupAnimation` | IPickupAnimation | DefaultPickupAnimation | Pickup/putdown animation strategy |
| `useAnimation` | IUseAnimation | NoneUseAnimation | Use animation strategy |

### Virtual Lifecycle Methods (override in subclasses)

```csharp
virtual void InitializeCard(Card card)             // called when data assigned; sets front/back sprites
virtual void OnPickedUp(Card card)                 // pointer down on card
virtual void OnDropped(Card card)                  // drag ended without placing
virtual void OnPlaced(Card card)                   // card placed on field via drop zone
virtual void OnTargetingStart(Card card)           // attack/targeting drag began
virtual void OnAttackHit(Card attacker, Card target)      // attack impact moment
virtual void OnUsed(Card card, CardGroup target)          // use card completed
virtual void OnTargetedUse(Card card, Card target)        // targeted use completed
```

### Null-Safe Animation Accessors

`PlaceAnimationSafe`, `AttackAnimationSafe`, `PickupAnimationSafe`, `UseAnimationSafe` - return default instances if the field is null.

---

## CardDropZone API

Receives dragged cards. Activates/deactivates automatically when `Card.OnCardMoved` fires.

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| `activateWhenMovingCards` | bool | true | Auto-show when a card is being dragged |
| `cardGroup` | CardGroup | - | Target group to add dropped cards to |
| `allowDroppingCardsFromDestinationGroup` | bool | false | Allow dropping cards already in the target group |

On drop: if the card is a Use type, calls `card.PlayUseAnimation(cardGroup)`. Otherwise calls `card.parentCardGroup.RemoveCard(card)`, `cardGroup.AddBack(card)`, `card.PlayPlaceAnimation()`.

---

## Animation Strategy System

All animations are `[Serializable]` classes implementing an interface. They appear in Inspector dropdowns via `[SubclassSelector]` and are named via `[AddTypeMenu("Name", order)]`.

### Interfaces

```csharp
// IPlaceAnimation - configured on CardData
void Place(Card card, Vector3 target, Action onComplete)

// IAttackAnimation - configured on CardData
void Attack(Card attacker, Transform target, Action onAttack, Action onComplete)

// IHoverAnimation - configured on CardGroup
void Hover(Card card)
void Unhover(Card card)

// IPickupAnimation - configured on CardData
void Pickup(Card card)
void PutDown(Card card)

// ISelectAnimation - configured on CardGroup
void Select(Card card)
void Deselect(Card card)

// IUseAnimation - configured on CardData
void Use(Card card, Transform target, Action onComplete)

// IHideAnimation - used as parameter in RemoveFront/RemoveBack/RemoveAtPosition
void Hide(Card card, Action onComplete)
```

### Built-in Implementations

**Place** (on CardData, field: `placeAnimation`):

| Class | AddTypeMenu | Fields | Notes |
|-------|-------------|--------|-------|
| `DefaultPlaceAnimation` | "Default" (0) | none | No-op, instant snap |
| `InstantPlaceAnimation` | "Instant" (101) | none | Kills tweens, snaps to destination, calls Unhover |
| `SwirlPlaceAnimation` | "Swirl" (100) | `duration=0.4f` | 360 rotation + scale pulse + bounce |
| `DealPlaceAnimation` | "Deal" (102) | `duration=0.6f, easing=Linear` | Z-axis spin. **BUG: never calls onComplete, never resets isBeingPlaced** |
| `FadeInPlaceAnimation` | "FadeIn" (103) | `duration=0.3f, easing=InQuad` | Fade from alpha 0 |
| `LinearSlidePlaceAnimation` | "LinearSlide" (102) | `duration=0.3f, easing=Linear` | Smooth linear slide to destination |
| `FromBelowPlaceAnimation` | "From Below" (200) | `duration=0.5f, shakeStrength=15f, shakeVibrato=10` | Scale from 0 + punch rotation |
| `CustomCurvePlaceAnimation` | "Custom Curve" (108) | `duration=0.3f, positionCurve, scaleCurve` | AnimationCurve-driven |
| `CustomDotweenPlaceAnimation` | "Custom DOTween" (109) | `duration=0.3f, positionEase, scaleMultiplier=1f, scaleEase` | Separate DOTween eases for position/scale |

**Attack** (on CardData, field: `attackAnimation`):

| Class | AddTypeMenu | Fields | Notes |
|-------|-------------|--------|-------|
| `ArcAttackAnimation` | "Arc" (100) | `duration=2f` | Bezier curve to target, scales 1.3x at impact |
| `QuickDashAttackAnimation` | "Quick Dash" (101) | `dashDuration=1f, holdDuration=0.1f, returnDuration=1f, easing=InOutQuad` | Linear dash + hold + return |
| `FadeTeleportAttackAnimation` | "Fade Teleport" (102) | `fadeOutDuration=0.15f, holdAtTargetDuration=0.2f, fadeInDuration=0.15f` | Fade out, teleport, fade in |
| `ImpactStrikeAttackAnimation` | "Impact Strike" (103) | `tiltAngle=25f, scalePunch=1.3f, duration=0.3f` | Tilt + scale punch in-place (no movement) |
| `CustomCurveAttackAnimation` | "Custom Curve" (105) | Approach: `duration, positionCurve, scaleCurve`; Return: same | Full AnimationCurve control for both phases |
| `CustomDotweenAttackAnimation` | "Custom DOTween" (106) | Approach: `duration=0.1f, positionEase, scaleMultiplier, scaleEase`; Return: same with `duration=0.3f` | DOTween ease for both phases |

**Hover** (on CardGroup, field: `hoverAnimation`):

| Class | AddTypeMenu | Fields | Notes |
|-------|-------------|--------|-------|
| `NoneHoverAnimation` | "None" (0) | none | No-op. File: `DefaultHoverAnimation.cs` |
| `ScaleUpHoverAnimation` | "Scale Up" (102) | `scaleMultiplier=1.15f, duration=0.2f` | Scales cardHoverer |
| `PunchScaleHoverAnimation` | "Punch Scale" (100) | `punchScale=(0.1,0.1,0), duration=0.3f, vibrato=6, elasticity=0.5f` | Punch on hover only |
| `ShakeRotation2DHoverAnimation` | "Shake Rotation 2D" (101) | `duration=0.3f, strength=10f, vibrato=6, randomness=45f` | Z-axis shake |
| `ShakeRotation3DHoverAnimation` | "Shake Rotation 3D" (101) | `duration=0.3f, strength=10f, vibrato=6, randomness=45f` | 3D rotation shake |

**Pickup** (on CardData, field: `pickupAnimation`):

| Class | AddTypeMenu | Fields | Notes |
|-------|-------------|--------|-------|
| `DefaultPickupAnimation` | "Default" (0) | none | No-op |
| `ScaleUpPickupAnimation` | "Scale Up" (103) | `scaleMultiplier=1.15f, duration=0.2f` | Scales cardPickuper |
| `PunchScalePickupAnimation` | "Punch Scale" (100) | `punchScale=(0.1,0.1,0), duration=0.3f, vibrato=6, elasticity=0.5f` | Punch on pickup |
| `ShakeRotation2DPickupAnimation` | "Shake Rotation 2D" (101) | `duration=0.3f, strength=10f, vibrato=6, randomness=45f` | Z-axis shake |
| `ShakeRotation3DPickupAnimation` | "Shake Rotation 3D" (102) | `duration=0.3f, strength=10f, vibrato=6, randomness=45f` | 3D rotation shake |
| `TransparencyPickupAnimation` | "Transparency" (104) | `pickupAlpha=0.6f, duration=0.15f` | Fades CanvasGroup alpha |

**Select** (on CardGroup, field: `selectAnimation`):

| Class | AddTypeMenu | Fields | Notes |
|-------|-------------|--------|-------|
| `NoneSelectAnimation` | "None" (0) | none | No-op |
| `ScaleSelectAnimation` | "Scale" (1) | `scaleMultiplier=1.15f, duration=0.2f` | Scales cardHoverer |
| `LiftSelectAnimation` | "Lift" (2) | `liftAmount=50f, duration=0.2f` | Lifts cardHoverer on Y |

**Use** (on CardData, field: `useAnimation`):

| Class | AddTypeMenu | Fields | Notes |
|-------|-------------|--------|-------|
| `NoneUseAnimation` | "None" (0) | none | Immediate onComplete |
| `DashUseAnimation` | "Dash" (101) | `dashDuration=0.25f, easing=InQuart` | Dash to target or scale to 0 |
| `ShakeUseAnimation` | "Shake" (100) | `duration=0.4f, strength=15f, vibrato=10, randomness=45f` | Z-axis shake before completion |

**Hide** (passed as parameter to Remove methods):

| Class | AddTypeMenu | Fields | Notes |
|-------|-------------|--------|-------|
| `FadeOutHideAnimation` | "FadeOut" (100) | `duration=0.3f, easing=OutQuad` | Fades CanvasGroup to 0 |
| `ShrinkHideAnimation` | "Shrink" (200) | `duration=0.5f, shakeStrength=15f, shakeVibrato=10` | Scale to 0 + punch rotation |

---

## How to Create a New Animation Strategy

1. Create a `[Serializable]` class implementing the appropriate interface (e.g., `IPlaceAnimation`)
2. Add `[AddTypeMenu("Category/Name", order)]` attribute for the Inspector dropdown label
3. Add serialized fields for configurable parameters
4. Implement the interface method(s). **Always invoke `onComplete` when the animation finishes** (for interfaces that have it)
5. Use DOTween for animations. Call `.SetUpdate(CardControllerSettings.Instance.useUnscaledTime)` on tweens to respect the pause setting
6. No registration needed - `SerializeReferenceExtensions` discovers implementations automatically via reflection

### Which hierarchy node to animate

| Effect | Target | Access |
|--------|--------|--------|
| Hover scale/shake | `card.cardHoverer.transform` | via `card.originalVisualsScale` for restore |
| Pickup scale/shake | `card.cardPickuper.transform` | via `card.originalPickuperScale` for restore |
| Whole card move/scale | `card.transform` | |
| Flip | `card.cardFlipper.transform` | Y-axis rotation |
| Tilt | `card.cardTilter.transform` | managed by Card's Update |
| Drag spring | `card.cardDragRotator.transform` | managed by Card's Update |
| Front/back swap | `card.front` / `card.back` | SetActive toggle |
| CanvasGroup alpha | `card.GetComponent<CanvasGroup>()` | |

### Example

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
            .Append(card.transform.DOScale(Vector3.one * bounceStrength, duration * 0.5f).SetEase(Ease.OutQuad))
            .Append(card.transform.DOScale(Vector3.one, duration * 0.5f).SetEase(Ease.InQuad))
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime)
            .OnComplete(() => {
                card.isBeingPlaced = false;
                onComplete?.Invoke();
            });
    }
}
```

---

## Responding to Card Lifecycle Events

Two mechanisms, both fire on the same actions. CardData virtual method runs first, then the Card Action event.

**Option 1: Override CardData virtual methods** (best for card-intrinsic logic):

```csharp
[CreateAssetMenu(menuName = "CardControllerPro/My Card Data")]
public class MyCardData : CardData {
    public override void InitializeCard(Card card) {
        base.InitializeCard(card); // sets front/back sprites
        // custom initialization
    }
    public override void OnPlaced(Card card) { }
    public override void OnAttackHit(Card attacker, Card target) { }
    public override void OnUsed(Card card, CardGroup target) { }
    public override void OnTargetedUse(Card card, Card target) { }
    public override void OnPickedUp(Card card) { }
    public override void OnDropped(Card card) { }
    public override void OnTargetingStart(Card card) { }
}
```

**Option 2: Subscribe to Card Action events** (best for external systems):

```csharp
card.onPlaced += () => { };
card.onAttackHit += (Card target) => { };
card.onUsed += (CardGroup dropZone) => { };
card.onTargetedUse += (Card target) => { };
card.onPickedUp += () => { };
card.onDropped += () => { };
card.onTargetingStarted += () => { };
card.onTargeted += () => { };       // this card became a target
card.onUntargeted += () => { };     // this card lost target status
```

| Event | CardData Virtual | Card Action | Parameter |
|-------|-----------------|-------------|-----------|
| Initialized | `InitializeCard(Card)` | - | - |
| Picked up | `OnPickedUp(Card)` | `onPickedUp` | - |
| Dropped | `OnDropped(Card)` | `onDropped` | - |
| Targeting started | `OnTargetingStart(Card)` | `onTargetingStarted` | - |
| Placed | `OnPlaced(Card)` | `onPlaced` | - |
| Attack hit | `OnAttackHit(Card, Card)` | `onAttackHit` | target Card |
| Used | `OnUsed(Card, CardGroup)` | `onUsed` | drop zone CardGroup |
| Targeted use | `OnTargetedUse(Card, Card)` | `onTargetedUse` | target Card |
| Became target | - | `onTargeted` | - |
| Lost target | - | `onUntargeted` | - |

---

## Common Workflows

### Spawning cards at runtime

```csharp
// Single card
GameObject cardObj = Instantiate(cardPrefab);
Card card = cardObj.GetComponent<Card>();
card.Initialize(myCardData);
cardGroup.AddBack(card, new LinearSlidePlaceAnimation());

// Batch via CardGroupInitializer (configure in Inspector)
cardGroupInitializer.Initialize();
```

### Moving a card between groups

```csharp
card.MoveToGroup(targetGroup); // removes from current, adds to new with AddBack
// or manually:
sourceGroup.RemoveCard(card);
targetGroup.AddBack(card, new SwirlPlaceAnimation());
```

### Removing cards with animation

```csharp
cardGroup.RemoveFront(new FadeOutHideAnimation(), (card) => {
    Destroy(card.gameObject);
});
```

### Dealing cards with delay

Use `CardGroupInitializer` with `delay > 0` in the Inspector, or write a coroutine:
```csharp
IEnumerator DealCards(CardGroup source, CardGroup dest, int count, float delay) {
    for (int i = 0; i < count; i++) {
        Card card = source.Back();
        card.MoveToGroup(dest);
        card.FlipCard();
        yield return new WaitForSeconds(delay);
    }
}
```

### Programmatic selection

```csharp
// Set up: cardGroup.onClickAction = OnClickAction.Select
List<Card> selected = cardGroup.GetSelectedCards();
List<Card> notSelected = cardGroup.GetNotSelectedCards();
```

### Using CardHealth

```csharp
// Add CardHealth component to card prefab alongside Card
CardHealth health = card.GetComponent<CardHealth>();
health.Initialize(20); // 20 max HP
health.TakeDamage(5);
health.Heal(3);
health.OnHealthChanged += (current, max) => { };
health.OnDied += () => { };
CardHealth.OnCardDied += (deadCardHealth) => { }; // static, fires for any card death
```

---

## CardGroupInitializer

Spawns cards into groups on Start. Configure entirely in Inspector.

```csharp
[Serializable]
public struct CardEntry {
    public GameObject cardPrefab;  // must have Card component
    public CardData cardData;      // assigned via card.Initialize()
    public int count;              // number of copies (min 1)
}

[Serializable]
public struct CardGroupEntry {
    public CardGroup cardGroup;
    public List<CardEntry> cards;
    public bool shuffle;                        // shuffle after all cards added
    public bool flipped;                        // flip cards face-down
    public float delay;                         // seconds between cards (0 = instant)
    public IPlaceAnimation placeAnimation;      // animation per card (null = InstantPlaceAnimation)
}
```

---

## Utility Classes

### ShowIfAttribute

Conditional Inspector field visibility. Use on serialized fields:
```csharp
[ShowIf("boolFieldName")]                              // show when bool is true
[ShowIf("enumField", (int)MyEnum.Value)]               // show when enum matches
[ShowIf("enumField", (int)MyEnum.A, (int)MyEnum.B)]   // show when enum matches either (OR)
[ShowIf("enumField", (int)MyEnum.A, OrField = "boolField")]  // enum match OR bool is true
```

### DampedSpring

Spring-damper for physics-based animation. Fields: `stiffness` (float, 150), `damping` (float, 10). Methods: `Update(dt)`, `AddVelocity(impulse)`, `Reset()`, `IsAtRest()`.

### Vector3Extensions

Extension method: `vector.WithZ(float z)` - returns new Vector3 with replaced Z.

### CardControllerSettings

Singleton ScriptableObject (loaded from Resources folder). Create via Assets > Create > CardControllerPro > Settings. Place in a `Resources/` folder.

Field: `useUnscaledTime` (bool, false) - when true, all DOTween animations use unscaled time (continue during Time.timeScale = 0).

---

## Demo/Sample Scripts (not for production)

| Script | Purpose |
|--------|---------|
| `CardTester.cs` | Dev helper: spawns cards into groups on Start |
| `DemoCardDealer.cs` | Deals one card from source to destination with flip |
| `PokerDemoCardDealer.cs` | Poker-style dealing: 2 cards per player, then community cards |
| `CardConfirmerDemo.cs` | Shows BalatroCardValue on selected cards, then removes all with hide animation |

### Demo CardData Subclasses

| Class | CreateAssetMenu | Purpose |
|-------|----------------|---------|
| `AttackCardData` | CardControllerPro/Attack Card Data | Has `damage` field, applies damage via CardHealth on attack hit, shows health on place |
| `BanditCardData` | CardControllerPro/Bandit Card Data | Shows health display on initialize |
| `FireballCardData` | CardControllerPro/Fireball Card Data | Has `damage` field, applies damage via CardHealth on targeted use |
| `RandomCardData` | CardControllerPro/Random Card Data | Randomly picks from `CardVariant[]` (sprite + value) on initialize |

### BalatroCardValue

Component for cards with numeric point values. Fields: `value` (int), `valueText` (GameObject with Text). Method: `ShowValue()` - displays "+value" text with shake animation.

---

## Editor Scripts

| Script | Purpose |
|--------|---------|
| `Editor/CardEditor.cs` | Custom editor for `Card`: shows warning when `cardData` is null |
| `Editor/ShowIfDrawer.cs` | PropertyDrawer for `ShowIfAttribute`: conditional field visibility |

---

## Known Issues / Technical Debt

- **`DealPlaceAnimation` is broken**: never calls `onComplete`, never resets `isBeingPlaced`. Callbacks will hang. Do not use until fixed.
- **`QuickDashAttackAnimation`**: the `holdDuration` field is serialized but unused in the actual tween.
- **Static global state** (`Card.isTargetting`, `Card.movingCard`, `Card.stopOnHover`, `Card.targetedCard`): only one card can be targeted/dragged globally. Breaks in multi-scene or multiplayer setups.
- **`InvokeRepeating("RecalculateCardOrder", 0.05f, 0.05f)`** in `CardGroup.Start()`: runs at 20 Hz unconditionally, even with no moving cards.
- **No namespace on core scripts**: `Card`, `CardGroup`, `CardData`, all animations in global namespace. Risk of naming collisions.
- **`DemoCardDealer.cs`**: unused `using Unity.VisualScripting` import.
- **`DefaultHoverAnimation.cs`** filename contains class `NoneHoverAnimation` - naming mismatch.

## Code Conventions

- Animation strategies: `[Serializable]`, implement interface, add `[AddTypeMenu]` attribute
- DOTween for all animations; call `.SetUpdate(CardControllerSettings.Instance.useUnscaledTime)` on sequences/tweens
- `[ShowIf]` attribute for conditional Inspector fields
- `CardData` virtual hooks + `Card` Action events = two extension points for game logic
- Use `CardGroupInitializer` for production card spawning, `CardTester` for dev only
- All card UI is Canvas/UGUI-based (RectTransform, CanvasGroup, Image)
