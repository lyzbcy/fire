# Fire Git Assistant

## 功能概览

- 在 `Tools/Fire/Git Assistant` 中打开图形界面。
- 左侧树状图显示 `git status --porcelain` 的改动，自动按目录分组。
- 顶部展示当前分支、远端信息与最近日志。
- 提供以下操作按钮：
  - **Refresh**：刷新状态、分支与日志。
  - **Stage All**：执行 `git add -A`。
  - **Commit**：输入提交信息并提交。
  - **Push**：推送到远端（支持指定远端与分支）。
  - **Pull**：拉取远端以保持同步。
- 底部日志窗口以 `git log --graph --oneline` 呈现记录，方便快速回顾。

## 安装与打包

1. 将 `Packages/com.fire.gitassistant` 整体保留在项目中，即可通过 UPM (Package Manager) 引用。
2. 若需在 Unity Asset Store/Package Manager 中售卖，可选择：
   - **UPM Git Tag**：将该文件夹推到独立仓库并创建 release。
   - **unitypackage**：在 Unity 中选中 `Packages/com.fire.gitassistant` 文件夹，使用 `Assets > Export Package...` 导出。

## 使用建议

- 确保系统已安装 `git` 并可在命令行中调用。
- Unity 编辑器需要对项目根目录拥有读写权限。
- 如果项目使用多仓库或子模块，可在设置中调整工作目录（窗口右上角齿轮）。

## 自定义扩展

- 所有 UI 逻辑位于 `Editor/GitAssistantWindow.cs`。
- Git 命令封装见 `Editor/GitProcessUtility.cs`，可根据需要拓展（如 `git tag`、`git stash`）。
- 树视图实现位于 `Editor/GitTreeView.cs`，可扩展右键菜单、单独暂存等功能。

## 支持

如需商业授权支持，请联系 `support@fire-tools.example`。

