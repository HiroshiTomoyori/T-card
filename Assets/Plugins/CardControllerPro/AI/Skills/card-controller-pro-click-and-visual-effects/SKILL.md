---
name: "card-controller-pro-click-and-visual-effects"
description: "Use this skill whenever the user wants Card Controller Pro cards to react to clicks or gain built-in visual feedback — e.g. \"flip cards when i click them\", \"let players select cards\", \"run my callback on click\", \"make cards pop on hover\", \"add pickup feedback\", \"turn on the wobble\", or \"make card animations keep playing while paused\". Covers CCP.OnClickAction, animated and instant flips, user-click selection queries, built-in hover/pickup/select strategies, group tilt, card drag spring defaults, and unscaled animation time. Do NOT use for spawning, moving, shuffling, dealing, or removing cards (see card-controller-pro-manage-cards-at-runtime). When in doubt whether a card interaction or visual-feedback request could involve Card Controller Pro, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "Card Controller Pro"
  publisher: "Bartoshco Tools"
  asset-version: "1.0.4"
  skill-version: "1.0.0"
  unity: "6000.0+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/gui"
  asset-store-url: "https://assetstore.unity.com/packages/tools/gui/card-controller-pro-370860"
  documentation-url: "https://bartoshco.com/card-controller-pro/"
  support-url: "mailto:dannydeerboi@gmail.com"
  last-verified: "2026-08-29"
---

# Configure Card Controller Pro Clicks and Visual Effects

Configure each card group's click behavior and built-in interaction effects, then verify flips, player selection, hover/pickup feedback, tilt, drag spring motion, and pause-time animation without relying on internal APIs.

## When to use this skill

- The user asks to do nothing, flip, select, or invoke a custom event when a card is clicked.
- The user asks to flip a card from gameplay code, either animated or instantly.
- The user asks for selected/unselected card lists after player clicks.
- The user asks to change hover, pickup, or selection feedback using shipped effects.
- The user asks to tune idle/cursor tilt or the drag-rotation spring.
- The user wants supported DOTween effects to continue while `Time.timeScale` is zero.

Not for:

- Runtime spawning, adding, moving, shuffling, dealing, or removal; see `card-controller-pro-manage-cards-at-runtime`.
- Attack, targeted-use, drop-zone, or reordering behavior; use the matching Card Controller Pro sibling skill.
- Creating a new animation strategy; use the behavior-and-animations sibling skill.

## Prerequisites

- Card Controller Pro `1.0.4` in Unity `6000.0+`.
- A UGUI Canvas and EventSystem, with cards containing `CCP.Card` and belonging to a `CCP.CardGroup`.
- Card prefab hierarchy references assigned for the requested effect: `cardFlipper`, `front`, and `back` for flips; `cardHoverer` for hover/select; `cardPickuper` for pickup; `cardTilter` for tilt; and `cardDragRotator` for drag spring.
- Unity Input System and DOTween available, as required by the runtime source.
- Confirm installation before configuration:

```csharp
Debug.Log(typeof(CCP.Card).FullName);
Debug.Log(typeof(CCP.OnClickAction).FullName);
Debug.Log(typeof(CCP.ScaleUpHoverAnimation).FullName);
```

These must resolve in namespace `CCP`, and the Console must contain no compile errors. If they do not, tell the user the asset is missing or is the wrong version and provide https://assetstore.unity.com/packages/tools/gui/card-controller-pro-370860.

## Quick start

1. Select the GameObject with `CCP.CardGroup`.
2. In the Inspector, open **On Click Settings**.
3. Set **On Click Action** to **Flip**.
4. Confirm each member card has `cardFlipper`, `front`, and `back` assigned on its `CCP.Card` component.
5. Enter Play mode and click a card.

Expected result: each click toggles the visible face with a Y-axis flip, the card stays in its group, and the Console remains free of compile errors and unhandled exceptions.

## Workflows

### Workflow: Choose a click action

**Goal**

Configure one supported action for every card in a group.

**Steps**

1. Select the `CCP.CardGroup`.
2. Under **On Click Settings > On Click Action**, choose one `CCP.OnClickAction` value:
   - `Nothing`: no click action.
   - `Flip`: call the clicked card's animated flip.
   - `Custom`: invoke the group's `onClickCustom` UnityEvent with the clicked `CCP.Card`.
   - `Select`: toggle internal selection state, run the selected strategy, and optionally toggle the selection mark.
3. For `Custom`, add a persistent listener under **On Click Custom** or register one from code:

```csharp
using CCP;
using UnityEngine;

public sealed class CardClickListener : MonoBehaviour
{
    [SerializeField] private CardGroup group;

    private void Awake()
    {
        group.onClickAction = OnClickAction.Custom;
        group.onClickCustom.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        group.onClickCustom.RemoveListener(HandleClick);
    }

    private void HandleClick(Card card)
    {
        Debug.Log($"Clicked {card.name}", card);
    }
}
```

