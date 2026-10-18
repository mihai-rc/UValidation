using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UValidation.Editor
{
    /// <summary>
    /// Draws validated serialized properties with contextual messages above the field.
    /// </summary>
    internal static class FieldDrawerHelper
    {
        private const string k_ElementErrorIconName = "validation-element-error-icon";
        private const string k_ErrorIcon = "d_console.erroricon.sml";
        private const float k_MinimumMessageHeight = 40.0f;
        private const float k_MessageSpacing = 2.0f;
        private const string k_WarningMessage = "[{0}] is only valid on {1} fields. Field '{2}' has type '{3}'.";

        internal static VisualElement CreatePropertyGUI(
            SerializedProperty property,
            string attributeName,
            string allowedTypeDescription,
            string violationMessage,
            Func<SerializedProperty, bool> usageValidationFn,
            Func<SerializedProperty, bool> propertyValidationFn,
            Func<SerializedProperty, bool> elementValidationFn = null)
        {
            var container = new VisualElement();
            var helpBox = new HelpBox
            {
                style =
                {
                    minHeight = k_MinimumMessageHeight,
                    marginBottom = k_MessageSpacing
                }
            };
            var propertyField = new PropertyField(property);
            var targets = property.serializedObject.targetObjects;
            var propertyPath = property.propertyPath;

            container.style.flexGrow = 1.0f;
            container.style.alignSelf = Align.Stretch;
            container.style.width = Length.Percent(100.0f);
            helpBox.style.alignSelf = Align.Stretch;
            propertyField.style.flexGrow = 1.0f;
            propertyField.style.alignSelf = Align.Stretch;
            propertyField.style.width = Length.Percent(100.0f);
            container.Add(helpBox);
            container.Add(propertyField);

            RefreshMessage(helpBox,
                property,
                attributeName,
                allowedTypeDescription,
                violationMessage,
                usageValidationFn,
                propertyValidationFn);

            RefreshElementIcons(propertyField, property, violationMessage, elementValidationFn);

            container.TrackPropertyValue(property, changedProperty =>
            {
                RefreshMessage(helpBox,
                    changedProperty,
                    attributeName,
                    allowedTypeDescription,
                    violationMessage,
                    usageValidationFn,
                    propertyValidationFn);

                propertyField.schedule.Execute(() => RefreshElementIcons(
                    propertyField,
                    targets,
                    propertyPath,
                    violationMessage,
                    elementValidationFn));
            });

            propertyField.RegisterCallback<GeometryChangedEvent>(_ => RefreshElementIcons(
                propertyField,
                targets,
                propertyPath,
                violationMessage,
                elementValidationFn));

            return container;
        }

        internal static void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            string attributeName,
            string allowedTypeDescription,
            string violationMessage,
            Func<SerializedProperty, bool> usageValidationFn,
            Func<SerializedProperty, bool> propertyValidationFn,
            Func<SerializedProperty, bool> elementValidationFn = null)
        {
            EditorGUI.BeginProperty(position, label, property);
            var message = GetMessage(property,
                attributeName,
                allowedTypeDescription,
                violationMessage,
                usageValidationFn,
                propertyValidationFn,
                out var messageType);

            if (message != null)
            {
                var messageHeight = GetMessageHeight(position.width, message);
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width, messageHeight), message, messageType);
                position.y += messageHeight + k_MessageSpacing;
                position.height -= messageHeight + k_MessageSpacing;
            }

            EditorGUI.PropertyField(position, property, label, true);
            DrawElementIcons(position, property, violationMessage, elementValidationFn);
            EditorGUI.EndProperty();
        }

        internal static float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label,
            string attributeName,
            string allowedTypeDescription,
            string violationMessage,
            Func<SerializedProperty, bool> usageValidationFn,
            Func<SerializedProperty, bool> propertyValidationFn)
        {
            var propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
            var message = GetMessage(property,
                attributeName,
                allowedTypeDescription,
                violationMessage,
                usageValidationFn,
                propertyValidationFn,
                out _);

            return message == null
                ? propertyHeight
                : propertyHeight + GetMessageHeight(EditorGUIUtility.currentViewWidth, message) + k_MessageSpacing;
        }

        internal static bool IsCollection(SerializedProperty property)
        {
            return property.isArray && property.propertyType != SerializedPropertyType.String;
        }

        internal static bool IsCollectionElement(SerializedProperty property)
        {
            return property.propertyPath.Contains(".Array.data[") &&
                   property.propertyPath.EndsWith("]", StringComparison.Ordinal);
        }

        internal static bool HasNoNullObjectReferences(SerializedProperty property)
        {
            for (var i = 0; i < property.arraySize; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                if (element.propertyType != SerializedPropertyType.ObjectReference || element.objectReferenceValue == null)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool HasNoEmptyStrings(SerializedProperty property)
        {
            for (var i = 0; i < property.arraySize; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                if (element.propertyType != SerializedPropertyType.String || string.IsNullOrEmpty(element.stringValue))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Evaluates the same property independently on every selected target.
        /// </summary>
        /// <param name="property"> The multi-object property being drawn. </param>
        /// <param name="validationFn"> Validation to run on each target's property. </param>
        /// <returns> True when every selected target passes the validation. </returns>
        internal static bool EveryTargetPasses(SerializedProperty property, Func<SerializedProperty, bool> validationFn)
        {
            if (validationFn == null)
            {
                return true;
            }

            var serializedObject = property.serializedObject;
            if (!serializedObject.isEditingMultipleObjects)
            {
                return validationFn(property);
            }

            var propertyPath = property.propertyPath;
            foreach (var target in serializedObject.targetObjects)
            {
                if (target == null)
                {
                    continue;
                }

                using var targetObject = new SerializedObject(target);
                var targetProperty = targetObject.FindProperty(propertyPath);
                if (targetProperty == null || !validationFn(targetProperty))
                {
                    return false;
                }
            }

            return true;
        }

        private static void RefreshMessage(
            HelpBox helpBox,
            SerializedProperty property,
            string attributeName,
            string allowedTypeDescription,
            string violationMessage,
            Func<SerializedProperty, bool> usageValidationFn,
            Func<SerializedProperty, bool> propertyValidationFn)
        {
            var message = GetMessage(property,
                attributeName,
                allowedTypeDescription,
                violationMessage,
                usageValidationFn,
                propertyValidationFn,
                out var messageType);

            helpBox.style.display = message == null ? DisplayStyle.None : DisplayStyle.Flex;
            helpBox.text = message ?? string.Empty;
            helpBox.messageType = messageType == MessageType.Warning
                ? HelpBoxMessageType.Warning
                : HelpBoxMessageType.Error;
        }

        private static void RefreshElementIcons(
            PropertyField collectionField,
            UnityEngine.Object[] targets,
            string propertyPath,
            string violationMessage,
            Func<SerializedProperty, bool> elementValidationFn)
        {
            var validTargets = Array.FindAll(targets, target => target != null);
            if (validTargets.Length == 0)
            {
                return;
            }

            using var serializedObject = new SerializedObject(validTargets);
            var collection = serializedObject.FindProperty(propertyPath);
            if (collection != null)
            {
                RefreshElementIcons(collectionField, collection, violationMessage, elementValidationFn);
            }
        }

        private static void RefreshElementIcons(
            PropertyField collectionField,
            SerializedProperty collection,
            string violationMessage,
            Func<SerializedProperty, bool> elementValidationFn)
        {
            if (elementValidationFn == null || !IsCollection(collection))
            {
                return;
            }

            for (var i = 0; i < collection.arraySize; i++)
            {
                var element = collection.GetArrayElementAtIndex(i);

                var elementField = collectionField
                    .Query<PropertyField>()
                    .Where(field => !string.IsNullOrEmpty(field.bindingPath) &&
                        (field.bindingPath == element.propertyPath ||
                         element.propertyPath.EndsWith(field.bindingPath, StringComparison.Ordinal)))
                    .First();

                if (elementField == null)
                {
                    continue;
                }

                var icon = elementField.Q<Image>(k_ElementErrorIconName);
                var isValid = IsElementValidForEveryTarget(collection, i, elementValidationFn);
                if (isValid)
                {
                    icon?.RemoveFromHierarchy();
                    continue;
                }

                if (icon != null)
                {
                    continue;
                }

                icon = new Image
                {
                    name = k_ElementErrorIconName,
                    image = EditorGUIUtility.IconContent(k_ErrorIcon).image,
                    tooltip = violationMessage
                };

                icon.style.width = 16.0f;
                icon.style.height = 16.0f;
                icon.style.marginRight = 4.0f;
                icon.style.flexShrink = 0.0f;
                elementField.style.flexDirection = FlexDirection.Row;
                elementField.style.alignItems = Align.Center;
                elementField.style.flexGrow = 1.0f;
                elementField.style.alignSelf = Align.Stretch;
                elementField.style.width = Length.Percent(100.0f);

                foreach (var child in elementField.Children())
                {
                    child.style.flexGrow = 1.0f;
                    child.style.minWidth = 0.0f;
                }

                elementField.Insert(0, icon);
            }
        }

        private static void DrawElementIcons(
            Rect position,
            SerializedProperty collection,
            string violationMessage,
            Func<SerializedProperty, bool> elementValidationFn)
        {
            if (elementValidationFn == null || !collection.isExpanded || !IsCollection(collection))
            {
                return;
            }

            var elementY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            for (var i = 0; i < collection.arraySize; i++)
            {
                var element = collection.GetArrayElementAtIndex(i);
                if (!IsElementValidForEveryTarget(collection, i, elementValidationFn))
                {
                    var iconRect = new Rect(position.x + 14.0f, elementY, 16.0f, 16.0f);
                    GUI.Label(iconRect, new GUIContent(EditorGUIUtility.IconContent(k_ErrorIcon).image, violationMessage));
                }

                elementY += EditorGUI.GetPropertyHeight(element, true) + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private static string GetMessage(
            SerializedProperty property,
            string attributeName,
            string allowedTypeDescription,
            string violationMessage,
            Func<SerializedProperty, bool> usageValidationFn,
            Func<SerializedProperty, bool> propertyValidationFn,
            out MessageType messageType)
        {
            if (IsCollectionElement(property))
            {
                messageType = MessageType.None;
                return null;
            }

            if (!(usageValidationFn?.Invoke(property) ?? true))
            {
                messageType = MessageType.Warning;
                return string.Format(k_WarningMessage,
                    attributeName,
                    allowedTypeDescription,
                    property.displayName,
                    property.propertyType);
            }

            messageType = MessageType.Error;
            return EveryTargetPasses(property, propertyValidationFn) ? null : violationMessage;
        }

        private static bool IsElementValidForEveryTarget(
            SerializedProperty collection,
            int index,
            Func<SerializedProperty, bool> elementValidationFn)
        {
            return EveryTargetPasses(
                collection,
                targetCollection => index < targetCollection.arraySize
                    && elementValidationFn(targetCollection.GetArrayElementAtIndex(index)));
        }

        private static float GetMessageHeight(float width, string message)
        {
            return Mathf.Max(
                k_MinimumMessageHeight,
                EditorStyles.helpBox.CalcHeight(new GUIContent(message), width));
        }
    }
}
