using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
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
        public void IsSceneValidAtPath_ValidUnloadedScene_PassesWithoutChangingOpenScenes()
        {
            CreateStoredScene(k_ValidScenePath, VALID_VALUE, "Validation Root");
            AssertValidationPreservesSceneSetup(k_ValidScenePath, true);
        }

        [Test]
        public void IsSceneValidAtPath_InvalidUnloadedScene_FailsWithoutChangingOpenScenes()
        {
            CreateStoredScene(k_InvalidScenePath, string.Empty, "Validation Root");
            AssertValidationPreservesSceneSetup(k_InvalidScenePath, false);
        }

        [Test]
        public void IsSceneValidAtPath_MultipleInvalidRoots_ReportsEveryFailure()
        {
            CreateStoredScene(k_MultipleInvalidScenePath, string.Empty, "Invalid Root A", "Invalid Root B");

            LogAssert.Expect(
                LogType.Error,
                CreateValidationErrorPattern("Invalid Root A"));

            LogAssert.Expect(
                LogType.Error,
                CreateValidationErrorPattern("Invalid Root B"));

            Assert.IsFalse(ValidationHelper.IsSceneValidAtPath(k_MultipleInvalidScenePath, true));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void IsSceneValidAtPath_MissingScene_IsInvalidAndPendingForSaveEnforcement()
        {
            var missingPath = k_TestFolderPath + "/Missing.unity";

            Assert.IsFalse(ValidationHelper.IsSceneValidAtPath(missingPath, false));
            Assert.AreEqual(
                AssetValidationState.ValidateAfterImport,
                ValidationHelper.ValidateAssetAtPathForSave(missingPath, false));
        }

        private static void CreateStoredScene(string path, string value, params string[] rootNames)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            foreach (var rootName in rootNames)
            {
                SceneManager.MoveGameObjectToScene(CreateProbeObject(rootName, value), scene);
            }

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
            Assert.IsFalse(SceneManager.GetSceneByPath(path).IsValid(), "Path validation must close its temporary preview scene.");
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
