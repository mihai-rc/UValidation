using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UValidation.Editor
{
    /// <summary>
    /// Tracks saved assets whose contents can only be validated once Unity imports them.
    /// </summary>
    internal static class PendingAssetValidation
    {
        // Maps each queued path to whether the asset did not exist yet when it was queued.
        private static readonly Dictionary<string, bool> s_Paths = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Queues a supported asset path for validation after import.
        /// </summary>
        /// <param name="path"> The asset path reported by Unity. </param>
        internal static void Queue(string path)
        {
            var normalizedPath = Normalize(path);
            if (!IsSupported(normalizedPath))
            {
                return;
            }

            var isNew = !AssetDatabase.AssetPathExists(normalizedPath);
            s_Paths[normalizedPath] = isNew || (s_Paths.TryGetValue(normalizedPath, out var wasNew) && wasNew);
        }

        /// <summary>
        /// Removes a queued path when its imported asset becomes available.
        /// </summary>
        /// <param name="path"> The imported asset path. </param>
        /// <param name="normalizedPath"> The normalized path that was queued. </param>
        /// <param name="isNew"> Whether the asset did not exist yet when it was queued. </param>
        /// <returns> True if the asset was awaiting validation; otherwise, false. </returns>
        internal static bool TryConsume(string path, out string normalizedPath, out bool isNew)
        {
            normalizedPath = Normalize(path);
            return s_Paths.Remove(normalizedPath, out isNew);
        }

        private static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var normalizedPath = path.Replace('\\', '/');
            if (normalizedPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath.Substring(0, normalizedPath.Length - ".meta".Length);
            }

            return normalizedPath;
        }

        private static bool IsSupported(string path)
        {
            return path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase);
        }
    }

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
        }

        private static void ValidateImportedAsset(string path, bool isNew, bool isRetry)
        {
            if (!ValidationEnforcement.BlockInvalidSaves)
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
                : $"UValidation found invalid data in '{path}' after Unity saved it. Changes such as Apply Overrides " +
                  "are written before UValidation can block them, so fix the reported errors or undo the change.";
        }
    }
}
