#if UNITY_6000_6_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UValidation.Editor.Tests
{
    public class DictionaryValidationTests
    {
        [Serializable]
        private sealed class ItemData
        {
            [SerializeField, NotEmpty] private string m_Name;

            public ItemData(string name)
            {
                m_Name = name;
            }
        }

        private sealed class NotEmptyHost : ScriptableObject
        {
            [SerializeField, NotEmpty] private Dictionary<string, int> m_Values = new();

            public Dictionary<string, int> Values => m_Values;
        }

        private sealed class ObjectValuesHost : ScriptableObject
        {
            [SerializeField, HasNoNulls] private Dictionary<string, GameObject> m_Values = new();

            public Dictionary<string, GameObject> Values => m_Values;
        }

        private sealed class ObjectKeyHost : ScriptableObject
        {
            [SerializeField, NotEmpty] private Dictionary<GameObject, string> m_Values = new();

            public Dictionary<GameObject, string> Values => m_Values;
        }

        private sealed class ObjectKeysAndValuesHost : ScriptableObject
        {
            [SerializeField, HasNoNulls] private Dictionary<GameObject, GameObject> m_Values = new();
        }

        private sealed class StringValuesHost : ScriptableObject
        {
            [SerializeField, HasNoEmpties] private Dictionary<int, string> m_Values = new();

            public Dictionary<int, string> Values => m_Values;
        }

        private sealed class NestedValuesHost : ScriptableObject
        {
            [SerializeField, IsValid] private Dictionary<string, ItemData> m_Values = new();

            public Dictionary<string, ItemData> Values => m_Values;
        }

        [Test]
        public void NotEmpty_EmptyDictionary_FailsAttributeValidation()
        {
            var host = ScriptableObject.CreateInstance<NotEmptyHost>();
            try
            {
                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void NotEmpty_PopulatedDictionary_PassesAttributeValidation()
        {
            var host = ScriptableObject.CreateInstance<NotEmptyHost>();
            try
            {
                host.Values.Add("one", 1);

                using var validation = Validate(host);
                Assert.IsTrue(validation.Passed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void HasNoNulls_NullDictionaryValue_FailsAttributeValidation()
        {
            var host = ScriptableObject.CreateInstance<ObjectValuesHost>();
            try
            {
                host.Values.Add("missing", null);

                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void HasNoNulls_DestroyedDictionaryValue_FailsAttributeValidation()
        {
            var host = ScriptableObject.CreateInstance<ObjectValuesHost>();
            var destroyed = new GameObject("destroyed-dictionary-value");
            host.Values.Add("destroyed", destroyed);
            UnityEngine.Object.DestroyImmediate(destroyed);

            try
            {
                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void HasNoEmpties_EmptyDictionaryValue_FailsAttributeValidation()
        {
            var host = ScriptableObject.CreateInstance<StringValuesHost>();
            try
            {
                host.Values.Add(1, string.Empty);

                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void IsValid_DictionaryValue_ReportsIndexedValuePath()
        {
            var host = ScriptableObject.CreateInstance<NestedValuesHost>();
            try
            {
                host.Values.Add("invalid", new ItemData(string.Empty));

                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
                LogAssert.Expect(LogType.Error, new Regex(@" - Variable: m_Values\[0\]\.Value\.m_Name\r?\n"));
                validation.Report();
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void IsValid_DictionaryTarget_IsClassifiedAsDictionaryValues()
        {
            var field = typeof(NestedValuesHost).GetField(
                "m_Values",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.IsNotNull(field);
            Assert.AreEqual(IsValidTargetKind.DictionaryValues, IsValidTargetUtility.Classify(field));
        }

        [Test]
        public void IsValidDrawerSummary_CollapsedDictionaryStillReportsInvalidNestedValue()
        {
            var host = ScriptableObject.CreateInstance<NestedValuesHost>();
            try
            {
                host.Values.Add("invalid", new ItemData(string.Empty));
                using var serialized = new SerializedObject(host);
                var dictionary = serialized.FindProperty("m_Values");
                var field = typeof(NestedValuesHost).GetField(
                    "m_Values",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                dictionary.isExpanded = false;

                Assert.IsNotNull(field);
                Assert.IsFalse(SerializedFieldsValidator.IsNestedSerializedPropertyValid(
                    dictionary,
                    IsValidTargetUtility.Classify(field)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void IsValidDrawerSummary_ValidDictionaryDoesNotReportHeadline()
        {
            var host = ScriptableObject.CreateInstance<NestedValuesHost>();
            try
            {
                host.Values.Add("valid", new ItemData("Configured"));
                using var serialized = new SerializedObject(host);
                var dictionary = serialized.FindProperty("m_Values");
                var field = typeof(NestedValuesHost).GetField(
                    "m_Values",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                Assert.IsNotNull(field);
                Assert.IsTrue(SerializedFieldsValidator.IsNestedSerializedPropertyValid(
                    dictionary,
                    IsValidTargetUtility.Classify(field)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DictionaryDrawerPredicates_DistinguishValidAndInvalidValues()
        {
            var objectHost = ScriptableObject.CreateInstance<ObjectValuesHost>();
            var stringHost = ScriptableObject.CreateInstance<StringValuesHost>();
            var assigned = new GameObject("assigned-dictionary-value");
            try
            {
                objectHost.Values.Add("assigned", assigned);
                stringHost.Values.Add(1, "Configured");

                using var objectSerialized = new SerializedObject(objectHost);
                using var stringSerialized = new SerializedObject(stringHost);
                var objectValues = objectSerialized.FindProperty("m_Values");
                var stringValues = stringSerialized.FindProperty("m_Values");

                Assert.IsTrue(FieldDrawerHelper.HasNoNullDictionaryValues(objectValues));
                Assert.IsTrue(FieldDrawerHelper.HasNoEmptyDictionaryValues(stringValues));

                objectHost.Values.Add("missing", null);
                stringHost.Values.Add(2, string.Empty);
                objectSerialized.Update();
                stringSerialized.Update();

                Assert.IsFalse(FieldDrawerHelper.HasNoNullDictionaryValues(objectValues));
                Assert.IsFalse(FieldDrawerHelper.HasNoEmptyDictionaryValues(stringValues));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(assigned);
                UnityEngine.Object.DestroyImmediate(objectHost);
                UnityEngine.Object.DestroyImmediate(stringHost);
            }
        }

        [Test]
        public void NotEmptyDrawer_DistinguishesEffectiveEntriesFromNullKeyPlaceholderRows()
        {
            var placeholderHost = ScriptableObject.CreateInstance<ObjectKeyHost>();
            var populatedHost = ScriptableObject.CreateInstance<ObjectKeyHost>();
            var assignedKey = new GameObject("assigned-dictionary-key");
            try
            {
                using var placeholderSerialized = new SerializedObject(placeholderHost);
                var placeholderDictionary = placeholderSerialized.FindProperty("m_Values");
                placeholderDictionary.arraySize = 2;
                placeholderSerialized.ApplyModifiedPropertiesWithoutUndo();
                placeholderSerialized.Update();

                placeholderDictionary = placeholderSerialized.FindProperty("m_Values");
                Assert.AreEqual(2, placeholderDictionary.arraySize);
                Assert.IsFalse(FieldDrawerHelper.HasEffectiveDictionaryEntries(placeholderDictionary));

                populatedHost.Values.Add(assignedKey, "Configured");
                using var populatedSerialized = new SerializedObject(populatedHost);
                var populatedDictionary = populatedSerialized.FindProperty("m_Values");
                Assert.IsTrue(FieldDrawerHelper.HasEffectiveDictionaryEntries(populatedDictionary));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(assignedKey);
                UnityEngine.Object.DestroyImmediate(placeholderHost);
                UnityEngine.Object.DestroyImmediate(populatedHost);
            }
        }

        [Test]
        public void DictionaryTargetUtilities_UseValueTypes()
        {
            Assert.IsTrue(ValidationAttributeTargetUtility.SupportsNotEmpty(typeof(Dictionary<int, int>)));
            Assert.IsTrue(ValidationAttributeTargetUtility.SupportsHasNoNulls(typeof(Dictionary<int, GameObject>)));
            Assert.IsFalse(ValidationAttributeTargetUtility.SupportsHasNoNulls(typeof(Dictionary<int, string>)));
            Assert.IsTrue(ValidationAttributeTargetUtility.SupportsHasNoEmpties(typeof(Dictionary<int, string>)));
            Assert.IsFalse(ValidationAttributeTargetUtility.SupportsHasNoEmpties(typeof(Dictionary<int, GameObject>)));
        }

        [UnityTest]
        public System.Collections.IEnumerator UiToolkit_HasNoNullsDictionary_AddsIconOnlyBeforeInvalidValue()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("UI Toolkit EditorWindow rendering requires a graphics device.");
            }

            var host = ScriptableObject.CreateInstance<ObjectValuesHost>();
            var assigned = new GameObject("assigned-dictionary-value");
            var window = ScriptableObject.CreateInstance<UiToolkitTestWindow>();
            try
            {
                host.Values.Add("valid", assigned);
                host.Values.Add("invalid", null);

                using var serialized = new SerializedObject(host);
                var dictionary = serialized.FindProperty("m_Values");
                dictionary.isExpanded = true;
                var invalidValuePath = GetDictionaryValuePath(dictionary, "invalid");
                var validValuePath = GetDictionaryValuePath(dictionary, "valid");
                var root = CreateDictionaryValueDrawer(
                    dictionary,
                    "The collection must not contain null object references.",
                    value => value.propertyType == SerializedPropertyType.ObjectReference &&
                             value.objectReferenceValue != null);
                root.Bind(serialized);

                window.Configure(root);
                window.position = new Rect(0.0f, 0.0f, 420.0f, 320.0f);
                window.Show();

                PropertyField invalidValueField = null;
                Image errorIcon = null;
                for (var frame = 0; frame < 20 && errorIcon == null; frame++)
                {
                    window.Repaint();
                    yield return null;
                    invalidValueField = FindPropertyField(root, invalidValuePath);
                    errorIcon = invalidValueField?.Q<Image>("validation-element-error-icon");
                }

                var validValueField = FindPropertyField(root, validValuePath);
                Assert.IsNotNull(invalidValueField);
                Assert.IsNotNull(validValueField);
                Assert.IsNotNull(errorIcon);
                Assert.AreSame(invalidValueField, errorIcon.parent);
                Assert.AreEqual(0, invalidValueField.IndexOf(errorIcon));
                Assert.IsNull(GetDirectErrorIcon(validValueField));

                var objectField = invalidValueField.Q<ObjectField>();
                Assert.IsNotNull(objectField);
                Assert.Less(errorIcon.worldBound.xMin, objectField.worldBound.xMin);
                Assert.LessOrEqual(objectField.worldBound.xMax, root.worldBound.xMax + 0.5f);
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(assigned);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [UnityTest]
        public System.Collections.IEnumerator UiToolkit_HasNoEmptiesDictionary_AddsIconBeforeEmptyValue()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("UI Toolkit EditorWindow rendering requires a graphics device.");
            }

            var host = ScriptableObject.CreateInstance<StringValuesHost>();
            var window = ScriptableObject.CreateInstance<UiToolkitTestWindow>();
            try
            {
                host.Values.Add(1, string.Empty);
                host.Values.Add(2, "Configured");

                using var serialized = new SerializedObject(host);
                var dictionary = serialized.FindProperty("m_Values");
                dictionary.isExpanded = true;
                var invalidValuePath = GetDictionaryValuePath(dictionary, 1);
                var validValuePath = GetDictionaryValuePath(dictionary, 2);
                var root = CreateDictionaryValueDrawer(
                    dictionary,
                    "The collection must not contain null or empty strings.",
                    value => value.propertyType == SerializedPropertyType.String &&
                             !string.IsNullOrEmpty(value.stringValue));
                root.Bind(serialized);

                window.Configure(root);
                window.position = new Rect(0.0f, 0.0f, 420.0f, 320.0f);
                window.Show();

                PropertyField invalidValueField = null;
                Image errorIcon = null;
                for (var frame = 0; frame < 20 && errorIcon == null; frame++)
                {
                    window.Repaint();
                    yield return null;
                    invalidValueField = FindPropertyField(root, invalidValuePath);
                    errorIcon = invalidValueField?.Q<Image>("validation-element-error-icon");
                }

                var validValueField = FindPropertyField(root, validValuePath);
                Assert.IsNotNull(invalidValueField);
                Assert.IsNotNull(validValueField);
                Assert.IsNotNull(errorIcon);
                Assert.AreSame(invalidValueField, errorIcon.parent);
                Assert.IsNull(GetDirectErrorIcon(validValueField));
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [UnityTest]
        public System.Collections.IEnumerator UiToolkit_HasNoNullsDictionary_SkipsNullKeyPlaceholderValue()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("UI Toolkit EditorWindow rendering requires a graphics device.");
            }

            var host = ScriptableObject.CreateInstance<ObjectKeysAndValuesHost>();
            var window = ScriptableObject.CreateInstance<UiToolkitTestWindow>();
            try
            {
                using var serialized = new SerializedObject(host);
                var dictionary = serialized.FindProperty("m_Values");
                dictionary.arraySize = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                serialized.Update();
                dictionary = serialized.FindProperty("m_Values");
                dictionary.isExpanded = true;

                var root = CreateDictionaryValueDrawer(
                    dictionary,
                    "The collection must not contain null object references.",
                    value => value.propertyType == SerializedPropertyType.ObjectReference &&
                             value.objectReferenceValue != null);
                root.Bind(serialized);

                window.Configure(root);
                window.position = new Rect(0.0f, 0.0f, 420.0f, 280.0f);
                window.Show();

                for (var frame = 0; frame < 10; frame++)
                {
                    window.Repaint();
                    yield return null;
                }

                Assert.IsNull(root.Q<Image>("validation-element-error-icon"));
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static VisualElement CreateDictionaryValueDrawer(
            SerializedProperty dictionary,
            string violationMessage,
            Func<SerializedProperty, bool> valueValidation)
        {
            return FieldDrawerHelper.CreatePropertyGUI(
                dictionary,
                nameof(HasNoNullsAttribute),
                "dictionary values",
                violationMessage,
                _ => true,
                property => SerializedDictionaryAdapter.EveryEffectiveValuePasses(property, valueValidation),
                valueValidation,
                validateDictionaryValues: true);
        }

        private static string GetDictionaryValuePath(SerializedProperty dictionary, string key)
        {
            for (var i = 0; i < dictionary.arraySize; i++)
            {
                var entry = dictionary.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("key").stringValue == key)
                {
                    return entry.FindPropertyRelative("value").propertyPath;
                }
            }

            Assert.Fail($"Dictionary key '{key}' was not serialized.");
            return null;
        }

        private static string GetDictionaryValuePath(SerializedProperty dictionary, int key)
        {
            for (var i = 0; i < dictionary.arraySize; i++)
            {
                var entry = dictionary.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("key").intValue == key)
                {
                    return entry.FindPropertyRelative("value").propertyPath;
                }
            }

            Assert.Fail($"Dictionary key '{key}' was not serialized.");
            return null;
        }

        private static PropertyField FindPropertyField(VisualElement root, string propertyPath)
        {
            return root
                .Query<PropertyField>()
                .Where(field => !string.IsNullOrEmpty(field.bindingPath) &&
                                (field.bindingPath == propertyPath ||
                                 propertyPath.EndsWith(field.bindingPath, StringComparison.Ordinal)))
                .First();
        }

        private static Image GetDirectErrorIcon(PropertyField field)
        {
            var icon = field?.Q<Image>("validation-element-error-icon");
            return icon?.parent == field ? icon : null;
        }

        private static Validation Validate(UnityEngine.Object target)
        {
            var validation = new Validation(target);
            SerializedFieldsValidator.ValidateAttributes(target, validation);
            return validation;
        }

        private sealed class UiToolkitTestWindow : EditorWindow
        {
            internal void Configure(VisualElement content)
            {
                rootVisualElement.Add(content);
            }
        }
    }
}
#endif
