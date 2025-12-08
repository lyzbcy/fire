# Version Control Assistant / 版本控制助手

**Version Control Assistant** is a visual Git client integrated into the Unity Editor, providing status tracking, committing, and pushing functionality via an intuitive GUI.

> **Note**: This plugin is designed to streamline your version control workflow directly within Unity.

---

## English Documentation

### Feature Highlights

- **Visual Interface**: Open via `Tools/Version Control Assistant`.
- **Status Tracking**: Left tree view mirrors `git status --porcelain` output, auto-grouped by folder.
- **Hero Dashboard**: Top cards display current branch, remote information, and quick links to documentation.
- **Action Toolbar**: Frequently used commands (Refresh, Stage All, Commit, Push, Pull) are centralized with clear icon-based buttons.
- **Smart Feedback**:
  - The **Status Card** summarizes pending changes and alerts on Git errors.
  - The **Log Card** lists recent commits with author tags and supports pulling specific revisions.
- **Localization**: UI supports Chinese, English, and Japanese, switchable in settings.

### Interface Highlights (New)

- **Hero Card**: Displays current branch, remote, and unpushed count with a gradient background for critical info at a glance.
- **Card-Style Toolbar**: Buttons for Stage/Commit/Push/Pull use a modern card design with icons, clearly indicating enabled/disabled states.
- **Unified Visuals**: The change tree, recent commits list, and bottom log share a consistent card-based design with optimized scrollbars and empty states.
- **Centralized Settings**: Language switching, preferences, and notifications are grouped in the top-right corner.

### Installation & Distribution

1. **Unity Package Manager**: Keep the `Packages/com.fire.gitassistant` folder in your project to use via UPM.
2. **Exporting**:
   - **UPM Git Tag**: Push the folder to a dedicated repo and use git tags.
   - **.unitypackage**: Select `Packages/com.fire.gitassistant` in Unity and use `Assets > Export Package...`.

### Usage Tips

- Ensure `git` is installed and in your system PATH.
- Unity Editor must have read/write access to the project root and `.git` folder.
- Configure default remote/branch in **Project Settings > Version Control Assistant** or via the toolbar.
- Click a commit in the "Recent Commits" panel to pull that specific revision.

### Customization

- **UI Logic**: `Editor/VersionControlAssistantWindow.cs`
- **Git Commands**: `Editor/GitProcessUtility.cs` (extendable)
- **Tree View**: `Editor/GitTreeView.cs`

### Support

For support or business inquiries, contact `support@fire-tools.example`.

---

## 中文文档 (Chinese Documentation)

### 功能概览

- 在 `Tools/Version Control Assistant` 中打开图形界面。
- 左侧树状图显示 `git status --porcelain` 的改动，自动按目录分组。
- 顶部展示当前分支、远端信息与最近日志。
- 提供以下操作按钮：
  - **Refresh**：刷新状态、分支与日志。
  - **Stage All**：执行 `git add -A`。
  - **Commit**：输入提交信息并提交。
  - **Push**：推送到远端（支持指定远端与分支）。
  - **Pull**：打开历史版本窗口，从远端日志中挑选目标版本后再执行拉取与同步。
- 顶部视觉卡片展示当前分支与远端，并可一键打开[完整使用教程](https://lyzbcy.github.io/posts/Unity%E6%8F%92%E4%BB%B6-Git%E5%8A%A9%E6%89%8B%E5%BC%80%E5%8F%91%E6%8A%A5%E5%91%8A/)。
- 右侧“最近提交”卡片会根据窗口高度自适应显示，当提交较多时自动出现滚动条，保证能够查看全部历史记录。
- 底部日志窗口以 `git log --graph --oneline` 呈现记录，方便快速回顾。

### 界面升级要点

- 顶部状态带以渐变卡片同时展示当前分支、远端、未推送数量与提示链接，更符合“助手”身份。
- 中部操作区采用卡片+图标按钮的组合，Stage/Commit/Push/Pull 等按钮在视觉上与文档说明一致，便于团队培训。
- 变更树、最近提交、日志区域统一加上浅色背景与滚动条提示，保持与其他插件一致的高级视觉语言。
- 所有提示、帮助链接与多语言切换入口集中在窗口右上角，操作路径更清晰。

### 安装与打包

1. 将 `Packages/com.fire.gitassistant` 整体保留在项目中，即可通过 UPM (Package Manager) 引用。
2. 若需在 Unity Asset Store/Package Manager 中售卖，可选择：
   - **UPM Git Tag**：将该文件夹推到独立仓库并创建 release。
   - **unitypackage**：在 Unity 中选中 `Packages/com.fire.gitassistant` 文件夹，使用 `Assets > Export Package...` 导出。

### 使用建议

- 确保系统已安装 `git` 并可在命令行中调用。
- Unity 编辑器需要对项目根目录拥有读写权限。
- 窗口右上角的齿轮按钮可快速切换语言，并打开项目设置自定义默认远端与分支。

### 自定义扩展

- 所有 UI 逻辑位于 `Editor/VersionControlAssistantWindow.cs`。
- Git 命令封装见 `Editor/GitProcessUtility.cs`，可根据需要拓展（如 `git tag`、`git stash`）。
- 树视图实现位于 `Editor/GitTreeView.cs`，可扩展右键菜单、单独暂存等功能。

### 支持

如需商业授权支持，请联系 `support@fire-tools.example`。
