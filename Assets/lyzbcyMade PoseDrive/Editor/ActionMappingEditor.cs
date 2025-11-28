using System.Collections.Generic;
using PoseDrive.Runtime.Input;
using UnityEditor;
using UnityEngine;

namespace PoseDrive.Editor
{
    /// <summary>
    /// 动作映射编辑器，支持多语言。
    /// </summary>
    public class ActionMappingEditor : EditorWindow
    {
        private ActionMappingAsset _asset;
        private Vector2 _scroll;

        [MenuItem("Tools/PoseDrive/动作映射", priority = 215)]
        public static void Open()
        {
            ActionMappingEditor window = GetWindow<ActionMappingEditor>();
            window.UpdateTitle();
            window.minSize = new Vector2(420, 480);
        }

        private void OnEnable()
        {
            PoseDriveLocalization.LanguageChanged += HandleLanguageChanged;
            UpdateTitle();
        }

        private void OnDisable()
        {
            PoseDriveLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged()
        {
            UpdateTitle();
            Repaint();
        }

        private void UpdateTitle()
        {
            titleContent = new GUIContent(PoseDriveLocalization.Tr("module.mapping"));
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope(EditorStylesLibrary.Container))
            {
                EditorStylesLibrary.DrawHeader(
                    PoseDriveLocalization.Tr("module.mapping"),
                    PoseDriveLocalization.Tr("mapping.editor.description"));

                using (EditorStylesLibrary.CardScope(
                           PoseDriveLocalization.Tr("module.mapping"),
                           PoseDriveLocalization.Tr("mapping.editor.description")))
                {
                    EditorGUI.BeginChangeCheck();
                    _asset = (ActionMappingAsset)EditorGUILayout.ObjectField(
                        PoseDriveLocalization.Tr("mapping.editor.asset"),
                        _asset,
                        typeof(ActionMappingAsset),
                        false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EnsureAssetList();
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(PoseDriveLocalization.Tr("mapping.editor.create"), GUILayout.Height(26)))
                    {
                        CreateAsset();
                    }

                    GUI.enabled = _asset != null;
                    if (GUILayout.Button(PoseDriveLocalization.Tr("mapping.editor.save"), GUILayout.Height(26)))
                    {
                        EditorUtility.SetDirty(_asset);
                        AssetDatabase.SaveAssets();
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                if (_asset == null)
                {
                    GUILayout.Label(PoseDriveLocalization.Tr("mapping.editor.empty"), EditorStylesLibrary.Secondary);
                }
                else
                {
                    _scroll = EditorGUILayout.BeginScrollView(_scroll, true, true);
                    for (int i = 0; i < _asset.Bindings.Count; i++)
                    {
                        DrawBindingCard(i);
                    }
                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(10);
                    if (GUILayout.Button(PoseDriveLocalization.Tr("mapping.binding.add"), GUILayout.Height(32)))
                    {
                        _asset.Bindings.Add(new GestureBinding
                        {
                            GestureName = "NewGesture",
                            Key = VirtualKey.Keyboard(KeyCode.Space),
                            Hold = false
                        });
                        EditorUtility.SetDirty(_asset);
                    }
                }
            }
        }

        private void DrawBindingCard(int index)
        {
            GestureBinding binding = _asset.Bindings[index];
            string title = $"{PoseDriveLocalization.Tr("mapping.binding.title")} {index + 1}";
            using (EditorStylesLibrary.CardScope(title))
            {
                GUILayout.BeginHorizontal();
                binding.GestureName = EditorGUILayout.TextField(
                    PoseDriveLocalization.Tr("mapping.binding.gesture"), binding.GestureName);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(PoseDriveLocalization.Tr("mapping.binding.delete"), GUILayout.Width(60)))
            {
                _asset.Bindings.RemoveAt(index);
                    EditorUtility.SetDirty(_asset);
                return;
            }
                GUILayout.EndHorizontal();

                EditorStylesLibrary.DrawDivider();

                binding.Key.Type = (VirtualKeyType)EditorGUILayout.EnumPopup(
                    PoseDriveLocalization.Tr("mapping.binding.type"), binding.Key.Type);
            switch (binding.Key.Type)
            {
                case VirtualKeyType.Keyboard:
                        binding.Key.KeyboardKey = (KeyCode)EditorGUILayout.EnumPopup("Key", binding.Key.KeyboardKey);
                    break;
                case VirtualKeyType.Mouse:
                        binding.Key.MouseButton = EditorGUILayout.IntSlider("Mouse", binding.Key.MouseButton, 0, 7);
                    break;
                case VirtualKeyType.Gamepad:
                        binding.Key.GamepadButton = EditorGUILayout.TextField("Gamepad", binding.Key.GamepadButton);
                    break;
            }

                binding.Hold = EditorGUILayout.Toggle(PoseDriveLocalization.Tr("mapping.binding.hold"), binding.Hold);
                GUILayout.Label(PoseDriveLocalization.Tr("mapping.binding.holdHint"), EditorStylesLibrary.Secondary);

            _asset.Bindings[index] = binding;
                EditorUtility.SetDirty(_asset);
            }
        }

        private void EnsureAssetList()
        {
            if (_asset != null && _asset.Bindings == null)
            {
                _asset.Bindings = new List<GestureBinding>();
                EditorUtility.SetDirty(_asset);
            }
        }

        private void CreateAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                PoseDriveLocalization.Tr("mapping.editor.newTitle"),
                "ActionMappingAsset",
                "asset",
                PoseDriveLocalization.Tr("mapping.editor.newDesc"));
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ActionMappingAsset asset = CreateInstance<ActionMappingAsset>();
            asset.Bindings = new List<GestureBinding>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _asset = asset;
        }
    }
}
