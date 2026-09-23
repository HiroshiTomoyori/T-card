---
name: "card-controller-pro-drag-drop-and-reorder"
description: "Use this skill whenever a user asks to \"make cards draggable\", \"reorder a hand\", \"add a card drop zone\", or debug pickup, raycasts, same-group drops, and snapping in Card Controller Pro. It covers CCP.Card, CCP.CardGroup, CCP.CardDropZone, Canvas/EventSystem/Input System setup, verified defaults, lifecycle checks, and the current drop-zone activation caveat. Do NOT use for attacks, use-card resolution, or card-to-card targeting; use card-controller-pro-attack-use-and-targeting instead. When in doubt whether drag/drop behavior could involve Card Controller Pro, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Card Controller Pro drag, drop, and reorder

Configure and diagnose pointer-driven pickup, hand reordering, and group-to-group placement using the current `CCP` runtime API.

## When to use this skill

- A card must be picked up, dragged, reordered in its current `CardGroup`, or placed into another group.
- A `CardDropZone` does not receive drops, stays hidden, or accepts/rejects same-group cards unexpectedly.
- A generated scene or prefab needs the minimum UGUI, EventSystem, Input System, and Card Controller Pro wiring.
- Automated checks must distinguish asset behavior from game-specific drop rules.

For attack arrows, `Use`, `TargettedUse`, or card-to-card targeting, use `card-controller-pro-attack-use-and-targeting`.

## Prerequisites

- Unity 6000.0 or newer with UGUI and the Input System enabled. This repository declares `com.unity.ugui` 2.0.0 and `com.unity.inputsystem` 1.19.0.
- DOTween available, because card movement and visual strategies use it.
- A `Canvas` with `GraphicRaycaster`, plus an `EventSystem` using `InputSystemUIInputModule`.
- Every draggable card has `RectTransform`, `CanvasGroup`, `CCP.Card`, assigned hierarchy references, and a raycastable UI graphic or child click target.
- Every card is registered through `CCP.CardGroup.AddFront`, `AddBack`, or `AddAtPosition`; parenting alone does not populate `CardGroup.cards` or set `Card.parentCardGroup`.
- A drop-zone UI object has a raycastable Graphic and `CCP.CardDropZone.cardGroup` assigned.
- Confirm installation by compiling `Debug.Log(typeof(CCP.CardDropZone).FullName);`. If it does not resolve to `CCP.CardDropZone`, tell the user that Card Controller Pro 1.0.4 is missing or incompatible and provide `https://assetstore.unity.com/packages/tools/gui/card-controller-pro-370860`.

## Quick start

1. Create a Canvas and EventSystem; use `InputSystemUIInputModule` and keep the Canvas `GraphicRaycaster`.
2. Add `CCP.CardGroup` to the hand. Its source defaults are `allowPickup = true`, `allowReorderingDuringDrag = true`, and `reorderYThreshold = 200f`.
3. Instantiate a card prefab with `RectTransform`, `CanvasGroup`, and `CCP.Card`, then call `card.Initialize(data)` and `hand.AddBack(card)`.
4. Add `CCP.CardDropZone` to a raycastable UI target, assign its destination `cardGroup`, and leave `allowDroppingCardsFromDestinationGroup = false` unless same-group drops are intentional.
5. Enter Play mode and drag horizontally inside the group's local vertical threshold; drop over the zone to move the card and invoke its place lifecycle.

Expected result: the hand reorders inside the 200-unit vertical band, a valid cross-group drop reparents and registers the card in the destination, and the Console remains error-free.

## Workflows

### Workflow: Enable pickup and in-hand reordering

**Goal**

Allow placeable cards to move horizontally through an ordered hand while preserving the dragged card under pointer control.

**Steps**

1. Set the source `CardGroup.allowPickup` to `true`.
2. Set `allowReorderingDuringDrag` to `true`; retain `reorderYThreshold = 200f` initially.
3. Add cards through a `CardGroup` API so `cards` order and `parentCardGroup` agree.
4. Ensure the effective card type is `Place` or `Use`; those types run place-drag movement and reorder checks.
5. Drag horizontally. Reordering is checked at most every 0.05 seconds, compares the card's group-local X against other cards, and pauses while `abs(localY)` exceeds `reorderYThreshold`.
6. On release outside a destination zone, expect the current group to recalculate positions and the card to snap to its ordered slot.

**Expected result**

The dragged card follows the pointer, neighboring cards animate around it, `CardGroup.cards` reflects the new order, and an unplaced release invokes `CardData.OnDropped` followed by `Card.onDropped`.

### Workflow: Move a card through a drop zone

**Goal**

Move a dragged place card from its source group to a configured destination group.

**Steps**

