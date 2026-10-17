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

        private sealed class ValidationTraversalState
        {
            public readonly HashSet<object> ObjectPath = new(ReferenceComparer.Instance);
            public readonly HashSet<FieldInfo> ReportedNonSerializedFields;
            public readonly UnityEngine.Object Context;

            public ValidationTraversalState(UnityEngine.Object context)
            {
                Context = context;
                ReportedNonSerializedFields = s_ReportedNonSerializedFieldsByTarget.GetOrCreateValue(context);
            }
        }

        private static readonly HashSet<Type> s_UnitySerializableValueTypes = new()
        {
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Rect),
            typeof(RectInt),
            typeof(Quaternion),
            typeof(Matrix4x4),
            typeof(Color),
            typeof(Color32),
            typeof(LayerMask),
            typeof(Bounds),
            typeof(BoundsInt),
            typeof(Vector2Int),
            typeof(Vector3Int),
            typeof(RenderingLayerMask),
            typeof(UnityEngine.Rendering.SphericalHarmonicsL2),
            typeof(PropertyName)
        };

        private static readonly Type[] s_UnitySerializableReferenceTypes =
        {
            typeof(AnimationCurve),
            typeof(Gradient),
            typeof(GUIStyle),
            typeof(RectOffset)
        };

        private static readonly ConditionalWeakTable<UnityEngine.Object, HashSet<FieldInfo>>
            s_ReportedNonSerializedFieldsByTarget = new();

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

            var traversalState = new ValidationTraversalState(target);
            ValidateAttributesInternal(target, validation, traversalState);
        }

        private static void ValidateAttributesInternal(
            UnityEngine.Object target,
            Validation validation,
            ValidationTraversalState traversalState)
        {
            var type = target.GetType();
            var fieldsAttributeData = GetFieldsAttributeData(type);

            foreach (var data in fieldsAttributeData)
            {
                if (!IsSerializedField(data.Field, traversalState))
                {
                    continue;
                }

                var value = data.Field.GetValue(target);
                ApplyFieldValidation(data, data.CleanName, value, validation);

                if (data.IsValid != null && value != null)
                {
                    ValidateNestedValue(data.IsValidTarget, value, data.CleanName, validation, traversalState);
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
        /// <param name="traversalState"> State shared across the current recursive traversal. </param>
        private static void ValidateNestedObject(
            object nestedObj,
            string parentFieldName,
            Validation validation,
            ValidationTraversalState traversalState)
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

            if (!traversalState.ObjectPath.Add(nestedObj))
            {
                return;
            }

            try
            {
                var handlers = GetFieldsAttributeData(type);

                foreach (var data in handlers)
                {
                    if (!IsSerializedField(data.Field, traversalState))
                    {
                        continue;
                    }

                    var value = data.Field.GetValue(nestedObj);
                    var qualifiedName = $"{parentFieldName}.{data.CleanName}";
                    ApplyFieldValidation(data, qualifiedName, value, validation);

                    if (data.IsValid != null && value != null)
                    {
                        ValidateNestedValue(data.IsValidTarget, value, qualifiedName, validation, traversalState);
                    }
                }

                if (nestedObj is IValidatable validatable)
                {
                    RunCustomValidation(validatable, parentFieldName, validation);
                }
            }
            finally
            {
                traversalState.ObjectPath.Remove(nestedObj);
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
            ValidationTraversalState traversalState)
        {
            if (targetKind == IsValidTargetKind.Object)
            {
                ValidateNestedObject(value, fieldName, validation, traversalState);
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
                    ValidateNestedObject(element, $"{fieldName}[{index}]", validation, traversalState);
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

        private static bool IsSerializedField(
            FieldInfo field,
            ValidationTraversalState traversalState)
        {
            var hasSerializationAttribute =
                field.IsDefined(typeof(SerializeField), true) ||
                field.IsDefined(typeof(SerializeReference), true);
            var isSerialized =
                !field.IsStatic &&
                !field.IsLiteral &&
                !field.IsInitOnly &&
                !field.IsNotSerialized &&
                (field.IsPublic || hasSerializationAttribute) &&
                IsSupportedSerializedType(field);

            if (isSerialized)
            {
                return true;
            }

            if (traversalState.ReportedNonSerializedFields.Add(field))
            {
                var fieldName = $"{field.DeclaringType?.FullName}.{field.Name}";
                Debug.LogWarning(
                    $"[UValidation] Validation attributes on nonserialized field '{fieldName}' are ignored. " +
                    "Remove the validation attributes or make the field serializable according to Unity's rules.",
                    traversalState.Context);
            }

            return false;
        }

        private static bool IsSupportedSerializedType(FieldInfo field)
        {
            return field.IsDefined(typeof(SerializeReference), true)
                ? IsSupportedManagedReferenceType(field.FieldType, true)
                : IsSupportedInlineType(field.FieldType, true);
        }

        private static bool IsSupportedInlineType(Type type, bool allowContainer)
        {
            if (type == null)
            {
                return false;
            }

            if (type.IsPrimitive ||
                type.IsEnum ||
                type == typeof(string) ||
                typeof(UnityEngine.Object).IsAssignableFrom(type) ||
                IsUnitySerializableBuiltInType(type))
            {
                return true;
            }

            if (type.IsArray)
            {
                return allowContainer &&
                       type.GetArrayRank() == 1 &&
                       IsSupportedInlineType(type.GetElementType(), false);
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return allowContainer &&
                       IsSupportedInlineType(type.GetGenericArguments()[0], false);
            }

            if (type.IsInterface ||
                type.IsAbstract ||
                type.IsGenericTypeDefinition ||
                typeof(IDictionary).IsAssignableFrom(type) ||
                typeof(Delegate).IsAssignableFrom(type) ||
                type.Namespace == "System" ||
                type.Namespace?.StartsWith("System.", StringComparison.Ordinal) == true)
            {
                return false;
            }

            return type.IsDefined(typeof(SerializableAttribute), false);
        }

        private static bool IsUnitySerializableBuiltInType(Type type)
        {
            if (s_UnitySerializableValueTypes.Contains(type))
            {
                return true;
            }

            foreach (var serializableType in s_UnitySerializableReferenceTypes)
            {
                if (serializableType.IsAssignableFrom(type))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSupportedManagedReferenceType(Type type, bool isFieldType)
        {
            if (type == null)
            {
                return false;
            }

            if (type.IsArray)
            {
                return isFieldType &&
                       type.GetArrayRank() == 1 &&
                       IsSupportedManagedReferenceType(type.GetElementType(), false);
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return isFieldType &&
                       IsSupportedManagedReferenceType(type.GetGenericArguments()[0], false);
            }

            if (type.IsValueType ||
                type == typeof(string) ||
                typeof(UnityEngine.Object).IsAssignableFrom(type) ||
                typeof(Delegate).IsAssignableFrom(type))
            {
                return false;
            }

            if (type == typeof(object) || type.IsInterface)
            {
                return true;
            }

            // Unity serializes a single managed reference declared as any class except Dictionary<,>,
            // but requires array and list elements declared as a class to be [Serializable].
            return isFieldType
                ? !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                : type.IsDefined(typeof(SerializableAttribute), false);
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
