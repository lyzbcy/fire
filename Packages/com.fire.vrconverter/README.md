# Fire VR 项目转换工具使用说明

本文档介绍如何在 Unity 中使用 `Fire.VRConverter.Editor.VRProjectConverterWindow`，以便快速将现有项目配置为基础 VR 环境。

## 前置条件
- Unity 2021.3 LTS 或更高版本，且包含 Unity XR 相关模块。
- 已安装 Fire VR Converter（位于 `Packages/com.fire.vrconverter`）。
- 具备项目写入权限（需要修改 `Packages/manifest.json` 以及在 `Assets/FireVRGenerated` 目录下生成资源）。

## 打开工具窗口
1. 启动 Unity 并打开目标项目。
2. 等待脚本编译完成后，点击菜单 `Tools > Fire VR > 一键转换当前项目为VR...`。
3. 打开的窗口包含三个主要操作：
   - `一键执行所有步骤（推荐）`
   - `第1步：只检查并添加 XR 依赖包`
   - `第2步：配置 XR 设置 + 创建/更新场景 VR Rig`

## 推荐流程：一键执行所有步骤
1. 在窗口中点击 **一键执行所有步骤（推荐）**。
2. 工具首先检查 `Packages/manifest.json`，确保以下依赖存在：
   - `com.unity.xr.management`
   - `com.unity.xr.openxr`
   - `com.unity.xr.interaction.toolkit`
3. 如有缺失或版本无效的依赖，工具会调用 Unity Package Manager 安装/更新最新可用版本，并在日志中输出结果。安装完成后 Unity 会自动重新导入并编译，请耐心等待。
4. 编译完成后（`EditorApplication.isCompiling == false`），工具会继续：
   - 为 Standalone / Android Build Target 自动创建并注册 `XRGeneralSettings`、`XRManagerSettings`。
   - 通过 XR Plug-in Management API 启用 OpenXR Loader。
   - 在当前场景中尝试创建或更新 XR Interaction Toolkit 的 `XR Origin (Action Based)` 结构；若 XR Interaction Toolkit 尚不可用，则回退到一个基础 VR Rig。

## 手动步骤

### Step 1：确保 XR 依赖（独立执行）
适用于刚导入工具、尚未添加 XR 包的项目。
1. 点击 **第1步：只检查并添加 XR 依赖包**。
2. 工具会移除 `manifest.json` 中过去遗留的 `"latest"` 占位符，并使用 Package Manager 安装缺失的包。
3. 查看日志确认安装成功与否；如日志提示需要等待编译，请稍后再进行 Step 2。

### Step 2：配置 XR 设置 + 场景转换
在 XR 包已导入并完成编译后执行。
1. 确认 Unity 状态栏未显示 *Compiling Scripts*。
2. 点击 **第2步：配置 XR 设置 + 创建/更新场景 VR Rig**。
3. 结果包括：
   - `Assets/FireVRGenerated/XR/XRGeneralSettings.asset`（随包附带的 per-build target 资产）。
   - `EditorBuildSettings` 中注册对应的 XR General Settings。
   - Standalone/Android 的 `XR Manager Settings` 并启用 OpenXR Loader。
   - 当前场景生成的 XR Origin，含 `Camera Offset`、`Main Camera`、左右手控制器、Tracked Pose Driver、Action Based Controller、XR Ray Interactor、Line Renderer、XR Interaction Manager、Input Action Manager。
   - 如 XR Interaction Toolkit 类型不可用，则创建基础 `VRRig`（包含主摄像机和左右手空节点）。
   - 自动禁用场景中原有的 `Main Camera`（如果存在且可识别）。

## 常见问题
- **日志提示“未检测到 XR Management 程序集”**  
  说明 XR 包仍在导入或版本不符。等待编译完成后重新点击 Step 2。
- **控制器没有输入行为**  
  工具只创建 Action Based Controller 组件，但不会自动加载默认 Input Action 资产。请在项目中导入 `XRI Default Input Actions` 并绑定到 `Input Action Manager`。
- **自定义场景已有 XR 结构**  
  工具会检测已有的 `XR Origin` 或 `VRRig`，并尽量复用、补齐缺失节点。执行前建议备份场景。

## 日志查看
窗口底部的“执行日志”区域记录所有操作及提示，可用于排查问题。日志内容不会持久化，若需要保留请手动复制。

## 后续建议
- 在 `Project Settings > XR Plug-in Management` 中确认 OpenXR 启用情况，并根据目标平台启用对应 Feature Groups（如 Oculus Quest Support）。
- 根据需求补充 Action Maps、手势交互、触觉反馈等系统。工具只生成基础可运行的 XR Rig。
- 若项目使用自定义 CI/CD，请确保在命令行模式下也调用本工具或复用其生成的资产。

