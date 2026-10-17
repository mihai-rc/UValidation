using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UValidation.Editor;

namespace UValidation.Tests
{
    public class EditorFeedbackTests
    {
        private const string k_TestFolderName = "__UValidationEditorFeedbackTests";
        private const string k_TestFolderPath = "Assets/" + k_TestFolderName;
        private const string k_ValidValue = "Valid";

        [SetUp]
        public void SetUp()
        {
            ValidationResultCache.Clear();
            AssetDatabase.DeleteAsset(k_TestFolderPath);
            AssetDatabase.CreateFolder("Assets", k_TestFolderName);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(k_TestFolderPath);
            ValidationResultCache.Clear();
        }

        [Test]
        public void HeaderTargets_SceneObjectWithInvalidComponent_IsInvalid()
        {
            var gameObject = CreateProbeObject("InvalidSceneObject", string.Empty);
            try
            {
                Assert.AreEqual(1, ValidationFailureEditorHandler.CountInvalidHeaderTargets(new Object[] { gameObject }));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void HeaderTargets_MultiSelection_CountsEveryInvalidTarget()
        {
            var valid = CreateScriptableObject(k_ValidValue);
            var firstInvalid = CreateScriptableObject(string.Empty);
            var secondInvalid = CreateScriptableObject(string.Empty);
            try
            {
                var targets = new Object[] { valid, firstInvalid, secondInvalid };
                Assert.AreEqual(2, ValidationFailureEditorHandler.CountInvalidHeaderTargets(targets));
            }
            finally
            {
                Object.DestroyImmediate(valid);
                Object.DestroyImmediate(firstInvalid);
                Object.DestroyImmediate(secondInvalid);
            }
        }

        [Test]
        public void HeaderTargets_PrefabAssetWithInvalidChild_IsInvalidOnlyThroughItsRoot()
        {
            var path = $"{k_TestFolderPath}/InvalidChild.prefab";
            var source = CreateProbeObject("ValidRoot", k_ValidValue);
            try
            {
                var child = CreateProbeObject("InvalidChild", string.Empty);
                child.transform.SetParent(source.transform);
                PrefabUtility.SaveAsPrefabAsset(source, path);
            }
            finally
            {
                Object.DestroyImmediate(source);
            }

            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var importer = AssetImporter.GetAtPath(path);

            Assert.AreEqual(1, ValidationFailureEditorHandler.CountInvalidHeaderTargets(new Object[] { prefabRoot }));
            Assert.AreEqual(0, ValidationFailureEditorHandler.CountInvalidHeaderTargets(new Object[] { importer }));
        }

        [Test]
        public void Cache_KeepsResultUntilCleared()
        {
            var gameObject = CreateProbeObject("Cached", k_ValidValue);
            try
            {
                Assert.IsTrue(ValidationResultCache.IsObjectValid(gameObject));

                gameObject.GetComponent<PrefabValidationProbe>().SetValue(string.Empty);
                Assert.IsTrue(ValidationResultCache.IsObjectValid(gameObject));

                ValidationResultCache.Clear();
                Assert.IsFalse(ValidationResultCache.IsObjectValid(gameObject));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Cache_RecordedPropertyChange_InvalidatesResult()
        {
            var gameObject = CreateProbeObject("Edited", k_ValidValue);
            try
            {
                yield return null;
                Assert.IsTrue(ValidationResultCache.IsObjectValid(gameObject));

                var serializedProbe = new SerializedObject(gameObject.GetComponent<PrefabValidationProbe>());
                serializedProbe.FindProperty("m_Value").stringValue = string.Empty;
                serializedProbe.ApplyModifiedProperties();
                yield return null;

                Assert.IsFalse(ValidationResultCache.IsObjectValid(gameObject));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static GameObject CreateProbeObject(string name, string value)
        {
            var gameObject = new GameObject(name);
            gameObject.AddComponent<PrefabValidationProbe>().SetValue(value);
            return gameObject;
        }

        private static ScriptableObjectValidationProbe CreateScriptableObject(string value)
        {
            var asset = ScriptableObject.CreateInstance<ScriptableObjectValidationProbe>();
            asset.SetValue(value);
            return asset;
        }
    }
}
