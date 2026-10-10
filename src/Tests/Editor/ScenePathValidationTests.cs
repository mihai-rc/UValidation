using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UValidation.Editor;

namespace UValidation.Tests
{
    public class ScenePathValidationTests
    {
        private const string k_TestFolderName = "__UValidationScenePathTests";
        private const string k_TestFolderPath = "Assets/" + k_TestFolderName;
        private const string k_ScenePath = k_TestFolderPath + "/Valid.unity";

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
            var testScene = EditorSceneManager.GetSceneByPath(k_ScenePath);
            if (testScene.IsValid())
            {
                EditorSceneManager.CloseScene(testScene, true);
            }

            AssetDatabase.DeleteAsset(k_TestFolderPath);
            UValidationProjectSettings.Instance.BlockInvalidSaves = m_BlockInvalidSaves;
        }

        [Test]
        public void ExistingUnloadedScene_IsValidatedFromItsStoredContents()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var root = new GameObject("Valid Root");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);

            Assert.IsTrue(EditorSceneManager.SaveScene(scene, k_ScenePath));
            Assert.IsTrue(EditorSceneManager.CloseScene(scene, true));
            Assert.IsTrue(ValidationHelper.IsSceneValidAtPath(k_ScenePath, false));
        }
    }
}
