using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
    public class HasNoEmptiesFieldDrawer : PropertyDrawer
    {
        private const string k_AttributeName = nameof(HasNoEmptiesAttribute);
        private const string k_AllowedTypeDescription = "collections of strings";
        private const string k_ViolationMessage = "The collection must not contain null or empty strings.";

        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return FieldDrawerHelper.CreatePropertyGUI(
                property,
                k_AttributeName,
                k_AllowedTypeDescription,
                k_ViolationMessage,
                p => FieldDrawerHelper.IsCollection(p) &&
                    FieldDrawerHelper.IsCollectionOf(fieldInfo.FieldType, typeof(string)),
                FieldDrawerHelper.HasNoEmptyStrings,
                p => !string.IsNullOrEmpty(p.stringValue));
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            FieldDrawerHelper.OnGUI(
                position,
                property,
                label,
                k_AttributeName,
                k_AllowedTypeDescription,
                k_ViolationMessage,
                p => FieldDrawerHelper.IsCollection(p) &&
                    FieldDrawerHelper.IsCollectionOf(fieldInfo.FieldType, typeof(string)),
                FieldDrawerHelper.HasNoEmptyStrings,
                p => !string.IsNullOrEmpty(p.stringValue));
        }

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return FieldDrawerHelper.GetPropertyHeight(property, label, k_AttributeName,
                k_AllowedTypeDescription, k_ViolationMessage,
                p => FieldDrawerHelper.IsCollection(p) &&
                    FieldDrawerHelper.IsCollectionOf(fieldInfo.FieldType, typeof(string)),
                FieldDrawerHelper.HasNoEmptyStrings);
        }
    }
}
