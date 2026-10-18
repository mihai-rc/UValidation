using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UValidation.Editor;
using static UValidation.Tests.ValidationTestUtility;

namespace UValidation.Tests
{
    public class ScenePathValidationTests
    {
        private const string k_TestFolderName = "__UValidationScenePathTests";
        private const string k_TestFolderPath = "Assets/" + k_TestFolderName;
        private const string k_BaseScenePath = k_TestFolderPath + "/Base.unity";
        private const string k_ValidScenePath = k_TestFolderPath + "/Valid.unity";
        private const string k_InvalidScenePath = k_TestFolderPath + "/Invalid.unity";
        private const string k_MultipleInvalidScenePath = k_TestFolderPath + "/MultipleInvalid.unity";
        private const string k_ValidValue = "Valid";

        private bool m_BlockInvalidSaves;
        private bool m_OpenedBaseScene;

        [SetUp]
        public void SetUp()
        {
            m_BlockInvalidSaves = UValidationProjectSettings.Instance.BlockInvalidSaves;
            UValidationProjectSettings.Instance.BlockInvalidSaves = false;
            AssetDatabase.DeleteAsset(k_TestFolderPath);
            AssetDatabase.CreateFolder("Assets", k_TestFolderName);
            m_OpenedBaseScene = OpenSavedSceneIfUntitled(k_BaseScenePath);
        }

        [TearDown]
        public void TearDown()
        {
            CloseSceneIfLoaded(k_ValidScenePath);
            CloseSceneIfLoaded(k_InvalidScenePath);
            CloseSceneIfLoaded(k_MultipleInvalidScenePath);

            if (m_OpenedBaseScene)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

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

        [Test]
        public void MultipleInvalidRoots_ReportEveryFailure()
        {
            CreateStoredSceneWithTwoInvalidRoots(k_MultipleInvalidScenePath);

            LogAssert.Expect(
                LogType.Error,
                CreateValidationErrorPattern("Invalid Root A"));
            LogAssert.Expect(
                LogType.Error,
                CreateValidationErrorPattern("Invalid Root B"));

            Assert.IsFalse(ValidationHelper.IsSceneValidAtPath(k_MultipleInvalidScenePath, true));
            LogAssert.NoUnexpectedReceived();
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

        private static void CreateStoredSceneWithTwoInvalidRoots(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            CreateInvalidRoot("Invalid Root A", scene);
            CreateInvalidRoot("Invalid Root B", scene);

            Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
            Assert.IsTrue(EditorSceneManager.CloseScene(scene, true));
        }

        private static void CreateInvalidRoot(string name, Scene scene)
        {
            var root = new GameObject(name);
            root.AddComponent<PrefabValidationProbe>().SetValue(string.Empty);
            SceneManager.MoveGameObjectToScene(root, scene);
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
