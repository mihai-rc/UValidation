#if UNITY_6000_6_OR_NEWER
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using UValidation.Editor;
using static UValidation.Tests.ValidationTestUtility;

namespace UValidation.Tests
{
    public class DictionarySceneValidationTests : SceneSaveTestFixture
    {
        /// <inheritdoc />
        protected override string TestFolderName => "__UValidationDictionarySceneTests";

        [Test]
        public void IsSceneValidAtPath_StoredValidDictionary_PassesAfterReload()
        {
            var path = CreateStoredScene("ValidDictionary", probe => probe.SetValid());

            Assert.IsFalse(SceneManager.GetSceneByPath(path).IsValid());
            Assert.IsTrue(ValidationHelper.IsSceneValidAtPath(path, false));
            Assert.IsFalse(SceneManager.GetSceneByPath(path).IsValid());
        }

        [Test]
        public void IsSceneValidAtPath_StoredInvalidNestedDictionaryValue_FailsAfterReload()
        {
            var path = CreateStoredScene("InvalidNestedDictionary", probe => probe.SetInvalidNestedValue());

            LogAssert.Expect(
                LogType.Error,
                CreateDictionaryValidationErrorPattern(
                    "InvalidNestedDictionary",
                    @"m_Entries\[0\]\.Value\.m_Name"));

            Assert.IsFalse(ValidationHelper.IsSceneValidAtPath(path, true));
            Assert.IsFalse(SceneManager.GetSceneByPath(path).IsValid());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SaveScene_InvalidDictionaryEditInPlace_IsBlocked()
        {
            var path = CreateStoredScene("BlockedDictionarySave", probe => probe.SetValid());
            var savedContents = File.ReadAllText(GetAbsolutePath(path));
            var scene = TrackScene(EditorSceneManager.OpenScene(path, OpenSceneMode.Additive));

            var probe = scene.GetRootGameObjects()[0].GetComponent<DictionaryValidationProbe>();
            probe.SetInvalidLabelValue();
            EditorSceneManager.MarkSceneDirty(scene);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(LogType.Error, CreateDictionaryValidationErrorPattern("BlockedDictionarySave", "m_Labels"));

            Assert.IsFalse(EditorSceneManager.SaveScene(scene));
            Assert.AreEqual(savedContents, File.ReadAllText(GetAbsolutePath(path)));

            LogAssert.NoUnexpectedReceived();
        }

        private string CreateStoredScene(string name, Action<DictionaryValidationProbe> configure)
        {
            var path = $"{TestFolderPath}/{name}.unity";
            var scene = TrackScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive));

            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);

            var probe = root.AddComponent<DictionaryValidationProbe>();
            configure(probe);

            Assert.IsTrue(EditorSceneManager.SaveScene(scene, path), $"Failed to create test scene at '{path}'.");
            Assert.IsTrue(EditorSceneManager.CloseScene(scene, true));
            return path;
        }

        private static Regex CreateDictionaryValidationErrorPattern(string contextName, string variablePattern)
        {
            return new Regex(
                @"(?s)\[Validation\].*" +
                $@" - Context: {Regex.Escape(contextName)}\r?\n.*" +
                $@" - Variable: {variablePattern}\r?\n");
        }
    }
}
#endif
