using UnityEditor;
using UnityEngine;

namespace UValidation.Editor
{
    /// <summary>
    /// Validates queued assets once Unity finishes importing their saved contents.
    /// </summary>
    internal sealed class PendingAssetValidationPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var importedAsset in importedAssets)
            {
                if (!PendingAssetValidation.TryConsume(importedAsset, out var pendingPath, out var isNew))
                {
                    continue;
                }

                ValidateImportedAsset(pendingPath, isNew, false);
            }

            foreach (var deletedAsset in deletedAssets)
            {
                PendingAssetValidation.TryConsume(deletedAsset, out _, out _);
            }

            foreach (var movedFromAssetPath in movedFromAssetPaths)
            {
                PendingAssetValidation.TryConsume(movedFromAssetPath, out _, out _);
            }
        }

        private static void ValidateImportedAsset(string path, bool isNew, bool isRetry)
        {
            if (!UValidationProjectSettings.Instance.BlockInvalidSaves)
            {
                return;
            }

            var validationState = ValidationHelper.ValidateImportedAssetAtPath(path, true);
            if (validationState == AssetValidationState.Valid)
            {
                return;
            }

            if (validationState == AssetValidationState.ValidateAfterImport && !isRetry)
            {
                EditorApplication.delayCall += () => ValidateImportedAsset(path, isNew, true);
                return;
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            Debug.LogError(GetFailureMessage(path, validationState, isNew), asset);
            EditorApplication.RepaintProjectWindow();

            if (!Application.isBatchMode && asset != null)
            {
                EditorGUIUtility.PingObject(asset);
            }
        }

        private static string GetFailureMessage(string path, AssetValidationState validationState, bool isNew)
        {
            if (validationState != AssetValidationState.Invalid)
            {
                var description = isNew ? "newly created asset" : "saved asset";
                return $"UValidation could not read {description} '{path}' after Unity imported it. Its validation state is unknown.";
            }

            return isNew
                ? $"UValidation found invalid data in newly created asset '{path}'. Fix the reported errors before saving it again."
                : $"UValidation found invalid data in '{path}' after Unity saved it. Saves such as Apply Overrides " +
                  "or Save As onto an existing file are written before UValidation can block them, so fix the " +
                  "reported errors or undo the change.";
        }
    }
}
