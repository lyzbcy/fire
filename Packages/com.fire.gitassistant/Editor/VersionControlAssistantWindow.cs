using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Fire.VersionControlAssistant
{
    public sealed class VersionControlAssistantWindow : EditorWindow
    {
        private const float RightPanelMinWidth = 320f;
        private const string HelpUrl = "https://lyzbcy.github.io/posts/Unity%E6%8F%92%E4%BB%B6-Git%E5%8A%A9%E6%89%8B%E5%BC%80%E5%8F%91%E6%8A%A5%E5%91%8A/";
        private const float LogEntryApproxHeight = 74f;

        private readonly List<string> _remoteOptions = new();

        private TreeViewState _treeState;
        private GitTreeView _treeView;
        private SearchField _searchField;

        private IReadOnlyList<GitStatusEntry> _entries = Array.Empty<GitStatusEntry>();
        private IReadOnlyList<GitCommitEntry> _commitHistory = Array.Empty<GitCommitEntry>();
        private string _branch = string.Empty;
        private string _statusSummary = string.Empty;
        private string _commitMessage = string.Empty;
        private string _remoteName = "origin";
        private string _pushBranch = "main";
        private string _errorMessage = string.Empty;
        private bool _useCustomPushTarget;
        private string _customPushTarget = string.Empty;
        private int _remoteIndex = -1;

        private Vector2 _rightPanelScroll;
        private Vector2 _logHistoryScroll;
        private GUIStyle _cardStyle;
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
        private GUIStyle _logCommitCardStyle;
        private GUIStyle _logCommitTitleStyle;
        private GUIStyle _logCommitMetaStyle;
        private GUIStyle _authorTagStyle;
        private GUIStyle _cardHeaderTitleStyle;
        private GUIStyle _cardHeaderSubtitleStyle;
        private GUIStyle _cardHeaderIconStyle;
        private GUIContent _settingsIconContent;
        private GUIContent _remoteMenuIconContent;
        private GUIContent _helpIconContent;
        private GUIContent _statusCardIconContent;
        private GUIContent _changesCardIconContent;
        private GUIContent _commitCardIconContent;
        private GUIContent _logCardIconContent;
        private Texture2D _heroBackgroundTexture;
        private Texture2D _metricBackgroundTexture;
        private Texture2D _cardBackgroundTexture;
        private readonly Dictionary<string, Color> _authorColorCache = new();
        private static readonly Color[] AuthorColorPalette =
        {
            new(0.90f, 0.47f, 0.24f),
            new(0.36f, 0.65f, 0.99f),
            new(0.53f, 0.80f, 0.55f),
            new(0.92f, 0.72f, 0.27f),
            new(0.73f, 0.60f, 0.96f),
            new(0.98f, 0.42f, 0.50f)
        };
        private static readonly Color TimelineLineColor = new(1f, 1f, 1f, 0.18f);
        private static readonly Color AddedColor = new(0.29f, 0.73f, 0.52f);
        private static readonly Color ModifiedColor = new(0.29f, 0.56f, 0.94f);
        private static readonly Color DeletedColor = new(0.86f, 0.38f, 0.38f);
        private static readonly Color RenamedColor = new(0.93f, 0.69f, 0.32f);
        private static readonly Color UntrackedColor = new(0.78f, 0.46f, 0.92f);
        private static readonly Color UnknownColor = new(0.55f, 0.55f, 0.55f);

        private bool HasChanges => _entries != null && _entries.Count > 0;
        private bool CanCommit => HasChanges && !string.IsNullOrWhiteSpace(_commitMessage);
        private bool CanPull => !string.IsNullOrWhiteSpace(_remoteName) && !string.IsNullOrWhiteSpace(_pushBranch);
        private bool CanPush => !string.IsNullOrWhiteSpace(GetPushTarget()) && !string.IsNullOrWhiteSpace(_pushBranch);

        [MenuItem("Tools/Version Control Assistant")]
        public static void ShowWindow()
        {
            var window = GetWindow<VersionControlAssistantWindow>();
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

                var rightPanelWidth = GetRightPanelWidth();
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(rightPanelWidth), GUILayout.ExpandHeight(true)))
                {
                    _rightPanelScroll = EditorGUILayout.BeginScrollView(_rightPanelScroll, GUILayout.ExpandHeight(true));
                    DrawCommitCard();
                    EditorGUILayout.Space(6);
                    DrawLogCard();
                    EditorGUILayout.EndScrollView();
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
            var adaptiveWidth = Mathf.Clamp(position.width * 0.2f, 140f, 260f);
            using (new EditorGUILayout.VerticalScope(_metricBadgeStyle, GUILayout.MinWidth(adaptiveWidth), GUILayout.MaxWidth(adaptiveWidth)))
            {
                EditorGUILayout.LabelField(label, _metricLabelStyle);
                EditorGUILayout.LabelField(value, _metricValueStyle);
            }
        }

        private void DrawCardHeader(GUIContent icon, string title, string subtitle)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (icon != null && icon.image != null)
                {
                    GUILayout.Label(icon, _cardHeaderIconStyle, GUILayout.Width(28), GUILayout.Height(28));
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(title, _cardHeaderTitleStyle);
                    if (!string.IsNullOrEmpty(subtitle))
                    {
                        EditorGUILayout.LabelField(subtitle, _cardHeaderSubtitleStyle);
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawStatusCard()
        {
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                var summaryText = string.IsNullOrEmpty(_statusSummary)
                    ? GitLocalization.Tr("status.clean")
                    : _statusSummary;
                DrawCardHeader(_statusCardIconContent, GitLocalization.Tr("status.overview"), summaryText);
                EditorGUILayout.Space(6);

                var branchLabel = string.IsNullOrEmpty(_branch)
                    ? GitLocalization.Tr("status.branchUnknown")
                    : GitLocalization.Tr("status.currentBranch", _branch);
                EditorGUILayout.LabelField(branchLabel, EditorStyles.boldLabel);

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
                DrawCardHeader(_changesCardIconContent, GitLocalization.Tr("changes.cardTitle"), GitLocalization.Tr("changes.total", _entries.Count));

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

            DrawChangeDistributionBar(groups);
            EditorGUILayout.Space(6);

            var perRow = Mathf.Max(1, Mathf.FloorToInt(position.width / 210f));
            for (var i = 0; i < groups.Count; i += perRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var rowCount = Mathf.Min(perRow, groups.Count - i);
                    for (var j = 0; j < rowCount; j++)
                    {
                        var group = groups[i + j];
                        DrawChangeBadge(group.Key, group.Count());
                    }

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space(2);
            }
        }

        private void DrawChangeDistributionBar(IReadOnlyList<IGrouping<GitChangeKind, GitStatusEntry>> groups)
        {
            if (_entries == null || _entries.Count == 0)
            {
                return;
            }

            var lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(10), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(lineRect, new Color(1f, 1f, 1f, 0.06f));

            var total = (float)_entries.Count;
            var cursor = lineRect.x;
            foreach (var group in groups)
            {
                var count = group.Count();
                if (count <= 0)
                {
                    continue;
                }

                var width = lineRect.width * (count / total);
                if (width <= 0)
                {
                    continue;
                }

                var sliceRect = new Rect(cursor, lineRect.y, width, lineRect.height);
                EditorGUI.DrawRect(sliceRect, GetChangeColor(group.Key));
                cursor += width;
            }
        }

        private void DrawChangeBadge(GitChangeKind kind, int count)
        {
            var label = $"{GitLocalization.GetStatusLabel(kind)} · {count}";
            var content = new GUIContent(label);
            var badgeRect = GUILayoutUtility.GetRect(content, _pillStyle, GUILayout.MinWidth(120));
            EditorGUI.DrawRect(badgeRect, GetChangeColor(kind));
            var innerRect = new Rect(badgeRect.x + 1, badgeRect.y + 1, badgeRect.width - 2, badgeRect.height - 2);
            EditorGUI.DrawRect(innerRect, new Color(0f, 0f, 0f, 0.28f));
            GUI.Label(badgeRect, label, _pillStyle);
        }

        private void DrawCommitCard()
        {
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                DrawCardHeader(_commitCardIconContent, GitLocalization.Tr("commit.cardTitle"), GitLocalization.Tr("commit.tip"));
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField(GitLocalization.Tr("commit.messageLabel"));

                _commitMessage = EditorGUILayout.TextArea(
                    _commitMessage,
                    GUILayout.MinHeight(Mathf.Lerp(90f, 150f, Mathf.InverseLerp(760f, 1600f, position.width))));

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
                var hasCommits = _commitHistory != null && _commitHistory.Count > 0;
                var subtitle = hasCommits
                    ? $"{GitLocalization.Tr("log.title")} · {_commitHistory.Count}"
                    : GitLocalization.Tr("log.empty");
                DrawCardHeader(_logCardIconContent, GitLocalization.Tr("log.title"), subtitle);

                if (!hasCommits)
                {
                    EditorGUILayout.HelpBox(GitLocalization.Tr("log.empty"), MessageType.Info);
                    return;
                }

                var estimatedHeight = _commitHistory.Count * LogEntryApproxHeight;
                var maxScrollableHeight = Mathf.Clamp(position.height * 0.4f, 200f, 420f);

                if (estimatedHeight > maxScrollableHeight)
                {
                    using (var scroll = new EditorGUILayout.ScrollViewScope(_logHistoryScroll, GUILayout.Height(maxScrollableHeight)))
                    {
                        _logHistoryScroll = scroll.scrollPosition;
                        DrawLogEntries();
                    }
                }
                else
                {
                    DrawLogEntries();
                }
            }
        }

        private void DrawLogEntries()
        {
            if (_commitHistory == null || _commitHistory.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _commitHistory.Count; i++)
            {
                DrawCommitTimelineEntry(_commitHistory[i], i, _commitHistory.Count);
                if (i < _commitHistory.Count - 1)
                {
                    EditorGUILayout.Space(4);
                }
            }
        }

        private void TryCommit()
        {
            if (!CanCommit)
            {
                return;
            }

            if (!TryAutoStageAll())
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

            var remote = _remoteName?.Trim() ?? string.Empty;
            var branch = _pushBranch?.Trim() ?? string.Empty;
            VersionControlPullHistoryWindow.Show(remote, branch, hash => HandleHistoricalPull(remote, branch, hash));
        }

        private void HandleHistoricalPull(string remote, string branch, string commitHash)
        {
            if (string.IsNullOrWhiteSpace(commitHash))
            {
                return;
            }

            if (!ExecuteGitCommand($"fetch \"{remote}\" \"{branch}\"", GitLocalization.Tr("notify.fetchSuccess")))
            {
                return;
            }

            if (ExecuteGitCommand($"reset --hard {commitHash}", GitLocalization.Tr("notify.pullSuccess")))
            {
                RefreshData();
            }
        }

        private void RefreshData()
        {
            _branch = GitProcessUtility.GetCurrentBranch();
            _entries = GitProcessUtility.GetStatusEntries();
            _commitHistory = GitProcessUtility.GetRecentCommits();
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

        private bool TryAutoStageAll()
        {
            var result = GitProcessUtility.Run("add -A");
            if (result.Success)
            {
                _errorMessage = string.Empty;
                return true;
            }

            _errorMessage = result.Error;
            EditorUtility.DisplayDialog(
                GitLocalization.Tr("window.title"),
                GitLocalization.Tr("dialog.gitError", "add -A", result.Error),
                GitLocalization.Tr("dialog.ok"));
            return false;
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
                () => SettingsService.OpenProjectSettings("Project/Version Control Assistant"));

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
            var prefs = VersionControlAssistantPreferences.Instance;
            _remoteName = prefs.DefaultRemote;
            _pushBranch = prefs.DefaultPushBranch;
            _useCustomPushTarget = prefs.UseCustomPushTarget;
            _customPushTarget = prefs.CustomPushTarget;
        }

        private void SavePreferences()
        {
            var prefs = VersionControlAssistantPreferences.Instance;
            prefs.DefaultRemote = _remoteName;
            prefs.DefaultPushBranch = _pushBranch;
            prefs.UseCustomPushTarget = _useCustomPushTarget;
            prefs.CustomPushTarget = _customPushTarget;
        }

        private float GetRightPanelWidth()
        {
            var adaptiveWidth = position.width * 0.38f;
            var maxWidth = Mathf.Max(position.width - 360f, RightPanelMinWidth);
            return Mathf.Clamp(adaptiveWidth, RightPanelMinWidth, maxWidth);
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
                _cardBackgroundTexture ??= CreateVerticalGradientTexture(new Color(0.15f, 0.18f, 0.26f), new Color(0.09f, 0.10f, 0.14f));
                _heroBackgroundTexture ??= CreateVerticalGradientTexture(new Color(0.18f, 0.21f, 0.32f), new Color(0.08f, 0.10f, 0.18f));
                _metricBackgroundTexture ??= CreateVerticalGradientTexture(new Color(1f, 1f, 1f, 0.16f), new Color(1f, 1f, 1f, 0.05f));

                _cardStyle = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(14, 14, 12, 12)
                };
                _cardStyle.normal.background = _cardBackgroundTexture;

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
                _pillStyle.normal.textColor = Color.white;

                _primaryButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 34,
                    padding = new RectOffset(16, 16, 6, 6),
                    normal = { textColor = Color.white }
                };
                var primaryNormalTex = CreateColorTexture(new Color(0.2f, 0.6f, 0.9f));
                var primaryHoverTex = CreateColorTexture(new Color(0.25f, 0.65f, 0.95f));
                var primaryActiveTex = CreateColorTexture(new Color(0.18f, 0.55f, 0.85f));
                _primaryButtonStyle.normal.background = primaryNormalTex;
                _primaryButtonStyle.hover.background = primaryHoverTex;
                _primaryButtonStyle.active.background = primaryActiveTex;

                _secondaryButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fixedHeight = 34,
                    padding = new RectOffset(14, 14, 5, 5)
                };
                var secondaryNormalTex = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.3f, 0.32f, 0.38f) : new Color(0.7f, 0.72f, 0.78f));
                var secondaryHoverTex = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.35f, 0.37f, 0.43f) : new Color(0.75f, 0.77f, 0.83f));
                var secondaryActiveTex = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.25f, 0.27f, 0.33f) : new Color(0.65f, 0.67f, 0.73f));
                _secondaryButtonStyle.normal.background = secondaryNormalTex;
                _secondaryButtonStyle.hover.background = secondaryHoverTex;
                _secondaryButtonStyle.active.background = secondaryActiveTex;

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

                _logCommitCardStyle = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(10, 12, 6, 6),
                    margin = new RectOffset(0, 0, 2, 2)
                };

                _logCommitTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    wordWrap = true,
                    fontSize = 13
                };

                _logCommitMetaStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.75f, 0.78f, 0.82f) }
                };

                _authorTagStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(8, 8, 2, 2),
                    margin = new RectOffset(0, 0, 0, 0),
                    normal = { textColor = Color.white }
                };

                _cardHeaderTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14
                };
                _cardHeaderSubtitleStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    fontSize = 11,
                    normal = { textColor = new Color(0.72f, 0.78f, 0.88f) }
                };
                _cardHeaderIconStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(0, 10, 0, 0)
                };
            }

            _settingsIconContent ??= EditorGUIUtility.IconContent("_Popup");
            _settingsIconContent.tooltip = GitLocalization.Tr("toolbar.settings.tooltip");
            _remoteMenuIconContent ??= new GUIContent("...", GitLocalization.Tr("toolbar.remoteMenuTitle"));
            _remoteMenuIconContent.tooltip = GitLocalization.Tr("toolbar.remoteMenuTitle");
            _helpIconContent ??= EditorGUIUtility.IconContent("_Help");
            _helpIconContent.tooltip = GitLocalization.Tr("hero.helpTooltip");
            _statusCardIconContent ??= EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow");
            _statusCardIconContent.tooltip = GitLocalization.Tr("status.overview");
            _changesCardIconContent ??= EditorGUIUtility.IconContent("d_UnityEditor.HierarchyWindow");
            _changesCardIconContent.tooltip = GitLocalization.Tr("changes.cardTitle");
            _commitCardIconContent ??= EditorGUIUtility.IconContent("d_UnityEditor.Graphs.AnimatorControllerTool");
            _commitCardIconContent.tooltip = GitLocalization.Tr("commit.cardTitle");
            _logCardIconContent ??= EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow");
            _logCardIconContent.tooltip = GitLocalization.Tr("log.title");
        }

        private static Texture2D CreateVerticalGradientTexture(Color top, Color bottom)
        {
            const int height = 32;
            var texture = new Texture2D(1, height);
            for (var y = 0; y < height; y++)
            {
                var t = y / (height - 1f);
                texture.SetPixel(0, y, Color.Lerp(top, bottom, t));
            }

            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private static Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }


        private void DrawCommitTimelineEntry(GitCommitEntry entry, int index, int total)
        {
            using (new EditorGUILayout.HorizontalScope(_logCommitCardStyle))
            {
                var timelineRect = GUILayoutUtility.GetRect(28, 54, GUILayout.Width(28), GUILayout.ExpandHeight(true));
                DrawTimelineGizmo(timelineRect, index, total, entry.Author);

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(entry.Message) ? "-" : entry.Message, _logCommitTitleStyle);
                    EditorGUILayout.Space(2);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawAuthorTag(entry.Author);
                        GUILayout.Space(6);
                        EditorGUILayout.LabelField(entry.RelativeTime, _logCommitMetaStyle, GUILayout.Width(90));
                        GUILayout.Space(4);
                        EditorGUILayout.LabelField(entry.Hash, _logCommitMetaStyle, GUILayout.Width(70));
                        GUILayout.FlexibleSpace();
                    }
                }
            }
        }

        private void DrawTimelineGizmo(Rect rect, int index, int total, string author)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var center = new Vector3(rect.x + rect.width / 2f, rect.center.y, 0f);
            Handles.BeginGUI();
            Handles.color = TimelineLineColor;
            if (index > 0)
            {
                Handles.DrawLine(new Vector3(center.x, rect.y, 0f), new Vector3(center.x, center.y - 8f, 0f));
            }

            if (index < total - 1)
            {
                Handles.DrawLine(new Vector3(center.x, center.y + 8f, 0f), new Vector3(center.x, rect.yMax, 0f));
            }

            Handles.color = GetAuthorColor(author);
            Handles.DrawSolidDisc(center, Vector3.forward, 5f);
            Handles.color = Color.white;
            Handles.DrawSolidDisc(center, Vector3.forward, 2.4f);
            Handles.EndGUI();
        }

        private void DrawAuthorTag(string author)
        {
            var display = string.IsNullOrWhiteSpace(author) ? "-" : author;
            var content = new GUIContent(display);
            var rect = GUILayoutUtility.GetRect(content, _authorTagStyle, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(rect, GetAuthorColor(author));
            var innerRect = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
            EditorGUI.DrawRect(innerRect, new Color(0f, 0f, 0f, 0.35f));
            GUI.Label(rect, content, _authorTagStyle);
        }

        private Color GetAuthorColor(string author)
        {
            if (string.IsNullOrWhiteSpace(author))
            {
                author = "unknown";
            }

            if (_authorColorCache.TryGetValue(author, out var cached))
            {
                return cached;
            }

            var color = AuthorColorPalette[_authorColorCache.Count % AuthorColorPalette.Length];
            _authorColorCache[author] = color;
            return color;
        }

        private Color GetChangeColor(GitChangeKind kind)
        {
            return kind switch
            {
                GitChangeKind.Added => AddedColor,
                GitChangeKind.Modified => ModifiedColor,
                GitChangeKind.Deleted => DeletedColor,
                GitChangeKind.Renamed => RenamedColor,
                GitChangeKind.Untracked => UntrackedColor,
                _ => UnknownColor
            };
        }
    }
}

