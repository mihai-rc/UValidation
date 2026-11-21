using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Declares required values at the root of the inheritance showcase chain.
    /// </summary>
    public abstract class InheritanceShowcaseBase : MonoBehaviour
    {
        [Header("Base class - private serialized field")]
        [SerializeField, NotNull] private GameObject m_BaseReference;

        /// <summary>
        /// Gets the required label declared on the base class.
        /// </summary>
        [field: Header("Base class - serialized auto-property")]
        [field: SerializeField, NotEmpty]
        public string BaseLabel { get; private set; } = "Configured";
    }
}
