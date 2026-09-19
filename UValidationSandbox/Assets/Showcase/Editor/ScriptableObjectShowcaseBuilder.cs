using System;
using UnityEditor;
using UnityEngine;

namespace UValidation.Showcase.Editor
{
    /// <summary>
    /// Builds deterministic ScriptableObject assets that exercise the asset validation path.
    /// </summary>
    public static class ScriptableObjectShowcaseBuilder
    {
        private const string k_ValidationEnabledKey = "Framework.EnableValidation";
        private const string k_ValidationMenuPath = "HighTower/Enable Validation";
        private const string k_ShowcaseFolderPath = "Assets/Showcase/ScriptableObjects";
        private const string k_SharedFolderPath = k_ShowcaseFolderPath + "/_Shared";

        /// <summary>
        /// Rebuilds the valid and invalid ScriptableObject showcase assets.
        /// </summary>
        [MenuItem("HighTower/UValidation/Rebuild ScriptableObject Showcase")]
        public static void RebuildScriptableObjectShowcase()
        {
            var wasValidationEnabled = EditorPrefs.GetBool(k_ValidationEnabledKey, true);
            EditorPrefs.SetBool(k_ValidationEnabledKey, false);

            try
            {
                RecreateShowcaseFolders();
                var reference = CreateReferenceAsset();

                var validAsset = CreateShowcaseAsset("01 - Valid.asset", reference);

                CreateShowcaseAsset("02 - NotNull Failure.asset", reference,
                    serializedObject => SetObject(serializedObject, "m_RequiredReference", null));

                CreateShowcaseAsset("03 - NotEmpty Failure.asset", reference, serializedObject =>
                {
                    SetString(serializedObject, "m_RequiredName", "");
                    SetIntegerArray(serializedObject, "m_RequiredValues");
                });

                CreateShowcaseAsset("04 - HasNoNulls Failure.asset", reference,
                    serializedObject => SetObjectArray(
                        serializedObject, "m_References", reference, null));

                CreateShowcaseAsset("05 - HasNoEmpties Failure.asset", reference,
                    serializedObject => SetStringArray(
                        serializedObject, "m_Labels", "Alpha", ""));

                CreateShowcaseAsset("06 - IsValid Failure.asset", reference, serializedObject =>
                {
                    SetString(serializedObject, "m_Nested.m_Name", "");
                    SetObject(serializedObject, "m_Nested.m_Target", null);
                });

                CreateShowcaseAsset("07 - IValidatable Failure.asset", reference,
                    serializedObject => SetInteger(serializedObject, "m_CurrentValue", 4));

                CreateShowcaseAsset("08 - Combined Failures.asset", reference, serializedObject =>
                {
                    SetObject(serializedObject, "m_RequiredReference", null);
                    SetString(serializedObject, "m_RequiredName", "");
                    SetIntegerArray(serializedObject, "m_RequiredValues");
                    SetObjectArray(serializedObject, "m_References", reference, null);
                    SetStringArray(serializedObject, "m_Labels", "Alpha", "");
                    SetString(serializedObject, "m_Nested.m_Name", "");
                    SetObject(serializedObject, "m_Nested.m_Target", null);
                    SetInteger(serializedObject, "m_CurrentValue", 4);
                });

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = validAsset;
                EditorGUIUtility.PingObject(validAsset);
                Debug.Log($"UValidation ScriptableObject showcase rebuilt at '{k_ShowcaseFolderPath}'.");
            }
            finally
            {
                EditorPrefs.SetBool(k_ValidationEnabledKey, wasValidationEnabled);
                Menu.SetChecked(k_ValidationMenuPath, wasValidationEnabled);
            }
        }

        private static void RecreateShowcaseFolders()
        {
            if (AssetDatabase.IsValidFolder(k_ShowcaseFolderPath)
                && !AssetDatabase.DeleteAsset(k_ShowcaseFolderPath))
            {
                throw new InvalidOperationException(
                    $"Could not delete the existing showcase folder at '{k_ShowcaseFolderPath}'.");
            }

            var showcaseFolderGuid = AssetDatabase.CreateFolder("Assets/Showcase", "ScriptableObjects");
            if (string.IsNullOrEmpty(showcaseFolderGuid))
            {
                throw new InvalidOperationException(
                    $"Could not create the showcase folder at '{k_ShowcaseFolderPath}'.");
            }

            var sharedFolderGuid = AssetDatabase.CreateFolder(k_ShowcaseFolderPath, "_Shared");
            if (string.IsNullOrEmpty(sharedFolderGuid))
            {
                throw new InvalidOperationException(
                    $"Could not create the shared folder at '{k_SharedFolderPath}'.");
            }
        }

        private static ScriptableObjectShowcaseReference CreateReferenceAsset()
        {
            var reference = ScriptableObject.CreateInstance<ScriptableObjectShowcaseReference>();
            reference.name = "Assigned Reference";
            AssetDatabase.CreateAsset(reference, k_SharedFolderPath + "/Assigned Reference.asset");
            return reference;
        }

        private static ScriptableObjectValidationShowcase CreateShowcaseAsset(
            string fileName,
            ScriptableObjectShowcaseReference reference,
            Action<SerializedObject> configureFailure = null)
        {
            var asset = ScriptableObject.CreateInstance<ScriptableObjectValidationShowcase>();
            asset.name = fileName[..^".asset".Length];

            using (var serializedObject = new SerializedObject(asset))
            {
                serializedObject.Update();
                SetObject(serializedObject, "m_RequiredReference", reference);
                SetString(serializedObject, "m_RequiredName", "Configured");
                SetIntegerArray(serializedObject, "m_RequiredValues", 1);
                SetObjectArray(serializedObject, "m_References", reference);
                SetStringArray(serializedObject, "m_Labels", "Alpha", "Beta");
                SetString(serializedObject, "m_Nested.m_Name", "Configured");
                SetObject(serializedObject, "m_Nested.m_Target", reference);
                SetInteger(serializedObject, "m_MinimumValue", 5);
                SetInteger(serializedObject, "m_CurrentValue", 5);
                configureFailure?.Invoke(serializedObject);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.CreateAsset(asset, $"{k_ShowcaseFolderPath}/{fileName}");
            return asset;
        }

        private static void SetObject(
            SerializedObject serializedObject,
            string propertyPath,
            UnityEngine.Object value)
        {
            GetProperty(serializedObject, propertyPath).objectReferenceValue = value;
        }

        private static void SetString(SerializedObject serializedObject, string propertyPath, string value)
        {
            GetProperty(serializedObject, propertyPath).stringValue = value;
        }

        private static void SetInteger(SerializedObject serializedObject, string propertyPath, int value)
        {
            GetProperty(serializedObject, propertyPath).intValue = value;
        }

        private static void SetObjectArray(
            SerializedObject serializedObject,
            string propertyPath,
            params UnityEngine.Object[] values)
        {
            var property = GetProperty(serializedObject, propertyPath);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void SetStringArray(
            SerializedObject serializedObject,
            string propertyPath,
            params string[] values)
        {
            var property = GetProperty(serializedObject, propertyPath);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }

        private static void SetIntegerArray(
            SerializedObject serializedObject,
            string propertyPath,
            params int[] values)
        {
            var property = GetProperty(serializedObject, propertyPath);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).intValue = values[i];
            }
        }

        private static SerializedProperty GetProperty(
            SerializedObject serializedObject,
            string propertyPath)
        {
            return serializedObject.FindProperty(propertyPath)
                ?? throw new InvalidOperationException(
                    $"Property '{propertyPath}' was not found on '{serializedObject.targetObject.GetType().Name}'.");
        }
    }
}
