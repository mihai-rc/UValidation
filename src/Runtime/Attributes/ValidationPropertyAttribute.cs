using UnityEngine;

namespace UValidation
{
    /// <summary>
    /// Provides source-location information for a validation property attribute.
    /// </summary>
    public abstract class ValidationPropertyAttribute : PropertyAttribute
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
        /// Initializes a new source-located validation attribute.
        /// </summary>
        /// <param name="filePath"> The source file containing the annotated field. </param>
        /// <param name="lineNumber"> The source line containing the annotated field. </param>
        /// <param name="applyToCollection"> Whether Unity applies the drawer to the collection itself. </param>
        protected ValidationPropertyAttribute(string filePath, int lineNumber, bool applyToCollection = false)
            : base(applyToCollection)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
        }
    }
}
