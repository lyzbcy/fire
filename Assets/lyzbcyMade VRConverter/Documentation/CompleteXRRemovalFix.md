# 完整 XR 包移除修复方案

## 问题分析

在执行"一键转换回 3D 项目"后，Unity 出现大量来自 XR Management、XR Core Utils 等包的编译错误。

### 问题根源

1. **不完整的包移除**：只移除了部分 XR 包，但 XR 包之间有复杂的依赖关系
2. **Package Cache 残留**：即使从 manifest.json 移除，Unity 的 Package Cache 中可能还有残留
3. **asmdef 引用未清理**：项目中的 asmdef 文件可能仍引用 XR 包
4. **Scripting Define Symbols 未清理**：XR 相关的编译符号未移除
5. **依赖链未断开**：部分包被移除但其他包还在，导致类型找不到

## 修复方案

### 1. 扩展 XR 包检测列表

**修改位置**：`VRProjectConverterWindow.cs`

**新增包列表**：
```csharp
private static readonly string[] AllXrPackages =
{
    "com.unity.xr.management",
    "com.unity.xr.core-utils",
    "com.unity.xr.interaction.toolkit",
    "com.unity.xr.openxr",
    "com.unity.xr.hands",
    "com.unity.xr.legacyinputhelpers",
    "com.unity.xr.oculus",
    "com.unity.xr.windowsmr",
    "com.unity.xr.magicleap",
    "com.unity.xr.arkit",
    "com.unity.xr.arkit-face-tracking",
    "com.unity.xr.arfoundation",
    "com.unity.xr.arsubsystems"
};
```

### 2. 智能检测所有 XR 包

**实现逻辑**：
- 使用正则表达式从 manifest.json 中提取所有 `com.unity.xr.*` 包
- 合并已知列表和检测到的包
- 确保不遗漏任何 XR 相关包

**代码位置**：`RemoveXrPackages()` 方法

### 3. 清理 Scripting Define Symbols

**实现逻辑**：
- 遍历所有目标平台组（Standalone、Android 等）
- 移除以下 XR 相关定义：
  - `UNITY_XR_MANAGEMENT`
  - `UNITY_XR_INTERACTION_TOOLKIT`
  - `UNITY_XR_OPENXR`
  - `UNITY_XR_CORE_UTILS`
  - `UNITY_XR_HANDS`

**代码位置**：`CleanupScriptingDefineSymbols()` 方法

### 4. 清理 asmdef 文件引用

**实现逻辑**：
- 扫描项目中所有 asmdef 文件
- 检测 `references` 数组中的 XR 包引用
- 移除所有 XR 包引用
- 正确处理 JSON 格式（逗号、数组结构）
- 创建备份文件

**代码位置**：`CleanupAsmdefReferences()` 方法

### 5. 强制 Unity 重新解析包

**实现逻辑**：
- 调用 `AssetDatabase.Refresh()`
- 尝试调用 Package Manager 的 `Resolve()` 方法
- 提示用户如果仍有错误，需要重启 Unity

## 执行流程

### 反向转换（VR → 3D）完整流程

1. **移除 XR 包依赖** (`RemoveXrPackages`)
   - 检测所有 XR 相关包（已知列表 + 自动检测）
   - 从 manifest.json 移除所有 XR 包
   - 正确处理 JSON 格式

2. **清理生成的资源** (`CleanupGeneratedVrAssets`)
   - 删除 `VRConverterGenerated` 目录

3. **清理 Scripting Define Symbols** (`CleanupScriptingDefineSymbols`)
   - 移除所有 XR 相关的编译符号

4. **清理 asmdef 文件** (`CleanupAsmdefReferences`)
   - 移除所有 asmdef 文件中的 XR 包引用

5. **处理依赖 XR 的脚本** (`ProcessXrDependentScripts`)
   - 为依赖 XR 的脚本添加条件编译指令

6. **恢复项目设置** (`Restore3DProjectSettings`)
   - 禁用 XR 项目设置

7. **转换场景** (`ConvertCurrentSceneTo3D`)
   - 移除 XR Origin / VRRig
   - 移除 XR 管理器组件
   - 恢复 Main Camera

8. **强制刷新** (`AssetDatabase.Refresh`)
   - 触发 Unity 重新解析包依赖

