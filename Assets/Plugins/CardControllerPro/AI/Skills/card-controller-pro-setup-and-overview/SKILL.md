---
name: "card-controller-pro-setup-and-overview"
description: "Use this skill whenever a Unity user asks \"is Card Controller Pro installed?\", \"set up Card Controller Pro\", \"what comes with the asset?\", or needs dependency, settings, demo, prefab, compatibility, or package-orientation help. It covers installation checks, Unity 6+, DOTween, Input System, UGUI, render-pipeline independence, shipped content, and global settings. Do NOT use for authoring cards or hand layouts; use card-controller-pro-create-cards-and-layouts. When in doubt whether a setup request could involve Card Controller Pro, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Card Controller Pro setup and overview

Set up and inspect Card Controller Pro as a Canvas/UGUI card toolkit whose current public API is entirely in the `CCP` namespace.

## When to use this skill

- Confirm that the asset and its dependencies are present.
- Explain compatibility, supplied files, prefabs, and demos.
- Create and place the global settings asset.
- Establish the correct API namespace before writing code.
- Do NOT use for creating `CardData`, card prefabs, or `CardGroup` layouts; use `card-controller-pro-create-cards-and-layouts`.
- Skill index: `card-controller-pro-create-cards-and-layouts`; `card-controller-pro-drag-drop-and-reorder`; `card-controller-pro-attack-use-and-targeting`; `card-controller-pro-manage-cards-at-runtime`; `card-controller-pro-click-and-visual-effects`; `card-controller-pro-extend-behavior-and-animations`.

## Prerequisites

- Use Unity 6000.0 or newer.
- Install and set up DOTween or DOTween Pro. Card Controller Pro calls `DG.Tweening` directly.
- Install the Input System package. `CCP.Card` reads `UnityEngine.InputSystem.Mouse`.
- Install UGUI. Cards require `Canvas`, `RectTransform`, `CanvasGroup`, `Image`, and EventSystem pointer events.
- Use Built-in, URP, or HDRP. Runtime card rendering is UGUI-based and contains no render-pipeline-specific API.
- Let Unity finish importing and compiling before judging installation state.
- Confirm installation by compiling `Debug.Log(typeof(CCP.Card).FullName);`. If it does not resolve to `CCP.Card`, tell the user that Card Controller Pro 1.0.4 is missing or incompatible and provide `https://assetstore.unity.com/packages/tools/gui/card-controller-pro-370860`.

## Quick start

1. In the Project window, confirm `Assets/Plugins/CardControllerPro/Runtime/Card.cs` exists, or compile code that resolves `CCP.Card`.
2. Open **Window > Package Manager** and confirm **Input System** and **Unity UI** are installed.
3. Open **Tools > Demigiant > DOTween Utility Panel**, run **Setup DOTween...** when required, and clear compile errors.
4. In a writable project folder, choose **Assets > Create > CardControllerPro > Settings**.
5. Name the asset `CardControllerSettings.asset` and place it directly in any `Resources` folder, for example `Assets/Resources/CardControllerSettings.asset`.
6. Add an EventSystem when the scene has none by choosing **GameObject > UI > Event System**.
7. Add a Canvas when the scene has none by choosing **GameObject > UI > Canvas**.

Use this compile-time probe only when a source-level installation check is useful:

```csharp
using UnityEngine;

namespace MyGame
{
    public sealed class CardControllerProInstallProbe : MonoBehaviour
    {
        [SerializeField] private CCP.Card card;

        private void Awake()
        {
            Debug.Log(card != null
                ? $"Card Controller Pro found on {card.name}."
                : "CCP.Card compiled; assign a card to complete the scene check.");
        }
    }
}
```

Expected result: `CCP.Card` compiles, the scene has one Canvas and EventSystem, a Resources-backed settings asset exists, and the Console contains no missing-dependency errors.

## Workflows

### Workflow: Verify installation and dependencies

**Goal**

Prove that the current package can compile and receive UGUI input.

**Steps**

1. Confirm either `Assets/Plugins/CardControllerPro/Runtime/Card.cs` exists or `CCP.Card` resolves in C#.
2. Confirm the project editor version is Unity 6000.0+ from `ProjectSettings/ProjectVersion.txt`, the Unity title bar, or the Unity Hub project entry.
3. Confirm `com.unity.inputsystem` and `com.unity.ugui` in **Window > Package Manager**.
4. Confirm DOTween is imported and configured through **Tools > Demigiant > DOTween Utility Panel**.
5. Open a scene with a Canvas and EventSystem.
6. Enter Play mode and inspect **Window > General > Console**.

**Expected result**

`CCP.Card` compiles, the Canvas receives pointer input, and the Console contains no missing-package or missing-type errors.

### Workflow: Inspect supplied content

**Goal**

Find reusable production assets and distinguish demonstrations from runtime API.

