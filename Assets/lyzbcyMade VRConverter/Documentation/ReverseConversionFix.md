# VR 项目转换为 3D 项目 - 脚本依赖修复方案

## 问题描述

在将 VR 项目转换为 3D 项目后，项目中依赖 XR 包的脚本文件（如 `VRHandTrackingSync.cs`）会导致编译错误，因为 XR 包已被移除。

## 修复思路

### 1. 自动检测依赖 XR 的脚本
- 扫描项目中所有脚本文件
- 检测是否包含 XR 相关命名空间引用
- 检测是否使用了 XR 相关类型

### 2. 添加条件编译指令
- 为依赖 XR 的脚本自动添加条件编译指令
- 使用 `#if UNITY_XR_INTERACTION_TOOLKIT || UNITY_XR_MANAGEMENT` 包裹脚本内容
- 保留文件头注释，确保代码结构完整

### 3. 创建备份
- 在修改脚本前自动创建 `.vrbackup` 备份文件
- 用户可以随时恢复原始脚本

## 修复后的脚本示例

### 修复前（会导致编译错误）：
```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRHandTrackingSync : MonoBehaviour
{
    private XRRayInteractor rayInteractor;
    // ...
}
```

### 修复后（自动添加条件编译）：
```csharp
#if UNITY_XR_INTERACTION_TOOLKIT || UNITY_XR_MANAGEMENT
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRHandTrackingSync : MonoBehaviour
{
    private XRRayInteractor rayInteractor;
    // ...
}
#endif // UNITY_XR_INTERACTION_TOOLKIT || UNITY_XR_MANAGEMENT
```

## 执行流程

1. **移除 XR 包** - 从 manifest.json 中移除 XR 相关依赖
2. **清理生成的资源** - 删除 `VRConverterGenerated` 目录
3. **清理场景组件** - 移除场景中的 XR 组件实例
4. **处理脚本文件** - 为依赖 XR 的脚本添加条件编译指令 ⭐
5. **恢复项目设置** - 禁用 XR 项目设置
6. **转换场景** - 移除 XR Origin，恢复 Main Camera

## 注意事项

- 脚本文件会被自动修改，但会创建 `.vrbackup` 备份
- 如果脚本已有条件编译指令，会被跳过
- 工具自己的脚本和 Editor 脚本不会被处理
- 转换后，脚本在无 XR 包时不会编译，避免错误

## 恢复原始脚本

如果需要恢复原始脚本，可以：
1. 删除修改后的脚本文件
2. 将 `.vrbackup` 文件重命名回原文件名
3. 或者在 Unity 中重新导入脚本

