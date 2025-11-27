# One-Click VR Project Converter

## Plugin Overview

One-Click VR Project Converter helps you quickly configure a basic VR environment for existing Unity projects. The tool automates XR package installation, XR settings configuration, and VR Rig creation, reducing setup time from hours to minutes.

## Core Features

- **One-Click Full Setup**: Automatically installs XR dependencies, configures XR settings, and creates VR Rig in a single operation.
- **Step-by-Step Mode**: Execute installation and configuration separately for more control.
- **Automatic Package Management**: Detects and installs required XR packages (XR Management, OpenXR, XR Interaction Toolkit, Input System).
- **Smart VR Rig Creation**: Creates XR Origin with Action-Based Controllers, or falls back to basic VR Rig if XR Interaction Toolkit is unavailable.
- **Input Action Auto-Binding**: Automatically copies and binds XRI Default Input Actions from Starter Assets Sample.
- **Platform Support**: Configures XR settings for Standalone and Android platforms.

## Installation

1. Keep the `Assets/lyzbcyMade VRConverter` folder in your project; Unity will recognize it as a local package.
2. For distribution, push this directory to a dedicated Git repository and reference it via Git URL in `manifest.json`.

## Prerequisites

- Unity 2021.3 LTS or higher (with Unity XR module installed).
- Project write permissions (to modify `Packages/manifest.json` and generate `Assets/VRConverterGenerated` resources).

## Quick Start

1. Open Unity and wait for scripts to compile.
2. Navigate to `Tools > VR Converter > Convert Current Project to VR...`.
3. Click **"Execute All Steps (Recommended)"**.
4. Wait for package installation and compilation to complete.
5. The tool automatically configures XR settings and creates VR Rig in the current scene.

## What It Does

### Step 1: XR Package Installation
- Checks `Packages/manifest.json` for required packages:
  - `com.unity.xr.management`
  - `com.unity.xr.openxr`
  - `com.unity.xr.interaction.toolkit`
  - `com.unity.inputsystem`
- Automatically installs missing packages or updates to compatible versions.

### Step 2: XR Configuration & VR Rig Creation
- Creates `XRGeneralSettings` and `XRManagerSettings` for Standalone/Android.
- Enables OpenXR Loader via XR Plug-in Management API.
- Creates/updates `XR Origin (Action Based)` in the current scene with:
  - Camera Offset
  - Main Camera
  - Left/Right Hand Controllers
  - Tracked Pose Drivers
  - Action-Based Controllers
  - XR Ray Interactors
  - Line Renderers
  - XR Interaction Manager
  - Input Action Manager
- Automatically disables the original Main Camera in the scene.
- Falls back to basic VR Rig if XR Interaction Toolkit is unavailable.

## Customization

All UI logic is located in `Editor/VRProjectConverterWindow.cs`. The tool can be extended to support additional XR providers, custom VR Rig configurations, or additional platform settings.

## Support

For feedback and suggestions, contact `support@fire-tools.example`. The plugin is open for extension; all core logic is located in the `Editor` directory.

