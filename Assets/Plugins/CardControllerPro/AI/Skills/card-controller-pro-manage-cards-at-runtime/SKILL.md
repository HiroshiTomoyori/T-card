---
name: "card-controller-pro-manage-cards-at-runtime"
description: "Use this skill whenever the user wants to create, initialize, add, remove, move, shuffle, deal, flip, or query Card Controller Pro cards at runtime — e.g. \"spawn three cards into my hand\", \"deal from the deck\", \"move this card to discard\", \"shuffle the draw pile\", \"remove the top card with a fade\", or \"get the selected cards\". Covers the public CCP.Card, CCP.CardGroup, and CCP.CardGroupInitializer runtime APIs, removal callbacks, and selection queries. Do NOT use for configuring clicks, hover, pickup, selection visuals, tilt, or drag spring effects (see card-controller-pro-click-and-visual-effects). When in doubt whether runtime card management could involve Card Controller Pro, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Manage Card Controller Pro Cards at Runtime

Use Card Controller Pro's public runtime API to initialize cards, maintain ordered groups, move and deal cards, run removal effects with completion callbacks, flip cards, and read user-driven selection state.

## When to use this skill

- The user asks to "spawn cards into my hand", "initialize a card prefab", or populate a group at scene start.
- The user asks to add at the front, back, or a specific index; draw or remove a card; or move a card between groups.
- The user asks to "shuffle the deck", "deal one every half second", or flip dealt cards.
- The user asks to remove a card with a hide animation and continue only when the animation completes.
- The user asks which cards the player selected or did not select.

Not for:

- Click actions, hover/pickup/select effects, tilt, or drag spring configuration; see `card-controller-pro-click-and-visual-effects`.
- Attack, use, targeting, or drop-zone behavior; use the matching Card Controller Pro sibling skill.
- Writing custom animation strategy classes; use the behavior-and-animations sibling skill.

## Prerequisites

- Card Controller Pro `1.0.4` in Unity `6000.0+`.
- A UGUI Canvas, card prefab with `CCP.Card`, and destination object with `CCP.CardGroup`.
- The package dependencies used by the source: DOTween, Unity Input System, Unity UGUI, and the bundled SerializeReferenceExtensions.
- Confirm installation programmatically before editing gameplay code:

```csharp
Debug.Log(typeof(CCP.Card).FullName);
Debug.Log(typeof(CCP.CardGroup).FullName);
Debug.Log(typeof(CCP.CardGroupInitializer).FullName);
```

All three names must log with the `CCP` namespace and the Console must show no compile errors. If a type is missing, tell the user Card Controller Pro is not installed or is the wrong version and provide the Asset Store URL: https://assetstore.unity.com/packages/tools/gui/card-controller-pro-370860.

## Quick start

1. Add `using CCP;` and `using UnityEngine;` to a runtime `MonoBehaviour`.
2. Assign a card prefab, `CardData`, and destination `CardGroup` in the Inspector.
3. Instantiate the prefab, get its `Card`, initialize it, and add it to the group:

```csharp
using CCP;
using UnityEngine;

public sealed class RuntimeCardSpawner : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private CardData cardData;
    [SerializeField] private CardGroup hand;

    public Card Spawn()
    {
        GameObject instance = Instantiate(cardPrefab);
        Card card = instance.GetComponent<Card>();
        if (card == null)
        {
            Debug.LogError("The assigned prefab needs a CCP.Card component.", instance);
            Destroy(instance);
            return null;
        }

        card.Initialize(cardData);
        hand.AddBack(card, new InstantPlaceAnimation());
        return card;
    }
}
```

4. Enter Play mode and call `Spawn`.

Expected result: one initialized card becomes a child of the group, appears at the group's back, and the Console remains free of compile errors and unhandled exceptions.

## Workflows

### Workflow: Populate groups with CardGroupInitializer

**Goal**

Instantiate configured card prefabs into one or more groups when the scene starts.

**Steps**

