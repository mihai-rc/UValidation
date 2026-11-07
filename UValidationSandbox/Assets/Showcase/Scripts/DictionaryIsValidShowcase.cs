using System;
using System.Collections.Generic;
using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Shows recursive validation of custom serializable dictionary values.
    /// </summary>
    public sealed class DictionaryIsValidShowcase : MonoBehaviour
    {
        [Serializable]
        public sealed class EntryData
        {
            [SerializeField, NotEmpty] private string m_Name;
            [SerializeField, NotNull] private GameObject m_Target;
        }

        [Tooltip("Every custom dictionary value satisfies its nested validation rules.")]
        [SerializeField, IsValid] private Dictionary<string, EntryData> m_ValidEntries = new();

        [Tooltip("The custom value has an empty name and a missing target.")]
        [SerializeField, IsValid] private Dictionary<string, EntryData> m_InvalidEntries = new();

        [Tooltip("Empty dictionaries pass recursive validation.")]
        [SerializeField, IsValid] private Dictionary<string, EntryData> m_EmptyEntries = new();

        [Header("Misuse - Inspector warning")]
        [Tooltip("IsValid requires custom serializable values, not primitive values.")]
        [SerializeField, IsValid] private Dictionary<string, int> m_PrimitiveValueMisuse = new();
    }
}
