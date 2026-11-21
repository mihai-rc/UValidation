using System.Collections.Generic;
using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Shows value-oriented dictionary uses of <see cref="HasNoEmptiesAttribute"/>.
    /// </summary>
    public sealed class DictionaryHasNoEmptiesShowcase : MonoBehaviour
    {
        [Tooltip("Every dictionary value contains text.")]
        [SerializeField, HasNoEmpties] private Dictionary<int, string> m_AllPopulated = new();

        [Tooltip("The second dictionary value is empty.")]
        [SerializeField, HasNoEmpties] private Dictionary<int, string> m_ContainsEmpty = new();

        [Tooltip("The second dictionary value is null.")]
        [SerializeField, HasNoEmpties] private Dictionary<int, string> m_ContainsNull = new();

        [Tooltip("Empty dictionaries pass because there are no empty values.")]
        [SerializeField, HasNoEmpties] private Dictionary<int, string> m_Empty = new();
    }
}
