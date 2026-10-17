using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UValidation.Editor
{
    /// <summary>
    /// Caches validation results for Editor GUI callbacks, which run several times per frame for every visible item.
    /// </summary>
    /// <remarks>
    /// Results are discarded whenever the Editor reports a change to objects, the hierarchy, or the project,
    /// and after undo or redo. The cache is bypassed in Play Mode, where gameplay code changes objects
    /// without reporting it.
    /// </remarks>
    [InitializeOnLoad]
    internal static class ValidationResultCache
    {
        private static readonly Dictionary<UnityEngine.Object, bool> s_ObjectResults = new();
        private static readonly Dictionary<string, bool> s_AssetResults = new(StringComparer.Ordinal);

        static ValidationResultCache()
        {
            ObjectChangeEvents.changesPublished += OnChangesPublished;
            Undo.undoRedoPerformed += Clear;
            EditorApplication.hierarchyChanged += Clear;
            EditorApplication.projectChanged += Clear;
            EditorApplication.playModeStateChanged += _ => Clear();
        }

        /// <summary>
        /// Gets whether a GameObject, prefab asset, or other Unity object passes validation.
        /// </summary>
        /// <param name="target"> The object to validate. </param>
        /// <returns> True if the object is valid; otherwise, false. </returns>
        /// <remarks>
        /// A GameObject is validated with its components. A prefab asset GameObject is validated as the whole prefab.
        /// </remarks>
        internal static bool IsObjectValid(UnityEngine.Object target)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return ValidateObject(target);
            }

            if (!s_ObjectResults.TryGetValue(target, out var isValid))
            {
                isValid = ValidateObject(target);
                s_ObjectResults[target] = isValid;
            }

            return isValid;
        }

        /// <summary>
        /// Gets whether the asset at a path would pass save validation.
        /// </summary>
        /// <param name="assetPath"> The asset path to validate. </param>
        /// <returns> True unless the asset contains invalid data. </returns>
        internal static bool IsAssetValid(string assetPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return ValidateAsset(assetPath);
            }

            if (!s_AssetResults.TryGetValue(assetPath, out var isValid))
            {
                isValid = ValidateAsset(assetPath);
                s_AssetResults[assetPath] = isValid;
            }

            return isValid;
        }

        /// <summary>
        /// Discards every cached result.
        /// </summary>
        internal static void Clear()
        {
            s_ObjectResults.Clear();
            s_AssetResults.Clear();
        }

        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            Clear();
        }

        private static bool ValidateObject(UnityEngine.Object target)
        {
            if (target is not GameObject gameObject)
            {
                return ValidationHelper.IsScriptValid(target, false);
            }

            // The Inspector shows a prefab asset through its root, but the asset is invalid when any object in it is.
            return EditorUtility.IsPersistent(gameObject)
                ? ValidationHelper.IsPrefabValidAtPath(AssetDatabase.GetAssetPath(gameObject), false)
                : ValidationHelper.IsGameObjectValid(gameObject, false);
        }

        private static bool ValidateAsset(string assetPath)
        {
            return ValidationHelper.ValidateAssetAtPathForSave(assetPath, false) != AssetValidationState.Invalid;
        }
    }
}
