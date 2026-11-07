#if UNITY_6000_6_OR_NEWER
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UValidation.Editor;

namespace UValidation.Showcase.Tests
{
    public class DictionaryShowcaseTests
    {
        private const string k_ShowcaseScenePath = "Assets/Scenes/ValidationDictionaryShowcase.unity";
        private Scene m_Scene;

        [OneTimeSetUp]
        public void OpenShowcaseScene()
        {
            m_Scene = EditorSceneManager.OpenPreviewScene(k_ShowcaseScenePath);
        }

        [OneTimeTearDown]
        public void CloseShowcaseScene()
        {
            if (m_Scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(m_Scene);
            }
        }

        [Test]
        public void ShowcaseScene_Exists()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(k_ShowcaseScenePath));
            Assert.IsTrue(m_Scene.IsValid());
        }

        [TestCase("00 - Overview", true)]
        [TestCase("01 - NotEmpty", false)]
        [TestCase("02 - HasNoNulls", false)]
        [TestCase("03 - HasNoEmpties", false)]
        [TestCase("04 - IsValid", false)]
        public void ShowcaseSection_HasExpectedValidationResult(string objectName, bool expectedResult)
        {
            var target = FindGameObject(objectName);
            Assert.AreEqual(expectedResult, ValidationHelper.IsGameObjectValid(target, false));
        }

        [Test]
        public void NotEmptySection_PreservesNullKeyRowsButCountsNoEffectiveEntries()
        {
            var component = FindGameObject("01 - NotEmpty").GetComponent<DictionaryNotEmptyShowcase>();
            using var serializedObject = new SerializedObject(component);
            var dictionary = serializedObject.FindProperty("m_NullKeyPlaceholders");
            var ignoredEntries = dictionary.GetDictionaryIgnoredEntries();

            Assert.AreEqual(2, dictionary.arraySize);
            Assert.AreEqual(2, ignoredEntries.nullKeyEntryIndices.Length);
            Assert.IsFalse(FieldDrawerHelper.HasEffectiveDictionaryEntries(dictionary));
        }

        [Test]
        public void ShowcaseScene_IsIntentionallyInvalid()
        {
            Assert.IsFalse(ValidationHelper.IsSceneValid(ref m_Scene, false));
        }

        private GameObject FindGameObject(string objectName)
        {
            return m_Scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject)
                .Single(gameObject => gameObject.name == objectName);
        }
    }
}
#endif
