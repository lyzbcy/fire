# 技术文档：Focus Optimizer (焦点卡顿优化助手)

## 1. 项目概览
**Focus Optimizer** 是一款 Unity 编辑器性能优化插件，旨在解决 Unity 在失去/重新获取焦点时因自动刷新资源（Auto Refresh）导致的卡顿问题。通过接管 `AssetDatabase` 的刷新行为，结合系统级的文件监控，它实现了智能的资源刷新策略，显著提升了开发时的流畅度。

## 2. 技术栈 (Technology Stack)

### 核心技术
*   **开发语言**: C# (Unity Editor)
*   **运行环境**: Unity Editor (Editor Scripting)
*   **API 依赖**:
    *   `UnityEditor.AssetDatabase`: 控制资源数据库的刷新与锁定。
    *   `UnityEditor.EditorApplication`: 监听编辑器的生命周期与 Update 帧。
    *   `System.IO.FileSystemWatcher`: 独立于 Unity 的文件系统监控。

### 关键系统 API
*   **`InternalEditorUtility.isApplicationActive`**: 用于在 `EditorApplication.update` 中精确检测 Unity 窗口的焦点获取与丢失状态。
*   **`FileSystemWatcher`**: 核心组件之一。当 Unity 失去焦点并暂停自动刷新时，该组件接管对 `Assets`、`Packages` 和 `ProjectSettings` 目录的监控，记录外部文件变动。

## 3. 架构与实现细节

本项目采用**拦截-监控-决策**的架构模式。

### 3.1 焦点状态管理 (`FocusOptimizerController.cs`)
这是插件的中枢控制器，负责协调各个子系统。
*   **生命周期挂钩**: 使用 `[InitializeOnLoad]` 确保编辑器启动时立即生效。
*   **焦点事件处理**:
    *   **失去焦点 (Lost Focus)**: 调用 `AssetDatabase.DisallowAutoRefresh()` 彻底暂停 Unity 的自动刷新机制，防止无意义的后台 CPU 占用和卡顿。同时启动 `FocusChangeTracker`。
    *   **获取焦点 (Gain Focus)**: 获取后台期间积累的文件变动数量，根据用户配置的策略（立即刷新、限流刷新、手动刷新）决定下一步操作。

### 3.2 独立文件监控体系 (`FocusChangeTracker.cs`)
为了在 Unity 暂停刷新期间（`kAutoRefresh` 被禁用）仍然掌握项目变动，插件实现了一套独立的文件监控层。
*   **多目录监听**: 并行监控关键目录（Assets, Packages 等），使用 `NotifyFilters` 过滤文件大小、写入时间等变动。
*   **线程安全与缓冲区**: 由于 `FileSystemWatcher` 回调运行在系统线程池，代码使用了 `lock` 机制和缓冲区策略（`ChangedPaths` HashSet）来安全地记录变动路径，并去重。
*   **性能保护**: 设置了最大记录上限（如 5000 条），防止在发生大规模文件操作（如 Git 切换分支）时内存溢出。

### 3.3 增量导入与限流 (`FocusBackgroundImporter.cs`)
*   **限流刷新 (Throttling)**: 不仅仅是简单的延时调用，而是结合了时间戳判断，避免在用户高频切换窗口时触发多次刷新。
*   **实验性后台导入**: 尝试在编辑器空闲时段，分批次（`MaxImportsPerCycle`）主动调用 `AssetDatabase.ImportAsset`，以平摊主线程的卡顿峰值。

### 3.4 播放模式优化 (`EnterPlayModeOptionHelper.cs`)
*   **Preset 注入**: 利用 `EditorSettings.enterPlayModeOptions` API，动态注入“禁用 Domain Reload”或“禁用 Scene Reload”的配置，加速进入 Play Mode 的过程，并在必要时自动还原用户原有设置。

## 4. 关键技术特性

### 智能决策系统
插件不仅仅是简单的开关，它包含了一套决策逻辑：
*   **大规模变动检测**: 如果后台监控到的文件变动超过阈值（如 Git Pull 导致上千个文件变化），插件会智能判断“这可能是一次且昂贵的刷新”，从而自动保持暂停状态并弹窗询问用户，避免意外卡死。

### 安全性设计
*   **资源锁管理**: 使用 `AssetDatabase.DisallowAutoRefresh()` 和 `AllowAutoRefresh()` 配对调用，并维护内部状态标志位 (`_autoRefreshSuspended`)，防止因异常退出导致编辑器永久处于不刷新状态。
*   **异常隔离**: 文件监控和后台导入均包裹在 `try-catch`块中，确保辅助功能的报错不会中断正常的编辑器工作流。

## 5. 性能指标
*   **监控开销**: `FileSystemWatcher` 运行在独立线程，对主线程几乎无影响。
*   **Update 开销**: 极低。每帧仅是一个布尔值检查 (`isApplicationActive`)，耗时操作均被分摊或按需触发。
