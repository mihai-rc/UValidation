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

        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (!EditorValidationToggle.IsValidationEnabled)
            {
                return paths;
            }

            var validPaths = new List<string>(paths);
            foreach (var path in paths)
            {
                var isValid = path switch
                {
                    _ when path.EndsWith(".unity") => IsSceneValidAtPath(path),
                    _ when path.EndsWith(".prefab") => IsPrefabValidAtPath(path),
                    _ when path.EndsWith(".asset") => IsScriptableObjectValidAtPath(path),
                    _ => true
                };

                if (!isValid)
                {
                    validPaths.Remove(path);
                }
            }

            return validPaths.ToArray();
        }

        private static bool OnWantsToQuit()
        {
            if (!EditorValidationToggle.IsValidationEnabled)
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
                if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".asset"))
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

        private static bool IsSceneValidAtPath(string path)
        {
            var isValid = ValidationHelper.IsSceneValidAtPath(path, true);
            if (!isValid)
            {
                ValidationFailedDialog.PromptSceneFailed();
            }

            return isValid;
        }

        private static bool IsPrefabValidAtPath(string path)
        {
            var isValid = ValidationHelper.IsPrefabValidAtPath(path, true);
            if (!isValid)
            {
                ValidationFailedDialog.PromptPrefabFailed();
            }

            return isValid;
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