using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using UValidation.Editor;
using static UValidation.Tests.ValidationTestUtility;

namespace UValidation.Tests
{
    public class ScriptableObjectSubAssetValidationTests
    {
        private const string k_TestFolderName = "__UValidationScriptableObjectSubAssetTests";
        private const string k_TestFolderPath = "Assets/" + k_TestFolderName;
        private const string k_MainAssetFileId = "&11400000";
        private const string k_YamlDocumentSeparator = "--- !u!";

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
        public void IsScriptableObjectValidAtPath_InvalidSubAsset_Fails()
        {
            var path = CreateAsset("InvalidSubAsset", VALID_VALUE, string.Empty);
            Assert.IsFalse(ValidationHelper.IsScriptableObjectValidAtPath(path, false));
        }

        [Test]
        public void IsScriptableObjectValidAtPath_InvalidMainAsset_Fails()
        {
            var path = CreateAsset("InvalidMainAsset", string.Empty, VALID_VALUE);
            Assert.IsFalse(ValidationHelper.IsScriptableObjectValidAtPath(path, false));
        }

        [Test]
        public void IsScriptableObjectValidAtPath_AllObjectsValid_Passes()
        {
            var path = CreateAsset("AllValid", VALID_VALUE, VALID_VALUE);
            Assert.IsTrue(ValidationHelper.IsScriptableObjectValidAtPath(path, false));
        }

        [Test]
        public void IsScriptableObjectValidAtPath_InvalidMainAndSubAsset_ReportsEveryFailure()
        {
            var path = CreateAsset("BothInvalid", string.Empty, string.Empty);
            var expectedError = new Regex(
                @"(?s)\[Validation\] String is null or empty!.*" +
                @" - Context: BothInvalid(?: SubAsset)?\r?\n.*" +
                @" - Variable: m_Value");

            LogAssert.Expect(LogType.Error, expectedError);
            LogAssert.Expect(LogType.Error, expectedError);

            Assert.IsFalse(ValidationHelper.IsScriptableObjectValidAtPath(path, true));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void IsScriptableObjectValidAtPath_SubAssetWithMissingScript_IsRejectedAndReported()
        {
            var path = CreateAssetWithMissingScript("MissingSubAssetScript", false);

            Assert.IsFalse(ValidationHelper.IsScriptableObjectValidAtPath(path, false));
            Assert.AreEqual(AssetValidationState.Invalid, ValidationHelper.ValidateAssetAtPathForSave(path, false));

            LogAssert.Expect(LogType.Error, $"[Validation] Asset '{path}' contains 1 object whose script is missing.");
            Assert.IsFalse(ValidationHelper.IsScriptableObjectValidAtPath(path, true));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ValidateAssetAtPathForSave_MainAssetWithMissingScript_IsRejectedAndReported()
        {
            var path = CreateAssetWithMissingScript("MissingMainAssetScript", true);

            Assert.IsFalse(ValidationHelper.IsScriptableObjectValidAtPath(path, false));
            Assert.AreEqual(AssetValidationState.Invalid, ValidationHelper.ValidateAssetAtPathForSave(path, false));

            LogAssert.Expect(LogType.Error, $"[Validation] Asset '{path}' contains 1 object whose script is missing.");
            Assert.AreEqual(AssetValidationState.Invalid, ValidationHelper.ValidateAssetAtPathForSave(path, true));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void IsScriptableObjectValidAtPath_MissingAsset_IsInvalidAndPendingForSaveEnforcement()
        {
            var missingPath = k_TestFolderPath + "/Missing.asset";

            Assert.IsFalse(ValidationHelper.IsScriptableObjectValidAtPath(missingPath, false));
            Assert.AreEqual(AssetValidationState.ValidateAfterImport, ValidationHelper.ValidateAssetAtPathForSave(missingPath, false));
        }

        [Test]
        public void ValidateAssetAtPathForSave_NonScriptableObjectAsset_IsValid()
        {
            var path = k_TestFolderPath + "/Animation.asset";
            AssetDatabase.CreateAsset(new AnimationClip(), path);

            Assert.IsFalse(ValidationHelper.IsScriptableObjectValidAtPath(path, false));
            Assert.AreEqual(AssetValidationState.Valid, ValidationHelper.ValidateAssetAtPathForSave(path, false));
        }

        [UnityTest]
        public IEnumerator CreateAsset_NewInvalidAsset_IsReportedAfterInitialImport()
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
                CreateNewAssetFailurePattern("NewInvalid", ".asset"));

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

        private static string CreateAssetWithMissingScript(string name, bool breakMainAsset)
        {
            var path = CreateAsset(name, VALID_VALUE, VALID_VALUE);
            var mainAsset = AssetDatabase.LoadAssetAtPath<ScriptableObjectValidationProbe>(path);
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(mainAsset));
            var scriptGuid = AssetDatabase.AssetPathToGUID(scriptPath);
            var absolutePath = GetAbsolutePath(path);
            var documents = File
                .ReadAllText(absolutePath)
                .Split(new[] { k_YamlDocumentSeparator }, StringSplitOptions.None);

            // Break only the requested object so the other one still loads with its script.
            for (var i = 1; i < documents.Length; i++)
            {
                var isMainAsset = documents[i].Split('\n')[0].Contains(k_MainAssetFileId);
                if (isMainAsset == breakMainAsset)
                {
                    Assert.That(documents[i], Does.Contain($"guid: {scriptGuid}"));
                    documents[i] = documents[i].Replace($"guid: {scriptGuid}", $"guid: {MISSING_SCRIPT_GUID}");
                }
            }

            File.WriteAllText(absolutePath, string.Join(k_YamlDocumentSeparator, documents));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }
    }
}
