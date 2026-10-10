using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace UValidation.Editor
{
    /// <summary>
    /// Enforces the configured validation policy for scenes included in player builds.
    /// </summary>
    internal sealed class ValidationBuildProcessor : IProcessSceneWithReport
    {
        /// <summary>
        /// Gets the build callback order.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Validates each processed scene when build enforcement is enabled.
        /// </summary>
        /// <param name="scene"> The scene being processed for the build. </param>
        /// <param name="report"> The build report. </param>
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (!ValidationEnforcement.FailBuild)
            {
                return;
            }

            if (ValidationHelper.IsSceneValid(ref scene, true))
            {
                return;
            }

            var sceneName = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
            throw new BuildFailedException(
                $"UValidation stopped the player build because scene '{sceneName}' contains invalid data.");
        }
    }
}
