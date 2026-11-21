using System.Collections;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using UValidation.Editor;
using static UValidation.Tests.ValidationTestUtility;

namespace UValidation.Tests
{
    public class SceneSaveEnforcementTests : SceneSaveTestFixture
    {
        /// <inheritdoc />
        protected override string TestFolderName => "__UValidationSceneSaveTests";

        [Test]
        public void SaveScene_InvalidEditInPlace_IsBlocked()
        {
            var path = CreateStoredScene("SavedInPlace", VALID_VALUE);
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
            var sourcePath = CreateStoredScene("FixedSource", VALID_VALUE);
            var targetPath = CreateStoredScene("FixedTarget", string.Empty);
            var scene = OpenScene(sourcePath);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            Assert.IsTrue(EditorSceneManager.SaveScene(scene, targetPath));
            yield return null;

            StringAssert.Contains($"m_Value: {VALID_VALUE}", ReadFile(targetPath));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SaveSceneAs_InvalidSceneOverValidFile_IsReportedAfterImport()
        {
            var targetPath = CreateStoredScene("OverwrittenTarget", VALID_VALUE);
            var scene = CreateScene("Overwritten", string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("Overwritten"));
            LogAssert.Expect(LogType.Error, CreateSavedAssetFailurePattern("OverwrittenTarget", ".unity"));
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, targetPath));
            yield return null;

            Assert.AreEqual(targetPath, scene.path);
            Assert.IsFalse(ValidationHelper.IsSceneValid(ref scene, false));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SaveSceneAsCopy_InvalidSceneOverValidFile_IsReportedAfterImport()
        {
            var targetPath = CreateStoredScene("CopyTarget", VALID_VALUE);
            var scene = CreateScene("Copied", string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("Copied"));
            LogAssert.Expect(LogType.Error, CreateSavedAssetFailurePattern("CopyTarget", ".unity"));
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, targetPath, true));
            yield return null;

            Assert.IsFalse(ValidationHelper.IsSceneValidAtPath(targetPath, false));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SaveScene_NewInvalidScene_IsCreatedAndReportedAfterImport()
        {
            var path = $"{TestFolderPath}/Created.unity";
            var scene = CreateScene("Created", string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("Created"));
            LogAssert.Expect(LogType.Error, CreateNewAssetFailurePattern("Created", ".unity"));
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
            yield return null;

            Assert.IsTrue(File.Exists(GetAbsolutePath(path)));
            LogAssert.NoUnexpectedReceived();
        }

        private string CreateStoredScene(string name, string value)
        {
            var path = $"{TestFolderPath}/{name}.unity";
            var scene = CreateScene(name, value);

            Assert.IsTrue(EditorSceneManager.SaveScene(scene, path), $"Failed to create test scene at '{path}'.");
            Assert.IsTrue(EditorSceneManager.CloseScene(scene, true));
            return path;
        }

        private Scene CreateScene(string rootName, string value)
        {
            var scene = TrackScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive));
            SceneManager.MoveGameObjectToScene(CreateProbeObject(rootName, value), scene);
            return scene;
        }

        private Scene OpenScene(string path)
        {
            return TrackScene(EditorSceneManager.OpenScene(path, OpenSceneMode.Additive));
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
    }
}
