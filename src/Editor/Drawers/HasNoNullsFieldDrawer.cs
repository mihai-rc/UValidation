using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UValidation.Editor
{
    /// <summary>
    /// A custom property drawer for fields marked with the <see cref="HasNoNullsAttribute"/>.
    /// Displays an error message above a collection containing null object references.
    /// </summary>
    /// <remarks>
    /// This drawer only operates on array/list fields. For single object reference fields use
    /// <see cref="NotNullFieldDrawer"/> (via <see cref="NotNullAttribute"/>) instead.
    /// </remarks>
    [CustomPropertyDrawer(typeof(HasNoNullsAttribute))]
    public class HasNoNullsFieldDrawer : PropertyDrawer
    {
        private const string k_AttributeName = nameof(HasNoNullsAttribute);
        private const string k_AllowedTypeDescription = "collections of UnityEngine.Object references";
        private const string k_ViolationMessage = "The collection must not contain null object references.";

        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return FieldDrawerHelper.CreatePropertyGUI(
                property, 
                k_AttributeName,
                k_AllowedTypeDescription,
                k_ViolationMessage,
                p => FieldDrawerHelper.IsCollection(p) &&
                    FieldDrawerHelper.IsCollectionOf(fieldInfo.FieldType, typeof(UnityEngine.Object)),
                FieldDrawerHelper.HasNoNullObjectReferences,
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
                p => FieldDrawerHelper.IsCollection(p) &&
                    FieldDrawerHelper.IsCollectionOf(fieldInfo.FieldType, typeof(UnityEngine.Object)),
                FieldDrawerHelper.HasNoNullObjectReferences,
                p => p.objectReferenceValue != null);
        }

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return FieldDrawerHelper.GetPropertyHeight(property, label, k_AttributeName,
                k_AllowedTypeDescription, k_ViolationMessage,
                p => FieldDrawerHelper.IsCollection(p) &&
                    FieldDrawerHelper.IsCollectionOf(fieldInfo.FieldType, typeof(UnityEngine.Object)),
                FieldDrawerHelper.HasNoNullObjectReferences);
        }
    }
}
