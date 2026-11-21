using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UValidation.Editor
{
    /// <summary>
    /// Identifies saves whose source is the open Prefab Stage.
    /// </summary>
    /// <remarks>
    /// OnWillSaveAssets only reports the destination path. The Prefab Stage callback supplies the source root,
    /// which lets save enforcement validate that root without mistaking another object for the save candidate.
    /// </remarks>
    [InitializeOnLoad]
    internal static class PrefabStageSaveTracker
    {
        private static GameObject s_SavingRoot;
        private static string s_SavingPath;
        private static uint s_SaveVersion;

        static PrefabStageSaveTracker()
        {
            PrefabStage.prefabSaving += OnPrefabSaving;
            PrefabStage.prefabSaved += OnPrefabSaved;
        }

        /// <summary>
        /// Gets the Prefab Stage root currently being saved to the requested path.
        /// </summary>
        internal static bool TryGetSavingRoot(string prefabPath, out GameObject prefabRoot)
        {
            prefabRoot = s_SavingRoot;
            return prefabRoot != null && string.Equals(s_SavingPath, prefabPath, StringComparison.Ordinal);
        }

        private static void OnPrefabSaving(GameObject prefabRoot)
        {
            var prefabStage = PrefabStageUtility.GetPrefabStage(prefabRoot);
            if (prefabStage == null)
            {
                return;
            }

            s_SavingRoot = prefabRoot;
            s_SavingPath = prefabStage.assetPath;
            var saveVersion = ++s_SaveVersion;

            // A blocked save does not necessarily raise prefabSaved, so always discard the context afterward.
            EditorApplication.delayCall += () =>
            {
                if (s_SaveVersion == saveVersion)
                {
                    Clear();
                }
            };
        }

        private static void OnPrefabSaved(GameObject prefabRoot)
        {
            if (prefabRoot == s_SavingRoot)
            {
                Clear();
            }
        }

        private static void Clear()
        {
            s_SavingRoot = null;
            s_SavingPath = null;
        }
    }
}
