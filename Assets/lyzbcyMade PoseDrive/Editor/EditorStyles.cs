using System;
using UnityEditor;
using UnityEngine;

namespace PoseDrive.Editor
{
    /// <summary>
    /// 模仿焦点优化助手的编辑器样式集合。
    /// </summary>
    public static class EditorStylesLibrary
    {
        private static Texture2D _cardTexture;
        private static Texture2D _headerTexture;

        private static GUIStyle _title;
        private static GUIStyle _subtitle;
        private static GUIStyle _body;
        private static GUIStyle _secondary;
        private static GUIStyle _container;
        private static GUIStyle _card;
        private static GUIStyle _sectionTitle;
        private static GUIStyle _toolbarButton;

        public static GUIStyle Container => _container ??= new GUIStyle
        {
            padding = new RectOffset(18, 18, 14, 14)
        };

        public static GUIStyle Title => _title ??= CreateLabel(FontStyle.Bold, 18, Color.white);
        public static GUIStyle Subtitle => _subtitle ??= CreateLabel(FontStyle.Normal, 12, new Color(0.7f, 0.7f, 0.7f));
        public static GUIStyle Body => _body ??= CreateLabel(FontStyle.Normal, 13, EditorGUIUtility.isProSkin ? Color.white : Color.black);
        public static GUIStyle Secondary => _secondary ??= CreateLabel(FontStyle.Normal, 12, EditorGUIUtility.isProSkin ? new Color(0.75f, 0.75f, 0.75f) : new Color(0.4f, 0.4f, 0.4f));
        public static GUIStyle SectionTitle => _sectionTitle ??= CreateLabel(FontStyle.Bold, 14, EditorGUIUtility.isProSkin ? Color.white : Color.black);

        public static GUIStyle Card
        {
            get
            {
                if (_card == null)
                {
                    _card = new GUIStyle("HelpBox")
                    {
                        padding = new RectOffset(16, 16, 12, 16),
                        margin = new RectOffset(0, 0, 8, 8)
                    };
                }

                if (_cardTexture != null)
                {
                    _card.normal.background = _cardTexture;
                }

                return _card;
            }
        }

        public static GUIStyle ToolbarButton => _toolbarButton ??= new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            padding = new RectOffset(10, 10, 4, 4),
            margin = new RectOffset(2, 2, 2, 2),
            fixedHeight = 30,
            imagePosition = ImagePosition.ImageLeft
        };

        public static void DrawHeader(string title, string subtitle, Action buttons = null)
        {
            EnsureHeaderTexture();

            Rect headerRect = EditorGUILayout.GetControlRect(false, 64);
            GUI.DrawTexture(headerRect, _headerTexture);

            Rect titleRect = new Rect(headerRect.x + 16, headerRect.y + 10, headerRect.width - 32, 24);
            Rect subtitleRect = new Rect(headerRect.x + 16, headerRect.y + 34, headerRect.width - 32, 20);

            GUI.Label(titleRect, title, Title);
            GUI.Label(subtitleRect, subtitle, Subtitle);

            if (buttons != null)
            {
                GUILayout.Space(-10);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                buttons.Invoke();
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            DrawDivider();
        }

        public static void DrawDivider(float space = 6f)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, space);
            rect.height = 1f;
            EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.2f));
        }

        public static IDisposable CardScope(string title = null, string subtitle = null)
                {
            return new CardScopeInternal(title, subtitle);
        }

        private static void EnsureHeaderTexture()
        {
            if (_headerTexture != null)
            {
                return;
            }

            _headerTexture = new Texture2D(1, 2);
            _headerTexture.SetPixels(new[]
            {
                new Color(0.12f, 0.17f, 0.25f),
                new Color(0.18f, 0.26f, 0.37f)
            });
            _headerTexture.wrapMode = TextureWrapMode.Clamp;
            _headerTexture.Apply();

            _cardTexture = new Texture2D(1, 1);
            _cardTexture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? new Color(0.16f, 0.16f, 0.17f) : new Color(0.95f, 0.95f, 0.96f));
            _cardTexture.Apply();
            if (_card != null)
            {
                _card.normal.background = _cardTexture;
            }
        }

        private static GUIStyle CreateLabel(FontStyle style, int size, Color color)
        {
            return new GUIStyle(EditorStyles.label)
                    {
                fontStyle = style,
                fontSize = size,
                normal = { textColor = color },
                wordWrap = true
            };
        }

        private readonly struct CardScopeInternal : IDisposable
        {
            public CardScopeInternal(string title, string subtitle)
            {
                EnsureHeaderTexture();
                EditorGUILayout.BeginVertical(Card);
                if (!string.IsNullOrEmpty(title))
                {
                    GUILayout.Label(title, SectionTitle);
            }

                if (!string.IsNullOrEmpty(subtitle))
                {
                    GUILayout.Label(subtitle, Secondary);
                }

                if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(subtitle))
                {
                    GUILayout.Space(4);
                    DrawDivider(4);
    }
}

            public void Dispose()
            {
                EditorGUILayout.EndVertical();
            }
        }
    }
}
