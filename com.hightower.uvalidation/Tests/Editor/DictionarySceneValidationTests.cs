#if UNITY_6000_6_OR_NEWER
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
using static UValidation.Tests.ValidationTestUtility;

namespace UValidation.Tests
{
    public class DictionarySceneValidationTests
    {
        private const string k_TestFolderName = "__UValidationDictionarySceneTests";
        private const string k_TestFolderPath = "Assets/" + k_TestFolderName;
        private const string k_BaseScenePath = k_TestFolderPath + "/Base.unity";

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
            m_OpenedBaseScene = OpenSavedSceneIfUntitled(k_BaseScenePath);
        }

        [TearDown]
        public void TearDown()
        {
            try
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
            }
            finally
            {
                AssetValidator.PromptOnBlockedSave = true;
                UValidationProjectSettings.Instance.BlockInvalidSaves = m_BlockInvalidSaves;
            }
        }

        [Test]
        public void StoredValidDictionaryScene_PassesPathValidationAfterReload()
        {
            var path = CreateStoredScene("ValidDictionary", probe => probe.SetValid());

            Assert.IsFalse(SceneManager.GetSceneByPath(path).IsValid());
            Assert.IsTrue(ValidationHelper.IsSceneValidAtPath(path, false));
            Assert.IsFalse(SceneManager.GetSceneByPath(path).IsValid());
        }

        [Test]
        public void StoredInvalidNestedDictionaryValue_FailsPathValidationAfterReload()
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
        public void InvalidDictionaryEditOnOpenScene_SaveInPlace_IsBlocked()
        {
            var path = CreateStoredScene("BlockedDictionarySave", probe => probe.SetValid());
            var savedContents = File.ReadAllText(GetAbsolutePath(path));
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            m_Scenes.Add(scene);

            var probe = scene.GetRootGameObjects()[0].GetComponent<DictionaryValidationProbe>();
            probe.SetInvalidLabelValue();
            EditorSceneManager.MarkSceneDirty(scene);
            UValidationProjectSettings.Instance.BlockInvalidSaves = true;

            LogAssert.Expect(
                LogType.Error,
                CreateDictionaryValidationErrorPattern("BlockedDictionarySave", "m_Labels"));

            Assert.IsFalse(EditorSceneManager.SaveScene(scene));
            Assert.AreEqual(savedContents, File.ReadAllText(GetAbsolutePath(path)));
            LogAssert.NoUnexpectedReceived();
        }

        private string CreateStoredScene(string name, System.Action<DictionaryValidationProbe> configure)
        {
            var path = $"{k_TestFolderPath}/{name}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            m_Scenes.Add(scene);

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
