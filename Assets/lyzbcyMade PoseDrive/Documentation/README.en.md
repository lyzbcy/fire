# PoseController Plugin

PoseController is an AI-based motion capture input plugin for Unity using Unity Barracuda. It enables real-time human pose recognition via camera and maps gestures/actions to virtual keys, providing an alternative control method beyond keyboard, mouse, and gamepad.

## Key Features

- 🔍 **MoveNet Pose Detection**: Real-time 33 keypoints output with confidence values and smoothing.
- 🧠 **Action Classifier**: Supports user-trained `action_classifier.onnx` models to recognize actions like nod, shake, wave, grab, etc.
- 🎮 **Virtual Input System**: Unified wrapper for keyboard, mouse, and gamepad virtual keys with runtime mapping and key press simulation.
- 🪟 **Editor Tools**: Built-in control panel, mapping editor, and "Quick Setup Wizard" for camera preview, skeleton overlay, gesture recognition, and one-click runtime setup.
- 🧩 **Sample Scenes**: Includes third-person and first-person demo scenes showcasing movement, camera control, jump, and interaction.
- 📦 **Complete Documentation**: Installation, configuration, FAQ, and custom model workflow included.

## Directory Structure

```
PoseController/
 ├─ Runtime/  Core runtime code and model placeholders
 ├─ Editor/  Control panel and mapping editor
 ├─ Samples~/ThirdPersonDemo/  Third-person demo
 ├─ Samples~/FirstPersonDemo/  First-person demo
 └─ Documentation/  Documentation
```

## Requirements

- Unity 2021 LTS or later
- Barracuda 3.0+
- Camera support for UWP/Windows/macOS (USB or built-in)

## Installation

1. Copy the `PoseController` folder to your Unity project's `Assets/` directory.
2. Open Package Manager and ensure **Barracuda** is installed.
3. Replace the placeholder files in `Runtime/Models` with real `movenet.onnx` and `action_classifier.onnx` model files.
4. Use the menu `Tools/PoseController/Setup Wizard` to create runtime components and assign models/mappings, or open sample scenes and enter Play Mode to verify camera and skeleton recognition.

## Compatibility

| Platform | Status |
| ---- | ---- |
| Windows | ✅ Tested |
| macOS | ✅ Camera permission required |
| Linux | ⚠️ Theoretically supported, requires Barracuda and camera drivers |

## License

This plugin is distributed with the project and can be freely used and modified within your team. Please include this documentation when redistributing.

