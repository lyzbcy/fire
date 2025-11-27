# Focus Stutter Optimizer – User Guide

## Opening the Tool

1. Start Unity and open your project.
2. Navigate to `Tools > Focus Stutter Optimizer > Main Window`.
3. The window can be docked like any other Unity editor panel.

## 界面速览

- **英雄横幅**：窗口顶部的渐变背景区域会实时展示插件启用状态、所选刷新模式、最近一次检测到的外部改动数量与刷新耗时，并附带颜色提示；若外部改动超过阈值，标签会转为警示色。
- **快捷操作条**：位于横幅下方，提供图标化的“快速刷新”“恢复自动刷新”按钮，任何刷新策略下都能即时触发。
- **刷新策略卡片**：以卡片形式排列，每块区域提供标题+说明+操作控件。刷新模式选择器使用胶囊按钮，限流模式下会出现延迟进度条，便于视觉化调节。
- **统计与辅助信息**：底部保留刷新统计、状态说明与折叠式帮助文本，风格与上方卡片一致，便于截图/文档引用。

## Initial Setup

1. On first launch, a "Recommended Configuration" card appears at the top. Click **"Apply Recommended Configuration"** to enable throttled refresh (1.5s delay), pause-on-focus-loss, external change monitoring, and console notifications.
2. Enable the main toggle: **"Enable Focus Stutter Optimizer"**.
3. Choose a refresh strategy based on your project size:
   - **Small projects**: Immediate refresh
   - **Medium to large projects**: Throttled refresh (recommended)
   - **Full control needed**: Manual refresh

## Daily Workflow

### Refresh Strategies

#### Immediate Refresh
Unity refreshes assets immediately when regaining focus.

**Best for**:
- Small projects (< 1000 assets)
- When you need instant feedback
- When stutter is not a concern

#### Throttled Refresh (Recommended)
Unity waits a configurable delay (suggested: 1.5–3 seconds) before refreshing after regaining focus.

**Best for**:
- Medium to large projects
- Balancing responsiveness and performance
- Most development scenarios

**Delay Settings**:
- **0.2–1s**: Fast response, suitable for small to medium projects
- **1–3s**: Recommended, balances performance and responsiveness
- **3–10s**: Maximum stutter reduction, suitable for large projects

#### Manual Refresh
Unity does not auto-refresh. Click the **"Refresh Now"** button when needed.

**Best for**:
- When you need complete control
- When refreshing at specific times
- To avoid frequent refreshes disrupting workflow

### Pause on Focus Loss

**Recommended: Enabled**. When Unity loses focus (e.g., switching to a code editor), automatic asset refresh pauses. This prevents unnecessary refreshes while editing code externally.

### Console Status Notifications

When enabled, the plugin outputs detailed refresh information to the Unity Console, including:
- Refresh trigger reason
- Refresh execution time
- Current refresh status

**Use when**: Debugging and monitoring refresh behavior.

### Enter Play Mode Optimization

Optional feature to speed up entering Play Mode.

#### Enable Enter Play Mode Assistant
Main toggle to enable/disable this feature.

#### Apply Preset on Entering Play Mode
When enabled, automatically applies the configured options when entering Play Mode.

#### Preset Options

##### Disable Domain Reload
Skips script recompilation, speeding up startup.

**Benefits**:
- Faster entry into Play Mode
- Reduced wait time

**Notes**:
- Static variables and initialization code do not re-execute
- Some scripts that depend on domain reload may not work correctly
- Recommended for testing phases

##### Disable Scene Reload
Maintains current scene state for faster Play Mode entry.

**Benefits**:
- Scene state remains unchanged
- Faster entry into Play Mode

**Notes**:
- Scene object states may not be initial states
- Some scene logic that requires reset may be affected

#### Auto-Restore Original Settings on Disable
When disabling the assistant, automatically restores Unity's original Enter Play Mode settings.

**Recommended: Enabled** to ensure disabling the feature does not affect project settings.

### Refresh Statistics

Displays detailed information about the most recent asset refresh to help understand refresh performance.

#### Last Refresh Reason
Shows what triggered the most recent refresh (e.g., immediate, throttled, manual, button trigger).

#### Last Duration
Shows the time (in seconds) spent on the most recent asset refresh.

**Performance Reference**:
- **< 0.5s**: Excellent (green)
- **0.5–1.0s**: Good (yellow)
- **> 1.0s**: Needs optimization (red)

#### External Change Warnings
When a large number of external changes are detected, the statistics card provides three levels of warnings: "Low refresh pressure / Approaching threshold / Exceeds threshold, proceed with caution" to help you decide whether to commit or backup first.

#### Time Since Last Refresh
Shows the time in seconds since Unity's last refresh, helping confirm whether it's been a long time or just refreshed.

## Troubleshooting

- **No auto-refresh after code changes**: Check if "Enable Focus Stutter Optimizer" is enabled, if "Manual Refresh" mode is selected, if "Pause on Focus Loss" is enabled, or if throttled refresh delay hasn't elapsed.
- **Refresh still causes stutter**: Increase the throttling delay or switch to manual mode.
- **Enter Play Mode issues**: Try clicking "Restore Original Play Mode Settings" to revert to Unity defaults.
- **Settings not persisting**: Ensure `ProjectSettings/FocusOptimizerSettings.asset` exists and has write permissions.

## Keyboard & UX Tips

- Use the **"Refresh Now"** and **"Resume Auto Refresh"** buttons in the window header for quick access.
- Menu entries under `Tools > Focus Stutter Optimizer` provide access even when the window is closed.
- Check the refresh statistics card regularly to monitor performance.
- Enable console notifications during initial setup to understand refresh behavior.

## Feedback

Please send bug reports or feature requests to `support@fire-tools.example`. Screenshots and reproduction steps are appreciated.

