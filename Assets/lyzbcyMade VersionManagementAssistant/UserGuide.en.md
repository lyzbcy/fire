# Version Control Assistant – User Guide

## Opening the Tool
1. Start Unity and open your project.
2. Navigate to `Tools > Version Control Assistant`.
3. The window can be docked like any other Unity editor panel.

## 界面速览

- **英雄信息卡**：窗口顶部的渐变卡片同时展示当前分支、远端、未推送数量与最近一次拉取时间，并提供帮助链接，方便团队成员同步上下文。
- **操作工具条**：Refresh/Stage All/Commit/Push/Pull 统一采用图标+文字的卡片式按钮，并根据远端/分支可用性自动启用或禁用。
- **三栏布局**：左侧为目录分组的改动树，中间/右侧分别为提交表单与最近提交列表，底部是 `git log --graph --oneline` 日志区，全部使用一致的卡片化视觉。
- **偏好与通知入口**：右上角的齿轮按钮用于打开设置及切换语言，旁边的消息图标会罗列 Git 执行提示。

## Initial Setup
1. Confirm that `git` is installed on the operating system and accessible from the command line.
2. In the toolbar, enter your default remote (e.g., `origin`) and branch (e.g., `main`).
3. Optional: open `Project Settings > Version Control Assistant` via the gear icon to store defaults and choose the UI language.

## Daily Workflow

### Inspect Changes
- The left tree view lists modified files grouped by folder.
- Use the search box to filter entries by file name.
- The “Changes” card displays counts for each change type (Added, Modified, Deleted, etc.).

### Stage and Commit
1. Click **Stage All** if you want to stage everything (`git add -A`).
2. Enter a commit message in the “Commit” card; the commit button enables once there is staged content.
3. Press **Commit** to run `git commit -m "<message>"`. Notifications appear in the status card if Git reports errors.

### Push to Remote
1. Make sure the remote and branch fields are filled, or enable “Use custom remote URL” to provide an explicit URL.
2. Hit **Push** to run `git push <remote> <branch>`.

### Pull Specific Revisions
1. Click **Pull** in the toolbar to open the history picker, or click any entry in the “Recent commits” log.
2. Confirm the dialog to fetch the remote branch and run `git reset --hard <hash>`.
3. The window refreshes automatically after the pull completes.

## Troubleshooting
- **No Git output / commands fail**: ensure Git is installed and that the Unity process has permission to run it.
- **Buttons disabled**: remote or branch fields may be empty; fill them to enable Pull/Push.
- **Localization placeholders**: reopen the window after switching languages to force a refresh.

## Keyboard & UX Tips
- Use the refresh toolbar button (or shortcut `Ctrl/Cmd + R` if assigned) to re-fetch status without re-opening the window.
- Hover over toolbar icons to see tooltips; the three-dot button lists detected remotes.
- Notifications appear in the lower-right corner of the window; click elsewhere to dismiss them.

## Feedback
Please send bug reports or feature requests to `support@fire-tools.example`. Screenshots and reproduction steps are appreciated.

