namespace UValidation.Editor
{
    /// <summary>
    /// Resolves the enforcement policies configured for the current project.
    /// </summary>
    internal static class ValidationEnforcement
    {
        /// <summary>
        /// Gets whether invalid scene, prefab, and ScriptableObject saves should be blocked.
        /// </summary>
        internal static bool BlockInvalidSaves => UValidationProjectSettings.Instance.BlockInvalidSaves;

        /// <summary>
        /// Gets whether entering Play Mode should be blocked when the active scene is invalid.
        /// </summary>
        internal static bool BlockPlayMode => UValidationProjectSettings.Instance.BlockPlayMode;

        /// <summary>
        /// Gets whether quitting the Editor should be blocked when a dirty ScriptableObject is invalid.
        /// </summary>
        internal static bool BlockEditorQuit => UValidationProjectSettings.Instance.BlockEditorQuit;

        /// <summary>
        /// Gets whether a player build should fail when a processed scene is invalid.
        /// </summary>
        internal static bool FailBuild => UValidationProjectSettings.Instance.FailBuild;
    }
}
