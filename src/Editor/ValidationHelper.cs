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
        public static bool IsPrefabValidAtPath(string prefabPath, bool reportError)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                return false;
            }

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null &&
                string.Equals(prefabStage.assetPath, prefabPath, StringComparison.Ordinal))
            {
                return IsGameObjectValidRecursively(prefabStage.prefabContentsRoot, reportError);
            }

            GameObject prefabRoot = null;
            var isValid = false;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                isValid = IsGameObjectValidRecursively(prefabRoot, reportError);
            }
            catch (Exception exception)
            {
                ReportUnreadableAsset("prefab", prefabPath, exception, reportError);
            }
            finally
            {
                if (prefabRoot != null)
                {
                    try
                    {
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                    catch (Exception exception)
                    {
                        ReportUnreadableAsset("prefab", prefabPath, exception, reportError);
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        /// <summary>
        /// Validates every ScriptableObject stored at the specified asset path.
        /// </summary>
        /// <param name="assetPath"> The path to the asset file. </param>
        /// <param name="reportError"> Whether errors should be reported. </param>
        /// <returns> True if every ScriptableObject in the asset file is valid; otherwise, false. </returns>
        public static bool IsScriptableObjectValidAtPath(string assetPath, bool reportError)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
            {
                return false;
            }

            var scriptableObjects = AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<ScriptableObject>()
                .ToArray();

            return scriptableObjects.Length > 0 &&
                   scriptableObjects.All(asset => IsScriptValid(asset, reportError));
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
        /// Determines whether a save candidate is valid or must wait for its initial import.
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

            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                if (!IsGameObjectValidRecursively(rootGameObject, reportError))
                {
                    return false;
                };
            }

            return true;
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

            var missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (missingScriptCount > 0)
            {
                if (reportError)
                {
                    var suffix = missingScriptCount == 1 ? "script" : "scripts";
                    Debug.LogError(
                        $"[Validation] GameObject '{gameObject.name}' contains " +
                        $"{missingScriptCount} missing MonoBehaviour {suffix}.",
                        gameObject);
                }

                return false;
            }

            return gameObject
                .GetComponents<MonoBehaviour>()
                .All(script => IsScriptValid(script, reportError));
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
            if (!IsGameObjectValid(gameObject, reportError))
            {
                return false;
            }

            foreach (Transform child in gameObject.transform)
            {
                if (!IsGameObjectValidRecursively(child.gameObject, reportError))
                {
                    return false;
                }
            }

            return true;
        }

        private static AssetValidationState ValidatePrefabAtPathForSave(string prefabPath, bool reportError)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null &&
                string.Equals(prefabStage.assetPath, prefabPath, StringComparison.Ordinal))
            {
                return ToState(IsGameObjectValidRecursively(prefabStage.prefabContentsRoot, reportError));
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                return AssetDatabase.AssetPathExists(prefabPath)
                    ? AssetValidationState.Invalid
                    : AssetValidationState.PendingCreation;
            }

            return ToState(IsPrefabValidAtPath(prefabPath, reportError));
        }

        private static AssetValidationState ValidateSceneAtPathForSave(string scenePath, bool reportError)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                return ToState(IsSceneValid(ref scene, reportError));
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                return AssetDatabase.AssetPathExists(scenePath)
                    ? AssetValidationState.Invalid
                    : AssetValidationState.PendingCreation;
            }

            return ToState(IsSceneValidAtPath(scenePath, reportError));
        }

        private static AssetValidationState ValidateScriptableObjectAtPathForSave(
            string assetPath,
            bool reportError)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
            {
                return AssetDatabase.AssetPathExists(assetPath)
                    ? AssetValidationState.Invalid
                    : AssetValidationState.PendingCreation;
            }

            var scriptableObjects = AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<ScriptableObject>()
                .ToArray();

            if (scriptableObjects.Length == 0)
            {
                return AssetValidationState.Valid;
            }

            return ToState(scriptableObjects.All(asset => IsScriptValid(asset, reportError)));
        }

        private static AssetValidationState ToState(bool isValid)
        {
            return isValid ? AssetValidationState.Valid : AssetValidationState.Invalid;
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