4. Do not combine actions by expecting the enum to run more than one branch. Put additional behavior in the custom listener when needed.

**Expected result**

Every pointer click follows exactly the selected enum branch, and `Custom` listeners receive the clicked card.

### Workflow: Flip cards from code

**Goal**

Toggle the visible card face with or without transition animation.

**Steps**

1. Ensure `cardFlipper`, `front`, and `back` are assigned.
2. Call `card.FlipCard()` for the animated two-half rotation. The serialized `flipDuration` defaults to `0.1` seconds per half.
3. Call `card.FlipCardNoAnimation()` for an immediate toggle.
4. Treat both methods as toggles, not setters; track desired face state in game logic if an idempotent "show front" operation is required.

```csharp
using CCP;

public static class CardFaceActions
{
    public static void ToggleAnimated(Card card) => card.FlipCard();
    public static void ToggleInstant(Card card) => card.FlipCardNoAnimation();
}
```

**Expected result**

Exactly one of `front` and `back` changes active state on each call, while animated flips return `cardFlipper` to zero local Y rotation.

### Workflow: Enable player-click selection

**Goal**

Let users select cards by clicking and let gameplay code query the result.

**Steps**

1. Set **On Click Action** to **Select** on the `CCP.CardGroup`.
2. Under **Selection Settings**, choose `None`, `Scale`, or `Lift`.
3. Optionally enable **Show Selection Mark** and assign each card's `selectionMark`.
4. Have the player click cards in Play mode.
5. Read copies of the current subsets:

```csharp
using System.Collections.Generic;
using CCP;

List<Card> selected = hand.GetSelectedCards();
List<Card> notSelected = hand.GetNotSelectedCards();
```

Never call `CardGroup.IsSelected`, `CardGroup.SelectCard`, or `CardGroup.DeselectCard`; all three are internal. The supported consumer flow is user clicks plus `GetSelectedCards()` and `GetNotSelectedCards()`.

**Expected result**

Clicks toggle the built-in selection state and visual effect, and the two query lists partition the cards currently in the group.

### Workflow: Choose built-in hover, pickup, and select effects

**Goal**

Apply shipped interaction feedback from the Inspector without writing a custom strategy.

**Steps**

1. On `CCP.CardGroup`, choose a **Hover Animation**:
   - `None`
   - `Scale Up` (`scaleMultiplier 1.15`, `duration 0.2`)
   - `Punch Scale` (`punchScale (0.1, 0.1, 0)`, `duration 0.3`, `vibrato 6`, `elasticity 0.5`)
   - `Shake Rotation 2D` or `Shake Rotation 3D` (`duration 0.3`, `strength 10`, `vibrato 6`, `randomness 45`)
2. On the card's `CCP.CardData`, choose a **Pickup Animation**:
   - `Default`
   - `Scale Up` (`scaleMultiplier 1.15`, `duration 0.2`)
   - `Punch Scale` (same defaults as hover punch)
   - `Shake Rotation 2D` or `Shake Rotation 3D` (same defaults as hover shake)
   - `Transparency` (`pickupAlpha 0.6`, `duration 0.15`)
3. On `CCP.CardGroup`, choose a **Select Animation**:
   - `None`
   - `Scale` (`scaleMultiplier 1.15`, `duration 0.2`)
   - `Lift` (`liftAmount 50`, `duration 0.2`)
4. Test combinations in Play mode. Hover and select both animate `cardHoverer`, so avoid combining competing scale strategies when stable restoration matters.

**Expected result**

Pointer hover, pickup/putdown, and selection/deselection invoke the selected built-in strategy and restore the relevant hierarchy node.

### Workflow: Tune group tilt and card drag spring

**Goal**

Adjust the built-in idle/cursor tilt and horizontal-drag wobble from their source defaults.

**Steps**

1. On `CCP.CardGroup`, inspect **Tilt Settings**:
   - `Enable Tilt`: `true`
   - `Tilt Towards Cursor`: `true`
   - `Auto Tilt Amount`: `5`
   - `Manual Tilt Amount`: `15`
   - `Tilt Speed`: `40`
2. Ensure every card has `cardTilter` assigned. Disable tilt at the group when no tilt node exists.
3. On `CCP.Card`, inspect **Drag Spring Settings**:
   - `Enable Drag Spring`: `true`
   - `DampedSpring.stiffness`: `150`
   - `DampedSpring.damping`: `10`
   - `Drag Spring Input Scale`: `20`
   - `Drag Spring Max Angle`: `25`
4. Treat those as new-component source defaults. The shipped `Card.prefab` overrides stiffness/damping to `1000`/`125`; duplicated prefabs retain those stronger values.
5. Ensure `cardDragRotator` is assigned. Drag spring activates only while dragging effective `Place` or `Use` cards.
6. Start from the actual serialized prefab values, then change one value at a time and verify return-to-rest behavior.

**Expected result**

Idle cards oscillate by the auto amount, hovered cards tilt relative to the pointer, and horizontal dragging produces a clamped spring rotation that returns to zero.

