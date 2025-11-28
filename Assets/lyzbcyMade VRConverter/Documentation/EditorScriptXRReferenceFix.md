# Editor 脚本 XR 引用修复方案

## 问题分析

在执行"一键转换回 3D 项目"后，插件的 Editor 脚本（DiagnosticReporter.cs）出现编译错误，因为直接引用了 XR 相关的命名空间和类型，而这些包已被移除。

### 错误示例

```
error CS0234: The type or namespace name 'Management' does not exist in the namespace 'UnityEditor.XR'
error CS0234: The type or namespace name 'CoreUtils' does not exist in the namespace 'Unity.XR'
```

### 问题根源

1. **直接类型引用**：`DiagnosticReporter.cs` 中直接使用了：
   - `UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget`
   - `Unity.XR.CoreUtils.XROrigin`

2. **缺少条件编译保护**：没有使用条件编译指令或运行时类型检查

3. **编译时依赖**：直接的类型引用导致编译时依赖 XR 包

## 修复方案

### 1. 使用条件编译指令

**修复位置**：`DiagnosticReporter.cs`

**修复方法**：
- 使用 `#if UNITY_XR_MANAGEMENT` 包裹 XR Management 相关代码
- 使用 `#if UNITY_XR_INTERACTION_TOOLKIT || UNITY_XR_CORE_UTILS` 包裹 XR Core Utils 相关代码
- 在 `#else` 分支返回默认值（false）

### 2. 使用运行时类型检查

**修复方法**：
- 使用 `Type.GetType()` 动态查找类型
- 使用反射调用方法和属性
- 如果类型不存在，返回 false

### 3. 修复后的代码结构

```csharp
private static bool IsXrManagementEnabled()
{
#if UNITY_XR_MANAGEMENT
    try
    {
        // 使用 Type.GetType 动态查找类型
        var perBuildTargetType = Type.GetType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget, Unity.XR.Management.Editor");
        if (perBuildTargetType == null)
        {
            return false;
        }
        
        // 使用反射调用方法
        var method = perBuildTargetType.GetMethod("XRGeneralSettingsForBuildTarget", ...);
        // ...
    }
    catch
    {
        return false;
    }
#else
    return false;
#endif
}
```

## 修复内容

### 修复的方法

1. **IsXrManagementEnabled()**
   - 添加 `#if UNITY_XR_MANAGEMENT` 条件编译
   - 使用 `Type.GetType()` 和反射替代直接类型引用

2. **IsOpenXrLoaderEnabled()**
   - 添加 `#if UNITY_XR_MANAGEMENT` 条件编译
   - 使用反射访问 `activeLoaders` 属性

3. **HasXrOriginInScene()**
   - 添加 `#if UNITY_XR_INTERACTION_TOOLKIT || UNITY_XR_CORE_UTILS` 条件编译
   - 使用 `Type.GetType()` 动态查找 XROrigin 类型
   - 支持多个可能的命名空间

## 保护机制

### 1. 条件编译保护

- `#if UNITY_XR_MANAGEMENT`：保护 XR Management 相关代码
- `#if UNITY_XR_INTERACTION_TOOLKIT || UNITY_XR_CORE_UTILS`：保护 XR Core Utils 相关代码
- `#else`：在 XR 包不存在时返回安全默认值

### 2. 运行时类型检查

- 使用 `Type.GetType()` 动态查找类型
- 检查类型是否存在后再使用
- 使用 try-catch 捕获所有异常

### 3. 双重保护

- 条件编译：避免编译时错误
- 运行时检查：确保运行时安全

## VRProjectConverterWindow.cs 的保护

`VRProjectConverterWindow.cs` 已经使用了安全的模式：

1. **使用字符串常量**：
   ```csharp
   private const string XrOriginTypeName = "Unity.XR.CoreUtils.XROrigin";
   ```

2. **使用 FindType() 方法**：
   ```csharp
   var xrOriginType = FindType(XrOriginTypeName);
   if (xrOriginType == null)
   {
       return false; // 安全处理
   }
   ```

3. **运行时类型检查**：
   - 所有 XR 相关操作都先检查类型是否存在
   - 如果类型不存在，安全返回或跳过操作

## 最终保证

修复后的 Editor 脚本确保：

1. ✅ **编译时安全**
   - 使用条件编译指令避免编译错误
   - 在 XR 包不存在时不会编译 XR 相关代码

2. ✅ **运行时安全**
   - 使用运行时类型检查
   - 如果类型不存在，返回安全默认值

3. ✅ **向后兼容**
   - 在 XR 包存在时正常工作
   - 在 XR 包不存在时也能正常编译和运行

4. ✅ **无残留引用**
   - 所有直接的类型引用都已移除
   - 所有 XR API 调用都通过反射或条件编译保护

## 验证清单

修复后检查：
- ✅ DiagnosticReporter.cs 可以正常编译（无 XR 包时）
- ✅ VRProjectConverterWindow.cs 可以正常编译（无 XR 包时）
- ✅ 所有 Editor 脚本都没有直接使用 XR 类型
- ✅ 所有 XR API 调用都有条件编译或运行时检查保护

