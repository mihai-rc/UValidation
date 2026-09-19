using UnityEditor;

namespace UValidation.Editor
{
    /// <summary>
    /// Provides methods to display validation failure dialogs for scenes, prefabs, and scriptable objects.
    /// </summary>
    public static class ValidationFailedDialog
    {
        private const string k_SceneDialogTitle = "Validation Failed!";
        private const string k_DialogSaveSceneMessage = "There are GameObjects with required fields that are not set. Please fix them before saving the scene.";
        private const string k_DialogPlayMessage = "There are GameObjects with required fields that are not set. Please fix them before playing the scene.";
        private const string k_DialogSavePrefabMessage = "There are GameObjects with required fields that are not set. Please fix them before playing the scene.";
        private const string k_DialogSaveScriptableObjectMessage = "This ScriptableObject has required fields that are not set. Please fix them before saving.";
        private const string k_DialogButton = "Ok";

        /// <summary>
        /// Displays a dialog indicating that scene validation has failed.
        /// </summary>
        public static void PromptSceneFailed()
        {
            EditorUtility.DisplayDialog(k_SceneDialogTitle, k_DialogSaveSceneMessage, k_DialogButton);
        }

        /// <summary>
        /// Displays a dialog indicating that play mode validation has failed.
        /// </summary>
        public static void PromptPlayModeFailed()
        {
            EditorUtility.DisplayDialog(k_SceneDialogTitle, k_DialogPlayMessage, k_DialogButton);
        }

        /// <summary>
        /// Displays a dialog indicating that prefab validation has failed.
        /// </summary>
        public static void PromptPrefabFailed()
        {
            EditorUtility.DisplayDialog(k_SceneDialogTitle, k_DialogSavePrefabMessage, k_DialogButton);
        }

        /// <summary>
        /// Displays a dialog indicating that scriptable object validation has failed.
        /// </summary>
        public static void PromptScriptableObjectFailed()
        {
            EditorUtility.DisplayDialog(k_SceneDialogTitle, k_DialogSaveScriptableObjectMessage, k_DialogButton);
        }
    }
}