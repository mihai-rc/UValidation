using UnityEditor;

namespace UValidation.Editor
{
    /// <summary>
    /// A custom property drawer for fields marked with the <see cref="NotEmptyAttribute"/>.
    /// Displays an error message if the corresponding string or collection is empty.
    /// </summary>
    [CustomPropertyDrawer(typeof(NotEmptyAttribute))]
    internal class NotEmptyFieldDrawer : ValidationFieldDrawer
    {
        private const string k_AttributeName = nameof(NotEmptyAttribute);
        private const string k_AllowedTypeDescription = "strings or collections";
        private const string k_ViolationMessage = "The value must not be empty.";

        /// <inheritdoc />
        protected override string AttributeName => k_AttributeName;

        /// <inheritdoc />
        protected override string AllowedTypeDescription => k_AllowedTypeDescription;

        /// <inheritdoc />
        protected override string ViolationMessage => k_ViolationMessage;

        /// <inheritdoc />
        protected override bool IsUsageValid(SerializedProperty property)
        {
            return (property.propertyType == SerializedPropertyType.String || FieldDrawerHelper.IsCollection(property)) &&
                ValidationAttributeTargetUtility.SupportsNotEmpty(fieldInfo.FieldType);
        }

        /// <inheritdoc />
        protected override bool IsPropertyValid(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.String
                ? !string.IsNullOrEmpty(property.stringValue)
                : property.arraySize > 0;
        }
    }
}
