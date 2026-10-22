using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UValidation.Editor;

namespace UValidation.Tests
{
    public class DrawerRenderingTests
    {
        private const string k_ValidValue = "Configured";
        private const string k_ViolationMessage = "The value must not be empty.";
        private const float k_MessageIconSize = 32.0f;
        private const float k_MinimumMessageHeight = 48.0f;

        private static readonly Type[] s_RegisteredDrawerTypes =
        {
            typeof(NotNullFieldDrawer),
            typeof(NotEmptyFieldDrawer),
            typeof(HasNoNullsFieldDrawer),
            typeof(HasNoEmptiesFieldDrawer),
            typeof(IsValidFieldDrawer)
        };

        [Test]
        public void UiToolkit_InvalidProperty_CreatesVisibleErrorAndPropertyField()
        {
            var target = CreateProbe(string.Empty);
            try
            {
                using var serializedObject = new SerializedObject(target);
                var root = CreateUiToolkitDrawer(serializedObject.FindProperty("m_Value"));
                var helpBox = root.Q<HelpBox>();

                Assert.IsNotNull(helpBox);
                Assert.AreEqual(DisplayStyle.Flex, helpBox.style.display.value);
                Assert.AreEqual(HelpBoxMessageType.Error, helpBox.messageType);
                Assert.AreEqual(k_ViolationMessage, helpBox.text);
                Assert.GreaterOrEqual(helpBox.style.minHeight.value.value, k_MinimumMessageHeight);
                Assert.AreEqual(Align.Center, helpBox.style.alignItems.value);

                var icon = helpBox.Q(className: HelpBox.iconUssClassName);
                Assert.IsNotNull(icon);
                Assert.AreEqual(k_MessageIconSize, icon.style.width.value.value);
                Assert.AreEqual(k_MessageIconSize, icon.style.height.value.value);
                Assert.AreEqual(Align.Center, icon.style.alignSelf.value);
                Assert.IsNotNull(root.Q<PropertyField>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [UnityTest]
        public IEnumerator UiToolkit_InvalidProperty_CentersIconAtReadableSize()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("UI Toolkit EditorWindow rendering requires a graphics device.");
            }

            var target = CreateProbe(string.Empty);
            var window = ScriptableObject.CreateInstance<UiToolkitTestWindow>();
            try
            {
                using var serializedObject = new SerializedObject(target);
                var root = CreateUiToolkitDrawer(serializedObject.FindProperty("m_Value"));
                var helpBox = root.Q<HelpBox>();
                var icon = helpBox.Q(className: HelpBox.iconUssClassName);

                window.Configure(root);
                window.position = new Rect(0.0f, 0.0f, 360.0f, 240.0f);
                window.Show();

                for (var frame = 0;
                     frame < 10 &&
                     (float.IsNaN(helpBox.resolvedStyle.height) || helpBox.resolvedStyle.height <= 0.0f);
                     frame++)
                {
                    window.Repaint();
                    yield return null;
                }

                Assert.GreaterOrEqual(helpBox.resolvedStyle.height, k_MinimumMessageHeight);
                Assert.GreaterOrEqual(icon.resolvedStyle.width, k_MessageIconSize);
                Assert.GreaterOrEqual(icon.resolvedStyle.height, k_MessageIconSize);

                var iconCenter = icon.worldBound.center.y;
                var rowCenter = helpBox.worldBound.center.y;
                Assert.AreEqual(rowCenter, iconCenter, 0.5f);
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void UiToolkit_ValidProperty_HidesError()
        {
            var target = CreateProbe(k_ValidValue);
            try
            {
                using var serializedObject = new SerializedObject(target);
                var root = CreateUiToolkitDrawer(serializedObject.FindProperty("m_Value"));
                var helpBox = root.Q<HelpBox>();

                Assert.IsNotNull(helpBox);
                Assert.AreEqual(DisplayStyle.None, helpBox.style.display.value);
                Assert.IsNotNull(root.Q<PropertyField>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [UnityTest]
        public IEnumerator Imgui_InvalidProperty_AllocatesAdditionalMessageHeight()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("IMGUI EditorWindow rendering requires a graphics device.");
            }

            var target = CreateProbe(string.Empty);
            var window = ScriptableObject.CreateInstance<ImguiTestWindow>();
            try
            {
                using var serializedObject = new SerializedObject(target);
                var property = serializedObject.FindProperty("m_Value");
                var invalidHeight = 0.0f;
                var validHeight = 0.0f;
                var hasRendered = false;
                Exception renderingException = null;

                window.Configure(() =>
                {
                    if (hasRendered)
                    {
                        return;
                    }

                    try
                    {
                        serializedObject.Update();
                        invalidHeight = EditorGUI.GetPropertyHeight(property, GUIContent.none, true);

                        target.SetValue(k_ValidValue);
                        serializedObject.Update();
                        validHeight = EditorGUI.GetPropertyHeight(property, GUIContent.none, true);
                    }
                    catch (Exception exception)
                    {
                        renderingException = exception;
                    }
                    finally
                    {
                        hasRendered = true;
                    }
                });
                window.Show();

                for (var frame = 0; frame < 10 && !hasRendered; frame++)
                {
                    window.Repaint();
                    yield return null;
                }

                Assert.IsTrue(hasRendered, "The IMGUIContainer did not receive an OnGUI event.");
                Assert.IsNull(renderingException);
                Assert.GreaterOrEqual(
                    invalidHeight - validHeight,
                    k_MinimumMessageHeight,
                    "The validation message should receive a readable minimum height.");
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Imgui_MessageContent_UsesReadableContainedGeometry()
        {
            var helpBoxRect = new Rect(10.0f, 20.0f, 320.0f, ValidationMessageGui.MinimumHeight);

            ValidationMessageGui.GetContentRects(helpBoxRect, out var iconRect, out var textRect);

            Assert.AreEqual(12, ValidationMessageGui.FontSize);
            Assert.AreEqual(32.0f, iconRect.width);
            Assert.AreEqual(32.0f, iconRect.height);
            Assert.GreaterOrEqual(iconRect.xMin, helpBoxRect.xMin);
            Assert.LessOrEqual(iconRect.xMax, helpBoxRect.xMax);
            Assert.GreaterOrEqual(iconRect.yMin, helpBoxRect.yMin);
            Assert.LessOrEqual(iconRect.yMax, helpBoxRect.yMax);
            Assert.Greater(textRect.xMin, iconRect.xMax);
            Assert.LessOrEqual(textRect.xMax, helpBoxRect.xMax);
        }

        [Test]
        public void Imgui_InvalidCollectionElement_ReservesPrefixSlot()
        {
            var elementRect = new Rect(30.0f, 40.0f, 300.0f, EditorGUIUtility.singleLineHeight);

            var validFieldRect = FieldDrawerHelper.GetCollectionElementFieldRect(
                elementRect,
                false,
                out _);
            var invalidFieldRect = FieldDrawerHelper.GetCollectionElementFieldRect(
                elementRect,
                true,
                out var iconRect);

            Assert.AreEqual(elementRect.x, validFieldRect.x, 0.1f);
            Assert.AreEqual(elementRect.x + 20.0f, invalidFieldRect.x, 0.1f);
            Assert.AreEqual(elementRect.xMax, invalidFieldRect.xMax, 0.1f);
            Assert.AreEqual(elementRect.x, iconRect.x, 0.1f);
            Assert.AreEqual(16.0f, iconRect.width, 0.1f);
            Assert.AreEqual(16.0f, iconRect.height, 0.1f);
            Assert.AreEqual(4.0f, invalidFieldRect.x - iconRect.xMax, 0.1f);
            Assert.AreEqual(
                validFieldRect.y + EditorGUIUtility.singleLineHeight * 0.5f,
                iconRect.center.y,
                0.1f);
        }

        [Test]
        public void Imgui_CollectionElementLabelWidth_UsesUnmodifiedRow()
        {
            var elementRect = new Rect(30.0f, 40.0f, 300.0f, EditorGUIUtility.singleLineHeight);
            var target = ScriptableObject.CreateInstance<StringCollectionProbe>();
            try
            {
                using var serializedObject = new SerializedObject(target);
                var collection = serializedObject.FindProperty("m_Values");
                var element = collection.GetArrayElementAtIndex(0);
                var fieldRect = FieldDrawerHelper.GetCollectionElementFieldRect(
                    elementRect,
                    true,
                    out _);
                var labelWidth = EditorGuiCompatibility.GetCollectionElementLabelWidth(
                    elementRect,
                    element);
                var shiftedLabelWidth = EditorGuiCompatibility.GetCollectionElementLabelWidth(
                    fieldRect,
                    element);

                Assert.AreEqual(100.0f, labelWidth, 0.1f);
                Assert.Less(shiftedLabelWidth, labelWidth);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [UnityTest]
        public IEnumerator UiToolkit_UnsupportedCollection_DoesNotValidateElements()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("UI Toolkit EditorWindow rendering requires a graphics device.");
            }

            var target = ScriptableObject.CreateInstance<StringCollectionProbe>();
            var window = ScriptableObject.CreateInstance<UiToolkitTestWindow>();
            try
            {
                using var serializedObject = new SerializedObject(target);
                var property = serializedObject.FindProperty("m_Values");
                property.isExpanded = true;
                var elementValidationAttempted = false;
                var root = CreateUnsupportedCollectionDrawer(property, () => elementValidationAttempted = true);
                root.Bind(serializedObject);

                window.Configure(root);
                window.position = new Rect(0.0f, 0.0f, 360.0f, 300.0f);
                window.Show();

                for (var frame = 0; frame < 5; frame++)
                {
                    window.Repaint();
                    yield return null;
                }

                Assert.IsFalse(elementValidationAttempted);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [UnityTest]
        public IEnumerator Imgui_UnsupportedCollection_DoesNotValidateElements()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("IMGUI EditorWindow rendering requires a graphics device.");
            }

            var target = ScriptableObject.CreateInstance<StringCollectionProbe>();
            var window = ScriptableObject.CreateInstance<ImguiTestWindow>();
            try
            {
                using var serializedObject = new SerializedObject(target);
                var property = serializedObject.FindProperty("m_Values");
                property.isExpanded = true;
                var elementValidationAttempted = false;
                var hasRendered = false;

                window.Configure(() =>
                {
                    serializedObject.UpdateIfRequiredOrScript();
                    var height = FieldDrawerHelper.GetPropertyHeight(
                        property,
                        GUIContent.none,
                        nameof(HasNoNullsAttribute),
                        "collections of UnityEngine.Object references",
                        "The collection must not contain null object references.",
                        _ => false,
                        _ => false);
                    var rect = EditorGUILayout.GetControlRect(false, height);
                    FieldDrawerHelper.OnGUI(
                        rect,
                        property,
                        GUIContent.none,
                        nameof(HasNoNullsAttribute),
                        "collections of UnityEngine.Object references",
                        "The collection must not contain null object references.",
                        _ => false,
                        _ => false,
                        element =>
                        {
                            elementValidationAttempted = true;
                            return element.objectReferenceValue != null;
                        });
                    hasRendered = true;
                });
                window.Show();

                for (var frame = 0; frame < 5 && !hasRendered; frame++)
                {
                    window.Repaint();
                    yield return null;
                }

                Assert.IsTrue(hasRendered);
                Assert.IsFalse(elementValidationAttempted);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [UnityTest]
        public IEnumerator Imgui_SupportedCollection_InstallsElementDrawerOnUnityList()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("IMGUI EditorWindow rendering requires a graphics device.");
            }

            var target = ScriptableObject.CreateInstance<ObjectCollectionProbe>();
            var window = ScriptableObject.CreateInstance<ImguiTestWindow>();
            try
            {
                using var serializedObject = new SerializedObject(target);
                var property = serializedObject.FindProperty("m_Values");
                property.isExpanded = true;
                var hasRendered = false;
                ReorderableList list = null;

                window.Configure(() =>
                {
                    serializedObject.UpdateIfRequiredOrScript();
                    var height = FieldDrawerHelper.GetPropertyHeight(
                        property,
                        GUIContent.none,
                        nameof(HasNoNullsAttribute),
                        "collections of UnityEngine.Object references",
                        "The collection must not contain null object references.",
                        _ => true,
                        _ => false,
                        element => element.objectReferenceValue != null);
                    var rect = EditorGUILayout.GetControlRect(false, height);
                    FieldDrawerHelper.OnGUI(
                        rect,
                        property,
                        GUIContent.none,
                        nameof(HasNoNullsAttribute),
                        "collections of UnityEngine.Object references",
                        "The collection must not contain null object references.",
                        _ => true,
                        _ => false,
                        element => element.objectReferenceValue != null);
                    list = ReorderableList.GetReorderableListFromSerializedProperty(property);
                    hasRendered = true;
                });
                window.Show();

                for (var frame = 0; frame < 5 && !hasRendered; frame++)
                {
                    window.Repaint();
                    yield return null;
                }

                Assert.IsTrue(hasRendered);
                Assert.IsNotNull(list);
                Assert.IsNotNull(list.drawElementCallback);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void RenderingContract_EveryDrawerUsesSharedDualPathBase()
        {
            const BindingFlags declaredPublicInstance =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

            Assert.IsNotNull(typeof(ValidationFieldDrawer).GetMethod(
                nameof(PropertyDrawer.CreatePropertyGUI),
                declaredPublicInstance));

            Assert.IsNotNull(typeof(ValidationFieldDrawer).GetMethod(
                nameof(PropertyDrawer.OnGUI),
                declaredPublicInstance));

            Assert.IsNotNull(typeof(ValidationFieldDrawer).GetMethod(
                nameof(PropertyDrawer.GetPropertyHeight),
                declaredPublicInstance));

            foreach (var drawerType in s_RegisteredDrawerTypes)
            {
                Assert.IsTrue(drawerType.IsSubclassOf(typeof(ValidationFieldDrawer)), drawerType.Name);
                Assert.IsNotNull(drawerType.GetCustomAttribute<CustomPropertyDrawer>(), drawerType.Name);
            }
        }

        private static ScriptableObjectValidationProbe CreateProbe(string value)
        {
            var target = ScriptableObject.CreateInstance<ScriptableObjectValidationProbe>();
            target.SetValue(value);
            return target;
        }

        private static VisualElement CreateUiToolkitDrawer(SerializedProperty property)
        {
            return FieldDrawerHelper.CreatePropertyGUI(
                property,
                nameof(NotEmptyAttribute),
                "strings or collections",
                k_ViolationMessage,
                _ => true,
                IsNotEmpty);
        }

        private static VisualElement CreateUnsupportedCollectionDrawer(
            SerializedProperty property,
            Action handleElementValidation)
        {
            return FieldDrawerHelper.CreatePropertyGUI(
                property,
                nameof(HasNoNullsAttribute),
                "collections of UnityEngine.Object references",
                "The collection must not contain null object references.",
                _ => false,
                _ => false,
                element =>
                {
                    handleElementValidation();
                    return element.objectReferenceValue != null;
                });
        }

        private static bool IsNotEmpty(SerializedProperty property)
        {
            return !string.IsNullOrEmpty(property.stringValue);
        }

        private sealed class ImguiTestWindow : EditorWindow
        {
            private Action m_DrawAction;

            /// <summary>
            /// Adds an IMGUI host for the configured test action.
            /// </summary>
            public void CreateGUI()
            {
                rootVisualElement.Add(new IMGUIContainer(() => m_DrawAction?.Invoke()));
            }

            /// <summary>
            /// Sets the action invoked by the IMGUI host.
            /// </summary>
            /// <param name="drawAction"> Action to invoke during an IMGUI event. </param>
            internal void Configure(Action drawAction)
            {
                m_DrawAction = drawAction;
            }
        }

        private sealed class UiToolkitTestWindow : EditorWindow
        {
            /// <summary>
            /// Adds the content whose resolved layout is under test.
            /// </summary>
            /// <param name="content"> The content to display. </param>
            internal void Configure(VisualElement content)
            {
                rootVisualElement.Add(content);
            }
        }

        private sealed class StringCollectionProbe : ScriptableObject
        {
            [SerializeField] private List<string> m_Values = new() { "Configured", null };
        }

        private sealed class ObjectCollectionProbe : ScriptableObject
        {
            [SerializeField] private GameObject[] m_Values = { null };
        }
    }
}
