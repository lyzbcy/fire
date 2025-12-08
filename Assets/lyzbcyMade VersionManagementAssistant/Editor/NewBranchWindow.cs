using System;
using UnityEditor;
using UnityEngine;

namespace Fire.VersionControlAssistant
{
    public sealed class NewBranchWindow : EditorWindow
    {
        private string _branchName = "feature/";
        private VersionControlAssistantWindow _parent;

        public static void ShowWindow(VersionControlAssistantWindow parent)
        {
            var window = GetWindow<NewBranchWindow>(true, "Create New Branch", true);
            window._parent = parent;
            window.minSize = new Vector2(300, 80);
            window.maxSize = new Vector2(300, 80);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            // Simple style for input
            EditorGUILayout.Space(10);
            
            GUILayout.Label("Branch Name:", EditorStyles.boldLabel);
            GUI.SetNextControlName("BranchNameInput");
            _branchName = EditorGUILayout.TextField(_branchName);

            // Focus field on start
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.FocusTextInControl("BranchNameInput");
            }

            EditorGUILayout.Space(10);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }

                if (GUILayout.Button("Create", GUILayout.Width(60)))
                {
                    CreateBranch();
                }
            }

            // Handle Enter key
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                CreateBranch();
                Event.current.Use();
            }
        }

        private void CreateBranch()
        {
            if (string.IsNullOrWhiteSpace(_branchName))
            {
                EditorUtility.DisplayDialog("Error", "Branch name cannot be empty.", "OK");
                return;
            }

            _parent?.CreateBranchCallback(_branchName.Trim());
            Close();
        }
    }
}
