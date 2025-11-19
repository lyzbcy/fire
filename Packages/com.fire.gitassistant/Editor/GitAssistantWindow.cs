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
        private TreeViewState _treeState;
        private GitTreeView _treeView;
        private SearchField _searchField;

        private string _branch = string.Empty;
        private string _statusSummary = string.Empty;
        private string _log = string.Empty;
        private string _commitMessage = string.Empty;
        private string _remoteName = "origin";
        private string _pushBranch = "main";
        private string _errorMessage = string.Empty;
        private Vector2 _logScroll;

        private IReadOnlyList<GitStatusEntry> _entries = Array.Empty<GitStatusEntry>();

        [MenuItem("Tools/Fire/Git Assistant")]
        public static void ShowWindow()
        {
            var window = GetWindow<GitAssistantWindow>("Git Assistant");
            window.minSize = new Vector2(640, 420);
            window.RefreshData();
        }

        private void OnEnable()
        {
            _treeState ??= new TreeViewState();
            _treeView ??= new GitTreeView(_treeState);
            _searchField ??= new SearchField();

            RefreshData();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawStatusInfo();
            DrawTreeView();
            DrawCommitSection();
            DrawLogSection();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    RefreshData();
                }

                if (GUILayout.Button("Stage All", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    ExecuteGitCommand("add -A", "暂存所有改动成功");
                    RefreshData();
                }

                if (GUILayout.Button("Pull", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    ExecuteGitCommand($"pull {_remoteName} {_pushBranch}", "Pull 完成");
                    RefreshData();
                }

                GUILayout.FlexibleSpace();

                GUILayout.Label("远端", GUILayout.Width(30));
                _remoteName = GUILayout.TextField(_remoteName, GUILayout.Width(90));
                GUILayout.Label("分支", GUILayout.Width(30));
                _pushBranch = GUILayout.TextField(_pushBranch, GUILayout.Width(90));

                _treeView.searchString = _searchField.OnToolbarGUI(_treeView.searchString);
            }
        }

        private void DrawStatusInfo()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"当前分支: {_branch}", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(_statusSummary))
            {
                EditorGUILayout.HelpBox(_statusSummary, MessageType.Info);
            }

            if (!string.IsNullOrEmpty(_errorMessage))
            {
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
            }
        }

        private void DrawTreeView()
        {
            var treeRect = GUILayoutUtility.GetRect(0, 1000, 200, 400, GUILayout.ExpandHeight(true));
            _treeView.OnGUI(treeRect);
        }

        private void DrawCommitSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("提交信息", EditorStyles.boldLabel);

            _commitMessage = EditorGUILayout.TextField(_commitMessage);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_commitMessage)))
                {
                    if (GUILayout.Button("Commit", GUILayout.Width(90)))
                    {
                        if (ExecuteGitCommand($"commit -m \"{_commitMessage.Replace("\"", "\\\"")}\"", "提交成功"))
                        {
                            _commitMessage = string.Empty;
                            RefreshData();
                        }
                    }
                }

                if (GUILayout.Button("Push", GUILayout.Width(90)))
                {
                    if (ExecuteGitCommand($"push {_remoteName} {_pushBranch}", "Push 完成"))
                    {
                        RefreshData();
                    }
                }
            }
        }

        private void DrawLogSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("最近提交 (git log --graph)", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(140));
                EditorGUILayout.TextArea(_log, EditorStyles.textArea, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
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
        }

        private string BuildSummary(IReadOnlyList<GitStatusEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "工作区干净，无需提交。";
            }

            var groups = entries
                .GroupBy(e => e.StatusLabel)
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToArray();

            return string.Join(" | ", groups);
        }

        private bool ExecuteGitCommand(string arguments, string successMessage)
        {
            var result = GitProcessUtility.Run(arguments);
            if (!result.Success)
            {
                _errorMessage = result.Error;
                EditorUtility.DisplayDialog("Git Assistant", $"执行 git {arguments} 失败:\n{result.Error}", "确定");
                return false;
            }

            _errorMessage = string.Empty;
            ShowNotification(new GUIContent(successMessage));
            return true;
        }
    }
}

