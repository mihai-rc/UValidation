using UnityEditor;

namespace UValidation.Editor
{
    /// <summary>
    /// A custom property drawer for fields marked with the <see cref="NotNullAttribute"/>.
    /// Displays an error message if the object reference field is not assigned.
    /// </summary>
    /// <remarks>
    /// This drawer intentionally does nothing on collection fields. Use <see cref="HasNoNullsFieldDrawer"/>
    /// (via <see cref="HasNoNullsAttribute"/>) for collections where each element must be non-null.
    /// </remarks>
    [CustomPropertyDrawer(typeof(NotNullAttribute))]
    public class NotNullFieldDrawer : ValidationFieldDrawer
    {
        private const string k_AttributeName = nameof(NotNullAttribute);
        private const string k_AllowedTypeDescription = "UnityEngine.Object reference";
        private const string k_ViolationMessage = "An object reference must be assigned.";

        /// <inheritdoc />
        protected override string AttributeName => k_AttributeName;

        /// <inheritdoc />
        protected override string AllowedTypeDescription => k_AllowedTypeDescription;

        /// <inheritdoc />
        protected override string ViolationMessage => k_ViolationMessage;

        /// <inheritdoc />
        protected override bool IsUsageValid(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.ObjectReference &&
                ValidationAttributeTargetUtility.SupportsNotNull(fieldInfo.FieldType);
        }

        /// <inheritdoc />
        protected override bool IsPropertyValid(SerializedProperty property)
        {
            return property.objectReferenceValue != null;
        }
    }
}
