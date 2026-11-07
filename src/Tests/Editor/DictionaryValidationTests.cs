#if UNITY_6000_6_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

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
        public void DictionaryDrawerPredicates_ValidateValuesInsteadOfEntries()
        {
            var objectHost = ScriptableObject.CreateInstance<ObjectValuesHost>();
            var stringHost = ScriptableObject.CreateInstance<StringValuesHost>();
            try
            {
                objectHost.Values.Add("missing", null);
                stringHost.Values.Add(1, string.Empty);

                using var objectSerialized = new SerializedObject(objectHost);
                using var stringSerialized = new SerializedObject(stringHost);
                var objectValues = objectSerialized.FindProperty("m_Values");
                var stringValues = stringSerialized.FindProperty("m_Values");

                Assert.IsFalse(FieldDrawerHelper.HasNoNullDictionaryValues(objectValues));
                Assert.IsFalse(FieldDrawerHelper.HasNoEmptyDictionaryValues(stringValues));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objectHost);
                UnityEngine.Object.DestroyImmediate(stringHost);
            }
        }

        [Test]
        public void NotEmptyDrawer_IgnoresNullKeyPlaceholderRows()
        {
            var host = ScriptableObject.CreateInstance<ObjectKeyHost>();
            try
            {
                using var serialized = new SerializedObject(host);
                var dictionary = serialized.FindProperty("m_Values");
                dictionary.arraySize = 2;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                serialized.Update();

                dictionary = serialized.FindProperty("m_Values");
                Assert.AreEqual(2, dictionary.arraySize);
                Assert.IsFalse(FieldDrawerHelper.HasEffectiveDictionaryEntries(dictionary));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
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

        private static Validation Validate(UnityEngine.Object target)
        {
            var validation = new Validation(target);
            SerializedFieldsValidator.ValidateAttributes(target, validation);
            return validation;
        }
    }
}
#endif
