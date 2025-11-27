# 焦点卡顿优化助手

## 功能概览

- 在 `Tools/焦点卡顿优化助手/主窗口` 中打开图形界面。
- 提供三种刷新策略：立即刷新、限流刷新、手动刷新，根据项目需求灵活选择。
- 失焦时自动暂停资源刷新，避免在外部编辑器中修改代码时触发不必要的刷新。
- 可选 Enter Play Mode 优化，加快进入播放模式的速度。
- 刷新耗时统计和状态监控，帮助团队排查慢编译/慢导入问题。
- 安全模式：默认不修改项目原有设置，所有选项均可独立开关。

## 安装与打包

1. 将 `Assets/lyzbcyMade FocusOptimizer` 整体保留在项目中，即可通过本地包引用。
2. 若需在 Unity Asset Store/Package Manager 中售卖，可选择：
   - **UPM Git Tag**：将该文件夹推到独立仓库并创建 release。
   - **unitypackage**：在 Unity 中选中该文件夹，使用 `Assets > Export Package...` 导出。

## 使用建议

- 小型项目（< 1000 个资源）：推荐使用立即刷新或限流刷新（0.5-1 秒延迟）。
- 中大型项目（> 1000 个资源）：推荐使用限流刷新（1-3 秒延迟）或手动刷新。
- Enter Play Mode 优化适合在开发测试阶段使用，正式发布前建议关闭。
- 首次使用建议点击"一键应用推荐配置"，获得平衡的体验。

## 自定义扩展

- 所有 UI 逻辑位于 `Editor/FocusOptimizerWindow.cs`。
- 刷新控制逻辑位于 `Editor/FocusChangeTracker.cs`，可根据需要扩展。
- 设置管理位于 `Editor/FocusOptimizerSettings.cs`，可添加新的配置选项。

## 支持

如需商业授权支持，请联系 `support@fire-tools.example`。

