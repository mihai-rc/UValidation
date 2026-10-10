using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UValidation.Editor;

namespace UValidation.Tests
{
    public class PrefabValidationTests
    {
        private const string k_TestFolderName = "__UValidationPrefabTests";
        private const string k_TestFolderPath = "Assets/" + k_TestFolderName;
        private const string k_MissingScriptGuid = "ffffffffffffffffffffffffffffffff";
        private const string k_ValidValue = "Valid";

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
        public void NoPrefabStage_ValidatesStoredPrefabsIndependently()
        {
            var validPath = CreatePrefab("Valid", k_ValidValue);
            var invalidPath = CreatePrefab("Invalid", string.Empty);

            Assert.IsTrue(ValidationHelper.IsPrefabValidAtPath(validPath, false));
            Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(invalidPath, false));
        }

        [UnityTest]
        public IEnumerator OpenValidPrefab_DoesNotMaskInvalidRequestedPrefab()
        {
            var validPath = CreatePrefab("OpenValid", k_ValidValue);
            var invalidPath = CreatePrefab("RequestedInvalid", string.Empty);

            PrefabStageUtility.OpenPrefab(validPath);
            yield return null;

            Assert.AreEqual(validPath, PrefabStageUtility.GetCurrentPrefabStage()?.assetPath);
            Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(invalidPath, false));
        }

        [UnityTest]
        public IEnumerator OpenInvalidPrefab_DoesNotRejectValidRequestedPrefab()
        {
            var invalidPath = CreatePrefab("OpenInvalid", string.Empty);
            var validPath = CreatePrefab("RequestedValid", k_ValidValue);

            PrefabStageUtility.OpenPrefab(invalidPath);
            yield return null;

            Assert.AreEqual(invalidPath, PrefabStageUtility.GetCurrentPrefabStage()?.assetPath);
            Assert.IsTrue(ValidationHelper.IsPrefabValidAtPath(validPath, false));
        }

        [UnityTest]
        public IEnumerator MatchingPrefabStage_UsesUnsavedContents()
        {
            var prefabPath = CreatePrefab("MatchingStage", k_ValidValue);

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
                probe.SetValue(k_ValidValue);
            }
        }

        [Test]
        public void MissingPrefabPath_IsInvalidPubliclyAndPendingForSaveEnforcement()
        {
            var missingPath = k_TestFolderPath + "/Missing.prefab";

            Assert.IsFalse(ValidationHelper.IsPrefabValidAtPath(missingPath, false));
            Assert.AreEqual(
                AssetValidationState.PendingCreation,
                ValidationHelper.ValidateAssetAtPathForSave(missingPath, false));
        }

        [Test]
        public void MissingScript_IsRejectedAndReportsOwningGameObject()
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

        private static string CreatePrefab(string name, string value)
        {
            var path = $"{k_TestFolderPath}/{name}.prefab";
            var source = new GameObject(name);

            try
            {
                source.AddComponent<PrefabValidationProbe>().SetValue(value);
                var prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
                Assert.IsNotNull(prefab, $"Failed to create test prefab at '{path}'.");
                return path;
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        private static string CreatePrefabWithMissingScript()
        {
            var prefabPath = CreatePrefab("MissingScript", k_ValidValue);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var probe = prefab.GetComponent<PrefabValidationProbe>();
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(probe));
            var scriptGuid = AssetDatabase.AssetPathToGUID(scriptPath);
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            var absolutePrefabPath = Path.Combine(projectPath, prefabPath);
            var yaml = File.ReadAllText(absolutePrefabPath);

            Assert.That(yaml, Does.Contain($"guid: {scriptGuid}"));
            File.WriteAllText(
                absolutePrefabPath,
                yaml.Replace($"guid: {scriptGuid}", $"guid: {k_MissingScriptGuid}"));
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
            return prefabPath;
        }
    }
}
