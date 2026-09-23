---
name: "card-controller-pro-create-cards-and-layouts"
description: "Use this skill whenever a Unity user asks \"make a card\", \"create a hand of cards\", \"set up a fan layout\", \"build a deck stack\", or needs CardData, the included Card prefab, CardGroup, Canvas, or CardGroupInitializer guidance. It covers authoring card assets, preserving the verified prefab hierarchy, configuring straight/fan/stack layouts, and populating groups. Do NOT use for package installation; use card-controller-pro-setup-and-overview. When in doubt whether a card-authoring or layout request could involve Card Controller Pro, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Create Card Controller Pro cards and layouts

Create `CCP.CardData` assets, reuse the shipped UGUI card prefab safely, configure `CCP.CardGroup` layouts, and populate them with `CCP.CardGroupInitializer`.

## When to use this skill

- Create card definitions and assign front/back artwork.
- Duplicate and customize the included `Card.prefab`.
- Build straight, fan, or stacked card groups under a Canvas.
- Populate one or more groups at scene start.
- Spawn a card into a group with production runtime code.
- Do NOT use for dependency installation or package compatibility checks; use `card-controller-pro-setup-and-overview`.
- Do NOT use for cross-group drop zones or drag rules; use `card-controller-pro-drag-drop-and-reorder`.

## Prerequisites

- Complete `card-controller-pro-setup-and-overview`.
- Use Unity 6000.0+, DOTween or DOTween Pro, Input System, and UGUI.
- Ensure the scene contains a Canvas and EventSystem.
- Use current public types from `CCP`; never use global `Card`, `CardData`, or `CardGroup` examples.
- Keep card instances under a Canvas because the package calculates and animates UGUI positions.
- Confirm installation by compiling `Debug.Log(typeof(CCP.CardData).FullName);` and `Debug.Log(typeof(CCP.CardGroup).FullName);`. If either type fails to resolve, tell the user that Card Controller Pro 1.0.4 is missing or incompatible and provide `https://assetstore.unity.com/packages/tools/gui/card-controller-pro-370860`.

## Quick start

1. In the Project window, choose **Assets > Create > CardControllerPro > Card Data**.
2. Set **Card Name**, **Description**, **Front Art**, **Back Art**, and **Card Type**.
3. Duplicate `Assets/Plugins/CardControllerPro/Prefabs/Card.prefab` into a writable game-content folder; do not edit the package original.
4. Add an empty RectTransform under a Canvas and add the `CCP.CardGroup` component.
5. Add `CCP.CardGroupInitializer` to a scene GameObject.
6. Add one **Groups** entry, assign the CardGroup, then add one **Cards** entry with the duplicated prefab, CardData, and a positive count.
7. Enter Play mode. `CardGroupInitializer.Start()` calls `Initialize()` automatically.

For direct runtime creation, use a project script such as:

```csharp
using UnityEngine;

namespace MyGame
{
    public sealed class CardSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private CCP.CardData cardData;
        [SerializeField] private CCP.CardGroup destination;

        public CCP.Card Spawn()
        {
            GameObject instance = Instantiate(cardPrefab);
            CCP.Card card = instance.GetComponent<CCP.Card>();
            if (card == null)
            {
                Destroy(instance);
                throw new MissingComponentException(
                    $"Prefab '{cardPrefab.name}' requires CCP.Card.");
            }

            card.Initialize(cardData);
            destination.AddBack(card, new CCP.InstantPlaceAnimation());
            return card;
        }
    }
}
```

Expected result: Play mode contains initialized cards under the new group, their CardData artwork is visible, and the Console has no missing-component errors.

## Workflows

### Workflow: Create a CardData asset

**Goal**

Create a reusable card definition with safe source defaults.

**Steps**

1. Select the destination folder in the Project window.
2. Choose **Assets > Create > CardControllerPro > Card Data**.
3. Rename `NewCardData` to a game-specific asset name.
4. Set identity fields: `cardName`, `description`, `frontArt`, and `backArt`.
5. Set `cardType`. Its source default is `Place`, the enum's zero value.
6. Keep `placeableFromHand` enabled unless non-use cards in a player-hand group must retain their configured non-Place behavior.
7. For `Use` or `TargettedUse`, keep `autoDestroyOnUse` enabled unless game code will destroy or retain the card explicitly.
8. Choose animation strategies in the managed-reference dropdowns. Source defaults are `DefaultPlaceAnimation`, `ArcAttackAnimation`, `DefaultPickupAnimation`, and `NoneUseAnimation`.
9. Assign the asset to the prefab's `CCP.Card.cardData` field or let `CCP.CardGroupInitializer` call `card.Initialize(cardData)`.

