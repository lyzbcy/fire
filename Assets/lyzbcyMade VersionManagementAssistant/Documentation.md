# Version Control Assistant - User Manual

This document provides detailed usage instructions for the **Version Control Assistant**.

---

## English User Guide

### Opening the Tool
1. Start Unity and open your project.
2. Navigate to `Tools > Version Control Assistant`.
3. The window can be docked like any other Unity editor panel.

### Interface at a Glance
- **Hero Info Card**: Top gradient card displaying current branch, remote, unpushed count, and last pull time, with help links for team context.
- **Action Toolbar**: Refresh/Stage All/Commit/Push/Pull use unified icon+text card buttons, automatically enabled/disabled based on remote/branch availability.
- **Three-Column Layout**: Left change tree (grouped by folder), middle/right commit form and recent commits, bottom `git log` area—all using a consistent card visual style.
- **Preferences & Notifications**: Top-right gear icon for settings and language switching; notification icon for Git status messages.

### Initial Setup
1. Confirm that `git` is installed on the operating system and accessible from the command line.
2. In the toolbar, enter your default remote (e.g., `origin`) and branch (e.g., `main`).
3. Optional: open `Project Settings > Version Control Assistant` via the gear icon to store defaults and choose the UI language.

### Daily Workflow

#### Inspect Changes
- The left tree view lists modified files grouped by folder.
- Use the search box to filter entries by file name.
- The “Changes” card displays counts for each change type (Added, Modified, Deleted, etc.).

#### Stage and Commit
1. Click **Stage All** if you want to stage everything (`git add -A`).
2. Enter a commit message in the “Commit” card; the commit button enables once there is staged content.
3. Press **Commit** to run `git commit -m "<message>"`. Notifications appear in the status card if Git reports errors.

#### Push to Remote
1. Make sure the remote and branch fields are filled, or enable “Use custom remote URL” to provide an explicit URL.
2. Hit **Push** to run `git push <remote> <branch>`.

#### Pull Specific Revisions
1. Click **Pull** in the toolbar to open the history picker, or click any entry in the “Recent commits” log.
2. Confirm the dialog to fetch the remote branch and run `git reset --hard <hash>`.
3. The window refreshes automatically after the pull completes.

### Troubleshooting
- **No Git output / commands fail**: ensure Git is installed and that the Unity process has permission to run it.
- **Buttons disabled**: remote or branch fields may be empty; fill them to enable Pull/Push.
- **Localization placeholders**: reopen the window after switching languages to force a refresh.

### Keyboard & UX Tips
- Use the refresh toolbar button (or shortcut `Ctrl/Cmd + R` if assigned) to re-fetch status without re-opening the window.
- Hover over toolbar icons to see tooltips; the three-dot button lists detected remotes.
- Notifications appear in the lower-right corner of the window; click elsewhere to dismiss them.

### Support
Please send bug reports or feature requests to `support@fire-tools.example`.

---

## 中文使用说明 (Chinese User Manual)

### 界面速览

- **英雄信息卡**：分支、远端、未推送数量以及帮助链接集中在顶部卡片中，方便团队上下同步文。
- **工具条按钮**：Refresh/Stage/Commit/Push/Pull 采用图标+文字的卡片式设计，禁用态与提示语一致。
- **三栏布局**：左侧改动树、右侧最近提交和底部日志区域统一配色，保证大仓库下仍然整洁。
- **设置入口**：右上角的齿轮按钮可切换语言、打开项目设置，通知提示也集中在同一区域。

### 目录

