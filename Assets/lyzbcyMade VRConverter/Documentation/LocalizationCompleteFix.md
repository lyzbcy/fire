# 本地化完整修复说明

## 修复内容

本次修复解决了两个主要的本地化问题：

### 1. 菜单项本地化

**问题**：菜单项不会随语言切换而变化，始终显示硬编码的中文路径。

**解决方案**：
由于 Unity 的 `MenuItem` 属性不支持动态字符串（编译时限制），我们使用了多个 `MenuItem`，每个语言一个：

```csharp
// 中文菜单
[MenuItem("工具/VR转换助手/主界面", false, 1)]
public static void OpenWindowChinese()
{
    Localization.SetLanguage(Localization.Language.Chinese);
    OpenWindow();
}

// 英文菜单
[MenuItem("Tools/VR Converter/Main Window", false, 1)]
public static void OpenWindowEnglish()
{
    Localization.SetLanguage(Localization.Language.English);
    OpenWindow();
}

// 日文菜单
[MenuItem("ツール/VR変換アシスタント/メインウィンドウ", false, 1)]
public static void OpenWindowJapanese()
{
    Localization.SetLanguage(Localization.Language.Japanese);
    OpenWindow();
}
```

**菜单路径**：
- 中文：`工具/VR转换助手/主界面`
- 英文：`Tools/VR Converter/Main Window`
- 日文：`ツール/VR変換アシスタント/メインウィンドウ`

**注意**：Unity 的菜单系统不支持运行时本地化，所以使用多个 MenuItem。用户点击对应语言的菜单项时，会自动设置语言并打开窗口。

### 2. ProjectHealthCheck 界面本地化

**问题**：项目体检界面中有大量硬编码的中文文本，不会随语言切换而变化。

**修复内容**：
- 修复了 `DrawCompatibilityInsights()` 方法中所有硬编码文本
- 修复了所有 `DiagnosticRow` 中的硬编码文本
- 修复了所有日志消息中的硬编码文本

**修复的文本**：
- "Unity 版本" → `Localization.Get("Compatibility.UnityVersion")`
- "渲染管线" → `Localization.Get("Compatibility.RenderPipeline")`
- "已检测到" → `Localization.Get("Compatibility.Detected")`
- "尚未安装" → `Localization.Get("Compatibility.NotInstalled")`
- "已就绪" → `Localization.Get("Compatibility.Ready")`
- "未检测到" → `Localization.Get("Compatibility.NotDetected")`
- "已导入" → `Localization.Get("Compatibility.Imported")`
- "未导入" → `Localization.Get("Compatibility.NotImported")`
- "已配置" → `Localization.Get("Compatibility.Configured")`
- "未配置" → `Localization.Get("Compatibility.NotConfigured")`
- "已安装" → `Localization.Get("Compatibility.Installed")`
- "未安装" → `Localization.Get("Compatibility.NotInstalled")`

**修复的日志消息**：
- 所有 `Log()` 调用中的硬编码中文文本都已替换为 `Localization.Get()` 调用
- 添加了相应的本地化键（中文、英文、日文）

## 新增的本地化键

### 菜单相关
- `Menu.Tools` - "工具" / "Tools" / "ツール"
- `Menu.VRConverter` - "VR转换助手" / "VR Converter" / "VR変換アシスタント"
- `Menu.MainWindow` - "主界面" / "Main Window" / "メインウィンドウ"

### 错误消息
- `Error.GitAssistantNotDetected` - "未检测到版本控制助手。"
- `Error.GitAssistantNotInstalled` - "未安装版本控制助手。"

