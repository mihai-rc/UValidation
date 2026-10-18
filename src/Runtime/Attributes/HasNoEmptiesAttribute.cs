using System;
using System.Runtime.CompilerServices;

namespace UValidation
{
    /// <summary>
    /// Specifies that a serialized string array or list must not contain null or empty items.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class HasNoEmptiesAttribute : ValidationPropertyAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HasNoEmptiesAttribute"/> class.
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
