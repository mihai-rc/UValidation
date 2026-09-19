using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Shows supported, failing, and unsupported uses of <see cref="NotNullAttribute"/>.
    /// </summary>
    public sealed class NotNullShowcase : MonoBehaviour
    {
        [Header("Supported - passes")]
        [SerializeField, NotNull] private GameObject m_AssignedGameObject;

        [Header("Supported - fails")]
        [SerializeField, NotNull] private Transform m_MissingTransform;

        [Header("Misuse - Inspector warning")]
        [Tooltip("Runtime validation sees a non-null boxed string, but the drawer supports Unity object references only.")]
        [SerializeField, NotNull] private string m_StringMisuse = "Not null at runtime";

        [Tooltip("Value types can never be null and are not a meaningful NotNull target.")]
        [SerializeField, NotNull] private int m_ValueTypeMisuse = 42;
    }
}
