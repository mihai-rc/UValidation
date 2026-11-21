using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using UValidation.Editor;
using static UValidation.Tests.ValidationTestUtility;

namespace UValidation.Tests
{
    public class PrefabValidationTests
    {
        private const string k_TestFolderName = "__UValidationPrefabTests";
        private const string k_TestFolderPath = "Assets/" + k_TestFolderName;

        private bool m_BlockInvalidSaves;

        [SetUp]
        public void SetUp()
        {
            m_BlockInvalidSaves = UValidationProjectSettings.Instance.BlockInvalidSaves;
            UValidationProjectSettings.Instance.BlockInvalidSaves = false;
            StageUtility.GoToMainStage();
            AssetDatabase.DeleteAsset(k_TestFolderPath);
            AssetDatabase.CreateFolder("Assets", k_TestFolderName);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            StageUtility.GoToMainStage();
            yield return null;

            AssetDatabase.DeleteAsset(k_TestFolderPath);
            UValidationProjectSettings.Instance.BlockInvalidSaves = m_BlockInvalidSaves;
        }

        [Test]
        public void IsPrefabValidAtPath_NoPrefabStage_ValidatesStoredPrefabsIndependently()
        {
            var validPath = CreatePrefab("Valid", VALID_VALUE);
            var invalidPath = CreatePrefab("Invalid", string.Empty);

            Assert.IsTrue(ValidationHelper.IsPrefabValidAtPath(validPath, false));
            Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(invalidPath, false));
        }

        [UnityTest]
        public IEnumerator IsPrefabValidAtPath_OtherValidPrefabOpen_StillRejectsRequestedInvalidPrefab()
        {
            var validPath = CreatePrefab("OpenValid", VALID_VALUE);
            var invalidPath = CreatePrefab("RequestedInvalid", string.Empty);

            PrefabStageUtility.OpenPrefab(validPath);
            yield return null;

            Assert.AreEqual(validPath, PrefabStageUtility.GetCurrentPrefabStage()?.assetPath);
            Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(invalidPath, false));
        }

        [UnityTest]
        public IEnumerator IsPrefabValidAtPath_OtherInvalidPrefabOpen_StillAcceptsRequestedValidPrefab()
        {
            var invalidPath = CreatePrefab("OpenInvalid", string.Empty);
            var validPath = CreatePrefab("RequestedValid", VALID_VALUE);

            PrefabStageUtility.OpenPrefab(invalidPath);
            yield return null;

            Assert.AreEqual(invalidPath, PrefabStageUtility.GetCurrentPrefabStage()?.assetPath);
            Assert.IsTrue(ValidationHelper.IsPrefabValidAtPath(validPath, false));
        }

        [UnityTest]
        public IEnumerator IsPrefabValidAtPath_MatchingPrefabStage_UsesUnsavedContents()
        {
            var prefabPath = CreatePrefab("MatchingStage", VALID_VALUE);

            PrefabStageUtility.OpenPrefab(prefabPath);
            yield return null;

            var prefabRoot = PrefabStageUtility.GetCurrentPrefabStage()?.prefabContentsRoot;
            var probe = prefabRoot != null
                ? prefabRoot.GetComponent<PrefabValidationProbe>()
                : null;

            Assert.IsNotNull(probe);

            try
            {
                probe.SetValue(string.Empty);
                Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(prefabPath, false));
            }
            finally
            {
                probe.SetValue(VALID_VALUE);
            }
        }

        [Test]
        public void IsPrefabValidAtPath_MissingPrefab_IsInvalidAndPendingForSaveEnforcement()
        {
            var missingPath = k_TestFolderPath + "/Missing.prefab";

            Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(missingPath, false));
            Assert.AreEqual(
                AssetValidationState.ValidateAfterImport,
                ValidationHelper.ValidateAssetAtPathForSave(missingPath, false));
        }

        [Test]
        public void IsGameObjectValid_MissingScript_IsRejectedAndReportsOwningGameObject()
        {
            var prefabPath = CreatePrefabWithMissingScript();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            Assert.IsNotNull(prefab);
            Assert.AreEqual(1, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab));
            Assert.IsFalse(ValidationHelper.IsGameObjectValid(prefab, false));

            LogAssert.Expect(
                LogType.Error,
                "[Validation] GameObject 'MissingScript' contains 1 missing MonoBehaviour script.");

            Assert.IsFalse(ValidationHelper.IsGameObjectValid(prefab, true));
            Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(prefabPath, false));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void IsPrefabValidAtPath_InvalidParentAndChild_ReportsEveryFailure()
        {
            var source = CreateProbeObject("InvalidParent", string.Empty);
            CreateProbeObject("InvalidChild", string.Empty).transform.SetParent(source.transform);
            var prefabPath = SaveAsPrefab(source, $"{k_TestFolderPath}/InvalidHierarchy.prefab");

            LogAssert.Expect(
                LogType.Error,
                CreateValidationErrorPattern("InvalidHierarchy"));

            LogAssert.Expect(
                LogType.Error,
                CreateValidationErrorPattern("InvalidChild"));

            Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(prefabPath, true));
            LogAssert.NoUnexpectedReceived();
        }

        private static string CreatePrefab(string name, string value)
        {
            return SaveAsPrefab(CreateProbeObject(name, value), $"{k_TestFolderPath}/{name}.prefab");
        }

        private static string CreatePrefabWithMissingScript()
        {
            var prefabPath = CreatePrefab("MissingScript", VALID_VALUE);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var probe = prefab.GetComponent<PrefabValidationProbe>();
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(probe));
            var scriptGuid = AssetDatabase.AssetPathToGUID(scriptPath);
            var absolutePrefabPath = GetAbsolutePath(prefabPath);
            var yaml = File.ReadAllText(absolutePrefabPath);

            Assert.That(yaml, Does.Contain($"guid: {scriptGuid}"));
            File.WriteAllText(absolutePrefabPath, yaml.Replace($"guid: {scriptGuid}", $"guid: {MISSING_SCRIPT_GUID}"));
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);

            return prefabPath;
        }
    }
}
