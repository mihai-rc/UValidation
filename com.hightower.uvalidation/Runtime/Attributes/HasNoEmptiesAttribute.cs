using System;
using System.Runtime.CompilerServices;

namespace UValidation
{
    /// <summary>
    /// Specifies that a serialized array or list of strings, or a supported dictionary with string values,
    /// must not contain null or empty values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class HasNoEmptiesAttribute : ValidationPropertyAttribute
    {
        /// <summary>
        /// Creates a new <see cref="HasNoEmptiesAttribute"/> instance.
        /// </summary>
        /// <param name="filePath"> The source file containing the annotated field. </param>
        /// <param name="lineNumber"> The source line containing the annotated field. </param>
        public HasNoEmptiesAttribute(
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber, true)
        {
        }
    }
}