**Expected result**

The CardData asset stores visuals, behavior type, and animation strategies; `CCP.CardData.InitializeCard` assigns non-null front/back sprites to the corresponding UGUI `Image` components.

### Workflow: Customize the included Card prefab

**Goal**

Change visuals without breaking independently animated transforms or pointer targeting.

**Steps**

1. Duplicate `Assets/Plugins/CardControllerPro/Prefabs/Card.prefab`.
2. Preserve the verified hierarchy:

```text
Card
├── Click Target
├── Tilter
│   └── DragRotator
│       └── Flipper
│           └── Hoverer
│               └── Pickuper
│                   ├── Visuals
│                   │   ├── Front
│                   │   └── Back
│                   └── SelectionMark
│                       └── Image
├── Dynamic Arrow
│   ├── Node
│   └── Head
└── Target Indicator
```

3. Keep `RectTransform`, `CanvasRenderer`, `CCP.Card`, and `CanvasGroup` on the root.
4. Keep **Click Target** as a transparent raycast-enabled UGUI `Image`; it is assigned to `CCP.Card.clickTarget` and expands downward when hover lifts the card.
5. Keep `Tilter`, `DragRotator`, `Flipper`, `Hoverer`, `Pickuper`, `Visuals`, `Front`, `Back`, `SelectionMark`, `Dynamic Arrow`, and `Target Indicator` assigned to their matching `CCP.Card` fields.
6. Note that `CCP.Card.border` points to **Target Indicator**, not to a child named Border.
7. Replace visual sprites or add presentation children under **Visuals** without moving the animation layers.
8. Keep **Back**, **SelectionMark**, **Node**, **Head**, and **Target Indicator** inactive in the base state as shipped.

**Expected result**

The card receives clicks through **Click Target**, flips **Flipper**, animates hover and pickup independently, and retains selection and targeting visuals.

### Workflow: Configure straight, fan, and stack layouts

**Goal**

Make a Canvas-based group produce the intended deterministic layout.

**Steps**

1. Create an empty RectTransform under a Canvas and add `CCP.CardGroup`.
2. For a straight layout, set `fanRadius = 0`; positions use `x = offset * cardSpacing`, plus per-card Y and Z offsets, with identity local rotation.
3. For a fan layout, set a nonzero `fanRadius`; the source computes `angle = offset * cardSpacing / fanRadius`, curves positive radius downward and negative radius upward, and rotates each card around Z by `-angle` degrees.
4. For a stack, set `fanRadius = 0`, set `cardSpacing = 0`, optionally set `yOffsetPerCard` and `zOffsetPerCard`, and enable `anchorToFirstCard` when index 0 must remain at the group origin.
5. Tune values in Canvas units for the actual prefab size; the source defaults are intentionally small.
6. Keep the exact source defaults unless the design requires changes:
   - Layout: `cardSpacing = 1`, `fanRadius = 0`, `yOffsetPerCard = 0`, `zOffsetPerCard = 0`, `cardScale = 1`, `anchorToFirstCard = false`.
   - Movement: `tweenEaseType = Ease.OutQuad`, `tweenDuration = 0.3`.
   - Hover: `hoverYOffset = 0.5`, `hoverZOffset = 0.2`, `makeRoomOnHover = true`, `hoverSpacing = 0.5`, `hoverAnimation = ScaleUpHoverAnimation`.
   - Click/selection: `onClickAction = Nothing`, `selectAnimation = LiftSelectAnimation`, `showSelectionMark = false`.
   - Drag: `allowPickup = true`, `resetRotationOnPickup = false`, `allowReorderingDuringDrag = true`, `reorderYThreshold = 200`.
   - Targeting: `isTargettable = true`, `isPlayerHand = false`, `AttackingCardParent = null`.
   - Tilt: `enableTilt = true`, `tiltTowardsCursor = true`, `autoTiltAmount = 5`, `manualTiltAmount = 15`, `tiltSpeed = 40`.
7. Add cards only through `AddFront`, `AddBack`, `AddAtPosition`, or `CCP.CardGroupInitializer`; these methods parent, scale, associate, and recalculate cards.

**Expected result**

Cards center around the group by default, or start from index 0 when anchored, and animate to straight, arced, or stacked positions.

