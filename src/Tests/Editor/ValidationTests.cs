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
        [Test]
        public void IsTrue_NullPredicate_RecordsFailure()
        {
            var validation = new Validation();
            try
            {
                validation.IsTrue<object>("x", null, null);
                Assert.IsTrue(validation.Failed, "Null predicate should fail validation.");
            }
            finally
            {
                validation.Dispose();
            }
        }

        [Test]
        public void IsTrue_PredicateThrows_ExceptionAppearsInReport()
        {
            var validation = new Validation();
            try
            {
                validation.IsTrue("x", new object(), _ => throw new InvalidOperationException("PREDICATE_BOOM"));
                Assert.IsTrue(validation.Failed);

                LogAssert.Expect(LogType.Error, new Regex("PREDICATE_BOOM"));
                validation.Report();
            }
            finally
            {
                validation.Dispose();
            }
        }

        [Test]
        public void IsNotNull_DestroyedUnityObjectAsObject_Fails()
        {
            var go = new GameObject("destroyed-target");
            object boxed = go;
            UnityEngine.Object.DestroyImmediate(go);

            var validation = new Validation();
            try
            {
                validation.IsNotNull("destroyedGO", boxed);
                Assert.IsTrue(validation.Failed,
                    "Destroyed Unity object should be detected even when upcast to object.");
            }
            finally
            {
                validation.Dispose();
            }
        }

        [Test]
        public void HasNoNulls_NullObjectCollection_FailsCleanly()
        {
            var validation = new Validation();
            try
            {
                IEnumerable<object> nullList = null;
                // If this throws, the test fails — that's the regression we're guarding against.
                validation.HasNoNulls("x", nullList);
                Assert.IsTrue(validation.Failed);
            }
            finally
            {
                validation.Dispose();
            }
        }

        [Test]
        public void HasNoNulls_UnityEnumerable_DetectsDestroyedElement()
        {
            var alive = new GameObject("alive");
            var dead = new GameObject("dead");
            UnityEngine.Object.DestroyImmediate(dead);

            var list = new UnityEngine.Object[] { alive, dead };
            var validation = new Validation();
            try
            {
                validation.HasNoNulls("list", (IEnumerable<UnityEngine.Object>)list);
                Assert.IsTrue(validation.Failed,
                    "Destroyed element should be detected via the UnityEngine.Object overload.");
            }
            finally
            {
                validation.Dispose();
                UnityEngine.Object.DestroyImmediate(alive);
            }
        }

        [Test]
        public void HappyPath_NoChecksRecorded_DisposeIsSafe()
        {
            var validation = new Validation();
            try
            {
                Assert.IsTrue(validation.Passed);
            }
            finally
            {
                validation.Dispose();
            }

            // Idempotent dispose on a never-failed validation must not throw, even called twice.
            var v2 = new Validation();
            v2.Dispose();
            v2.Dispose();
        }

        [Test]
        public void DefaultConstruction_IsOperationallySafe()
        {
            Validation v = default;
            Assert.IsTrue(v.Passed);
            v.Dispose();

            Validation v2 = default;
            try
            {
                v2.IsNotNull("nonNullValue", new object());
                Assert.IsTrue(v2.Passed, "Non-null check should not record failure on default(Validation).");

                v2.IsNotNull("nullValue", (object)null);
                Assert.IsTrue(v2.Failed,
                    "Null check on default(Validation) should lazy-rent and record failure cleanly.");
            }
            finally
            {
                v2.Dispose();
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

                var validation = new Validation(host);
                try
                {
                    SerializedFieldsValidator.ValidateAttributes(host, ref validation);
                }
                finally
                {
                    validation.Dispose();
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