1. Add `CCP.CardGroupInitializer` to a scene object.
2. In `Groups`, add an entry and assign its `Card Group`.
3. Add card entries and assign each `Card Prefab`, `Card Data`, and `Count`.
4. Set `Shuffle` or `Flipped` as needed.
5. Set `Delay` to `0` for immediate population or a positive number for a scaled-time interval between consecutive cards.
6. Choose a place animation or leave it unassigned for `CCP.InstantPlaceAnimation`.
7. Let `Start` call `Initialize`, or call `initializer.Initialize()` explicitly when another system controls startup.

`Count` is clamped with `Mathf.Max(1, count)`, so zero and negative values still create one card. Positive delay uses `WaitForSeconds`, so it follows `Time.timeScale`; the first card is immediate and each later card waits. A null place animation is replaced with `InstantPlaceAnimation`. Do not call `Initialize` twice unless duplicate cards are intended.

**Expected result**

Each valid prefab produces at least one initialized `CCP.Card`, cards are added to the assigned group, optional flipping happens before placement, and optional shuffling happens after the group finishes populating.

### Workflow: Add, remove, and move cards

**Goal**

Maintain ordered groups while preserving Card Controller Pro's parenting, layout, and selection cleanup.

**Steps**

1. Use `AddFront`, `AddBack`, or `AddAtPosition` to add a card. Valid insertion indices are `0` through `cards.Count`.
2. Use `Front()` or `Back()` to inspect an endpoint; either returns `null` for an empty group.
3. Use `RemoveFront`, `RemoveBack`, `RemoveAtPosition`, or `RemoveCard`. Prefer these methods over directly mutating `cards`.
4. Move an existing card with `card.MoveToGroup(destination)` or `card.MoveToGroupAtPosition(destination, index)`. These remove it from the current group and add it to the new one.
5. Pass an `IPlaceAnimation` only when the add/move itself should play that strategy.

```csharp
Card top = drawPile.Back();
if (top != null)
{
    top.MoveToGroupAtPosition(hand, 0, new InstantPlaceAnimation());
}
```

**Expected result**

The card's `parentCardGroup`, Transform parent, ordered group lists, scale, and destination layout are updated together.

### Workflow: Remove with a hide callback

**Goal**

Run cleanup only after an optional hide effect completes.

**Steps**

1. Pass a public `CCP.IHideAnimation` implementation and an `Action<Card>` callback to a remove method.
2. Destroy or pool the detached card in that callback.
3. Handle an empty group or invalid index because those cases return `null` and do not invoke the callback.

```csharp
Card removed = discard.RemoveFront(
    new FadeOutHideAnimation(),
    card => Destroy(card.gameObject));

if (removed == null)
    Debug.Log("Discard was empty.");
```

With a null hide animation, the callback is invoked synchronously. With an animation, the card is removed from the group before the hide callback finishes.

**Expected result**

The card leaves the group immediately, the selected list no longer contains it, and cleanup runs once through the callback.

### Workflow: Shuffle and deal cards

**Goal**

Randomize a source group and transfer a fixed number of cards over time.

**Steps**

1. Call `source.Shuffle()`; it performs an in-place Fisher-Yates shuffle and recalculates layout.
2. In a coroutine, read `source.Back()` on each iteration and stop if it returns `null`.
3. Move the card to the destination and optionally call `FlipCard()` or `FlipCardNoAnimation()`.
4. Yield `WaitForSeconds(delay)` between cards when scaled-time dealing is desired.

```csharp
private System.Collections.IEnumerator Deal(
    CardGroup source, CardGroup destination, int amount, float delay)
{
    source.Shuffle();

    for (int i = 0; i < amount; i++)
    {
        Card card = source.Back();
        if (card == null)
            yield break;

        card.MoveToGroup(destination, new InstantPlaceAnimation());
        card.FlipCard();

        if (i + 1 < amount && delay > 0f)
            yield return new WaitForSeconds(delay);
    }
}
```

Do not use `DealPlaceAnimation` for completion-dependent flows; its current implementation does not reliably complete its state/callback contract.

**Expected result**

Up to `amount` cards move from the randomized source to the destination in order, with no out-of-range access when the source empties.

### Workflow: Flip cards and read selections

**Goal**

Change card faces and consume the current player-selected subset without accessing internal APIs.

**Steps**

