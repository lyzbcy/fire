using System;
using System.Collections.Generic;
using UnityEditor;

namespace Fire.GitAssistant
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
        private const string PrefKey = "Fire.GitAssistant.Language";
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
                        ["window.title"] = "Git 助手",
                        ["toolbar.refresh"] = "刷新",
                        ["toolbar.stageAll"] = "全部暂存",
                        ["toolbar.pull"] = "拉取",
                        ["toolbar.remote"] = "远端",
                        ["toolbar.branch"] = "分支",
                        ["toolbar.settings"] = "设置",
                        ["toolbar.settings.tooltip"] = "选择界面语言",
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
                        ["commit.emptyWarning"] = "没有可提交的改动。",
                        ["actions.commit"] = "提交",
                        ["actions.push"] = "推送",
                        ["notify.stageAll"] = "已暂存所有改动",
                        ["notify.commitSuccess"] = "提交完成",
                        ["notify.pullSuccess"] = "拉取完成",
                        ["notify.pushSuccess"] = "推送完成",
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
                        ["language.selector"] = "界面语言"
                    }
                },
                {
                    GitLanguage.English, new Dictionary<string, string>
                    {
                        ["window.title"] = "Git Assistant",
                        ["toolbar.refresh"] = "Refresh",
                        ["toolbar.stageAll"] = "Stage All",
                        ["toolbar.pull"] = "Pull",
                        ["toolbar.remote"] = "Remote",
                        ["toolbar.branch"] = "Branch",
                        ["toolbar.settings"] = "Settings",
                        ["toolbar.settings.tooltip"] = "Choose interface language",
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
                        ["commit.emptyWarning"] = "No changes to commit.",
                        ["actions.commit"] = "Commit",
                        ["actions.push"] = "Push",
                        ["notify.stageAll"] = "All changes staged",
                        ["notify.commitSuccess"] = "Commit completed",
                        ["notify.pullSuccess"] = "Pull completed",
                        ["notify.pushSuccess"] = "Push completed",
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
                        ["language.selector"] = "Language"
                    }
                },
                {
                    GitLanguage.Japanese, new Dictionary<string, string>
                    {
                        ["window.title"] = "Git アシスタント",
                        ["toolbar.refresh"] = "再読み込み",
                        ["toolbar.stageAll"] = "すべてステージ",
                        ["toolbar.pull"] = "プル",
                        ["toolbar.remote"] = "リモート",
                        ["toolbar.branch"] = "ブランチ",
                        ["toolbar.settings"] = "設定",
                        ["toolbar.settings.tooltip"] = "UI 言語を選択",
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
                        ["commit.emptyWarning"] = "コミットできる変更がありません。",
                        ["actions.commit"] = "コミット",
                        ["actions.push"] = "プッシュ",
                        ["notify.stageAll"] = "すべての変更をステージしました",
                        ["notify.commitSuccess"] = "コミットが完了しました",
                        ["notify.pullSuccess"] = "プルが完了しました",
                        ["notify.pushSuccess"] = "プッシュが完了しました",
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
                        ["language.selector"] = "表示言語"
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

