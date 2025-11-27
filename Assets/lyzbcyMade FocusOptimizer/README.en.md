# Focus Stutter Optimizer

## Plugin Overview

Focus Stutter Optimizer is designed to reduce Unity editor stuttering when regaining focus after external script modifications. The plugin pauses automatic asset refresh when the editor loses focus and executes refresh strategies (immediate, throttled, or manual) when focus returns, helping teams control refresh timing.

## Core Features

- **Multiple Refresh Strategies**: Switch freely between immediate refresh, throttled refresh, and manual confirmation.
- **Optional Enter Play Mode Optimization**: One-click toggle to enable/disable domain reload skipping, scene reload skipping, and auto-restore after play mode ends.
- **Refresh Performance Statistics**: Records the last refresh duration and trigger method, helping teams identify slow compilation/import issues.
- **Safe Mode**: Default behavior does not modify project settings; all options can be toggled independently.

## Installation

1. Keep the `Assets/lyzbcyMade FocusOptimizer` folder in your project; Unity will recognize it as a local package.
2. For distribution, push this directory to a dedicated Git repository and reference it via Git URL in `manifest.json`.

## Usage

1. Open the tool window via `Tools > Focus Stutter Optimizer > Main Window` in Unity's top menu.
2. Select refresh strategy, throttling duration, and notification preferences based on team collaboration needs.
3. To accelerate entering Play Mode, enable Enter Play Mode optimization in the window and check the stages to skip.

## Support

For feedback and suggestions, contact `support@fire-tools.example`. The plugin is open for extension; all core logic is located in the `Editor` directory.

