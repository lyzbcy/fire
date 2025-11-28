# PoseDrive 插件升级方案

## 📋 概述

本文档描述了 PoseDrive（原 PoseController）插件的升级方案，包括新的命名、文件结构、API 设计和编辑器工具改进。

---

## 🎯 新命名方案

### 插件名称
- **旧名称**: PoseController（动捕控制器）
- **新名称**: PoseDrive（姿控驱动）

### 核心类命名

| 旧类名 | 新类名 | 说明 |
|--------|--------|------|
| `PoseControllerManager` | `PoseDriveManager` | 主管理器（保留，向后兼容） |
| - | `PoseDriveInput` | 输入控制器（新增） |
| - | `PoseDriveController` | 驱动控制器（新增） |
| `PoseDetector` | `PoseDetector` | 姿态检测器（保留） |
| `ActionClassifier` | `ActionClassifier` | 动作分类器（保留） |
| `PoseInputMapper` | `PoseInputMapper` | 输入映射器（保留） |

### 命名空间
- **旧**: `PoseController.*`
- **新**: `PoseDrive.*`

---

## 📁 文件结构建议

```
PoseDrive/
├── Runtime/
│   ├── Core/
│   │   ├── PoseDriveManager.cs          # 主管理器（兼容旧 API）
│   │   ├── PoseDriveInput.cs            # 输入控制器（新增）
│   │   ├── PoseDriveController.cs       # 驱动控制器（新增）
│   │   ├── PoseDetector.cs              # 姿态检测器
│   │   ├── ActionClassifier.cs          # 动作分类器
│   │   ├── PoseData.cs                  # 姿态数据
│   │   └── ActionEvent.cs               # 动作事件
│   ├── Input/
│   │   ├── PoseInputMapper.cs           # 输入映射器
│   │   ├── ActionMappingAsset.cs        # 映射资产
│   │   └── VirtualKey.cs                # 虚拟按键
│   └── Utils/
│       ├── MathUtils.cs                 # 数学工具
│       └── WebcamProvider.cs            # 摄像头提供者
├── Editor/
│   ├── PoseDriveMacroManager.cs         # 宏管理器（新增）
│   ├── PoseDriveSetupWizard.cs          # 配置向导
│   ├── PoseDriveWindow.cs               # 主面板
│   ├── PoseDriveLocalization.cs         # 本地化
│   ├── PoseDriveLogger.cs               # 日志系统
│   └── EditorStyles.cs                  # 编辑器样式
└── Documentation/
    ├── README.md                        # 主文档
    ├── UPGRADE_PLAN.md                  # 升级方案（本文档）
    └── UserGuide.md                     # 用户指南
```

---

## 🔧 关键类 API 设计

### 1. PoseDriveManager（主管理器）

**职责**: 特征提取、动作分类、输入映射协调

```csharp
namespace PoseDrive.Runtime.Core
{
    public class PoseDriveManager : MonoBehaviour
    {
        // 现有 API（保持兼容）
        public Vector2 ViewDelta { get; }
        public Vector2 MoveVector { get; }
        public event Action<string> OnGestureTriggered;
        
        // 新增：与 PoseDriveController 集成
        public PoseDriveInput InputController { get; }
        public PoseDriveController DriveController { get; }
    }
}
```

### 2. PoseDriveInput（输入控制器）

**职责**: 处理全局启用开关

```csharp
namespace PoseDrive.Runtime.Core
{
    public class PoseDriveInput : MonoBehaviour
    {
        public enum ActivationMode { Hold, Toggle }
        
        [SerializeField] private KeyCode _activationKey = KeyCode.V;
        [SerializeField] private ActivationMode _activationMode = ActivationMode.Toggle;
        
        public bool PoseDriveActive { get; }
        public event Action<bool> OnPoseDriveActiveChanged;
        
        public void SetPoseDriveActive(bool active);
        public KeyCode GetActivationKey();
        public void SetActivationKey(KeyCode key);
    }
}
```

### 3. PoseDriveController（驱动控制器）

**职责**: 头部姿态控制镜头、身体姿态控制移动

