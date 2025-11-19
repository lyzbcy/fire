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
        private const string HelpUrl = "https://lyzbcy.github.io/posts/Unity%E6%8F%92%E4%BB%B6-Git%E5%8A%A9%E6%89%8B%E5%BC%80%E5%8F%91%E6%8A%A5%E5%91%8A/";

        private readonly List<string> _remoteOptions = new();

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
        private bool _useCustomPushTarget;
        private string _customPushTarget = string.Empty;
        private int _remoteIndex = -1;

        private Vector2 _logScroll;
        private GUIStyle _cardStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _mutedLabelStyle;
        private GUIStyle _pillStyle;
        private GUIStyle _primaryButtonStyle;
        private GUIStyle _secondaryButtonStyle;
        private GUIStyle _heroStyle;
        private GUIStyle _heroTitleStyle;
        private GUIStyle _heroSubtitleStyle;
        private GUIStyle _metricBadgeStyle;
        private GUIStyle _metricLabelStyle;
        private GUIStyle _metricValueStyle;
        private GUIStyle _heroHelpButtonStyle;
        private GUIContent _settingsIconContent;
        private GUIContent _remoteMenuIconContent;
        private GUIContent _helpIconContent;
        private Texture2D _heroBackgroundTexture;
        private Texture2D _metricBackgroundTexture;

        private bool HasChanges => _entries != null && _entries.Count > 0;
        private bool CanCommit => HasChanges && !string.IsNullOrWhiteSpace(_commitMessage);
        private bool CanPull => !string.IsNullOrWhiteSpace(_remoteName) && !string.IsNullOrWhiteSpace(_pushBranch);
        private bool CanPush => !string.IsNullOrWhiteSpace(GetPushTarget()) && !string.IsNullOrWhiteSpace(_pushBranch);

        [MenuItem("Tools/Git 助手")]
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
            LoadPreferences();

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
            DrawHeroHeader();
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

                using (new EditorGUI.DisabledScope(!CanPull))
                {
                    if (GUILayout.Button(GitLocalization.Tr("toolbar.pull"), EditorStyles.toolbarButton, GUILayout.Width(70)))
                    {
                        TryPull();
                    }
                }

                GUILayout.FlexibleSpace();

                GUILayout.Label(GitLocalization.Tr("toolbar.remote"), EditorStyles.miniLabel, GUILayout.Width(50));
                var currentRemote = GUILayout.TextField(_remoteName, GUILayout.Width(140));
                if (!string.Equals(currentRemote, _remoteName, StringComparison.Ordinal))
                {
                    _remoteName = currentRemote;
                    SavePreferences();
                }
                using (new EditorGUI.DisabledScope(_remoteOptions.Count == 0))
                {
                    if (GUILayout.Button(_remoteMenuIconContent, EditorStyles.toolbarButton, GUILayout.Width(28)))
                    {
                        var rect = GUILayoutUtility.GetLastRect();
                        ShowRemoteMenu(new Rect(rect.x, rect.yMax, 0, 0));
                    }
                }

                GUILayout.Label(GitLocalization.Tr("toolbar.branch"), EditorStyles.miniLabel, GUILayout.Width(45));
                var nextBranch = GUILayout.TextField(_pushBranch, GUILayout.Width(120));
                if (!string.Equals(nextBranch, _pushBranch, StringComparison.Ordinal))
                {
                    _pushBranch = nextBranch;
                    SavePreferences();
                }

                if (GUILayout.Button(GitLocalization.Tr("toolbar.help"), EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    Application.OpenURL(HelpUrl);
                }

                if (GUILayout.Button(_settingsIconContent, EditorStyles.toolbarButton, GUILayout.Width(26)))
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    ShowUtilityMenu(new Rect(rect.x, rect.yMax, 0, 0));
                }
            }
        }
        private void DrawHeroHeader()
        {
            using (new EditorGUILayout.VerticalScope(_heroStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(GitLocalization.Tr("hero.headline"), _heroTitleStyle);
                        EditorGUILayout.LabelField(GitLocalization.Tr("hero.subline"), _heroSubtitleStyle);
                        EditorGUILayout.Space(6);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            DrawMetricBadge(GitLocalization.Tr("hero.metric.branch"), string.IsNullOrEmpty(_branch) ? GitLocalization.Tr("status.branchUnknown") : _branch);
                            DrawMetricBadge(GitLocalization.Tr("hero.metric.remote"), string.IsNullOrEmpty(_remoteName) ? GitLocalization.Tr("toolbar.noRemoteDetected") : _remoteName);
                        }
                    }

                    GUILayout.FlexibleSpace();

                    var helpContent = new GUIContent(GitLocalization.Tr("hero.helpButton"), _helpIconContent.image, GitLocalization.Tr("hero.helpTooltip"));
                    if (GUILayout.Button(helpContent, _heroHelpButtonStyle, GUILayout.Width(170), GUILayout.Height(38)))
                    {
                        Application.OpenURL(HelpUrl);
                    }
                }
            }
        }

        private void DrawMetricBadge(string label, string value)
        {
            using (new EditorGUILayout.VerticalScope(_metricBadgeStyle, GUILayout.Width(180)))
            {
                EditorGUILayout.LabelField(label, _metricLabelStyle);
                EditorGUILayout.LabelField(value, _metricValueStyle);
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

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(GitLocalization.Tr("commit.pushTargetLabel"), EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(GitLocalization.Tr("toolbar.remote"), GUILayout.Width(60));
                    EditorGUI.BeginDisabledGroup(_useCustomPushTarget);
                    var newRemote = EditorGUILayout.TextField(_remoteName);
                    EditorGUI.EndDisabledGroup();
                    if (!_useCustomPushTarget && !string.Equals(newRemote, _remoteName, StringComparison.Ordinal))
                    {
                        _remoteName = newRemote;
                        SavePreferences();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(GitLocalization.Tr("toolbar.branch"), GUILayout.Width(60));
                    var updatedBranch = EditorGUILayout.TextField(_pushBranch);
                    if (!string.Equals(updatedBranch, _pushBranch, StringComparison.Ordinal))
                    {
                        _pushBranch = updatedBranch;
                        SavePreferences();
                    }
                }

                var useCustom = EditorGUILayout.ToggleLeft(GitLocalization.Tr("commit.customPushToggle"), _useCustomPushTarget);
                if (useCustom != _useCustomPushTarget)
                {
                    _useCustomPushTarget = useCustom;
                    SavePreferences();
                }

                using (new EditorGUI.DisabledScope(!_useCustomPushTarget))
                {
                    var newTarget = EditorGUILayout.TextField(GitLocalization.Tr("commit.customPushTarget"), _customPushTarget);
                    if (!string.Equals(newTarget, _customPushTarget, StringComparison.Ordinal))
                    {
                        _customPushTarget = newTarget;
                        SavePreferences();
                    }
                }

                EditorGUILayout.HelpBox(
                    _useCustomPushTarget
                        ? GitLocalization.Tr("commit.customPushHint")
                        : GitLocalization.Tr("commit.remoteHint", string.IsNullOrWhiteSpace(_remoteName) ? GitLocalization.Tr("toolbar.noRemoteDetected") : _remoteName),
                    MessageType.Info);

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

                    using (new EditorGUI.DisabledScope(!CanPush))
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
            if (!CanPush)
            {
                _errorMessage = GitLocalization.Tr("errors.pushTargetMissing");
                return;
            }

            var target = GetPushTarget();
            var command = _useCustomPushTarget
                ? $"push \"{target}\" {_pushBranch}"
                : $"push {target} {_pushBranch}";

            if (ExecuteGitCommand(command, GitLocalization.Tr("notify.pushSuccess")))
            {
                RefreshData();
            }
        }

        private void TryPull()
        {
            if (!CanPull)
            {
                _errorMessage = GitLocalization.Tr("errors.pushTargetMissing");
                return;
            }

            if (ExecuteGitCommand($"pull {_remoteName} {_pushBranch}", GitLocalization.Tr("notify.pullSuccess")))
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
            UpdateRemoteOptions();

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

        private void ShowUtilityMenu(Rect anchorRect)
        {
            var menu = new GenericMenu();
            menu.AddDisabledItem(new GUIContent(GitLocalization.Tr("language.selector")));
            foreach (var info in GitLocalization.AvailableLanguages)
            {
                var language = info.Language;
                var isCurrent = language == GitLocalization.CurrentLanguage;
                menu.AddItem(new GUIContent(info.DisplayName), isCurrent, () => GitLocalization.SetLanguage(language));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(
                new GUIContent(GitLocalization.Tr("toolbar.openSettings")),
                false,
                () => SettingsService.OpenProjectSettings("Project/Git 助手"));

            menu.DropDown(anchorRect);
        }

        private void ShowRemoteMenu(Rect anchorRect)
        {
            var menu = new GenericMenu();
            if (_remoteOptions.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent(GitLocalization.Tr("toolbar.noRemoteDetected")));
            }
            else
            {
                foreach (var remote in _remoteOptions)
                {
                    var isCurrent = string.Equals(remote, _remoteName, StringComparison.Ordinal);
                    menu.AddItem(new GUIContent(remote), isCurrent, () =>
                    {
                        _remoteName = remote;
                        SavePreferences();
                        Repaint();
                    });
                }
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent(GitLocalization.Tr("toolbar.remoteMenuReload")), false, RefreshData);
            menu.DropDown(anchorRect);
        }

        private void LoadPreferences()
        {
            var prefs = GitAssistantPreferences.Instance;
            _remoteName = prefs.DefaultRemote;
            _pushBranch = prefs.DefaultPushBranch;
            _useCustomPushTarget = prefs.UseCustomPushTarget;
            _customPushTarget = prefs.CustomPushTarget;
        }

        private void SavePreferences()
        {
            var prefs = GitAssistantPreferences.Instance;
            prefs.DefaultRemote = _remoteName;
            prefs.DefaultPushBranch = _pushBranch;
            prefs.UseCustomPushTarget = _useCustomPushTarget;
            prefs.CustomPushTarget = _customPushTarget;
        }

        private void UpdateRemoteOptions()
        {
            _remoteOptions.Clear();
            _remoteOptions.AddRange(GitProcessUtility.GetRemoteNames());
            if (_remoteOptions.Count == 0)
            {
                _remoteIndex = -1;
                return;
            }

            var index = _remoteOptions.FindIndex(r => string.Equals(r, _remoteName, StringComparison.Ordinal));
            _remoteIndex = index;
        }

        private string GetPushTarget() => _useCustomPushTarget ? _customPushTarget : _remoteName;

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

                _heroBackgroundTexture ??= CreateColorTexture(new Color(0.12f, 0.15f, 0.22f));
                _metricBackgroundTexture ??= CreateColorTexture(new Color(1f, 1f, 1f, 0.1f));

                _heroStyle = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(18, 18, 16, 16),
                    margin = new RectOffset(0, 0, 0, 0),
                    normal = { background = _heroBackgroundTexture }
                };

                _heroTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16
                };

                _heroSubtitleStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    fontSize = 12,
                    normal = { textColor = new Color(0.8f, 0.85f, 0.9f) }
                };

                _metricBadgeStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { background = _metricBackgroundTexture },
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(0, 8, 0, 0)
                };

                _metricLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.75f, 0.82f, 0.9f) }
                };

                _metricValueStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13
                };

                _heroHelpButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 36,
                    padding = new RectOffset(12, 12, 6, 6)
                };
            }

            _settingsIconContent ??= EditorGUIUtility.IconContent("_Popup");
            _settingsIconContent.tooltip = GitLocalization.Tr("toolbar.settings.tooltip");
            _remoteMenuIconContent ??= new GUIContent("...", GitLocalization.Tr("toolbar.remoteMenuTitle"));
            _remoteMenuIconContent.tooltip = GitLocalization.Tr("toolbar.remoteMenuTitle");
            _helpIconContent ??= EditorGUIUtility.IconContent("_Help");
            _helpIconContent.tooltip = GitLocalization.Tr("hero.helpTooltip");
        }

        private static Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }
    }
}

