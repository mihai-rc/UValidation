#if UNITY_6000_6_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UValidation.Showcase.Editor
{
    /// <summary>
    /// Builds the Unity 6.6 serialized-dictionary validation showcase.
    /// </summary>
    public static class DictionaryShowcaseBuilder
    {
        private const string k_ScenePath = "Assets/Scenes/ValidationDictionaryShowcase.unity";

        /// <summary>
        /// Rebuilds the dictionary showcase without replacing the open scenes.
        /// </summary>
        [MenuItem("UValidation/Rebuild Dictionary Showcase")]
        public static void RebuildShowcaseScene()
        {
            BuildScene(k_ScenePath);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(k_ScenePath));
        }

        /// <summary>
        /// Writes the dictionary showcase at the requested scene path.
        /// </summary>
        /// <param name="scenePath"> Asset path at which to save the scene. </param>
        public static void BuildScene(string scenePath)
        {
            var existingScene = SceneManager.GetSceneByPath(scenePath);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                throw new InvalidOperationException("Close the dictionary showcase scene before rebuilding it.");
            }

            var previousScene = SceneManager.GetActiveScene();
            var scene = default(Scene);

            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                var root = new GameObject("UValidation Dictionary Showcase");
                SceneManager.MoveGameObjectToScene(root, scene);

                CreateSection<DictionaryShowcaseOverview>(root.transform, "00 - Overview");

                var references = new GameObject("Shared References");
                SceneManager.MoveGameObjectToScene(references, scene);
                references.transform.SetParent(root.transform);
                var referenceA = CreateReference(references.transform, "Assigned Reference A");
                var referenceB = CreateReference(references.transform, "Assigned Reference B");

                var notEmpty = CreateSection<DictionaryNotEmptyShowcase>(root.transform, "01 - NotEmpty");
                SetDictionary(notEmpty, "m_Populated", 2, (entry, index) =>
                {
                    GetRelative(entry, "key").stringValue = index == 0 ? "First" : "Second";
                    GetRelative(entry, "value").intValue = index + 1;
                });
                SetDictionary(notEmpty, "m_NullKeyPlaceholders", 2, (entry, index) =>
                {
                    GetRelative(entry, "key").objectReferenceValue = null;
                    GetRelative(entry, "value").stringValue = index == 0 ? "Preserved A" : "Preserved B";
                });

                var noNulls = CreateSection<DictionaryHasNoNullsShowcase>(root.transform, "02 - HasNoNulls");
                SetStringObjectDictionary(noNulls, "m_AllAssigned", referenceA, referenceB);
                SetStringObjectDictionary(noNulls, "m_ContainsNull", referenceA, null);

                var noEmpties = CreateSection<DictionaryHasNoEmptiesShowcase>(root.transform, "03 - HasNoEmpties");
                SetIntegerStringDictionary(noEmpties, "m_AllPopulated", "Alpha", "Beta");
                SetIntegerStringDictionary(noEmpties, "m_ContainsEmpty", "Alpha", string.Empty);
                SetIntegerStringDictionary(noEmpties, "m_ContainsNull", "Alpha", null);

                var isValid = CreateSection<DictionaryIsValidShowcase>(root.transform, "04 - IsValid");
                SetNestedDictionary(isValid, "m_ValidEntries", "Configured", referenceA);
                SetNestedDictionary(isValid, "m_InvalidEntries", string.Empty, null);
                SetDictionary(isValid, "m_PrimitiveValueMisuse", 1, (entry, _) =>
                {
                    GetRelative(entry, "key").stringValue = "Primitive";
                    GetRelative(entry, "value").intValue = 1;
                });

                if (!EditorSceneManager.SaveScene(scene, scenePath))
                {
                    throw new InvalidOperationException($"Could not save dictionary showcase at '{scenePath}'.");
                }
            }
            finally
            {
                if (scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (previousScene.IsValid() && previousScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousScene);
                }
            }
        }

        private static T CreateSection<T>(Transform parent, string name) where T : Component
        {
            var section = new GameObject(name);
            SceneManager.MoveGameObjectToScene(section, parent.gameObject.scene);
            section.transform.SetParent(parent);
            return section.AddComponent<T>();
        }

        private static GameObject CreateReference(Transform parent, string name)
        {
            var reference = new GameObject(name);
            SceneManager.MoveGameObjectToScene(reference, parent.gameObject.scene);
            reference.transform.SetParent(parent);
            reference.AddComponent<ShowcaseReference>();
            return reference;
        }

        private static void SetStringObjectDictionary(
            Component component,
            string propertyPath,
            params GameObject[] values)
        {
            SetDictionary(component, propertyPath, values.Length, (entry, index) =>
            {
                GetRelative(entry, "key").stringValue = $"Entry {index + 1}";
                GetRelative(entry, "value").objectReferenceValue = values[index];
            });
        }

        private static void SetIntegerStringDictionary(
            Component component,
            string propertyPath,
            params string[] values)
        {
            SetDictionary(component, propertyPath, values.Length, (entry, index) =>
            {
                GetRelative(entry, "key").intValue = index + 1;
                GetRelative(entry, "value").stringValue = values[index];
            });
        }

        private static void SetNestedDictionary(
            Component component,
            string propertyPath,
            string name,
            GameObject target)
        {
            SetDictionary(component, propertyPath, 1, (entry, _) =>
            {
                GetRelative(entry, "key").stringValue = "Primary";
                var value = GetRelative(entry, "value");
                GetRelative(value, "m_Name").stringValue = name;
                GetRelative(value, "m_Target").objectReferenceValue = target;
            });
        }

        private static void SetDictionary(
            Component component,
            string propertyPath,
            int entryCount,
            Action<SerializedProperty, int> configureEntry)
        {
            using var serializedObject = new SerializedObject(component);
            serializedObject.Update();
            var dictionary = GetProperty(serializedObject, propertyPath);
            dictionary.arraySize = entryCount;

            for (var i = 0; i < entryCount; i++)
            {
                configureEntry(dictionary.GetArrayElementAtIndex(i), i);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty GetProperty(SerializedObject serializedObject, string propertyPath)
        {
            return serializedObject.FindProperty(propertyPath)
                ?? throw new InvalidOperationException($"Showcase property '{propertyPath}' was not found.");
        }

        private static SerializedProperty GetRelative(SerializedProperty property, string relativePath)
        {
            return property.FindPropertyRelative(relativePath)
                ?? throw new InvalidOperationException(
                    $"Showcase property '{property.propertyPath}.{relativePath}' was not found.");
        }
    }
}
#endif
