using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using UValidation.Editor;

namespace UValidation.Tests
{
    /// <summary>
    /// Verifies field validation across base classes and nested objects.
    /// </summary>
    public class InheritedFieldsTests
    {
        private abstract class AssetBase : ScriptableObject
        {
            [SerializeField, NotEmpty] private string m_BaseLabel = string.Empty;
        }

        private abstract class AssetMiddle : AssetBase
        {
        }

        private sealed class AssetLeaf : AssetMiddle
        {
        }

        private abstract class NestedHostBase : ScriptableObject
        {
            [SerializeField, IsValid] private NestedLeaf m_Data = new();
        }

        private sealed class NestedHost : NestedHostBase
        {
        }

        [Serializable]
        private abstract class NestedBase
        {
            [SerializeField, NotEmpty] private string m_BaseLabel = string.Empty;
        }

        [Serializable]
        private sealed class NestedLeaf : NestedBase
        {
            [SerializeField, NotEmpty] private string m_DerivedLabel = string.Empty;
        }

        private abstract class VisibleFieldsBase : ScriptableObject
        {
            /// <summary>
            /// Provides an invalid public field inherited by the test asset.
            /// </summary>
            [NotEmpty] public string PublicLabel = string.Empty;

            /// <summary>
            /// Provides an invalid protected field inherited by the test asset.
            /// </summary>
            [SerializeField, NotEmpty] protected string m_ProtectedLabel = string.Empty;
        }

        private abstract class VisibleFieldsMiddle : VisibleFieldsBase
        {
        }

        private sealed class VisibleFieldsLeaf : VisibleFieldsMiddle
        {
        }

        /// <summary>
        /// Checks inherited private fields and reuses cached metadata with updated values.
        /// </summary>
        [Test]
        public void ScriptableObject_PrivateBaseField_UsesCurrentValue()
        {
            var target = ScriptableObject.CreateInstance<AssetLeaf>();
            try
            {
                Assert.That(ValidationHelper.IsScriptValid(target, false), Is.False);

                using (var serializedObject = new SerializedObject(target))
                {
                    var property = serializedObject.FindProperty("m_BaseLabel");
                    Assert.That(property, Is.Not.Null);
                    property.stringValue = "Configured";
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }

                Assert.That(ValidationHelper.IsScriptValid(target, false), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        /// <summary>
        /// Checks inherited IsValid fields and private fields in nested base classes.
        /// </summary>
        [Test]
        public void IsValid_PrivateBaseField_ReportsNestedInheritedFailures()
        {
            var target = ScriptableObject.CreateInstance<NestedHost>();
            try
            {
                AssertFailuresReportedOnce(target, "m_Data.m_DerivedLabel", "m_Data.m_BaseLabel");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        /// <summary>
        /// Ensures walking the hierarchy does not report public or protected fields repeatedly.
        /// </summary>
        [Test]
        public void PublicAndProtectedBaseFields_ReportEachFailureOnce()
        {
            var target = ScriptableObject.CreateInstance<VisibleFieldsLeaf>();
            try
            {
                AssertFailuresReportedOnce(target, "PublicLabel", "m_ProtectedLabel");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void AssertFailuresReportedOnce(UnityEngine.Object target, params string[] fieldNames)
        {
            var validation = new Validation(target);
            try
            {
                SerializedFieldsValidator.ValidateAttributes(target, ref validation);
                Assert.That(validation.Failed, Is.True);

                foreach (var fieldName in fieldNames)
                {
                    LogAssert.Expect(LogType.Error, new Regex(@" - Variable: " + Regex.Escape(fieldName) + @"\r?\n"));
                }

                validation.Report();
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                validation.Dispose();
            }
        }
    }
}
