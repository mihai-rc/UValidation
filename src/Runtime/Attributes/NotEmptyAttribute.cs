using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UValidation
{
    /// <summary>
    /// Specifies that the field must not contain null items.
    /// Can be applied to fields that are strings or collections.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class NotEmptyAttribute : PropertyAttribute
    {
        /// <summary>
        /// The path of the file where the attribute is used.
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// The line of code at which the attribute is used.
        /// </summary>
        public readonly int LineNumber;

        public NotEmptyAttribute(
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
            : base(true)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
        }
    }
}
