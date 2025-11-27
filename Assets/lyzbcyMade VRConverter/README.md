# 一键 VR 项目转换工具

## 功能概览

- 在 `Tools/VR Converter/一键转换当前项目为VR...` 中打开图形界面。
- 一键自动安装 XR 依赖包（XR Management、OpenXR、XR Interaction Toolkit、Input System）。
- 自动配置 XR 设置（XRGeneralSettings、XRManagerSettings、OpenXR Loader）。
- 智能创建 VR Rig（XR Origin with Action-Based Controllers，或基础 VR Rig）。
- 自动绑定输入动作（XRI Default Input Actions）。
- 支持 Standalone 和 Android 平台配置。
- 提供分步骤执行模式，便于精细控制。

## 安装与打包

1. 将 `Assets/lyzbcyMade VRConverter` 整体保留在项目中，即可通过本地包引用。
2. 若需在 Unity Asset Store/Package Manager 中售卖，可选择：
   - **UPM Git Tag**：将该文件夹推到独立仓库并创建 release。
   - **unitypackage**：在 Unity 中选中该文件夹，使用 `Assets > Export Package...` 导出。

## 使用建议

- 确保 Unity 2021.3 LTS 或更高版本（已安装 Unity XR 模块）。
- 首次使用建议点击"一键执行所有步骤（推荐）"，自动完成所有配置。
- 转换前建议备份场景，工具会修改当前场景并创建 VR Rig。
- 转换后可在 `Project Settings > XR Plug-in Management` 中确认 OpenXR 已启用。
- 根据目标平台（如 Oculus Quest）启用对应的 Feature Groups。

## 自定义扩展

- 所有 UI 逻辑位于 `Editor/VRProjectConverterWindow.cs`。
- XR 配置逻辑位于 `Editor/` 目录中的各个工具类，可根据需要扩展。
- 支持添加自定义 XR Provider、自定义 VR Rig 配置或额外平台设置。

## 支持

如需商业授权支持，请联系 `support@fire-tools.example`。
