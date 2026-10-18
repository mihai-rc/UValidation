using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UValidation.Tests
{
    /// <summary>
    /// Provides shared infrastructure for editor validation tests.
    /// </summary>
    internal static class ValidationTestUtility
    {
        /// <summary>
        /// Creates a pattern matching the standard empty-string validation error for an object.
        /// </summary>
        /// <param name="contextName"> The expected validation context name. </param>
        /// <returns> The validation error pattern. </returns>
        internal static Regex CreateValidationErrorPattern(string contextName)
        {
            return new Regex(
                @"(?s)\[Validation\] String is null or empty!.*" +
                $@" - Context: {Regex.Escape(contextName)}\r?\n.*" +
                @" - Variable: m_Value");
        }

        /// <summary>
        /// Creates a pattern matching a post-save validation failure.
        /// </summary>
        /// <param name="assetName"> The expected asset name without its extension. </param>
        /// <param name="assetExtension"> The expected asset extension, including its leading period. </param>
        /// <returns> The saved-asset failure pattern. </returns>
        internal static Regex CreateSavedAssetFailurePattern(string assetName, string assetExtension)
        {
            var escapedAssetName = Regex.Escape(assetName + assetExtension);
            return new Regex($@"UValidation found invalid data in '.*/{escapedAssetName}' after Unity saved it");
        }

        /// <summary>
        /// Creates a pattern matching a newly created asset validation failure.
        /// </summary>
        /// <param name="assetName"> The expected asset name without its extension. </param>
        /// <param name="assetExtension"> The expected asset extension, including its leading period. </param>
        /// <returns> The new-asset failure pattern. </returns>
        internal static Regex CreateNewAssetFailurePattern(string assetName, string assetExtension)
        {
            var escapedAssetName = Regex.Escape(assetName + assetExtension);
            return new Regex($@"UValidation found invalid data in newly created asset '.*/{escapedAssetName}'");
        }

        /// <summary>
        /// Converts a project-relative asset path into an absolute filesystem path.
        /// </summary>
        /// <param name="assetPath"> The project-relative asset path. </param>
        /// <returns> The absolute path. </returns>
        internal static string GetAbsolutePath(string assetPath)
        {
            return Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
        }

        /// <summary>
        /// Replaces Unity's initial untitled scene with a saved empty scene when needed by additive-scene tests.
        /// </summary>
        /// <param name="baseScenePath"> The path at which to save the replacement scene. </param>
        /// <returns> Whether a replacement scene was opened. </returns>
        internal static bool OpenSavedSceneIfUntitled(string baseScenePath)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (!string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                {
                    continue;
                }

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, baseScenePath));
                return true;
            }

            return false;
        }
    }
}
