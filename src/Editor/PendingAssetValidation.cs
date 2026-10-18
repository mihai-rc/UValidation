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
        private readonly struct PendingPath
        {
            internal PendingPath(bool isNew)
            {
                IsNew = isNew;
            }

            internal bool IsNew { get; }
        }

        private static readonly Dictionary<string, PendingPath> s_Paths = new(StringComparer.OrdinalIgnoreCase);
        private static bool s_DiscardScheduled;

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
            if (s_Paths.TryGetValue(normalizedPath, out var pendingPath))
            {
                isNew |= pendingPath.IsNew;
            }

            s_Paths[normalizedPath] = new PendingPath(isNew);

            ScheduleDiscard();
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
            if (!s_Paths.Remove(normalizedPath, out var pendingPath))
            {
                isNew = false;
                return false;
            }

            isNew = pendingPath.IsNew;
            return true;
        }

        /// <summary>
        /// Gets whether a path is currently awaiting post-import validation.
        /// </summary>
        internal static bool Contains(string path)
        {
            return s_Paths.ContainsKey(Normalize(path));
        }

        private static void ScheduleDiscard()
        {
            if (s_DiscardScheduled)
            {
                return;
            }

            s_DiscardScheduled = true;
            EditorApplication.update += DiscardPendingPaths;
        }

        private static void DiscardPendingPaths()
        {
            EditorApplication.update -= DiscardPendingPaths;
            s_DiscardScheduled = false;

            // Imports caused by a save complete before the next editor update. Anything still pending now came
            // from a canceled or failed write and must not be inherited by a later unrelated import.
            s_Paths.Clear();
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