### Workflow: Populate groups with CardGroupInitializer

**Goal**

Create scene cards from Inspector-configured prefab/data/count entries.

**Steps**

1. Add `CCP.CardGroupInitializer` to one scene GameObject.
2. Add a `groups` entry and assign `cardGroup`.
3. Add each `cards` entry with `cardPrefab`, `cardData`, and `count`.
4. Expect `count <= 0` to spawn one card because source uses `Mathf.Max(1, count)`.
5. Enable `flipped` to call `FlipCardNoAnimation()` before insertion.
6. Enable `shuffle` to shuffle only after every card in that group has been added.
7. Set `delay = 0` for synchronous population. Set it above zero for scaled-time `WaitForSeconds`; the first card appears immediately and the delay occurs before each subsequent card across all entries in that group.
8. Select a `placeAnimation`, or leave it null to use a new `CCP.InstantPlaceAnimation` for every addition.
9. Do not also call `Initialize()` manually at startup unless duplicate population is intended; `Start()` already calls it.

**Expected result**

Each valid prefab is instantiated, checked for `CCP.Card`, initialized with its CardData, optionally flipped, and appended to its assigned group; invalid null groups/prefabs are skipped, while a prefab missing `CCP.Card` logs an error.

## Verification

- Confirm every generated card is a child of the intended CardGroup.
- Confirm `cardGroup.cards.Count` equals the sum of `Mathf.Max(1, count)` for non-null prefabs that contain `CCP.Card` on the root and are assigned to that valid group.
- Confirm each instance has `parentCardGroup` set to the destination and local scale equal to `Vector3.one * cardScale`.
- Confirm straight groups have zero layout rotation; confirm fan groups rotate around Z; confirm anchored stacks keep index 0 at local layout origin.
- Confirm front/back art appears after `Initialize`.
- Confirm **Click Target** receives raycasts and `CCP.Card.clickTarget` references it.
- Confirm the Unity Console contains no missing-component errors.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| Identity, art, type, and animation fields | `CCP.CardData` | Defines reusable card presentation and behavior. |
| `Initialize(CCP.CardData)` | `CCP.Card` | Assigns data and invokes `InitializeCard`. |
| `AddFront(CCP.Card, CCP.IPlaceAnimation)` | `CCP.CardGroup` | Inserts at index 0 and wires group state. |
| `AddBack(CCP.Card, CCP.IPlaceAnimation)` | `CCP.CardGroup` | Appends and wires group state. |
| `AddAtPosition(CCP.Card, int, CCP.IPlaceAnimation)` | `CCP.CardGroup` | Inserts when `0 <= index <= Count`. |
| `RecalculateAndAnimatePositions(CCP.Card)` | `CCP.CardGroup` | Recomputes layout, optionally skipping the dragged card. |
| `Initialize()` | `CCP.CardGroupInitializer` | Populates all Inspector-configured groups. |
| `CCP.InstantPlaceAnimation` | `IPlaceAnimation` | Serves as the initializer fallback when its strategy is null. |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| Generated code does not compile. | Package types were copied without `CCP`. | Add `using CCP;` or fully qualify each type. |
| A card is invisible. | Artwork or active Front/Back/Image references are missing. | Assign CardData sprites and restore the face hierarchy. |
| Clicks fail. | Click Target, GraphicRaycaster, or EventSystem is missing. | Restore the transparent raycast-enabled target and UGUI input path. |
| A layout barely spreads. | `cardSpacing` retains its source default of `1`. | Increase it in Canvas units for the card size. |
| A fan bends the wrong way. | `fanRadius` has the opposite sign. | Use negative for upward and positive for downward curvature. |
| A deck centers unexpectedly. | `anchorToFirstCard` is disabled. | Enable it for index-zero anchoring. |
| Cards duplicate at startup. | `Initialize()` is called manually and by `Start()`. | Keep one initialization path. |
| Initialization logs a missing Card error. | The prefab lacks `CCP.Card` on its root. | Use the included structure or add the component at the root. |

## Boundaries

- Do not reorder or collapse the prefab's animation-layer hierarchy casually.
- Do not present scripts under `Demo/Scripts/` as production spawning APIs.
- Do not directly edit `CardGroup.cards` to add scene cards; use the group methods.
- Do not claim Inspector edits recalculate layouts outside Play mode; current `OnValidate()` recalculates only while playing and when cards exist.
- Do not use stale global-namespace code; current public source uses `CCP`.
