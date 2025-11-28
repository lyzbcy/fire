# PackageCache 保护修复方案

## 问题分析

在执行"一键转换回 3D 项目"后，Unity 官方 XR 包（在 PackageCache 中）出现编译错误，表明插件可能修改或破坏了 Unity 官方包的内容。

### 问题根源

1. **误修改 PackageCache 中的文件**：
   - `ProcessXrDependentScripts()` 可能处理了 PackageCache 中的脚本
   - `CleanupAsmdefReferences()` 可能修改了 PackageCache 中的 asmdef 文件

2. **直接删除 PackageCache 目录**：
   - `CleanupPackageCache()` 尝试直接删除 PackageCache 中的 XR 包目录
   - 如果 Unity 正在使用这些包，删除可能失败或导致部分删除，破坏包结构

3. **路径检查不完整**：
   - 代码中只检查了特定路径，没有明确排除 PackageCache 和 Packages 目录

## 修复方案

### 1. 明确排除 PackageCache 和 Packages 目录

**修改位置**：`ProcessXrDependentScripts()` 和 `CleanupAsmdefReferences()`

**修复内容**：
- 添加明确的 PackageCache 路径检查
- 添加明确的 Packages 目录检查
- 只处理 Assets 目录下的文件

**代码示例**：
```csharp
// 重要：明确排除 PackageCache 和 Packages 目录中的文件（Unity 官方包）
if (scriptPath.Contains("PackageCache") ||
    scriptPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
{
    continue;
}

// 只处理 Assets 目录下的脚本
if (!scriptPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
{
    continue;
}
```

### 2. 改进 PackageCache 清理逻辑

**修改位置**：`CleanupPackageCache()`

**修复内容**：
- 不再直接删除 PackageCache 中的目录
- 只检查并列出需要清理的包
- 提示用户手动清理（关闭 Unity 后）

**原因**：
- PackageCache 是 Unity 的缓存目录，不应在运行时修改
- 直接删除可能导致包结构不完整
- Unity 在关闭后会自己清理不再需要的包缓存

### 3. 保护 Unity 官方包

**原则**：
- ✅ 只修改用户项目（Assets 目录）中的文件
- ✅ 只修改 manifest.json（包依赖声明）
- ✅ 只修改项目设置（Scripting Define Symbols）
- ❌ 不修改 PackageCache 中的任何文件
- ❌ 不修改 Packages 目录中的任何文件
- ❌ 不删除 PackageCache 中的目录（运行时）

## 修复后的执行流程

### 反向转换（VR → 3D）安全流程

1. **移除 manifest.json 中的 XR 包** ✅
   - 只修改 `Packages/manifest.json`
   - 不触碰 PackageCache

2. **清理 Scripting Define Symbols** ✅
   - 只修改项目设置
   - 不影响包文件

3. **清理 asmdef 文件引用** ✅
   - 只处理 Assets 目录下的 asmdef 文件
   - 明确排除 PackageCache 和 Packages

4. **检查 PackageCache** ✅
   - 只检查，不删除
   - 提示用户手动清理

5. **处理依赖 XR 的脚本** ✅
   - 只处理 Assets 目录下的脚本
   - 明确排除 PackageCache 和 Packages

6. **恢复项目设置** ✅
   - 只修改项目设置资产
   - 不影响包文件

7. **转换场景** ✅
   - 只修改场景文件
   - 不影响包文件

## 核心代码修改

### 1. ProcessXrDependentScripts() - 添加路径保护

```csharp
// 重要：明确排除 PackageCache 和 Packages 目录中的文件（Unity 官方包）
if (scriptPath.Contains("PackageCache") ||
    scriptPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
{
    continue;
}

// 只处理 Assets 目录下的脚本
if (!scriptPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
{
    continue;
}
```

### 2. CleanupAsmdefReferences() - 添加路径保护

```csharp
// 重要：明确排除 PackageCache 和 Packages 目录中的文件（Unity 官方包）
if (asmdefPath.Contains("PackageCache") ||
    asmdefPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
{
    continue;
}

// 只处理 Assets 目录下的 asmdef 文件
if (!asmdefPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
{
    continue;
}
```

### 3. CleanupPackageCache() - 改为只检查不删除

```csharp
// 不再直接删除，只检查并提示
if (xrPackagesFound.Count > 0)
{
    Log("检测到 PackageCache 中有 XR 相关包目录");
    Log("这些是 Unity 官方包，不应在运行时删除");
    Log("转换完成后，请关闭 Unity 编辑器，然后手动删除...");
}
```

## 如何确保不再修改 Unity 官方包

### 保护机制

1. **路径检查**：
   - 所有文件操作前都检查路径
   - 明确排除 `PackageCache` 和 `Packages/` 开头的路径
   - 只处理 `Assets/` 开头的路径

2. **AssetDatabase 过滤**：
   - 使用 `AssetDatabase.GUIDToAssetPath()` 获取路径
   - 在处理前进行路径验证

3. **不直接操作 PackageCache**：
   - 不再尝试删除 PackageCache 中的目录
   - 只检查并提示用户手动清理

### 验证清单

转换后检查：
- ✅ manifest.json 中无 XR 包（正确）
- ✅ Assets 目录下的脚本已添加条件编译（正确）
- ✅ Assets 目录下的 asmdef 已清理（正确）
- ✅ PackageCache 中的文件未被修改（正确）
- ✅ Packages 目录中的文件未被修改（正确）

## 用户操作指南

如果转换后仍有编译错误：

1. **关闭 Unity 编辑器**（完全退出）

2. **手动清理 PackageCache**：
   - 打开项目根目录
   - 进入 `Library/PackageCache` 目录
   - 删除所有以 `com.unity.xr.` 开头的目录，例如：
     - `com.unity.xr.management@4.4.0`
     - `com.unity.xr.core-utils@2.3.0`
     - `com.unity.xr.interaction.toolkit@*`
     - 等等

3. **重新打开 Unity 编辑器**：
   - Unity 会自动检测并清理不再需要的包缓存
   - 项目应该能正常编译

## 最终保证

修复后的转换流程确保：

1. ✅ **不修改 Unity 官方包**
   - PackageCache 中的文件完全不受影响
   - Packages 目录中的文件完全不受影响

2. ✅ **只修改用户项目**
   - 只处理 Assets 目录下的文件
   - 只修改 manifest.json 和项目设置

3. ✅ **安全清理**
   - 不直接删除 PackageCache
   - 提示用户手动清理（关闭 Unity 后）

4. ✅ **避免编译错误**
   - 不会破坏包结构
   - 不会导致类型找不到的错误

