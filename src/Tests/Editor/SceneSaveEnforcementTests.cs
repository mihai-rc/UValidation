using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UValidation.Editor;

namespace UValidation.Tests
{
    public class SceneSaveEnforcementTests
    {
        private const string k_TestFolderName = "__UValidationSceneSaveTests";
        private const string k_TestFolderPath = "Assets/" + k_TestFolderName;
        private const string k_BaseScenePath = k_TestFolderPath + "/Base.unity";
        private const string k_ValidValue = "Valid";

        private readonly List<Scene> m_Scenes = new();
        private bool m_BlockInvalidSaves;
        private bool m_OpenedBaseScene;

        [SetUp]
        public void SetUp()
        {
            m_BlockInvalidSaves = UValidationProjectSettings.Instance.BlockInvalidSaves;
            UValidationProjectSettings.Instance.BlockInvalidSaves = false;
            AssetValidator.PromptOnBlockedSave = false;
            AssetDatabase.DeleteAsset(k_TestFolderPath);
            AssetDatabase.CreateFolder("Assets", k_TestFolderName);
            m_OpenedBaseScene = OpenSavedSceneIfUntitled();
        }

        [TearDown]
        public void TearDown()
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
            AssetDatabase.DeleteAsset(k_TestFolderPath);

            AssetValidator.PromptOnBlockedSave = true;
            UValidationProjectSettings.Instance.BlockInvalidSaves = m_BlockInvalidSaves;
        }

        [Test]
        public void InvalidEditOnOpenScene_SaveInPlace_IsBlocked()
        {
            var path = CreateStoredScene("SavedInPlace", k_ValidValue);
            var savedContents = ReadFile(path);
            var scene = OpenScene(path);
            SetValue(scene, string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("SavedInPlace"));
            Assert.IsFalse(EditorSceneManager.SaveScene(scene));

            Assert.AreEqual(savedContents, ReadFile(path));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SaveSceneAs_ValidSceneOverInvalidFile_IsSaved()
        {
            var sourcePath = CreateStoredScene("FixedSource", k_ValidValue);
            var targetPath = CreateStoredScene("FixedTarget", string.Empty);
            var scene = OpenScene(sourcePath);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            Assert.IsTrue(EditorSceneManager.SaveScene(scene, targetPath));
            yield return null;

            StringAssert.Contains($"m_Value: {k_ValidValue}", ReadFile(targetPath));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SaveSceneAs_InvalidSceneOverValidFile_IsReportedAfterImport()
        {
            var targetPath = CreateStoredScene("OverwrittenTarget", k_ValidValue);
            var scene = CreateScene("Overwritten", string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("Overwritten"));
            LogAssert.Expect(LogType.Error, CreateSavedAssetFailurePattern("OverwrittenTarget"));
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, targetPath));
            yield return null;

            Assert.AreEqual(targetPath, scene.path);
            Assert.IsFalse(ValidationHelper.IsSceneValid(ref scene, false));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SaveSceneAsCopy_InvalidSceneOverValidFile_IsReportedAfterImport()
        {
            var targetPath = CreateStoredScene("CopyTarget", k_ValidValue);
            var scene = CreateScene("Copied", string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("Copied"));
            LogAssert.Expect(LogType.Error, CreateSavedAssetFailurePattern("CopyTarget"));
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, targetPath, true));
            yield return null;

            Assert.IsFalse(ValidationHelper.IsSceneValidAtPath(targetPath, false));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NewInvalidScene_IsCreatedAndReportedAfterImport()
        {
            var path = $"{k_TestFolderPath}/Created.unity";
            var scene = CreateScene("Created", string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("Created"));
            LogAssert.Expect(LogType.Error, CreateNewAssetFailurePattern("Created"));
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
            yield return null;

            Assert.IsTrue(File.Exists(GetAbsolutePath(path)));
            LogAssert.NoUnexpectedReceived();
        }

        private string CreateStoredScene(string name, string value)
        {
            var path = $"{k_TestFolderPath}/{name}.unity";
            var scene = CreateScene(name, value);

            Assert.IsTrue(EditorSceneManager.SaveScene(scene, path), $"Failed to create test scene at '{path}'.");
            Assert.IsTrue(EditorSceneManager.CloseScene(scene, true));
            return path;
        }

        private Scene CreateScene(string rootName, string value)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            m_Scenes.Add(scene);

            var root = new GameObject(rootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<PrefabValidationProbe>().SetValue(value);
            return scene;
        }

        private Scene OpenScene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            m_Scenes.Add(scene);
            return scene;
        }

        private static void SetValue(Scene scene, string value)
        {
            var probe = scene.GetRootGameObjects()[0].GetComponent<PrefabValidationProbe>();
            probe.SetValue(value);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static string ReadFile(string assetPath)
        {
            return File.ReadAllText(GetAbsolutePath(assetPath));
        }

        private static string GetAbsolutePath(string assetPath)
        {
            return Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
        }

        private static Regex CreateValidationErrorPattern(string contextName)
        {
            return new Regex(
                @"(?s)\[Validation\] String is null or empty!.*" +
                $@" - Context: {Regex.Escape(contextName)}\r?\n.*" +
                @" - Variable: m_Value");
        }

        private static Regex CreateSavedAssetFailurePattern(string sceneName)
        {
            return new Regex($@"UValidation found invalid data in '.*/{Regex.Escape(sceneName)}\.unity' after Unity saved it");
        }

        private static Regex CreateNewAssetFailurePattern(string sceneName)
        {
            return new Regex($@"UValidation found invalid data in newly created asset '.*/{Regex.Escape(sceneName)}\.unity'");
        }

        private static bool OpenSavedSceneIfUntitled()
        {
            // Unity refuses to create additive scenes while an untitled scene is loaded, which is how EditMode runs start.
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                {
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    Assert.IsTrue(EditorSceneManager.SaveScene(scene, k_BaseScenePath));
                    return true;
                }
            }

            return false;
        }
    }
}
