using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Shows validation across three inheritance levels with independently invalid fields.
    /// </summary>
    public sealed class InheritanceShowcase : InheritanceShowcaseMiddle
    {
        [Header("Derived class - private serialized field")]
        [SerializeField, NotNull] private GameObject m_DerivedReference;
    }
}
