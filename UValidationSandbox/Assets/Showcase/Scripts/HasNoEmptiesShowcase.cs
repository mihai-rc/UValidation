using System.Collections.Generic;
using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Shows supported, failing, and unsupported uses of <see cref="HasNoEmptiesAttribute"/>.
    /// </summary>
    public sealed class HasNoEmptiesShowcase : MonoBehaviour
    {
        [Header("String collections")]
        [SerializeField, HasNoEmpties] private List<string> m_AllPopulated = new() { "Alpha", "Beta" };
        [SerializeField, HasNoEmpties] private List<string> m_ContainsEmpty = new() { "Alpha", "" };
        [SerializeField, HasNoEmpties] private List<string> m_ContainsNull = new() { "Alpha", null };

        [Tooltip("Empty collections pass because there are no empty elements.")]
        [SerializeField, HasNoEmpties] private List<string> m_EmptyCollection = new();

        [Header("Misuse - Inspector warnings and runtime no-op")]
        [SerializeField, HasNoEmpties] private GameObject[] m_ObjectCollectionMisuse = { null };
        [SerializeField, HasNoEmpties] private int m_ScalarMisuse;
    }
}
