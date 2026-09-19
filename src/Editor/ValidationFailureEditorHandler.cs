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
            if (!EditorValidationToggle.IsValidationEnabled)
            {
                return;
            }

            var gameObject = EditorUtility.EntityIdToObject((EntityId)instanceId) as GameObject;
            if (gameObject == null)
            {
                return;
            }

            if (ValidationHelper.IsGameObjectValid(gameObject, false))
            {
                return;
            }

            DrawRedLabel(selectionRect, gameObject.name, false);
        }

        private static void OnProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            if (!EditorValidationToggle.IsValidationEnabled)
            {
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset"))
            {
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset != null && !ValidationHelper.IsScriptValid(asset, false))
            {
                selectionRect.x -= 3;
                selectionRect.y -= 1;
                DrawRedLabel(selectionRect, asset.name, true);
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!EditorValidationToggle.IsValidationEnabled)
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
            if (!EditorValidationToggle.IsValidationEnabled)
            {
                return;
            }

            if (editor.target is UnityEngine.Object target && !ValidationHelper.IsScriptValid(target, false))
            {
                EditorGUILayout.HelpBox("Validation Failed: This object contains invalid data. It cannot be saved or used in Play Mode.", MessageType.Error);
            }
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