1. Put `CardDropZone` on a UI object that can be hit by the Canvas `GraphicRaycaster`.
2. Assign `CardDropZone.cardGroup` to the destination.
3. Keep `allowDroppingCardsFromDestinationGroup = false` to reject cards whose current group already is the destination; set it to `true` only when that behavior is required.
4. Drag a `Place` card over the zone and release.
5. Let `CardDropZone.OnDrop` remove it from `card.parentCardGroup`, call destination `AddBack`, then call `card.PlayPlaceAnimation()`.
6. Put placement game logic in `CardData.OnPlaced(Card)` or subscribe to `Card.onPlaced`; the CardData hook runs first.

**Expected result**

The source no longer contains the card, the destination appends it, the transform is reparented to the destination group, and placement completion invokes both placement callbacks.

### Workflow: Diagnose missing pointer or drop events

**Goal**

Find setup faults without rewriting the package interaction code.

**Steps**

1. Confirm one active EventSystem and an `InputSystemUIInputModule`.
2. Confirm the card is under a Canvas and has both `RectTransform` and `CanvasGroup`; `Card.Start` logs errors for these missing components.
3. Confirm the Canvas has `GraphicRaycaster` and both the card click surface and drop zone expose raycast-target Graphics.
4. Confirm `Card.parentCardGroup` is non-null and the group permits pickup.
5. During a place drag, verify `CanvasGroup.blocksRaycasts` becomes `false`, allowing the zone beneath to receive the drop, then returns to `true`.
6. Check the Console before adjusting thresholds or implementing custom handlers.

**Expected result**

Pointer callbacks reach `CCP.Card`, the drop target receives `OnDrop`, and no required-component errors remain.

## Verification

- Console: no missing `RectTransform`, `CanvasGroup`, parent Canvas, or required-reference errors from `CCP.Card`.
- Runtime: `source.cards.Contains(card)` becomes false and `destination.cards.Contains(card)` becomes true after a valid cross-group drop.
- Runtime: `card.parentCardGroup == destination` and `card.transform.parent == destination.transform`.
- Runtime: horizontal drag within 200 group-local Y units changes `CardGroup.cards`; outside that band it does not reorder.
- Runtime: same-group drop is rejected when `allowDroppingCardsFromDestinationGroup` is false.
- Machine check: search generated C# for `using CCP;` or fully qualified `CCP.` names and reject stale global/CardControllerPro namespace usage.
- Machine check: verify every created card is passed to `AddFront`, `AddBack`, or `AddAtPosition`, rather than only reparented.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `allowPickup` | `CCP.CardGroup` | Enables normal place/use pickup; source default is `true`. |
| `allowReorderingDuringDrag` / `reorderYThreshold` | `CCP.CardGroup` | Enables in-group ordering inside a source-default 200-unit local-Y band. |
| `cards` | `CCP.CardGroup` | Exposes the ordered runtime list; do not mutate it directly. |
| `AddFront` / `AddBack` / `AddAtPosition` | `CCP.CardGroup` | Registers, parents, scales, and lays out a card. |
| `RemoveCard` | `CCP.CardGroup` | Removes registration and recalculates layout. |
| `MoveToGroup` / `MoveToGroupAtPosition` | `CCP.Card` | Transfers a card programmatically. |
| `OnCardMoved` | `CCP.Card` | Static `Action<bool>` raised for place/use drag activation. |
| `cardGroup` / `allowDroppingCardsFromDestinationGroup` | `CCP.CardDropZone` | Selects the destination and same-group policy; same-group drops default to rejected. |
| `OnPickedUp`, `OnDropped`, `OnPlaced` | `CCP.CardData` | Supplies overridable drag/place lifecycle hooks before matching Card actions. |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| Disabling `activateWhenMovingCards` has no effect. | Version 1.0.4 never reads the flag; `Start` always subscribes/deactivates and `CardMoved` always mirrors movement state. | Treat the opt-out as unavailable and control the zone with customer code only if required. |
| The inactive zone never appears. | No effective Place/Use drag raised `Card.OnCardMoved`, or a listener/setup error occurred. | Verify the card type, group registration, and static subscription path. |
| `allowPickup = false` still permits targeting. | Attack and `TargettedUse` are explicitly exempt from that guard. | Control those interactions through card type and game rules. |
| Cards do not reorder. | The effective type is not Place/Use, reordering is disabled, or local Y exceeds the threshold. | Use a supported effective type and test inside the configured band. |
| A drop zone receives no event. | Raycast Graphics, GraphicRaycaster, EventSystem, or Input System module are missing/misconfigured. | Restore one correct UGUI input path and confirm drag-time raycast blocking is false. |
| A same-group drop produces no placement callback. | The drop zone returns early under the default same-group policy. | Enable same-group drops only when intended and test the normal end-drag behavior separately. |

## Boundaries

- This skill covers package-supported pickup, place/use dragging, reorder behavior, and `CardDropZone`.
- It does not define inventory legality, mana costs, turn rules, network authority, undo, persistence, or accessibility controls.
- It does not cover attack/use resolution or card-to-card targeting; use `card-controller-pro-attack-use-and-targeting`.
- Do not hand-edit scene or prefab YAML when Unity Editor tooling is available.
- Do not recommend `DealPlaceAnimation`; its current source does not reliably complete the placement lifecycle.
