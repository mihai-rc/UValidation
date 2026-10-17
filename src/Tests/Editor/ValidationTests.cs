using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UValidation;
using UValidation.Editor;

namespace Systems.UValidation.Tests
{
    /// <summary>
    /// Serializable test data whose namespace shares the System prefix.
    /// </summary>
    [Serializable]
    internal sealed class SystemsNamespaceData
    {
        [SerializeField, NotEmpty] private string m_Value = string.Empty;
    }
}

namespace UValidation.Tests
{
    public class ValidationTests
    {
        private const string k_RootValidatorExceptionMessage = "Root validator failed intentionally.";
        private const string k_NestedValidatorExceptionMessage = "Nested validator failed intentionally.";

        private sealed class ThrowingValidationHost : MonoBehaviour, IValidatable
        {
            /// <inheritdoc />
            public void Validate(Validation validation)
            {
                throw new InvalidOperationException(k_RootValidatorExceptionMessage);
            }
        }

        [Serializable]
        private sealed class ThrowingNestedData : IValidatable
        {
            /// <inheritdoc />
            public void Validate(Validation validation)
            {
                throw new InvalidOperationException(k_NestedValidatorExceptionMessage);
            }
        }

        private sealed class ThrowingNestedHost : MonoBehaviour
        {
            [SerializeField, IsValid] private ThrowingNestedData m_Data = new();
        }

        private sealed class EmptyValueCollectionHost : MonoBehaviour
        {
            [SerializeField, NotEmpty] private int[] m_Values = Array.Empty<int>();
        }

        private sealed class EmptyUnityBuiltInCollectionsHost : MonoBehaviour
        {
            [SerializeField, NotEmpty] private Vector3[] m_Vectors = Array.Empty<Vector3>();
            [SerializeField, NotEmpty] private AnimationCurve[] m_Curves = Array.Empty<AnimationCurve>();
            [SerializeField, NotEmpty] private SphericalHarmonicsL2[] m_Lighting = Array.Empty<SphericalHarmonicsL2>();
        }

        private sealed class SystemsNamespaceHost : MonoBehaviour
        {
            [SerializeField, IsValid]
            private global::Systems.UValidation.Tests.SystemsNamespaceData m_Data = new();
        }

        private sealed class UnsupportedAttributeTargetsHost : MonoBehaviour
        {
            [SerializeField, NotNull] private string m_NullString;
            [SerializeField, HasNoNulls] private List<string> m_Strings = new() { null };
            [SerializeField, HasNoEmpties] private GameObject[] m_Objects = { null };
            [SerializeField, NotEmpty] private int m_Number;
        }

        private sealed class PrivateNonSerializedAttributeHost : MonoBehaviour
        {
            [NotEmpty] private string m_RuntimeValue = string.Empty;
        }

        private sealed class PublicNonSerializedAttributeHost : MonoBehaviour
        {
            [NonSerialized, NotEmpty] public string RuntimeValue = string.Empty;
        }

        private sealed class PublicSerializedAttributeHost : MonoBehaviour
        {
            [NotEmpty] public string Value = string.Empty;
        }

        private sealed class NonSerializableData
        {
        }

        private sealed class UnsupportedSerializedTypeHost : MonoBehaviour
        {
            [SerializeField, NotEmpty] private List<NonSerializableData> m_Values = new();
        }

        [Serializable]
        private sealed class NonSerializedNestedData
        {
            [NotEmpty] private string m_RuntimeValue = string.Empty;
        }

        private sealed class NonSerializedNestedHost : MonoBehaviour
        {
            [SerializeField, IsValid] private NonSerializedNestedData m_Data = new();
        }

        [Test]
        public void IsTrue_NullPredicate_RecordsFailure()
        {
            using var validation = new Validation();
            validation.IsTrue<object>("x", null, null);
            Assert.IsTrue(validation.Failed, "Null predicate should fail validation.");
        }

