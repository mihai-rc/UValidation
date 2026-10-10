using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UValidation.Editor
{
    /// <summary>
    /// Validates objects automatically based on validation attributes.
    /// </summary>
    public static class SerializedFieldsValidator
    {
        private class FieldValidationData
        {
            public FieldInfo Field;
            public string CleanName;
            public NotNullAttribute NotNull;
            public NotEmptyAttribute NotEmpty;
            public HasNoNullsAttribute HasNoNulls;
            public HasNoEmptiesAttribute HasNoEmpties;
            public IsValidAttribute IsValid;
            public IsValidTargetKind IsValidTarget;
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new();

            public new bool Equals(object left, object right)
            {
                return ReferenceEquals(left, right);
            }

            public int GetHashCode(object value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }

        private static readonly Dictionary<Type, List<FieldValidationData>> s_Cache = new();

        /// <summary>
        /// Reads through all fields of the target and applies validations if they are
        /// </summary>
        public static void ValidateAttributes(UnityEngine.Object target, Validation validation)
        {
            if (target == null)
            {
                return;
            }

            var traversalPath = new HashSet<object>(ReferenceComparer.Instance);
            ValidateAttributesInternal(target, validation, traversalPath);
        }

        private static void ValidateAttributesInternal(
            UnityEngine.Object target,
            Validation validation,
            HashSet<object> traversalPath)
        {
            var type = target.GetType();
            var fieldsAttributeData = GetFieldsAttributeData(type);

            foreach (var data in fieldsAttributeData)
            {
                var value = data.Field.GetValue(target);
                ApplyFieldValidation(data, data.CleanName, value, validation);

                if (data.IsValid != null && value != null)
                {
                    ValidateNestedValue(data.IsValidTarget, value, data.CleanName, validation, traversalPath);
                }
            }

            if (target is IValidatable validatable)
            {
                RunCustomValidation(validatable, null, validation);
            }
        }

        /// <summary>
        /// Recursively validates a plain C# object (non-Unity.Object) whose fields may carry validation attributes.
        /// </summary>
        /// <param name="nestedObj">The nested object instance to validate.</param>
        /// <param name="parentFieldName">The field name of the parent (used as prefix in error messages).</param>
        /// <param name="validation">The validation context to accumulate failures into.</param>
        /// <param name="traversalPath"> Reference-identity set containing the active recursion path. </param>
        private static void ValidateNestedObject(
            object nestedObj,
            string parentFieldName,
            Validation validation,
            HashSet<object> traversalPath)
        {
            if (nestedObj == null)
            {
                return;
            }

            var type = nestedObj.GetType();
            if (!IsValidTargetUtility.IsSerializableCustomClass(type))
            {
                return;
            }

            if (!traversalPath.Add(nestedObj))
            {
                return;
            }

            try
            {
                var handlers = GetFieldsAttributeData(type);

                foreach (var data in handlers)
                {
                    var value = data.Field.GetValue(nestedObj);
                    var qualifiedName = $"{parentFieldName}.{data.CleanName}";
                    ApplyFieldValidation(data, qualifiedName, value, validation);

                    if (data.IsValid != null && value != null)
                    {
                        ValidateNestedValue(data.IsValidTarget, value, qualifiedName, validation, traversalPath);
                    }
                }

                if (nestedObj is IValidatable validatable)
                {
                    RunCustomValidation(validatable, parentFieldName, validation);
                }
            }
            finally
            {
                traversalPath.Remove(nestedObj);
            }
        }

        private static void RunCustomValidation(
            IValidatable validatable,
            string objectPath,
            Validation validation)
        {
            using var pathScope = validation.PushVariablePath(objectPath);

            try
            {
                validatable.Validate(validation);
            }
            catch (Exception exception)
            {
                var failurePath = string.IsNullOrEmpty(objectPath)
                    ? validatable.GetType().Name
                    : null;
                validation.RecordCustomValidationException(failurePath, exception);
            }
        }

        private static void ValidateNestedValue(
            IsValidTargetKind targetKind,
            object value,
            string fieldName,
            Validation validation,
            HashSet<object> traversalPath)
        {
            if (targetKind == IsValidTargetKind.Object)
            {
                ValidateNestedObject(value, fieldName, validation, traversalPath);
                return;
            }

            if (targetKind != IsValidTargetKind.Collection || value is not IEnumerable collection)
            {
                return;
            }

            var index = 0;
            foreach (var element in collection)
            {
                if (element != null)
                {
                    ValidateNestedObject(element, $"{fieldName}[{index}]", validation, traversalPath);
                }

                index++;
            }
        }

        private static List<FieldValidationData> GetFieldsAttributeData(Type type)
        {
            if (s_Cache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            var handlers = new List<FieldValidationData>();
            for (var currentType = type; currentType != null; currentType = currentType.BaseType)
            {
                // Read each declaration once, including private fields skipped by inherited lookup.
                var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                  BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (var field in fields)
                {
                    var notNull = field.GetCustomAttribute<NotNullAttribute>();
                    var notEmpty = field.GetCustomAttribute<NotEmptyAttribute>();
                    var noNullItems = field.GetCustomAttribute<HasNoNullsAttribute>();
                    var noEmptyItems = field.GetCustomAttribute<HasNoEmptiesAttribute>();
                    var isValid = field.GetCustomAttribute<IsValidAttribute>();

                    if (notNull != null || notEmpty != null || noNullItems != null || noEmptyItems != null || isValid != null)
                    {
                        handlers.Add(new FieldValidationData
                        {
                            Field = field,
                            CleanName = GetCleanName(field.Name),
                            NotNull = notNull,
                            NotEmpty = notEmpty,
                            HasNoNulls = noNullItems,
                            HasNoEmpties = noEmptyItems,
                            IsValid = isValid,
                            IsValidTarget = isValid != null
                                ? IsValidTargetUtility.Classify(field, out _)
                                : IsValidTargetKind.Unsupported
                        });
                    }
                }
            }

            s_Cache[type] = handlers;
            return handlers;
        }

        private static void ApplyFieldValidation(FieldValidationData data, string fieldName, object value, Validation validation)
        {
            var fieldType = data.Field.FieldType;

            if (data.NotNull != null && ValidationAttributeTargetUtility.SupportsNotNull(fieldType))
            {
                var attr = data.NotNull;
                validation.IsNotNull(fieldName, value, attr.FilePath, null, attr.LineNumber);
            }

            if (data.NotEmpty != null && ValidationAttributeTargetUtility.SupportsNotEmpty(fieldType))
            {
                var attr = data.NotEmpty;
                if (fieldType == typeof(string))
                {
                    validation.IsNotEmpty(fieldName, (string)value, attr.FilePath, null, attr.LineNumber);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(fieldType))
                {
                    validation.IsNotEmpty(
                        fieldName,
                        (IEnumerable)value,
                        attr.FilePath,
                        null,
                        attr.LineNumber);
                }
            }

            if (data.HasNoNulls != null && ValidationAttributeTargetUtility.SupportsHasNoNulls(fieldType))
            {
                var attr = data.HasNoNulls;
                validation.HasNoNulls(
                    fieldName,
                    (IEnumerable)value,
                    attr.FilePath,
                    null,
                    attr.LineNumber);
            }

            if (data.HasNoEmpties != null && ValidationAttributeTargetUtility.SupportsHasNoEmpties(fieldType))
            {
                var attr = data.HasNoEmpties;
                validation.HasNoEmpties(fieldName, (IEnumerable<string>)value, attr.FilePath, null, attr.LineNumber);
            }
        }

        private static string GetCleanName(string fieldName)
        {
            if (fieldName.StartsWith("<") && fieldName.EndsWith(">k__BackingField"))
            {
                return fieldName[1..fieldName.IndexOf('>')];
            }

            return fieldName;
        }
    }
}