1. Call `card.FlipCard()` for the animated two-stage Y rotation.
2. Call `card.FlipCardNoAnimation()` for an immediate front/back toggle.
3. Configure selection as a user click behavior, then query snapshots with `group.GetSelectedCards()` and `group.GetNotSelectedCards()`.
4. Treat returned lists as copies; changing them does not modify the group's internal selection state.

```csharp
List<Card> selected = hand.GetSelectedCards();
List<Card> remaining = hand.GetNotSelectedCards();
Debug.Log($"Selected: {selected.Count}; not selected: {remaining.Count}");
```

Never call `CardGroup.IsSelected`, `CardGroup.SelectCard`, or `CardGroup.DeselectCard`; they are internal implementation details and are inaccessible to normal consumer assemblies.

**Expected result**

Faces toggle through public `Card` methods, and gameplay code receives safe list snapshots of user-selected and unselected cards.

## Verification

- Confirm every first-party type reference is qualified by `CCP` or covered by `using CCP;`; no example assumes a stale global namespace.
- After spawning, verify `card.cardData` matches the supplied data, `card.parentCardGroup` matches the destination, and `destination.cards.Contains(card)` is true.
- After moving, verify the source no longer contains the card and the destination contains it at the expected index.
- After removing, verify `parentCardGroup == null`, the callback count is exactly one when removal succeeds, and selected queries exclude the removed card.
- After a deal, verify source and destination counts changed by the actual number dealt.
- Confirm no compile errors or unhandled exceptions appear in the Console.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `Initialize(CardData)` | `CCP.Card` | Assigns data and calls its initialization lifecycle hook. |
| `FlipCard()` / `FlipCardNoAnimation()` | `CCP.Card` | Toggles the front/back face with or without animation. |
| `MoveToGroup(...)` / `MoveToGroupAtPosition(...)` | `CCP.Card` | Removes from the current group and adds to another group. |
| `AddFront` / `AddBack` / `AddAtPosition` | `CCP.CardGroup` | Adds, reparents, scales, and lays out a card. |
| `RemoveFront` / `RemoveBack` / `RemoveAtPosition` / `RemoveCard` | `CCP.CardGroup` | Detaches a card and optionally runs a hide animation and callback. |
| `Front()` / `Back()` / `Shuffle()` | `CCP.CardGroup` | Reads endpoints or randomizes group order. |
| `GetSelectedCards()` / `GetNotSelectedCards()` | `CCP.CardGroup` | Returns copied selection snapshots. |
| `Initialize()` | `CCP.CardGroupInitializer` | Instantiates all configured group entries. |

## Common issues

- **Symptom:** Consumer code cannot resolve `Card` or `CardGroup`. **Cause:** It copied stale global-namespace examples. **Fix:** Add `using CCP;` or use `CCP.Card` and `CCP.CardGroup`.
- **Symptom:** An initializer entry with count `0` still spawns a card. **Cause:** Count is clamped to at least one. **Fix:** Remove or disable the entry instead of setting zero.
- **Symptom:** Delayed initialization pauses at `Time.timeScale = 0`. **Cause:** It uses `WaitForSeconds`. **Fix:** Initialize without delay or implement an explicitly unscaled custom coroutine.
- **Symptom:** Removing an invalid index never calls cleanup. **Cause:** The remove method returns `null` before invoking a callback. **Fix:** validate the index or check the returned card.
- **Symptom:** Editing the result of `GetSelectedCards()` does not change selection. **Cause:** The method returns a copy. **Fix:** let user clicks update selection and use the list only as a query result.
- **Symptom:** A null initializer place animation does not use the card's configured place effect. **Cause:** `CardGroupInitializer` substitutes `InstantPlaceAnimation`. **Fix:** assign the intended initializer place animation explicitly.

## Boundaries

- Public consumer code can query selection but cannot programmatically call the internal `IsSelected`, `SelectCard`, or `DeselectCard` methods. Drive selection through user clicks or build a separate public gameplay selection layer.
- `CardGroup.cards` is public, but direct list mutation bypasses parenting, selection cleanup, and layout; use group methods.
- Initializer delay is scaled time. `CardControllerSettings.useUnscaledTime` controls DOTween effects, not `CardGroupInitializer`'s `WaitForSeconds`.
- Card Controller Pro manages Canvas/UGUI card presentation and interaction; persistence, networking, deck rules, and save/load remain application responsibilities.
