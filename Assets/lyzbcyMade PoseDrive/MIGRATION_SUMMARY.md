# PoseDrive Barracuda/Sentis 兼容层迁移总结

## ✅ 已完成的修改

### 1. 新增文件

**`Assets/lyzbcyMade PoseDrive/Runtime/Utils/ModelCompat.cs`**
- 创建了统一的兼容层，支持 Barracuda 和 Sentis
- 优先使用 Barracuda（如果已安装），否则使用 Sentis
- 提供统一的 API：`LoadModel()`, `CreateWorker()`, `CreateTensorFromTexture()`, `CreateTensorFromFloats()`

### 2. 修改的文件

**`Assets/lyzbcyMade PoseDrive/Runtime/Core/PoseDetector.cs`**
- 移除了直接引用 `Unity.Sentis`
- 使用 `ModelCompat` 兼容层进行模型加载和推理
- 添加了条件编译支持（`#if UNITY_BARRACUDA` / `#if UNITY_SENTIS`）
- 保持了原有的 API 接口（`CurrentPose`, `IsPoseValid`）

**`Assets/lyzbcyMade PoseDrive/Runtime/Core/ActionClassifier.cs`**
- 移除了直接引用 `Unity.Sentis`
- 使用 `ModelCompat` 兼容层进行模型加载和推理
- 添加了条件编译支持
- 保持了原有的事件和属性接口

## 🔧 技术细节

### 兼容层设计

1. **类型映射**：
   - `TensorFloat` → Barracuda 的 `Tensor` 或 Sentis 的 `TensorFloat`
   - `ModelAsset` → Barracuda 的 `NNModel` 或 Sentis 的 `ModelAsset`
   - `Model` → 两个框架的 `Model` 类型
   - `IWorker` → 两个框架的 `IWorker` 接口

2. **API 统一**：
   - `ModelCompat.LoadModel(UnityEngine.Object)` - 从 ModelAsset/NNModel 加载
   - `ModelCompat.LoadModel(byte[])` - 从 ONNX bytes 加载
   - `ModelCompat.CreateWorker(Model)` - 创建 Worker（自动选择后端）
   - `ModelCompat.CreateTensorFromTexture()` - 从 Texture2D 创建 Tensor
   - `ModelCompat.CreateTensorFromFloats()` - 从 float[] 创建 Tensor

3. **条件编译策略**：
   - 如果同时安装了 Barracuda 和 Sentis，优先使用 Barracuda
   - 如果只安装了其中一个，使用已安装的
   - 如果都没安装，提供占位类型（编译通过但运行时无效）

## 📋 编译要求

### 当前项目状态
- ✅ `UNITY_BARRACUDA` 宏已在 ProjectSettings 中定义
- ✅ `com.unity.barracuda` 包已在 `Packages/manifest.json` 中

### 如果需要使用 Sentis
1. 在 Package Manager 中安装 `com.unity.sentis`
2. 在 Project Settings → Player → Scripting Define Symbols 中添加 `UNITY_SENTIS`
3. 注意：如果同时有 Barracuda 和 Sentis，兼容层会优先使用 Barracuda

## 🧪 测试步骤

### 1. 编译验证
- [x] 代码已通过编译检查（无 lint 错误）
- [ ] 在 Unity Editor 中打开项目，确认无编译错误

### 2. 运行时验证
1. 在场景中创建 GameObject，添加 `PoseDetector` 组件
2. 在 Inspector 中：
   - 如果使用 Barracuda：将 `NNModel` 资源拖到 `_modelAsset` 字段
   - 或使用 `_onnxModelFallback` 字段拖入 ONNX TextAsset
3. 进入 Play 模式，检查：
   - 控制台无错误日志
   - `PoseDetector.CurrentPose` 有数据更新
   - `PoseDetector.IsPoseValid` 返回 true（当检测到人体时）

### 3. ActionClassifier 验证
1. 添加 `ActionClassifier` 组件
2. 配置模型（同上）
3. 调用 `Evaluate(float[] featureVector)` 方法
4. 检查 `OnActionRecognized` 事件是否触发

## 🔄 回滚说明

如果需要回滚到之前的 Sentis-only 版本：

1. **Git 回滚**（推荐）：
   ```bash
   git checkout <previous-commit-hash> -- Assets/lyzbcyMade\ PoseDrive/Runtime/Core/PoseDetector.cs
   git checkout <previous-commit-hash> -- Assets/lyzbcyMade\ PoseDrive/Runtime/Core/ActionClassifier.cs
   git rm Assets/lyzbcyMade\ PoseDrive/Runtime/Utils/ModelCompat.cs
   ```

2. **手动回滚**：
   - 删除 `ModelCompat.cs`
   - 恢复 `PoseDetector.cs` 和 `ActionClassifier.cs` 到使用 `Unity.Sentis` 的版本
   - 确保项目中安装了 `com.unity.sentis` 包

## ⚠️ 已知限制

1. **API 差异处理**：
   - Barracuda 的 `Tensor.channels` vs Sentis 的 `Tensor.shape[3]` - 已在兼容层中处理
   - Barracuda 的 `WorkerFactory.Type` vs Sentis 的 `BackendType` - 已在兼容层中统一

2. **模型格式**：
   - Barracuda 使用 `NNModel`（编译后的格式）
   - Sentis 使用 `ModelAsset`（ONNX 导入后的格式）
   - 两者都支持直接加载 ONNX bytes

3. **性能差异**：
   - Barracuda 和 Sentis 的性能特性可能不同
   - 建议在实际项目中测试并选择更适合的框架

## 📝 后续建议

1. **统一模型格式**：考虑统一使用 ONNX TextAsset，避免 ModelAsset/NNModel 的差异
2. **性能测试**：在实际硬件上对比 Barracuda 和 Sentis 的性能
3. **文档更新**：更新用户文档，说明如何选择和使用不同的推理引擎

---

**修改日期**: 2024-12-19  
**修改人**: Cursor AI Assistant  
**版本**: 1.0.0


