using System;
using System.Collections.Generic;
using UnityEditor;

namespace Fire.VersionControlAssistant
{
    internal enum GitLanguage
    {
        ChineseSimplified = 0,
        English = 1,
        Japanese = 2
    }

    internal readonly struct LanguageInfo
    {
        public GitLanguage Language { get; }
        public string DisplayName { get; }

        public LanguageInfo(GitLanguage language, string displayName)
        {
            Language = language;
            DisplayName = displayName;
        }
    }

    internal static class GitLocalization
    {
        private const string PrefKey = "VersionControlAssistant.Language";
        private static readonly GitLanguage DefaultLanguage = GitLanguage.ChineseSimplified;

        private static readonly LanguageInfo[] LanguageInfos =
        {
            new(GitLanguage.ChineseSimplified, "简体中文"),
            new(GitLanguage.English, "English"),
            new(GitLanguage.Japanese, "日本語")
        };

        private static readonly Dictionary<GitLanguage, Dictionary<string, string>> Tables =
            new(GitLanguageComparer.Instance)
            {
                {
                    GitLanguage.ChineseSimplified, new Dictionary<string, string>
                    {
                        ["window.title"] = "版本控制助手",
                        ["toolbar.refresh"] = "刷新",
                        ["toolbar.stageAll"] = "全部暂存",
                        ["toolbar.pull"] = "拉取",
                        ["toolbar.remote"] = "远端",
                        ["toolbar.branch"] = "分支",
                        ["toolbar.settings"] = "设置",
                        ["toolbar.settings.tooltip"] = "语言与偏好设置",
                        ["toolbar.openSettings"] = "打开项目设置",
                        ["toolbar.remoteMenuTitle"] = "选择远端",
                        ["toolbar.remoteMenuReload"] = "重新检测远端",
                        ["toolbar.noRemoteDetected"] = "未检测到远端",
                        ["toolbar.help"] = "帮助",
                        ["hero.headline"] = "让提交流程更顺滑",
                        ["hero.subline"] = "左侧查看改动，右侧填写提交说明，常用的 Git 操作都集中在这里。",
                        ["hero.metric.branch"] = "当前分支",
                        ["hero.metric.remote"] = "推送远端",
                        ["hero.helpButton"] = "查看完整使用教程",
                        ["hero.helpTooltip"] = "打开在线帮助，了解版本控制助手的全部特性。",
                        ["status.overview"] = "状态总览",
                        ["status.currentBranch"] = "当前分支：{0}",
                        ["status.branchUnknown"] = "未检测到分支",
                        ["status.clean"] = "工作区干净，可直接推送。",
                        ["status.summaryPlaceholder"] = "尚未检测到改动。",
                        ["status.error"] = "Git 命令执行失败",
                        ["changes.cardTitle"] = "变更列表",
                        ["changes.total"] = "共 {0} 个改动",
                        ["changes.search"] = "搜索",
                        ["changes.empty"] = "暂无改动",
                        ["commit.cardTitle"] = "提交信息",
                        ["commit.messageLabel"] = "提交说明",
                        ["commit.placeholder"] = "描述本次修改的内容与原因...",
                        ["commit.tip"] = "建议简明扼要地说明修改目的、影响和风险。",
                        ["commit.pushTargetLabel"] = "推送目标",
                        ["commit.customPushToggle"] = "使用自定义远端地址",
                        ["commit.customPushTarget"] = "远端地址",
                        ["commit.customPushHint"] = "如果你的仓库没有配置远端名称，可以直接填写完整 Git URL。",
                        ["commit.remoteHint"] = "当前将使用远端：{0}",
                        ["commit.emptyWarning"] = "没有可提交的改动。",
                        ["actions.commit"] = "提交",
                        ["actions.push"] = "推送",
                        ["notify.stageAll"] = "已暂存所有改动",
                        ["notify.commitSuccess"] = "提交完成",
                        ["notify.pullSuccess"] = "拉取完成",
                        ["notify.pushSuccess"] = "推送完成",
                        ["notify.fetchSuccess"] = "远端信息已更新",
                        ["log.title"] = "最近提交 (git log --graph)",
                        ["log.empty"] = "暂无日志，可先进行一次提交。",
                        ["dialog.gitError"] = "执行 git {0} 失败：\n{1}",
                        ["dialog.ok"] = "确定",
                        ["status.untracked"] = "未跟踪",
                        ["status.added"] = "新增",
                        ["status.modified"] = "已修改",
                        ["status.deleted"] = "已删除",
                        ["status.renamed"] = "已重命名",
                        ["status.unknown"] = "其它",
                        ["tree.noChanges"] = "暂无改动",
                        ["language.selector"] = "界面语言",
                        ["settings.remoteSection"] = "默认远端",
                        ["settings.remoteHelp"] = "这里的配置会作为窗口默认值，随时可以在工具栏中覆盖。",
                        ["settings.defaultRemote"] = "默认远端名称",
                        ["settings.defaultBranch"] = "默认推送分支",
                        ["settings.customPushSection"] = "自定义推送目标",
                        ["settings.customPushToggle"] = "改用完整远端地址",
                        ["settings.customPushTarget"] = "推送地址",
                        ["settings.resetDefaults"] = "重置为默认值",
                        ["errors.pushTargetMissing"] = "请先在工具栏或设置中配置远端与分支。",
                        ["pullPicker.title"] = "选择拉取版本",
                        ["pullPicker.description"] = "请选择要同步的远端历史版本（{0}/{1}）。",
                        ["pullPicker.loading"] = "正在读取远端日志...",
                        ["pullPicker.empty"] = "未能获取远端历史记录。",
                        ["pullPicker.confirm"] = "拉取所选版本",
                        ["pullPicker.cancel"] = "取消",
                        ["pullPicker.refresh"] = "重新获取",
                        ["pullPicker.invalidTarget"] = "远端或分支未配置，无法获取历史。"
                    }
                },
                {
                    GitLanguage.English, new Dictionary<string, string>
                    {
                        ["window.title"] = "Version Control Assistant",
                        ["toolbar.refresh"] = "Refresh",
                        ["toolbar.stageAll"] = "Stage All",
                        ["toolbar.pull"] = "Pull",
                        ["toolbar.remote"] = "Remote",
                        ["toolbar.branch"] = "Branch",
                        ["toolbar.settings"] = "Settings",
                        ["toolbar.settings.tooltip"] = "Language & preferences",
                        ["toolbar.openSettings"] = "Open Project Settings",
                        ["toolbar.remoteMenuTitle"] = "Pick remote",
                        ["toolbar.remoteMenuReload"] = "Rescan remotes",
                        ["toolbar.noRemoteDetected"] = "No remotes detected",
                        ["toolbar.help"] = "Help",
                        ["hero.headline"] = "Smoother Git workflows",
                        ["hero.subline"] = "Inspect file changes on the left, craft commits on the right, and run common Git commands in one place.",
                        ["hero.metric.branch"] = "Active branch",
                        ["hero.metric.remote"] = "Push remote",
                        ["hero.helpButton"] = "Open full guide",
                        ["hero.helpTooltip"] = "Opens the online documentation for Version Control Assistant.",
                        ["status.overview"] = "Status overview",
                        ["status.currentBranch"] = "Current branch: {0}",
                        ["status.branchUnknown"] = "No branch detected",
                        ["status.clean"] = "Working tree clean. Ready to push.",
                        ["status.summaryPlaceholder"] = "Nothing to commit yet.",
                        ["status.error"] = "Git command failed",
                        ["changes.cardTitle"] = "Changes",
                        ["changes.total"] = "{0} change(s)",
                        ["changes.search"] = "Search",
                        ["changes.empty"] = "No pending changes",
                        ["commit.cardTitle"] = "Commit",
                        ["commit.messageLabel"] = "Message",
                        ["commit.placeholder"] = "Describe what changed and why...",
                        ["commit.tip"] = "Mention the intent and impact in one or two sentences.",
                        ["commit.pushTargetLabel"] = "Push target",
                        ["commit.customPushToggle"] = "Use custom remote URL",
                        ["commit.customPushTarget"] = "Remote URL",
                        ["commit.customPushHint"] = "Use this when your repository has no named remote configured.",
                        ["commit.remoteHint"] = "Current remote: {0}",
                        ["commit.emptyWarning"] = "No changes to commit.",
                        ["actions.commit"] = "Commit",
                        ["actions.push"] = "Push",
                        ["notify.stageAll"] = "All changes staged",
                        ["notify.commitSuccess"] = "Commit completed",
                        ["notify.pullSuccess"] = "Pull completed",
                        ["notify.pushSuccess"] = "Push completed",
                        ["notify.fetchSuccess"] = "Remote refs refreshed",
                        ["log.title"] = "Recent commits (git log --graph)",
                        ["log.empty"] = "Log is empty. Make a commit to get started.",
                        ["dialog.gitError"] = "git {0} failed:\n{1}",
                        ["dialog.ok"] = "OK",
                        ["status.untracked"] = "Untracked",
                        ["status.added"] = "Added",
                        ["status.modified"] = "Modified",
                        ["status.deleted"] = "Deleted",
                        ["status.renamed"] = "Renamed",
                        ["status.unknown"] = "Other",
                        ["tree.noChanges"] = "No pending changes",
                        ["language.selector"] = "Language",
                        ["settings.remoteSection"] = "Default remote",
                        ["settings.remoteHelp"] = "These values become the defaults in the Version Control Assistant window.",
                        ["settings.defaultRemote"] = "Default remote name",
                        ["settings.defaultBranch"] = "Default push branch",
                        ["settings.customPushSection"] = "Custom push target",
                        ["settings.customPushToggle"] = "Use full remote URL",
                        ["settings.customPushTarget"] = "Push target",
                        ["settings.resetDefaults"] = "Reset to defaults",
                        ["errors.pushTargetMissing"] = "Please configure a remote and branch first.",
                        ["pullPicker.title"] = "Pick pull target",
                        ["pullPicker.description"] = "Select a remote revision to sync ({0}/{1}).",
                        ["pullPicker.loading"] = "Loading remote log...",
                        ["pullPicker.empty"] = "No remote history available.",
                        ["pullPicker.confirm"] = "Pull selected revision",
                        ["pullPicker.cancel"] = "Cancel",
                        ["pullPicker.refresh"] = "Refresh list",
                        ["pullPicker.invalidTarget"] = "Remote or branch missing. Cannot read history."
                    }
                },
                {
                    GitLanguage.Japanese, new Dictionary<string, string>
                    {
                        ["window.title"] = "バージョンコントロールアシスタント",
                        ["toolbar.refresh"] = "再読み込み",
                        ["toolbar.stageAll"] = "すべてステージ",
                        ["toolbar.pull"] = "プル",
                        ["toolbar.remote"] = "リモート",
                        ["toolbar.branch"] = "ブランチ",
                        ["toolbar.settings"] = "設定",
                        ["toolbar.settings.tooltip"] = "言語と設定",
                        ["toolbar.openSettings"] = "プロジェクト設定を開く",
                        ["toolbar.remoteMenuTitle"] = "リモートを選択",
                        ["toolbar.remoteMenuReload"] = "リモートを再取得",
                        ["toolbar.noRemoteDetected"] = "リモートが見つかりません",
                        ["toolbar.help"] = "ヘルプ",
                        ["hero.headline"] = "Git 作業をもっとスマートに",
                        ["hero.subline"] = "左で変更を確認し、右でメッセージをまとめて、よく使う Git 操作をワンクリックで実行。",
                        ["hero.metric.branch"] = "現在のブランチ",
                        ["hero.metric.remote"] = "プッシュ先リモート",
                        ["hero.helpButton"] = "オンラインガイドを見る",
                        ["hero.helpTooltip"] = "バージョンコントロールアシスタントの詳細ドキュメントをブラウザで開きます。",
                        ["status.overview"] = "ステータス概要",
                        ["status.currentBranch"] = "現在のブランチ：{0}",
                        ["status.branchUnknown"] = "ブランチを検出できません",
                        ["status.clean"] = "作業ツリーはクリーンです。すぐにプッシュできます。",
                        ["status.summaryPlaceholder"] = "まだ変更はありません。",
                        ["status.error"] = "Git コマンドに失敗しました",
                        ["changes.cardTitle"] = "変更一覧",
                        ["changes.total"] = "変更 {0} 件",
                        ["changes.search"] = "検索",
                        ["changes.empty"] = "変更はありません",
                        ["commit.cardTitle"] = "コミットメッセージ",
                        ["commit.messageLabel"] = "メッセージ",
                        ["commit.placeholder"] = "今回の変更内容と理由を記入してください...",
                        ["commit.tip"] = "目的と影響を簡潔に記載すると共有しやすくなります。",
                        ["commit.pushTargetLabel"] = "プッシュ先",
                        ["commit.customPushToggle"] = "カスタム URL を使う",
                        ["commit.customPushTarget"] = "リモート URL",
                        ["commit.customPushHint"] = "リモート名が未設定の場合は完全な Git URL を入力してください。",
                        ["commit.remoteHint"] = "現在のリモート：{0}",
                        ["commit.emptyWarning"] = "コミットできる変更がありません。",
                        ["actions.commit"] = "コミット",
                        ["actions.push"] = "プッシュ",
                        ["notify.stageAll"] = "すべての変更をステージしました",
                        ["notify.commitSuccess"] = "コミットが完了しました",
                        ["notify.pullSuccess"] = "プルが完了しました",
                        ["notify.pushSuccess"] = "プッシュが完了しました",
                        ["notify.fetchSuccess"] = "リモート情報を更新しました",
                        ["log.title"] = "直近のコミット (git log --graph)",
                        ["log.empty"] = "履歴はまだありません。まずはコミットしましょう。",
                        ["dialog.gitError"] = "git {0} に失敗しました：\n{1}",
                        ["dialog.ok"] = "OK",
                        ["status.untracked"] = "未追跡",
                        ["status.added"] = "追加",
                        ["status.modified"] = "変更",
                        ["status.deleted"] = "削除",
                        ["status.renamed"] = "リネーム",
                        ["status.unknown"] = "その他",
                        ["tree.noChanges"] = "変更はありません",
                        ["language.selector"] = "表示言語",
                        ["settings.remoteSection"] = "既定のリモート",
                        ["settings.remoteHelp"] = "ここでの設定はウィンドウの初期値として使用されます。",
                        ["settings.defaultRemote"] = "既定のリモート名",
                        ["settings.defaultBranch"] = "既定のプッシュ先ブランチ",
                        ["settings.customPushSection"] = "カスタムプッシュ先",
                        ["settings.customPushToggle"] = "完全なリモート URL を使用",
                        ["settings.customPushTarget"] = "プッシュ URL",
                        ["settings.resetDefaults"] = "既定値に戻す",
                        ["errors.pushTargetMissing"] = "まずリモートとブランチを設定してください。",
                        ["pullPicker.title"] = "取得する履歴を選択",
                        ["pullPicker.description"] = "同期したいリモート履歴を選んでください（{0}/{1}）。",
                        ["pullPicker.loading"] = "リモートログを読み込み中...",
                        ["pullPicker.empty"] = "リモート履歴を取得できませんでした。",
                        ["pullPicker.confirm"] = "選択した履歴を取得",
                        ["pullPicker.cancel"] = "キャンセル",
                        ["pullPicker.refresh"] = "再取得",
                        ["pullPicker.invalidTarget"] = "リモートまたはブランチが未設定のため、履歴を取得できません。"
                    }
                }
            };

        private static GitLanguage _currentLanguage;

        public static event Action LanguageChanged;

        public static GitLanguage CurrentLanguage
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

        static GitLocalization()
        {
            var saved = (GitLanguage)EditorPrefs.GetInt(PrefKey, (int)DefaultLanguage);
            _currentLanguage = Tables.ContainsKey(saved) ? saved : DefaultLanguage;
        }

        public static void SetLanguage(GitLanguage language)
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

        public static string GetStatusLabel(GitChangeKind kind) => kind switch
        {
            GitChangeKind.Untracked => Tr("status.untracked"),
            GitChangeKind.Added => Tr("status.added"),
            GitChangeKind.Modified => Tr("status.modified"),
            GitChangeKind.Deleted => Tr("status.deleted"),
            GitChangeKind.Renamed => Tr("status.renamed"),
            _ => Tr("status.unknown")
        };

        private sealed class GitLanguageComparer : IEqualityComparer<GitLanguage>
        {
            public static readonly GitLanguageComparer Instance = new();

            public bool Equals(GitLanguage x, GitLanguage y) => x == y;

            public int GetHashCode(GitLanguage obj) => (int)obj;
        }
    }
}

