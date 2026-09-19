using System;
using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Shows nested attribute and custom validation through <see cref="IsValidAttribute"/>.
    /// </summary>
    public sealed class IsValidShowcase : MonoBehaviour
    {
        [Serializable]
        public sealed class NestedData
        {
            [SerializeField, NotEmpty] private string m_Name;
            [SerializeField, NotNull] private GameObject m_Target;
        }

        [Serializable]
        public sealed class RuleData : IValidatable
        {
            [SerializeField] private int m_PositiveValue = 1;

            /// <inheritdoc />
            public void Validate(ref Validation validation)
            {
                validation.IsTrue(nameof(m_PositiveValue), this, data => data.m_PositiveValue > 0);
            }
        }

        [Header("Nested serialized objects")]
        [Tooltip("All nested validation rules pass.")]
        [SerializeField, IsValid] private NestedData m_ValidNested = new();

        [Tooltip("The nested name is empty and its target is missing.")]
        [SerializeField, IsValid] private NestedData m_InvalidNested = new();

        [Tooltip("IsValid alone permits null; combine it with NotNull when the value is required.")]
        [SerializeField, IsValid] private NestedData m_NullNested;

        [Header("Nested IValidatable objects")]
        [SerializeField, IsValid] private RuleData m_ValidRule = new();
        [SerializeField, IsValid] private RuleData m_InvalidRule = new();

        [Header("Misuse - silently ignored by recursive validation")]
        [SerializeField, IsValid] private int m_PrimitiveMisuse;
        [SerializeField, IsValid] private GameObject m_UnityObjectMisuse;
    }
}
