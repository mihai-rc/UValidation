using System;

namespace UValidation.Editor
{
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
            return fieldType != null && typeof(UnityEngine.Object).IsAssignableFrom(fieldType);
        }

        /// <summary>
        /// Determines whether <see cref="NotEmptyAttribute"/> supports a field type.
        /// </summary>
        /// <param name="fieldType"> The field type to inspect. </param>
        /// <returns> Whether the attribute supports the field type. </returns>
        internal static bool SupportsNotEmpty(Type fieldType)
        {
            return fieldType == typeof(string) ||
                   UnitySerializedCollectionUtility.TryGetElementType(fieldType, out _);
        }

        /// <summary>
        /// Determines whether <see cref="HasNoNullsAttribute"/> supports a field type.
        /// </summary>
        /// <param name="fieldType"> The field type to inspect. </param>
        /// <returns> Whether the attribute supports the field type. </returns>
        internal static bool SupportsHasNoNulls(Type fieldType)
        {
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
            return UnitySerializedCollectionUtility.TryGetElementType(fieldType, out var elementType) &&
                   elementType == typeof(string);
        }
    }
}
