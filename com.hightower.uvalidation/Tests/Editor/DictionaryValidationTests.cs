#if UNITY_6000_6_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using NUnit.Framework;
using UValidation.Editor;
using static UValidation.Tests.ValidationTestUtility;

namespace UValidation.Tests
{
    public class DictionaryValidationTests
    {
        private const string k_ElementErrorIconName = "validation-element-error-icon";

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

        private sealed class NestedCollectionValuesHost : ScriptableObject
        {
            [SerializeField, IsValid] private Dictionary<string, ItemData[]> m_Values = new();

            public Dictionary<string, ItemData[]> Values => m_Values;
        }

        private sealed class ObjectCollectionValuesHost : ScriptableObject
        {
            [SerializeField, IsValid] private Dictionary<string, AnimationClip[]> m_Values = new();

            public Dictionary<string, AnimationClip[]> Values => m_Values;
        }

        [Test]
        public void NotEmpty_EmptyDictionary_FailsAttributeValidation()
        {
            var host = ScriptableObject.CreateInstance<NotEmptyHost>();
            try
            {
                using var validation = ValidateAttributes(host);
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

                using var validation = ValidateAttributes(host);
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

                using var validation = ValidateAttributes(host);
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
            try
            {
                var destroyed = new GameObject("destroyed-dictionary-value");
                host.Values.Add("destroyed", destroyed);
                UnityEngine.Object.DestroyImmediate(destroyed);

                using var validation = ValidateAttributes(host);
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

                using var validation = ValidateAttributes(host);
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

                using var validation = ValidateAttributes(host);
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
            var field = GetNestedValuesField();

            Assert.IsNotNull(field);
            Assert.AreEqual(
                IsValidTargetKind.DictionaryValues,
                ValidationAttributeTargetUtility.GetIsValidTargetKind(field));
        }

        [Test]
        public void IsValid_DictionaryCollectionValue_ReportsIndexedElementPath()
        {
            var host = ScriptableObject.CreateInstance<NestedCollectionValuesHost>();
            try
            {
                host.Values.Add("invalid", new[] { new ItemData(string.Empty) });

                using var validation = ValidateAttributes(host);
                Assert.IsTrue(validation.Failed);
                LogAssert.Expect(
                    LogType.Error,
                    new Regex(@" - Variable: m_Values\[0\]\.Value\[0\]\.m_Name\r?\n"));
                validation.Report();
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void IsValid_DictionaryCollectionTargets_AreClassifiedAsDictionaryValues()
        {
            var nestedField = GetValuesField<NestedCollectionValuesHost>();
            var objectField = GetValuesField<ObjectCollectionValuesHost>();

            Assert.IsNotNull(nestedField);
            Assert.IsNotNull(objectField);
            Assert.AreEqual(
                IsValidTargetKind.DictionaryValues,
                ValidationAttributeTargetUtility.GetIsValidTargetKind(nestedField));
            Assert.AreEqual(
                IsValidTargetKind.DictionaryValues,
                ValidationAttributeTargetUtility.GetIsValidTargetKind(objectField));
        }

        [Test]
        public void IsNestedSerializedPropertyValid_CollapsedDictionaryWithInvalidValue_ReturnsFalse()
        {
            var host = ScriptableObject.CreateInstance<NestedValuesHost>();
            try
            {
                host.Values.Add("invalid", new ItemData(string.Empty));
                using var serialized = new SerializedObject(host);
                var dictionary = serialized.FindProperty("m_Values");
                var field = GetNestedValuesField();
                dictionary.isExpanded = false;

                Assert.IsNotNull(field);
                Assert.IsFalse(SerializedFieldsValidator.IsNestedSerializedPropertyValid(
                    dictionary,
                    ValidationAttributeTargetUtility.GetIsValidTargetKind(field)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void IsNestedSerializedPropertyValid_ValidDictionary_ReturnsTrue()
        {
            var host = ScriptableObject.CreateInstance<NestedValuesHost>();
            try
            {
                host.Values.Add("valid", new ItemData("Configured"));
                using var serialized = new SerializedObject(host);
                var dictionary = serialized.FindProperty("m_Values");
                var field = GetNestedValuesField();

                Assert.IsNotNull(field);
                Assert.IsTrue(SerializedFieldsValidator.IsNestedSerializedPropertyValid(
                    dictionary,
                    ValidationAttributeTargetUtility.GetIsValidTargetKind(field)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void IsNestedSerializedPropertyValid_InvalidDictionaryCollectionValue_ReturnsFalse()
        {
            var host = ScriptableObject.CreateInstance<NestedCollectionValuesHost>();
            try
            {
                host.Values.Add("invalid", new[] { new ItemData(string.Empty) });
                using var serialized = new SerializedObject(host);
                var dictionary = serialized.FindProperty("m_Values");
                var field = GetValuesField<NestedCollectionValuesHost>();

                Assert.IsNotNull(field);
                Assert.IsFalse(SerializedFieldsValidator.IsNestedSerializedPropertyValid(
                    dictionary,
                    ValidationAttributeTargetUtility.GetIsValidTargetKind(field)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void IsNestedSerializedPropertyValid_ObjectReferenceArrayValue_ReturnsTrue()
        {
            var host = ScriptableObject.CreateInstance<ObjectCollectionValuesHost>();
            var clip = new AnimationClip();
            try
            {
                host.Values.Add("clips", new[] { clip });
                using var serialized = new SerializedObject(host);
                var dictionary = serialized.FindProperty("m_Values");
                var field = GetValuesField<ObjectCollectionValuesHost>();

                Assert.IsNotNull(field);
                Assert.IsTrue(SerializedFieldsValidator.IsNestedSerializedPropertyValid(
                    dictionary,
                    ValidationAttributeTargetUtility.GetIsValidTargetKind(field)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DictionaryValuePredicates_ValuesChangeFromValidToInvalid_ReflectCurrentValues()
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
        public void HasEffectiveDictionaryEntries_NullKeyPlaceholderRows_AreNotEffective()
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
        public void AttributeTargetSupport_DictionaryTypes_UseValueType()
        {
            Assert.IsTrue(ValidationAttributeTargetUtility.SupportsNotEmpty(typeof(Dictionary<int, int>)));
            Assert.IsTrue(ValidationAttributeTargetUtility.SupportsHasNoNulls(typeof(Dictionary<int, GameObject>)));
            Assert.IsFalse(ValidationAttributeTargetUtility.SupportsHasNoNulls(typeof(Dictionary<int, string>)));
            Assert.IsTrue(ValidationAttributeTargetUtility.SupportsHasNoEmpties(typeof(Dictionary<int, string>)));
            Assert.IsFalse(ValidationAttributeTargetUtility.SupportsHasNoEmpties(typeof(Dictionary<int, GameObject>)));
        }

        [UnityTest]
        public IEnumerator UiToolkit_HasNoNullsDictionary_AddsIconOnlyBeforeInvalidValue()
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
                var root = CreateRegisteredDrawer(serialized, dictionary);

                window.Configure(root);
                window.position = new Rect(0.0f, 0.0f, 420.0f, 320.0f);
                window.Show();

                PropertyField invalidValueField = null;
                Image errorIcon = null;
                for (var frame = 0; frame < 20 && errorIcon is null; frame++)
                {
                    window.Repaint();
                    yield return null;
                    invalidValueField = FindPropertyField(root, invalidValuePath);
                    errorIcon = invalidValueField?.Q<Image>(k_ElementErrorIconName);
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
        public IEnumerator UiToolkit_HasNoEmptiesDictionary_AddsIconBeforeEmptyValue()
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
                var root = CreateRegisteredDrawer(serialized, dictionary);

                window.Configure(root);
                window.position = new Rect(0.0f, 0.0f, 420.0f, 320.0f);
                window.Show();

                PropertyField invalidValueField = null;
                Image errorIcon = null;
                for (var frame = 0; frame < 20 && errorIcon is null; frame++)
                {
                    window.Repaint();
                    yield return null;
                    invalidValueField = FindPropertyField(root, invalidValuePath);
                    errorIcon = invalidValueField?.Q<Image>(k_ElementErrorIconName);
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
        public IEnumerator UiToolkit_HasNoNullsDictionary_SkipsNullKeyPlaceholderValue()
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
                var root = CreateRegisteredDrawer(serialized, dictionary);

                window.Configure(root);
                window.position = new Rect(0.0f, 0.0f, 420.0f, 280.0f);
                window.Show();

                for (var frame = 0; frame < 10; frame++)
                {
                    window.Repaint();
                    yield return null;
                }

                Assert.IsNull(root.Q<Image>(k_ElementErrorIconName));
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        // Unity's PropertyField resolves the drawer registered for the field's attribute, so the production
        // drawer and its element rule are exercised rather than a predicate supplied by the test.
        private static VisualElement CreateRegisteredDrawer(
            SerializedObject serializedObject,
            SerializedProperty dictionary)
        {
            var field = new PropertyField(dictionary);
            field.Bind(serializedObject);
            return field;
        }

        private static FieldInfo GetNestedValuesField()
        {
            return GetValuesField<NestedValuesHost>();
        }

        private static FieldInfo GetValuesField<THost>()
        {
            return typeof(THost).GetField("m_Values", BindingFlags.Instance | BindingFlags.NonPublic);
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
                                (field.bindingPath == propertyPath || propertyPath.EndsWith(field.bindingPath, StringComparison.Ordinal)))
                .First();
        }

        private static Image GetDirectErrorIcon(PropertyField field)
        {
            var icon = field?.Q<Image>(k_ElementErrorIconName);
            return icon?.parent == field ? icon : null;
        }
    }
}
#endif
