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
    public class ValidationFailureEditorHandler
    {
        static ValidationFailureEditorHandler()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemOnGUI;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
        }

        private static void OnHierarchyWindowItemOnGUI(int instanceId, Rect selectionRect)
        {
            var gameObject = EditorUtility.EntityIdToObject((EntityId)instanceId) as GameObject;
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
                !path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase) ||
                ValidationResultCache.IsAssetValid(path))
            {
                return;
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset != null)
            {
                selectionRect.x -= 3;
                selectionRect.y -= 1;
                DrawRedLabel(selectionRect, asset.name, true);
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!ValidationEnforcement.BlockPlayMode)
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
            EditorGUILayout.HelpBox(message, MessageType.Error);
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

        private static void DrawRedLabel(Rect selectionRect, string labelName, bool isAsset)
        {
            var textStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.red },
                padding = new RectOffset
                {
                    left = isAsset ? 21 : 18,
                    right = 2,
                    top = isAsset ? 1 : 0,
                    bottom = 1
                }
            };

            EditorGUI.LabelField(selectionRect, labelName, textStyle);
        }
    }
}
