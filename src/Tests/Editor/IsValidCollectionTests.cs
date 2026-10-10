using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UValidation.Editor;

namespace UValidation.Tests
{
    public class IsValidCollectionTests
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

        [Serializable]
        private sealed class RuleData : IValidatable
        {
            [SerializeField] private int m_Value;

            public RuleData(int value)
            {
                m_Value = value;
            }

            /// <inheritdoc />
            public void Validate(Validation validation)
            {
                validation.IsTrue(nameof(m_Value), this, data => data.m_Value > 0);
            }
        }

        [Serializable]
        private sealed class CycleNode
        {
            [IsValid] public List<CycleNode> Children = new();
        }

        private sealed class ListHost : MonoBehaviour
        {
            [SerializeField, IsValid] private List<ItemData> m_Items;

            public void SetItems(List<ItemData> items)
            {
                m_Items = items;
            }
        }

        private sealed class ArrayHost : MonoBehaviour
        {
            [SerializeField, IsValid] private ItemData[] m_Items;

            public void SetItems(ItemData[] items)
            {
                m_Items = items;
            }
        }

        private sealed class RuleListHost : MonoBehaviour
        {
            [SerializeField, IsValid] private List<RuleData> m_Items;

            public void SetItems(List<RuleData> items)
            {
                m_Items = items;
            }
        }

        private sealed class CycleHost : MonoBehaviour
        {
            [SerializeField, IsValid] private CycleNode m_Root;

            public void SetRoot(CycleNode root)
            {
                m_Root = root;
            }
        }

        private sealed class UsageHost : MonoBehaviour
        {
            [SerializeField, IsValid] private ItemData m_Object;
            [SerializeField, IsValid] private ItemData[] m_Array;
            [SerializeField, IsValid] private List<ItemData> m_List;
            [SerializeField, IsValid] private int m_Integer;
            [SerializeField, IsValid] private GameObject m_UnityObject;
            [SerializeField, IsValid] private List<int> m_IntegerList;
        }

        [Test]
        public void IsValid_ListWithValidElements_Passes()
        {
            var gameObject = new GameObject(nameof(ListHost));
            try
            {
                var host = gameObject.AddComponent<ListHost>();
                host.SetItems(new List<ItemData> { new("First"), new("Second") });

                using var validation = Validate(host);
                Assert.IsTrue(validation.Passed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsValid_ListWithInvalidElement_ReportsIndexedFieldPath()
        {
            var gameObject = new GameObject(nameof(ListHost));
            try
            {
                var host = gameObject.AddComponent<ListHost>();
                host.SetItems(new List<ItemData> { new("Valid"), new(string.Empty) });

                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
                ExpectFailure(validation, @"m_Items\[1\]\.m_Name");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsValid_ArrayWithInvalidElement_ReportsIndexedFieldPath()
        {
            var gameObject = new GameObject(nameof(ArrayHost));
            try
            {
                var host = gameObject.AddComponent<ArrayHost>();
                host.SetItems(new[] { new ItemData(string.Empty) });

                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
                ExpectFailure(validation, @"m_Items\[0\]\.m_Name");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsValid_NullAndEmptyCollections_Pass()
        {
            var gameObject = new GameObject(nameof(ListHost));
            try
            {
                var host = gameObject.AddComponent<ListHost>();
                host.SetItems(null);

                using (var nullValidation = Validate(host))
                {
                    Assert.IsTrue(nullValidation.Passed);
                }

                host.SetItems(new List<ItemData>());
                using var emptyValidation = Validate(host);
                Assert.IsTrue(emptyValidation.Passed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsValid_NullCollectionElement_IsIgnored()
        {
            var gameObject = new GameObject(nameof(ListHost));
            try
            {
                var host = gameObject.AddComponent<ListHost>();
                host.SetItems(new List<ItemData> { null, new("Valid") });

                using var validation = Validate(host);
                Assert.IsTrue(validation.Passed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsValid_CustomRuleFailure_IncludesElementPath()
        {
            var gameObject = new GameObject(nameof(RuleListHost));
            try
            {
                var host = gameObject.AddComponent<RuleListHost>();
                host.SetItems(new List<RuleData> { new(1), new(0) });

                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
                ExpectFailure(validation, @"m_Items\[1\]\.m_Value");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsValid_CollectionCycle_Terminates()
        {
            var node = new CycleNode();
            node.Children.Add(node);

            var gameObject = new GameObject(nameof(CycleHost));
            try
            {
                var host = gameObject.AddComponent<CycleHost>();
                host.SetRoot(node);

                using var validation = Validate(host);
                Assert.IsTrue(validation.Passed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsValid_SharedInvalidElement_ReportsEveryCollectionPath()
        {
            var sharedItem = new ItemData(string.Empty);
            var gameObject = new GameObject(nameof(ListHost));
            try
            {
                var host = gameObject.AddComponent<ListHost>();
                host.SetItems(new List<ItemData> { sharedItem, sharedItem });

                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
                LogAssert.Expect(LogType.Error, new Regex(@" - Variable: m_Items\[0\]\.m_Name\r?\n"));
                LogAssert.Expect(LogType.Error, new Regex(@" - Variable: m_Items\[1\]\.m_Name\r?\n"));
                validation.Report();
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [TestCase("m_Object", nameof(IsValidTargetKind.Object))]
        [TestCase("m_Array", nameof(IsValidTargetKind.Collection))]
        [TestCase("m_List", nameof(IsValidTargetKind.Collection))]
        [TestCase("m_Integer", nameof(IsValidTargetKind.Unsupported))]
        [TestCase("m_UnityObject", nameof(IsValidTargetKind.Unsupported))]
        [TestCase("m_IntegerList", nameof(IsValidTargetKind.Unsupported))]
        public void IsValid_TargetClassification_MatchesSupportedContract(
            string fieldName,
            string expected)
        {
            var field = typeof(UsageHost).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(field);
            Assert.AreEqual(expected, IsValidTargetUtility.Classify(field, out _).ToString());
        }

        private static Validation Validate(UnityEngine.Object target)
        {
            var validation = new Validation(target);
            SerializedFieldsValidator.ValidateAttributes(target, validation);
            return validation;
        }

        private static void ExpectFailure(Validation validation, string variablePattern)
        {
            LogAssert.Expect(
                LogType.Error,
                new Regex(@" - Variable: " + variablePattern + @"\r?\n"));
            validation.Report();
            LogAssert.NoUnexpectedReceived();
        }
    }
}
