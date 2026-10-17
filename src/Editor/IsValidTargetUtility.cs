using System;
using System.Collections.Generic;
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
        /// The target is an array or list of custom serializable objects or managed references.
        /// </summary>
        Collection
    }

    /// <summary>
    /// Classifies fields supported by <see cref="IsValidAttribute"/>.
    /// </summary>
    internal static class IsValidTargetUtility
    {
        /// <summary>
        /// Classifies an attributed field and returns the type to validate recursively.
        /// </summary>
        /// <param name="field"> The field to classify. </param>
        /// <param name="nestedType"> The object or collection element type to validate. </param>
        /// <returns> The supported traversal kind. </returns>
        internal static IsValidTargetKind Classify(FieldInfo field, out Type nestedType)
        {
            var isManagedReference = field.IsDefined(typeof(SerializeReference), true);
            return Classify(field.FieldType, isManagedReference, out nestedType);
        }

        /// <summary>
        /// Classifies a field type and returns the type to validate recursively.
        /// </summary>
        /// <param name="fieldType"> The field type to classify. </param>
        /// <param name="isManagedReference"> Whether the field is serialized with [SerializeReference]. </param>
        /// <param name="nestedType"> The declared object or collection element type to validate. </param>
        /// <returns> The supported traversal kind. </returns>
        /// <remarks>
        /// A [SerializeReference] field may be declared as an interface, an abstract class, or object. Its values
        /// are validated according to their runtime type.
        /// </remarks>
        internal static IsValidTargetKind Classify(Type fieldType, bool isManagedReference, out Type nestedType)
        {
            var elementType = GetSupportedCollectionElementType(fieldType);
            if (elementType != null)
            {
                if (IsTraversableType(elementType, isManagedReference))
                {
                    nestedType = elementType;
                    return IsValidTargetKind.Collection;
                }

                nestedType = null;
                return IsValidTargetKind.Unsupported;
            }

            if (IsTraversableType(fieldType, isManagedReference))
            {
                nestedType = fieldType;
                return IsValidTargetKind.Object;
            }

            nestedType = null;
            return IsValidTargetKind.Unsupported;
        }

        /// <summary>
        /// Determines whether a type is a custom serializable class supported by recursive validation.
        /// </summary>
        /// <param name="type"> The type to inspect. </param>
        /// <returns> Whether the type is supported. </returns>
        internal static bool IsSerializableCustomClass(Type type)
        {
            return type != null &&
                   type.IsClass &&
                   type != typeof(string) &&
                   !typeof(UnityEngine.Object).IsAssignableFrom(type) &&
                   type.IsDefined(typeof(SerializableAttribute), false);
        }

        private static bool IsTraversableType(Type type, bool isManagedReference)
        {
            if (IsSerializableCustomClass(type))
            {
                return true;
            }

            // A managed reference can hold any serializable subclass of its declared type, so traversal
            // checks each value's runtime type instead.
            return isManagedReference &&
                   (type.IsInterface || type.IsClass) &&
                   type != typeof(string) &&
                   !typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        private static Type GetSupportedCollectionElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)
                ? type.GetGenericArguments()[0]
                : null;
        }
    }
}
