# Fire Unity Project

本仓库是一个基于 Unity 2021.3 LTS 的示例项目，包含多个美术资源包（低多边形环境、角色、道具等）和一个自研的 VR 转换工具包 `Packages/com.fire.vrconverter`。通过该项目，你可以快速体验或搭建 Fire VR 的基础运行环境。

## 环境要求
- Windows 10 或更高版本。
- Unity 2021.3 LTS（已安装 Universal RP、XR 模块）。
- Git 2.30+，可通过 HTTPS 或 SSH 访问 GitHub。

## 仓库结构
- `Assets/`：全部场景、模型、材质、脚本资源。
- `Packages/`：Unity Package 依赖及本地包（例如 `com.fire.vrconverter`）。
- `ProjectSettings/`：Unity 项目的全局配置。
- `UserSettings/`：本地化设置（可忽略版本控制）。
- `Logs/`、`Library/`、`obj/` 等目录为 Unity/IDE 生成，可通过 `.gitignore` 排除。

## 快速开始
1. **克隆仓库**  
   ```powershell
   git clone https://github.com/lyzbcy/fire.git
   cd fire
   ```
   如使用 SSH：`git clone git@github.com:lyzbcy/fire.git`
2. **打开 Unity**  
   - 启动 Unity Hub，选择 *Open* 指向仓库根目录。
   - 首次导入可能耗时较长，请等待脚本编译完成。
3. **运行示例场景**  
   - 在 `Assets/` 中选择任意示例场景（例如环境演示场景，或 VR 相关场景），点击 *Play* 进入编辑器预览。

## VR 项目转换工具
仓库内置 `Fire VR Converter`，位于 `Packages/com.fire.vrconverter`，其详细使用方式见包内 `README.md`。核心能力：
- 自动写入 XR 所需的 Unity Package（XR Management / OpenXR / XR Interaction Toolkit）。
- 生成并配置 `XR General Settings`、`XR Manager Settings`。
- 在当前场景创建或更新 XR Origin / VR Rig（摄像机、左右手控制器、Interaction Manager 等）。

### 快速指引
1. 打开 Unity 菜单 `Tools > Fire VR > 一键转换当前项目为VR...`
2. 在弹出窗口中点击 **一键执行所有步骤（推荐）**。
3. 等待日志提示完成后，场景会生成可运行的 VR Rig。

更多高级操作（分步执行、常见问题）请参考 `Packages/com.fire.vrconverter/README.md`。

## Git 协作流程
> 以下示例使用主仓库 `https://github.com/lyzbcy/fire`。

1. **同步最新代码**
   ```powershell
   git pull origin main
   ```
   如有多分支协作，替换为对应分支名称。
2. **开发与提交**
   ```powershell
   git status               # 查看当前改动
   git add <file|.>         # 暂存
   git commit -m "feat: 描述变更"
   ```
3. **推送到远程**
   ```powershell
   git push origin <branch>
   ```
4. **创建/更新功能分支**
   ```powershell
   git checkout -b feature/my-task   # 新分支
   git push -u origin feature/my-task
   ```
5. **处理冲突**
   - `git pull --rebase origin <branch>` 同步时若提示冲突，在编辑器中解决后重新 `git add`、`git rebase --continue`。
   - 保持提交记录整洁，必要时使用 `git status`、`git diff` 检查。

## 常见问题
- 首次打开项目导入时间较长：属于正常现象，等待 Unity 完成 Asset 和脚本编译即可。
- Git 提示行尾符差异：Windows/Unity 会生成 CRLF，可保留默认设置或在 `.gitattributes` 中统一。
- 权限不足无法推送：确认已在 GitHub 上添加协作者权限或使用正确的凭证（个人访问令牌/SSH Key）。

如需更多帮助，可在 Issue 中提问或联系项目维护者。祝开发顺利！


