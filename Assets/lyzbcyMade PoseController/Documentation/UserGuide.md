# PoseController 使用指南

## 1. 快速上手

1. **导入插件**：将 `PoseController` 文件夹复制到 `Assets/`。
2. **替换模型**：把真实的 `movenet.onnx` 与 `action_classifier.onnx` 拷贝至 `Runtime/Models/`。
3. **运行快速配置向导**：菜单 `Tools/PoseController/Setup Wizard`，按提示勾选模型、映射并点击 “创建/更新 PoseController System”，自动创建 WebCam + PoseDetector + ActionClassifier + PoseInputMapper + PoseControllerManager。
4. **或手动添加**：
   - 拖入示例场景（`Samples~/ThirdPersonDemo/ThirdPersonDemo.unity` 或 `FirstPersonDemo/FirstPersonDemo.unity`），或
   - 在任意场景里放置 `PoseControllerSampleBootstrap` 以自动创建运行时对象。
5. **运行**：连接摄像头，进入 Play Mode 即可看到骨架与动作识别效果。

## 2. 主要组件

| 组件 | 作用 | 如何快速添加 |
| ---- | ---- | ---- |
| `WebcamProvider` | 初始化摄像头、提供纹理帧 | 向导自动添加 |
| `PoseDetector` | 运行 MoveNet，生成 `PoseData` | 向导自动添加并可绑定模型 |
| `ActionClassifier` | 接收特征向量，输出手势标签 | 向导自动添加 |
| `PoseControllerManager` | 特征提取、动作事件、移动/视角计算 | 向导自动添加 |
| `PoseInputMapper` | 将手势映射至虚拟按键 | 向导可绑定 ScriptableObject |

所有公共 API 均带有 XML 注释，可在 IDE 中查看。

## 3. 手势 → 按键映射

1. 打开菜单 `Tools/PoseController/Action Mapping`。
2. 选择或创建 `ActionMappingAsset`。
3. 为每一个手势配置 `VirtualKey`（键盘、鼠标或手柄按钮）。
4. 将资产拖拽至场景中的 `PoseInputMapper` 组件。
5. Play Mode 期间可通过 `PoseControllerWindow` 查看实时映射状态。

## 4. 添加/删除手势

1. 在 `ActionClassifier` 的训练数据中加入新的标签，并重新导出 `action_classifier.onnx`。
2. 在 `PoseControllerManager.ExtractFeatureVector` 中新增或调整特征。
3. 在 `PoseInputMapper` 的映射资产中新增对应的手势名称。
4. （可选）在 `PoseControllerManager.DetectSimpleGestures` 中添加阈值驱动的传统手势检测逻辑。

## 5. 导入新模型

1. 使用 TensorFlow/TF Lite、PyTorch 等工具导出 ONNX。
2. 确保输入维度与 `PoseControllerManager` 生成的特征长度一致。
3. 替换 `Runtime/Models` 目录中的同名文件，或在 Inspector 中直接引用新的 TextAsset / NNModel。
4. 运行场景检查控制台调试输出，确认模型成功加载。

## 6. 常见问题

- **Q: 没有摄像头时是否会报错？**  
  A: 不会，`WebcamProvider` 会安静失败并在控制台提示，可改用录制视频或虚拟摄像头。

- **Q: 延迟较高怎么办？**  
  A: 降低输入分辨率（`PoseDetector` 的 `_inputWidth/_inputHeight`）、关闭调试日志，并在 Barracuda 中使用 Burst/BLAS 后端。

- **Q: 如何在编译后的游戏中切换手柄按键？**  
  A: 运行时可以调用 `PoseInputMapper.SetRuntimeBindings` 重新注入手势映射，或在启动时加载不同的 `ActionMappingAsset`。

## 7. FAQ

| 问题 | 建议 |
| ---- | ---- |
| 摄像头画面倒置 | 通过 `WebCamTexture.videoRotationAngle` 校正，或在 `PoseDetector` 中添加翻转 |
| 骨架抖动 | 调整 `_smoothing`，默认 0.6，越大越稳定 |
| 自定义控制逻辑 | 订阅 `PoseControllerManager.OnGestureTriggered`，或读取 `MoveVector`/`ViewDelta` 直接驱动角色 |

祝开发顺利，享受体感控制带来的新玩法！

