using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UValidation.Showcase.Editor
{
    /// <summary>
    /// Builds the validation attribute showcase scene with deterministic example values.
    /// </summary>
    public static class ValidationShowcaseBuilder
    {
        private const string k_ValidationEnabledKey = "Framework.EnableValidation";
        private const string k_ValidationMenuPath = "HighTower/Enable Validation";
        private const string k_ShowcaseScenePath = "Assets/Scenes/ValidationAttributeShowcase.unity";

        /// <summary>
        /// Rebuilds and saves the validation attribute showcase scene.
        /// </summary>
        [MenuItem("HighTower/UValidation/Rebuild Showcase Scene")]
        public static void RebuildShowcaseScene()
        {
            var wasValidationEnabled = EditorPrefs.GetBool(k_ValidationEnabledKey, true);
            EditorPrefs.SetBool(k_ValidationEnabledKey, false);

            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var root = new GameObject("UValidation Attribute Showcase");
                CreateSection<ShowcaseOverview>(root.transform, "00 - Overview");

                var references = new GameObject("Shared References");
                references.transform.SetParent(root.transform);
                var referenceA = CreateReference(references.transform, "Assigned Reference A");
                var referenceB = CreateReference(references.transform, "Assigned Reference B");

                var notNull = CreateSection<NotNullShowcase>(root.transform, "01 - NotNull");
                SetObject(notNull, "m_AssignedGameObject", referenceA);

                var notEmpty = CreateSection<NotEmptyShowcase>(root.transform, "02 - NotEmpty");
                SetObjectArray(notEmpty, "m_PopulatedObjects", referenceA);

                var noNulls = CreateSection<HasNoNullsShowcase>(root.transform, "03 - HasNoNulls");
                SetObjectArray(noNulls, "m_AllAssigned", referenceA, referenceB);
                SetObjectArray(noNulls, "m_ContainsNull", referenceA, null);

                CreateSection<HasNoEmptiesShowcase>(root.transform, "04 - HasNoEmpties");

                var isValid = CreateSection<IsValidShowcase>(root.transform, "05 - IsValid");
                SetString(isValid, "m_ValidNested.m_Name", "Configured");
                SetObject(isValid, "m_ValidNested.m_Target", referenceA);
                SetInteger(isValid, "m_ValidRule.m_PositiveValue", 5);
                SetInteger(isValid, "m_InvalidRule.m_PositiveValue", -1);
                SetObject(isValid, "m_UnityObjectMisuse", referenceB);

                var combined = CreateSection<CombinedAttributesShowcase>(
                    root.transform, "06 - Combined Attributes");
                SetObjectArray(combined, "m_RequiredObjects", referenceA, referenceB);
                SetObjectArray(combined, "m_RequiredObjectsWithNull", referenceA, null);
                SetObject(combined, "m_RequiredNested.m_Target", referenceA);

                var customPass = CreateSection<CustomValidationShowcase>(
                    root.transform, "07 - IValidatable - Pass");
                SetInteger(customPass, "m_MinimumValue", 5);
                SetInteger(customPass, "m_CurrentValue", 5);

                var customFail = CreateSection<CustomValidationShowcase>(
                    root.transform, "08 - IValidatable - Fail");
                SetInteger(customFail, "m_MinimumValue", 5);
                SetInteger(customFail, "m_CurrentValue", 4);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, k_ShowcaseScenePath))
                {
                    throw new InvalidOperationException($"Could not save showcase scene at '{k_ShowcaseScenePath}'.");
                }

                Selection.activeGameObject = root.transform.Find("00 - Overview").gameObject;
                Debug.Log($"UValidation showcase rebuilt at '{k_ShowcaseScenePath}'.");
            }
            finally
            {
                EditorPrefs.SetBool(k_ValidationEnabledKey, wasValidationEnabled);
                Menu.SetChecked(k_ValidationMenuPath, wasValidationEnabled);
            }
        }

        private static T CreateSection<T>(Transform parent, string name) where T : Component
        {
            var section = new GameObject(name);
            section.transform.SetParent(parent);
            return section.AddComponent<T>();
        }

        private static GameObject CreateReference(Transform parent, string name)
        {
            var reference = new GameObject(name);
            reference.transform.SetParent(parent);
            reference.AddComponent<ShowcaseReference>();
            return reference;
        }

        private static void SetObject(Component component, string propertyPath, UnityEngine.Object value)
        {
            Modify(component, propertyPath, property => property.objectReferenceValue = value);
        }

        private static void SetString(Component component, string propertyPath, string value)
        {
            Modify(component, propertyPath, property => property.stringValue = value);
        }

        private static void SetInteger(Component component, string propertyPath, int value)
        {
            Modify(component, propertyPath, property => property.intValue = value);
        }

        private static void SetObjectArray(Component component, string propertyPath, params UnityEngine.Object[] values)
        {
            Modify(component, propertyPath, property =>
            {
                property.arraySize = values.Length;
                for (var i = 0; i < values.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            });
        }

        private static void Modify(Component component, string propertyPath, Action<SerializedProperty> mutation)
        {
            using var serializedObject = new SerializedObject(component);
            serializedObject.Update();
            var property = serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Property '{propertyPath}' was not found on '{component.GetType().Name}'.");
            }

            mutation(property);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