- [插件简介](#插件简介)
- [快速开始](#快速开始)
- [界面介绍](#界面介绍)
- [功能详解](#功能详解)
- [使用场景](#使用场景)
- [常见问题](#常见问题)
- [注意事项](#注意事项)

### 插件简介

**版本控制助手** 是一款专为 Unity 编辑器设计的 Git 版本控制工具，提供直观的图形界面，让 Git 操作更加便捷高效。

#### 主要解决的问题
- **无需离开 Unity**：在 Unity 编辑器内完成所有 Git 操作
- **可视化改动**：树状图清晰展示文件变更状态
- **简化工作流**：常用 Git 操作一键完成
- **历史记录查看**：快速浏览提交历史和远端版本

#### 核心优势
- ✅ **直观的图形界面**：树状图展示文件变更，颜色编码区分状态
- ✅ **一站式操作**：查看、暂存、提交、推送、拉取都在一个窗口
- ✅ **智能提示**：自动检测分支、远端，提供操作建议
- ✅ **历史版本选择**：可视化选择远端历史版本进行拉取
- ✅ **多语言支持**：支持中文、英文、日文界面

### 快速开始

#### 1. 前置要求
- 确保系统已安装 **Git** 并可在命令行中调用
- Unity 编辑器需要对项目根目录拥有读写权限
- 项目必须是 Git 仓库（已初始化）

#### 2. 打开插件窗口
在 Unity 编辑器顶部菜单栏，点击 **`Tools` → `Version Control Assistant`** 打开插件窗口。

#### 3. 首次使用
1. 插件会自动检测当前分支和远端信息
2. 如果没有配置远端，可以在工具栏中手动输入
3. 查看左侧的变更列表，了解当前工作区状态
4. 开始使用各种 Git 操作

### 界面介绍

#### 顶部工具栏
位于窗口最上方，包含常用操作按钮和配置选项：
- **刷新**：刷新 Git 状态、分支信息和提交历史
- **全部暂存**：执行 `git add -A`，暂存所有改动
- **拉取**：从远端拉取更新（会打开历史版本选择窗口）
- **远端**：显示和编辑当前远端名称
- **分支**：显示和编辑推送分支名称
- **帮助**：打开在线帮助文档
- **设置**（齿轮图标）：切换语言和打开项目设置

#### 顶部信息卡片
显示当前 Git 仓库的关键信息：
- **当前分支**：显示你正在工作的分支
- **推送远端**：显示默认的推送远端名称
- **查看完整使用教程**：快速访问在线文档

#### 左侧面板
**状态总览卡片**
- **当前分支**：显示分支名称
- **状态摘要**：显示改动统计（如"已修改 5 · 新增 2"）
- **错误信息**：如果 Git 命令执行失败，会显示错误提示

**变更列表卡片**
- **搜索框**：快速查找特定文件
- **变更统计**：顶部显示各类变更的数量和分布
- **文件树**：按目录分组显示所有改动的文件
- **颜色编码**：
  - 🟢 **绿色**：新增文件
  - 🔵 **蓝色**：已修改文件
  - 🔴 **红色**：已删除文件
  - 🟡 **黄色**：已重命名文件
  - 🟣 **紫色**：未跟踪文件

#### 右侧面板
**提交信息卡片**
用于编写提交信息和配置推送目标：
- **提交说明**：多行文本输入框，填写提交信息
- **推送目标**：
  - **远端**：选择或输入远端名称（如 `origin`）
  - **分支**：选择或输入分支名称（如 `main`）
  - **自定义远端地址**：如果仓库没有配置远端名称，可以输入完整的 Git URL
- **提交按钮**：提交当前改动
- **推送按钮**：推送到远端

**最近提交卡片**
显示最近的提交历史：
- **时间线视图**：以时间线形式展示提交记录
- **提交信息**：显示提交消息、作者、时间和哈希值
- **作者标签**：不同作者使用不同颜色标识
- **自动滚动**：提交较多时自动出现滚动条

### 功能详解

#### 🔄 刷新
点击工具栏的 **"刷新"** 按钮，插件会：
1. 重新读取 Git 状态（`git status`）
2. 更新当前分支信息
3. 刷新提交历史记录
4. 更新远端列表

**使用场景**：在外部修改文件后、执行 Git 命令后、需要查看最新状态时。

#### 📦 全部暂存
点击工具栏的 **"全部暂存"** 按钮，会执行 `git add -A`，将所有改动（包括新增、修改、删除）添加到暂存区。
**注意**：这会暂存**所有**改动，包括未跟踪的文件。

#### 💾 提交
**编写提交信息**
在右侧 **"提交信息"** 卡片中填写提交说明。
建议格式：
```
标题：简短描述
（空行）
详细说明：修改内容、原因等
```

**执行提交**
1. 确保有改动可以提交
2. 填写提交说明
3. 点击 **"提交"** 按钮
插件会自动暂存所有改动并执行提交。

#### 📤 推送
**配置推送目标**
- **默认**：输入远端名称（如 `origin`）和分支（如 `main`）
- **自定义**：勾选 **"使用自定义远端地址"** 并输入完整 URL

**执行推送**
点击 **"推送"** 按钮执行 `git push`。
**注意**：推送前建议先拉取，避免冲突。

#### 📥 拉取
点击工具栏的 **"拉取"** 按钮，打开 **"选择拉取版本"** 窗口。
**工作流程**：
1. 插件执行 `git fetch` 获取远端信息
2. 用户选择目标版本
3. 点击 **"拉取所选版本"**，插件执行 `git reset --hard`

**⚠️ 警告**：`git reset --hard` 会**丢弃**本地所有未提交的改动！使用前请务必提交或备份。

#### 🔍 搜索文件
在 **"变更列表"** 卡片顶部的搜索框中输入关键词，支持文件名或路径实时过滤。

#### ⚙️ 设置
点击工具栏右侧的 **设置图标** 可以切换语言（中文/English/日本語）或打开 Unity 项目设置进行默认值配置。

### 常见问题

**Q1: 插件无法检测到 Git 仓库？**
A: 确保项目根目录有 `.git` 文件夹，且 Git 已安装并配置在系统 PATH 中。Unity 编辑器需要读写权限。

**Q2: 为什么看不到远端信息？**
A: 检查 `git remote -v` 是否配置了远端。点击刷新按钮重试。

**Q3: 提交按钮是灰色的？**
A: 提交按钮启用需要：1. 有改动；2. 填写了提交说明。

**Q4: 推送失败怎么办？**
A: 检查权限、网络、分支是否存在。首次推送可能需要 `git push -u`。

**Q5: 拉取操作会丢失我的改动吗？**
A: **是的**，`git reset --hard` 会丢弃未提交改动。请先提交。

### 支持

如需商业授权支持或有任何疑问，请联系 `support@fire-tools.example`。
