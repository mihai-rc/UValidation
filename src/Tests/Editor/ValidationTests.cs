using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UValidation;
using UValidation.Editor;

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
        public void IsSceneValidAtPath_NewSceneAsset_ReturnsTrue()
        {
            const string unsavedScenePath = "Assets/__UValidation_NewScene__.unity";

            Assert.IsTrue(ValidationHelper.IsSceneValidAtPath(unsavedScenePath, false),
                "A scene must be allowed through validation before its asset exists on the first save.");
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
    }
}
