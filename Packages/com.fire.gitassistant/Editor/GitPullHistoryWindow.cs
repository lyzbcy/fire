using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fire.GitAssistant
{
    internal sealed class GitPullHistoryWindow : EditorWindow
    {
        private const int DefaultLogCount = 40;

        private string _remote = string.Empty;
        private string _branch = string.Empty;
        private Action<string> _onConfirm;
        private IReadOnlyList<GitCommitEntry> _entries = Array.Empty<GitCommitEntry>();
        private Vector2 _scrollPosition;
        private int _selectedIndex = -1;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private GUIStyle _entryStyle;

        public static void Show(string remote, string branch, Action<string> onConfirm)
        {
            var window = CreateInstance<GitPullHistoryWindow>();
            window._remote = remote ?? string.Empty;
            window._branch = branch ?? string.Empty;
            window._onConfirm = onConfirm;
            window.titleContent = new GUIContent(GitLocalization.Tr("pullPicker.title"));
            window.minSize = new Vector2(520f, 380f);
            window.ShowUtility();
            window.RefreshCommits();
        }

        private void OnEnable()
        {
            CreateEntryStyle();
        }

        private void CreateEntryStyle()
        {
            _entryStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                fontSize = 12,
                richText = false,
                padding = new RectOffset(12, 12, 8, 8)
            };
        }

        private void RefreshCommits()
        {
            _isLoading = true;
            _errorMessage = string.Empty;
            _entries = Array.Empty<GitCommitEntry>();
            _selectedIndex = -1;
            Repaint();

            var entries = GitProcessUtility.GetRemoteCommits(_remote, _branch, DefaultLogCount, out var error);
            _entries = entries;
            _errorMessage = error;
            _isLoading = false;
            if (_entries.Count > 0)
            {
                _selectedIndex = 0;
            }

            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                GitLocalization.Tr("pullPicker.description", _remote, _branch),
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(GitLocalization.Tr("pullPicker.refresh"), GUILayout.Width(110f)))
                {
                    RefreshCommits();
                }
            }

            EditorGUILayout.Space();

            if (_isLoading)
            {
                EditorGUILayout.HelpBox(GitLocalization.Tr("pullPicker.loading"), MessageType.Info);
                DrawFooter();
                return;
            }

            if (!string.IsNullOrEmpty(_errorMessage))
            {
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Warning);
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;

                if (_entries.Count == 0)
                {
                    EditorGUILayout.HelpBox(GitLocalization.Tr("pullPicker.empty"), MessageType.Info);
                }
                else
                {
                    for (var i = 0; i < _entries.Count; i++)
                    {
                        DrawEntry(i, _entries[i]);
                    }
                }
            }

            EditorGUILayout.Space();
            DrawFooter();
        }

        private void DrawEntry(int index, in GitCommitEntry entry)
        {
            var content = new GUIContent(
                $"{entry.Hash}  {entry.Message}\n{entry.Author} · {entry.RelativeTime}");
            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = _selectedIndex == index
                ? new Color(0.26f, 0.53f, 0.96f, 0.35f)
                : GUI.backgroundColor;

            if (GUILayout.Button(content, _entryStyle))
            {
                _selectedIndex = index;
            }

            GUI.backgroundColor = previousColor;
        }

        private void DrawFooter()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(GitLocalization.Tr("pullPicker.cancel"), GUILayout.Width(120f)))
                {
                    Close();
                    return;
                }

                using (new EditorGUI.DisabledScope(_selectedIndex < 0 || _selectedIndex >= _entries.Count))
                {
                    if (GUILayout.Button(GitLocalization.Tr("pullPicker.confirm"), GUILayout.Width(150f)))
                    {
                        ConfirmSelection();
                    }
                }
            }
        }

        private void ConfirmSelection()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _entries.Count)
            {
                return;
            }

            var hash = _entries[_selectedIndex].Hash;
            _onConfirm?.Invoke(hash);
            Close();
        }
    }
}

