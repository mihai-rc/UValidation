using System;
using System.Runtime.CompilerServices;

namespace UValidation
{
    /// <summary>
    /// Specifies that a serialized Unity object reference must be assigned.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class NotNullAttribute : ValidationPropertyAttribute
    {
        /// <summary>
        /// Creates a new <see cref="NotNullAttribute"/> instance.
        /// </summary>
        /// <param name="filePath"> The source file containing the annotated field. </param>
        /// <param name="lineNumber"> The source line containing the annotated field. </param>
        public NotNullAttribute(
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
        }
    }
}
