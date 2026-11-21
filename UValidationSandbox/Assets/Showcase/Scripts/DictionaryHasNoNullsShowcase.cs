using System.Collections.Generic;
using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Shows value-oriented dictionary uses of <see cref="HasNoNullsAttribute"/>.
    /// </summary>
    public sealed class DictionaryHasNoNullsShowcase : MonoBehaviour
    {
        [Tooltip("Every dictionary value is an assigned Unity object.")]
        [SerializeField, HasNoNulls] private Dictionary<string, GameObject> m_AllAssigned = new();

        [Tooltip("The second dictionary value is missing.")]
        [SerializeField, HasNoNulls] private Dictionary<string, GameObject> m_ContainsNull = new();

        [Tooltip("Empty dictionaries pass because there are no null values.")]
        [SerializeField, HasNoNulls] private Dictionary<string, GameObject> m_Empty = new();
    }
}
