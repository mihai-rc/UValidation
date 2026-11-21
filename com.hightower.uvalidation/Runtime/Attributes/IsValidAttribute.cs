using System;
using UnityEngine;

namespace UValidation
{
    /// <summary>
    /// Recursively validates a custom serializable class or struct, each element of an array or list of that type,
    /// or each value in a supported dictionary. Dictionary values may themselves be arrays or lists.
    /// </summary>
    /// <remarks>
    /// On a [SerializeReference] field declared as an interface, an abstract class, or object, each value is
    /// validated according to its runtime type. Null objects, null collections, empty collections, and null
    /// collection elements are skipped. Unity requires a class used as the declared element type of a
    /// [SerializeReference] array or list to have [Serializable]. Dictionary support requires Unity 6.6 or
    /// newer, applies to values only, and requires native [SerializeField] dictionary serialization.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class IsValidAttribute : PropertyAttribute
    {
        /// <summary>
        /// Creates a new <see cref="IsValidAttribute"/> instance.
        /// </summary>
        public IsValidAttribute()
            : base(true)
        {
        }
    }
}