```csharp
namespace PoseDrive.Runtime.Core
{
    [RequireComponent(typeof(PoseDriveInput))]
    public class PoseDriveController : MonoBehaviour
    {
        [Header("镜头旋转设置")]
        [SerializeField] private float _deadZoneAngle = 5f;
        [SerializeField] private float _maxPoseAngle = 60f;
        [SerializeField] private float _minRotateSpeed = 30f;
        [SerializeField] private float _maxRotateSpeed = 180f;
        [SerializeField] private AnimationCurve _rotationCurve;
        
        [Header("移动控制设置")]
        [SerializeField] private float _moveDeadZone = 2f;
        [SerializeField] private float _maxLeanAngle = 30f;
        [SerializeField] private float _maxMoveSpeed = 5f;
        
        // 输出
        public Vector2 CurrentRotationVelocity { get; }
        public float CurrentMoveSpeed { get; }
        public Vector3 GetMoveVector();
    }
}
```

---

## 🎨 编辑器 Setup Wizard 重写建议

### 当前问题
1. 宏定义检测不完善
2. 错误提示不够友好
3. 缺少自动化配置

### 改进方案

#### 1. 自动宏管理
- ✅ 已实现：`PoseDriveMacroManager` 自动检测并添加 `UNITY_BARRACUDA` 宏
- 在配置向导中自动调用，无需手动操作

#### 2. 智能错误处理
```csharp
// 改进前：直接报错
if (field == null) {
    LogError("无法访问字段");
}

// 改进后：智能诊断
if (field == null) {
#if UNITY_BARRACUDA
    // 宏已定义但字段不存在 -> 重新编译提示
#else
    // 宏未定义 -> 自动添加宏提示
#endif
}
```

#### 3. 分步骤引导
- **步骤 1**: 检查依赖（自动检测并修复）
- **步骤 2**: 选择模型（智能拉取，友好提示）
- **步骤 3**: 输入映射（可视化编辑）
- **步骤 4**: 创建运行时（一键创建所有组件）
- **步骤 5**: 快速入口（场景、工具链接）

#### 4. 用户友好提示
- ✅ 使用 `EditorUtility.DisplayDialog` 替代控制台错误
- ✅ 提供解决方案按钮（如"自动添加宏"）
- ✅ 详细日志记录到文件

---

## 🚀 使用流程

### 新用户快速开始

1. **安装依赖**
   - 打开配置向导（Tools/PoseDrive/配置向导）
   - 步骤 1 自动检测并安装 Barracuda
   - 自动添加 `UNITY_BARRACUDA` 宏

2. **配置模型**
   - 步骤 2 选择模型
   - 点击"从 PoseDetector 拉取"自动获取

3. **创建运行时**
   - 步骤 4 点击"创建 / 更新 PoseDrive System"
   - 自动创建所有必要组件

4. **使用新功能**
   - 添加 `PoseDriveInput` 组件（处理按键）
   - 添加 `PoseDriveController` 组件（控制镜头和移动）
   - 按 V 键激活/停用

### 现有用户迁移

1. **自动重命名**
   - 所有类名和命名空间已自动更新
   - 场景引用会自动更新（Unity 处理）

2. **新功能集成**
   - 在现有场景中添加 `PoseDriveInput` 和 `PoseDriveController`
   - 配置参数后即可使用

---

## 🔍 技术细节

### 宏管理机制

```csharp
// 自动检测流程
1. 检查 manifest.json 中是否有 com.unity.barracuda
2. 检查 PackageManager API
3. 检查运行时类型是否存在
4. 如果包存在但宏不存在 -> 自动添加
5. 如果包不存在但宏存在 -> 警告（可选移除）
```

### 反射优化

```csharp
// 改进的反射逻辑
1. 先尝试 SerializedProperty（最快）
2. 如果失败，使用反射（兼容条件编译）
3. 如果都失败，检查宏定义状态
4. 根据宏状态给出不同提示
```

---

## 📝 待办事项

- [x] 自动宏管理
- [x] 优化编辑器反射逻辑
- [x] 生成升级方案文档
- [ ] 更新用户指南
- [ ] 添加示例场景
- [ ] 性能优化

---

## 📞 支持

如有问题，请查看：
- 日志文件：`项目根目录/Logs/PoseDrive/`
- 用户指南：`Documentation/UserGuide.md`
- 配置向导：`Tools/PoseDrive/配置向导`

---

**版本**: 2.0.0  
**更新日期**: 2024-12-28

