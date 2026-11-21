using System;
using System.Reflection;
using UnityEngine;

namespace UValidation.Editor
{
    /// <summary>
    /// Describes how an <see cref="IsValidAttribute"/> target should be traversed.
    /// </summary>
    internal enum IsValidTargetKind
    {
        /// <summary>
        /// The target cannot be traversed by <see cref="IsValidAttribute"/>.
        /// </summary>
        Unsupported,

        /// <summary>
        /// The target is one custom serializable object or managed reference.
        /// </summary>
        Object,

        /// <summary>
        /// The target is an array or list of custom-serializable objects or managed references.
        /// </summary>
        Collection,

        /// <summary>
        /// The target is a dictionary whose values are custom-serializable objects or serialized collections.
        /// </summary>
        DictionaryValues
    }

    /// <summary>
    /// Defines the serialized field types supported by each validation attribute.
    /// </summary>
    internal static class ValidationAttributeTargetUtility
    {
        /// <summary>
        /// Determines whether <see cref="NotNullAttribute"/> supports a field type.
        /// </summary>
        /// <param name="fieldType"> The field type to inspect. </param>
        /// <returns> Whether the attribute supports the field type. </returns>
        internal static bool SupportsNotNull(Type fieldType)
        {
            return fieldType is not null && typeof(UnityEngine.Object).IsAssignableFrom(fieldType);
        }

        /// <summary>
        /// Determines whether <see cref="NotEmptyAttribute"/> supports a field type.
        /// </summary>
        /// <param name="fieldType"> The field type to inspect. </param>
        /// <returns> Whether the attribute supports the field type. </returns>
        internal static bool SupportsNotEmpty(Type fieldType)
        {
            return fieldType == typeof(string)
                || UnitySerializedCollectionUtility.IsDictionary(fieldType)
                || UnitySerializedCollectionUtility.TryGetElementType(fieldType, out _);
        }

        /// <summary>
        /// Determines whether <see cref="HasNoNullsAttribute"/> supports a field type.
        /// </summary>
        /// <param name="fieldType"> The field type to inspect. </param>
        /// <returns> Whether the attribute supports the field type. </returns>
        internal static bool SupportsHasNoNulls(Type fieldType)
        {
            if (UnitySerializedCollectionUtility.TryGetDictionaryTypes(fieldType, out _, out var valueType))
            {
                return typeof(UnityEngine.Object).IsAssignableFrom(valueType);
            }

            return UnitySerializedCollectionUtility.TryGetElementType(fieldType, out var elementType) &&
                   typeof(UnityEngine.Object).IsAssignableFrom(elementType);
        }

        /// <summary>
        /// Determines whether <see cref="HasNoEmptiesAttribute"/> supports a field type.
        /// </summary>
        /// <param name="fieldType"> The field type to inspect. </param>
        /// <returns> Whether the attribute supports the field type. </returns>
        internal static bool SupportsHasNoEmpties(Type fieldType)
        {
            if (UnitySerializedCollectionUtility.TryGetDictionaryTypes(fieldType, out _, out var valueType))
            {
                return valueType == typeof(string);
            }

            return UnitySerializedCollectionUtility.TryGetElementType(fieldType, out var elementType) &&
                   elementType == typeof(string);
        }

        /// <summary>
        /// Gets how an <see cref="IsValidAttribute"/> field should be traversed.
        /// </summary>
        /// <param name="field"> The field to classify. </param>
        /// <returns> The supported traversal kind. </returns>
        internal static IsValidTargetKind GetIsValidTargetKind(FieldInfo field)
        {
            var isManagedReference = field.IsDefined(typeof(SerializeReference), true);
            return GetIsValidTargetKind(field.FieldType, isManagedReference);
        }

        /// <summary>
        /// Determines whether a type supports recursive validation as a custom serializable class or struct.
        /// </summary>
        /// <param name="type"> The type to inspect. </param>
        /// <returns> Whether the type supports recursive validation. </returns>
        internal static bool SupportsRecursiveValidation(Type type)
        {
            return type is not null
                && (type.IsClass || IsSerializableCustomStruct(type))
                && type != typeof(string)
                && !typeof(UnityEngine.Object).IsAssignableFrom(type)
                && !UnitySerializedCollectionUtility.IsCollection(type)
                && !UnitySerializedCollectionUtility.IsDictionary(type)
                && !SerializedFieldsValidator.IsUnitySerializableBuiltInType(type)
                && type.IsDefined(typeof(SerializableAttribute), false);
        }

        /// <summary>
        /// Gets how an <see cref="IsValidAttribute"/> field type should be traversed.
        /// </summary>
        /// <param name="fieldType"> The field type to classify. </param>
        /// <param name="isManagedReference"> Whether the field is serialized with [SerializeReference]. </param>
        /// <returns> The supported traversal kind. </returns>
        /// <remarks>
        /// A [SerializeReference] field may be declared as an interface, an abstract class, or an object.
        /// Its values are validated according to their runtime type.
        /// </remarks>
        private static IsValidTargetKind GetIsValidTargetKind(Type fieldType, bool isManagedReference)
        {
            if (UnitySerializedCollectionUtility.TryGetDictionaryTypes(fieldType, out _, out var valueType))
            {
                return !isManagedReference &&
                    (UnitySerializedCollectionUtility.TryGetElementType(valueType, out _) || IsTraversableType(valueType, false, false))
                    ? IsValidTargetKind.DictionaryValues
                    : IsValidTargetKind.Unsupported;
            }

            if (UnitySerializedCollectionUtility.IsCollection(fieldType))
            {
                if (UnitySerializedCollectionUtility.TryGetElementType(fieldType, out var elementType) &&
                    IsTraversableType(elementType, isManagedReference, true))
                {
                    return IsValidTargetKind.Collection;
                }

                return IsValidTargetKind.Unsupported;
            }

            if (IsTraversableType(fieldType, isManagedReference, false))
            {
                return IsValidTargetKind.Object;
            }

            return IsValidTargetKind.Unsupported;
        }

        private static bool IsTraversableType(
            Type type,
            bool isManagedReference,
            bool isCollectionElement)
        {
            if (SupportsRecursiveValidation(type))
            {
                return true;
            }

            // A managed reference can hold any serializable subclass of its declared type, so traversal
            // checks each value's runtime type instead.
            return isManagedReference
                && (type.IsInterface || type.IsClass)
                && type != typeof(string)
                && !typeof(UnityEngine.Object).IsAssignableFrom(type)
                && (!isCollectionElement || type == typeof(object) || type.IsInterface);
        }

        private static bool IsSerializableCustomStruct(Type type)
        {
            return type.IsValueType
                && !type.IsPrimitive
                && !type.IsEnum;
        }
    }
}