        [Test]
        public void IsTrue_PredicateThrows_ExceptionAppearsInReport()
        {
            using var validation = new Validation();
            validation.IsTrue("x", new object(), _ => throw new InvalidOperationException("PREDICATE_BOOM"));
            Assert.IsTrue(validation.Failed);

            LogAssert.Expect(LogType.Error, new Regex("PREDICATE_BOOM"));
            validation.Report();
        }

        [Test]
        public void IsNotNull_DestroyedUnityObjectAsObject_Fails()
        {
            var go = new GameObject("destroyed-target");
            object boxed = go;
            UnityEngine.Object.DestroyImmediate(go);

            using var validation = new Validation();
            validation.IsNotNull("destroyedGO", boxed);
            Assert.IsTrue(validation.Failed,
                "Destroyed Unity object should be detected even when upcast to object.");
        }

        [Test]
        public void HasNoNulls_NullObjectCollection_FailsCleanly()
        {
            using var validation = new Validation();
            IEnumerable<object> nullList = null;
            // If this throws, the test fails — that's the regression we're guarding against.
            validation.HasNoNulls("x", nullList);
            Assert.IsTrue(validation.Failed);
        }

        [Test]
        public void HasNoNulls_UnityEnumerable_DetectsDestroyedElement()
        {
            var alive = new GameObject("alive");
            var dead = new GameObject("dead");
            UnityEngine.Object.DestroyImmediate(dead);

            var list = new UnityEngine.Object[] { alive, dead };
            using var validation = new Validation();
            try
            {
                validation.HasNoNulls("list", (IEnumerable<UnityEngine.Object>)list);
                Assert.IsTrue(validation.Failed,
                    "Destroyed element should be detected via the UnityEngine.Object overload.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(alive);
            }
        }

        [Test]
        public void IsTrue_BooleanCondition_RecordsFailure()
        {
            using var validation = new Validation();
            validation.IsTrue("condition", false);
            Assert.IsTrue(validation.Failed);
        }

        [Test]
        public void IsTrue_BooleanCondition_Passes()
        {
            using var validation = new Validation();
            validation.IsTrue("condition", true);
            Assert.IsTrue(validation.Passed);
        }

        [Test]
        public void IsNotNull_NullableValue_UsesHasValue()
        {
            int? missingValue = null;
            int? existingValue = 42;

            using (var failedValidation = new Validation())
            {
                failedValidation.IsNotNull(nameof(missingValue), missingValue);
                Assert.IsTrue(failedValidation.Failed);
            }

            using (var passedValidation = new Validation())
            {
                passedValidation.IsNotNull(nameof(existingValue), existingValue);
                Assert.IsTrue(passedValidation.Passed);
            }
        }

        [Test]
        public void IsNotEmpty_ValueTypeCollections_AreSupported()
        {
            using (var failedValidation = new Validation())
            {
                failedValidation.IsNotEmpty("values", Array.Empty<int>());
                Assert.IsTrue(failedValidation.Failed);
            }

            using (var passedValidation = new Validation())
            {
                passedValidation.IsNotEmpty("values", new[] { 1 });
                Assert.IsTrue(passedValidation.Passed);
            }
        }

        [Test]
        public void HasNoNulls_ObjectArray_DetectsDestroyedUnityObject()
        {
            var dead = new GameObject("dead");
            object[] values = { dead };
            UnityEngine.Object.DestroyImmediate(dead);

            using var validation = new Validation();
            validation.HasNoNulls(nameof(values), values);
            Assert.IsTrue(validation.Failed,
                "Destroyed Unity objects should be detected through an object collection.");
        }

        [Test]
        public void HasNoNulls_NullableValueCollection_UsesHasValue()
        {
            IEnumerable<int?> values = new int?[] { 1, null };

            using var validation = new Validation();
            validation.HasNoNulls(nameof(values), values);
            Assert.IsTrue(validation.Failed);
        }

        [Test]
        public void AttributeValidation_EmptyValueTypeCollection_Fails()
        {
            var gameObject = new GameObject(nameof(EmptyValueCollectionHost));
            try
            {
                var host = gameObject.AddComponent<EmptyValueCollectionHost>();
                using var validation = new Validation(host);

                SerializedFieldsValidator.ValidateAttributes(host, validation);
                Assert.IsTrue(validation.Failed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AttributeValidation_EmptyUnityBuiltInCollections_FailWithoutSerializationWarnings()
        {
            var gameObject = new GameObject(nameof(EmptyUnityBuiltInCollectionsHost));
            try
            {
                var host = gameObject.AddComponent<EmptyUnityBuiltInCollectionsHost>();
                using var validation = new Validation(host);

                SerializedFieldsValidator.ValidateAttributes(host, validation);

                Assert.IsTrue(validation.Failed);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AttributeValidation_SerializableTypeInSystemsNamespace_IsValidated()
        {
            var gameObject = new GameObject(nameof(SystemsNamespaceHost));
            try
            {
                var host = gameObject.AddComponent<SystemsNamespaceHost>();
                using var validation = new Validation(host);

                SerializedFieldsValidator.ValidateAttributes(host, validation);

                Assert.IsTrue(validation.Failed);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AttributeValidation_UnsupportedTargets_AreIgnored()
        {
            var gameObject = new GameObject(nameof(UnsupportedAttributeTargetsHost));
            try
            {
                var host = gameObject.AddComponent<UnsupportedAttributeTargetsHost>();
                using var validation = new Validation(host);

                SerializedFieldsValidator.ValidateAttributes(host, validation);

                Assert.IsTrue(validation.Passed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AttributeValidation_PrivateNonSerializedField_IsIgnoredAndWarned()
        {
            AssertNonSerializedFieldIsIgnoredAndWarned<PrivateNonSerializedAttributeHost>(
                "m_RuntimeValue");
        }

        [Test]
        public void AttributeValidation_PublicNonSerializedField_IsIgnoredAndWarned()
        {
            AssertNonSerializedFieldIsIgnoredAndWarned<PublicNonSerializedAttributeHost>(
                nameof(PublicNonSerializedAttributeHost.RuntimeValue));
        }

        [Test]
        public void AttributeValidation_PublicSerializedField_IsValidated()
        {
            var gameObject = new GameObject(nameof(PublicSerializedAttributeHost));
            try
            {
                var host = gameObject.AddComponent<PublicSerializedAttributeHost>();
                using var validation = new Validation(host);

                SerializedFieldsValidator.ValidateAttributes(host, validation);

                Assert.IsTrue(validation.Failed);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AttributeValidation_UnsupportedSerializedType_IsIgnoredAndWarned()
        {
            AssertNonSerializedFieldIsIgnoredAndWarned<UnsupportedSerializedTypeHost>(
                "m_Values");
        }

        [Test]
        public void AttributeValidation_NestedNonSerializedField_IsIgnoredAndWarned()
        {
            AssertNonSerializedFieldIsIgnoredAndWarned<NonSerializedNestedHost>(
                "m_RuntimeValue",
                typeof(NonSerializedNestedData));
        }

        [Test]
        public void IsGameObjectValid_ReportErrors_ReportsEveryInvalidComponent()
        {
            var gameObject = new GameObject("MultipleInvalidComponents");
            try
            {
                gameObject.AddComponent<PrefabValidationProbe>().SetValue(string.Empty);
                gameObject.AddComponent<PrefabValidationProbe>().SetValue(string.Empty);

                var expectedError = new Regex(
                    @"(?s)\[Validation\] String is null or empty!.*" +
                    @" - Context: MultipleInvalidComponents\r?\n.*" +
                    @" - Variable: m_Value");
                LogAssert.Expect(LogType.Error, expectedError);
                LogAssert.Expect(LogType.Error, expectedError);

                Assert.IsFalse(ValidationHelper.IsGameObjectValid(gameObject, true));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void HappyPath_NoChecksRecorded_DisposeIsSafe()
        {
            using var validation = new Validation();
            Assert.IsTrue(validation.Passed);

            // Idempotent dispose on a never-failed validation must not throw, even called twice.
            var v2 = new Validation();
            v2.Dispose();
            v2.Dispose();
        }

        [Test]
        public void AliasesShareFailureState()
        {
            using var validation = new Validation();
            var alias = validation;

            alias.IsNotNull("nullValue", (object)null);

            Assert.IsTrue(validation.Failed,
                "Every alias should observe failures recorded through the same validation instance.");
        }

        [Test]
        public void IsSceneValidAtPath_MissingScene_IsInvalidPubliclyAndPendingForSaveEnforcement()
        {
            const string unsavedScenePath = "Assets/__UValidation_NewScene__.unity";

            Assert.IsFalse(ValidationHelper.IsSceneValidAtPath(unsavedScenePath, false));
            Assert.AreEqual(
                AssetValidationState.PendingCreation,
                ValidationHelper.ValidateAssetAtPathForSave(unsavedScenePath, false));
        }

        [Test]
        public void IValidatable_RootException_IsCapturedAsFailure()
        {
            var gameObject = new GameObject(nameof(ThrowingValidationHost));
            try
            {
                var host = gameObject.AddComponent<ThrowingValidationHost>();
                using var validation = new Validation(host);

                Assert.DoesNotThrow(() => SerializedFieldsValidator.ValidateAttributes(host, validation));
                Assert.IsTrue(validation.Failed);
                LogAssert.Expect(
                    LogType.Error,
                    new Regex(
                        @"(?s) - Variable: ThrowingValidationHost\r?\n.*" +
                        Regex.Escape(k_RootValidatorExceptionMessage)));
                validation.Report();
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IValidatable_NestedException_IsCapturedWithObjectPath()
        {
            var gameObject = new GameObject(nameof(ThrowingNestedHost));
            try
            {
                var host = gameObject.AddComponent<ThrowingNestedHost>();
                using var validation = new Validation(host);

                Assert.DoesNotThrow(() => SerializedFieldsValidator.ValidateAttributes(host, validation));
                Assert.IsTrue(validation.Failed);
                LogAssert.Expect(
                    LogType.Error,
                    new Regex(
                        @"(?s) - Variable: m_Data\r?\n.*" +
                        Regex.Escape(k_NestedValidatorExceptionMessage)));
                validation.Report();
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private class CycleNode
        {
            [IsValid] public CycleNode Next;
        }

        private class CycleHost : MonoBehaviour
        {
            [IsValid] public CycleNode Root;
        }

        [Test]
        public void Cyclic_IsValid_Graph_Terminates()
        {
            var a = new CycleNode();
            var b = new CycleNode();
            a.Next = b;
            b.Next = a;

            var go = new GameObject("CycleHost");
            try
            {
                var host = go.AddComponent<CycleHost>();
                host.Root = a;

                using var validation = new Validation(host);
                SerializedFieldsValidator.ValidateAttributes(host, validation);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void AssertNonSerializedFieldIsIgnoredAndWarned<THost>(
            string fieldName,
            Type declaringType = null)
            where THost : MonoBehaviour
        {
            declaringType ??= typeof(THost);
            var qualifiedFieldName = $"{declaringType.FullName}.{fieldName}";
            LogAssert.Expect(
                LogType.Warning,
                new Regex(
                    Regex.Escape(
                        $"[UValidation] Validation attributes on nonserialized field " +
                        $"'{qualifiedFieldName}' are ignored.")));

            var gameObject = new GameObject(typeof(THost).Name);
            try
            {
                var host = gameObject.AddComponent<THost>();
                using var validation = new Validation(host);

                SerializedFieldsValidator.ValidateAttributes(host, validation);

                Assert.IsTrue(validation.Passed);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
