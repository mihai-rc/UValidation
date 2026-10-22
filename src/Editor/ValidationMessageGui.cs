using UnityEditor;
using UnityEngine;

namespace UValidation.Editor
{
    /// <summary>
    /// Provides shared sizing for validation messages drawn through Editor GUI APIs.
    /// </summary>
    internal static class ValidationMessageGui
    {
        private const float k_HorizontalPadding = 8.0f;
        private const float k_IconSpacing = 8.0f;
        private const float k_VerticalPadding = 8.0f;

        private static GUIStyle s_MessageTextStyle;
        private static bool s_StyleUsesProSkin;

        internal const int FontSize = 12;
        internal const float IconSize = 32.0f;
        internal const float MinimumHeight = 48.0f;

        /// <summary>
        /// Draws a validation HelpBox with readable IMGUI text and a full-size severity icon.
        /// </summary>
        /// <param name="position"> The rectangle occupied by the HelpBox. </param>
        /// <param name="message"> The message displayed by the HelpBox. </param>
        /// <param name="messageType"> The severity icon displayed by the HelpBox. </param>
        internal static void DrawHelpBox(Rect position, string message, MessageType messageType)
        {
            GetContentRects(position, out var iconRect, out var textRect);
            GUI.Box(position, GUIContent.none, EditorStyles.helpBox);
            GUI.DrawTexture(iconRect, GetIcon(messageType), ScaleMode.ScaleToFit, true);
            GUI.Label(textRect, message, GetMessageTextStyle());
        }

        /// <summary>
        /// Gets an IMGUI HelpBox height that accommodates both its message and icon.
        /// </summary>
        /// <param name="width"> The available HelpBox width. </param>
        /// <param name="message"> The message displayed by the HelpBox. </param>
        /// <returns> The required HelpBox height. </returns>
        internal static float GetHelpBoxHeight(float width, string message)
        {
            var textWidth = Mathf.Max(
                1.0f,
                width - k_HorizontalPadding * 2.0f - IconSize - k_IconSpacing);
            var textHeight = GetMessageTextStyle().CalcHeight(new GUIContent(message), textWidth);

            return Mathf.Max(
                MinimumHeight,
                textHeight + k_VerticalPadding * 2.0f);
        }

        /// <summary>
        /// Gets the icon and text rectangles used inside an IMGUI validation HelpBox.
        /// </summary>
        /// <param name="position"> The full HelpBox rectangle. </param>
        /// <param name="iconRect"> The centered severity-icon rectangle. </param>
        /// <param name="textRect"> The remaining padded message rectangle. </param>
        internal static void GetContentRects(Rect position, out Rect iconRect, out Rect textRect)
        {
            iconRect = new Rect(
                position.x + k_HorizontalPadding,
                position.y + (position.height - IconSize) * 0.5f,
                IconSize,
                IconSize);

            textRect = new Rect(
                iconRect.xMax + k_IconSpacing,
                position.y + k_VerticalPadding,
                Mathf.Max(0.0f, position.xMax - k_HorizontalPadding - iconRect.xMax - k_IconSpacing),
                Mathf.Max(0.0f, position.height - k_VerticalPadding * 2.0f));
        }

        private static Texture GetIcon(MessageType messageType)
        {
            return EditorGUIUtility.IconContent(messageType switch
            {
                MessageType.Info    => "console.infoicon",
                MessageType.Warning => "console.warnicon",
                _                   => "console.erroricon"
            }).image;
        }

        private static GUIStyle GetMessageTextStyle()
        {
            if (s_MessageTextStyle == null || s_StyleUsesProSkin != EditorGUIUtility.isProSkin)
            {
                s_StyleUsesProSkin = EditorGUIUtility.isProSkin;
                var sourceStyle = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector)?.label;
                s_MessageTextStyle = sourceStyle != null
                    ? new GUIStyle(sourceStyle)
                    : new GUIStyle();
                s_MessageTextStyle.alignment = TextAnchor.MiddleLeft;
                s_MessageTextStyle.fontSize = FontSize;
                s_MessageTextStyle.padding = new RectOffset();
                s_MessageTextStyle.wordWrap = true;
            }

            return s_MessageTextStyle;
        }
    }
}
