using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UValidation.Editor;

namespace UValidation.Tests
{
    public class ScenePathValidationTests
    {
        private const string k_TestFolderName = "__UValidationScenePathTests";
        private const string k_TestFolderPath = "Assets/" + k_TestFolderName;
        private const string k_ValidScenePath = k_TestFolderPath + "/Valid.unity";
        private const string k_InvalidScenePath = k_TestFolderPath + "/Invalid.unity";
        private const string k_ValidValue = "Valid";

        private bool m_BlockInvalidSaves;

        [SetUp]
        public void SetUp()
        {
            m_BlockInvalidSaves = UValidationProjectSettings.Instance.BlockInvalidSaves;
            UValidationProjectSettings.Instance.BlockInvalidSaves = false;
            AssetDatabase.DeleteAsset(k_TestFolderPath);
            AssetDatabase.CreateFolder("Assets", k_TestFolderName);
        }

        [TearDown]
        public void TearDown()
        {
            CloseSceneIfLoaded(k_ValidScenePath);
            CloseSceneIfLoaded(k_InvalidScenePath);

            AssetDatabase.DeleteAsset(k_TestFolderPath);
            UValidationProjectSettings.Instance.BlockInvalidSaves = m_BlockInvalidSaves;
        }

        [Test]
        public void ExistingValidUnloadedScene_PassesWithoutChangingOpenScenes()
        {
            CreateStoredScene(k_ValidScenePath, k_ValidValue);

            AssertValidationPreservesSceneSetup(k_ValidScenePath, true);
        }

        [Test]
        public void ExistingInvalidUnloadedScene_FailsWithoutChangingOpenScenes()
        {
            CreateStoredScene(k_InvalidScenePath, string.Empty);

            AssertValidationPreservesSceneSetup(k_InvalidScenePath, false);
        }

        private static void CreateStoredScene(string path, string value)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var root = new GameObject("Validation Root");
            root.AddComponent<PrefabValidationProbe>().SetValue(value);
            SceneManager.MoveGameObjectToScene(root, scene);

            Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
            Assert.IsTrue(EditorSceneManager.CloseScene(scene, true));
        }

        private static void AssertValidationPreservesSceneSetup(string path, bool expectedResult)
        {
            var sceneCount = SceneManager.sceneCount;
            var activeScene = SceneManager.GetActiveScene();
            Assert.IsFalse(SceneManager.GetSceneByPath(path).IsValid(), "The test scene must be unloaded.");

            Assert.AreEqual(expectedResult, ValidationHelper.IsSceneValidAtPath(path, false));

            Assert.AreEqual(sceneCount, SceneManager.sceneCount);
            Assert.AreEqual(activeScene, SceneManager.GetActiveScene());
            Assert.IsFalse(SceneManager.GetSceneByPath(path).IsValid(),
                "Path validation must close its temporary preview scene.");
        }

        private static void CloseSceneIfLoaded(string path)
        {
            var scene = EditorSceneManager.GetSceneByPath(path);
            if (scene.IsValid())
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
