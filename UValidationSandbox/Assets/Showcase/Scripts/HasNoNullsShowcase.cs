using System.Collections.Generic;
using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Shows supported, failing, and unsupported uses of <see cref="HasNoNullsAttribute"/>.
    /// </summary>
    public sealed class HasNoNullsShowcase : MonoBehaviour
    {
        [Header("Unity object collections")]
        [SerializeField, HasNoNulls] private GameObject[] m_AllAssigned = { null, null };
        [SerializeField, HasNoNulls] private GameObject[] m_ContainsNull = { null, null };

        [Tooltip("Empty collections pass because there are no null elements.")]
        [SerializeField, HasNoNulls] private GameObject[] m_EmptyCollection = { };

        [Header("Misuse - Inspector warnings")]
        [Tooltip("Runtime validation supports IEnumerable<object>, but the drawer supports Unity object collections only.")]
        [SerializeField, HasNoNulls] private List<string> m_StringCollectionMisuse = new() { "Configured", null };

        [Tooltip("A scalar is not enumerable, so runtime attribute validation ignores this field.")]
        [SerializeField, HasNoNulls] private int m_ScalarMisuse;
    }
}
