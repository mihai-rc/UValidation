using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;

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
        }

        private static readonly Dictionary<Type, List<FieldValidationData>> s_Cache = new();

        /// <summary>
        /// Reads through all fields of the target and applies validations if they are
        /// </summary>
        public static void ValidateAttributes(UnityEngine.Object target, ref Validation validation)
        {
            if (target == null)
            {
                return;
            }

            var visited = HashSetPool<object>.Get();
            try
            {
                ValidateAttributesInternal(target, ref validation, visited);
            }
            finally
            {
                HashSetPool<object>.Release(visited);
            }
        }

        private static void ValidateAttributesInternal(UnityEngine.Object target, ref Validation validation, HashSet<object> visited)
        {
            var type = target.GetType();
            var fieldsAttributeData = GetFieldsAttributeData(type);

            foreach (var data in fieldsAttributeData)
            {
                var value = data.Field.GetValue(target);
                ApplyFieldValidation(data, data.CleanName, value, ref validation);

                if (data.IsValid != null && value != null)
                {
                    ValidateNestedObject(value, data.CleanName, ref validation, visited);
                }
            }

            if (target is IValidatable validatable)
            {
                validatable.Validate(ref validation);
            }
        }

        /// <summary>
        /// Recursively validates a plain C# object (non-Unity.Object) whose fields may carry validation attributes.
        /// </summary>
        /// <param name="nestedObj">The nested object instance to validate.</param>
        /// <param name="parentFieldName">The field name of the parent (used as prefix in error messages).</param>
        /// <param name="validation">The validation context to accumulate failures into.</param>
        /// <param name="visited">Reference-equality set of plain C# objects already visited, to prevent cycles.</param>
        private static void ValidateNestedObject(object nestedObj, string parentFieldName, ref Validation validation, HashSet<object> visited)
        {
            if (nestedObj == null)
            {
                return;
            }

            var type = nestedObj.GetType();

            // Ignore Unity objects and primitive / well-known value types to avoid infinite loops
            // and meaningless recursion.
            if (typeof(UnityEngine.Object).IsAssignableFrom(type) || type.IsPrimitive || type == typeof(string))
            {
                return;
            }

            // Reference-identity cycle guard. Only meaningful for reference types — value types boxed
            // here would always hash-compare equal by value, but we don't recurse into them anyway.
            if (!type.IsValueType && !visited.Add(nestedObj))
            {
                return;
            }

            var handlers = GetFieldsAttributeData(type);

            foreach (var data in handlers)
            {
                var value = data.Field.GetValue(nestedObj);

                // Prefix the field name so the error message reads "parentField.childField"
                var qualifiedName = $"{parentFieldName}.{data.CleanName}";
                ApplyFieldValidation(data, qualifiedName, value, ref validation);

                if (data.IsValid != null && value != null)
                {
                    ValidateNestedObject(value, qualifiedName, ref validation, visited);
                }
            }

            if (nestedObj is IValidatable validatable)
            {
                validatable.Validate(ref validation);
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
                            IsValid = isValid
                        });
                    }
                }
            }

            s_Cache[type] = handlers;
            return handlers;
        }

        private static void ApplyFieldValidation(FieldValidationData data, string fieldName, object value, ref Validation validation)
        {
            var fieldType = data.Field.FieldType;

            if (data.NotNull != null)
            {
                var attr = data.NotNull;
                validation.IsNotNull(fieldName, value, attr.FilePath, null, attr.LineNumber);
            }

            if (data.NotEmpty != null)
            {
                var attr = data.NotEmpty;
                if (fieldType == typeof(string))
                {
                    validation.IsNotEmpty(fieldName, (string)value, attr.FilePath, null, attr.LineNumber);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(fieldType))
                {
                    var enumerable = (IEnumerable)value;
                    validation.IsNotEmpty(fieldName, enumerable?.Cast<object>(), attr.FilePath, null, attr.LineNumber);
                }
            }

            if (data.HasNoNulls != null && typeof(IEnumerable).IsAssignableFrom(fieldType))
            {
                var attr = data.HasNoNulls;
                var enumerable = (IEnumerable)value;
                var elementType = GetEnumerableElementType(fieldType);

                if (elementType != null && typeof(UnityEngine.Object).IsAssignableFrom(elementType))
                {
                    validation.HasNoNulls(fieldName, enumerable?.Cast<UnityEngine.Object>(), attr.FilePath, null, attr.LineNumber);
                }
                else
                {
                    validation.HasNoNulls(fieldName, enumerable?.Cast<object>(), attr.FilePath, null, attr.LineNumber);
                }
            }

            if (data.HasNoEmpties != null && typeof(IEnumerable<string>).IsAssignableFrom(fieldType))
            {
                var attr = data.HasNoEmpties;
                validation.HasNoEmpties(fieldName, (IEnumerable<string>)value, attr.FilePath, null, attr.LineNumber);
            }
        }

        private static Type GetEnumerableElementType(Type collectionType)
        {
            if (collectionType.IsArray)
            {
                return collectionType.GetElementType();
            }

            foreach (var iface in collectionType.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return iface.GetGenericArguments()[0];
                }
            }

            return null;
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
