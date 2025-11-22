# One-Click VR Converter – User Guide

## Table of Contents

- [Before You Start](#before-you-start)
- [Plugin Overview](#plugin-overview)
- [Recommended Use Cases](#recommended-use-cases)
- [Quick Start](#quick-start)
- [UI Overview](#ui-overview)
- [Standard Workflow](#standard-workflow)
- [Professional Mode Workflow](#professional-mode-workflow)
- [Git Assistant Integration](#git-assistant-integration)
- [Feature Details](#feature-details)
- [FAQ](#faq)
- [Best Practices](#best-practices)

---

## Before You Start

- ⚠️ **Expect failures on complex 3D projects.** The converter touches packages, Project Settings, and the active scene. Always save your scene and create a backup (Git commit or copy) before running any step.
- If your studio already uses a Git assistant (Cursor Git Assistant or an internal tool), trigger a quick checkpoint via that assistant first. This guarantees you can roll back when the conversion fails.
- Teams without version control should initialize Git and install the assistant before using the converter; lack of VCS is the number-one risk for unrecoverable states.
- From v1.2 onward every “One-Click Run” or “Step 2” starts with a mandatory confirmation dialog. When a Git assistant is detected you can press “Quick Backup via Git Assistant” right in the popup to auto-commit before continuing.

---

## Plugin Overview

The **One-Click VR Converter** is an editor window that configures a vanilla Unity project into a baseline VR project within minutes. It installs XR dependencies, configures XR Plug-in Management, and builds a ready-to-run VR rig in the current scene.

**Menu Path:** `Tools → VR Converter → Convert Current Project to VR...`

Since v1.3 the window offers **Simple vs. Pro mode switching**, **Compatibility Insights**, and automatic **XR Device Simulator** setup:

- Simple mode keeps the one-click experience and auto-imports XR Device Simulator when missing so you can test without a headset.
- Pro mode lets you pick individual steps, platforms, and scene strategies, preserves the main camera when needed, and exposes a toggle to configure the simulator.
- Compatibility Insights displays rendering pipeline, XR packages, input system, simulator, and Git assistant status with inline suggestions.

---

## Recommended Use Cases

- **First-time VR enablement:** bootstrap all XR dependencies with one button.
- **Upgrading legacy projects:** switch to OpenXR quickly and spawn an XR Origin.
- **Education/trainings:** guided UI helps newcomers understand each step.
- **Team standardization:** unify XR setup to avoid manual inconsistencies.

---

## Quick Start

1. Open the Unity project, save the current scene, and create a Git backup if available.
2. Navigate to `Tools → VR Converter → Convert Current Project to VR...`.
3. Hover over **“One-Click Run (Recommended)”**, read the tooltip, then click it. A backup confirmation dialog appears. When a Git assistant is detected press “Quick Backup via Git Assistant” to auto-commit before proceeding.
4. If the scene has unsaved changes, Unity shows the native save dialog; choose save/discard/cancel accordingly.
5. Wait for Unity to finish package imports and script compilation. The window log records every action.
6. When the XR Device Simulator is missing, the tool copies it from the XR Interaction Toolkit sample into `Assets/VRConverterGenerated/DeviceSimulator` and enables “instantiate in editor only.” Missing `XRI Default Input Actions` are also copied automatically.
7. After compilation you may re-run **“Step 2”** alone to refresh XR settings and the scene; the same backup confirmation is enforced.
8. When the conversion succeeds the **Git Assistant** card reveals a `Rollback to Pre-Conversion` button (requires the assistant). It calls `git reset --hard <baseline>` through the assistant for instant recovery.

---

## UI Overview

### 1. Header Card

- **Title & summary** describing the tool scope.
- **Status bar** showing whether Unity is importing/compiling plus next-step tips.

### 2. Mode Switch + Compatibility Insights

- **Mode toggle** remembers your choice between Simple and Pro.
- **Compatibility card** lays out rendering pipeline, XR Management, OpenXR, XR Interaction Toolkit, Input System, XR Device Simulator, and Git assistant status with suggestions.

### 3. Quick Start Card (Simple Mode)

- **“One-Click Run (Recommended)”** button with tooltip explaining the full workflow.
- **Newcomer hint** encouraging first-time users to start here.

### 4. Step-by-Step Card (Simple Mode)

- **Step 1 – Prepare XR Dependencies**: instals XR packages and shows tooltip details.
- **Step 2 – Configure Project & Scene**: enables OpenXR, updates the scene, and auto-configures the simulator when missing. Buttons disable while Unity compiles.

### 5. Pro Mode Card

- **Step checkboxes** for package checks, Project Settings, and scene conversion.
- **Target platform toggles** for Standalone and Android.
- **Scene strategy selector** (Auto / Update XR Origin / Force base VRRig / Skip scene) plus **Preserve Main Camera**.
- **Simulator toggle** to control whether XR Device Simulator is imported/configured.
- **Execution button** labeled “Run Pro Plan,” also gated by the backup dialog.

### 6. Git Assistant Card

- Shows shortcuts to open the assistant, trigger quick backups, and roll back when available. When the assistant is missing, it displays installation guidance.

### 7. Log Area

- Scrollable log with timestamps for every action, great for troubleshooting.

---

## Standard Workflow

### One-Click Path

1. Press **“One-Click Run (Recommended)”**.
2. The tool runs Step 1 (dependencies) and waits for package import.
3. If Unity is compiling you’ll see a log message and the tool waits automatically.
4. Step 2 configures XR settings and builds the VR rig; the log summarizes results.
5. Missing simulator assets are imported and configured automatically.

### Step-by-Step

1. **Run Step 1 (Dependencies)**:
   - Touches only XR-related entries in `Packages/manifest.json`.
   - Removes any `"latest"` placeholders before reinstalling packages.
   - Shows install progress directly in the log.
2. **Run Step 2 (Config + Scene)**:
   - Requires Unity to finish compiling; clicking shows a confirmation dialog with save options.
   - Enables OpenXR Loader for Standalone and Android, and creates/updates XR Origin.
   - Copies Starter Assets when `XRI Default Input Actions` is missing and rebinds inputs.
   - Falls back to a basic VRRig when XR Interaction Toolkit is unavailable.
   - Simple-mode launches also ensure the simulator is configured.

---

## Professional Mode Workflow

1. Switch the mode toggle to **Pro Mode**.
2. Choose the steps you need (XR package check, Project Settings, scene conversion).
3. Select target platforms; deselecting both reverts to dual-platform defaults.
4. Pick a **scene strategy**:
   - **Auto**: same as Simple mode.
   - **Update Existing XR Origin**: never spawns a new rig.
   - **Force Base VRRig**: skips XRI and creates a lightweight rig.
   - **Skip Scene Conversion**: only handles packages/settings.
   - Optional **Preserve Main Camera** prevents disabling the original camera.
5. Toggle **Configure XR Device Simulator** if you want to import/setup the simulator.
6. Click **“Run Pro Plan”**. The backup confirmation and Git assistant quick-backup flow are identical to Simple mode.
7. Review results in the log; Git assistant actions (backup/rollback) remain available.

---

## Git Assistant Integration

- **Pre-execution confirmation**: both One-Click and Step 2 prompt a dialog requiring manual confirmation or a Git assistant quick backup (`git add -A` + `git commit`).
- **Quick backup button**: always available in the Git assistant card to create a “Before VR Conversion” commit.
- **Rollback button**: unlocked after a successful run once a baseline commit exists; triggers `git reset --hard <baseline>`.
- **Guidance when missing**: the card displays instructions to install `com.fire.gitassistant` when not detected.

---

## Feature Details

### XR Dependency Management

- Ensures the following packages exist:
  - `com.unity.xr.management`
  - `com.unity.xr.openxr`
  - `com.unity.xr.interaction.toolkit`
  - `com.unity.inputsystem`
- Removes `"latest"` placeholders in `manifest.json`.
- Uses `UnityEditor.PackageManager.Client.Add` for installations and logs status.

### XR Plug-in Management Configuration

- Creates/reuses `XRGeneralSettingsPerBuildTarget` at `Assets/VRConverterGenerated/XR/XRGeneralSettings.asset`.
- For Standalone and Android:
  - Generates XR General/Manager Settings assets.
  - Enables `Init Manager On Start`.
  - Uses `XRPackageMetadataStore.AssignLoader` to activate OpenXR Loader.

### Scene VR Rig Construction

- Disables the legacy `Main Camera` to avoid double rendering.
- When XR Interaction Toolkit is present:
  - Creates/updates `XR Origin (Action Based)` hierarchy.
  - Sets up Camera Offset, Main Camera, left/right controllers.
  - Adds `Camera`, `AudioListener`, `TrackedPoseDriver`, `ActionBasedController`, `XRRayInteractor`, `LineRenderer`, etc.
  - Spawns singletons for `XR Interaction Manager` and `XR Input Action Manager`.
  - Locates or copies `XRI Default Input Actions` and binds left/right action maps automatically.
- Without XRI types it generates a minimal `VRRig` with camera offset and controller placeholders.

### Logging & Feedback

- Timestamps every action, including install progress, configuration status, and failures.
- Highlights blocking states such as “Unity is compiling.”
- Provides failure reasons for package installs, loader assignment, and scene updates.

### Compatibility Insights

- Detects rendering pipeline (Built-in/URP/HDRP), XR packages, Input System status, simulator configuration, and Git assistant availability.
- Each row includes practical guidance to resolve missing requirements.

### XR Device Simulator Automation

- Simple mode auto-imports the sample into `Assets/VRConverterGenerated/DeviceSimulator` when missing.
- Configures the simulator to instantiate in editor only to avoid build bloat.
- Pro mode exposes a checkbox so advanced teams can opt out.

---

## FAQ

**Q1: Buttons are greyed out.**  
A1: Unity is importing or compiling scripts. Wait until the header status bar reports “Idle,” then retry.

**Q2: Step 1 completed but nothing changed.**  
A2: Package installs may take time and trigger recompiles. After compilation finishes, run Step 2 or One-Click again.

**Q3: Log shows “XR Interaction Toolkit missing.”**  
A3: Confirm the package finished installing. The converter will fall back to the base VRRig so you can still preview the scene.

**Q4: OpenXR Loader not activated.**  
A4: API differences in `XRPackageMetadataStore` can block assignment. Open `Project Settings → XR Plug-in Management` and enable OpenXR manually; the log explains which call failed.

**Q5: What if I already have a custom XR rig?**  
A5: The tool tries to reuse existing `XR Origin` or `VRRig`. When in doubt, create a Git checkpoint or scene copy before running.

**Q6: Console errors after conversion.**  
A6: Read the window log to identify the failed step, then inspect Unity’s Console. Frequent causes include package install failures, compile errors, or locked scene objects.

**Q7: Starter Assets missing `XRI Left` maps.**  
A7: XR Interaction Toolkit 2.6 renamed action maps to `XRI LeftHand` / `XRI RightHand`. The converter supports both naming conventions automatically.

**Q8: “XR Device Simulator prefab was missing...”**  
A8: Simple mode now re-imports the sample automatically, but if you disabled the step or deleted generated assets, re-run One-Click or enable the simulator option in Pro mode. As a fallback, import the sample manually from Package Manager and re-check the simulator option under `Project Settings → XR Plug-in Management → XR Interaction Toolkit`.

---

## Best Practices

1. **Backup or commit first.** Manifest, Project Settings, and scenes all change; leverage the Git assistant for one-click commits.
2. **Keep the network stable.** Installing XR packages relies on Unity’s registry.
3. **Run steps separately for production projects.** Execute Step 1, verify the outcome, then continue with Step 2.
4. **Read the log.** Most issues are explained right in the window.
5. **Track generated assets.** Everything lives under `Assets/VRConverterGenerated`, making it easy to review or roll back in version control.

---

Happy converting! Share feedback so we can keep improving the experience. 🚀











