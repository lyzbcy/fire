# 本地化修复说明

## 修复内容

本次修复解决了两个与语言本地化相关的问题：

### 1. 项目体检界面本地化

**问题**：项目体检界面的所有文本都是硬编码的中文字符串，不会随语言切换而变化。

**修复**：
- 修复了 `VRProjectConverterWindow.cs` 中 `DrawCompatibilityInsights()` 方法的所有硬编码文本
- 修复了 `GetProjectDiagnostics()` 方法中的硬编码文本
- 修复了 `GetVersionStatusText()` 方法中的硬编码文本
- 修复了诊断报告按钮和对话框的硬编码文本
- 修复了 `DiagnosticReporter.cs` 中所有硬编码文本
- 修复了 `UnityVersionChecker.cs` 中的硬编码文本
- 在 `Localization.cs` 中添加了所有缺失的本地化文本（中文、英文、日文）

**涉及的本地化键**：
- `Compatibility.*` - 项目体检相关文本
- `Diagnostic.Report.*` - 诊断报告相关文本
- `Diagnostic.Suggestion.*` - 诊断建议相关文本
- `VersionCheck.*` - 版本检查相关文本
- `Button.*` - 按钮文本
- `Dialog.*` - 对话框文本

### 2. 菜单项本地化说明

**问题**：菜单项不会随语言切换而变化。

**说明**：
Unity 的 `MenuItem` 属性不支持动态字符串，这是 Unity 的编译时限制。菜单路径必须在编译时确定，无法在运行时根据语言设置动态更改。

**当前实现**：
- 菜单路径使用中文：`Tools/VR Converter/一键转换当前项目为VR...`
- 窗口标题使用本地化：`Localization.Get("Window.Title")`
- 所有 UI 文本都使用本地化系统

**其他插件的做法**：
查看其他 `lyzbcyMade` 插件（如 PoseController、FocusOptimizer），它们也都使用中文菜单路径，这是 Unity 菜单系统的限制。

**建议**：
如果需要菜单项也支持多语言，可以考虑：
1. 使用静态方法在初始化时注册菜单（复杂，需要维护多个菜单项）
2. 保持菜单路径为中文（当前做法，与其他插件一致）
3. 确保窗口标题和所有 UI 文本都使用本地化（已实现）

## 修复的文件

1. `Assets/lyzbcyMade VRConverter/Editor/VRProjectConverterWindow.cs`
   - 修复了项目体检界面的所有硬编码文本
   - 修复了窗口标题的本地化

2. `Assets/lyzbcyMade VRConverter/Editor/DiagnosticReporter.cs`
   - 修复了诊断报告生成器的所有硬编码文本

3. `Assets/lyzbcyMade VRConverter/Editor/UnityVersionChecker.cs`
   - 修复了版本检查器的所有硬编码文本

4. `Assets/lyzbcyMade VRConverter/Editor/Localization.cs`
   - 添加了所有缺失的本地化文本（中文、英文、日文）

## 测试建议

1. 切换语言为中文，检查项目体检界面是否显示中文
2. 切换语言为英文，检查项目体检界面是否显示英文
3. 切换语言为日文，检查项目体检界面是否显示日文
4. 生成诊断报告，检查报告内容是否使用当前语言
5. 检查窗口标题是否随语言切换而变化

## 注意事项

- Unity 的菜单系统不支持运行时本地化，这是 Unity 的限制
- 菜单路径保持为中文，与其他 `lyzbcyMade` 插件一致
- 所有 UI 文本和窗口标题都已实现本地化

