using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Fire.VersionControlAssistant
{
    internal sealed class GitTreeView : TreeView
    {
        private List<GitStatusEntry> _entries = new();

        public GitTreeView(TreeViewState state)
            : base(state)
        {
            showBorder = false;
            Reload();
        }

        public void SetEntries(IReadOnlyList<GitStatusEntry> entries)
        {
            _entries = entries?.ToList() ?? new List<GitStatusEntry>();
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            if (_entries.Count == 0)
            {
                root.children = new List<TreeViewItem>
                {
                    new TreeViewItem(1, 0, GitLocalization.Tr("tree.noChanges"))
                };
                return root;
            }

            var map = new Dictionary<string, TreeViewItem>
            {
                { string.Empty, root }
            };
            int nextId = 1;

            foreach (var entry in _entries)
            {
                var parts = entry.Path.Split('/');
                var currentPath = string.Empty;
                var parent = root;

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";

                    if (!map.TryGetValue(currentPath, out var node))
                    {
                        node = new TreeViewItem
                        {
                            id = nextId++,
                            depth = i,
                            displayName = i == parts.Length - 1
                                ? $"{part} ({entry.StatusLabel})"
                                : part
                        };

                        if (parent.children == null)
                        {
                            parent.children = new List<TreeViewItem>();
                        }

                        parent.children.Add(node);
                        map[currentPath] = node;
                    }

                    parent = node;
                }
            }

            if (root.children != null)
            {
                SortRecursively(root);
            }

            return root;
        }

        private static void SortRecursively(TreeViewItem node)
        {
            if (node.children == null)
            {
                return;
            }

            node.children = node.children
                .OrderByDescending(child => child.children != null && child.children.Count > 0)
                .ThenBy(child => child.displayName)
                .ToList();

            foreach (var child in node.children)
            {
                SortRecursively(child);
            }
        }

        protected override void SearchChanged(string newSearch)
        {
            base.SearchChanged(newSearch);
            Reload();
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            if (string.IsNullOrEmpty(searchString))
            {
                return rows;
            }

            return rows.Where(item =>
                item.depth >= 0 &&
                item.displayName.ToLowerInvariant().Contains(searchString.ToLowerInvariant())).ToList();
        }
    }
}

