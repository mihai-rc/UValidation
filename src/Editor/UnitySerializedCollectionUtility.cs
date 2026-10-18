using System;
using System.Collections.Generic;

namespace UValidation.Editor
{
    /// <summary>
    /// Classifies collection shapes supported by Unity serialization.
    /// </summary>
    internal static class UnitySerializedCollectionUtility
    {
        /// <summary>
        /// Gets whether a type is an array or a concrete <see cref="List{T}"/>.
        /// </summary>
        /// <param name="type"> The type to inspect. </param>
        /// <returns> Whether the type is a recognized Unity collection container. </returns>
        internal static bool IsCollection(Type type)
        {
            return type != null &&
                   (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
        }

        /// <summary>
        /// Gets the element type of a supported one-dimensional array or concrete <see cref="List{T}"/>.
        /// </summary>
        /// <param name="type"> The collection type to inspect. </param>
        /// <param name="elementType"> The collection element type when supported. </param>
        /// <returns> Whether the collection shape is supported by Unity serialization. </returns>
        internal static bool TryGetElementType(Type type, out Type elementType)
        {
            elementType = null;
            if (type == null)
            {
                return false;
            }

            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                {
                    return false;
                }

                elementType = type.GetElementType();
                return elementType != null;
            }

            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(List<>))
            {
                return false;
            }

            elementType = type.GetGenericArguments()[0];
            return true;
        }
    }
}
