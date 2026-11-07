#if UNITY_6000_6_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UValidation.Tests
{
    /// <summary>
    /// Provides serialized dictionary data for scene and asset integration tests.
    /// </summary>
    public sealed class DictionaryValidationProbe : MonoBehaviour
    {
        [Serializable]
        private sealed class EntryData
        {
            [SerializeField, NotEmpty] private string m_Name;

            public EntryData(string name)
            {
                m_Name = name;
            }
        }

        [SerializeField, NotEmpty, HasNoEmpties]
        private Dictionary<string, string> m_Labels = new();

        [SerializeField, IsValid]
        private Dictionary<string, EntryData> m_Entries = new();

        /// <summary>
        /// Populates every dictionary with valid serialized data.
        /// </summary>
        public void SetValid()
        {
            m_Labels.Clear();
            m_Labels.Add("primary", "Valid");
            m_Entries.Clear();
            m_Entries.Add("primary", new EntryData("Valid"));
        }

        /// <summary>
        /// Makes a dictionary string value fail <see cref="HasNoEmptiesAttribute"/>.
        /// </summary>
        public void SetInvalidLabelValue()
        {
            SetValid();
            m_Labels["primary"] = string.Empty;
        }

        /// <summary>
        /// Makes a nested dictionary value fail recursive <see cref="IsValidAttribute"/> validation.
        /// </summary>
        public void SetInvalidNestedValue()
        {
            SetValid();
            m_Entries["primary"] = new EntryData(string.Empty);
        }
    }
}
#endif
