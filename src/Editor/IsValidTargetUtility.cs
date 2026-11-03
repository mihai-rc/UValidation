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
        Collection
    }

    /// <summary>
    /// Classifies fields supported by <see cref="IsValidAttribute"/>.
    /// </summary>
    internal static class IsValidTargetUtility
    {
        /// <summary>
        /// Classifies an attributed field for recursive validation.
        /// </summary>
        /// <param name="field"> The field to classify. </param>
        /// <returns> The supported traversal kind. </returns>
        internal static IsValidTargetKind Classify(FieldInfo field)
        {
            var isManagedReference = field.IsDefined(typeof(SerializeReference), true);
            return Classify(field.FieldType, isManagedReference);
        }

        /// <summary>
        /// Determines whether a type is a custom serializable class or struct supported by recursive validation.
        /// </summary>
        /// <param name="type"> The type to inspect. </param>
        /// <returns> Whether the type is supported. </returns>
        internal static bool IsSerializableCustomType(Type type)
        {
            return type != null &&
                   (type.IsClass || IsSerializableCustomStruct(type)) &&
                   type != typeof(string) &&
                   !typeof(UnityEngine.Object).IsAssignableFrom(type) &&
                   !SerializedFieldsValidator.IsUnitySerializableBuiltInType(type) &&
                   type.IsDefined(typeof(SerializableAttribute), false);
        }

        /// <summary>
        /// Classifies a field type for recursive validation.
        /// </summary>
        /// <param name="fieldType"> The field type to classify. </param>
        /// <param name="isManagedReference"> Whether the field is serialized with [SerializeReference]. </param>
        /// <returns> The supported traversal kind. </returns>
        /// <remarks>
        /// A [SerializeReference] field may be declared as an interface, an abstract class, or an object.
        /// Its values are validated according to their runtime type.
        /// </remarks>
        private static IsValidTargetKind Classify(Type fieldType, bool isManagedReference)
        {
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
            if (IsSerializableCustomType(type))
            {
                return true;
            }

            // A managed reference can hold any serializable subclass of its declared type, so traversal
            // checks each value's runtime type instead.
            return isManagedReference &&
                   (type.IsInterface || type.IsClass) &&
                   type != typeof(string) &&
                   !typeof(UnityEngine.Object).IsAssignableFrom(type) &&
                   (!isCollectionElement || type == typeof(object) || type.IsInterface);
        }

        private static bool IsSerializableCustomStruct(Type type)
        {
            return type.IsValueType &&
                   !type.IsPrimitive &&
                   !type.IsEnum;
        }
    }
}
