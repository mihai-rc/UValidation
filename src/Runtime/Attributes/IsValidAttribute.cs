using System;
using UnityEngine;

namespace UValidation
{
    /// <summary>
    /// Recursively validates a custom serializable class or each element of an array or list of that type.
    /// </summary>
    /// <remarks>
    /// On a [SerializeReference] field declared as an interface, an abstract class, or object, each value is
    /// validated according to its runtime type. Null objects, null collections, empty collections, and null
    /// collection elements are skipped.
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
