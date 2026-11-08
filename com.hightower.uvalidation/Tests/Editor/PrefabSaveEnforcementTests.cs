using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UValidation.Editor;
using static UValidation.Tests.ValidationTestUtility;

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
            try
            {
                StageUtility.GoToMainStage();
                yield return null;

                UValidationProjectSettings.Instance.BlockInvalidSaves = false;
                if (m_PreviewScene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(m_PreviewScene);
                }

                AssetDatabase.DeleteAsset(k_TestFolderPath);
            }
            finally
            {
                AssetValidator.PromptOnBlockedSave = true;
                UValidationProjectSettings.Instance.BlockInvalidSaves = m_BlockInvalidSaves;
            }
        }

        [UnityTest]
        public IEnumerator InvalidEditOnPrefabAsset_SaveAssets_IsReportedAfterImport()
        {
            var path = CreatePrefab("EditedAsset", k_ValidValue);
            var probe = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<PrefabValidationProbe>();
            probe.SetValue(string.Empty);
            EditorUtility.SetDirty(probe);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("EditedAsset"));
            LogAssert.Expect(LogType.Error, CreateSavedAssetFailurePattern("EditedAsset", ".prefab"));
            AssetDatabase.SaveAssets();
            yield return null;

            Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(path, false));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator InvalidEditOnPrefabAsset_SavePrefabAsset_IsReportedAfterImport()
        {
            var path = CreatePrefab("SavedAsset", k_ValidValue);
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var probe = prefabRoot.GetComponent<PrefabValidationProbe>();
            probe.SetValue(string.Empty);
            EditorUtility.SetDirty(probe);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("SavedAsset"));
            LogAssert.Expect(LogType.Error, CreateSavedAssetFailurePattern("SavedAsset", ".prefab"));
            PrefabUtility.SavePrefabAsset(prefabRoot);
            yield return null;

            Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(path, false));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SaveAsPrefabAsset_InvalidContentsOverExistingPrefab_IsReportedAfterImport()
        {
            var path = CreatePrefab("Overwritten", k_ValidValue);
            var source = CreateProbeObject("Overwritten", string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateValidationErrorPattern("Overwritten"));
            LogAssert.Expect(LogType.Error, CreateSavedAssetFailurePattern("Overwritten", ".prefab"));
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
            LogAssert.Expect(LogType.Error, CreateSavedAssetFailurePattern("Applied", ".prefab"));
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
        public IEnumerator SaveAsPrefabAsset_ValidContentsOverDirtyInvalidPrefab_IsSaved()
        {
            var path = CreatePrefab("DirtyTarget", k_ValidValue);
            var loadedProbe = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<PrefabValidationProbe>();
            loadedProbe.SetValue(string.Empty);
            EditorUtility.SetDirty(loadedProbe);
            var source = CreateProbeObject("DirtyTarget", k_ValidValue);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            PrefabUtility.SaveAsPrefabAsset(source, path);
            yield return null;

            Assert.IsFalse(
                ValidationHelper.IsPrefabValidAtPath(path, false),
                "The deliberately dirty loaded object should remain invalid.");

            Assert.AreEqual(
                AssetValidationState.Valid,
                ValidationHelper.ValidateImportedAssetAtPath(path, false),
                "The separately written prefab contents should be valid.");

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
            LogAssert.Expect(LogType.Error, CreateNewAssetFailurePattern("Created", ".prefab"));
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
            LogAssert.Expect(LogType.Error, CreateNewAssetFailurePattern("Dragged", ".prefab"));
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
            LogAssert.Expect(LogType.Error, CreateNewAssetFailurePattern("Configured", ".prefab"));
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

        [UnityTest]
        public IEnumerator PendingValidation_WithoutImport_Expires()
        {
            var path = CreatePrefab("Canceled", k_ValidValue);

            PendingAssetValidation.Queue(path);
            Assert.IsTrue(PendingAssetValidation.Contains(path));
            yield return null;
            yield return null;

            Assert.IsFalse(PendingAssetValidation.Contains(path));
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

    }
}
