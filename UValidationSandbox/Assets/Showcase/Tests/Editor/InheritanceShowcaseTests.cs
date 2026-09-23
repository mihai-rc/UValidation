using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using NUnit.Framework;
using UValidation.Editor;

namespace UValidation.Showcase.Tests
{
    /// <summary>
    /// Verifies that required values are validated at every inheritance level.
    /// </summary>
    public class InheritanceShowcaseTests
    {
        private static readonly string[] s_PropertyPaths =
        {
            "m_BaseReference",
            "<BaseLabel>k__BackingField",
            "m_MiddleLabel",
            "m_DerivedReference"
        };

        private Scene m_Scene;

        /// <summary>
        /// Opens the saved examples in an isolated preview scene.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            var paths = AssetDatabase.FindAssets("ValidationInheritanceShowcase t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path) == "ValidationInheritanceShowcase")
                .ToArray();

            Assert.That(paths, Has.Length.EqualTo(1), "Exactly one inheritance showcase scene must exist.");
            m_Scene = EditorSceneManager.OpenPreviewScene(paths[0]);
            Assert.That(m_Scene.IsValid(), Is.True);
        }

        /// <summary>
        /// Closes the preview without replacing or saving the user's scenes.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (m_Scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(m_Scene);
            }
        }

        /// <summary>
        /// Checks each isolated example against the intended inheritance behavior.
        /// </summary>
        /// <param name="objectName"> Name of the saved example object. </param>
        /// <param name="invalidPropertyPath"> The only invalid property, or null for the valid control. </param>
        [TestCase("01 - All Levels Valid", null)]
        [TestCase("02 - Base Private Field Missing", "m_BaseReference")]
        [TestCase("03 - Middle Private Field Empty", "m_MiddleLabel")]
        [TestCase("04 - Derived Private Field Missing", "m_DerivedReference")]
        [TestCase("05 - Base Auto-Property Empty", "<BaseLabel>k__BackingField")]
        public void InheritanceChain_HasExpectedValidationResult(string objectName, string invalidPropertyPath)
        {
            var target = m_Scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<InheritanceShowcase>(true))
                .Single(component => component.name == objectName);

            using (var serializedObject = new SerializedObject(target))
            {
                foreach (var propertyPath in s_PropertyPaths)
                {
                    var property = serializedObject.FindProperty(propertyPath);
                    Assert.That(property, Is.Not.Null, $"Unity must serialize '{propertyPath}'.");

                    var isEmpty = property.propertyType == SerializedPropertyType.ObjectReference
                        ? property.objectReferenceValue == null
                        : string.IsNullOrEmpty(property.stringValue);

                    Assert.That(isEmpty, Is.EqualTo(propertyPath == invalidPropertyPath),
                        $"'{objectName}' must isolate its intended failure; check '{propertyPath}'.");
                }
            }

            Assert.That(ValidationHelper.IsScriptValid(target, false), Is.EqualTo(invalidPropertyPath == null),
                $"'{objectName}' must validate required fields across the entire inheritance chain.");
        }
    }
}
