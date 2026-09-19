using System;
using System.Collections.Generic;
using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Shows useful combinations of validation attributes.
    /// </summary>
    public sealed class CombinedAttributesShowcase : MonoBehaviour
    {
        [Serializable]
        public sealed class RequiredNestedData
        {
            [SerializeField, NotEmpty] private string m_Name = "Configured";
            [SerializeField, NotNull] private GameObject m_Target;
        }

        [Header("Required string collection with valid elements")]
        [SerializeField, NotEmpty, HasNoEmpties]
        private List<string> m_RequiredStrings = new() { "Alpha", "Beta" };

        [Header("Required string collection with an invalid element")]
        [SerializeField, NotEmpty, HasNoEmpties]
        private List<string> m_RequiredStringsWithEmpty = new() { "Alpha", "" };

        [Header("Required object collection with valid elements")]
        [SerializeField, NotEmpty, HasNoNulls]
        private GameObject[] m_RequiredObjects = { null, null };

        [Header("Required object collection with a null element")]
        [SerializeField, NotEmpty, HasNoNulls]
        private GameObject[] m_RequiredObjectsWithNull = { null, null };

        [Header("Required and recursively validated object")]
        [SerializeField, NotNull, IsValid]
        private RequiredNestedData m_RequiredNested = new();
    }
}
