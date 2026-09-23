---
name: "card-controller-pro-extend-behavior-and-animations"
description: "Use this skill whenever a user asks to extend Card Controller Pro behavior or animations — e.g. \"make cards poison targets\", \"add a bounce animation\", \"hook into card use\", \"play an effect when an attack hits\", or \"make a custom Inspector animation\". It covers CCP.CardData lifecycle overrides, CCP.Card Action subscriptions, and serializable animation strategies while protecting callback and public-state contracts. Do NOT use for layout setup or prefab wiring (see card-controller-pro-create-cards-and-layouts) or for replacing pointer handlers. When in doubt whether custom card logic could involve Card Controller Pro, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Extend Card Controller Pro Behavior and Animations

Add customer-owned card rules, external event listeners, and Inspector-selectable animation strategies through Card Controller Pro's public extension points.

## When to use this skill

- Add card-specific damage, healing, status, scoring, or other lifecycle behavior.
- Notify audio, VFX, UI, analytics, or game-state systems when a card interaction occurs.
- Add a reusable Place, Attack, Hover, Pickup, Select, Use, or Hide animation to the managed-reference dropdowns.
- Diagnose a custom animation that never appears, loses serialized values, stalls callbacks, or leaves a card stuck.

Not for:

- Creating card assets, groups, layouts, or prefab hierarchy; use `card-controller-pro-create-cards-and-layouts`.
- Configuring built-in click and visual effects; use `card-controller-pro-click-and-visual-effects`.
- Replacing `CCP.Card` pointer handlers or accessing package internals.

## Prerequisites

- Use Card Controller Pro 1.0.4 with Unity 6000.0+, DOTween or DOTween Pro, Input System, and UGUI.
- Put new scripts in a customer-owned folder outside `Assets/Plugins/CardControllerPro`.
- Keep extension scripts in an assembly that can resolve the current `CCP` runtime types, DOTween, and `MackySoft.SerializeReferenceExtensions`.
- Confirm installation by compiling:

```csharp
Debug.Log(typeof(CCP.CardData).FullName);
Debug.Log(typeof(CCP.IAttackAnimation).FullName);
```

Both names must resolve in `CCP` with no Console errors. If either fails, tell the user that Card Controller Pro 1.0.4 is missing or incompatible and provide `https://assetstore.unity.com/packages/tools/gui/card-controller-pro-370860`.

## Quick start

1. Create this customer-owned runtime script:

```csharp
using UnityEngine;

namespace MyGame.Cards
{
    [CreateAssetMenu(
        fileName = "NewLoggedAttackCard",
        menuName = "My Game/Cards/Logged Attack")]
    public sealed class LoggedAttackCardData : CCP.CardData
    {
        public override void InitializeCard(CCP.Card card)
        {
            base.InitializeCard(card);
        }

        public override void OnAttackHit(CCP.Card attacker, CCP.Card target)
        {
            Debug.Log($"{attacker.name} hit {target?.name ?? "no target"}.", attacker);
        }
    }
}
```

2. Let Unity compile, then choose **Assets > Create > My Game > Cards > Logged Attack**.
3. Assign front/back art, set **Card Type** to **Attack**, and assign the new asset to a configured card prefab or instance.
4. Enter Play mode and complete one valid attack.

Expected result: base initialization applies the configured art, and the Console records exactly one hit message at the attack animation's impact point.

## Workflows

### Workflow: Extend CardData behavior

**Goal**

Attach reusable game rules to a specific CardData asset type.

**Steps**

1. Subclass `CCP.CardData` in customer code and add a game-owned `[CreateAssetMenu]` path.
2. Override only the lifecycle needed: `InitializeCard`, `OnPickedUp`, `OnDropped`, `OnPlaced`, `OnTargetingStart`, `OnAttackHit`, `OnUsed`, or `OnTargetedUse`.
3. Always call `base.InitializeCard(card)` when overriding initialization because the base assigns non-null front/back sprites to their UGUI Images.
4. Null-check optional cards, groups, and game-owned components before applying effects.
5. Configure the created subclass asset on a `CCP.Card` and exercise the matching interaction.

**Expected result**

The CardData hook runs at the package-defined lifecycle point; for matching events, the hook runs before the `CCP.Card` Action delegate.

### Workflow: Subscribe an external system to Card events

**Goal**

Keep audio, VFX, UI, and orchestration outside the CardData asset.

**Steps**

1. Subscribe with `+=` while the listener is enabled and unsubscribe with `-=` when disabled.
2. Match the delegate shape: no argument for pickup/drop/place/targeting state, `CCP.Card` for attack/targeted use, and `CCP.CardGroup` for untargeted use.
3. Avoid anonymous lambdas when the listener must unsubscribe later; store a delegate or use a named method.

```csharp
using UnityEngine;

namespace MyGame.Cards
{
    public sealed class CardAttackListener : MonoBehaviour
    {
        [SerializeField] private CCP.Card card;

        private void OnEnable()
        {
            if (card != null)
                card.onAttackHit += HandleAttackHit;
        }

        private void OnDisable()
        {
            if (card != null)
                card.onAttackHit -= HandleAttackHit;
        }

        private void HandleAttackHit(CCP.Card target)
        {
            Debug.Log($"External listener saw target {target?.name ?? "none"}.", this);
        }
    }
}
```

**Expected result**

The listener runs once after the matching CardData hook and stops receiving events after it is disabled.

### Workflow: Add an Inspector-selectable attack animation

**Goal**

Create a serializable strategy that appears in CardData's Attack Animation dropdown and completes safely.

**Steps**