### 日志消息
- `Log.XRPackagesToRemove` - "检测到 {0} 个 XR 相关包需要移除"
- `Log.XRPackagesRemoved` - "已移除 XR 包依赖，Unity 正在重新解析包..."
- `Log.ScriptingDefineSymbolRemoved` - "已从 {0} 的 Scripting Define Symbols 中移除: {1}"
- `Log.ErrorCleaningScriptingDefineSymbols` - "清理 Scripting Define Symbols 时出错: {0}"
- `Log.ErrorCleaningAsmdef` - "清理 asmdef 文件时出错: {0}"
- `Log.XRGeneralSettingsNotFound` - "未找到 XR General Settings 资产，跳过恢复"
- `Log.CannotGetXRSettings` - "无法获取 XR 设置，跳过恢复"
- `Log.CleaningGeneratedVrAssets` - "正在清理生成的 VR 资源目录: {0}"
- `Log.ErrorCleaningGeneratedVrAssets` - "清理生成的 VR 资源时出错: {0}"
- `Log.ErrorProcessingXrDependentScripts` - "处理依赖 XR 的脚本时出错: {0}"
- `Log.PackageCacheNotExists` - "PackageCache 目录不存在，跳过检查"
- `Log.PackageCacheXrPackagesFound` - "检测到 PackageCache 中有 {0} 个 XR 相关包目录"
- `Log.PackageCacheOfficialPackages` - "这些是 Unity 官方包，不应在运行时删除"
- `Log.PackageCacheManualCleanupRequired` - "转换完成后，请关闭 Unity 编辑器，然后手动删除以下目录："
- `Log.PackageCachePackagePath` - "  - Library/PackageCache/{0}"
- `Log.PackageCacheAfterCleanup` - "删除后重新打开 Unity，Unity 会自动清理不再需要的包缓存"
- `Log.NoXrPackagesInCache` - "PackageCache 中未找到 XR 相关包"
- `Log.ErrorCheckingPackageCache` - "检查 PackageCache 时出错: {0}"

## 修复的文件

1. **`VRProjectConverterWindow.cs`**
   - 修复了菜单项本地化（使用多个 MenuItem）
   - 修复了项目体检界面的所有硬编码文本
   - 修复了所有日志消息中的硬编码文本

2. **`Localization.cs`**
   - 添加了所有缺失的本地化键（中文、英文、日文）
   - 确保所有语言文件都包含完整的翻译

## 使用方法

### 菜单项使用
用户可以通过以下方式打开窗口：
- **中文**：点击菜单 `工具 → VR转换助手 → 主界面`
- **英文**：点击菜单 `Tools → VR Converter → Main Window`
- **日文**：点击菜单 `ツール → VR変換アシスタント → メインウィンドウ`

点击对应语言的菜单项后，会自动设置语言并打开窗口。

### 语言切换
在窗口内切换语言后：
- 窗口标题会立即更新
- 所有 UI 文本会立即更新
- 项目体检界面的所有文本会立即更新
- 日志消息会使用当前语言

## 测试建议

1. **菜单项测试**
   - 点击中文菜单，确认窗口打开且语言为中文
   - 点击英文菜单，确认窗口打开且语言为英文
   - 点击日文菜单，确认窗口打开且语言为日文

2. **项目体检界面测试**
   - 切换到中文，检查所有文本显示为中文
   - 切换到英文，检查所有文本显示为英文
   - 切换到日文，检查所有文本显示为日文

3. **日志消息测试**
   - 执行转换操作，检查日志消息是否使用当前语言
   - 切换语言后，检查新的日志消息是否使用新语言

## 注意事项

1. **菜单项限制**：Unity 的菜单系统不支持运行时本地化，所以使用多个 MenuItem。这是 Unity 的限制，不是插件的限制。

2. **语言持久化**：语言设置会保存在 `PlayerPrefs` 中，下次打开窗口时会自动恢复。

3. **缺失翻译**：如果某个键没有当前语言的翻译，会回退到中文，如果中文也没有，则返回键名。

## 总结

✅ **菜单项本地化**：通过多个 MenuItem 实现（Unity 限制）
✅ **项目体检界面本地化**：所有文本都使用 Localization.Get()
✅ **日志消息本地化**：所有日志消息都使用 Localization.Get()
✅ **完整的本地化键**：所有语言（中文、英文、日文）都包含完整的翻译

所有 UI 文案现在都会根据语言设置动态切换，不再有硬编码的中文文本。

