# One-Click VR Project Converter – User Guide

## Opening the Tool

1. Start Unity and open your project.
2. Wait for scripts to compile.
3. Navigate to `Tools > VR Converter > Convert Current Project to VR...`.
4. The window provides three operations:
   - **Execute All Steps (Recommended)**
   - **Step 1: Check and Add XR Dependencies Only**
   - **Step 2: Configure XR Settings + Create/Update Scene VR Rig**

## Recommended Workflow: Execute All Steps

1. Click **"Execute All Steps (Recommended)"**.
2. The tool checks `Packages/manifest.json` to ensure the following packages exist:
   - `com.unity.xr.management`
   - `com.unity.xr.openxr`
   - `com.unity.xr.interaction.toolkit`
   - `com.unity.inputsystem`
3. If packages are missing or have invalid versions, the tool automatically installs/updates them via Package Manager and outputs results in the log. Unity will reimport and recompile after installation.
4. After compilation completes (`EditorApplication.isCompiling == false`), the tool automatically:
   - Creates and registers `XRGeneralSettings` and `XRManagerSettings` for Standalone/Android.
   - Enables OpenXR Loader via XR Plug-in Management API.
   - Creates/updates `XR Origin (Action Based)` in the current scene. If XR Interaction Toolkit is unavailable, it falls back to a basic `VR Rig`.

## Manual Workflow (Step-by-Step Execution)

### Step 1: Ensure XR Dependencies

Suitable for projects that have just imported the tool, haven't added XR packages yet, or don't have Unity's new Input System.

1. Click **"Step 1: Check and Add XR Dependencies Only"**.
2. The tool removes `"latest"` placeholders in `manifest.json` and installs missing packages.
3. Monitor the log to confirm installation completion. If prompted to wait for compilation, wait before executing Step 2.

### Step 2: Configure XR Settings + Scene Conversion

Execute this after XR packages are successfully imported and Unity is no longer compiling.

1. Confirm the status bar shows no *Compiling Scripts*.
2. Click **"Step 2: Configure XR Settings + Create/Update Scene VR Rig"**.
3. Results include:
   - Generation of `Assets/VRConverterGenerated/XR/XRGeneralSettings.asset` (platform-specific assets).
   - Registration of corresponding XR General Settings in `EditorBuildSettings`.
   - Creation of `XR Manager Settings` for Standalone/Android with OpenXR Loader enabled.
   - Generation of XR Origin in the current scene (includes Camera Offset, Main Camera, left/right hand controllers, Tracked Pose Drivers, Action-Based Controllers, XR Ray Interactors, Line Renderers, XR Interaction Manager, Input Action Manager).
   - If the project lacks `XRI Default Input Actions`, the tool attempts to automatically copy required input assets from XR Interaction Toolkit's Starter Assets Sample and rebind them.
   - If XR Interaction Toolkit is unavailable, generates a basic `VRRig` (main camera + left/right hand empty nodes).
   - Automatically disables the original Main Camera in the scene if detected.

## Common Issues

- **"XR Management assembly not detected"**: XR packages are still importing or version mismatch. Wait for compilation to complete and retry Step 2.
- **Controllers have no input behavior**: The tool attempts to automatically copy XR Interaction Toolkit's Starter Assets Sample and bind `XRI Default Input Actions`. If the log still indicates not found, manually import the Sample via Package Manager.
- **XRI Action Map naming changes**: XR Interaction Toolkit 2.6+ renamed `XRI Left`/`XRI Right` to `XRI LeftHand`/`XRI RightHand`. The tool automatically handles both naming conventions; no manual script changes needed.
- **Scene already has custom XR structure**: The tool attempts to reuse and supplement missing nodes. It's recommended to backup the scene before execution.

## Log Viewing

The "Execution Log" at the bottom of the tool window records all operations in real-time for troubleshooting. Logs are not persisted; copy manually if you need to save them.

## Post-Conversion Recommendations

- In `Project Settings > XR Plug-in Management`, confirm OpenXR is enabled and enable corresponding Feature Groups for your target platform (e.g., Oculus Quest Support).
- Add Action Maps, gesture interactions, haptic feedback, and other systems as needed for your business requirements. The current tool only generates a runnable basic XR Rig.
- If your project uses CI/CD, call this tool in command-line mode or reuse the assets it generates.

## Troubleshooting

- **Packages not installing**: Check internet connection and Unity Package Manager access. Ensure you have write permissions to `Packages/manifest.json`.
- **VR Rig not appearing**: Check the scene hierarchy. The tool creates XR Origin at the root level. Ensure the scene is saved.
- **Input actions not working**: Verify that XR Interaction Toolkit Starter Assets Sample is imported. Check the Execution Log for specific error messages.
- **OpenXR not enabled**: Manually enable it in `Project Settings > XR Plug-in Management > OpenXR` if automatic enabling fails.

## Keyboard & UX Tips

- The Execution Log scrolls automatically to show the latest operations.
- All operations are logged with timestamps for easy debugging.
- The tool window can be docked alongside other Unity panels.
- Use Step-by-Step mode if you need to inspect intermediate states.

## Feedback

Please send bug reports or feature requests to `support@fire-tools.example`. Screenshots and reproduction steps are appreciated.

