using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Declares a required value at the intermediate level of the showcase chain.
    /// </summary>
    public abstract class InheritanceShowcaseMiddle : InheritanceShowcaseBase
    {
        [Header("Middle class - private serialized field")]
        [SerializeField, NotEmpty] private string m_MiddleLabel = "Configured";
    }
}
