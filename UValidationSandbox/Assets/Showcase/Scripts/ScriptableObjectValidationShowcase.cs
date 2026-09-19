using System;
using System.Collections.Generic;
using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Demonstrates validation attributes and custom validation on a ScriptableObject asset.
    /// </summary>
    [CreateAssetMenu(menuName = "HighTower/UValidation/ScriptableObject Showcase")]
    public sealed class ScriptableObjectValidationShowcase : ScriptableObject, IValidatable
    {
        [Serializable]
        private sealed class NestedData
        {
            [SerializeField, NotEmpty] private string m_Name;
            [SerializeField, NotNull] private ScriptableObjectShowcaseReference m_Target;
        }

        [Header("NotNull")]
        [SerializeField, NotNull] private ScriptableObjectShowcaseReference m_RequiredReference;

        [Header("NotEmpty")]
        [SerializeField, NotEmpty] private string m_RequiredName = "Configured";
        [SerializeField, NotEmpty] private List<int> m_RequiredValues = new() { 1 };

        [Header("HasNoNulls")]
        [SerializeField, HasNoNulls]
        private ScriptableObjectShowcaseReference[] m_References = { };

        [Header("HasNoEmpties")]
        [SerializeField, HasNoEmpties]
        private List<string> m_Labels = new() { "Alpha", "Beta" };

        [Header("IsValid")]
        [SerializeField, IsValid] private NestedData m_Nested = new();

        [Header("IValidatable")]
        [SerializeField] private int m_MinimumValue = 1;
        [SerializeField] private int m_CurrentValue = 1;

        /// <inheritdoc />
        public void Validate(ref Validation validation)
        {
            validation.IsTrue(nameof(m_CurrentValue), this,
                asset => asset.m_CurrentValue >= asset.m_MinimumValue);
        }
    }
}
