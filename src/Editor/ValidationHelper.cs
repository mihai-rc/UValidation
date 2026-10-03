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
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null &&
                string.Equals(prefabStage.assetPath, prefabPath, StringComparison.Ordinal))
            {
                return IsGameObjectValidRecursively(prefabStage.prefabContentsRoot, reportError);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                // Unity can invoke OnWillSaveAssets before a newly-created prefab is resolvable
                // by path. Allow that initial save so creation does not deadlock.
                return true;
            }

            GameObject prefabRoot = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                return IsGameObjectValidRecursively(prefabRoot, reportError);
            }
            finally
            {
                if (prefabRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }

        /// <summary>
        /// Validates a ScriptableObject at the specified asset path.
        /// </summary>
        /// <param name="assetPath"> The path to the scriptable object asset. </param>
        /// <param name="reportError"> Whether errors should be reported. </param>
        /// <returns> True if the scriptable object is valid; otherwise, false. </returns>
        public static bool IsScriptableObjectValidAtPath(string assetPath, bool reportError)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                return true;
            }

            return IsScriptValid(asset, reportError);
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
                // A new scene does not exist as an asset when Unity invokes OnWillSaveAssets
                // for its first save, so it cannot be resolved by path yet.
                return true;
            }

            var scene = SceneManager.GetSceneByPath(scenePath);
            return IsSceneValid(ref scene, reportError);
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
    }
}
