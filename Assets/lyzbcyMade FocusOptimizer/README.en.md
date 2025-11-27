# Focus Stutter Optimizer

## Plugin Overview

Focus Stutter Optimizer is designed to reduce Unity editor stuttering when regaining focus after external script modifications. The plugin pauses automatic asset refresh when the editor loses focus and executes refresh strategies (immediate, throttled, or manual) when focus returns, helping teams control refresh timing.

## Core Features

- **Multiple Refresh Strategies**: Switch freely between immediate refresh, throttled refresh, and manual confirmation.
- **Optional Enter Play Mode Optimization**: One-click toggle to enable/disable domain reload skipping, scene reload skipping, and auto-restore after play mode ends.
- **Refresh Performance Statistics**: Records the last refresh duration and trigger method, helping teams identify slow compilation/import issues.
- **Safe Mode**: Default behavior does not modify project settings; all options can be toggled independently.

## 新版界面亮点

- 顶部英雄横幅（Hero Banner）会以渐变背景展示插件启用状态、当前策略、检测到的外部改动数量以及最近一次刷新耗时，方便团队快速了解风险。
- 快速操作条采用图标+文案的卡片式按钮设计，任何刷新模式下都能一键“立即刷新”或“恢复自动刷新”。
- 刷新模式区域新增胶囊切换与节流进度条，调节延迟时可以直接看到实时比例与建议区间。
- 统计卡与通知区域沿用统一的卡片化语言，文档所述的关键指标在界面中都有直观反馈。

## Installation

1. Keep the `Assets/lyzbcyMade FocusOptimizer` folder in your project; Unity will recognize it as a local package.
2. For distribution, push this directory to a dedicated Git repository and reference it via Git URL in `manifest.json`.

## Usage

1. Open the tool window via `Tools > Focus Stutter Optimizer > Main Window` in Unity's top menu.
2. Select refresh strategy, throttling duration, and notification preferences based on team collaboration needs.
3. To accelerate entering Play Mode, enable Enter Play Mode optimization in the window and check the stages to skip.

## Support

For feedback and suggestions, contact `support@fire-tools.example`. The plugin is open for extension; all core logic is located in the `Editor` directory.

