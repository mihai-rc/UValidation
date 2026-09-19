using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UValidation
{
    /// <summary>
    /// Specifies that the field must not be null.
    /// Can be applied to reference-type fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class NotNullAttribute : PropertyAttribute
    {
        /// <summary>
        /// The path of the file where the attribute is used.
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// The line of code at which the attribute is used.
        /// </summary>
        public readonly int LineNumber;

        public NotNullAttribute(
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
        }
    }
}
