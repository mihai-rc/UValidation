using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UValidation
{
    /// <summary>
    /// Recursively validates a custom serializable class or each element of an array or list of that type.
    /// </summary>
    /// <remarks>
    /// Null objects, null collections, empty collections, and null collection elements are skipped.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class IsValidAttribute : PropertyAttribute
    {
        /// <summary>
        /// The path of the file where the attribute is used.
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// The line of code at which the attribute is used.
        /// </summary>
        public readonly int LineNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsValidAttribute"/> class.
        /// </summary>
        /// <param name="filePath"> The source file containing the annotated field. </param>
        /// <param name="lineNumber"> The source line containing the annotated field. </param>
        public IsValidAttribute(
            [CallerFilePath] string filePath = "", 
            [CallerLineNumber] int lineNumber = 0)
            : base(true)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
        }
    }
}
