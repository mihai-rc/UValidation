using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
    public class NotNullFieldDrawer : PropertyDrawer
    {
        private const string k_AttributeName = nameof(NotNullAttribute);
        private const string k_AllowedTypeDescription = "UnityEngine.Object reference";
        private const string k_ViolationMessage = "An object reference must be assigned.";

        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return FieldDrawerHelper.CreatePropertyGUI(
                property,
                k_AttributeName,
                k_AllowedTypeDescription,
                k_ViolationMessage,
                p => IsObjectReference(p) && typeof(UnityEngine.Object).IsAssignableFrom(fieldInfo.FieldType),
                p => p.objectReferenceValue != null);
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
                p => IsObjectReference(p) && typeof(UnityEngine.Object).IsAssignableFrom(fieldInfo.FieldType),
                p => p.objectReferenceValue != null);
        }

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return FieldDrawerHelper.GetPropertyHeight(property, label, k_AttributeName,
                k_AllowedTypeDescription, k_ViolationMessage,
                p => IsObjectReference(p) && typeof(UnityEngine.Object).IsAssignableFrom(fieldInfo.FieldType),
                p => p.objectReferenceValue != null);
        }

        private static bool IsObjectReference(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.ObjectReference;
        }
    }
}
