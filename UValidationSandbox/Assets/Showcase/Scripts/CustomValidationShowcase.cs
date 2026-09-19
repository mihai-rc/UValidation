using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Demonstrates component-level custom validation with <see cref="IValidatable"/>.
    /// </summary>
    public sealed class CustomValidationShowcase : MonoBehaviour, IValidatable
    {
        [SerializeField] private int m_MinimumValue = 1;
        [SerializeField] private int m_CurrentValue = 1;

        /// <inheritdoc />
        public void Validate(ref Validation validation)
        {
            validation.IsTrue(nameof(m_CurrentValue), this,
                component => component.m_CurrentValue >= component.m_MinimumValue);
        }
    }
}
