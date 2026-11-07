using System;
using UnityEngine;

namespace UValidation
{
    /// <summary>
    /// Recursively validates a custom serializable class/struct, each element of an array or list of that type,
    /// or each value in a supported dictionary of that type.
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
        /// Initializes a new instance of the <see cref="IsValidAttribute"/> class.
        /// </summary>
        public IsValidAttribute()
            : base(true)
        {
        }
    }
}
