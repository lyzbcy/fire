# Version Control Assistant

## Feature Highlights

- Open the UI via `Tools/Version Control Assistant`.
- Left tree view mirrors `git status --porcelain` output and auto-groups by folder.
- Hero cards display current branch, remote information, and quick links to the online guide.
- Frequently used commands (Refresh, Stage All, Commit, Push, Pull) are centralized in the top toolbar.
- The status card summarizes pending changes and surfaces any Git errors.
- The log card lists recent commits with author tags and supports pulling a specific revision.
- All UI strings are localized (Chinese, English, Japanese) and switchable from the settings menu.

## 界面亮点（新版）

- 顶部英雄卡片以渐变背景突出当前分支、远端、未推送数量，并附带帮助链接，首次打开即可掌握关键信息。
- 工具条中的 Stage/Commit/Push/Pull 采用图标+文字的卡片式按钮，状态禁用/启用一目了然。
- 左侧变更树、右侧最近提交与底部日志在视觉上保持统一的卡片语言，滚动条与空状态也经过重新设计。
- 语言切换、偏好设置、通知入口集中在窗口右上角，与团队文档中描述的交互保持一致。

## Installation & Distribution

1. Keep the entire `Packages/com.fire.gitassistant` folder inside your Unity project to consume it via UPM.
2. To distribute externally:
   - **UPM Git Tag**: push this folder to a dedicated repository and create tagged releases.
   - **unitypackage**: in Unity select `Packages/com.fire.gitassistant` and export via `Assets > Export Package...`.

## Usage Tips

- Ensure `git` is installed and available in the system PATH.
- The Unity editor must have read/write permissions to the project root and `.git` directory.
- Configure default remote/branch in `Project Settings > Version Control Assistant` or directly in the window toolbar.
- Click any entry in the “Recent commits” panel to pull that specific revision (requires remote/branch fields to be filled).

## Customization Hints

- Window/UI logic lives in `Editor/VersionControlAssistantWindow.cs`.
- Git command wrappers are inside `Editor/GitProcessUtility.cs`; extend this class to add new operations.
- The change tree is implemented by `Editor/GitTreeView.cs`; extend it for per-file staging, context menus, etc.

## Support

For commercial licensing or dedicated support, contact `support@fire-tools.example`.

