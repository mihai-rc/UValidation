using System.Collections;

#if UNITY_6000_6_OR_NEWER
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
#endif

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

#if UNITY_6000_6_OR_NEWER
using UnityEngine.UIElements;
#endif

using NUnit.Framework;
using UValidation.Editor;
using static UValidation.Tests.ValidationTestUtility;

namespace UValidation.Tests
{
    public class EditorFeedbackTests
    {
        private const string k_TestFolderName = "__UValidationEditorFeedbackTests";
        private const string k_TestFolderPath = "Assets/" + k_TestFolderName;

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
        public void CountInvalidHeaderTargets_InvalidSceneObject_CountsIt()
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
        public void CountInvalidHeaderTargets_MultiSelection_CountsEveryInvalidTarget()
        {
            var valid = CreateScriptableObject(VALID_VALUE);
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
        public void CountInvalidHeaderTargets_PrefabAssetWithInvalidChild_CountsRootButNotImporter()
        {
            var path = $"{k_TestFolderPath}/InvalidChild.prefab";
            var source = CreateProbeObject("ValidRoot", VALID_VALUE);
            CreateProbeObject("InvalidChild", string.Empty).transform.SetParent(source.transform);
            SaveAsPrefab(source, path);

            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var importer = AssetImporter.GetAtPath(path);

            Assert.AreEqual(1, ValidationFailureEditorHandler.CountInvalidHeaderTargets(new Object[] { prefabRoot }));
            Assert.AreEqual(0, ValidationFailureEditorHandler.CountInvalidHeaderTargets(new Object[] { importer }));
        }

        [Test]
        public void IsSupportedProjectAssetPath_ValidatedAssetTypes_AreSupported()
        {
            Assert.IsTrue(ValidationFailureEditorHandler.IsSupportedProjectAssetPath("Assets/Test.unity"));
            Assert.IsTrue(ValidationFailureEditorHandler.IsSupportedProjectAssetPath("Assets/Test.prefab"));
            Assert.IsTrue(ValidationFailureEditorHandler.IsSupportedProjectAssetPath("Assets/Test.asset"));
            Assert.IsFalse(ValidationFailureEditorHandler.IsSupportedProjectAssetPath("Assets/Test.mat"));
        }

        [Test]
        public void GetHelpBoxHeight_ShortMessage_IsAtLeastMinimumHeight()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("IMGUI style initialization requires a graphics device.");
            }

            var height = ValidationMessageGui.GetHelpBoxHeight(
                320.0f,
                "Validation Failed: This object contains invalid data.");

            Assert.GreaterOrEqual(height, ValidationMessageGui.MINIMUM_HEIGHT);
        }

        [Test]
        public void IsAssetValid_InvalidStoredPrefab_ReturnsFalse()
        {
            var path = $"{k_TestFolderPath}/Invalid.prefab";
            SaveAsPrefab(CreateProbeObject("Invalid", string.Empty), path);

            ValidationResultCache.Clear();

            Assert.IsFalse(ValidationResultCache.IsAssetValid(path));
        }

        [Test]
        public void TryGetCachedAssetResult_UnvalidatedAsset_ReportsNoResult()
        {
            var path = $"{k_TestFolderPath}/Unvalidated.prefab";
            SaveAsPrefab(CreateProbeObject("Unvalidated", string.Empty), path);

            ValidationResultCache.Clear();

            Assert.IsFalse(ValidationResultCache.TryGetCachedAssetResult(path, out var isValid));
            Assert.IsTrue(isValid);
        }

        [Test]
        public void ScheduleAssetValidation_InvalidPrefab_DefersValidationUntilPendingAssetsRun()
        {
            var path = $"{k_TestFolderPath}/DeferredInvalid.prefab";
            SaveAsPrefab(CreateProbeObject("DeferredInvalid", string.Empty), path);
            ValidationResultCache.Clear();

            ValidationResultCache.ScheduleAssetValidation(path);
            Assert.IsFalse(ValidationResultCache.TryGetCachedAssetResult(path, out _));

            ValidationResultCache.ValidatePendingAssets();

            Assert.IsTrue(ValidationResultCache.TryGetCachedAssetResult(path, out var isValid));
            Assert.IsFalse(isValid);
        }

        [Test]
        public void ValidatePendingAssets_ValidPrefab_CachesValidResult()
        {
            var path = $"{k_TestFolderPath}/DeferredValid.prefab";
            SaveAsPrefab(CreateProbeObject("DeferredValid", VALID_VALUE), path);
            ValidationResultCache.Clear();

            ValidationResultCache.ScheduleAssetValidation(path);
            ValidationResultCache.ValidatePendingAssets();

            Assert.IsTrue(ValidationResultCache.TryGetCachedAssetResult(path, out var isValid));
            Assert.IsTrue(isValid);
        }

