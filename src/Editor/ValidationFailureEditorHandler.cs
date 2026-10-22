using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UValidation.Editor
{
    /// <summary>
    /// Handles coloring in the hierarchy window for invalid objects and prevents entering play mode
    /// if the active scene, prefab or asset contains validation errors.
    /// </summary>
    [InitializeOnLoad]
    internal class ValidationFailureEditorHandler
    {
        private static GUIStyle s_HierarchyLabelStyle;
        private static GUIStyle s_AssetLabelStyle;
        private static bool s_StylesUseProSkin;

        static ValidationFailureEditorHandler()
        {
            EditorEntityIdCompatibility.RegisterHierarchyWindowItemCallback(OnHierarchyWindowItemOnGUI);
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemOnGUI;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
        }

        /// <summary>
        /// Counts the Inspector header targets that fail validation.
        /// </summary>
        /// <param name="targets"> The targets of the editor whose header is drawn. </param>
        /// <returns> The number of invalid targets. </returns>
        internal static int CountInvalidHeaderTargets(UnityEngine.Object[] targets)
        {
            var invalidCount = 0;
            foreach (var target in targets)
            {
                // A prefab asset draws its importer header as well as its own; validating both would show two boxes.
                if (target == null || target is AssetImporter)
                {
                    continue;
                }

                if (!ValidationResultCache.IsObjectValid(target))
                {
                    invalidCount++;
                }
            }

            return invalidCount;
        }

        /// <summary>
        /// Gets whether a Project-window asset participates in validation feedback.
        /// </summary>
        /// <param name="path"> Project-relative asset path. </param>
        /// <returns> True for scenes, prefabs, and ScriptableObject asset files. </returns>
        internal static bool IsSupportedProjectAssetPath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   (path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase));
        }

        private static void OnHierarchyWindowItemOnGUI(EntityId entityId, Rect selectionRect)
        {
            var gameObject = EditorUtility.EntityIdToObject(entityId) as GameObject;
            if (gameObject == null)
            {
                return;
            }

            if (ValidationResultCache.IsObjectValid(gameObject))
            {
                return;
            }

            DrawRedLabel(selectionRect, gameObject.name, false);
        }

        private static void OnProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) ||
                !IsSupportedProjectAssetPath(path) ||
                ValidationResultCache.IsAssetValid(path))
            {
                return;
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset != null)
            {
                selectionRect = EditorGuiCompatibility.GetProjectWindowLabelRect(selectionRect);
                DrawRedLabel(selectionRect, asset.name, true);
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!UValidationProjectSettings.Instance.BlockPlayMode)
            {
                return;
            }

            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (ValidationHelper.IsSceneValid(ref scene, true))
            {
                return;
            }

            ValidationFailedDialog.PromptPlayModeFailed();
            EditorApplication.isPlaying = false;
        }

        private static void OnFinishedDefaultHeaderGUI(UnityEditor.Editor editor)
        {
            var targets = editor.targets;
            var invalidCount = CountInvalidHeaderTargets(targets);
            if (invalidCount == 0)
            {
                return;
            }

            var message = targets.Length == 1
                ? "Validation Failed: This object contains invalid data."
                : $"Validation Failed: {invalidCount} of {targets.Length} selected objects contain invalid data.";
            var height = ValidationMessageGui.GetHelpBoxHeight(
                EditorGUIUtility.currentViewWidth,
                message);
            var position = EditorGUILayout.GetControlRect(false, height);
            ValidationMessageGui.DrawHelpBox(position, message, MessageType.Error);
        }

        private static void DrawRedLabel(Rect selectionRect, string labelName, bool isAsset)
        {
            EditorGUI.LabelField(selectionRect, labelName, GetRedLabelStyle(isAsset));
        }

        private static GUIStyle GetRedLabelStyle(bool isAsset)
        {
            if (s_HierarchyLabelStyle == null || s_StylesUseProSkin != EditorGUIUtility.isProSkin)
            {
                s_StylesUseProSkin = EditorGUIUtility.isProSkin;
                s_HierarchyLabelStyle = CreateRedLabelStyle(false);
                s_AssetLabelStyle = CreateRedLabelStyle(true);
            }

            return isAsset ? s_AssetLabelStyle : s_HierarchyLabelStyle;
        }

        private static GUIStyle CreateRedLabelStyle(bool isAsset)
        {
            return new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.red },
                padding = isAsset
                    ? EditorGuiCompatibility.CreateProjectWindowLabelPadding()
                    : new RectOffset(18, 2, 0, 1)
            };
        }
    }
}
