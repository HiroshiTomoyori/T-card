---
name: "card-controller-pro-attack-use-and-targeting"
description: "Use this skill whenever a user asks to \"make a card attack\", \"use a spell card\", \"target another card\", \"show the targeting arrow\", or debug CardType, player-hand placement overrides, valid targets, callbacks, and destruction in Card Controller Pro. It covers the current CCP attack, Use, TargettedUse, DynamicArrow, and lifecycle APIs with verified defaults and caveats. Do NOT use for hand reordering or group drop-zone setup; use card-controller-pro-drag-drop-and-reorder instead. When in doubt whether combat or card-use behavior could involve Card Controller Pro, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Card Controller Pro attack, use, and targeting

Configure and extend the current `CCP` card-type resolution, card-to-card targeting, arrow feedback, attack impact, and use completion lifecycles.

## When to use this skill

- A card must attack another card, perform untargeted `Use`, or perform `TargettedUse`.
- Attack or targeted-use drags fail to highlight a target or display `DynamicArrow`.
- A `CardData` subclass or external system must respond at targeting start, attack impact, use completion, or targeted-use completion.
- `placeableFromHand`, `isPlayerHand`, `isTargettable`, ownership targeting, or auto-destruction behaves unexpectedly.

For pickup, in-hand reorder, and group-to-group `CardDropZone` setup, use `card-controller-pro-drag-drop-and-reorder`.

## Prerequisites

- Unity 6000.0 or newer with UGUI, Input System, and DOTween.
- A Canvas with `GraphicRaycaster`, one EventSystem using `InputSystemUIInputModule`, and raycastable card graphics.
- Every card has `RectTransform`, `CanvasGroup`, `CCP.Card`, a configured `CCP.CardData`, and assigned visual hierarchy references.
- Cards are registered with `CCP.CardGroup`; target groups set `isTargettable = true`.
- The exact enum values are `CCP.CardType.Place`, `Attack`, `Static`, `Use`, and `TargettedUse`. Preserve the package spelling `TargettedUse`.
- A targeting card optionally references a child `CCP.DynamicArrow` configured with node and head prefabs.
- Confirm installation by compiling `Debug.Log(typeof(CCP.Card).FullName);` and `Debug.Log(typeof(CCP.DynamicArrow).FullName);`. If either type fails to resolve, tell the user that Card Controller Pro 1.0.4 is missing or incompatible and provide `https://assetstore.unity.com/packages/tools/gui/card-controller-pro-370860`.

## Quick start

1. Create a `CardData` asset and choose `Attack`, `Use`, or `TargettedUse`.
2. For an attack that should remain an attack while in the player hand, set `placeableFromHand = false`; otherwise an eligible card in an `isPlayerHand` group resolves to `Place`.
3. Set target board groups to `isTargettable = true`; set the attacker's `canTargetOwnCardGroup = false` unless same-group targeting is intended. The shipped `Card.prefab` serializes this field as `true`, despite the component source default being `false`, so inspect duplicated prefabs explicitly.
4. Assign a `DynamicArrow` to the card and provide its `nodePrefab` and `headPrefab`.
5. Override the matching `CardData` lifecycle method for card-intrinsic rules, or subscribe to the matching `Card` action for external orchestration.
6. In Play mode, drag an attack or `TargettedUse` card over a valid target and release; drag `Use` cards to a suitable drop zone.

Expected result: valid targets highlight during targeting, the arrow hides on release, and exactly the matching attack/use lifecycle callback runs without Console errors.

## Workflows

### Workflow: Resolve the effective card type

**Goal**

Make Inspector configuration produce the intended runtime interaction.

**Steps**

1. Start from `CardData.cardType`.
2. If `cardData` is null, expect effective `Place`.
3. If `placeableFromHand` is true, the current group has `isPlayerHand = true`, and the configured type is neither `Use` nor `TargettedUse`, expect effective `Place`.
4. Otherwise expect the configured enum value.
5. Set `placeableFromHand = false` when an `Attack` must target directly from the player hand.
6. Do not use `Card.IsUseType` to detect `TargettedUse`; its source returns true only when the effective type equals `CardType.Use`.

**Expected result**

`Attack` and `TargettedUse` begin card-to-card targeting when intended, `Use` performs place-style dragging, and cards deliberately placeable from the player hand resolve to `Place`.

### Workflow: Configure attack and targeted-use selection

**Goal**

Show targeting feedback and accept only valid card targets.

**Steps**

1. Set candidate target groups to `isTargettable = true`.
2. Leave `Card.canTargetOwnCardGroup = false` to reject cards in the attacker's own group, or enable it explicitly.
3. Ensure target UI graphics participate in EventSystem raycasts.
4. Assign `Card.dynamicArrow` if visual aiming is required.
5. Begin dragging an effective `Attack` or `TargettedUse`; the card sets shared targeting state, disables its own raycast blocking, shows/updates the arrow, then invokes `CardData.OnTargetingStart` and `Card.onTargetingStarted`.
6. Release over a valid card. Selection searches EventSystem raycast results, walks to a parent `Card`, and reapplies target-group and own-group checks.

**Expected result**

Valid hovered cards set the shared target, invoke their `onTargeted`, and show their border; invalid cards are skipped. End-drag clears highlighting, restores raycast blocking, and hides the arrow.

### Workflow: Handle attack impact and completion

**Goal**

Apply game-specific attack effects at the animation's hit moment without coupling them to demo code.

**Steps**