        [Test]
        public void Clear_AfterDeferredValidation_DiscardsCachedAssetResult()
        {
            var path = $"{k_TestFolderPath}/DeferredCleared.prefab";
            SaveAsPrefab(CreateProbeObject("DeferredCleared", string.Empty), path);
            ValidationResultCache.Clear();

            ValidationResultCache.ScheduleAssetValidation(path);
            ValidationResultCache.ValidatePendingAssets();
            ValidationResultCache.Clear();

            Assert.IsFalse(ValidationResultCache.TryGetCachedAssetResult(path, out _));
        }

        [Test]
        public void EveryTargetPasses_MultiSelectionWithInvalidTarget_ReturnsFalse()
        {
            var valid = CreateScriptableObject(VALID_VALUE);
            var invalid = CreateScriptableObject(string.Empty);
            try
            {
                using var serializedTargets = new SerializedObject(new Object[] { valid, invalid });
                var property = serializedTargets.FindProperty("m_Value");

                Assert.IsNotNull(property);
                Assert.IsFalse(FieldDrawerHelper.EveryTargetPasses(
                    property,
                    targetProperty => !string.IsNullOrEmpty(targetProperty.stringValue)));
            }
            finally
            {
                Object.DestroyImmediate(valid);
                Object.DestroyImmediate(invalid);
            }
        }

        [Test]
        public void IsObjectValid_UnrecordedChange_KeepsCachedResultUntilCleared()
        {
            var gameObject = CreateProbeObject("Cached", VALID_VALUE);
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

        [Test]
        public void Clear_Called_RaisesResultsInvalidated()
        {
            var invalidationCount = 0;
            ValidationResultCache.ResultsInvalidated += HandleResultsInvalidated;
            try
            {
                ValidationResultCache.Clear();

                Assert.AreEqual(1, invalidationCount);
            }
            finally
            {
                ValidationResultCache.ResultsInvalidated -= HandleResultsInvalidated;
            }

            void HandleResultsInvalidated()
            {
                invalidationCount++;
            }
        }

#if UNITY_6000_6_OR_NEWER
        [UnityTest]
        public IEnumerator HierarchyV2_InvalidGameObject_UsesRedNameLabel()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Hierarchy window rendering requires a graphics device.");
            }

            var gameObject = CreateProbeObject("InvalidHierarchyItem", string.Empty);
            var window = EditorWindow.CreateWindow<HierarchyWindow>();
            try
            {
                window.position = new Rect(0.0f, 0.0f, 500.0f, 500.0f);
                window.Show();

                HierarchyViewItem invalidItem = null;
                for (var frame = 0; frame < 30 && invalidItem is null; frame++)
                {
                    window.Repaint();
                    yield return null;

                    window.View.Query<HierarchyViewItem>().ForEach(item =>
                    {
                        if (item.Handler is HierarchyGameObjectHandler handler &&
                            handler.GetGameObject(item.Node) == gameObject)
                        {
                            invalidItem = item;
                        }
                    });
                }

                Assert.IsNotNull(invalidItem, "The invalid GameObject did not appear in the Hierarchy window.");
                yield return null;
                Assert.AreEqual(Color.red, invalidItem.Name.resolvedStyle.color);
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(gameObject);
            }
        }
#endif

        [UnityTest]
        public IEnumerator IsObjectValid_RecordedPropertyChange_InvalidatesResult()
        {
            var gameObject = CreateProbeObject("Edited", VALID_VALUE);
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

        [UnityTest]
        public IEnumerator IsObjectValid_RecordedPropertyChange_PreservesUnchangedObjectResults()
        {
            var edited = CreateProbeObject("Edited", VALID_VALUE);
            var unchanged = CreateProbeObject("Unchanged", VALID_VALUE);
            try
            {
                yield return null;
                Assert.IsTrue(ValidationResultCache.IsObjectValid(edited));
                Assert.IsTrue(ValidationResultCache.IsObjectValid(unchanged));

                // Change this object without publishing an editor event so its cached result remains observable.
                unchanged.GetComponent<PrefabValidationProbe>().SetValue(string.Empty);

                var serializedProbe = new SerializedObject(edited.GetComponent<PrefabValidationProbe>());
                serializedProbe.FindProperty("m_Value").stringValue = "Edited value";
                serializedProbe.ApplyModifiedProperties();
                yield return null;

                Assert.IsTrue(ValidationResultCache.IsObjectValid(edited));
                Assert.IsTrue(ValidationResultCache.IsObjectValid(unchanged));

                ValidationResultCache.Clear();
                Assert.IsFalse(ValidationResultCache.IsObjectValid(unchanged));
            }
            finally
            {
                Object.DestroyImmediate(edited);
                Object.DestroyImmediate(unchanged);
            }
        }

        private static ScriptableObjectValidationProbe CreateScriptableObject(string value)
        {
            var asset = ScriptableObject.CreateInstance<ScriptableObjectValidationProbe>();
            asset.SetValue(value);
            return asset;
        }
    }
}
