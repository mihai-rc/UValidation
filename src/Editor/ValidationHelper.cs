using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UValidation.Editor
{
    /// <summary>
    /// Provides helper methods for validating scenes, prefabs, game objects, and scriptable objects.
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates a prefab at the specified path.
        /// </summary>
        /// <param name="prefabPath"> The path to the prefab asset. </param>
        /// <param name="reportError"> Whether errors should be reported. </param>
        /// <returns> True if the prefab is valid; otherwise, false. </returns>
        /// <remarks>
        /// When the prefab is open in Prefab Mode, its unsaved stage contents are validated. Otherwise the loaded
        /// prefab asset is validated, including changes made to it that have not been saved yet.
        /// </remarks>
        public static bool IsPrefabValidAtPath(string prefabPath, bool reportError)
        {
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabRoot == null)
            {
                return false;
            }

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null &&
                string.Equals(prefabStage.assetPath, prefabPath, StringComparison.Ordinal))
            {
                return IsGameObjectValidRecursively(prefabStage.prefabContentsRoot, reportError);
            }

            return IsGameObjectValidRecursively(prefabRoot, reportError);
        }

        /// <summary>
        /// Validates every ScriptableObject stored at the specified asset path.
        /// </summary>
        /// <param name="assetPath"> The path to the asset file. </param>
        /// <param name="reportError"> Whether errors should be reported. </param>
        /// <returns> True if every ScriptableObject in the asset file is valid; otherwise, false. </returns>
        /// <remarks>
        /// An asset file that contains an object whose script is missing, including a sub-asset, is invalid.
        /// </remarks>
        public static bool IsScriptableObjectValidAtPath(string assetPath, bool reportError)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets.Length == 0)
            {
                return false;
            }

            var isValid = AreAssetsValid(assetPath, assets, reportError);
            return isValid && assets.OfType<ScriptableObject>().Any();
        }

        /// <summary>
        /// Validates a scene at the specified path.
        /// </summary>
        /// <param name="scenePath"> The path to the scene asset. </param>
        /// <param name="reportError"> Indicates whether errors should be reported. </param>
        /// <returns> True if the scene is valid; otherwise, false. </returns>
        public static bool IsSceneValidAtPath(string scenePath, bool reportError)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                return false;
            }

            var scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                return IsSceneValid(ref scene, reportError);
            }

            var previewScene = default(Scene);
            var isValid = false;
            try
            {
                previewScene = EditorSceneManager.OpenPreviewScene(scenePath);
                isValid = IsSceneValid(ref previewScene, reportError);
            }
            catch (Exception exception)
            {
                ReportUnreadableAsset("scene", scenePath, exception, reportError);
            }
            finally
            {
                if (previewScene.IsValid())
                {
                    try
                    {
                        EditorSceneManager.ClosePreviewScene(previewScene);
                    }
                    catch (Exception exception)
                    {
                        ReportUnreadableAsset("scene", scenePath, exception, reportError);
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        /// <summary>
        /// Determines whether a save candidate is valid or must be validated after Unity imports it.
        /// </summary>
        /// <param name="assetPath"> The asset path reported by Unity. </param>
        /// <param name="reportError"> Whether validation errors should be reported. </param>
        /// <returns> The validation state used by Editor save enforcement. </returns>
        internal static AssetValidationState ValidateAssetAtPathForSave(string assetPath, bool reportError)
        {
            if (assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                return ValidateSceneAtPathForSave(assetPath, reportError);
            }

            if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return ValidatePrefabAtPathForSave(assetPath, reportError);
            }

            if (assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                return ValidateScriptableObjectAtPathForSave(assetPath, reportError);
            }

            return AssetValidationState.Valid;
        }

        /// <summary>
        /// Validates the contents Unity imported after saving an asset.
        /// </summary>
        /// <param name="assetPath"> The imported asset path. </param>
        /// <param name="reportError"> Whether validation errors should be reported. </param>
        /// <returns> The state of the imported contents, or <see cref="AssetValidationState.ValidateAfterImport"/> if they are not readable yet. </returns>
        internal static AssetValidationState ValidateImportedAssetAtPath(string assetPath, bool reportError)
        {
            if (assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                return ValidateImportedSceneAtPath(assetPath, reportError);
            }

            if (!assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return ValidateAssetAtPathForSave(assetPath, reportError);
            }

            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return prefabRoot == null
                ? GetUnreadableAssetState(assetPath)
                : ToState(IsGameObjectValidRecursively(prefabRoot, reportError));
        }

        /// <summary>
        /// Validates the specified scene.
        /// </summary>
        /// <param name="scene"> The scene to validate. </param>
        /// <param name="reportError"> Indicates whether errors should be reported. </param>
        /// <returns> True if the scene is valid; otherwise, false. </returns>
        public static bool IsSceneValid(ref Scene scene, bool reportError)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            var isValid = true;
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                if (IsGameObjectValidRecursively(rootGameObject, reportError))
                {
                    continue;
                }

                isValid = false;
                if (!reportError)
                {
                    return false;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Validates the specified game object.
        /// </summary>
        /// <param name="gameObject"> The game object to validate. </param>
        /// <param name="reportError"> Indicates whether errors should be reported. </param>
        /// <returns> True if the game object is valid; otherwise, false. </returns>
        public static bool IsGameObjectValid(GameObject gameObject, bool reportError)
        {
            if (gameObject == null)
            {
                return false;
            }

            var isValid = true;
            var missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (missingScriptCount > 0)
            {
                isValid = false;
                if (reportError)
                {
                    var suffix = missingScriptCount == 1 ? "script" : "scripts";
                    Debug.LogError(
                        $"[Validation] GameObject '{gameObject.name}' contains " +
                        $"{missingScriptCount} missing MonoBehaviour {suffix}.",
                        gameObject);
                }

                if (!reportError)
                {
                    return false;
                }
            }

            foreach (var script in gameObject.GetComponents<MonoBehaviour>())
            {
                if (script == null || IsScriptValid(script, reportError))
                {
                    continue;
                }

                isValid = false;
                if (!reportError)
                {
                    return false;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Validates a specific Unity Object (such as a MonoBehaviour or ScriptableObject).
        /// Runs automated attribute-based validation and evaluates <see cref="IValidatable"/> if implemented.
        /// </summary>
        /// <param name="script"> The Unity object to validate. </param>
        /// <param name="reportError"> Indicates whether errors should be reported. </param>
        /// <returns> True if the script passes all generic attributes and custom validations; otherwise, false. </returns>
        public static bool IsScriptValid(UnityEngine.Object script, bool reportError)
        {
            using var validation = new Validation(script);
            SerializedFieldsValidator.ValidateAttributes(script, validation);

            var passed = validation.Passed;
            if (reportError && !passed)
            {
                validation.Report();
            }

            return passed;
        }

        private static bool IsGameObjectValidRecursively(GameObject gameObject, bool reportError)
        {
            var isValid = IsGameObjectValid(gameObject, reportError);
            if (!isValid && !reportError)
            {
                return false;
            }

            foreach (Transform child in gameObject.transform)
            {
                if (IsGameObjectValidRecursively(child.gameObject, reportError))
                {
                    continue;
                }

                isValid = false;
                if (!reportError)
                {
                    return false;
                }
            }

            return isValid;
        }

        private static AssetValidationState ValidatePrefabAtPathForSave(string prefabPath, bool reportError)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null &&
                string.Equals(prefabStage.assetPath, prefabPath, StringComparison.Ordinal))
            {
                return ToState(IsGameObjectValidRecursively(prefabStage.prefabContentsRoot, reportError));
            }

            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabRoot == null)
            {
                return GetUnreadableAssetState(prefabPath);
            }

            // Apply Overrides and SaveAsPrefabAsset write contents built from objects this callback cannot
            // reach, while the loaded asset still holds the previous version. Only unsaved changes on the
            // loaded asset prove that it is the version being saved.
            if (!HasUnsavedChanges(prefabPath))
            {
                return AssetValidationState.ValidateAfterImport;
            }

            return ToState(IsGameObjectValidRecursively(prefabRoot, reportError));
        }

        private static bool HasUnsavedChanges(string assetPath)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset != null && EditorUtility.IsDirty(asset))
                {
                    return true;
                }
            }

            return false;
        }

        private static AssetValidationState ValidateSceneAtPathForSave(string scenePath, bool reportError)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                return ToState(IsSceneValid(ref scene, reportError));
            }

            // Saving a scene onto another path reports the target path while that path is not loaded, so the
            // file there still holds the previous contents. The scene being written is only readable after import.
            return AssetValidationState.ValidateAfterImport;
        }

        private static AssetValidationState ValidateImportedSceneAtPath(string scenePath, bool reportError)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                return ToState(IsSceneValid(ref scene, reportError));
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                return GetUnreadableAssetState(scenePath);
            }

            return ToState(IsSceneValidAtPath(scenePath, reportError));
        }

        private static AssetValidationState ValidateScriptableObjectAtPathForSave(
            string assetPath,
            bool reportError)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets.Length == 0)
            {
                return GetUnreadableAssetState(assetPath);
            }

            return ToState(AreAssetsValid(assetPath, assets, reportError));
        }

        private static bool AreAssetsValid(string assetPath, UnityEngine.Object[] assets, bool reportError)
        {
            var isValid = !HasMissingScripts(assetPath, assets, reportError);
            if (!isValid && !reportError)
            {
                return false;
            }

            var scriptableObjects = assets.OfType<ScriptableObject>().ToArray();
            return AreScriptableObjectsValid(scriptableObjects, reportError) && isValid;
        }

        private static bool HasMissingScripts(string assetPath, UnityEngine.Object[] assets, bool reportError)
        {
            // Unity returns null for an object whose script cannot be resolved, so filtering the
            // loaded objects by type would silently skip it.
            var missingScriptCount = assets.Count(asset => asset == null);
            if (missingScriptCount == 0)
            {
                return false;
            }

            if (reportError)
            {
                var suffix = missingScriptCount == 1 ? "object whose script is" : "objects whose scripts are";
                Debug.LogError(
                    $"[Validation] Asset '{assetPath}' contains {missingScriptCount} {suffix} missing.",
                    AssetDatabase.LoadMainAssetAtPath(assetPath));
            }

            return true;
        }

        private static bool AreScriptableObjectsValid(
            ScriptableObject[] scriptableObjects,
            bool reportError)
        {
            var isValid = true;
            foreach (var asset in scriptableObjects)
            {
                if (IsScriptValid(asset, reportError))
                {
                    continue;
                }

                isValid = false;
                if (!reportError)
                {
                    return false;
                }
            }

            return isValid;
        }

        private static AssetValidationState ToState(bool isValid)
        {
            return isValid ? AssetValidationState.Valid : AssetValidationState.Invalid;
        }

        private static AssetValidationState GetUnreadableAssetState(string assetPath)
        {
            // Unity can announce a newly created asset before the AssetDatabase can load it.
            return AssetDatabase.AssetPathExists(assetPath)
                ? AssetValidationState.Invalid
                : AssetValidationState.ValidateAfterImport;
        }

        private static void ReportUnreadableAsset(
            string assetType,
            string assetPath,
            Exception exception,
            bool reportError)
        {
            if (reportError)
            {
                Debug.LogError(
                    $"UValidation could not validate {assetType} '{assetPath}': {exception.Message}");
            }
        }
    }
}
