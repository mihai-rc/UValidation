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
            var prefabRoot = prefabStage != null 
                ? prefabStage.prefabContentsRoot 
                : AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabRoot == null)
            {
                // If the prefab root is null, it means the prefab it is just being created,
                // so in order not to block its creation, we'll return true here.
                return true;
            }

            return IsGameObjectValidRecursively(prefabRoot, reportError);
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
        /// <returns> True if the script passes all generic attribute and custom validations; otherwise, false. </returns>
        public static bool IsScriptValid(UnityEngine.Object script, bool reportError)
        {
            var validation = new Validation(script);
            try
            {
                SerializedFieldsValidator.ValidateAttributes(script, ref validation);

                var passed = validation.Passed;
                if (reportError && !passed)
                {
                    validation.Report();
                }

                return passed;
            }
            finally
            {
                validation.Dispose();
            }
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