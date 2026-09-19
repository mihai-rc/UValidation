using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UValidation.Editor;

namespace UValidation.Showcase.Tests
{
    public class ValidationShowcaseTests
    {
        private const string k_ShowcaseScenePath = "Assets/Scenes/ValidationAttributeShowcase.unity";
        private Scene m_Scene;

        [OneTimeSetUp]
        public void OpenShowcaseScene()
        {
            m_Scene = EditorSceneManager.OpenScene(k_ShowcaseScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void ShowcaseScene_Exists()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(k_ShowcaseScenePath));
            Assert.IsTrue(m_Scene.IsValid());
        }

        [TestCase("01 - NotNull", false)]
        [TestCase("02 - NotEmpty", false)]
        [TestCase("03 - HasNoNulls", false)]
        [TestCase("04 - HasNoEmpties", false)]
        [TestCase("05 - IsValid", false)]
        [TestCase("06 - Combined Attributes", false)]
        [TestCase("07 - IValidatable - Pass", true)]
        [TestCase("08 - IValidatable - Fail", false)]
        public void ShowcaseSection_HasExpectedValidationResult(string objectName, bool expectedResult)
        {
            var target = FindGameObject(objectName);
            Assert.AreEqual(expectedResult, ValidationHelper.IsGameObjectValid(target, false));
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
