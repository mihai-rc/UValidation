using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UValidation.Editor;

namespace UValidation.Tests
{
    public class IsValidCollectionTests
    {
        private const string k_CollectionValidatorExceptionMessage = "Collection element validator failed intentionally.";

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
        private struct StructData
        {
            [SerializeField, NotEmpty] private string m_Name;

            public StructData(string name)
            {
                m_Name = name;
            }
        }

        [Serializable]
        private struct StructRuleData : IValidatable
        {
            [SerializeField] private int m_Value;

            public StructRuleData(int value)
            {
                m_Value = value;
            }

            /// <inheritdoc />
            public void Validate(Validation validation)
            {
                validation.IsTrue(nameof(m_Value), m_Value > 0);
            }
        }

        [Serializable]
        private sealed class ThrowingItemData : IValidatable
        {
            [SerializeField, NotEmpty] private string m_Name;
            [SerializeField] private bool m_Throws;

            public ThrowingItemData(string name, bool throws)
            {
                m_Name = name;
                m_Throws = throws;
            }

            /// <inheritdoc />
            public void Validate(Validation validation)
            {
                if (m_Throws)
                {
                    throw new InvalidOperationException(k_CollectionValidatorExceptionMessage);
                }
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

        private sealed class StructHost : MonoBehaviour
        {
            [SerializeField, IsValid] private StructData m_Data = new("Valid");
            [SerializeField, IsValid] private List<StructData> m_Items = new() { new("Valid") };
            [SerializeField, IsValid] private StructRuleData m_Rule = new(1);

            public void SetData(StructData data)
            {
                m_Data = data;
            }

            public void SetItems(List<StructData> items)
            {
                m_Items = items;
            }

            public void SetRule(StructRuleData rule)
            {
                m_Rule = rule;
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

        private sealed class ThrowingItemListHost : MonoBehaviour
        {
            [SerializeField, IsValid] private List<ThrowingItemData> m_Items;

            public void SetItems(List<ThrowingItemData> items)
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
            [SerializeField, IsValid] private StructData m_Struct;
            [SerializeField, IsValid] private List<StructData> m_StructList;
            [SerializeField, IsValid] private Vector3 m_UnityStruct;
            [SerializeField, IsValid] private AnimationCurve m_UnityClass;
            [SerializeField, IsValid] private int m_Integer;
            [SerializeField, IsValid] private GameObject m_UnityObject;
            [SerializeField, IsValid] private List<int> m_IntegerList;
            [SerializeField, IsValid] private IRule m_PlainInterface;
            [SerializeReference, IsValid] private IRule m_ManagedInterface;
            [SerializeReference, IsValid] private List<IRule> m_ManagedInterfaceList;
            [SerializeReference, IsValid] private RuleBase m_ManagedAbstract;
            [SerializeReference, IsValid] private List<RuleBase> m_ManagedAbstractList;
            [SerializeReference, IsValid] private List<SerializableRuleBase> m_ManagedSerializableAbstractList;
            [SerializeReference, IsValid] private object m_ManagedObject;
        }

        private interface IRule
        {
        }

        // Deliberately not [Serializable]: Unity accepts it as a single [SerializeReference] field type.
        private abstract class RuleBase
        {
        }

        [Serializable]
        private abstract class SerializableRuleBase
        {
        }

        [Serializable]
        private sealed class NamedRule : RuleBase, IRule
        {
            [SerializeField, NotEmpty] private string m_Name;

            public NamedRule(string name)
            {
                m_Name = name;
            }
        }

        [Serializable]
        private sealed class SerializableNamedRule : SerializableRuleBase
        {
            [SerializeField, NotEmpty] private string m_Name;

            public SerializableNamedRule(string name)
            {
                m_Name = name;
            }
        }

        private sealed class ManagedReferenceHost : MonoBehaviour
        {
            [SerializeReference, IsValid] private IRule m_Interface;
            [SerializeReference, IsValid] private List<IRule> m_InterfaceList;
            [SerializeReference, IsValid] private RuleBase m_Abstract;
            [SerializeReference, IsValid] private List<SerializableRuleBase> m_AbstractList;
            [SerializeReference, IsValid] private object m_Object;

            public void SetAll(string name)
            {
                m_Interface = new NamedRule(name);
                m_InterfaceList = new List<IRule> { new NamedRule(name) };
                m_Abstract = new NamedRule(name);
                m_AbstractList = new List<SerializableRuleBase> { new SerializableNamedRule(name) };
                m_Object = new NamedRule(name);
            }

            public void Set(string fieldName, string name)
            {
                switch (fieldName)
                {
                    case nameof(m_Interface):
                        m_Interface = new NamedRule(name);
                        break;
                    case nameof(m_InterfaceList):
                        m_InterfaceList = new List<IRule> { new NamedRule(name) };
                        break;
                    case nameof(m_Abstract):
                        m_Abstract = new NamedRule(name);
                        break;
                    case nameof(m_AbstractList):
                        m_AbstractList = new List<SerializableRuleBase> { new SerializableNamedRule(name) };
                        break;
                    case nameof(m_Object):
                        m_Object = new NamedRule(name);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(fieldName), fieldName, null);
                }
            }
        }

        // These fixtures deliberately exercise unsupported [SerializeReference] dictionary declarations.
#pragma warning disable UAC1014
        private sealed class ManagedDictionaryHost : MonoBehaviour
        {
            [SerializeReference, IsValid] private Dictionary<string, NamedRule> m_Rules = new();
        }

        private sealed class ManagedDictionaryListHost : MonoBehaviour
        {
            [SerializeReference, IsValid] private List<Dictionary<string, NamedRule>> m_Rules = new();
        }

        private sealed class NonSerializableManagedClassCollectionHost : MonoBehaviour
        {
            [SerializeReference, IsValid] private List<RuleBase> m_Rules = new();
        }
#pragma warning restore UAC1014

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
        public void IsValid_SerializableStruct_ReportsNestedFieldPath()
        {
            var gameObject = new GameObject(nameof(StructHost));
            try
            {
                var host = gameObject.AddComponent<StructHost>();
                host.SetData(new StructData(string.Empty));
                using var serializedObject = new SerializedObject(host);
                Assert.IsNotNull(serializedObject.FindProperty("m_Data"));

                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
                ExpectFailure(validation, @"m_Data\.m_Name");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsValid_SerializableStructList_ReportsIndexedFieldPath()
        {
            var gameObject = new GameObject(nameof(StructHost));
            try
            {
                var host = gameObject.AddComponent<StructHost>();
                host.SetItems(new List<StructData> { new("Valid"), new(string.Empty) });
                using var serializedObject = new SerializedObject(host);
                Assert.IsNotNull(serializedObject.FindProperty("m_Items"));

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
        public void IsValid_SerializableStruct_RunsCustomValidation()
        {
            var gameObject = new GameObject(nameof(StructHost));
            try
            {
                var host = gameObject.AddComponent<StructHost>();
                host.SetRule(new StructRuleData(0));

                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
                ExpectFailure(validation, @"m_Rule\.m_Value");
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
        public void IsValid_CustomRuleException_ContinuesWithLaterElements()
        {
            var gameObject = new GameObject(nameof(ThrowingItemListHost));
            try
            {
                var host = gameObject.AddComponent<ThrowingItemListHost>();
                host.SetItems(new List<ThrowingItemData>
                {
                    new("Valid", true),
                    new(string.Empty, false)
                });

                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
                LogAssert.Expect(
                    LogType.Error,
                    new Regex(
                        @"(?s) - Variable: m_Items\[0\]\r?\n.*" +
                        Regex.Escape(k_CollectionValidatorExceptionMessage)));
                LogAssert.Expect(
                    LogType.Error,
                    new Regex(@" - Variable: m_Items\[1\]\.m_Name\r?\n"));
                validation.Report();
                LogAssert.NoUnexpectedReceived();
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
        [TestCase("m_Struct", nameof(IsValidTargetKind.Object))]
        [TestCase("m_StructList", nameof(IsValidTargetKind.Collection))]
        [TestCase("m_UnityStruct", nameof(IsValidTargetKind.Unsupported))]
        [TestCase("m_UnityClass", nameof(IsValidTargetKind.Unsupported))]
        [TestCase("m_Integer", nameof(IsValidTargetKind.Unsupported))]
        [TestCase("m_UnityObject", nameof(IsValidTargetKind.Unsupported))]
        [TestCase("m_IntegerList", nameof(IsValidTargetKind.Unsupported))]
        [TestCase("m_PlainInterface", nameof(IsValidTargetKind.Unsupported))]
        [TestCase("m_ManagedInterface", nameof(IsValidTargetKind.Object))]
        [TestCase("m_ManagedInterfaceList", nameof(IsValidTargetKind.Collection))]
        [TestCase("m_ManagedAbstract", nameof(IsValidTargetKind.Object))]
        [TestCase("m_ManagedAbstractList", nameof(IsValidTargetKind.Unsupported))]
        [TestCase("m_ManagedSerializableAbstractList", nameof(IsValidTargetKind.Collection))]
        [TestCase("m_ManagedObject", nameof(IsValidTargetKind.Object))]
        public void IsValid_TargetClassification_MatchesSupportedContract(string fieldName, string expected)
        {
            var field = typeof(UsageHost).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(field);
            Assert.AreEqual(expected, IsValidTargetUtility.Classify(field).ToString());
        }

        [TestCase("m_Interface", @"m_Interface\.m_Name")]
        [TestCase("m_InterfaceList", @"m_InterfaceList\[0\]\.m_Name")]
        [TestCase("m_Abstract", @"m_Abstract\.m_Name")]
        [TestCase("m_AbstractList", @"m_AbstractList\[0\]\.m_Name")]
        [TestCase("m_Object", @"m_Object\.m_Name")]
        public void IsValid_ManagedReference_ValidatesRuntimeValue(string fieldName, string variablePattern)
        {
            var gameObject = new GameObject(nameof(ManagedReferenceHost));
            try
            {
                var host = gameObject.AddComponent<ManagedReferenceHost>();
                host.Set(fieldName, string.Empty);

                using var validation = Validate(host);
                Assert.IsTrue(validation.Failed);
                ExpectFailure(validation, variablePattern);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsValid_ManagedReferencesWithValidValues_PassWithoutWarnings()
        {
            var gameObject = new GameObject(nameof(ManagedReferenceHost));
            try
            {
                var host = gameObject.AddComponent<ManagedReferenceHost>();
                host.SetAll("Valid");

                using var validation = Validate(host);
                Assert.IsTrue(validation.Passed);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsValid_ManagedReferenceDictionary_IsReportedAsNonserialized()
        {
            var gameObject = new GameObject(nameof(ManagedDictionaryHost));
            try
            {
                var host = gameObject.AddComponent<ManagedDictionaryHost>();
                SerializedFieldsValidator.ResetReportedNonSerializedFields();

                LogAssert.Expect(LogType.Warning, new Regex(@"nonserialized field '.*ManagedDictionaryHost\.m_Rules' are ignored"));
                using var validation = Validate(host);
                Assert.IsTrue(validation.Passed);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsValid_ManagedReferenceDictionaryList_IsReportedAsNonserialized()
        {
            var gameObject = new GameObject(nameof(ManagedDictionaryListHost));
            try
            {
                var host = gameObject.AddComponent<ManagedDictionaryListHost>();
                SerializedFieldsValidator.ResetReportedNonSerializedFields();

                LogAssert.Expect(LogType.Warning, new Regex(@"nonserialized field '.*ManagedDictionaryListHost\.m_Rules' are ignored"));
                using var validation = Validate(host);
                Assert.IsTrue(validation.Passed);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsValid_ManagedReferenceClassListWithoutSerializableDeclaration_IsReportedAsNonserialized()
        {
            var gameObject = new GameObject(nameof(NonSerializableManagedClassCollectionHost));
            try
            {
                var host = gameObject.AddComponent<NonSerializableManagedClassCollectionHost>();
                using var serializedObject = new SerializedObject(host);
                Assert.IsNull(serializedObject.FindProperty("m_Rules"));
                SerializedFieldsValidator.ResetReportedNonSerializedFields();

                LogAssert.Expect(
                    LogType.Warning,
                    new Regex(@"nonserialized field '.*NonSerializableManagedClassCollectionHost\.m_Rules' are ignored"));
                using var validation = Validate(host);
                Assert.IsTrue(validation.Passed);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static Validation Validate(UnityEngine.Object target)
        {
            var validation = new Validation(target);
            SerializedFieldsValidator.ValidateAttributes(target, validation);
            return validation;
        }

        private static void ExpectFailure(Validation validation, string variablePattern)
        {
            LogAssert.Expect(LogType.Error, new Regex(@" - Variable: " + variablePattern + @"\r?\n"));
            validation.Report();
            LogAssert.NoUnexpectedReceived();
        }
    }
}
