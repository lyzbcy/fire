using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OneClick.VRConverter.Editor
{
    /// <summary>
    /// 国际化支持类，提供多语言界面文本
    /// </summary>
    public static class Localization
    {
        public enum Language
        {
            Chinese,
            English,
            Japanese
        }

        private static Language _currentLanguage = Language.Chinese;
        private static Dictionary<string, Dictionary<Language, string>> _translations;

        static Localization()
        {
            InitializeTranslations();
        }

        /// <summary>
        /// 设置当前语言
        /// </summary>
        public static void SetLanguage(Language language)
        {
            _currentLanguage = language;
            PlayerPrefs.SetString("VRConverter.Language", language.ToString());
        }

        /// <summary>
        /// 获取当前语言
        /// </summary>
        public static Language GetCurrentLanguage()
        {
            if (PlayerPrefs.HasKey("VRConverter.Language"))
            {
                var saved = PlayerPrefs.GetString("VRConverter.Language");
                if (Enum.TryParse<Language>(saved, out var lang))
                {
                    _currentLanguage = lang;
                }
            }
            return _currentLanguage;
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            if (_translations == null || !_translations.ContainsKey(key))
            {
                return key; // 如果找不到翻译，返回键名
            }

            var translation = _translations[key];
            if (!translation.ContainsKey(_currentLanguage))
            {
                // 如果当前语言没有翻译，尝试使用中文
                if (translation.ContainsKey(Language.Chinese))
                {
                    return string.Format(translation[Language.Chinese], args);
                }
                return key;
            }

            var text = translation[_currentLanguage];
            return args.Length > 0 ? string.Format(text, args) : text;
        }

        /// <summary>
        /// 初始化翻译字典
        /// </summary>
        private static void InitializeTranslations()
        {
            _translations = new Dictionary<string, Dictionary<Language, string>>();

            // 窗口标题和头部
            AddTranslation("Window.Title", "一键VR转换", "One-Click VR Converter", "ワンクリックVR変換");
            AddTranslation("Window.Header.Title", "一键 VR 项目转换", "One-Click VR Project Converter", "ワンクリック VR プロジェクト変換");
            AddTranslation("Window.Header.Description", 
                "面向新手的引导式工具：帮助你将当前项目快速配置为基础 VR 项目，自动处理 XR 包依赖、XR Plug-in Management 配置以及场景中的 XR Rig。",
                "A guided tool for beginners: Quickly configure your project as a basic VR project, automatically handling XR package dependencies, XR Plug-in Management configuration, and XR Rig in scenes.",
                "初心者向けのガイドツール：現在のプロジェクトを基本的なVRプロジェクトに素早く設定し、XRパッケージ依存関係、XR Plug-in Management設定、シーン内のXR Rigを自動的に処理します。");

            // 模式选择
            AddTranslation("Mode.Title", "模式选择", "Mode Selection", "モード選択");
            AddTranslation("Mode.Guided", "傻瓜式一键", "Guided Mode", "ガイドモード");
            AddTranslation("Mode.Professional", "专业模式", "Professional Mode", "プロフェッショナルモード");

            // 状态信息
            AddTranslation("Status.Compiling", "Unity 正在导入或编译脚本，请等待完成后再执行\"第 2 步\"或\"一键执行\"操作。", 
                "Unity is importing or compiling scripts. Please wait for completion before executing \"Step 2\" or \"Run All Steps\".",
                "Unityがスクリプトをインポートまたはコンパイル中です。完了するまで待ってから「ステップ2」または「すべて実行」を実行してください。");
            AddTranslation("Status.Ready", "当前状态良好，可以直接执行\"一键执行所有步骤（推荐）\"。", 
                "Current status is good. You can directly execute \"Run All Steps (Recommended)\".",
                "現在の状態は良好です。「すべてのステップを実行（推奨）」を直接実行できます。");

            // 引导模式
            AddTranslation("Guided.Step1.Title", "第 1 步：确保 XR 相关包已安装", "Step 1: Ensure XR Packages are Installed", "ステップ1：XR関連パッケージのインストール確認");
            AddTranslation("Guided.Step1.Description", "检查并安装必要的 XR 包（XR Management、OpenXR、XR Interaction Toolkit、Input System）。", 
                "Check and install necessary XR packages (XR Management, OpenXR, XR Interaction Toolkit, Input System).",
                "必要なXRパッケージ（XR Management、OpenXR、XR Interaction Toolkit、Input System）を確認してインストールします。");
            AddTranslation("Guided.Step1.Button", "执行第 1 步", "Execute Step 1", "ステップ1を実行");

            AddTranslation("Guided.Step2.Title", "第 2 步：配置项目设置", "Step 2: Configure Project Settings", "ステップ2：プロジェクト設定の構成");
            AddTranslation("Guided.Step2.Description", "配置 XR Plug-in Management 和 OpenXR 设置。", 
                "Configure XR Plug-in Management and OpenXR settings.",
                "XR Plug-in ManagementとOpenXR設定を構成します。");
            AddTranslation("Guided.Step2.Button", "执行第 2 步", "Execute Step 2", "ステップ2を実行");

            AddTranslation("Guided.Step3.Title", "第 3 步：转换当前场景", "Step 3: Convert Current Scene", "ステップ3：現在のシーンの変換");
            AddTranslation("Guided.Step3.Description", "在场景中创建或更新 XR Origin，并配置必要的组件。", 
                "Create or update XR Origin in the scene and configure necessary components.",
                "シーン内にXR Originを作成または更新し、必要なコンポーネントを構成します。");
            AddTranslation("Guided.Step3.Button", "执行第 3 步", "Execute Step 3", "ステップ3を実行");

            AddTranslation("Guided.RunAll.Title", "一键执行所有步骤（推荐）", "Run All Steps (Recommended)", "すべてのステップを実行（推奨）");
            AddTranslation("Guided.RunAll.Description", "按顺序执行上述所有步骤，适合首次使用。", 
                "Execute all steps above in order. Suitable for first-time use.",
                "上記のすべてのステップを順番に実行します。初回使用に適しています。");
            AddTranslation("Guided.RunAll.Button", "一键执行", "Run All", "すべて実行");

            // 专业模式
            AddTranslation("Pro.EnsurePackages", "确保 XR 包已安装", "Ensure XR Packages Installed", "XRパッケージのインストール確認");
            AddTranslation("Pro.ConfigureSettings", "配置项目设置", "Configure Project Settings", "プロジェクト設定の構成");
            AddTranslation("Pro.ConvertScene", "转换场景", "Convert Scene", "シーンの変換");
            AddTranslation("Pro.ConfigureSimulator", "配置设备模拟器", "Configure Device Simulator", "デバイスシミュレーターの構成");
            AddTranslation("Pro.TargetGroups", "目标平台组", "Target Platform Groups", "ターゲットプラットフォームグループ");
            AddTranslation("Pro.RigStrategy", "Rig 策略", "Rig Strategy", "Rig戦略");
            AddTranslation("Pro.RigStrategy.Auto", "自动", "Auto", "自動");
            AddTranslation("Pro.RigStrategy.OnlyUpdate", "仅更新现有 XR Origin", "Only Update Existing XR Origin", "既存のXR Originのみ更新");
            AddTranslation("Pro.RigStrategy.ForceFallback", "强制使用备用 Rig", "Force Fallback Rig", "フォールバックRigを強制使用");
            AddTranslation("Pro.RigStrategy.Skip", "跳过场景更改", "Skip Scene Changes", "シーン変更をスキップ");
            AddTranslation("Pro.DisableLegacyCamera", "禁用旧版相机", "Disable Legacy Camera", "レガシーカメラを無効化");
            AddTranslation("Pro.Execute", "执行转换", "Execute Conversion", "変換を実行");

            // 兼容性检查
            AddTranslation("Compatibility.Title", "项目体检", "Project Health Check", "プロジェクト診断");
            AddTranslation("Compatibility.UnityVersion", "Unity 版本", "Unity Version", "Unityバージョン");
            AddTranslation("Compatibility.Status.Compatible", "兼容", "Compatible", "互換性あり");
            AddTranslation("Compatibility.Status.Warning", "警告", "Warning", "警告");
            AddTranslation("Compatibility.Status.Incompatible", "不兼容", "Incompatible", "互換性なし");

            // 日志区域
            AddTranslation("Log.Title", "执行日志（可帮助排查问题）", "Execution Log (Helpful for Troubleshooting)");
            AddTranslation("Log.Description", 
                "这里会实时显示每一步执行情况，例如：\n- 是否成功安装 XR 相关包；\n- 是否成功启用 OpenXR Loader；\n- 场景中是否成功创建 XR Origin / VRRig 等。\n当你遇到问题时，可以先查看此处日志再处理。",
                "This will display the execution status of each step in real-time, for example:\n- Whether XR packages were successfully installed;\n- Whether OpenXR Loader was successfully enabled;\n- Whether XR Origin / VRRig was successfully created in the scene.\nWhen you encounter problems, you can check this log first.");
            AddTranslation("Log.ViewFile", "查看日志文件", "View Log File");
            AddTranslation("Log.Export", "导出日志", "Export Log");

            // 错误消息
            AddTranslation("Error.OperationFailed", "操作失败", "Operation Failed");
            AddTranslation("Error.BackupFailed", "备份失败", "Backup Failed");
            AddTranslation("Error.LogExportFailed", "导出日志失败", "Log Export Failed");
            AddTranslation("Error.LogFileNotFound", "日志文件不存在或尚未创建。", "Log file does not exist or has not been created yet.");

            // 成功消息
            AddTranslation("Success.LogExported", "日志已导出到:\n{0}", "Log exported to:\n{0}");

            // 备份相关
            AddTranslation("Backup.Created", "已创建操作备份: {0}", "Operation backup created: {0}");
            AddTranslation("Backup.Rollback", "回滚到转换前版本", "Rollback to Pre-Conversion Version");
            AddTranslation("Backup.BaselineRecorded", "记录的提交：{0}", "Recorded commit: {0}");
            AddTranslation("Backup.NoBaseline", "尚未记录可回滚的提交", "No rollback commit recorded yet");

            // 通用按钮
            AddTranslation("Button.OK", "确定", "OK", "OK");
            AddTranslation("Button.Cancel", "取消", "Cancel", "キャンセル");
            AddTranslation("Button.Yes", "是", "Yes", "はい");
            AddTranslation("Button.No", "否", "No", "いいえ");
            AddTranslation("Button.Retry", "重试", "Retry", "再試行");
            AddTranslation("Button.Good", "好的", "OK", "了解");
            AddTranslation("Button.IGotIt", "好的，我知道了", "OK, I Got It", "了解しました");

            // 专业模式 UI
            AddTranslation("Pro.Plan.Title", "专业模式计划", "Professional Mode Plan");
            AddTranslation("Pro.Plan.Description", "自定义执行步骤与目标平台，适配已有项目结构。", 
                "Customize execution steps and target platforms to adapt to existing project structure.");
            AddTranslation("Pro.Plan.CheckPackages", "XR 依赖检查 / 安装", "XR Dependency Check / Install");
            AddTranslation("Pro.Plan.CheckProjectSettings", "Project Settings：XR Plug-in 配置", "Project Settings: XR Plug-in Configuration");
            AddTranslation("Pro.Plan.ConvertScene", "场景转换（XR Rig / VRRig）", "Scene Conversion (XR Rig / VRRig)");
            AddTranslation("Pro.Plan.DeviceSimulator", "配置 VR 模拟设备（XR Device Simulator）", "Configure VR Simulator Device (XR Device Simulator)");
            AddTranslation("Pro.TargetPlatforms", "目标平台", "Target Platforms");
            AddTranslation("Pro.TargetPlatform.For", "为 {0} 配置 XR", "Configure XR for {0}");
            AddTranslation("Pro.TargetPlatform.NoSelection", "未选择目标平台时将回退到默认（Standalone + Android）。", 
                "Will fallback to default (Standalone + Android) when no target platform is selected.");
            AddTranslation("Pro.SceneStrategy", "场景转换策略", "Scene Conversion Strategy");
            AddTranslation("Pro.RigStrategy.Tooltip", "选择如何处理 XR Origin / VRRig。", "Choose how to handle XR Origin / VRRig.");
            AddTranslation("Pro.PreserveLegacyCamera", "保留现有 Main Camera（不强制禁用）", "Preserve Existing Main Camera (Do Not Force Disable)");
            AddTranslation("Pro.ExecutePlan", "执行专业模式计划", "Execute Professional Mode Plan");

            // 快速开始
            AddTranslation("QuickStart.Title", "快速开始（推荐）", "Quick Start (Recommended)", "クイックスタート（推奨）");
            AddTranslation("QuickStart.Description", 
                "适合第一次接触 VR 项目的同学：点击一次即可按顺序执行所有必要步骤。" +
                "如果中途需要重新导入包，可以稍后再单独执行\"第 2 步\"。",
                "Suitable for those new to VR projects: Click once to execute all necessary steps in order. " +
                "If packages need to be reimported midway, you can execute \"Step 2\" separately later.",
                "VRプロジェクトを初めて扱う方に適しています：1回クリックするだけで、必要なすべてのステップを順番に実行します。" +
                "途中でパッケージを再インポートする必要がある場合は、後で「ステップ2」を個別に実行できます。");
            AddTranslation("QuickStart.RunAll.Tooltip", 
                "依次执行：\n" +
                "1. 检查并安装 XR Management / OpenXR / XR Interaction Toolkit；\n" +
                "2. 自动配置 XR Plug-in Management（Standalone + Android 启用 OpenXR）；\n" +
                "3. 将当前场景转换为 VR 场景并创建/更新 XR Rig。",
                "Execute in order:\n" +
                "1. Check and install XR Management / OpenXR / XR Interaction Toolkit;\n" +
                "2. Automatically configure XR Plug-in Management (Enable OpenXR for Standalone + Android);\n" +
                "3. Convert current scene to VR scene and create/update XR Rig.",
                "順番に実行：\n" +
                "1. XR Management / OpenXR / XR Interaction Toolkitを確認してインストール；\n" +
                "2. XR Plug-in Managementを自動構成（Standalone + AndroidでOpenXRを有効化）；\n" +
                "3. 現在のシーンをVRシーンに変換し、XR Rigを作成/更新。");
            AddTranslation("QuickStart.Hint", "如果你不熟悉 XR 配置，推荐优先使用上面的\"一键执行\"按钮。", 
                "If you are not familiar with XR configuration, it is recommended to use the \"Run All\" button above first.",
                "XR設定に慣れていない場合は、上記の「すべて実行」ボタンを優先的に使用することをお勧めします。");
            
            // 反向转换相关
            AddTranslation("Reverse.Title", "转换为 3D 项目", "Convert to 3D Project", "3Dプロジェクトに変換");
            AddTranslation("Reverse.Description", 
                "将当前 VR 项目转换回标准 3D 项目，移除 XR 相关配置和场景中的 VR 组件。",
                "Convert current VR project back to standard 3D project, removing XR configurations and VR components in scenes.",
                "現在のVRプロジェクトを標準の3Dプロジェクトに戻し、XR設定とシーン内のVRコンポーネントを削除します。");
            AddTranslation("Reverse.RunAll.Title", "一键转换为 3D 项目", "Convert to 3D Project", "3Dプロジェクトに変換");
            AddTranslation("Reverse.RunAll.Tooltip",
                "依次执行：\n" +
                "1. 移除 XR Management / OpenXR / XR Interaction Toolkit 包；\n" +
                "2. 禁用 XR Plug-in Management 设置；\n" +
                "3. 将当前场景转换回 3D 场景并恢复 Main Camera。",
                "Execute in order:\n" +
                "1. Remove XR Management / OpenXR / XR Interaction Toolkit packages;\n" +
                "2. Disable XR Plug-in Management settings;\n" +
                "3. Convert current scene back to 3D scene and restore Main Camera.",
                "順番に実行：\n" +
                "1. XR Management / OpenXR / XR Interaction Toolkitパッケージを削除；\n" +
                "2. XR Plug-in Management設定を無効化；\n" +
                "3. 現在のシーンを3Dシーンに戻し、Main Cameraを復元。");
            AddTranslation("Log.PackageRemoved", "已移除包依赖: {0}", "Removed package dependency: {0}", "パッケージ依存関係を削除: {0}");
            AddTranslation("Log.NoPackagesToRemove", "未找到需要移除的 XR 包", "No XR packages found to remove", "削除するXRパッケージが見つかりません");
            AddTranslation("Log.XrSettingsDisabled", "已禁用 {0} 的 XR 设置", "Disabled XR settings for {0}", "{0}のXR設定を無効化しました");
            AddTranslation("Log.SceneConvertedTo3D", "场景已转换为 3D 模式", "Scene converted to 3D mode", "シーンを3Dモードに変換しました");
            AddTranslation("Log.MainCameraRestored", "已恢复 Main Camera", "Main Camera restored", "Main Cameraを復元しました");
            AddTranslation("Log.MainCameraCreated", "已创建新的 Main Camera", "Created new Main Camera", "新しいMain Cameraを作成しました");
            AddTranslation("Log.MainCameraEnabled", "已启用被禁用的 Main Camera", "Enabled disabled Main Camera", "無効化されていたMain Cameraを有効化しました");
            AddTranslation("Log.XrOriginRemoved", "已移除 XR Origin", "XR Origin removed", "XR Originを削除しました");
            AddTranslation("Log.VrRigRemoved", "已移除 VRRig", "VRRig removed", "VRRigを削除しました");
            AddTranslation("Log.InteractionManagerRemoved", "已移除 XR Interaction Manager", "XR Interaction Manager removed", "XR Interaction Managerを削除しました");
            AddTranslation("Log.InputActionManagerRemoved", "已移除 XR Input Action Manager", "XR Input Action Manager removed", "XR Input Action Managerを削除しました");
            AddTranslation("Log.GeneratedVrAssetsCleaned", "已清理生成的 VR 资源目录", "Cleaned up generated VR assets directory", "生成されたVRリソースディレクトリをクリーンアップしました");
            
            // 项目类型
            AddTranslation("ProjectType.VR", "当前项目类型：VR 项目", "Current Project Type: VR Project", "現在のプロジェクトタイプ：VRプロジェクト");
            AddTranslation("ProjectType.Standard3D", "当前项目类型：3D 项目", "Current Project Type: 3D Project", "現在のプロジェクトタイプ：3Dプロジェクト");
            AddTranslation("ProjectType.Unknown", "当前项目类型：未知", "Current Project Type: Unknown", "現在のプロジェクトタイプ：不明");
            
            // 反向转换成功消息
            AddTranslation("Log.ReverseConversionComplete", "3D 项目转换完成，可以使用下方 Git 按钮创建备份或回滚。", 
                "3D project conversion complete, you can use the Git button below to create backup or rollback.",
                "3Dプロジェクト変換が完了しました。下のGitボタンを使用してバックアップまたはロールバックできます。");
            
            // 反向转换成功对话框
            AddTranslation("Dialog.ReverseConversionSuccess", "转换成功", "Conversion Successful", "変換成功");
            AddTranslation("Dialog.ReverseConversionSuccess.Message",
                "🎉 3D 转换成功！\n\n" +
                "您的项目已成功转换为 3D 项目。\n\n" +
                "主要变更：\n" +
                "• XR 包已从 manifest.json 移除\n" +
                "• XR 项目设置已禁用\n" +
                "• 场景中的 XR Origin/VRRig 已移除\n" +
                "• Main Camera 已恢复\n\n",
                "🎉 3D Conversion Successful!\n\n" +
                "Your project has been successfully converted to a 3D project.\n\n" +
                "Main Changes:\n" +
                "• XR packages removed from manifest.json\n" +
                "• XR project settings disabled\n" +
                "• XR Origin/VRRig removed from scene\n" +
                "• Main Camera restored\n\n",
                "🎉 3D変換成功！\n\n" +
                "プロジェクトが正常に3Dプロジェクトに変換されました。\n\n" +
                "主な変更：\n" +
                "• manifest.jsonからXRパッケージを削除\n" +
                "• XRプロジェクト設定を無効化\n" +
                "• シーンからXR Origin/VRRigを削除\n" +
                "• Main Cameraを復元\n\n");

            // 步骤卡片
            AddTranslation("Step1.Title", "第 1 步：准备 XR 依赖包", "Step 1: Prepare XR Dependency Packages", "ステップ1：XR依存パッケージの準備");
            AddTranslation("Step1.Description", 
                "在 Packages/manifest.json 中检查并安装如下 XR 相关包：\n" +
                "- XR Management\n- OpenXR\n- XR Interaction Toolkit\n\n" +
                "适合刚将普通项目升级为 VR 项目时使用。",
                "Check and install the following XR-related packages in Packages/manifest.json:\n" +
                "- XR Management\n- OpenXR\n- XR Interaction Toolkit\n\n" +
                "Suitable when upgrading a regular project to a VR project.",
                "Packages/manifest.jsonで以下のXR関連パッケージを確認してインストールします：\n" +
                "- XR Management\n- OpenXR\n- XR Interaction Toolkit\n\n" +
                "通常のプロジェクトをVRプロジェクトにアップグレードする際に適しています。");
            AddTranslation("Step1.Button", "执行第 1 步", "Execute Step 1", "ステップ1を実行");
            AddTranslation("Step1.Tooltip", 
                "仅执行 XR 依赖检查和安装，不会修改 XR 设置或场景。" +
                "\n建议在看到 Unity 编译完成后再继续执行第 2 步。",
                "Only execute XR dependency check and installation, will not modify XR settings or scene.\n" +
                "It is recommended to wait for Unity compilation to complete before continuing with Step 2.",
                "XR依存関係の確認とインストールのみを実行し、XR設定やシーンは変更しません。\n" +
                "Unityのコンパイルが完了するのを待ってからステップ2を続行することをお勧めします。");

            AddTranslation("Step2.Title", "第 2 步：配置项目 & 场景", "Step 2: Configure Project & Scene", "ステップ2：プロジェクトとシーンの構成");
            AddTranslation("Step2.Description", 
                "为 Standalone / Android 自动启用 OpenXR Loader，" +
                "并在当前场景内创建或更新 XR Origin（如可用）或基础 VRRig。\n\n" +
                "若你已经手动导入好 XR 包，可直接从第 2 步开始。",
                "Automatically enable OpenXR Loader for Standalone / Android, " +
                "and create or update XR Origin (if available) or basic VRRig in the current scene.\n\n" +
                "If you have already manually imported XR packages, you can start directly from Step 2.",
                "Standalone / Android用にOpenXR Loaderを自動的に有効化し、" +
                "現在のシーン内にXR Origin（利用可能な場合）または基本VRRigを作成または更新します。\n\n" +
                "XRパッケージを手動でインポート済みの場合は、ステップ2から直接開始できます。");
            AddTranslation("Step2.Button", "执行第 2 步", "Execute Step 2", "ステップ2を実行");
            AddTranslation("Step2.Tooltip.Compiling", "当前 Unity 正在编译，暂不可执行。请等待编译完成后再点击。", 
                "Unity is currently compiling, cannot execute. Please wait for compilation to complete before clicking.",
                "現在Unityがコンパイル中です。実行できません。コンパイルが完了するまで待ってからクリックしてください。");
            AddTranslation("Step2.Tooltip.Ready", "配置 XRGeneralSettings / XRManagerSettings，并在当前场景中创建或更新 VR Rig。", 
                "Configure XRGeneralSettings / XRManagerSettings, and create or update VR Rig in the current scene.",
                "XRGeneralSettings / XRManagerSettingsを構成し、現在のシーンにVR Rigを作成または更新します。");

            // Git 助手
            AddTranslation("Git.Title", "版本控制助手联动（备份 & 回滚）", "Version Control Assistant Integration (Backup & Rollback)", "バージョン管理アシスタント連携（バックアップ & ロールバック）");
            AddTranslation("Git.Description.Installed", 
                "检测到已安装版本控制助手：执行 VR 转换前请完成一次提交/标签备份，转换成功后可利用下方按钮快速回滚。",
                "Version Control Assistant detected: Please complete a commit/tag backup before executing VR conversion. " +
                "After successful conversion, you can use the button below to quickly rollback.",
                "バージョン管理アシスタントが検出されました：VR変換を実行する前に、コミット/タグバックアップを完了してください。変換が成功したら、下のボタンを使用してすばやくロールバックできます。");
            AddTranslation("Git.Description.NotInstalled", 
                "尚未检测到版本控制助手。建议先在 Package Manager 中导入 com.fire.gitassistant，以便执行自动备份与回滚。",
                "Version Control Assistant not detected. It is recommended to import com.fire.gitassistant from Package Manager first " +
                "to enable automatic backup and rollback.",
                "バージョン管理アシスタントが検出されていません。自動バックアップとロールバックを有効にするために、まずPackage Managerからcom.fire.gitassistantをインポートすることをお勧めします。");
            AddTranslation("Git.OpenAssistant", "打开版本控制助手", "Open Version Control Assistant", "バージョン管理アシスタントを開く");
            AddTranslation("Git.QuickBackup", "使用版本控制助手快速备份", "Quick Backup with Version Control Assistant", "バージョン管理アシスタントでクイックバックアップ");
            AddTranslation("Git.Rollback", "回滚到转换前版本", "Rollback to Pre-Conversion Version", "変換前のバージョンにロールバック");
            AddTranslation("Git.Baseline.Recorded", "记录的提交：{0}", "Recorded commit: {0}", "記録されたコミット：{0}");
            AddTranslation("Git.Baseline.NotRecorded", "尚未记录可回滚的提交", "No rollback commit recorded yet", "ロールバック可能なコミットがまだ記録されていません");
            AddTranslation("Git.Info", "安装版本控制助手后，可在此窗口中获得自动备份与回滚按钮。", 
                "After installing Version Control Assistant, you can get automatic backup and rollback buttons in this window.",
                "バージョン管理アシスタントをインストールすると、このウィンドウで自動バックアップとロールバックボタンを取得できます。");

            // 兼容性检查
            AddTranslation("Compatibility.RenderPipeline.BuiltIn", "内置渲染管线", "Built-in Render Pipeline");
            AddTranslation("Compatibility.RenderPipeline.BuiltInHint", "使用 Built-in Render Pipeline，可直接使用模板配置。", 
                "Using Built-in Render Pipeline, can directly use template configuration.");
            AddTranslation("Compatibility.RenderPipeline.Detected", "检测到 {0}，如使用 URP/HDRP，请确认对应 XR Renderer 已启用。", 
                "Detected {0}, if using URP/HDRP, please confirm the corresponding XR Renderer is enabled.");

            // 日志消息
            AddTranslation("Log.OpenTool", "打开一键 VR 转换工具。", "Opened One-Click VR Converter tool.");
            AddTranslation("Log.NoStepsSelected", "未选择需要执行的步骤。", "No steps selected for execution.");
            AddTranslation("Log.UserCancelled", "用户取消执行 {0}，原因：尚未完成备份确认。", 
                "User cancelled execution of {0}, reason: Backup confirmation not completed.");
            AddTranslation("Log.BackupCreated", "已创建操作备份: {0}", "Operation backup created: {0}");
            AddTranslation("Log.Compiling", "Unity 正在导入或编译脚本，请等待完成后再执行后续步骤。", 
                "Unity is importing or compiling scripts, please wait for completion before executing subsequent steps.");
            AddTranslation("Log.ManifestNotFound", "未找到 Packages/manifest.json，无法自动添加 XR 依赖。", 
                "Packages/manifest.json not found, cannot automatically add XR dependencies.");
            AddTranslation("Log.ManifestBackupCreated", "已创建 manifest.json 备份", "manifest.json backup created");
            AddTranslation("Log.LatestRemoved", "已移除 manifest.json 中的 \"latest\" 占位符，请等待 Unity 刷新。", 
                "Removed \"latest\" placeholder from manifest.json, please wait for Unity to refresh.");
            AddTranslation("Log.PackageExists", "已存在依赖：{0} ({1})", "Dependency already exists: {0} ({1})");
            AddTranslation("Log.PackageInvalidVersion", "检测到 {0} 使用无效版本 \"{1}\"，将重新安装。", 
                "Detected {0} using invalid version \"{1}\", will reinstall.");
            AddTranslation("Log.PackageNotFound", "manifest.json 中未找到 {0}，准备安装。", 
                "{0} not found in manifest.json, preparing to install.");
            AddTranslation("Log.AllPackagesInstalled", "所有必需 XR 包均已安装，无需额外操作。", 
                "All required XR packages are installed, no additional action needed.");
            AddTranslation("Log.XrManagementNotFound", "未检测到 XR Management 程序集，可能仍在导入。跳过自动配置。", 
                "XR Management assembly not detected, may still be importing. Skipping automatic configuration.");
            AddTranslation("Log.OpenXrLoaderNotFound", "未检测到 OpenXR Loader 类型，请确认 com.unity.xr.openxr 包已经导入。", 
                "OpenXR Loader type not detected, please confirm com.unity.xr.openxr package has been imported.");
            AddTranslation("Log.XrApiChanged", "XRGeneralSettingsPerBuildTarget API 发生变化，无法自动设置。", 
                "XRGeneralSettingsPerBuildTarget API has changed, cannot automatically configure.");
            AddTranslation("Log.CannotCreateGeneralSettings", "无法为 {0} 创建 XR General Settings。", 
                "Cannot create XR General Settings for {0}.");
            AddTranslation("Log.CannotCreateManagerSettings", "无法为 {0} 创建 XR Manager Settings。", 
                "Cannot create XR Manager Settings for {0}.");
            AddTranslation("Log.OpenXrEnabled", "已为 {0} 启用 OpenXR Loader。", "OpenXR Loader enabled for {0}.");
            AddTranslation("Log.OpenXrBindFailed", "未能为 {0} 自动绑定 OpenXR Loader，请手动在 Project Settings 中确认。", 
                "Failed to automatically bind OpenXR Loader for {0}, please manually confirm in Project Settings.");
            AddTranslation("Log.SimulatorNotWritable", "XR Device Simulator 设置不可写，已跳过自动配置。", 
                "XR Device Simulator settings are not writable, skipped automatic configuration.");
            AddTranslation("Log.SimulatorPrefabNotFound", "未能找到 XR Device Simulator 预制体，请在 Package Manager 中重新导入 XR Device Simulator Sample。", 
                "XR Device Simulator prefab not found, please reimport XR Device Simulator Sample from Package Manager.");
            AddTranslation("Log.SimulatorConfigured", "已配置 XR Device Simulator，进入 Play 模式即可使用键鼠模拟 VR 设备。", 
                "XR Device Simulator configured, you can use keyboard and mouse to simulate VR devices in Play mode.");
            AddTranslation("Log.SimulatorReady", "XR Device Simulator 已处于可用状态。", "XR Device Simulator is already available.");
            AddTranslation("Log.SimulatorVersionIncompatible", "当前 XR Interaction Toolkit 版本缺少 XR Device Simulator 设置，已跳过模拟设备配置。", 
                "Current XR Interaction Toolkit version lacks XR Device Simulator settings, skipped simulator device configuration.");
            AddTranslation("Log.SimulatorSettingsFailed", "未能创建 XR Device Simulator 设置实例。", 
                "Failed to create XR Device Simulator settings instance.");
            AddTranslation("Log.SimulatorSampleImported", "已自动导入 XR Device Simulator Sample，位于 Assets/VRConverterGenerated/DeviceSimulator。", 
                "XR Device Simulator Sample automatically imported, located at Assets/VRConverterGenerated/DeviceSimulator.");
            AddTranslation("Log.StarterAssetsCopied", "已复制 Starter Assets Sample，位于 Assets/VRConverterGenerated/StarterAssets。", 
                "Starter Assets Sample copied, located at Assets/VRConverterGenerated/StarterAssets.");
            AddTranslation("Log.SamplePackageNotFound", "未能定位 {0} 所在的包（{1}）。", 
                "Failed to locate package containing {0} ({1}).");
            AddTranslation("Log.SampleNotFound", "在 {0} 中未找到 {1} Sample（路径：{2}）。", 
                "Sample {1} not found in {0} (path: {2}).");
            AddTranslation("Log.NoSceneOpen", "当前没有打开的场景，无法转换。", "No scene is currently open, cannot convert.");
            AddTranslation("Log.SkipSceneChanges", "已根据专业模式设置，跳过场景转换步骤。", 
                "Skipping scene conversion step based on professional mode settings.");
            AddTranslation("Log.PreserveLegacyCamera", "专业模式：保留原 Main Camera，不自动禁用。", 
                "Professional mode: Preserve original Main Camera, do not automatically disable.");
            AddTranslation("Log.XrOriginUpdated", "已更新场景中的 XR Origin。", "XR Origin in scene updated.");
            AddTranslation("Log.XrOriginNotFound", "未找到现有 XR Origin，且策略为\"仅更新\"，未做额外改动。", 
                "No existing XR Origin found, and strategy is \"Only Update\", no additional changes made.");
            AddTranslation("Log.XrOriginCreated", "XR Origin (XR Interaction Toolkit) 已创建/更新。", 
                "XR Origin (XR Interaction Toolkit) created/updated.");
            AddTranslation("Log.FallbackRigUsed", "XR Interaction Toolkit 或 XR Core Utils 不可用，使用基础 VRRig。", 
                "XR Interaction Toolkit or XR Core Utils not available, using basic VRRig.");
            AddTranslation("Log.ConversionComplete", "VR 场景转换完成，可以使用下方 Git 按钮创建备份或回滚。", 
                "VR scene conversion complete, you can use the Git button below to create backup or rollback.");
            AddTranslation("Log.BaselineRecorded", "已记录转换前的 Git 提交：{0}", "Recorded pre-conversion Git commit: {0}");
            AddTranslation("Log.GeneralSettingsCreated", "已创建 XRGeneralSettingsPerBuildTarget 资产：{0}", 
                "Created XRGeneralSettingsPerBuildTarget asset: {0}");
            AddTranslation("Log.GeneralSettingsRegistered", "已注册 XR General Settings（{0}）。", 
                "Registered XR General Settings ({0}).");
            AddTranslation("Log.TargetGroupSettingsCreated", "已创建 {0} 的 XR General Settings。", 
                "Created XR General Settings for {0}.");
            AddTranslation("Log.ManagerSettingsCreated", "已创建 {0} 对应的 XR Manager Settings。", 
                "Created XR Manager Settings for {0}.");
            AddTranslation("Log.VrRigCreated", "已创建 {0}。", "Created {0}.");
            AddTranslation("Log.HandTrackingOptimized", "已为 {0} 添加手部追踪同步优化脚本。", 
                "Added hand tracking synchronization optimization script to {0}.");
            AddTranslation("Log.VrRigRootCreated", "已创建 VRRig 根对象。", "VRRig root object created.");
            AddTranslation("Log.VrRigExists", "场景中已存在 VRRig，对其进行复用/更新。", 
                "VRRig already exists in scene, reusing/updating it.");
            AddTranslation("Log.VrRigComplete", "基础 VR Rig 已创建/更新。", "Basic VR Rig created/updated.");
            AddTranslation("Log.CameraBinding", "已将 {0} 绑定到原主相机的父对象：{1}，并保持原位置和旋转。", 
                "Bound {0} to original main camera's parent object: {1}, maintaining original position and rotation.");
            AddTranslation("Log.MainCameraNotFound", "未找到标签为 MainCamera 的原主相机。", 
                "Original main camera with MainCamera tag not found.");
            AddTranslation("Log.MainCameraDisabled", "已禁用原主相机：{0}", "Original main camera disabled: {0}");
            AddTranslation("Log.CameraBindingDetected", "检测到原主相机绑定在：{0}，新的 XR Origin 将继承此绑定关系。", 
                "Detected original main camera bound to: {0}, new XR Origin will inherit this binding relationship.");
            AddTranslation("Log.UserDeferredSample", "用户选择稍后导入 {0} Sample。", "User chose to import {0} Sample later.");
            AddTranslation("Log.SampleAutoImported", "已自动导入 {0} Sample。", "{0} Sample automatically imported.");
            AddTranslation("Log.PackageManagerOpened", "已将\"XRI Default Input Actions\"绑定到 XR Input Action Manager，以自动启用输入映射。", 
                "Bound \"XRI Default Input Actions\" to XR Input Action Manager to automatically enable input mapping.");
            AddTranslation("Log.InputActionBound", "已为{0} Action Based Controller 绑定默认输入动作。", 
                "Bound default input actions for {0} Action Based Controller.");
            AddTranslation("Log.InputActionAssetCreated", "已生成输入动作引用资产：{0}", 
                "Generated input action reference asset: {0}");
            AddTranslation("Log.InputActionNotFound", "无法创建输入动作引用（{0}/{1}）：请确认\"XRI Default Input Actions\"中包含该 Action Map。", 
                "Cannot create input action reference ({0}/{1}): Please confirm \"XRI Default Input Actions\" contains this Action Map.");
            AddTranslation("Log.DefaultInputActionsNotFound", "未在项目中找到\"XRI Default Input Actions.inputactions\"。请在 Package Manager 中重新导入 XR Interaction Toolkit 的 Starter Assets。", 
                "\"XRI Default Input Actions.inputactions\" not found in project. Please reimport XR Interaction Toolkit Starter Assets from Package Manager.");
            AddTranslation("Log.GitRollbackComplete", "Git 回滚完成：{0}", "Git rollback complete: {0}");
            AddTranslation("Log.GitRollbackFailed", "回滚失败：{0}", "Rollback failed: {0}");
            AddTranslation("Log.GitCommitRecorded", "已记录当前 Git 提交 {0}，可在成功后回滚。", 
                "Recorded current Git commit {0}, can rollback after success.");
            AddTranslation("Log.GitCommitFailed", "未能记录当前 Git 提交，可能尚未初始化仓库。", 
                "Failed to record current Git commit, repository may not be initialized.");
            AddTranslation("Log.GitCommitError", "获取 Git 提交失败：{0}", "Failed to get Git commit: {0}");
            AddTranslation("Log.PackageInstallRequested", "已向 Package Manager 提交安装请求：{0}", 
                "Submitted installation request to Package Manager: {0}");
            AddTranslation("Log.PackageInstallSuccess", "包安装成功：{0}", "Package installation successful: {0}");
            AddTranslation("Log.PackageInstallFailed", "包安装失败：{0}", "Package installation failed: {0}");
            AddTranslation("Log.PackageInstallStatus", "包安装状态：{0}", "Package installation status: {0}");
            AddTranslation("Log.LatestVersionRemoved", "移除了 manifest.json 中 {0} 的 \"latest\" 版本约束。", 
                "Removed \"latest\" version constraint for {0} in manifest.json.");

            // 对话框消息
            AddTranslation("Dialog.Info", "提示", "Information");
            AddTranslation("Dialog.Success", "成功", "Success");
            AddTranslation("Dialog.Failed", "失败", "Failed");
            AddTranslation("Dialog.ConversionSuccess", "转换成功", "Conversion Successful");
            AddTranslation("Dialog.ImportComplete", "导入完成", "Import Complete");
            AddTranslation("Dialog.AutoImportFailed", "自动导入失败", "Auto Import Failed");
            AddTranslation("Dialog.PackageManagerOpened", "已打开 Package Manager", "Package Manager Opened");
            AddTranslation("Dialog.ManualImport", "请手动导入", "Please Import Manually");
            AddTranslation("Dialog.CannotOpenPackageManager", "无法打开 Package Manager", "Cannot Open Package Manager");
            AddTranslation("Dialog.BackupComplete", "备份完成", "Backup Complete");
            AddTranslation("Dialog.BackupFailed", "备份失败", "Backup Failed");
            AddTranslation("Dialog.CannotRollback", "无法回滚", "Cannot Rollback");
            AddTranslation("Dialog.RollbackComplete", "回滚完成", "Rollback Complete");
            AddTranslation("Dialog.RollbackFailed", "回滚失败", "Rollback Failed");
            AddTranslation("Dialog.GitAssistantNotFound", "未能打开版本控制助手，请确认已正确安装。", 
                "Failed to open Version Control Assistant, please confirm it is installed correctly.");
            AddTranslation("Dialog.LogFileNotFound", "日志文件不存在或尚未创建。", "Log file does not exist or has not been created yet.");
            AddTranslation("Dialog.LogExported", "日志已导出到:\n{0}", "Log exported to:\n{0}");
            AddTranslation("Dialog.LogExportFailed", "导出日志失败:\n{0}", "Log export failed:\n{0}");
            AddTranslation("Dialog.NoStepsSelected", "请至少勾选一个需要执行的步骤。", "Please select at least one step to execute.");
            AddTranslation("Dialog.SampleCopied", "已自动复制 Starter Assets Sample，稍后可重新执行操作。", 
                "Starter Assets Sample automatically copied, you can re-execute the operation later.");
            AddTranslation("Dialog.GitAssistantNotInstalled", "当前项目未安装版本控制助手，无法执行快速备份。", 
                "Version Control Assistant is not installed in the current project, cannot perform quick backup.");
            AddTranslation("Dialog.BackupFailedRetry", "{0}\n\n需要重试吗？", "{0}\n\nDo you want to retry?");
            AddTranslation("Dialog.CannotRollbackNoAssistant", "请先安装版本控制助手后再尝试回滚。", 
                "Please install Version Control Assistant before attempting rollback.");
            AddTranslation("Dialog.CannotRollbackNoBaseline", "当前会话未记录转换前的 Git 提交，无法执行自动回滚。", 
                "Current session has not recorded pre-conversion Git commit, cannot perform automatic rollback.");
            AddTranslation("Dialog.RollbackConfirm", "确认回滚", "Confirm Rollback");
            AddTranslation("Dialog.RollbackConfirmMessage", 
                "即将回滚到转换前的 Git 版本（提交：{0}）。\n\n此操作将丢弃转换后的所有更改，是否继续？",
                "About to rollback to pre-conversion Git version (commit: {0}).\n\nThis operation will discard all changes after conversion, continue?");
            AddTranslation("Dialog.PackageManagerManual", "请从菜单 Window/Package Manager 手动打开，然后导入所需 Sample。", 
                "Please manually open from menu Window/Package Manager, then import the required Sample.");
            AddTranslation("Dialog.ConversionSuccess.Message", 
                "🎉 VR 转换成功！\n\n" +
                "您的项目已成功转换为 VR 项目。\n\n" +
                "主要变更：\n" +
                "• XR Origin 已添加到场景\n" +
                "• 左右手控制器已配置\n" +
                "• 项目设置已更新为 VR 模式\n" +
                "• 手部追踪同步已优化\n\n",
                "🎉 VR Conversion Successful!\n\n" +
                "Your project has been successfully converted to a VR project.\n\n" +
                "Main Changes:\n" +
                "• XR Origin added to scene\n" +
                "• Left and right hand controllers configured\n" +
                "• Project settings updated to VR mode\n" +
                "• Hand tracking synchronization optimized\n\n");
            AddTranslation("Dialog.ConversionSuccess.Baseline", "已记录转换前的 Git 提交：{0}\n\n", 
                "Recorded pre-conversion Git commit: {0}\n\n");
            AddTranslation("Dialog.ConversionSuccess.Hint", "提示：可以使用窗口下方的 Git 按钮创建备份或回滚。", 
                "Tip: You can use the Git button below the window to create backup or rollback.");
            AddTranslation("Dialog.SampleImported", "{0} Sample 已导入，工具会在下一次执行时自动继续。", 
                "{0} Sample imported, the tool will automatically continue on next execution.");
            AddTranslation("Dialog.SampleImportFailed", "未能自动导入 {0} Sample，将尝试打开 Package Manager。", 
                "Failed to automatically import {0} Sample, will try to open Package Manager.");
            AddTranslation("Log.InputActionAssetBound", "已将\"XRI Default Input Actions\"绑定到 XR Input Action Manager，以自动启用输入映射。", 
                "Bound \"XRI Default Input Actions\" to XR Input Action Manager to automatically enable input mapping.");
            AddTranslation("Log.InputActionReferenceCreated", "已生成输入动作引用资产：{0}", 
                "Created input action reference asset: {0}");
            AddTranslation("Log.InputActionReferenceFailed", "无法创建输入动作引用（{0}/{1}）：请确认\"XRI Default Input Actions\"中包含该 Action Map。", 
                "Cannot create input action reference ({0}/{1}): Please confirm that \"XRI Default Input Actions\" contains this Action Map.");
            AddTranslation("Log.InputActionsNotFound", "未在项目中找到\"XRI Default Input Actions.inputactions\"。请在 Package Manager 中重新导入 XR Interaction Toolkit 的 Starter Assets。", 
                "XRI Default Input Actions.inputactions not found in project. Please reimport XR Interaction Toolkit Starter Assets from Package Manager.");
            AddTranslation("Log.XrGeneralSettingsAssetCreated", "已创建 XRGeneralSettingsPerBuildTarget 资产：{0}", 
                "Created XRGeneralSettingsPerBuildTarget asset: {0}");
            AddTranslation("Log.XrGeneralSettingsRegistered", "已注册 XR General Settings（{0}）。", 
                "Registered XR General Settings ({0}).");
            AddTranslation("Log.XrGeneralSettingsCreated", "已创建 {0} 的 XR General Settings。", 
                "Created XR General Settings for {0}.");
            AddTranslation("Log.XrManagerSettingsCreated", "已创建 {0} 对应的 XR Manager Settings。", 
                "Created XR Manager Settings for {0}.");
            AddTranslation("Log.DefaultObjectCreated", "已创建 {0}。", "Created {0}.");
            AddTranslation("Log.HandTrackingSyncAdded", "已为 {0} 添加手部追踪同步优化脚本。", 
                "Added hand tracking sync optimization script to {0}.");
            AddTranslation("Dialog.Tip", "提示", "Tip");
            AddTranslation("Dialog.SampleAutoCopied", "已自动复制 Starter Assets Sample，稍后可重新执行操作。", 
                "Starter Assets Sample has been automatically copied, you can re-execute the operation later.");
            AddTranslation("Log.SampleAutoImported", "已自动导入 {0} Sample。", 
                "Automatically imported {0} Sample.");
            AddTranslation("Dialog.PackageManagerOpened", "已打开 Package Manager", 
                "Package Manager Opened");
            AddTranslation("Log.PackageManagerOpened", "已打开 Package Manager。请手动选择 {0} 并导入 {1} Sample。", 
                "Package Manager opened. Please manually select {0} and import {1} Sample.");
            AddTranslation("Dialog.VersionControlNotInstalled", "当前项目未安装版本控制助手，无法执行快速备份。", 
                "Version control assistant is not installed in the current project, cannot perform quick backup.");
            AddTranslation("Dialog.BackupComplete", "备份完成", "Backup Complete");
            AddTranslation("Dialog.BackupFailed", "备份失败", "Backup Failed");
            AddTranslation("Dialog.CannotRollback", "无法回滚", "Cannot Rollback");
            AddTranslation("Dialog.RollbackComplete", "回滚完成", "Rollback Complete");
            AddTranslation("Log.RollbackComplete", "Git 回滚完成：{0}", "Git rollback complete: {0}");
            AddTranslation("Log.RollbackFailed", "回滚失败：{0}", "Rollback failed: {0}");
            AddTranslation("Log.GitCommitRecorded", "已记录当前 Git 提交 {0}，可在成功后回滚。", 
                "Recorded current Git commit {0}, can rollback after success.");
            AddTranslation("Log.GitCommitNotRecorded", "未能记录当前 Git 提交，可能尚未初始化仓库。", 
                "Failed to record current Git commit, repository may not be initialized.");
            AddTranslation("Log.GitCommitFetchFailed", "获取 Git 提交失败：{0}", 
                "Failed to fetch Git commit: {0}");
            AddTranslation("Log.PackageInstallRequested", "已向 Package Manager 提交安装请求：{0}", 
                "Submitted installation request to Package Manager: {0}");
            AddTranslation("Log.PackageInstallSuccess", "包安装成功：{0}", 
                "Package installation successful: {0}");
            AddTranslation("Log.PackageInstallFailed", "包安装失败：{0}", 
                "Package installation failed: {0}");
            AddTranslation("Log.PackageInstallStatus", "包安装状态：{0}", 
                "Package installation status: {0}");
            AddTranslation("Log.ManifestVersionRemoved", "移除了 manifest.json 中 {0} 的 \"latest\" 版本约束。", 
                "Removed \"latest\" version constraint for {0} in manifest.json.");
            AddTranslation("Dialog.RollbackNoBaseline", "当前会话未记录转换前的 Git 提交，无法执行自动回滚。", 
                "Current session did not record pre-conversion Git commit, cannot perform automatic rollback.");
            AddTranslation("Dialog.RollbackVersionControlRequired", "请先安装版本控制助手后再尝试回滚。", 
                "Please install version control assistant before attempting rollback.");
            AddTranslation("Dialog.RollbackRestored", "已恢复到转换前的 Git 版本。", 
                "Restored to pre-conversion Git version.");
            AddTranslation("Dialog.BackupRetry", "需要重试吗？", "Do you want to retry?");
            AddTranslation("Button.Retry", "重试", "Retry");
            AddTranslation("Button.Cancel", "取消", "Cancel");
            AddTranslation("Log.ActionMapNotSpecified", "<未指定 Action Map>", "<Action Map not specified>");
            AddTranslation("Dialog.ConfirmRollback", "确认回滚？", "Confirm Rollback?");
            AddTranslation("Dialog.RollbackWarning", "将使用 git reset --hard {0} 恢复到转换前的版本。\n\n该操作会丢弃当前所有未提交的改动，确定要继续吗？", 
                "Will use git reset --hard {0} to restore to pre-conversion version.\n\nThis operation will discard all current uncommitted changes, are you sure you want to continue?");
            AddTranslation("Button.ConfirmRollback", "确认回滚", "Confirm Rollback");
            AddTranslation("Dialog.BackupBeforeAction", "执行前请备份", "Backup Before Action");
            AddTranslation("Dialog.BackupBeforeActionMessage", "即将执行 {0}，该操作会修改 XR 依赖、项目设置以及当前场景。\n\n请确认你已经手动保存场景并完成一次备份或 Git 提交。", 
                "About to execute {0}, this operation will modify XR dependencies, project settings, and current scene.\n\nPlease confirm you have manually saved the scene and completed a backup or Git commit.");
            AddTranslation("Button.BackupConfirmed", "我已完成备份", "I Have Backed Up");
            AddTranslation("Dialog.BackupBeforeActionTitle", "执行前请先备份", "Backup Before Action");
            AddTranslation("Dialog.BackupBeforeActionMessage2", "执行 {0} 会批量修改 manifest、Project Settings 与当前场景。\n\n建议使用版本控制助手创建备份提交，或者确认已完成其他备份手段。", 
                "Executing {0} will batch modify manifest, Project Settings, and current scene.\n\nIt is recommended to use version control assistant to create backup commit, or confirm that other backup methods have been completed.");
            AddTranslation("Button.UseVersionControlBackup", "使用版本控制助手快速备份", "Use Version Control Assistant for Quick Backup");
            AddTranslation("Log.GitCommandFailed", "Git 命令执行失败，请查看控制台。", "Git command execution failed, please check console.");
        }

        private static void AddTranslation(string key, string chinese, string english, string japanese = null)
        {
            _translations[key] = new Dictionary<Language, string>
            {
                [Language.Chinese] = chinese,
                [Language.English] = english,
                [Language.Japanese] = japanese ?? english // 如果没有日文翻译，使用英文作为后备
            };
        }

        /// <summary>
        /// 获取所有支持的语言
        /// </summary>
        public static Language[] GetSupportedLanguages()
        {
            return new[] { Language.Chinese, Language.English, Language.Japanese };
        }

        /// <summary>
        /// 获取语言显示名称
        /// </summary>
        public static string GetLanguageDisplayName(Language language)
        {
            return language switch
            {
                Language.Chinese => "中文",
                Language.English => "English",
                Language.Japanese => "日本語",
                _ => language.ToString()
            };
        }
    }
}

