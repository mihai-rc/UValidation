using UnityEditor;

namespace UValidation.Editor
{
    /// <summary>
    /// Exposes editor toggle via the top-level <b>Framework</b> menu.
    /// Settings are persisted in <see cref="EditorPrefs"/> and survive domain reloads.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorValidationToggle
    {
        private const string k_EnableValidationKey = "Framework.EnableValidation";
        private const string k_EnableValidationMenuPath = "Framework/Enable Validation";

        /// <summary>
        /// Gets whether editor validation is currently enabled.
        /// </summary>
        public static bool IsValidationEnabled => EditorPrefs.GetBool(k_EnableValidationKey, true);

        // Sync the checkmark when the editor loads / after a domain reload.
        static EditorValidationToggle()
        {
            EditorApplication.delayCall += SyncMenuCheckmark;
        }

        [MenuItem(k_EnableValidationMenuPath)]
        private static void ToggleValidation()
        {
            var enabled = !IsValidationEnabled;
            EditorPrefs.SetBool(k_EnableValidationKey, enabled);
            Menu.SetChecked(k_EnableValidationMenuPath, enabled);
        }

        [MenuItem(k_EnableValidationMenuPath, validate = true)]
        private static bool ToggleValidationValidate()
        {
            Menu.SetChecked(k_EnableValidationMenuPath, IsValidationEnabled);
            return true;
        }

        private static void SyncMenuCheckmark()
        {
            Menu.SetChecked(k_EnableValidationMenuPath, IsValidationEnabled);
        }
    }
}
