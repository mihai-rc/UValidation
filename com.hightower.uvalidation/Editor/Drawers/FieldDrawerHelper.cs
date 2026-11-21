using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UValidation.Editor
{
    /// <summary>
    /// Draws validated serialized properties with contextual messages above the field.
    /// </summary>
    internal static class FieldDrawerHelper
    {
        /// <summary>
        /// Draws IMGUI reorderable-list elements with a validation icon beside invalid entries.
        /// </summary>
        internal sealed class ImguiCollectionElementDrawer
        {
            private readonly ReorderableList m_List;

            /// <summary>
            /// Creates a new <see cref="ImguiCollectionElementDrawer"/> instance.
            /// </summary>
            /// <param name="list"> The reorderable list whose elements are drawn. </param>
            internal ImguiCollectionElementDrawer(ReorderableList list)
            {
                m_List = list;
            }

            /// <summary>
            /// Gets or sets the tooltip shown on the validation icon of an invalid element.
            /// </summary>
            internal string ViolationMessage { get; set; }

            /// <summary>
            /// Gets or sets the rule that decides whether an element is valid.
            /// </summary>
            internal Func<SerializedProperty, bool> Validation { get; set; }

            /// <summary>
            /// Draws one list element, reserving space for a validation icon when the element is invalid.
            /// </summary>
            /// <param name="elementRect"> The content rectangle supplied by Unity's reorderable list. </param>
            /// <param name="index"> The index of the element being drawn. </param>
            /// <param name="isActive"> Whether the element is the active list selection. </param>
            /// <param name="isFocused"> Whether the element has keyboard focus. </param>
            internal void Draw(Rect elementRect, int index, bool isActive, bool isFocused)
            {
                var collection = m_List.serializedProperty;
                if (collection is null || index < 0 || index >= collection.arraySize)
                {
                    return;
                }

                var element = collection.GetArrayElementAtIndex(index);
                var isInvalid = !IsElementValidForEveryTarget(collection, index, Validation);
                var labelWidth = EditorGuiCompatibility.GetCollectionElementLabelWidth(
                    elementRect,
                    element);

                var fieldRect = GetCollectionElementFieldRect(
                    elementRect,
                    isInvalid,
                    out var iconRect);

                if (isInvalid)
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        var icon = EditorGUIUtility.IconContent(k_ErrorIcon).image;
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                    }

                    s_ElementTooltipContent.text = string.Empty;
                    s_ElementTooltipContent.tooltip = ViolationMessage;
                    GUI.Label(iconRect, s_ElementTooltipContent, GUIStyle.none);
                }

                var previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = labelWidth;
                try
                {
                    EditorGUI.PropertyField(fieldRect, element, true);
                }
                finally
                {
                    EditorGUIUtility.labelWidth = previousLabelWidth;
                }
            }
        }

        private sealed class DictionaryValueListBinding
        {
            private Action<VisualElement, int> m_OriginalBindItem;
            private Action m_Refresh;

            /// <summary>
            /// Wraps a list view's item binding so validation feedback refreshes after Unity binds each item.
            /// </summary>
            /// <param name="listView"> The list view whose item binding should trigger a refresh. </param>
            /// <param name="refresh"> The validation-feedback refresh action. </param>
            internal void Configure(ListView listView, Action refresh)
            {
                m_Refresh = refresh;
                Action<VisualElement, int> bindItem = BindItem;
                if (listView.bindItem == bindItem)
                {
                    return;
                }

                m_OriginalBindItem = listView.bindItem;
                listView.bindItem = bindItem;
            }

            private void BindItem(VisualElement element, int index)
            {
                m_OriginalBindItem?.Invoke(element, index);
                element.schedule.Execute(Refresh);
            }

            private void Refresh()
            {
                m_Refresh?.Invoke();
            }
        }

        private const string k_AttributeSuffix = "Attribute";
        private const string k_ElementErrorIconName = "validation-element-error-icon";
        private const string k_ErrorIcon = "d_console.erroricon.sml";
        private const float k_MessageSpacing = 2.0f;
        private const string k_WarningMessage = "[{0}] is only valid on {1} fields. Field '{2}' has type '{3}'.";

        private static readonly GUIContent s_ElementTooltipContent = new();
        private static readonly ConditionalWeakTable<ListView, DictionaryValueListBinding> s_DictionaryValueListBindings = new();

        /// <summary>
        /// Creates a UI Toolkit property field with contextual validation feedback.
        /// </summary>
        /// <param name="property"> The serialized property to draw. </param>
        /// <param name="attributeName"> The validation attribute's display name. </param>
        /// <param name="allowedTypeDescription"> A description of the field types supported by the attribute. </param>
        /// <param name="violationMessage"> The message shown when the property's value fails validation. </param>
        /// <param name="usageValidationFn"> The rule that determines whether the attribute supports the property. </param>
        /// <param name="propertyValidationFn"> The rule that validates the property value. </param>
        /// <param name="elementValidationFn"> The optional rule that validates individual collection elements. </param>
        /// <param name="validateDictionaryValues"> Whether dictionary values should be validated as collection elements. </param>
        /// <returns> A visual element containing the validation feedback and property field. </returns>
        internal static VisualElement CreatePropertyGUI(
            SerializedProperty property,
            string attributeName,
            string allowedTypeDescription,
            string violationMessage,
            Func<SerializedProperty, bool> usageValidationFn,
            Func<SerializedProperty, bool> propertyValidationFn,
            Func<SerializedProperty, bool> elementValidationFn = null,
            bool validateDictionaryValues = false)
        {
            var container = new VisualElement();
            var helpBox = new HelpBox
            {
                style =
                {
                    minHeight = ValidationMessageGui.MINIMUM_HEIGHT,
                    marginBottom = k_MessageSpacing
                }
            };

            var propertyField = new PropertyField(property);
            var targets = property.serializedObject.targetObjects;
            var propertyPath = property.propertyPath;

            elementValidationFn = GetApplicableElementValidation(
                property,
                usageValidationFn,
                elementValidationFn);

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

            ConfigureHelpBoxContent(helpBox);

            RefreshElementIcons(
                propertyField,
                property,
                violationMessage,
                elementValidationFn,
                validateDictionaryValues);

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
                    elementValidationFn,
                    validateDictionaryValues));
            });

            propertyField.RegisterCallback<GeometryChangedEvent>(_ => RefreshElementIcons(
                propertyField,
                targets,
                propertyPath,
                violationMessage,
                elementValidationFn,
                validateDictionaryValues));

            return container;
        }

        /// <summary>
        /// Draws an IMGUI property field with contextual validation feedback.
        /// </summary>
        /// <param name="position"> The rectangle in which to draw the property and its validation feedback. </param>
        /// <param name="property"> The serialized property to draw. </param>
        /// <param name="label"> The label shown for the property. </param>
        /// <param name="attributeName"> The validation attribute's display name. </param>
        /// <param name="allowedTypeDescription"> A description of the field types supported by the attribute. </param>
        /// <param name="violationMessage"> The message shown when the property's value fails validation. </param>
        /// <param name="usageValidationFn"> The rule that determines whether the attribute supports the property. </param>
        /// <param name="propertyValidationFn"> The rule that validates the property value. </param>
        /// <param name="elementValidationFn"> The optional rule that validates individual collection elements. </param>
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

            elementValidationFn = GetApplicableElementValidation(
                property,
                usageValidationFn,
                elementValidationFn);

            if (message is not null)
            {
                var propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
                var messageHeight = Mathf.Max(ValidationMessageGui.MINIMUM_HEIGHT, position.height - propertyHeight - k_MessageSpacing);

                ValidationMessageGui.DrawHelpBox(
                    new Rect(position.x, position.y, position.width, messageHeight),
                    message,
                    messageType);

                position.y += messageHeight + k_MessageSpacing;
                position.height = propertyHeight;
            }

            var listConfigured = ConfigureImguiCollectionElements(
                property,
                violationMessage,
                elementValidationFn);

            EditorGUI.PropertyField(position, property, label, true);
            if (!listConfigured)
            {
                ConfigureImguiCollectionElements(property, violationMessage, elementValidationFn);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Gets the total IMGUI height required for a property and its contextual validation feedback.
        /// </summary>
        /// <param name="property"> The serialized property to measure. </param>
        /// <param name="label"> The label shown for the property. </param>
        /// <param name="attributeName"> The validation attribute's display name. </param>
        /// <param name="allowedTypeDescription"> A description of the field types supported by the attribute. </param>
        /// <param name="violationMessage"> The message shown when the property's value fails validation. </param>
        /// <param name="usageValidationFn"> The rule that determines whether the attribute supports the property. </param>
        /// <param name="propertyValidationFn"> The rule that validates the property value. </param>
        /// <param name="elementValidationFn"> The optional rule that validates individual collection elements. </param>
        /// <returns> The required height in pixels. </returns>
        internal static float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label,
            string attributeName,
            string allowedTypeDescription,
            string violationMessage,
            Func<SerializedProperty, bool> usageValidationFn,
            Func<SerializedProperty, bool> propertyValidationFn,
            Func<SerializedProperty, bool> elementValidationFn = null)
        {
            var propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
            elementValidationFn = GetApplicableElementValidation(
                property,
                usageValidationFn,
                elementValidationFn);

            ConfigureImguiCollectionElements(property, violationMessage, elementValidationFn);

            var message = GetMessage(property,
                attributeName,
                allowedTypeDescription,
                violationMessage,
                usageValidationFn,
                propertyValidationFn,
                out _);

            return message is null
                ? propertyHeight
                : propertyHeight +
                  ValidationMessageGui.GetHelpBoxHeight(EditorGUIUtility.currentViewWidth, message) +
                  k_MessageSpacing;
        }

        /// <summary>
        /// Gets whether a serialized property represents a non-string collection.
        /// </summary>
        /// <param name="property"> The serialized property to inspect. </param>
        /// <returns> Whether the property is a collection supported by Unity's array interface. </returns>
        internal static bool IsCollection(SerializedProperty property)
        {
            return property.isArray && property.propertyType != SerializedPropertyType.String;
        }

        /// <summary>
        /// Gets whether a serialized property represents an element inside a collection.
        /// </summary>
        /// <param name="property"> The serialized property to inspect. </param>
        /// <returns> Whether the property's path identifies a collection element. </returns>
        internal static bool IsCollectionElement(SerializedProperty property)
        {
            return property.propertyPath.Contains(".Array.data[") &&
                   property.propertyPath.EndsWith("]", StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets whether every element in a serialized collection is an assigned object reference.
        /// </summary>
        /// <param name="property"> The serialized collection property. </param>
        /// <returns> Whether all elements are assigned object references. </returns>
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

        /// <summary>
        /// Gets whether every element in a serialized collection is a nonempty string.
        /// </summary>
        /// <param name="property"> The serialized collection property. </param>
        /// <returns> Whether all elements contain text. </returns>
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
        /// Gets whether a serialized dictionary contains at least one entry that Unity includes in the live
        /// <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="property"> The serialized dictionary property. </param>
        /// <returns> Whether at least one non-placeholder, non-duplicate entry exists. </returns>
        internal static bool HasEffectiveDictionaryEntries(SerializedProperty property)
        {
            return SerializedDictionaryAdapter.HasEffectiveEntries(property);
        }

        /// <summary>
        /// Gets whether every effective value in a serialized dictionary is an assigned object reference.
        /// </summary>
        /// <param name="property"> The serialized dictionary property. </param>
        /// <returns> Whether all effective values are assigned. </returns>
        internal static bool HasNoNullDictionaryValues(SerializedProperty property)
        {
            return SerializedDictionaryAdapter.EveryEffectiveValuePasses(
                property,
                value => value.propertyType == SerializedPropertyType.ObjectReference &&
                         value.objectReferenceValue != null);
        }

        /// <summary>
        /// Gets whether every effective string value in a serialized dictionary is nonempty.
        /// </summary>
        /// <param name="property"> The serialized dictionary property. </param>
        /// <returns> Whether all effective values contain text. </returns>
        internal static bool HasNoEmptyDictionaryValues(SerializedProperty property)
        {
            return SerializedDictionaryAdapter.EveryEffectiveValuePasses(
                property,
                value => value.propertyType == SerializedPropertyType.String &&
                         !string.IsNullOrEmpty(value.stringValue));
        }

        /// <summary>
        /// Evaluates the same property independently on every selected target.
        /// </summary>
        /// <param name="property"> The multi-object property being drawn. </param>
        /// <param name="validationFn"> Validation to run on each target's property. </param>
        /// <returns> True when every selected target passes the validation. </returns>
        internal static bool EveryTargetPasses(SerializedProperty property, Func<SerializedProperty, bool> validationFn)
        {
            if (validationFn is null)
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
                if (targetProperty is null || !validationFn(targetProperty))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the field and optional validation-icon rectangles for a collection element row.
        /// </summary>
        /// <param name="elementRect"> The content rectangle supplied by Unity's reorderable list. </param>
        /// <param name="hasError"> Whether the element needs a validation icon. </param>
        /// <param name="iconRect"> The rectangle reserved for the validation icon. </param>
        /// <returns> The rectangle in which to draw the element property. </returns>
        internal static Rect GetCollectionElementFieldRect(
            Rect elementRect,
            bool hasError,
            out Rect iconRect)
        {
            const float iconSize = 16.0f;
            const float iconSpacing = 4.0f;

            elementRect.y += 1.0f;
            var firstLineHeight = Mathf.Min(EditorGUIUtility.singleLineHeight, elementRect.height);

            iconRect = new Rect(
                elementRect.x,
                elementRect.y + Mathf.Max(0.0f, (firstLineHeight - iconSize) * 0.5f),
                iconSize,
                iconSize);

            if (hasError)
            {
                elementRect.xMin += iconSize + iconSpacing;
            }

            return elementRect;
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

            helpBox.style.display = message is null ? DisplayStyle.None : DisplayStyle.Flex;
            helpBox.text = message ?? string.Empty;
            helpBox.messageType = messageType == MessageType.Warning
                ? HelpBoxMessageType.Warning
                : HelpBoxMessageType.Error;
        }

        private static void ConfigureHelpBoxContent(HelpBox helpBox)
        {
            helpBox.style.alignItems = Align.Center;
            helpBox.style.width = Length.Percent(100.0f);
            helpBox.style.marginLeft = 0.0f;
            helpBox.style.marginRight = 0.0f;
            helpBox.style.paddingTop = ValidationMessageGui.VERTICAL_PADDING;
            helpBox.style.paddingBottom = ValidationMessageGui.VERTICAL_PADDING;

            var icon = helpBox.Q(className: HelpBox.iconUssClassName);
            if (icon is not null)
            {
                icon.style.width = ValidationMessageGui.ICON_SIZE;
                icon.style.height = ValidationMessageGui.ICON_SIZE;
                icon.style.flexShrink = 0.0f;
                icon.style.alignSelf = Align.Center;
            }

            var label = helpBox.Q(className: HelpBox.labelUssClassName);
            if (label is not null)
            {
                label.style.alignSelf = Align.Center;
                label.style.fontSize = ValidationMessageGui.FONT_SIZE;
            }
        }

        private static void RefreshElementIcons(
            PropertyField collectionField,
            UnityEngine.Object[] targets,
            string propertyPath,
            string violationMessage,
            Func<SerializedProperty, bool> elementValidationFn,
            bool validateDictionaryValues)
        {
            if (validateDictionaryValues && elementValidationFn is not null)
            {
                ConfigureDictionaryValueListBinding(
                    collectionField,
                    () => RefreshElementIcons(
                        collectionField,
                        targets,
                        propertyPath,
                        violationMessage,
                        elementValidationFn,
                        validateDictionaryValues));
            }

            var validTargets = Array.FindAll(targets, target => target != null);
            if (validTargets.Length == 0)
            {
                return;
            }

            using var serializedObject = new SerializedObject(validTargets);
            var collection = serializedObject.FindProperty(propertyPath);
            if (collection is not null)
            {
                RefreshElementIcons(
                    collectionField,
                    collection,
                    violationMessage,
                    elementValidationFn,
                    validateDictionaryValues);
            }
        }

        private static void RefreshElementIcons(
            PropertyField collectionField,
            SerializedProperty collection,
            string violationMessage,
            Func<SerializedProperty, bool> elementValidationFn,
            bool validateDictionaryValues)
        {
            if (elementValidationFn is null || !IsCollection(collection))
            {
                return;
            }

            if (validateDictionaryValues)
            {
                RefreshDictionaryValueIcons(
                    collectionField,
                    collection,
                    violationMessage,
                    elementValidationFn);

                return;
            }

            for (var i = 0; i < collection.arraySize; i++)
            {
                var element = collection.GetArrayElementAtIndex(i);
                var elementField = FindBoundPropertyField(collectionField, element.propertyPath);

                if (elementField is null)
                {
                    continue;
                }

                var isValid = IsElementValidForEveryTarget(collection, i, elementValidationFn);
                SetElementErrorIcon(elementField, !isValid, violationMessage);
            }
        }

        private static void ConfigureDictionaryValueListBinding(
            PropertyField collectionField,
            Action refresh)
        {
            var listView = collectionField.Q<ListView>();
            if (listView is null)
            {
                return;
            }

            var binding = s_DictionaryValueListBindings
                .GetValue(listView, _ => new DictionaryValueListBinding());

            binding.Configure(listView, refresh);
        }

        private static void RefreshDictionaryValueIcons(
            PropertyField collectionField,
            SerializedProperty dictionary,
            string violationMessage,
            Func<SerializedProperty, bool> valueValidationFn)
        {
            var effectiveValuePaths = HashSetPool<string>.Get();
            var invalidValuePaths = HashSetPool<string>.Get();
            try
            {
                using var values = SerializedDictionaryAdapter.EnumerateEffectiveValues(dictionary);
                while (values.MoveNext(out var value, out var entryIndex))
                {
                    if (value is null)
                    {
                        continue;
                    }

                    effectiveValuePaths.Add(value.propertyPath);
                    if (!IsDictionaryValueValidForEveryTarget(dictionary, entryIndex, valueValidationFn))
                    {
                        invalidValuePaths.Add(value.propertyPath);
                    }
                }

                collectionField.Query<PropertyField>().ForEach(field =>
                {
                    if (string.IsNullOrEmpty(field.bindingPath))
                    {
                        return;
                    }

                    var isEffectiveValue = ContainsMatchingPath(effectiveValuePaths, field.bindingPath);
                    var isInvalid = isEffectiveValue && ContainsMatchingPath(invalidValuePaths, field.bindingPath);
                    var icon = field.Q<Image>(k_ElementErrorIconName);
                    if (isEffectiveValue || icon?.parent == field)
                    {
                        SetElementErrorIcon(field, isInvalid, violationMessage);
                    }
                });
            }
            finally
            {
                HashSetPool<string>.Release(invalidValuePaths);
                HashSetPool<string>.Release(effectiveValuePaths);
            }
        }

        private static PropertyField FindBoundPropertyField(PropertyField collectionField, string propertyPath)
        {
            return collectionField
                .Query<PropertyField>()
                .Where(field => PathsMatch(propertyPath, field.bindingPath))
                .First();
        }

        private static bool ContainsMatchingPath(HashSet<string> propertyPaths, string bindingPath)
        {
            foreach (var propertyPath in propertyPaths)
            {
                if (PathsMatch(propertyPath, bindingPath))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PathsMatch(string propertyPath, string bindingPath)
        {
            return !string.IsNullOrEmpty(propertyPath)
                && !string.IsNullOrEmpty(bindingPath)
                && (bindingPath == propertyPath
                    || propertyPath.EndsWith(bindingPath, StringComparison.Ordinal));
        }

        private static void SetElementErrorIcon(
            PropertyField elementField,
            bool hasError,
            string violationMessage)
        {
            var icon = elementField.Q<Image>(k_ElementErrorIconName);
            if (icon?.parent != elementField)
            {
                icon = null;
            }

            if (!hasError)
            {
                icon?.RemoveFromHierarchy();
                return;
            }

            ConfigureElementFieldLayout(elementField);
            if (icon is not null)
            {
                icon.tooltip = violationMessage;
                return;
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
            elementField.Insert(0, icon);
        }

        private static void ConfigureElementFieldLayout(PropertyField elementField)
        {
            elementField.style.flexDirection = FlexDirection.Row;
            elementField.style.alignItems = Align.Center;
            elementField.style.flexGrow = 1.0f;
            elementField.style.flexShrink = 1.0f;
            elementField.style.alignSelf = Align.Stretch;
            elementField.style.width = Length.Percent(100.0f);
            elementField.style.minWidth = 0.0f;

            foreach (var child in elementField.Children())
            {
                if (child.name == k_ElementErrorIconName)
                {
                    continue;
                }

                child.style.flexGrow = 1.0f;
                child.style.flexShrink = 1.0f;
                child.style.minWidth = 0.0f;
            }
        }

        private static bool ConfigureImguiCollectionElements(
            SerializedProperty collection,
            string violationMessage,
            Func<SerializedProperty, bool> elementValidationFn)
        {
            if (!IsCollection(collection))
            {
                return false;
            }

            var list = ReorderableList.GetReorderableListFromSerializedProperty(collection);
            if (list is null)
            {
                return false;
            }

            if (elementValidationFn is null)
            {
                if (list.drawElementCallback?.Target is ImguiCollectionElementDrawer)
                {
                    list.drawElementCallback = null;
                }

                return true;
            }

            if (list.drawElementCallback?.Target is ImguiCollectionElementDrawer drawer)
            {
                drawer.ViolationMessage = violationMessage;
                drawer.Validation = elementValidationFn;
                return true;
            }

            drawer = new ImguiCollectionElementDrawer(list)
            {
                ViolationMessage = violationMessage,
                Validation = elementValidationFn
            };
            list.drawElementCallback = drawer.Draw;

            return true;
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
                    GetAttributeDisplayName(attributeName),
                    allowedTypeDescription,
                    property.displayName,
                    property.propertyType);
            }

            messageType = MessageType.Error;
            return EveryTargetPasses(property, propertyValidationFn) ? null : violationMessage;
        }

        private static string GetAttributeDisplayName(string attributeName)
        {
            return attributeName.EndsWith(k_AttributeSuffix, StringComparison.Ordinal)
                ? attributeName.Substring(0, attributeName.Length - k_AttributeSuffix.Length)
                : attributeName;
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

        private static bool IsDictionaryValueValidForEveryTarget(
            SerializedProperty dictionary,
            int entryIndex,
            Func<SerializedProperty, bool> valueValidationFn)
        {
            return EveryTargetPasses(dictionary, targetDictionary =>
            {
                using var values = SerializedDictionaryAdapter.EnumerateEffectiveValues(targetDictionary);
                while (values.MoveNext(out var value, out var targetEntryIndex))
                {
                    if (targetEntryIndex == entryIndex)
                    {
                        return value is not null && valueValidationFn(value);
                    }
                }

                return false;
            });
        }

        private static Func<SerializedProperty, bool> GetApplicableElementValidation(
            SerializedProperty property,
            Func<SerializedProperty, bool> usageValidationFn,
            Func<SerializedProperty, bool> elementValidationFn)
        {
            return elementValidationFn is not null && (usageValidationFn?.Invoke(property) ?? true)
                ? elementValidationFn
                : null;
        }
    }
}