**Steps**

1. Inspect `Prefabs/Card.prefab`, `Prefabs/Node.prefab`, and `Prefabs/Head.prefab`.
2. Inspect `Demo/Prefabs/TCGCard.prefab` and `Demo/Prefabs/BalatroCard.prefab` as styled examples.
3. Open `Demo/TCG Demo.unity`, `Demo/TCG Demo Perspective Camera.unity`, `Demo/Poker Demo.unity`, or `Demo/Balatro Demo.unity` to study configured scenes.
4. Treat `Runtime/` and `Animations/` as the production API implementation.
5. Treat `Editor/CardEditor.cs` and `Editor/CardGroupEditor.cs` as custom Inspectors only.
6. Treat `Demo/Scripts/` and `Demo/CardData/` as examples, not production APIs. Use `CCP.CardGroupInitializer` for production scene population instead of demo dealer/tester scripts.
7. Keep the vendored `SerializeReferenceExtensions/` files with the asset; they power managed-reference animation dropdowns.
8. Consult `README.md` for orientation, but prefer current `CCP` source and serialized prefab data if prose differs.

**Expected result**

Production code references `CCP` runtime types, uses the included base prefabs where suitable, and does not depend on demo-only scripts.

### Workflow: Create global settings

**Goal**

Configure whether card animations ignore `Time.timeScale`.

**Steps**

1. In the Project window, open the destination `Resources` folder.
2. Choose **Assets > Create > CardControllerPro > Settings**.
3. Keep the generated name `CardControllerSettings` so `Resources.Load<CCP.CardControllerSettings>("CardControllerSettings")` finds it.
4. Leave **Use Unscaled Time** disabled for scaled animation time, or enable it so package tweens continue while paused.
5. Create only one authoritative settings asset with that Resources path.

**Expected result**

`CCP.CardControllerSettings.Instance` loads the asset; without one, it creates an in-memory instance with `useUnscaledTime == false`.

## Verification

- Confirm `CCP.Card`, `CCP.CardGroup`, `CCP.CardData`, `CCP.CardGroupInitializer`, and `CCP.CardControllerSettings` resolve.
- Confirm the active scene has a Canvas and EventSystem.
- Confirm the card root has both `RectTransform` and `CanvasGroup`.
- Confirm the Console has no compiler errors and no missing `DG.Tweening`, Input System, or UGUI types.
- Confirm settings load by checking the intended **Use Unscaled Time** value in Play mode.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `CCP.Card` | `MonoBehaviour` | Handles pointer input, drag, flip, targeting, and card state. |
| `CCP.CardData` | `ScriptableObject` | Stores card configuration and virtual lifecycle hooks. |
| `CCP.CardGroup` | `MonoBehaviour` | Maintains ordered cards, layout, hover, selection, and reordering. |
| `Initialize()` | `CCP.CardGroupInitializer` | Populates configured groups; `Start()` calls it automatically. |
| `CCP.CardDropZone` | `MonoBehaviour` | Receives UGUI drops for place/use cards. |
| `Instance` | `CCP.CardControllerSettings` | Loads `Resources/CardControllerSettings` or creates an in-memory default. |
| `PointAt(...)` / `Hide()` | `CCP.DynamicArrow` | Displays or hides the pooled targeting arrow. |
| `Place`, `Attack`, `Static`, `Use`, `TargettedUse` | `CCP.CardType` | Defines the configured interaction type. |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| `Card` or `CardGroup` cannot be found. | Old examples omit the current namespace. | Use `CCP.Card` and `CCP.CardGroup`. |
| `DG.Tweening` cannot be found. | DOTween is absent or not set up. | Import DOTween or DOTween Pro and run its utility-panel setup. |
| `UnityEngine.InputSystem` cannot be found. | Input System is not installed. | Install **Input System** in Package Manager. |
| Pointer actions do nothing. | EventSystem, GraphicRaycaster, or the transparent **Click Target** raycast is missing. | Restore the UGUI input path and raycastable click target. |
| Global settings are ignored. | The asset has the wrong name/path or duplicates compete. | Keep one `CardControllerSettings.asset` under a `Resources` folder. |
| No package setup window exists. | Card Controller Pro defines no package-owned `EditorWindow` or `MenuItem`. | Use its two `CreateAssetMenu` commands and custom Inspectors. |

## Boundaries

- Do not claim a render pipeline is required; the card UI is pipeline-independent UGUI.
- Do not remove or rewrite vendored SerializeReferenceExtensions files during setup.
- Do not present `CardTester`, `DemoCardDealer`, `PokerDemoCardDealer`, or `CardConfirmerDemo` as production APIs.
- Do not use stale examples that omit the `CCP` namespace.
- Do not invent package-owned menus beyond **Assets > Create > CardControllerPro > Card Data** and **Assets > Create > CardControllerPro > Settings**.