### Workflow: Continue effects while paused

**Goal**

Allow Card Controller Pro DOTween effects that honor global settings to run at `Time.timeScale = 0`.

**Steps**

1. Create or locate `CCP.CardControllerSettings` through **Assets > Create > CardControllerPro > Settings**.
2. Name it `CardControllerSettings` and place it anywhere under a `Resources` folder so `Resources.Load<CardControllerSettings>("CardControllerSettings")` can find it.
3. Enable **Use Unscaled Time**.
4. Pause with `Time.timeScale = 0` and trigger the configured effect.
5. Verify the specific built-in uses `CardControllerSettings.Instance.useUnscaledTime`; shipped hover, pickup, select, and flip tweens do.

This setting does not convert gameplay coroutines such as `CardGroupInitializer` delay, which uses `WaitForSeconds`.

**Expected result**

Supported effects continue to advance while scaled game time is paused.

## Verification

- In Play mode, click-action `Nothing`, `Flip`, `Custom`, and `Select` each produce only their defined behavior.
- For `Custom`, count listener invocations and verify one invocation per click with the clicked `CCP.Card`.
- For selection, verify `selected.Count + notSelected.Count == group.cards.Count`, no card appears in both lists, and state changes only after user clicks.
- For flipping, verify active front/back states toggle and no null-reference exception appears.
- For hover, pickup, and selection, verify the assigned hierarchy node changes during the effect and returns after unhover, putdown, or deselect.
- For spring, verify rotation never exceeds `dragSpringMaxAngle` and settles back to zero after drag.
- With unscaled time enabled and `Time.timeScale == 0`, verify a supported effect still progresses.
- Confirm no compile errors or unhandled exceptions appear in the Console.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `Nothing`, `Flip`, `Custom`, `Select` | `CCP.OnClickAction` | Selects the group's pointer-click behavior. |
| `onClickAction` / `onClickCustom` | `CCP.CardGroup` | Stores the action and custom UnityEvent. |
| `FlipCard()` / `FlipCardNoAnimation()` | `CCP.Card` | Toggles faces with or without a tween. |
| `GetSelectedCards()` / `GetNotSelectedCards()` | `CCP.CardGroup` | Returns copied user-selection query lists. |
| `hoverAnimation` / `selectAnimation` | `CCP.CardGroup` | Inspector-selected group strategies. |
| `pickupAnimation` | `CCP.CardData` | Inspector-selected pickup strategy. |
| Tilt properties | `CCP.CardGroup` | Exposes effective group tilt settings read by cards. |
| Drag spring serialized fields | `CCP.Card` / `CCP.DampedSpring` | Controls horizontal-drag wobble; source stiffness/damping are `150`/`10`, while the shipped prefab stores `1000`/`125`. |
| `Instance.useUnscaledTime` | `CCP.CardControllerSettings` | Controls unscaled updates for supporting DOTween effects. |

## Common issues

- **Symptom:** Clicking throws a null-reference exception. **Cause:** The card has no parent `CCP.CardGroup` or required flip/effect references are missing. **Fix:** add cards through group APIs and assign the hierarchy references.
- **Symptom:** `Select` changes no visible element. **Cause:** `None` is selected, or `selectionMark`/`cardHoverer` is unassigned. **Fix:** choose `Scale` or `Lift`, and assign the required references.
- **Symptom:** Consumer code cannot call `SelectCard`. **Cause:** selection mutators are internal. **Fix:** use user clicks and public query methods.
- **Symptom:** Hover and select scales fight each other. **Cause:** both strategies tween `cardHoverer`. **Fix:** use `Lift` for selection or `None` for one competing scale effect.
- **Symptom:** Tilt warns or does nothing. **Cause:** `Enable Tilt` is true but `cardTilter` is missing. **Fix:** assign the node or disable group tilt.
- **Symptom:** Spring motion differs from the documented source defaults. **Cause:** the shipped prefab serializes stronger `1000`/`125` spring values. **Fix:** inspect the prefab instance and tune from its serialized values.
- **Symptom:** Effects stop while paused despite the setting. **Cause:** no correctly named settings asset is loadable from `Resources`, or that particular custom tween ignores the setting. **Fix:** place `CardControllerSettings` under `Resources` and inspect the strategy's `SetUpdate`.

## Boundaries

- Click behavior is one enum choice per group. Multi-action clicks require a `Custom` listener that composes application logic.
- Public selection support is intentionally query-only; direct selection mutation methods are internal.
- Flip methods toggle rather than set a requested face and require valid face references.
- Built-in tilt and spring are runtime interaction effects, not physically simulated card objects.
- `CardControllerSettings.useUnscaledTime` affects only tweens that opt into it; it does not affect `WaitForSeconds` or unrelated gameplay systems.
- This skill avoids `DealPlaceAnimation` and `QuickDashAttackAnimation` tuning because their current completion/hold behavior is unreliable. Use verified built-ins or a custom strategy instead.
