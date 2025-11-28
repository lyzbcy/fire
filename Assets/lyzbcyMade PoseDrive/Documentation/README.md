# PoseDrive 插件

PoseDrive（姿控驱动）是一个基于 Unity Barracuda 的 AI 体感输入插件，利用摄像头实时识别人类姿态，并把手势/动作映射为虚拟按键，可直接驱动现有的移动、摄像头与交互逻辑，实现无需手柄与鼠标键盘的操作体验。

## 新增功能

- 🎮 **一键激活控制**：通过按键（默认 V）快速开启/关闭姿态控制
- 📹 **头部姿态控制镜头**：通过头部旋转控制摄像机视角，支持死区和连续旋转
- 🚶 **身体姿态控制移动**：通过身体前倾/后仰控制角色前进/后退

## 功能亮点

- 🔍 **MoveNet 姿态检测**：实时返回 33 个关键点（含置信度与平滑处理）。
- 🧠 **动作分类器**：支持用户自训练的 `action_classifier.onnx`，识别点头、摇头、挥手、抓取等动作。
- 🎮 **虚拟输入系统**：统一封装键盘、鼠标与手柄虚拟按键，支持运行时映射与模拟按压。
- 🪟 **编辑器工具**：内置控制面板、映射编辑器与“快速配置向导”，可预览摄像头画面、骨架、当前手势并一键搭建运行时。
- 🧩 **示例场景**：提供第三人称与第一人称两个可运行 Demo，展示移动、视角控制、跳跃与交互。
- 📦 **完整文档**：含安装、配置、常见问题与自定义模型流程。

## 目录结构

```
PoseDrive/
 ├─ Runtime/ 核心运行时代码与模型占位
 ├─ Editor/  控制面板与映射编辑器
 ├─ Samples~/ThirdPersonDemo/  第三人称示例
 ├─ Samples~/FirstPersonDemo/  第一人称示例
 └─ Documentation/  文档
```

## 环境要求

- Unity 2021 LTS 及以上
- Barracuda 3.0+
- 支持 UWP/Windows/macOS 的摄像头（USB/内置）

## 安装步骤

1. 将 `PoseDrive` 文件夹复制到 Unity 项目 `Assets/` 目录下。
2. 打开 Package Manager，确保安装 **Barracuda**。
3. 将真实的 `movenet.onnx` 与 `action_classifier.onnx` 模型文件替换 `Runtime/Models` 中的占位文件。
4. 在菜单 `Tools/PoseDrive/配置向导` 中一键创建运行时并挂载模型/映射，或打开示例场景进入 Play Mode 验证摄像头与骨架识别。
5. 添加 `PoseDriveInput` 和 `PoseDriveController` 组件到场景中，配置按键和参数后即可使用。

## 兼容性

| 平台 | 状态 |
| ---- | ---- |
| Windows | ✅ 已测试 |
| macOS | ✅ 需授权摄像头 |
| Linux | ⚠️ 理论支持，需 Barracuda 与摄像头驱动 |

## License

此插件随项目分发，可在团队内自由使用与修改。请在分发时附带本说明文件。

