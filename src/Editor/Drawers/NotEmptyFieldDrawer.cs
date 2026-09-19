using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UValidation.Editor
{
    /// <summary>
    /// A custom property drawer for fields marked with the <see cref="NotEmptyAttribute"/>.
    /// Displays an error message if the corresponding string or collection is empty.
    /// </summary>
    [CustomPropertyDrawer(typeof(NotEmptyAttribute))]
    public class NotEmptyFieldDrawer : PropertyDrawer
    {
        private const string k_AttributeName = nameof(NotEmptyAttribute);
        private const string k_AllowedTypeDescription = "strings or collections";
        private const string k_ViolationMessage = "The value must not be empty.";

        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return FieldDrawerHelper.CreatePropertyGUI(
                property, 
                k_AttributeName,
                k_AllowedTypeDescription,
                k_ViolationMessage,
                IsSupportedType,
                IsNotEmpty);
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
                IsSupportedType,
                IsNotEmpty);
        }

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return FieldDrawerHelper.GetPropertyHeight(property, label, k_AttributeName,
                k_AllowedTypeDescription, k_ViolationMessage, IsSupportedType, IsNotEmpty);
        }

        private static bool IsSupportedType(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.String || FieldDrawerHelper.IsCollection(property);
        }

        private static bool IsNotEmpty(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.String
                ? !string.IsNullOrEmpty(property.stringValue)
                : property.arraySize > 0;
        }
    }
}
