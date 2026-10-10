using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UValidation.Editor
{
    /// <summary>
    /// Tracks newly created assets until Unity makes them available through the AssetDatabase.
    /// </summary>
    internal static class PendingAssetValidation
    {
        private static readonly HashSet<string> s_Paths = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Queues a supported asset path for validation after import.
        /// </summary>
        /// <param name="path"> The asset path reported by Unity. </param>
        internal static void Queue(string path)
        {
            var normalizedPath = Normalize(path);
            if (IsSupported(normalizedPath))
            {
                s_Paths.Add(normalizedPath);
            }
        }

        /// <summary>
        /// Removes a queued path when its imported asset becomes available.
        /// </summary>
        /// <param name="path"> The imported asset path. </param>
        /// <param name="normalizedPath"> The normalized path that was queued. </param>
        /// <returns> True if the asset was awaiting validation; otherwise, false. </returns>
        internal static bool TryConsume(string path, out string normalizedPath)
        {
            normalizedPath = Normalize(path);
            return s_Paths.Remove(normalizedPath);
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
    /// Validates queued assets once Unity finishes importing their initial contents.
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
                if (!PendingAssetValidation.TryConsume(importedAsset, out var pendingPath))
                {
                    continue;
                }

                ValidateImportedAsset(pendingPath, false);
            }
        }

        private static void ValidateImportedAsset(string path, bool isRetry)
        {
            if (!ValidationEnforcement.BlockInvalidSaves)
            {
                return;
            }

            var validationState = ValidationHelper.ValidateAssetAtPathForSave(path, true);
            if (validationState == AssetValidationState.Valid)
            {
                return;
            }

            if (validationState == AssetValidationState.PendingCreation && !isRetry)
            {
                EditorApplication.delayCall += () => ValidateImportedAsset(path, true);
                return;
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            var message = validationState == AssetValidationState.Invalid
                ? $"UValidation found invalid data in newly created asset '{path}'. Fix the reported errors before saving it again."
                : $"UValidation could not read newly created asset '{path}' after Unity imported it. Its validation state is unknown.";

            Debug.LogError(message, asset);
            EditorApplication.RepaintProjectWindow();

            if (!Application.isBatchMode && asset != null)
            {
                EditorGUIUtility.PingObject(asset);
            }
        }
    }
}
