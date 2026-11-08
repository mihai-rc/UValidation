using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Pool;

namespace UValidation.Editor
{
    /// <summary>
    /// Adapts Unity's native serialized-dictionary API to the version-independent validation code.
    /// </summary>
    /// <remarks>
    /// Unity versions before 6.6 compile the same validation pipeline, but report native dictionaries as
    /// unsupported. When the compatibility floor moves forward, only this adapter's fallback needs removal.
    /// </remarks>
    internal static class SerializedDictionaryAdapter
    {
        /// <summary>
        /// Enumerates values belonging to entries Unity includes in the live dictionary.
        /// </summary>
        internal struct EffectiveValueEnumerator : IDisposable
        {
            private SerializedProperty m_Dictionary;
            private HashSet<int> m_IgnoredEntryIndices;
            private int m_NextIndex;

            internal EffectiveValueEnumerator(SerializedProperty dictionary)
            {
#if UNITY_6000_6_OR_NEWER
                m_Dictionary = dictionary;
                m_IgnoredEntryIndices = HashSetPool<int>.Get();
                if (dictionary != null)
                {
                    var ignoredEntries = dictionary.GetDictionaryIgnoredEntries();
                    m_IgnoredEntryIndices.UnionWith(ignoredEntries.duplicateEntryIndices);
                    m_IgnoredEntryIndices.UnionWith(ignoredEntries.nullKeyEntryIndices);
                }
#else
                m_Dictionary = null;
                m_IgnoredEntryIndices = null;
#endif
                m_NextIndex = 0;
            }

            /// <summary>
            /// Advances to the next effective dictionary value.
            /// </summary>
            /// <param name="value"> The serialized value property. </param>
            /// <param name="entryIndex"> The value's serialized dictionary-entry index. </param>
            /// <returns> Whether another effective value was found. </returns>
            internal bool MoveNext(out SerializedProperty value, out int entryIndex)
            {
                while (m_Dictionary != null && m_NextIndex < m_Dictionary.arraySize)
                {
                    entryIndex = m_NextIndex++;
                    if (m_IgnoredEntryIndices.Contains(entryIndex))
                    {
                        continue;
                    }

                    var entry = m_Dictionary.GetArrayElementAtIndex(entryIndex);
                    value = entry.FindPropertyRelative("value");
                    return true;
                }

                value = null;
                entryIndex = -1;
                return false;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                m_Dictionary = null;
                if (m_IgnoredEntryIndices == null)
                {
                    return;
                }

                HashSetPool<int>.Release(m_IgnoredEntryIndices);
                m_IgnoredEntryIndices = null;
            }
        }

        /// <summary>
        /// Gets whether the current Unity version natively serializes the exact dictionary type.
        /// </summary>
        /// <param name="type"> The type to inspect. </param>
        /// <returns> Whether the type is a supported native dictionary. </returns>
        internal static bool IsSupportedType(Type type)
        {
#if UNITY_6000_6_OR_NEWER
            return type != null &&
                   type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
#else
            return false;
#endif
        }

        /// <summary>
        /// Gets an allocation-free enumerator over values Unity includes in the live dictionary.
        /// </summary>
        /// <param name="dictionary"> The serialized dictionary property. </param>
        /// <returns> An enumerator that must be disposed. </returns>
        internal static EffectiveValueEnumerator EnumerateEffectiveValues(SerializedProperty dictionary)
        {
            return new EffectiveValueEnumerator(dictionary);
        }

        /// <summary>
        /// Gets whether a serialized dictionary contains at least one effective entry.
        /// </summary>
        /// <param name="dictionary"> The serialized dictionary property. </param>
        /// <returns> Whether Unity includes at least one entry in the live dictionary. </returns>
        internal static bool HasEffectiveEntries(SerializedProperty dictionary)
        {
            using var values = EnumerateEffectiveValues(dictionary);
            return values.MoveNext(out _, out _);
        }

        /// <summary>
        /// Gets whether every effective dictionary value passes a validation predicate.
        /// </summary>
        /// <param name="dictionary"> The serialized dictionary property. </param>
        /// <param name="validation"> The validation predicate to apply to each value. </param>
        /// <returns> Whether every effective value passes. </returns>
        internal static bool EveryEffectiveValuePasses(
            SerializedProperty dictionary,
            Func<SerializedProperty, bool> validation)
        {
            if (validation == null)
            {
                return false;
            }

            using var values = EnumerateEffectiveValues(dictionary);
            while (values.MoveNext(out var value, out _))
            {
                if (value == null || !validation(value))
                {
                    return false;
                }
            }

#if UNITY_6000_6_OR_NEWER
            return true;
#else
            return false;
#endif
        }
    }
}