1. Implement the exact public interface in customer code.
2. Add `[System.Serializable]` and `[MackySoft.SerializeReferenceExtensions.AddTypeMenu("Custom/Impact Lunge", 1000)]`.
3. Expose tunable values with `[SerializeField]`; do not store scene-object references in the strategy.
4. Preserve and restore any public transform/state changed by the animation.
5. Invoke `onAttack` once at impact and `onComplete` once after restoration. Invoke completion on safe early exits too.
6. Use `CCP.Card.SetAttackingState` rather than internal state fields, and honor `CardControllerSettings.Instance.useUnscaledTime`.

```csharp
using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace MyGame.CardAnimations
{
    [Serializable]
    [AddTypeMenu("Custom/Impact Lunge", 1000)]
    public sealed class ImpactLungeAnimation : CCP.IAttackAnimation
    {
        [SerializeField, Min(0f)] private float duration = 0.25f;
        [SerializeField] private Ease ease = Ease.OutQuad;

        public void Attack(
            CCP.Card attacker,
            Transform target,
            Action onAttack,
            Action onComplete)
        {
            if (attacker == null || target == null || attacker.cardVisuals == null)
            {
                onComplete?.Invoke();
                return;
            }

            Transform visuals = attacker.cardVisuals.transform;
            Vector3 originalLocalPosition = visuals.localPosition;
            bool finished = false;

            void Finish()
            {
                if (finished)
                    return;

                finished = true;
                if (visuals != null)
                    visuals.localPosition = originalLocalPosition;
                if (attacker != null)
                    attacker.SetAttackingState(false);
                onComplete?.Invoke();
            }

            visuals.DOKill();
            attacker.SetAttackingState(true);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(visuals.DOMove(target.position, duration).SetEase(ease));
            sequence.AppendCallback(() => onAttack?.Invoke());
            sequence.Append(visuals.DOLocalMove(originalLocalPosition, duration).SetEase(ease));
            sequence.AppendCallback(Finish);
            sequence.SetUpdate(CCP.CardControllerSettings.Instance.useUnscaledTime);
            sequence.SetLink(attacker.gameObject);
            sequence.OnKill(Finish);
        }
    }
}
```

7. Let Unity compile, select a CardData asset, choose **Custom/Impact Lunge**, change a value, save, reload, and attack twice.

**Expected result**

The strategy survives serialization, fires hit before completion, restores the visuals and attack state on completion or kill, respects paused-time settings, and works repeatedly.

## Verification

- Confirm all generated scripts compile with current `CCP` types and live outside the plugin folder.
- Confirm an overridden `InitializeCard` calls the base implementation and applies artwork.
- Record lifecycle order and verify each CardData hook precedes its matching Card Action.
- Disable/re-enable external listeners and verify invocation counts do not accumulate.
- Save and reload the scene/CardData asset and confirm a custom strategy plus its field values remain selected.
- Exercise normal completion, null input, tween interruption, repeated use, and `Time.timeScale == 0` with unscaled time enabled.
- Confirm callbacks fire exactly once in the required order and public state/transforms are restored.
- Confirm the Console has no compile errors or unhandled exceptions.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `InitializeCard`, pickup/drop/place, targeting, attack, and use hooks | `CCP.CardData` | Supplies card-intrinsic virtual lifecycle behavior. |
| `onPickedUp`, `onDropped`, `onPlaced`, `onTargetingStarted` | `CCP.Card` | Notifies external systems with no-argument Actions. |
| `onAttackHit`, `onTargetedUse`, `onUsed` | `CCP.Card` | Notifies external systems with a target Card or CardGroup. |
| `onTargeted`, `onUntargeted` | `CCP.Card` | Notifies a target card when targeting enters or exits. |
| `IPlaceAnimation`, `IAttackAnimation`, `IUseAnimation`, `IHideAnimation` | `CCP` interfaces | Defines callback-based animation strategies. |
| `IHoverAnimation`, `IPickupAnimation`, `ISelectAnimation` | `CCP` interfaces | Defines paired enter/exit visual strategies. |
| `SetAttackingState(bool)` | `CCP.Card` | Exposes supported attack-state mutation to custom strategies. |
| `AddTypeMenuAttribute` | `MackySoft.SerializeReferenceExtensions` | Adds a serializable strategy to the Inspector dropdown. |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| A custom strategy is absent from the dropdown. | It lacks `[Serializable]`, `AddTypeMenu`, an exact interface, or a compatible assembly reference. | Restore all discovery requirements and recompile. |
| A strategy resets after reload. | Its fields are not serializable or the class moved/renamed. | Use supported serialized fields and keep the managed-reference type stable. |
| Placement/use/attack logic never finishes. | The strategy omitted or repeated its completion callback. | Guard and invoke completion exactly once on every supported path. |
| A card remains in attack state after interruption. | The strategy restores state only on normal completion. | Add idempotent cleanup for completion and tween kill. |
| Base card art disappears. | An `InitializeCard` override skipped its base call. | Call `base.InitializeCard(card)` before custom initialization. |
| Listener callbacks multiply. | Subscriptions are repeated or never removed. | Pair `+=` in `OnEnable` with `-=` in `OnDisable`. |
| Customer code cannot access `isBeingPlaced` or original-scale fields. | Those package members are internal. | Use only public members; do not copy stale samples that access internals. |

## Boundaries

- Do not edit plugin source to add game-specific rules or animation classes.
- Do not replace Card Controller Pro pointer handlers when lifecycle hooks or Actions solve the task.
- Do not access `Card.isBeingPlaced`, `isAttacking`, `isBeingUsed`, `originalVisualsScale`, or other internal members from customer assemblies.
- Custom strategies own their validation, callback ordering, tween cleanup, state restoration, and unscaled-time behavior.
- Card Controller Pro does not provide game-specific damage, costs, turns, networking, persistence, or undo; keep those systems in customer code.
