using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
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
        private const float k_MinimumMessageHeight = 40.0f;

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
                Assert.IsNotNull(root.Q<PropertyField>());
            }
            finally
            {
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
    }
}
