using System.Collections;
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
    public class PrefabSaveEnforcementTests
    {
        private const string k_TestFolderName = "__UValidationPrefabSaveTests";
        private const string k_TestFolderPath = "Assets/" + k_TestFolderName;
        private const string k_ValidValue = "Valid";

        private bool m_BlockInvalidSaves;
        private Scene m_PreviewScene;

        [SetUp]
        public void SetUp()
        {
            m_BlockInvalidSaves = UValidationProjectSettings.Instance.BlockInvalidSaves;
            UValidationProjectSettings.Instance.BlockInvalidSaves = false;
            AssetValidator.PromptOnBlockedSave = false;
            StageUtility.GoToMainStage();
            AssetDatabase.DeleteAsset(k_TestFolderPath);
            AssetDatabase.CreateFolder("Assets", k_TestFolderName);
            m_PreviewScene = EditorSceneManager.NewPreviewScene();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            StageUtility.GoToMainStage();
            yield return null;

            UValidationProjectSettings.Instance.BlockInvalidSaves = false;
            EditorSceneManager.ClosePreviewScene(m_PreviewScene);
            AssetDatabase.DeleteAsset(k_TestFolderPath);

            AssetValidator.PromptOnBlockedSave = true;
            UValidationProjectSettings.Instance.BlockInvalidSaves = m_BlockInvalidSaves;
        }

        [Test]
        public void InvalidEditOnPrefabAsset_SaveAssets_IsBlocked()
        {
            var path = CreatePrefab("EditedAsset", k_ValidValue);
            var savedContents = ReadFile(path);
            var probe = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<PrefabValidationProbe>();
            probe.SetValue(string.Empty);
            EditorUtility.SetDirty(probe);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("EditedAsset"));
            AssetDatabase.SaveAssets();

            Assert.AreEqual(savedContents, ReadFile(path));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void InvalidEditOnPrefabAsset_SavePrefabAsset_IsBlocked()
        {
            var path = CreatePrefab("SavedAsset", k_ValidValue);
            var savedContents = ReadFile(path);
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var probe = prefabRoot.GetComponent<PrefabValidationProbe>();
            probe.SetValue(string.Empty);
            EditorUtility.SetDirty(probe);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("SavedAsset"));
            LogAssert.Expect(LogType.Log, new Regex("Prefab not saved due to OnWillSaveAssets callback"));
            PrefabUtility.SavePrefabAsset(prefabRoot);

            Assert.AreEqual(savedContents, ReadFile(path));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SaveAsPrefabAsset_InvalidContentsOverExistingPrefab_IsReportedAfterImport()
        {
            var path = CreatePrefab("Overwritten", k_ValidValue);
            var source = CreateProbeObject("Overwritten", string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("Overwritten"));
            LogAssert.Expect(LogType.Error, CreateSavedAssetFailurePattern("Overwritten"));
            PrefabUtility.SaveAsPrefabAsset(source, path);
            yield return null;

            Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(path, false));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ApplyOverrides_InvalidOverride_IsReportedAfterImport()
        {
            var path = CreatePrefab("Applied", k_ValidValue);
            var instance = InstantiateWithValue(path, string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("Applied"));
            LogAssert.Expect(LogType.Error, CreateSavedAssetFailurePattern("Applied"));
            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            yield return null;

            Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(path, false));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SaveAsPrefabAsset_ValidContentsOverInvalidPrefab_IsSaved()
        {
            var path = CreatePrefab("FixedBySaveAs", string.Empty);
            var source = CreateProbeObject("FixedBySaveAs", k_ValidValue);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            PrefabUtility.SaveAsPrefabAsset(source, path);
            yield return null;

            Assert.IsTrue(ValidationHelper.IsPrefabValidAtPath(path, false));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ApplyOverrides_ValidOverrideOnInvalidPrefab_IsSaved()
        {
            var path = CreatePrefab("FixedByApply", string.Empty);
            var instance = InstantiateWithValue(path, k_ValidValue);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            yield return null;

            Assert.IsTrue(ValidationHelper.IsPrefabValidAtPath(path, false));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NewInvalidPrefab_IsCreatedAndReportedAfterImport()
        {
            var path = $"{k_TestFolderPath}/Created.prefab";
            var source = CreateProbeObject("Created", string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("Created"));
            LogAssert.Expect(LogType.Error, CreateNewAssetFailurePattern("Created"));
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            yield return null;

            Assert.IsNotNull(prefab);
            Assert.IsTrue(File.Exists(GetAbsolutePath(path)));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NewInvalidPrefab_CreatedFromHierarchy_IsConnectedAndReportedAfterImport()
        {
            var path = $"{k_TestFolderPath}/Dragged.prefab";
            var source = CreateProbeObject("Dragged", string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("Dragged"));
            LogAssert.Expect(LogType.Error, CreateNewAssetFailurePattern("Dragged"));
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(source, path, InteractionMode.UserAction);
            yield return null;

            Assert.IsNotNull(prefab);
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(source));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NewInvalidPrefab_CanBeFixedAndSavedInPrefabMode()
        {
            var path = $"{k_TestFolderPath}/Configured.prefab";
            var source = CreateProbeObject("Configured", string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("Configured"));
            LogAssert.Expect(LogType.Error, CreateNewAssetFailurePattern("Configured"));
            PrefabUtility.SaveAsPrefabAsset(source, path);
            yield return null;

            var prefabStage = PrefabStageUtility.OpenPrefab(path);
            yield return null;

            prefabStage.prefabContentsRoot.GetComponent<PrefabValidationProbe>().SetValue(k_ValidValue);
            PrefabUtility.SaveAsPrefabAsset(prefabStage.prefabContentsRoot, path);
            StageUtility.GoToMainStage();
            yield return null;

            Assert.IsTrue(ValidationHelper.IsPrefabValidAtPath(path, false));
            LogAssert.NoUnexpectedReceived();
        }

        private string CreatePrefab(string name, string value)
        {
            var path = $"{k_TestFolderPath}/{name}.prefab";
            var source = CreateProbeObject(name, value);

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
                Assert.IsNotNull(prefab, $"Failed to create test prefab at '{path}'.");
                return path;
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        private GameObject CreateProbeObject(string name, string value)
        {
            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, m_PreviewScene);
            gameObject.AddComponent<PrefabValidationProbe>().SetValue(value);
            return gameObject;
        }

        private GameObject InstantiateWithValue(string path, string value)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, m_PreviewScene);
            var probe = instance.GetComponent<PrefabValidationProbe>();
            probe.SetValue(value);
            PrefabUtility.RecordPrefabInstancePropertyModifications(probe);
            return instance;
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

        private static Regex CreateSavedAssetFailurePattern(string prefabName)
        {
            return new Regex($@"UValidation found invalid data in '.*/{Regex.Escape(prefabName)}\.prefab' after Unity saved it");
        }

        private static Regex CreateNewAssetFailurePattern(string prefabName)
        {
            return new Regex($@"UValidation found invalid data in newly created asset '.*/{Regex.Escape(prefabName)}\.prefab'");
        }
    }
}
