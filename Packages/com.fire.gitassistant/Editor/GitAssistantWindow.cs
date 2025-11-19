using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Fire.GitAssistant
{
    public sealed class GitAssistantWindow : EditorWindow
    {
        private const float RightPanelMinWidth = 320f;

        private TreeViewState _treeState;
        private GitTreeView _treeView;
        private SearchField _searchField;

        private IReadOnlyList<GitStatusEntry> _entries = Array.Empty<GitStatusEntry>();
        private string _branch = string.Empty;
        private string _statusSummary = string.Empty;
        private string _log = string.Empty;
        private string _commitMessage = string.Empty;
        private string _remoteName = "origin";
        private string _pushBranch = "main";
        private string _errorMessage = string.Empty;

        private Vector2 _logScroll;
        private GUIStyle _cardStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _mutedLabelStyle;
        private GUIStyle _pillStyle;
        private GUIStyle _primaryButtonStyle;
        private GUIStyle _secondaryButtonStyle;
        private GUIContent _settingsIconContent;

        private bool HasChanges => _entries != null && _entries.Count > 0;
        private bool CanCommit => HasChanges && !string.IsNullOrWhiteSpace(_commitMessage);

        [MenuItem("Tools/Fire/Git 助手")]
        public static void ShowWindow()
        {
            var window = GetWindow<GitAssistantWindow>();
            window.minSize = new Vector2(760, 460);
            window.titleContent = new GUIContent(GitLocalization.Tr("window.title"));
            window.RefreshData();
        }

        private void OnEnable()
        {
            _treeState ??= new TreeViewState();
            _treeView ??= new GitTreeView(_treeState);
            _searchField ??= new SearchField();

            GitLocalization.LanguageChanged += HandleLanguageChanged;
            titleContent = new GUIContent(GitLocalization.Tr("window.title"));

            RefreshData();
        }

        private void OnDisable()
        {
            GitLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged()
        {
            titleContent = new GUIContent(GitLocalization.Tr("window.title"));
            _statusSummary = BuildSummary(_entries);
            _treeView?.Reload();
            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();

            DrawToolbar();

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
                {
                    DrawStatusCard();
                    EditorGUILayout.Space(6);
                    DrawChangesCard();
                }

                GUILayout.Space(10);

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Max(RightPanelMinWidth, position.width * 0.38f))))
                {
                    DrawCommitCard();
                    EditorGUILayout.Space(6);
                    DrawLogCard();
                }
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(GitLocalization.Tr("toolbar.refresh"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    RefreshData();
                }

                if (GUILayout.Button(GitLocalization.Tr("toolbar.stageAll"), EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    if (ExecuteGitCommand("add -A", GitLocalization.Tr("notify.stageAll")))
                    {
                        RefreshData();
                    }
                }

                if (GUILayout.Button(GitLocalization.Tr("toolbar.pull"), EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    if (ExecuteGitCommand($"pull {_remoteName} {_pushBranch}", GitLocalization.Tr("notify.pullSuccess")))
                    {
                        RefreshData();
                    }
                }

                GUILayout.FlexibleSpace();

                GUILayout.Label(GitLocalization.Tr("toolbar.remote"), EditorStyles.miniLabel, GUILayout.Width(50));
                _remoteName = GUILayout.TextField(_remoteName, GUILayout.Width(110));

                GUILayout.Label(GitLocalization.Tr("toolbar.branch"), EditorStyles.miniLabel, GUILayout.Width(45));
                _pushBranch = GUILayout.TextField(_pushBranch, GUILayout.Width(110));

                if (GUILayout.Button(_settingsIconContent, EditorStyles.toolbarButton, GUILayout.Width(26)))
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    ShowLanguageMenu(new Rect(rect.x, rect.yMax, 0, 0));
                }
            }
        }

        private void DrawStatusCard()
        {
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                EditorGUILayout.LabelField(GitLocalization.Tr("status.overview"), _sectionTitleStyle);

                var branchLabel = string.IsNullOrEmpty(_branch)
                    ? GitLocalization.Tr("status.branchUnknown")
                    : GitLocalization.Tr("status.currentBranch", _branch);
                EditorGUILayout.LabelField(branchLabel, EditorStyles.boldLabel);

                if (!string.IsNullOrEmpty(_statusSummary))
                {
                    EditorGUILayout.LabelField(_statusSummary, _mutedLabelStyle);
                }
                else
                {
                    EditorGUILayout.LabelField(GitLocalization.Tr("status.clean"), _mutedLabelStyle);
                }

                if (!string.IsNullOrEmpty(_errorMessage))
                {
                    EditorGUILayout.HelpBox($"{GitLocalization.Tr("status.error")}\n{_errorMessage}", MessageType.Error);
                }
            }
        }

        private void DrawChangesCard()
        {
            using (new EditorGUILayout.VerticalScope(_cardStyle, GUILayout.ExpandHeight(true)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(GitLocalization.Tr("changes.cardTitle"), _sectionTitleStyle);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(GitLocalization.Tr("changes.total", _entries.Count), _mutedLabelStyle, GUILayout.Width(140));
                }

                EditorGUILayout.Space(4);
                DrawChangeBadges();
                EditorGUILayout.Space(6);

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label(GitLocalization.Tr("changes.search"), EditorStyles.miniLabel, GUILayout.Width(60));
                    _treeView.searchString = _searchField.OnToolbarGUI(_treeView.searchString);
                }

                var treeRect = GUILayoutUtility.GetRect(0, 10000, GUILayout.ExpandHeight(true));
                _treeView.OnGUI(treeRect);
            }
        }

        private void DrawChangeBadges()
        {
            var groups = _entries
                .GroupBy(e => e.Kind)
                .OrderByDescending(g => g.Count())
                .ToList();

            if (groups.Count == 0)
            {
                EditorGUILayout.LabelField(GitLocalization.Tr("changes.empty"), _mutedLabelStyle);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var group in groups)
                {
                    var label = $"{GitLocalization.GetStatusLabel(group.Key)} · {group.Count()}";
                    GUILayout.Label(label, _pillStyle);
                }
            }
        }

        private void DrawCommitCard()
        {
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                EditorGUILayout.LabelField(GitLocalization.Tr("commit.cardTitle"), _sectionTitleStyle);
                EditorGUILayout.LabelField(GitLocalization.Tr("commit.tip"), _mutedLabelStyle);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(GitLocalization.Tr("commit.messageLabel"));

                _commitMessage = EditorGUILayout.TextArea(
                    _commitMessage,
                    GUILayout.MinHeight(90));

                if (!HasChanges)
                {
                    EditorGUILayout.HelpBox(GitLocalization.Tr("commit.emptyWarning"), MessageType.Info);
                }

                EditorGUILayout.Space(6);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!CanCommit))
                    {
                        if (GUILayout.Button(GitLocalization.Tr("actions.commit"), _primaryButtonStyle))
                        {
                            TryCommit();
                        }
                    }

                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_remoteName) || string.IsNullOrWhiteSpace(_pushBranch)))
                    {
                        if (GUILayout.Button(GitLocalization.Tr("actions.push"), _secondaryButtonStyle))
                        {
                            TryPush();
                        }
                    }
                }
            }
        }

        private void DrawLogCard()
        {
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                EditorGUILayout.LabelField(GitLocalization.Tr("log.title"), _sectionTitleStyle);
                _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(200));
                EditorGUILayout.SelectableLabel(
                    string.IsNullOrEmpty(_log) ? GitLocalization.Tr("log.empty") : _log,
                    EditorStyles.textArea,
                    GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private void TryCommit()
        {
            if (!CanCommit)
            {
                return;
            }

            var sanitized = _commitMessage.Trim().Replace("\"", "\\\"");
            if (ExecuteGitCommand($"commit -m \"{sanitized}\"", GitLocalization.Tr("notify.commitSuccess")))
            {
                _commitMessage = string.Empty;
                RefreshData();
            }
        }

        private void TryPush()
        {
            if (ExecuteGitCommand($"push {_remoteName} {_pushBranch}", GitLocalization.Tr("notify.pushSuccess")))
            {
                RefreshData();
            }
        }

        private void RefreshData()
        {
            _branch = GitProcessUtility.GetCurrentBranch();
            _log = GitProcessUtility.GetRecentLog();
            _entries = GitProcessUtility.GetStatusEntries();
            _treeView?.SetEntries(_entries);
            _statusSummary = BuildSummary(_entries);
            _errorMessage = string.Empty;

            if (string.IsNullOrEmpty(_pushBranch) && !string.IsNullOrEmpty(_branch))
            {
                _pushBranch = _branch;
            }

            Repaint();
        }

        private string BuildSummary(IReadOnlyList<GitStatusEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" · ", entries
                .GroupBy(e => e.Kind)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{GitLocalization.GetStatusLabel(g.Key)} {g.Count()}"));
        }

        private bool ExecuteGitCommand(string arguments, string successMessage)
        {
            var result = GitProcessUtility.Run(arguments);
            if (!result.Success)
            {
                _errorMessage = result.Error;
                EditorUtility.DisplayDialog(
                    GitLocalization.Tr("window.title"),
                    GitLocalization.Tr("dialog.gitError", arguments, result.Error),
                    GitLocalization.Tr("dialog.ok"));
                return false;
            }

            _errorMessage = string.Empty;
            ShowNotification(new GUIContent(successMessage));
            return true;
        }

        private void ShowLanguageMenu(Rect anchorRect)
        {
            var menu = new GenericMenu();
            foreach (var info in GitLocalization.AvailableLanguages)
            {
                var language = info.Language;
                var isCurrent = language == GitLocalization.CurrentLanguage;
                menu.AddItem(new GUIContent(info.DisplayName), isCurrent, () => GitLocalization.SetLanguage(language));
            }

            menu.DropDown(anchorRect);
        }

        private void EnsureStyles()
        {
            if (_cardStyle == null)
            {
                _cardStyle = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(14, 14, 12, 12)
                };

                _sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13
                };

                _mutedLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    fontSize = 11
                };

                _pillStyle = new GUIStyle(EditorStyles.miniButtonMid)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    padding = new RectOffset(10, 10, 2, 2),
                    margin = new RectOffset(0, 6, 0, 4)
                };

                _primaryButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 34
                };

                _secondaryButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fixedHeight = 34
                };
            }

            _settingsIconContent ??= EditorGUIUtility.IconContent("_Popup");
            _settingsIconContent.tooltip = GitLocalization.Tr("toolbar.settings.tooltip");
        }
    }
}

