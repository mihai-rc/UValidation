using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NUnit.Framework;
using UValidation.Editor;
using static UValidation.Tests.ValidationTestUtility;

namespace UValidation.Tests
{
    /// <summary>
    /// Provides isolated setup and cleanup for tests that create, open, and save scenes in a temporary folder.
    /// </summary>
    /// <remarks>
    /// Save blocking and the blocked-save prompt are disabled during setup. Tests enable blocking explicitly,
    /// and every setting is restored during teardown even when cleanup fails.
    /// </remarks>
    public abstract class SceneSaveTestFixture
    {
        private readonly List<Scene> m_Scenes = new();
        private bool m_BlockInvalidSaves;
        private bool m_OpenedBaseScene;

        /// <summary>
        /// Gets the name of the temporary folder created under <c>Assets</c> for this fixture.
        /// </summary>
        protected abstract string TestFolderName { get; }

        /// <summary>
        /// Gets the project-relative path of the temporary folder.
        /// </summary>
        protected string TestFolderPath => "Assets/" + TestFolderName;

        /// <summary>
        /// Creates the temporary folder and replaces an untitled scene with a saved base scene.
        /// </summary>
        [SetUp]
        public void SetUpSceneEnvironment()
        {
            m_BlockInvalidSaves = UValidationProjectSettings.Instance.BlockInvalidSaves;
            UValidationProjectSettings.Instance.BlockInvalidSaves = false;
            AssetValidator.PromptOnBlockedSave = false;
            AssetDatabase.DeleteAsset(TestFolderPath);
            AssetDatabase.CreateFolder("Assets", TestFolderName);
            m_OpenedBaseScene = OpenSavedSceneIfUntitled(TestFolderPath + "/Base.unity");
        }

        /// <summary>
        /// Closes tracked scenes, deletes the temporary folder, and restores the project settings.
        /// </summary>
        [TearDown]
        public void TearDownSceneEnvironment()
        {
            try
            {
                UValidationProjectSettings.Instance.BlockInvalidSaves = false;
                foreach (var scene in m_Scenes)
                {
                    if (scene.IsValid())
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }

                if (m_OpenedBaseScene)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }

                m_Scenes.Clear();
                AssetDatabase.DeleteAsset(TestFolderPath);
            }
            finally
            {
                AssetValidator.PromptOnBlockedSave = true;
                UValidationProjectSettings.Instance.BlockInvalidSaves = m_BlockInvalidSaves;
            }
        }

        /// <summary>
        /// Registers a scene so it is closed during teardown.
        /// </summary>
        /// <param name="scene"> The scene to close after the test. </param>
        /// <returns> The registered scene. </returns>
        protected Scene TrackScene(Scene scene)
        {
            m_Scenes.Add(scene);
            return scene;
        }
    }
}
