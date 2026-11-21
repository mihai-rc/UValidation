using System.Collections.Generic;
using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Shows populated, empty, and ignored-entry dictionary uses of <see cref="NotEmptyAttribute"/>.
    /// </summary>
    public sealed class DictionaryNotEmptyShowcase : MonoBehaviour
    {
        [Header("Effective dictionary entries")]
        [Tooltip("At least one key/value pair is included in the live dictionary.")]
        [SerializeField, NotEmpty] private Dictionary<string, int> m_Populated = new();

        [Tooltip("An empty dictionary fails NotEmpty.")]
        [SerializeField, NotEmpty] private Dictionary<string, int> m_Empty = new();

        [Header("Null-key placeholder rows")]
        [Tooltip("Unity preserves these rows for editing but excludes them from the live dictionary, so NotEmpty still fails.")]
        [SerializeField, NotEmpty] private Dictionary<GameObject, string> m_NullKeyPlaceholders = new();
    }
}