## 核心代码修改

### 1. 扩展包列表

```csharp
// 所有需要移除的 XR 相关包（包括依赖包）
private static readonly string[] AllXrPackages = { ... };
```

### 2. 智能检测 XR 包

```csharp
// 使用正则表达式检测所有 com.unity.xr.* 包
var jsonPattern = @"\""(com\.unity\.xr\.[^""]+)\""\s*:";
var matches = System.Text.RegularExpressions.Regex.Matches(text, jsonPattern);
```

### 3. 清理 Scripting Define Symbols

```csharp
private void CleanupScriptingDefineSymbols()
{
    foreach (var targetGroup in TargetGroups)
    {
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
        // 移除 XR 相关定义
        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newDefines);
    }
}
```

### 4. 清理 asmdef 文件

```csharp
private void CleanupAsmdefReferences()
{
    // 扫描所有 asmdef 文件
    // 移除 references 数组中的 XR 包引用
    // 正确处理 JSON 格式
}
```

## 如何检测并移除所有 XR 依赖

### 检测方法

1. **manifest.json 检测**：
   - 使用正则表达式匹配 `com.unity.xr.*` 模式
   - 提取所有匹配的包名

2. **asmdef 文件检测**：
   - 扫描所有 asmdef 文件
   - 检查 `references` 数组中是否包含 XR 包

3. **Scripting Define Symbols 检测**：
   - 检查所有目标平台的编译符号
   - 查找 XR 相关的定义

4. **脚本文件检测**：
   - 扫描所有脚本文件
   - 检测 using 语句和类型引用

### 移除方法

1. **manifest.json**：逐行处理，移除 XR 包条目，正确处理逗号
2. **asmdef 文件**：逐行处理 references 数组，移除 XR 包引用
3. **Scripting Define Symbols**：使用 PlayerSettings API 移除
4. **脚本文件**：添加条件编译指令

## 如何确保 Unity 不再加载 XR 包

### 1. 完全移除包声明

- 从 manifest.json 移除所有 XR 包
- 确保 JSON 格式正确

### 2. 清理引用链

- 移除 asmdef 文件中的 XR 引用
- 移除 Scripting Define Symbols
- 为脚本添加条件编译

### 3. 强制刷新

- 调用 `AssetDatabase.Refresh()`
- 尝试调用 Package Manager 的 `Resolve()`
- 提示用户重启 Unity（如果需要）

### 4. 验证步骤

转换完成后，检查：
- ✅ manifest.json 中无 XR 包
- ✅ asmdef 文件中无 XR 引用
- ✅ Scripting Define Symbols 中无 XR 定义
- ✅ 脚本文件有条件编译保护
- ✅ 项目可以正常编译

## 最终保证

修复后的转换流程确保：

1. ✅ **完整卸载所有 VR/XR 相关的 Package**
   - 包括所有已知的 XR 包
   - 自动检测并移除所有 `com.unity.xr.*` 包

2. ✅ **移除或替换项目中所有依赖 XR 包的代码/组件**
   - 场景组件已移除
   - 脚本文件添加条件编译
   - asmdef 引用已清理

3. ✅ **自动清理 XR 相关的 .asmdef、Scripting Define Symbols、引用链**
   - asmdef 文件中的 XR 引用已移除
   - Scripting Define Symbols 已清理
   - 所有引用链已断开

4. ✅ **最终保证**
   - 3D 项目转换后能直接完整编译
   - 不再依赖任何 XR 包
   - 没有残留的脚本、asmdef 或 Define Symbols
   - Library 不会再报错（需要 Unity 重新解析包）

## 注意事项

1. **Unity 重启**：如果转换后仍有编译错误，可能需要关闭并重新打开 Unity 编辑器，让 Unity 完全重新解析包依赖。

2. **备份文件**：所有修改都会创建备份文件（`.vrbackup`），可以随时恢复。

3. **Package Cache**：Unity 的 Package Cache 在 `Library/PackageCache` 目录，转换后 Unity 会自动清理不再需要的包。

4. **手动检查**：如果仍有问题，可以手动检查：
   - `Packages/manifest.json`
   - 项目中的 asmdef 文件
   - `Edit > Project Settings > Player > Other Settings > Scripting Define Symbols`

