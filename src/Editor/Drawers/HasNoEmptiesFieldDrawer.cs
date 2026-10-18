using UnityEditor;

namespace UValidation.Editor
{
    /// <summary>
    /// A custom property drawer for fields marked with the <see cref="HasNoEmptiesAttribute"/>.
    /// Displays an error message above a collection containing null or empty strings.
    /// </summary>
    /// <remarks>
    /// This drawer only operates on array/list fields whose elements are strings. For single string fields
    /// use <see cref="NotEmptyFieldDrawer"/> (via <see cref="NotEmptyAttribute"/>) instead.
    /// </remarks>
    [CustomPropertyDrawer(typeof(HasNoEmptiesAttribute))]
    public class HasNoEmptiesFieldDrawer : ValidationFieldDrawer
    {
        private const string k_AttributeName = nameof(HasNoEmptiesAttribute);
        private const string k_AllowedTypeDescription = "collections of strings";
        private const string k_ViolationMessage = "The collection must not contain null or empty strings.";

        /// <inheritdoc />
        protected override string AttributeName => k_AttributeName;

        /// <inheritdoc />
        protected override string AllowedTypeDescription => k_AllowedTypeDescription;

        /// <inheritdoc />
        protected override string ViolationMessage => k_ViolationMessage;

        /// <inheritdoc />
        protected override bool SupportsElementValidation => true;

        /// <inheritdoc />
        protected override bool IsUsageValid(SerializedProperty property)
        {
            return FieldDrawerHelper.IsCollection(property) &&
                ValidationAttributeTargetUtility.SupportsHasNoEmpties(fieldInfo.FieldType);
        }

        /// <inheritdoc />
        protected override bool IsPropertyValid(SerializedProperty property)
        {
            return FieldDrawerHelper.HasNoEmptyStrings(property);
        }

        /// <inheritdoc />
        protected override bool IsElementValid(SerializedProperty property)
        {
            return !string.IsNullOrEmpty(property.stringValue);
        }
    }
}
