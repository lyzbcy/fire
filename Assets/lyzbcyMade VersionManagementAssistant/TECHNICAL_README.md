# 技术文档：Version Control Assistant (版本控制助手)

## 1. 项目概览
**Version Control Assistant** 是一个集成在 Unity 编辑器中的 Git 版本控制扩展工具。与依赖原生库（如 `libgit2`）的插件不同，本工具通过封装 **Git 命令行接口 (CLI)** 来实现功能，从而确保了与标准 Git 工作流和配置的高度兼容性。

## 2. 技术栈 (Technology Stack)

### 核心技术
*   **开发语言**: C# (Unity .NET Runtime)
*   **运行环境**: Unity Editor (Editor Scripting)
*   **版本控制系统**: Git (System CLI)

### 系统 API
*   **`System.Diagnostics.Process`**: 插件的核心骨架。用于启动外部 Git 进程、执行命令并捕获输出。
*   **`System.IO`**: 用于处理文件路径、验证目录存在性以及读取流。
*   **`System.Text`**: 显式处理 UTF-8 编码，确保在不同语言环境（中文/日文/英文）下字符显示的正确性。

## 3. 架构与实现细节

本项目采用分层架构，将 UI 表现层与核心 Git 逻辑层分离。

### 3.1 核心逻辑层 (`GitProcessUtility.cs`)
这是一个静态类，充当 Unity 与 Git 可执行文件之间的桥梁。

*   **进程执行策略**:
    *   使用 `ProcessStartInfo` 配置 `RedirectStandardOutput` 和 `RedirectStandardError`，在不弹出命令行窗口的情况下捕获命令结果 (`CreateNoWindow = true`)。
    *   **跨平台支持**: 动态检测 Git 可执行文件（Windows 下查找 `git` 或 `git.exe`，macOS/Linux 下查找 `git`）。
    *   **异步流读取**: 利用 `OutputDataReceived` 和 `ErrorDataReceived` 事件安全地捕获大量输出（如长日志），防止死锁，尽管为了保证编辑器内的数据一致性，主调用通常表现为同步等待。

*   **数据解析策略**:
    *   **状态解析**: 解析 `git status --porcelain` 的输出。这是一个“管道级”命令，提供稳定的、机器可读的格式，不受用户配置或 Git 版本影响，确保了状态追踪（新增、修改、删除、未跟踪、重命名）的健壮性。
    *   **日志解析**: 在 `git log` 中使用自定义的分隔符格式 (例如 `--pretty=format:"%h%x1F%an%x1F%cr%x1F%s"`)。利用单元分隔符 (`\x1F`) 来可靠地分割提交哈希、作者、日期和消息，避免因用户提交信息中包含特殊字符而导致标准正则解析失败的问题。

### 3.2 UI 层 (`VersionControlAssistantWindow.cs` & `GitTreeView.cs`)
*   **框架**: Unity Editor GUI (通过代码绘制的编辑器界面)。
*   **架构设计**:
    *   **树状视图 (Tree View)**: 实现了层级视图逻辑，将变动的文件按文件夹结构分组显示，复刻了 IDE（如 Rider 或 VS Code）的浏览体验。
    *   **本地化 (Localization)**: 自定义的 `GitLocalization` 系统支持 UI 标签和错误信息的动态语言切换（中文/英文/日文）。

## 4. 关键技术特性

### 健壮的命令处理
工具处理了 CLI 封装中常见的边界情况：
*   **编码处理**: 显式处理 UTF-8，以支持文件路径和提交信息中的非 ASCII 字符。
*   **错误安全**: 分离 `stdout` (标准输出，代表成功) 和 `stderr` (标准错误，代表错误/警告)，确保 Git 的警告信息（通常输出到 stderr）不会导致解析逻辑崩溃。

### 性能优化
*   **懒加载**: 对昂贵的操作（如 `git log`）进行限制（例如默认 `-n 12`），防止阻塞 UI 线程。
*   **后台处理**: (隐含设计) 状态检查通常由特定事件触发，以最小化对 Unity 主线程的性能影响。

## 5. 系统要求

*   **Unity 版本**: 兼容现代 Unity 版本（通过 `manifest.json` 依赖项验证）。
*   **系统依赖**:
    *   **Git**: 必须在宿主操作系统上安装 Git，并确保其已添加到系统环境变量 `PATH` 中。
