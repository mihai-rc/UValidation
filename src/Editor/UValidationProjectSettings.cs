using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UValidation.Editor
{
    /// <summary>
    /// Stores shared UValidation enforcement policies in the Unity project.
    /// </summary>
    [FilePath("ProjectSettings/UValidationSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class UValidationProjectSettings : ScriptableSingleton<UValidationProjectSettings>
    {
        [SerializeField] private bool m_BlockInvalidSaves;
        [SerializeField] private bool m_BlockPlayMode;
        [SerializeField] private bool m_BlockEditorQuit;
        [SerializeField] private bool m_FailBuild;

        /// <summary>
        /// Gets the settings instance for the current project.
        /// </summary>
        internal static UValidationProjectSettings Instance => instance;

        /// <summary>
        /// Gets or sets whether invalid scene, prefab, and ScriptableObject saves are blocked.
        /// </summary>
        internal bool BlockInvalidSaves
        {
            get => m_BlockInvalidSaves;
            set => m_BlockInvalidSaves = value;
        }

        /// <summary>
        /// Gets or sets whether entering Play Mode is blocked when the active scene is invalid.
        /// </summary>
        internal bool BlockPlayMode
        {
            get => m_BlockPlayMode;
            set => m_BlockPlayMode = value;
        }

        /// <summary>
        /// Gets or sets whether quitting the Editor is blocked when a dirty ScriptableObject is invalid.
        /// </summary>
        internal bool BlockEditorQuit
        {
            get => m_BlockEditorQuit;
            set => m_BlockEditorQuit = value;
        }

        /// <summary>
        /// Gets or sets whether player builds fail when a processed scene is invalid.
        /// </summary>
        internal bool FailBuild
        {
            get => m_FailBuild;
            set => m_FailBuild = value;
        }

        /// <summary>
        /// Writes the current policies to ProjectSettings.
        /// </summary>
        internal void SaveSettings()
        {
            Save(true);
        }
    }

    /// <summary>
    /// Draws the UValidation project settings page.
    /// </summary>
    internal static class UValidationProjectSettingsProvider
    {
        private const string k_SettingsPath = "Project/UValidation";

        private static readonly GUIContent s_BlockInvalidSavesLabel = new(
            "Block Invalid Saves",
            "Prevent invalid scenes, prefabs, and ScriptableObjects from being saved.");
        private static readonly GUIContent s_BlockPlayModeLabel = new(
            "Block Play Mode",
            "Prevent entering Play Mode when the active scene is invalid.");
        private static readonly GUIContent s_BlockEditorQuitLabel = new(
            "Block Editor Quit",
            "Prevent quitting while a dirty ScriptableObject asset is invalid.");
        private static readonly GUIContent s_FailBuildLabel = new(
            "Fail Player Builds",
            "Fail a player build when a processed scene contains invalid data.");

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider(k_SettingsPath, SettingsScope.Project)
            {
                label = "UValidation",
                guiHandler = DrawSettings,
                keywords = new HashSet<string>
                {
                    "validation",
                    "save",
                    "play mode",
                    "quit",
                    "build",
                    "enforcement"
                }
            };

            return provider;
        }

        private static void DrawSettings(string searchContext)
        {
            var settings = UValidationProjectSettings.Instance;
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.Update();

            EditorGUILayout.HelpBox(
                "Inspector messages and editor highlights are always active. These policies only control whether UValidation blocks an operation. New projects start with enforcement disabled.",
                MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("m_BlockInvalidSaves"), s_BlockInvalidSavesLabel);
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("m_BlockPlayMode"), s_BlockPlayModeLabel);
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("m_BlockEditorQuit"), s_BlockEditorQuitLabel);
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("m_FailBuild"), s_FailBuildLabel);

            if (serializedSettings.ApplyModifiedProperties())
            {
                settings.SaveSettings();
            }
        }
    }
}
