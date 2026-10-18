using System;
using System.Runtime.CompilerServices;

namespace UValidation
{
    /// <summary>
    /// Specifies that a serialized array or list of Unity object references must not contain null items.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class HasNoNullsAttribute : ValidationPropertyAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HasNoNullsAttribute"/> class.
        /// </summary>
        /// <param name="filePath"> The source file containing the annotated field. </param>
        /// <param name="lineNumber"> The source line containing the annotated field. </param>
        public HasNoNullsAttribute(
            [CallerFilePath] string filePath = "", 
            [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber, true)
        {
        }
    }
}
