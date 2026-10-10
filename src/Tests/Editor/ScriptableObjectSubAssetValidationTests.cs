using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UValidation.Editor;

namespace UValidation.Tests
{
    public class ScriptableObjectSubAssetValidationTests
    {
        private const string k_TestFolderName = "__UValidationScriptableObjectSubAssetTests";
        private const string k_TestFolderPath = "Assets/" + k_TestFolderName;
        private const string k_ValidValue = "Valid";

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
            AssetDatabase.DeleteAsset(k_TestFolderPath);
            UValidationProjectSettings.Instance.BlockInvalidSaves = m_BlockInvalidSaves;
        }

        [Test]
        public void ValidMainAsset_InvalidSubAsset_Fails()
        {
            var path = CreateAsset("InvalidSubAsset", k_ValidValue, string.Empty);

            Assert.IsFalse(ValidationHelper.IsScriptableObjectValidAtPath(path, false));
        }

        [Test]
        public void InvalidMainAsset_ValidSubAsset_Fails()
        {
            var path = CreateAsset("InvalidMainAsset", string.Empty, k_ValidValue);

            Assert.IsFalse(ValidationHelper.IsScriptableObjectValidAtPath(path, false));
        }

        [Test]
        public void ValidMainAsset_ValidSubAsset_Passes()
        {
            var path = CreateAsset("AllValid", k_ValidValue, k_ValidValue);

            Assert.IsTrue(ValidationHelper.IsScriptableObjectValidAtPath(path, false));
        }

        [Test]
        public void MissingAsset_IsInvalidPubliclyAndPendingForSaveEnforcement()
        {
            var missingPath = k_TestFolderPath + "/Missing.asset";

            Assert.IsFalse(ValidationHelper.IsScriptableObjectValidAtPath(missingPath, false));
            Assert.AreEqual(
                AssetValidationState.PendingCreation,
                ValidationHelper.ValidateAssetAtPathForSave(missingPath, false));
        }

        [Test]
        public void NonScriptableObjectAsset_IsNotTreatedAsInvalidBySaveEnforcement()
        {
            var path = k_TestFolderPath + "/Animation.asset";
            AssetDatabase.CreateAsset(new AnimationClip(), path);

            Assert.IsFalse(ValidationHelper.IsScriptableObjectValidAtPath(path, false));
            Assert.AreEqual(
                AssetValidationState.Valid,
                ValidationHelper.ValidateAssetAtPathForSave(path, false));
        }

        [UnityTest]
        public IEnumerator NewlyCreatedInvalidAsset_IsReportedAfterInitialImport()
        {
            var path = k_TestFolderPath + "/NewInvalid.asset";
            var asset = ScriptableObject.CreateInstance<ScriptableObjectValidationProbe>();
            asset.SetValue(string.Empty);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(
                LogType.Error,
                new Regex(@"(?s)\[Validation\] String is null or empty!.*Variable: m_Value"));
            LogAssert.Expect(
                LogType.Error,
                new Regex("UValidation found invalid data in newly created asset.*NewInvalid\\.asset"));

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            LogAssert.NoUnexpectedReceived();
        }

        private static string CreateAsset(string name, string mainValue, string subAssetValue)
        {
            var path = $"{k_TestFolderPath}/{name}.asset";
            var mainAsset = ScriptableObject.CreateInstance<ScriptableObjectValidationProbe>();
            var subAsset = ScriptableObject.CreateInstance<ScriptableObjectValidationProbe>();
            mainAsset.name = name;
            subAsset.name = name + " SubAsset";
            mainAsset.SetValue(mainValue);
            subAsset.SetValue(subAssetValue);

            AssetDatabase.CreateAsset(mainAsset, path);
            AssetDatabase.AddObjectToAsset(subAsset, mainAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            return path;
        }
    }
}
