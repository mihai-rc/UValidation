using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UValidation.Editor
{
    /// <summary>
    /// Adapts one validation rule to Unity's UI Toolkit and IMGUI property-drawer entry points.
    /// </summary>
    public abstract class ValidationFieldDrawer : PropertyDrawer
    {
        /// <summary>
        /// Gets the validation attribute name shown in usage warnings.
        /// </summary>
        protected abstract string AttributeName { get; }

        /// <summary>
        /// Gets the supported field-type description shown in usage warnings.
        /// </summary>
        protected abstract string AllowedTypeDescription { get; }

        /// <summary>
        /// Gets the message shown when the property fails validation.
        /// </summary>
        protected abstract string ViolationMessage { get; }

        /// <summary>
        /// Gets whether collection elements should receive individual validation icons.
        /// </summary>
        protected virtual bool SupportsElementValidation => false;

        /// <inheritdoc />
        public sealed override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return FieldDrawerHelper.CreatePropertyGUI(
                property,
                AttributeName,
                AllowedTypeDescription,
                ViolationMessage,
                IsUsageValid,
                IsPropertyValid,
                GetElementValidation());
        }

        /// <inheritdoc />
        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            FieldDrawerHelper.OnGUI(
                position,
                property,
                label,
                AttributeName,
                AllowedTypeDescription,
                ViolationMessage,
                IsUsageValid,
                IsPropertyValid,
                GetElementValidation());
        }

        /// <inheritdoc />
        public sealed override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return FieldDrawerHelper.GetPropertyHeight(
                property,
                label,
                AttributeName,
                AllowedTypeDescription,
                ViolationMessage,
                IsUsageValid,
                IsPropertyValid);
        }

        /// <summary>
        /// Gets whether the attribute supports the property declaration.
        /// </summary>
        /// <param name="property"> The property being drawn. </param>
        /// <returns> Whether the attribute is valid on the property. </returns>
        protected abstract bool IsUsageValid(SerializedProperty property);

        /// <summary>
        /// Gets whether the property value passes validation.
        /// </summary>
        /// <param name="property"> The property being drawn. </param>
        /// <returns> Whether the property value is valid. </returns>
        protected abstract bool IsPropertyValid(SerializedProperty property);

        /// <summary>
        /// Gets whether a collection element passes validation.
        /// </summary>
        /// <param name="property"> The collection element being drawn. </param>
        /// <returns> Whether the element is valid. </returns>
        protected virtual bool IsElementValid(SerializedProperty property)
        {
            return true;
        }

        private Func<SerializedProperty, bool> GetElementValidation()
        {
            return SupportsElementValidation ? IsElementValid : null;
        }
    }
}
