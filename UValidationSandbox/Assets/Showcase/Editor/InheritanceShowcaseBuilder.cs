using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UValidation.Showcase.Editor
{
    /// <summary>
    /// Builds isolated examples of validation across a three-level inheritance chain.
    /// </summary>
    public static class InheritanceShowcaseBuilder
    {
        private const string k_ValidationEnabledKey = "Framework.EnableValidation";
        private const string k_ValidationMenuPath = "HighTower/Enable Validation";
        private const string k_ScenePath = "Assets/Scenes/ValidationInheritanceShowcase.unity";

        /// <summary>
        /// Rebuilds the inheritance showcase without replacing the open scenes.
        /// </summary>
        [MenuItem("HighTower/UValidation/Rebuild Inheritance Showcase")]
        public static void RebuildShowcaseScene()
        {
            BuildScene(k_ScenePath);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(k_ScenePath));
        }

        /// <summary>
        /// Writes an inheritance showcase scene at the requested asset path.
        /// </summary>
        /// <param name="scenePath"> Asset path at which to save the scene. </param>
        public static void BuildScene(string scenePath)
        {
            var existingScene = SceneManager.GetSceneByPath(scenePath);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                throw new InvalidOperationException("Close the inheritance showcase scene before rebuilding it.");
            }

            var previousScene = SceneManager.GetActiveScene();
            var hadValidationPreference = EditorPrefs.HasKey(k_ValidationEnabledKey);
            var wasValidationEnabled = EditorPrefs.GetBool(k_ValidationEnabledKey, true);
            var scene = default(Scene);
            EditorPrefs.SetBool(k_ValidationEnabledKey, false);

            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                var root = new GameObject("UValidation Inheritance Showcase");
                SceneManager.MoveGameObjectToScene(root, scene);

                CreateCase(root, "01 - All Levels Valid", null);
                CreateCase(root, "02 - Base Private Field Missing", "m_BaseReference");
                CreateCase(root, "03 - Middle Private Field Empty", "m_MiddleLabel");
                CreateCase(root, "04 - Derived Private Field Missing", "m_DerivedReference");
                CreateCase(root, "05 - Base Auto-Property Empty", "<BaseLabel>k__BackingField");

                if (!EditorSceneManager.SaveScene(scene, scenePath))
                {
                    throw new InvalidOperationException($"Could not save inheritance showcase at '{scenePath}'.");
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

                if (hadValidationPreference)
                {
                    EditorPrefs.SetBool(k_ValidationEnabledKey, wasValidationEnabled);
                }
                else
                {
                    EditorPrefs.DeleteKey(k_ValidationEnabledKey);
                }

                Menu.SetChecked(k_ValidationMenuPath, wasValidationEnabled);
            }
        }

        private static void CreateCase(GameObject root, string name, string invalidPropertyPath)
        {
            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, root.scene);
            gameObject.transform.SetParent(root.transform);
            var component = gameObject.AddComponent<InheritanceShowcase>();

            using var serializedObject = new SerializedObject(component);
            serializedObject.Update();
            GetProperty(serializedObject, "m_BaseReference").objectReferenceValue = root;
            GetProperty(serializedObject, "<BaseLabel>k__BackingField").stringValue = "Configured";
            GetProperty(serializedObject, "m_MiddleLabel").stringValue = "Configured";
            GetProperty(serializedObject, "m_DerivedReference").objectReferenceValue = root;

            if (invalidPropertyPath != null)
            {
                var property = GetProperty(serializedObject, invalidPropertyPath);
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    property.objectReferenceValue = null;
                }
                else
                {
                    property.stringValue = string.Empty;
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty GetProperty(SerializedObject serializedObject, string propertyPath)
        {
            return serializedObject.FindProperty(propertyPath)
                ?? throw new InvalidOperationException($"Showcase property '{propertyPath}' was not found.");
        }
    }
}
