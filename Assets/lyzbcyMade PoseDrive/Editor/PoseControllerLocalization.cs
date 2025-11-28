using System;
using System.Collections.Generic;
using UnityEditor;

namespace PoseDrive.Editor
{
    internal enum PoseDriveLanguage
    {
        ChineseSimplified = 0,
        English = 1,
        Japanese = 2
    }

    internal readonly struct PoseLanguageInfo
    {
        public PoseDriveLanguage Language { get; }
        public string DisplayName { get; }

        public PoseLanguageInfo(PoseDriveLanguage language, string displayName)
        {
            Language = language;
            DisplayName = displayName;
        }
    }

    internal static class PoseDriveLocalization
    {
        private const string PrefKey = "PoseDrive.Language";
        private static readonly PoseDriveLanguage DefaultLanguage = PoseDriveLanguage.ChineseSimplified;

        private static readonly PoseLanguageInfo[] Languages =
        {
            new(PoseDriveLanguage.ChineseSimplified, "简体中文"),
            new(PoseDriveLanguage.English, "English"),
            new(PoseDriveLanguage.Japanese, "日本語")
        };

        private static readonly Dictionary<PoseDriveLanguage, Dictionary<string, string>> Tables =
            new()
            {
                {
                    PoseDriveLanguage.ChineseSimplified, new Dictionary<string, string>
                    {
                        ["menu.root"] = "Tools/PoseDrive",
                        ["menu.window"] = "Tools/PoseDrive/主面板",
                        ["menu.mapping"] = "Tools/PoseDrive/动作映射",
                        ["menu.wizard"] = "Tools/PoseDrive/配置向导",
                        ["menu.settings"] = "Tools/PoseDrive/设置",

                        ["header.title"] = "PoseDrive",
                        ["header.subtitle"] = "姿控驱动 - AI 体感输入系统",
                        ["button.help"] = "帮助",
                        ["button.setup"] = "配置向导",
                        ["button.settings"] = "设置",

                        ["module.status"] = "系统状态",
                        ["module.status.subtitle"] = "Component Status",
                        ["module.camera"] = "摄像头预览",
                        ["module.camera.subtitle"] = "Camera Preview",
                        ["module.skeleton"] = "骨架与关键点",
                        ["module.skeleton.subtitle"] = "Pose Skeleton",
                        ["module.action"] = "动作识别",
                        ["module.action.subtitle"] = "Gesture Recognition",
                        ["module.motion"] = "移动与视角",
                        ["module.motion.subtitle"] = "Movement & View Output",
                        ["module.mapping"] = "输入映射",
                        ["module.mapping.subtitle"] = "Gesture → Virtual Key",
                        ["module.debug"] = "配置与调试",
                        ["module.debug.subtitle"] = "Diagnostics & Shortcuts",

                        ["status.ready"] = "Ready",
                        ["status.missing"] = "Missing",
                        ["status.refresh"] = "重新查找组件",
                        ["camera.enable"] = "启用摄像头",
                        ["camera.skeleton"] = "显示骨架",
                        ["camera.noFrame"] = "Enter Play Mode 以查看画面",
                        ["camera.wait"] = "等待摄像头数据…",
                        ["camera.ready"] = "摄像头正常",
                        ["skeleton.none"] = "当前没有有效姿态数据。",
                        ["skeleton.count"] = "关键点数量",
                        ["skeleton.timestamp"] = "最后更新时间",
                        ["skeleton.confidence"] = "置信度热度图",
                        ["action.none"] = "无动作",
                        ["action.current"] = "当前动作",
                        ["action.confidence"] = "置信度",
                        ["motion.move"] = "移动向量",
                        ["motion.view"] = "视角增量",
                        ["motion.managerMissing"] = "未找到 PoseDriveManager。",
                        ["mapping.count"] = "映射数量",
                        ["mapping.none"] = "暂无映射，可通过映射编辑器添加。",
                        ["mapping.button"] = "打开映射编辑器",
                        ["mapping.hold"] = "保持",
                        ["mapping.once"] = "一次",
                        ["debug.log"] = "日志输出",
                        ["debug.detector"] = "检测器",
                        ["debug.manager"] = "管理器",
                        ["debug.fpsHint"] = "进入 Play Mode 可查看 FPS 与延迟。",
                        ["debug.panel"] = "控制面板",
                        ["debug.mapping"] = "映射编辑器",
                        ["debug.wizard"] = "配置向导",

                        ["wizard.title"] = "PoseDrive 快速配置",
                        ["wizard.subtitle"] = "按步骤创建运行时、绑定模型、设置映射并打开调试工具。",
                        ["wizard.step1"] = "1. 检查依赖",
                        ["wizard.step1.desc"] = "确保 Barracuda 与摄像头可用。",
                        ["wizard.step2"] = "2. 选择模型",
                        ["wizard.model.movenet"] = "MoveNet 模型",
                        ["wizard.model.action"] = "动作分类模型",
                        ["wizard.model.pullDetector"] = "从 PoseDetector 拉取",
                        ["wizard.model.pullClassifier"] = "从 ActionClassifier 拉取",
                        ["wizard.model.noBarracuda"] = "Barracuda 包未安装。请先在步骤 1 中安装 Barracuda 包。",
                        ["wizard.step3"] = "3. 输入映射",
                        ["wizard.step4"] = "4. 创建/更新运行时",
                        ["wizard.step5"] = "5. 快速入口",
                        ["wizard.binding.asset"] = "动作映射资产",
                        ["wizard.binding.default"] = "创建默认映射资产",
                        ["wizard.mapping.useExisting"] = "使用现有 PoseInputMapper 配置",
                        ["wizard.runtime.button"] = "创建 / 更新 PoseDrive System",
                        ["wizard.open.window"] = "打开控制面板",
                        ["wizard.open.mapping"] = "打开映射编辑器",
                        ["wizard.open.scene.tp"] = "打开 ThirdPerson 示例场景",
                        ["wizard.open.scene.fp"] = "打开 FirstPerson 示例场景",
                        ["wizard.dependency.barracuda.name"] = "Barracuda",
                        ["wizard.dependency.barracuda.tip"] = "神经网络推理依赖 Barracuda 包，缺失时将无法运行 MoveNet 与动作分类。",
                        ["wizard.dependency.camera.name"] = "摄像头",
                        ["wizard.dependency.camera.tip"] = "需要至少一个系统摄像头或虚拟摄像头。请检查系统权限或插入设备。",
                        ["wizard.dependency.barracuda.action"] = "前往 Package Manager",
                        ["wizard.dependency.barracuda.install"] = "自动安装 Barracuda",
                        ["wizard.dependency.barracuda.install.confirm"] = "是否要将 Barracuda 包添加到项目的 manifest.json 中？\n\n这将通过 GitHub 仓库链接自动安装 Barracuda。",
                        ["wizard.dependency.barracuda.install.success"] = "Barracuda 已添加到项目依赖中。\n\nUnity 将自动下载并导入该包，请稍候片刻。",
                        ["wizard.dependency.barracuda.install.error"] = "安装失败：{0}",
                        ["wizard.dependency.barracuda.install.already"] = "Barracuda 已存在于项目依赖中。",
                        ["wizard.dependency.camera.action"] = "查看帮助",

                        ["settings.title"] = "PoseDrive 设置",
                        ["settings.subtitle"] = "选择界面语言，调整偏好。",
                        ["settings.language"] = "界面语言",
                        ["settings.apply"] = "关闭",

                        ["mapping.editor.description"] = "为识别到的手势配置虚拟按键与触发模式。",
                        ["mapping.editor.asset"] = "映射资产",
                        ["mapping.editor.create"] = "新建资产",
                        ["mapping.editor.save"] = "保存",
                        ["mapping.editor.empty"] = "请选择或创建动作映射资产。",
                        ["mapping.binding.gesture"] = "手势名称",
                        ["mapping.binding.type"] = "输入类型",
                        ["mapping.binding.hold"] = "保持按下 (Hold)",
                        ["mapping.binding.holdHint"] = "选择后将在手势持续时一直保持按键。取消则只触发一次。",
                        ["mapping.binding.add"] = "+ 添加映射",
                        ["mapping.binding.delete"] = "删除",

                        ["dialog.sceneMissing.title"] = "场景不存在",
                        ["dialog.sceneMissing.message"] = "无法找到场景文件：\n{0}\n\n请确认场景文件是否存在。",
                        ["dialog.sceneMissing.ok"] = "确定"
                    }
                },
                {
                    PoseDriveLanguage.English, new Dictionary<string, string>
                    {
                        ["menu.root"] = "Tools/PoseDrive",
                        ["menu.window"] = "Tools/PoseDrive/Main Window",
                        ["menu.mapping"] = "Tools/PoseDrive/Action Mapping",
                        ["menu.wizard"] = "Tools/PoseDrive/Setup Wizard",
                        ["menu.settings"] = "Tools/PoseDrive/Settings",

                        ["header.title"] = "PoseDrive",
                        ["header.subtitle"] = "Pose Drive - AI-based Body Pose Input System",
                        ["button.help"] = "Help",
                        ["button.setup"] = "Setup Wizard",
                        ["button.settings"] = "Settings",

                        ["module.status"] = "Component Status",
                        ["module.status.subtitle"] = "Component Status",
                        ["module.camera"] = "Camera Preview",
                        ["module.camera.subtitle"] = "Camera Preview",
                        ["module.skeleton"] = "Pose Skeleton",
                        ["module.skeleton.subtitle"] = "Pose Skeleton",
                        ["module.action"] = "Gesture Recognition",
                        ["module.action.subtitle"] = "Gesture Recognition",
                        ["module.motion"] = "Movement & View",
                        ["module.motion.subtitle"] = "Movement & View Output",
                        ["module.mapping"] = "Input Mapping",
                        ["module.mapping.subtitle"] = "Gesture → Virtual Key",
                        ["module.debug"] = "Diagnostics",
                        ["module.debug.subtitle"] = "Diagnostics & Shortcuts",

                        ["status.ready"] = "Ready",
                        ["status.missing"] = "Missing",
                        ["status.refresh"] = "Re-scan Components",
                        ["camera.enable"] = "Enable Camera",
                        ["camera.skeleton"] = "Show Skeleton",
                        ["camera.noFrame"] = "Enter Play Mode to preview",
                        ["camera.wait"] = "Waiting for frames…",
                        ["camera.ready"] = "Camera ready",
                        ["skeleton.none"] = "No valid pose detected.",
                        ["skeleton.count"] = "Keypoints",
                        ["skeleton.timestamp"] = "Last Updated",
                        ["skeleton.confidence"] = "Confidence Heatmap",
                        ["action.none"] = "No Action",
                        ["action.current"] = "Current Action",
                        ["action.confidence"] = "Confidence",
                        ["motion.move"] = "Move Vector",
                        ["motion.view"] = "View Delta",
                        ["motion.managerMissing"] = "PoseDriveManager not found.",
                        ["mapping.count"] = "Mapping Count",
                        ["mapping.none"] = "No mapping configured. Open the editor to add entries.",
                        ["mapping.button"] = "Open Mapping Editor",
                        ["mapping.hold"] = "Hold",
                        ["mapping.once"] = "Once",
                        ["debug.log"] = "Logging",
                        ["debug.detector"] = "Detector",
                        ["debug.manager"] = "Manager",
                        ["debug.fpsHint"] = "Enter Play Mode to inspect FPS & latency.",
                        ["debug.panel"] = "Control Panel",
                        ["debug.mapping"] = "Mapping Editor",
                        ["debug.wizard"] = "Setup Wizard",

                        ["wizard.title"] = "PoseDrive Setup Assistant",
                        ["wizard.subtitle"] = "Create runtime objects, bind models and mappings step-by-step.",
                        ["wizard.step1"] = "1. Verify Dependencies",
                        ["wizard.step1.desc"] = "Ensure Barracuda and camera access are available.",
                        ["wizard.step2"] = "2. Select Models",
                        ["wizard.model.movenet"] = "MoveNet Model",
                        ["wizard.model.action"] = "Action Classifier Model",
                        ["wizard.model.pullDetector"] = "Pull from PoseDetector",
                        ["wizard.model.pullClassifier"] = "Pull from ActionClassifier",
                        ["wizard.model.noBarracuda"] = "Barracuda package is not installed. Please install Barracuda in Step 1 first.",
                        ["wizard.step3"] = "3. Input Mapping",
                        ["wizard.step4"] = "4. Create/Update Runtime",
                        ["wizard.step5"] = "5. Quick Shortcuts",
                        ["wizard.binding.asset"] = "Action Mapping Asset",
                        ["wizard.binding.default"] = "Create Default Asset",
                        ["wizard.mapping.useExisting"] = "Use current PoseInputMapper",
                        ["wizard.runtime.button"] = "Create / Update PoseController System",
                        ["wizard.open.window"] = "Open Control Panel",
                        ["wizard.open.mapping"] = "Open Mapping Editor",
                        ["wizard.open.scene.tp"] = "Open ThirdPerson Sample Scene",
                        ["wizard.open.scene.fp"] = "Open FirstPerson Sample Scene",
                        ["wizard.dependency.barracuda.name"] = "Barracuda",
                        ["wizard.dependency.barracuda.tip"] = "Barracuda package is required for MoveNet and action classifier.",
                        ["wizard.dependency.camera.name"] = "Camera",
                        ["wizard.dependency.camera.tip"] = "At least one camera device (or virtual camera) is required.",
                        ["wizard.dependency.barracuda.action"] = "Open Package Manager",
                        ["wizard.dependency.barracuda.install"] = "Install Barracuda",
                        ["wizard.dependency.barracuda.install.confirm"] = "Add Barracuda package to your project's manifest.json?\n\nThis will automatically install Barracuda via GitHub repository link.",
                        ["wizard.dependency.barracuda.install.success"] = "Barracuda has been added to project dependencies.\n\nUnity will automatically download and import the package. Please wait a moment.",
                        ["wizard.dependency.barracuda.install.error"] = "Installation failed: {0}",
                        ["wizard.dependency.barracuda.install.already"] = "Barracuda already exists in project dependencies.",
                        ["wizard.dependency.camera.action"] = "View Help",

                        ["settings.title"] = "PoseDrive Settings",
                        ["settings.subtitle"] = "Change interface language and preferences.",
                        ["settings.language"] = "Interface Language",
                        ["settings.apply"] = "Close",

                        ["mapping.editor.description"] = "Configure gesture bindings and trigger mode.",
                        ["mapping.editor.asset"] = "Mapping Asset",
                        ["mapping.editor.create"] = "Create Asset",
                        ["mapping.editor.save"] = "Save",
                        ["mapping.editor.empty"] = "Select or create a mapping asset.",
                        ["mapping.binding.gesture"] = "Gesture Name",
                        ["mapping.binding.type"] = "Input Type",
                        ["mapping.binding.hold"] = "Hold (keep pressed)",
                        ["mapping.binding.holdHint"] = "When enabled, the key remains pressed until explicit release.",
                        ["mapping.binding.add"] = "+ Add Mapping",
                        ["mapping.binding.delete"] = "Delete",

                        ["dialog.sceneMissing.title"] = "Scene Not Found",
                        ["dialog.sceneMissing.message"] = "Cannot find scene file:\n{0}\n\nPlease verify that the scene file exists.",
                        ["dialog.sceneMissing.ok"] = "OK"
                    }
                },
                {
                    PoseDriveLanguage.Japanese, new Dictionary<string, string>
                    {
                        ["menu.root"] = "Tools/PoseDrive",
                        ["menu.window"] = "Tools/PoseDrive/メインウィンドウ",
                        ["menu.mapping"] = "Tools/PoseDrive/アクションマッピング",
                        ["menu.wizard"] = "Tools/PoseDrive/セットアップウィザード",
                        ["menu.settings"] = "Tools/PoseDrive/設定",

                        ["header.title"] = "PoseDrive",
                        ["header.subtitle"] = "ポーズドライブ - AI ベースの体感入力システム",
                        ["button.help"] = "ヘルプ",
                        ["button.setup"] = "セットアップ",
                        ["button.settings"] = "設定",

                        ["module.status"] = "コンポーネント状況",
                        ["module.status.subtitle"] = "Component Status",
                        ["module.camera"] = "カメラプレビュー",
                        ["module.camera.subtitle"] = "Camera Preview",
                        ["module.skeleton"] = "骨格とキーポイント",
                        ["module.skeleton.subtitle"] = "Pose Skeleton",
                        ["module.action"] = "ジェスチャー認識",
                        ["module.action.subtitle"] = "Gesture Recognition",
                        ["module.motion"] = "移動と視点",
                        ["module.motion.subtitle"] = "Movement & View Output",
                        ["module.mapping"] = "入力マッピング",
                        ["module.mapping.subtitle"] = "Gesture → Virtual Key",
                        ["module.debug"] = "診断",
                        ["module.debug.subtitle"] = "Diagnostics & Shortcuts",

                        ["status.ready"] = "準備完了",
                        ["status.missing"] = "未検出",
                        ["status.refresh"] = "コンポーネント再検索",
                        ["camera.enable"] = "カメラを有効化",
                        ["camera.skeleton"] = "骨格を表示",
                        ["camera.noFrame"] = "プレビュ―には Play Mode が必要です",
                        ["camera.wait"] = "カメラを待機中…",
                        ["camera.ready"] = "カメラ正常",
                        ["skeleton.none"] = "有効な姿勢データがありません。",
                        ["skeleton.count"] = "キーポイント数",
                        ["skeleton.timestamp"] = "最終更新",
                        ["skeleton.confidence"] = "信頼度ヒートマップ",
                        ["action.none"] = "動作なし",
                        ["action.current"] = "現在の動作",
                        ["action.confidence"] = "信頼度",
                        ["motion.move"] = "移動ベクトル",
                        ["motion.view"] = "視点変化",
                        ["motion.managerMissing"] = "PoseDriveManager が見つかりません。",
                        ["mapping.count"] = "マッピング数",
                        ["mapping.none"] = "マッピングがありません。エディターで追加してください。",
                        ["mapping.button"] = "マッピングエディターを開く",
                        ["mapping.hold"] = "ホールド",
                        ["mapping.once"] = "一回",
                        ["debug.log"] = "ログ出力",
                        ["debug.detector"] = "検出器",
                        ["debug.manager"] = "マネージャー",
                        ["debug.fpsHint"] = "Play Mode で FPS と遅延を確認できます。",
                        ["debug.panel"] = "コントロールパネル",
                        ["debug.mapping"] = "マッピングエディター",
                        ["debug.wizard"] = "セットアップ",

                        ["wizard.title"] = "PoseDrive セットアップウィザード",
                        ["wizard.subtitle"] = "ランタイム構築とモデル／マッピングの設定をガイドします。",
                        ["wizard.step1"] = "1. 依存関係を確認",
                        ["wizard.step1.desc"] = "Barracuda とカメラが利用可能か確認します。",
                        ["wizard.step2"] = "2. モデル選択",
                        ["wizard.model.movenet"] = "MoveNet モデル",
                        ["wizard.model.action"] = "アクション分類モデル",
                        ["wizard.model.pullDetector"] = "PoseDetector から取得",
                        ["wizard.model.pullClassifier"] = "ActionClassifier から取得",
                        ["wizard.model.noBarracuda"] = "Barracuda パッケージがインストールされていません。まずステップ 1 で Barracuda をインストールしてください。",
                        ["wizard.step3"] = "3. マッピング",
                        ["wizard.step4"] = "4. ランタイム作成／更新",
                        ["wizard.step5"] = "5. ショートカット",
                        ["wizard.binding.asset"] = "アクションマッピングアセット",
                        ["wizard.binding.default"] = "デフォルトアセットを作成",
                        ["wizard.mapping.useExisting"] = "既存の PoseInputMapper を使用",
                        ["wizard.runtime.button"] = "PoseController System を作成／更新",
                        ["wizard.open.window"] = "コントロールパネルを開く",
                        ["wizard.open.mapping"] = "マッピングエディターを開く",
                        ["wizard.open.scene.tp"] = "ThirdPerson サンプルを開く",
                        ["wizard.open.scene.fp"] = "FirstPerson サンプルを開く",
                        ["wizard.dependency.barracuda.name"] = "Barracuda",
                        ["wizard.dependency.barracuda.tip"] = "MoveNet とアクション分類には Barracuda パッケージが必要です。",
                        ["wizard.dependency.camera.name"] = "カメラ",
                        ["wizard.dependency.camera.tip"] = "少なくとも 1 台のカメラデバイスが必要です。",
                        ["wizard.dependency.barracuda.action"] = "Package Manager を開く",
                        ["wizard.dependency.barracuda.install"] = "Barracuda を自動インストール",
                        ["wizard.dependency.barracuda.install.confirm"] = "Barracuda パッケージをプロジェクトの manifest.json に追加しますか？\n\nGitHub リポジトリリンク経由で Barracuda を自動インストールします。",
                        ["wizard.dependency.barracuda.install.success"] = "Barracuda がプロジェクトの依存関係に追加されました。\n\nUnity が自動的にパッケージをダウンロードしてインポートします。しばらくお待ちください。",
                        ["wizard.dependency.barracuda.install.error"] = "インストールに失敗しました：{0}",
                        ["wizard.dependency.barracuda.install.already"] = "Barracuda は既にプロジェクトの依存関係に存在します。",
                        ["wizard.dependency.camera.action"] = "ヘルプを見る",

                        ["settings.title"] = "PoseDrive 設定",
                        ["settings.subtitle"] = "インターフェース言語と設定を変更します。",
                        ["settings.language"] = "インターフェース言語",
                        ["settings.apply"] = "閉じる",

                        ["mapping.editor.description"] = "認識されたジェスチャーを仮想キーに割り当てます。",
                        ["mapping.editor.asset"] = "マッピングアセット",
                        ["mapping.editor.create"] = "新規アセット",
                        ["mapping.editor.save"] = "保存",
                        ["mapping.editor.empty"] = "マッピングアセットを選択または作成してください。",
                        ["mapping.binding.gesture"] = "ジェスチャー名",
                        ["mapping.binding.type"] = "入力タイプ",
                        ["mapping.binding.hold"] = "ホールド (押しっぱなし)",
                        ["mapping.binding.holdHint"] = "有効化するとキーが保持され、解除するまで押しっぱなしになります。",
                        ["mapping.binding.add"] = "+ マッピングを追加",
                        ["mapping.binding.delete"] = "削除",

                        ["dialog.sceneMissing.title"] = "シーンが見つかりません",
                        ["dialog.sceneMissing.message"] = "シーンファイルが見つかりません：\n{0}\n\nシーンファイルが存在することを確認してください。",
                        ["dialog.sceneMissing.ok"] = "OK"
                    }
                }
            };

        private static PoseDriveLanguage _currentLanguage;

        public static event Action LanguageChanged;

        public static PoseDriveLanguage CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage == value)
                {
                    return;
                }

                _currentLanguage = value;
                EditorPrefs.SetInt(PrefKey, (int)_currentLanguage);
                LanguageChanged?.Invoke();
            }
        }

        public static IReadOnlyList<PoseLanguageInfo> AvailableLanguages => Languages;

        static PoseDriveLocalization()
        {
            PoseDriveLanguage saved =
                (PoseDriveLanguage)EditorPrefs.GetInt(PrefKey, (int)DefaultLanguage);
            _currentLanguage = Tables.ContainsKey(saved) ? saved : DefaultLanguage;
        }

        public static string Tr(string key)
        {
            if (Tables.TryGetValue(CurrentLanguage, out Dictionary<string, string> table) &&
                table.TryGetValue(key, out string value))
            {
                return value;
            }

            if (Tables[DefaultLanguage].TryGetValue(key, out string fallback))
            {
                return fallback;
            }

            return key;
        }
    }
}

