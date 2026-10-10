using System.Collections;
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
    }
}