1. Put intrinsic attack logic in a `CardData` subclass overriding `OnAttackHit(Card attacker, Card target)`.
2. Alternatively, subscribe to `card.onAttackHit` for external combat orchestration.
3. Let `Card.ExecuteAttack` optionally reparent the attacker to `parentCardGroup.AttackingCardParent` for render order.
4. At the animation impact callback, expect `CardData.OnAttackHit` first and `Card.onAttackHit` second.
5. Let the animation invoke its completion callback so `ExecuteAttack` can restore the original parent and sibling index.
6. Keep damage, health, costs, and death rules in project code.

**Expected result**

The attack effect occurs once at impact, the animation completes, and the attacker returns to its original hierarchy position.

### Workflow: Handle Use and TargettedUse completion

**Goal**

Resolve use cards through the correct target shape and destruction policy.

**Steps**

1. For untargeted `Use`, drag through a `CardDropZone`; that path calls `ExecuteUse(CardGroup)`.
2. For `TargettedUse`, release over a valid card; that path calls `ExecuteTargettedUse(Card)`.
3. Both methods immediately remove the card from its source group and may reparent it under `AttackingCardParent`.
4. On animation completion, untargeted use invokes `CardData.OnUsed(card, dropZoneGroup)` then `Card.onUsed`.
5. On animation completion, targeted use invokes `CardData.OnTargetedUse(card, target)` then `Card.onTargetedUse`.
6. If `cardData` is null or `autoDestroyOnUse` is true, the card GameObject is destroyed after callbacks; otherwise project code owns its next state and cleanup.

**Expected result**

The source hand re-fans immediately, exactly the matching lifecycle pair runs after animation, and destruction follows `autoDestroyOnUse`.

## Verification

- Console: no missing Canvas, `RectTransform`, `CanvasGroup`, or hierarchy-reference errors.
- Runtime: an attack from an `isPlayerHand` group remains attack targeting only when `placeableFromHand` is false.
- Runtime: groups with `isTargettable = false` are never accepted as targets.
- Runtime: same-group targets are rejected unless the attacking card has `canTargetOwnCardGroup = true`.
- Runtime: `OnTargetingStart` precedes impact/use callbacks; CardData hooks precede matching `Card` actions.
- Runtime: `Use` passes a `CardGroup`; `TargettedUse` passes a target `Card`.
- Machine check: enum references use `TargettedUse` exactly and current types use `CCP`.
- Machine check: reject conditions that treat `card.IsUseType` as covering `TargettedUse`.
- Machine check: any custom `IAttackAnimation` or `IUseAnimation` invokes completion on every supported path.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `Place`, `Attack`, `Static`, `Use`, `TargettedUse` | `CCP.CardType` | Selects the configured interaction type. |
| `cardType`, `placeableFromHand`, `autoDestroyOnUse` | `CCP.CardData` | Configures effective-type overrides and use-card cleanup. |
| `isPlayerHand`, `isTargettable` | `CCP.CardGroup` | Controls player-hand placement overrides and target eligibility. |
| `canTargetOwnCardGroup` | `CCP.Card` | Allows same-group targets when enabled; source default is `false`, but the shipped prefab stores `true`. |
| `ExecuteAttack(Transform, Action)` | `CCP.Card` | Runs an attack strategy and the impact/completion callbacks. |
| `ExecuteUse(CardGroup)` | `CCP.Card` | Resolves an untargeted use against a group. |
| `ExecuteTargettedUse(Card)` | `CCP.Card` | Resolves a targeted use against a card. |
| `IsUseType` | `CCP.Card` | Returns true only for effective `Use`, not `TargettedUse`. |
| `PointAt(Vector2, Vector2)` / `Hide()` | `CCP.DynamicArrow` | Shows or hides the canvas-coordinate targeting display. |
| Serialized node/scale fields | `CCP.DynamicArrow` | Defaults to distance `50`, maximum `20`, node scales `0.5`–`1`, and head scale `1`; prefab references default to null. |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| Types do not compile. | Older examples omit `CCP` or use `CardControllerPro.DynamicArrow`. | Use the current `CCP` namespace for runtime and animation types. |
| Targeted-use code does not compile or enters the wrong branch. | The runtime spells it `TargettedUse`, and `IsUseType` covers only `Use`. | Use the exact enum/method spelling and test the two use types separately. |
| An enemy card cannot be targeted. | Its group is not targettable, it is in the same group, or raycasts do not reach it. | Enable `isTargettable`, set the own-group policy deliberately, and restore raycastable UI. |
| A duplicated shipped card can target its own group unexpectedly. | `Card.prefab` serializes `canTargetOwnCardGroup = true` although the code default is false. | Set the duplicated prefab value explicitly for the intended rules. |
| The arrow shows neither nodes nor head. | Node/head prefabs are unassigned, or the arrow is not under a Canvas. | Assign both prefabs and keep the arrow below a Canvas RectTransform. |
| Quick Dash ignores Inspector tuning. | Version 1.0.4 does not read its four serialized fields. | Use its fixed 0.25-second InQuart/OutQuad legs, choose another strategy, or add a customer-owned strategy. |
| An attack remains stuck or temporarily reparented. | Its custom animation returned without invoking completion. | Invoke `onComplete` once on every supported path after restoring public state. |
| Concurrent targeting corrupts the active target. | Targeting state is static and supports one global interaction. | Permit only one targeting interaction at a time. |

## Boundaries

- This skill covers package targeting mechanics and extension points, not a complete combat rules engine.
- Damage, armor, costs, turns, teams, AI decisions, network authority, and persistence belong to project code.
- `CardHealth` and the demo `AttackCardData`/`FireballCardData` are optional demo patterns, not required core architecture.
- Do not promote demo scripts into production dependencies without reviewing and adapting them.
- Do not recommend `DealPlaceAnimation`; its current completion behavior is unsuitable for reliable lifecycle work.
- For hand dragging, reorder, and drop-zone plumbing, use `card-controller-pro-drag-drop-and-reorder`.
