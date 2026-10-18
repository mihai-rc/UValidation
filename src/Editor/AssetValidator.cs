using System;
using System.Collections.Generic;
using UnityEditor;

namespace UValidation.Editor
{
    /// <summary>
    /// Validates assets before saving them in the Unity editor.
    /// Ensures scenes, prefabs, and scriptable objects meet validation requirements and prevents saving invalid assets.
    /// </summary>
    [InitializeOnLoad]
    public class AssetValidator : AssetModificationProcessor
    {
        static AssetValidator()
        {
            EditorApplication.wantsToQuit += OnWantsToQuit;
        }

        /// <summary>
        /// Gets or sets whether a blocked save shows the validation failure dialog.
        /// </summary>
        /// <remarks>
        /// Editor tests turn this off so they can exercise save enforcement without a modal dialog.
        /// </remarks>
        internal static bool PromptOnBlockedSave { get; set; } = true;

        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (!UValidationProjectSettings.Instance.BlockInvalidSaves)
            {
                return paths;
            }

            var validPaths = new List<string>(paths);
            foreach (var path in paths)
            {
                var validationState = ValidationHelper.ValidateAssetAtPathForSave(path, true);
                switch (validationState)
                {
                    case AssetValidationState.Valid:
                        break;
                    case AssetValidationState.Invalid:
                        PromptValidationFailed(path);
                        validPaths.Remove(path);
                        break;
                    case AssetValidationState.ValidateAfterImport:
                        PendingAssetValidation.Queue(path);
                        break;
                }
            }

            return validPaths.ToArray();
        }

        private static void OnWillCreateAsset(string assetName)
        {
            if (UValidationProjectSettings.Instance.BlockInvalidSaves)
            {
                PendingAssetValidation.Queue(assetName);
            }
        }

        private static bool OnWantsToQuit()
        {
            if (!UValidationProjectSettings.Instance.BlockEditorQuit)
            {
                return true;
            }

            var dirtyScriptableObjects = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.ScriptableObject>();
            foreach (var so in dirtyScriptableObjects)
            {
                if (!EditorUtility.IsDirty(so))
                {
                    continue;
                }

                var assetPath = AssetDatabase.GetAssetPath(so);
                if (string.IsNullOrEmpty(assetPath) ||
                    !assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!IsScriptableObjectValidAtPath(assetPath))
                {
                    return false;
                }
            }

            return true;
        }

        private static void PromptValidationFailed(string path)
        {
            if (!PromptOnBlockedSave)
            {
                return;
            }

            if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                ValidationFailedDialog.PromptSceneFailed();
                return;
            }

            if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                ValidationFailedDialog.PromptPrefabFailed();
                return;
            }

            ValidationFailedDialog.PromptScriptableObjectFailed();
        }

        private static bool IsScriptableObjectValidAtPath(string path)
        {
            var isValid = ValidationHelper.IsScriptableObjectValidAtPath(path, true);
            if (!isValid)
            {
                ValidationFailedDialog.PromptScriptableObjectFailed();
            }

            return isValid;
        }
    }
}
