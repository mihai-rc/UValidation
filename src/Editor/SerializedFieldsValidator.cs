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
            public bool IsSerialized;
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
            public readonly UnityEngine.Object Context;

            public ValidationTraversalState(UnityEngine.Object context)
            {
                Context = context;
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

        // The warning is about a field declaration, so each field is reported once per domain reload
        // rather than once for every object that declares it.
        private static readonly HashSet<FieldInfo> s_ReportedNonSerializedFields = new();

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
            ValidateObjectFields(target, null, validation, traversalState);
        }

        /// <summary>
        /// Forgets which nonserialized fields have already been reported.
        /// </summary>
        /// <remarks>
        /// Editor tests call this so each test observes the warning regardless of earlier runs in the same domain.
        /// </remarks>
        internal static void ResetReportedNonSerializedFields()
        {
            s_ReportedNonSerializedFields.Clear();
        }

        private static void ValidateObjectFields(
            object target,
            string objectPath,
            Validation validation,
            ValidationTraversalState traversalState)
        {
            var type = target.GetType();
            var fieldsAttributeData = GetFieldsAttributeData(type);

            foreach (var data in fieldsAttributeData)
            {
                if (!data.IsSerialized)
                {
                    ReportNonSerializedField(data.Field, traversalState.Context);
                    continue;
                }

                var value = data.Field.GetValue(target);
                var fieldName = string.IsNullOrEmpty(objectPath)
                    ? data.CleanName
                    : $"{objectPath}.{data.CleanName}";

                ApplyFieldValidation(data, fieldName, value, validation);

                if (data.IsValid != null && value != null)
                {
                    ValidateNestedValue(data.IsValidTarget, value, fieldName, validation, traversalState);
                }
            }

            if (target is IValidatable validatable)
            {
                RunCustomValidation(validatable, objectPath, validation);
            }
        }

        /// <summary>
        /// Recursively validates a plain C# object (non-Unity.Object) whose fields may carry validation attributes.
        /// </summary>
        /// <param name="nestedObj"> The nested object instance to validate. </param>
        /// <param name="parentFieldName"> The field name of the parent (used as prefix in error messages). </param>
        /// <param name="validation"> The validation context to accumulate failures into. </param>
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
                ValidateObjectFields(nestedObj, parentFieldName, validation, traversalState);
            }
            finally
            {
                traversalState.ObjectPath.Remove(nestedObj);
            }
        }

        private static void RunCustomValidation(IValidatable validatable, string objectPath, Validation validation)
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
                var fields = currentType.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

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
                            IsValidTarget = isValid != null ? IsValidTargetUtility.Classify(field) : IsValidTargetKind.Unsupported,
                            IsSerialized = IsSerializedField(field)
                        });
                    }
                }
            }

            s_Cache[type] = handlers;
            return handlers;
        }

        private static bool IsSerializedField(FieldInfo field)
        {
            var hasSerializationAttribute =
                field.IsDefined(typeof(SerializeField), true) ||
                field.IsDefined(typeof(SerializeReference), true);

            return !field.IsStatic &&
                   !field.IsLiteral &&
                   !field.IsInitOnly &&
                   !field.IsNotSerialized &&
                   (field.IsPublic || hasSerializationAttribute) &&
                   IsSupportedSerializedType(field);
        }

        private static void ReportNonSerializedField(FieldInfo field, UnityEngine.Object context)
        {
            if (!s_ReportedNonSerializedFields.Add(field))
            {
                return;
            }

            var fieldName = $"{field.DeclaringType?.FullName}.{field.Name}";
            Debug.LogWarning(
                $"[UValidation] Validation attributes on nonserialized field '{fieldName}' are ignored. " +
                "Remove the validation attributes or make the field serializable according to Unity's rules.",
                context);
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

            if (UnitySerializedCollectionUtility.IsCollection(type))
            {
                return allowContainer &&
                       UnitySerializedCollectionUtility.TryGetElementType(type, out var elementType) &&
                       IsSupportedInlineType(elementType, false);
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

            if (UnitySerializedCollectionUtility.IsCollection(type))
            {
                return isFieldType &&
                       UnitySerializedCollectionUtility.TryGetElementType(type, out var elementType) &&
                       IsSupportedManagedReferenceType(elementType, false);
            }

            if (type.IsValueType ||
                type == typeof(string) ||
                typeof(UnityEngine.Object).IsAssignableFrom(type) ||
                typeof(Delegate).IsAssignableFrom(type) ||
                typeof(IDictionary).IsAssignableFrom(type))
            {
                return false;
            }

            // SerializeReference accepts class, abstract-class, interface, and object declarations. For arrays
            // and lists, the attribute applies to each element, so the declaration itself does not need to be
            // [Serializable]; Unity requires that attribute on each concrete value assigned at runtime instead.
            return type == typeof(object) || type.IsInterface || type.IsClass;
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
