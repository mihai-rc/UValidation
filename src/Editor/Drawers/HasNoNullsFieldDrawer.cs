using UnityEditor;

namespace UValidation.Editor
{
    /// <summary>
    /// A custom property drawer for fields marked with the <see cref="HasNoNullsAttribute"/>.
    /// Displays an error message above a collection containing null object references.
    /// </summary>
    /// <remarks>
    /// This drawer operates on array/list fields and, in Unity 6.6+, on supported dictionary values. For a
    /// single object reference field use <see cref="NotNullFieldDrawer"/> (via
    /// <see cref="NotNullAttribute"/>) instead.
    /// </remarks>
    [CustomPropertyDrawer(typeof(HasNoNullsAttribute))]
    internal class HasNoNullsFieldDrawer : ValidationFieldDrawer
    {
        private const string k_AttributeName = nameof(HasNoNullsAttribute);
        private const string k_AllowedTypeDescription = "collections of UnityEngine.Object references";
        private const string k_ViolationMessage = "The collection must not contain null object references.";

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
                ValidationAttributeTargetUtility.SupportsHasNoNulls(fieldInfo.FieldType);
        }

        /// <inheritdoc />
        protected override bool IsPropertyValid(SerializedProperty property)
        {
            return UnitySerializedCollectionUtility.IsDictionary(fieldInfo.FieldType)
                ? FieldDrawerHelper.HasNoNullDictionaryValues(property)
                : FieldDrawerHelper.HasNoNullObjectReferences(property);
        }

        /// <inheritdoc />
        protected override bool IsElementValid(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.ObjectReference &&
                property.objectReferenceValue != null;
        }
    }
}
