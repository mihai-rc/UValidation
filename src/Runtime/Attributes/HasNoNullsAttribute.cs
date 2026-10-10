using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UValidation
{
    /// <summary>
    /// Specifies that a serialized array or list of Unity object references must not contain null items.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class HasNoNullsAttribute : PropertyAttribute
    {
        /// <summary>
        /// The path of the file where the attribute is used.
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// The line of code at which the attribute is used.
        /// </summary>
        public readonly int LineNumber;

        public HasNoNullsAttribute(
            [CallerFilePath] string filePath = "", 
            [CallerLineNumber] int lineNumber = 0)
            : base(true)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
        }
    }
}
