using System;
using UnityEditor;
using UnityEngine;

namespace UValidation.Editor
{
    /// <summary>
    /// Adapts legacy Unity Editor APIs that expose integer instance IDs to EntityId-based package APIs.
    /// </summary>
    internal static class EditorEntityIdCompatibility
    {
        /// <summary>
        /// Registers an EntityId-based callback for visible Hierarchy items.
        /// </summary>
        /// <param name="callback"> Callback invoked for each visible Hierarchy item. </param>
        internal static void RegisterHierarchyWindowItemCallback(Action<EntityId, Rect> callback)
        {
#if UNITY_6000_6_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI +=
                (entityId, selectionRect) => callback(entityId, selectionRect);
#else
            EditorApplication.hierarchyWindowItemOnGUI +=
                (instanceId, selectionRect) => callback((EntityId)instanceId, selectionRect);
#endif
        }

        /// <summary>
        /// Gets the EntityId of an object whose GameObject or component properties changed.
        /// </summary>
        /// <param name="change"> The Editor change event. </param>
        /// <returns> The changed object's EntityId. </returns>
        internal static EntityId GetEntityId(ChangeGameObjectOrComponentPropertiesEventArgs change)
        {
#if UNITY_6000_6_OR_NEWER
            return change.entityId;
#else
            return (EntityId)change.instanceId;
#endif
        }

        /// <summary>
        /// Gets the EntityId of an asset object whose properties changed.
        /// </summary>
        /// <param name="change"> The Editor change event. </param>
        /// <returns> The changed object's EntityId. </returns>
        internal static EntityId GetEntityId(ChangeAssetObjectPropertiesEventArgs change)
        {
#if UNITY_6000_6_OR_NEWER
            return change.entityId;
#else
            return (EntityId)change.instanceId;
#endif
        }
    }
}
