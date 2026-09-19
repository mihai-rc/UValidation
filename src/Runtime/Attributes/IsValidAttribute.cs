using System;
using System.Runtime.CompilerServices;

namespace UValidation
{
    /// <summary>
    /// Specifies that the field must reference a valid object.
    /// Can be applied to fields that are instance of a serializable custom type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class IsValidAttribute : Attribute
    {
        /// <summary>
        /// The path of the file where the attribute is used.
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// The line of code at which the attribute is used.
        /// </summary>
        public readonly int LineNumber;

        public IsValidAttribute(
            [CallerFilePath] string filePath = "", 
            [CallerLineNumber] int lineNumber = 0)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
        }
    }
}