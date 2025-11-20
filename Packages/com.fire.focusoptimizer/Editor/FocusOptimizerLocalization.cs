using System;
using System.Collections.Generic;
using UnityEditor;

namespace FireTools.FocusOptimizer
{
    internal enum FocusOptimizerLanguage
    {
        ChineseSimplified = 0,
        English = 1,
        Japanese = 2
    }

    internal readonly struct LanguageInfo
    {
        public FocusOptimizerLanguage Language { get; }
        public string DisplayName { get; }

        public LanguageInfo(FocusOptimizerLanguage language, string displayName)
        {
            Language = language;
            DisplayName = displayName;
        }
    }

    internal static class FocusOptimizerLocalization
    {
        private const string PrefKey = "FocusOptimizer.Language";
        private static readonly FocusOptimizerLanguage DefaultLanguage = FocusOptimizerLanguage.ChineseSimplified;

        private static readonly LanguageInfo[] LanguageInfos =
        {
            new(FocusOptimizerLanguage.ChineseSimplified, "简体中文"),
            new(FocusOptimizerLanguage.English, "English"),
            new(FocusOptimizerLanguage.Japanese, "日本語")
        };

        private static readonly Dictionary<FocusOptimizerLanguage, Dictionary<string, string>> Tables =
            new()
            {
                {
                    FocusOptimizerLanguage.ChineseSimplified, new Dictionary<string, string>
                    {
                        ["window.title"] = "焦点卡顿优化助手",
                        ["window.subtitle"] = "减少 Unity 编辑器重新获得焦点时的刷新卡顿",
                        
                        // 工具栏
                        ["toolbar.help"] = "帮助",
                        ["toolbar.help.tooltip"] = "打开帮助文档页面",
                        ["toolbar.settings"] = "设置",
                        ["toolbar.settings.tooltip"] = "打开设置窗口，调整语言和偏好选项",
                        
                        // 刷新策略
                        ["refresh.header"] = "刷新策略",
                        ["refresh.header.tooltip"] = "控制 Unity 编辑器在重新获得焦点后的资源刷新方式。选择合适的策略可以显著减少卡顿。",
                        ["refresh.enable"] = "启用焦点卡顿优化助手",
                        ["refresh.enable.tooltip"] = "开启后，插件将监控 Unity 编辑器的焦点变化，并按照设定的策略控制资源刷新。",
                        ["refresh.suspend"] = "失焦时暂停自动刷新",
                        ["refresh.suspend.tooltip"] = "当 Unity 失去焦点时，自动暂停资源刷新。这样可以避免在外部编辑器中修改代码时触发不必要的刷新。",
                        ["refresh.console"] = "在控制台输出状态提示",
                        ["refresh.console.tooltip"] = "在 Unity 控制台中显示刷新操作的详细信息，方便调试和监控。",
                        ["refresh.mode"] = "焦点回归策略",
                        ["refresh.mode.tooltip"] = "选择当 Unity 重新获得焦点时的刷新策略：\n• 立即刷新：立即执行刷新，适合小项目\n• 限流刷新：延迟一段时间后刷新，减少卡顿\n• 手动刷新：不自动刷新，需要手动触发",
                        ["refresh.mode.immediate"] = "立即刷新",
                        ["refresh.mode.throttled"] = "限流刷新",
                        ["refresh.mode.manual"] = "手动刷新",
                        ["refresh.throttle"] = "延迟秒数",
                        ["refresh.throttle.tooltip"] = "限流刷新模式下，Unity 重新获得焦点后等待多长时间再执行刷新。建议值：1-3 秒。",
                        ["refresh.prompt"] = "手动模式提示",
                        ["refresh.prompt.tooltip"] = "在手动刷新模式下，当检测到脚本更新时是否显示提示通知。",
                        
                        // 按钮
                        ["button.refresh"] = "立即刷新",
                        ["button.refresh.tooltip"] = "立即执行资源刷新操作，无论当前策略如何。",
                        ["button.restore"] = "重新允许自动刷新",
                        ["button.restore.tooltip"] = "如果自动刷新被暂停，点击此按钮可以立即恢复自动刷新功能。",
                        
                        // 状态信息
                        ["status.header"] = "刷新状态",
                        ["status.suspended"] = "已暂停",
                        ["status.normal"] = "正常",
                        ["status.scheduled"] = "自动刷新状态：{0}，预计 {1:0.0} 秒后刷新。",
                        ["status.current"] = "自动刷新状态：{0}",
                        
                        // Enter Play Mode
                        ["playmode.header"] = "Enter Play Mode 优化",
                        ["playmode.header.tooltip"] = "可选地自动切换 Enter Play Mode Options，以加快进入播放模式的速度。注意：禁用域重载可能影响某些脚本的初始化。",
                        ["playmode.enable"] = "启用 Enter Play Mode 辅助",
                        ["playmode.enable.tooltip"] = "开启后，插件会在进入播放模式时自动应用预设的选项，退出时恢复原设置。",
                        ["playmode.apply"] = "进入 Play Mode 时应用预设",
                        ["playmode.apply.tooltip"] = "当进入播放模式时，自动应用下方配置的选项。",
                        ["playmode.options"] = "预设选项",
                        ["playmode.options.tooltip"] = "选择要应用的 Enter Play Mode Options：\n• 禁用域重载：跳过脚本重新编译，加快启动速度\n• 禁用场景重载：保持当前场景状态，更快进入播放模式",
                        ["playmode.restore"] = "停用时自动恢复原设置",
                        ["playmode.restore.tooltip"] = "当禁用辅助功能时，自动恢复 Unity 的原始 Enter Play Mode 设置。",
                        ["playmode.button"] = "立即恢复原 Play Mode 设置",
                        ["playmode.button.tooltip"] = "立即恢复 Unity 的原始 Enter Play Mode 设置，无论当前配置如何。",
                        
                        // 统计信息
                        ["stats.header"] = "刷新统计",
                        ["stats.header.tooltip"] = "显示最近一次资源刷新的详细信息，帮助了解刷新性能。",
                        ["stats.reason"] = "最近刷新原因",
                        ["stats.reason.tooltip"] = "显示触发最近一次刷新的原因，例如：立即刷新、限流刷新、手动刷新等。",
                        ["stats.duration"] = "最近耗时",
                        ["stats.duration.tooltip"] = "显示最近一次资源刷新操作所花费的时间（秒）。数值越小表示性能越好。",
                        
                        // 设置窗口
                        ["settings.title"] = "设置",
                        ["settings.title.tooltip"] = "调整界面语言和其他偏好选项",
                        ["settings.language"] = "界面语言",
                        ["settings.language.tooltip"] = "选择界面显示的语言。更改后需要重新打开窗口才能生效。",
                        ["settings.close"] = "关闭",
                        
                        // 通知
                        ["notify.manualRefresh"] = "检测到脚本更新，请手动刷新",
                        ["notify.refreshComplete"] = "刷新完成",
                    }
                },
                {
                    FocusOptimizerLanguage.English, new Dictionary<string, string>
                    {
                        ["window.title"] = "Focus Lag Optimizer Assistant",
                        ["window.subtitle"] = "Reduce refresh lag when Unity regains focus",
                        
                        // Toolbar
                        ["toolbar.help"] = "Help",
                        ["toolbar.help.tooltip"] = "Open help documentation page",
                        ["toolbar.settings"] = "Settings",
                        ["toolbar.settings.tooltip"] = "Open settings window to adjust language and preferences",
                        
                        // Refresh Strategy
                        ["refresh.header"] = "Refresh Strategy",
                        ["refresh.header.tooltip"] = "Controls how Unity refreshes assets when the editor regains focus. Choosing the right strategy can significantly reduce lag.",
                        ["refresh.enable"] = "Enable Focus Lag Optimizer Assistant",
                        ["refresh.enable.tooltip"] = "When enabled, the plugin monitors Unity's focus changes and controls asset refresh according to the configured strategy.",
                        ["refresh.suspend"] = "Suspend auto-refresh when inactive",
                        ["refresh.suspend.tooltip"] = "Automatically pause asset refresh when Unity loses focus. This prevents unnecessary refreshes while editing code in external editors.",
                        ["refresh.console"] = "Show status hints in Console",
                        ["refresh.console.tooltip"] = "Display detailed refresh operation information in the Unity Console for debugging and monitoring.",
                        ["refresh.mode"] = "Focus Return Strategy",
                        ["refresh.mode.tooltip"] = "Choose the refresh strategy when Unity regains focus:\n• Immediate: Refresh immediately, suitable for small projects\n• Throttled: Refresh after a delay to reduce lag\n• Manual: No automatic refresh, requires manual trigger",
                        ["refresh.mode.immediate"] = "Immediate",
                        ["refresh.mode.throttled"] = "Throttled",
                        ["refresh.mode.manual"] = "Manual",
                        ["refresh.throttle"] = "Delay (seconds)",
                        ["refresh.throttle.tooltip"] = "In throttled mode, how long to wait after Unity regains focus before refreshing. Recommended: 1-3 seconds.",
                        ["refresh.prompt"] = "Manual mode prompt",
                        ["refresh.prompt.tooltip"] = "Whether to show a notification when script changes are detected in manual refresh mode.",
                        
                        // Buttons
                        ["button.refresh"] = "Refresh Now",
                        ["button.refresh.tooltip"] = "Immediately execute asset refresh, regardless of current strategy.",
                        ["button.restore"] = "Restore Auto-Refresh",
                        ["button.restore.tooltip"] = "If auto-refresh is suspended, click this button to immediately restore auto-refresh functionality.",
                        
                        // Status
                        ["status.header"] = "Refresh Status",
                        ["status.suspended"] = "Suspended",
                        ["status.normal"] = "Normal",
                        ["status.scheduled"] = "Auto-refresh status: {0}, scheduled in {1:0.0} seconds.",
                        ["status.current"] = "Auto-refresh status: {0}",
                        
                        // Enter Play Mode
                        ["playmode.header"] = "Enter Play Mode Optimization",
                        ["playmode.header.tooltip"] = "Optionally auto-switch Enter Play Mode Options to speed up entering play mode. Note: Disabling domain reload may affect some script initialization.",
                        ["playmode.enable"] = "Enable Enter Play Mode Helper",
                        ["playmode.enable.tooltip"] = "When enabled, the plugin automatically applies preset options when entering play mode and restores original settings when exiting.",
                        ["playmode.apply"] = "Apply preset when entering Play Mode",
                        ["playmode.apply.tooltip"] = "Automatically apply the configured options below when entering play mode.",
                        ["playmode.options"] = "Preset Options",
                        ["playmode.options.tooltip"] = "Select Enter Play Mode Options to apply:\n• Disable Domain Reload: Skip script recompilation for faster startup\n• Disable Scene Reload: Keep current scene state for faster play mode entry",
                        ["playmode.restore"] = "Auto-restore original settings when disabled",
                        ["playmode.restore.tooltip"] = "When the helper is disabled, automatically restore Unity's original Enter Play Mode settings.",
                        ["playmode.button"] = "Restore Original Play Mode Settings",
                        ["playmode.button.tooltip"] = "Immediately restore Unity's original Enter Play Mode settings, regardless of current configuration.",
                        
                        // Statistics
                        ["stats.header"] = "Refresh Statistics",
                        ["stats.header.tooltip"] = "Display detailed information about the most recent asset refresh to help understand refresh performance.",
                        ["stats.reason"] = "Last Refresh Reason",
                        ["stats.reason.tooltip"] = "Shows the reason that triggered the most recent refresh, such as: immediate refresh, throttled refresh, manual refresh, etc.",
                        ["stats.duration"] = "Last Duration",
                        ["stats.duration.tooltip"] = "Shows the time (in seconds) taken by the most recent asset refresh operation. Lower values indicate better performance.",
                        
                        // Settings Window
                        ["settings.title"] = "Settings",
                        ["settings.title.tooltip"] = "Adjust interface language and other preferences",
                        ["settings.language"] = "Interface Language",
                        ["settings.language.tooltip"] = "Select the language for the interface. Changes take effect after reopening the window.",
                        ["settings.close"] = "Close",
                        
                        // Notifications
                        ["notify.manualRefresh"] = "Script changes detected, please refresh manually",
                        ["notify.refreshComplete"] = "Refresh completed",
                    }
                },
                {
                    FocusOptimizerLanguage.Japanese, new Dictionary<string, string>
                    {
                        ["window.title"] = "フォーカス遅延最適化アシスタント",
                        ["window.subtitle"] = "Unity がフォーカスを再取得する際のリフレッシュ遅延を軽減",
                        
                        // ツールバー
                        ["toolbar.help"] = "ヘルプ",
                        ["toolbar.help.tooltip"] = "ヘルプドキュメントページを開く",
                        ["toolbar.settings"] = "設定",
                        ["toolbar.settings.tooltip"] = "設定ウィンドウを開いて言語と設定を調整",
                        
                        // リフレッシュ戦略
                        ["refresh.header"] = "リフレッシュ戦略",
                        ["refresh.header.tooltip"] = "Unity エディターがフォーカスを再取得した際のアセットリフレッシュ方法を制御します。適切な戦略を選択することで、遅延を大幅に削減できます。",
                        ["refresh.enable"] = "フォーカス遅延最適化アシスタントを有効化",
                        ["refresh.enable.tooltip"] = "有効にすると、プラグインは Unity のフォーカス変化を監視し、設定された戦略に従ってアセットリフレッシュを制御します。",
                        ["refresh.suspend"] = "非アクティブ時に自動リフレッシュを一時停止",
                        ["refresh.suspend.tooltip"] = "Unity がフォーカスを失ったときに、自動的にアセットリフレッシュを一時停止します。これにより、外部エディターでコードを編集している際の不要なリフレッシュを防ぎます。",
                        ["refresh.console"] = "コンソールにステータスヒントを表示",
                        ["refresh.console.tooltip"] = "デバッグと監視のために、Unity コンソールにリフレッシュ操作の詳細情報を表示します。",
                        ["refresh.mode"] = "フォーカス復帰戦略",
                        ["refresh.mode.tooltip"] = "Unity がフォーカスを再取得した際のリフレッシュ戦略を選択：\n• 即座：即座にリフレッシュ、小規模プロジェクトに適しています\n• スロットル：遅延後にリフレッシュ、遅延を軽減\n• 手動：自動リフレッシュなし、手動でトリガーが必要",
                        ["refresh.mode.immediate"] = "即座",
                        ["refresh.mode.throttled"] = "スロットル",
                        ["refresh.mode.manual"] = "手動",
                        ["refresh.throttle"] = "遅延（秒）",
                        ["refresh.throttle.tooltip"] = "スロットルモードで、Unity がフォーカスを再取得してからリフレッシュするまでの待機時間。推奨値：1-3 秒。",
                        ["refresh.prompt"] = "手動モードのプロンプト",
                        ["refresh.prompt.tooltip"] = "手動リフレッシュモードで、スクリプト変更が検出されたときに通知を表示するかどうか。",
                        
                        // ボタン
                        ["button.refresh"] = "今すぐリフレッシュ",
                        ["button.refresh.tooltip"] = "現在の戦略に関係なく、アセットリフレッシュを即座に実行します。",
                        ["button.restore"] = "自動リフレッシュを復元",
                        ["button.restore.tooltip"] = "自動リフレッシュが一時停止されている場合、このボタンをクリックして自動リフレッシュ機能を即座に復元します。",
                        
                        // ステータス
                        ["status.header"] = "リフレッシュステータス",
                        ["status.suspended"] = "一時停止中",
                        ["status.normal"] = "正常",
                        ["status.scheduled"] = "自動リフレッシュステータス：{0}、{1:0.0} 秒後に予定。",
                        ["status.current"] = "自動リフレッシュステータス：{0}",
                        
                        // Enter Play Mode
                        ["playmode.header"] = "Enter Play Mode 最適化",
                        ["playmode.header.tooltip"] = "Enter Play Mode Options を自動的に切り替えて、プレイモードへの移行を高速化します。注意：ドメインリロードを無効にすると、一部のスクリプトの初期化に影響する可能性があります。",
                        ["playmode.enable"] = "Enter Play Mode ヘルパーを有効化",
                        ["playmode.enable.tooltip"] = "有効にすると、プラグインはプレイモードに入るときに自動的にプリセットオプションを適用し、終了時に元の設定を復元します。",
                        ["playmode.apply"] = "プレイモードに入るときにプリセットを適用",
                        ["playmode.apply.tooltip"] = "プレイモードに入るときに、下記の設定オプションを自動的に適用します。",
                        ["playmode.options"] = "プリセットオプション",
                        ["playmode.options.tooltip"] = "適用する Enter Play Mode Options を選択：\n• ドメインリロードを無効化：スクリプトの再コンパイルをスキップして起動を高速化\n• シーンリロードを無効化：現在のシーン状態を保持してプレイモードへの移行を高速化",
                        ["playmode.restore"] = "無効化時に元の設定を自動復元",
                        ["playmode.restore.tooltip"] = "ヘルパーが無効化されたとき、Unity の元の Enter Play Mode 設定を自動的に復元します。",
                        ["playmode.button"] = "元の Play Mode 設定を復元",
                        ["playmode.button.tooltip"] = "現在の設定に関係なく、Unity の元の Enter Play Mode 設定を即座に復元します。",
                        
                        // 統計情報
                        ["stats.header"] = "リフレッシュ統計",
                        ["stats.header.tooltip"] = "最新のアセットリフレッシュの詳細情報を表示し、リフレッシュパフォーマンスを理解するのに役立ちます。",
                        ["stats.reason"] = "最後のリフレッシュ理由",
                        ["stats.reason.tooltip"] = "最新のリフレッシュをトリガーした理由を表示します。例：即座リフレッシュ、スロットルリフレッシュ、手動リフレッシュなど。",
                        ["stats.duration"] = "最後の所要時間",
                        ["stats.duration.tooltip"] = "最新のアセットリフレッシュ操作にかかった時間（秒）を表示します。値が小さいほどパフォーマンスが良いことを示します。",
                        
                        // 設定ウィンドウ
                        ["settings.title"] = "設定",
                        ["settings.title.tooltip"] = "インターフェース言語とその他の設定を調整",
                        ["settings.language"] = "インターフェース言語",
                        ["settings.language.tooltip"] = "インターフェースの表示言語を選択します。変更はウィンドウを再開した後に有効になります。",
                        ["settings.close"] = "閉じる",
                        
                        // 通知
                        ["notify.manualRefresh"] = "スクリプト変更が検出されました。手動でリフレッシュしてください",
                        ["notify.refreshComplete"] = "リフレッシュ完了",
                    }
                }
            };

        private static FocusOptimizerLanguage _currentLanguage;

        public static event Action LanguageChanged;

        public static FocusOptimizerLanguage CurrentLanguage
        {
            get => _currentLanguage;
            private set
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

        public static IReadOnlyList<LanguageInfo> AvailableLanguages => LanguageInfos;

        static FocusOptimizerLocalization()
        {
            var saved = (FocusOptimizerLanguage)EditorPrefs.GetInt(PrefKey, (int)DefaultLanguage);
            _currentLanguage = Tables.ContainsKey(saved) ? saved : DefaultLanguage;
        }

        public static void SetLanguage(FocusOptimizerLanguage language)
        {
            if (!Tables.ContainsKey(language))
            {
                language = DefaultLanguage;
            }

            CurrentLanguage = language;
        }

        public static string Tr(string key)
        {
            if (Tables.TryGetValue(CurrentLanguage, out var table) && table.TryGetValue(key, out var value))
            {
                return value;
            }

            if (Tables[DefaultLanguage].TryGetValue(key, out var fallback))
            {
                return fallback;
            }

            return key;
        }

        public static string Tr(string key, params object[] args)
        {
            var format = Tr(key);
            return string.Format(format, args);
        }
    }
}

