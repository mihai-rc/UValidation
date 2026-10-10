using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UValidation.Editor
{
    /// <summary>
    /// A custom property drawer for fields marked with the <see cref="IsValidAttribute"/>.
    /// Displays a warning when the field cannot be recursively validated.
    /// </summary>
    [CustomPropertyDrawer(typeof(IsValidAttribute))]
    public class IsValidFieldDrawer : PropertyDrawer
    {
        private const string k_AttributeName = nameof(IsValidAttribute);
        private const string k_AllowedTypeDescription =
            "custom serializable classes or arrays/lists of custom serializable classes";
        private const string k_ViolationMessage = "Nested values must satisfy their validation rules.";

        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return FieldDrawerHelper.CreatePropertyGUI(
                property,
                k_AttributeName,
                k_AllowedTypeDescription,
                k_ViolationMessage,
                IsSupportedType,
                _ => true);
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
                _ => true);
        }

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return FieldDrawerHelper.GetPropertyHeight(
                property,
                label,
                k_AttributeName,
                k_AllowedTypeDescription,
                k_ViolationMessage,
                IsSupportedType,
                _ => true);
        }

        private bool IsSupportedType(SerializedProperty property)
        {
            return IsValidTargetUtility.Classify(fieldInfo, out _) != IsValidTargetKind.Unsupported;
        }
    }
}
