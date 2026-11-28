using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UValidation.Editor
{
    /// <summary>
    /// Caches validation results for Editor GUI callbacks, which run several times per frame for every visible item.
    /// </summary>
    /// <remarks>
    /// Results are discarded whenever the Editor reports a change to objects, the hierarchy, or the project,
    /// and after undo or redo. <see cref="IsObjectValid"/> and <see cref="IsAssetValid"/> bypass the cache in
    /// Play Mode, where gameplay code changes objects without reporting it. Deferred asset results from
    /// <see cref="ScheduleAssetValidation"/> are cached in every mode, because re-validating stored assets on
    /// every Project window repaint would reload them continuously.
    /// </remarks>
    [InitializeOnLoad]
    internal static class ValidationResultCache
    {
        private static readonly Dictionary<UnityEngine.Object, bool> s_ObjectResults = new();
        private static readonly Dictionary<string, bool> s_AssetResults = new(StringComparer.Ordinal);
        private static readonly HashSet<string> s_PendingAssetPaths = new(StringComparer.Ordinal);

        private static bool s_IsAssetValidationScheduled;

        /// <summary>
        /// Raised after cached validation results are invalidated.
        /// </summary>
        internal static event Action ResultsInvalidated;

        static ValidationResultCache()
        {
            ObjectChangeEvents.changesPublished += OnChangesPublished;
            Undo.undoRedoPerformed += Clear;
            EditorApplication.hierarchyChanged += ClearObjectResults;
            EditorApplication.projectChanged += ClearProjectResults;
            EditorApplication.playModeStateChanged += _ => Clear();
        }

        /// <summary>
        /// Gets whether a GameObject, prefab asset, or other Unity object passes validation.
        /// </summary>
        /// <param name="target"> The object to validate. </param>
        /// <returns> True if the object is valid; otherwise, false. </returns>
        /// <remarks>
        /// A GameObject is validated with its components. A prefab asset GameObject is validated as the whole prefab.
        /// </remarks>
        internal static bool IsObjectValid(UnityEngine.Object target)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return ValidateObject(target);
            }

            if (!s_ObjectResults.TryGetValue(target, out var isValid))
            {
                isValid = ValidateObject(target);
                s_ObjectResults[target] = isValid;
            }

            return isValid;
        }

        /// <summary>
        /// Gets whether the stored asset at a path passes validation.
        /// </summary>
        /// <param name="assetPath"> The asset path to validate. </param>
        /// <returns> True unless the asset contains invalid data. </returns>
        internal static bool IsAssetValid(string assetPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return ValidateAsset(assetPath);
            }

            if (!s_AssetResults.TryGetValue(assetPath, out var isValid))
            {
                isValid = ValidateAsset(assetPath);
                s_AssetResults[assetPath] = isValid;
            }

            return isValid;
        }

        /// <summary>
        /// Gets the cached validation result for a stored asset without validating it.
        /// </summary>
        /// <param name="assetPath"> The asset path to look up. </param>
        /// <param name="isValid"> The cached result, or true when no result is cached. </param>
        /// <returns> True if a result is cached for the asset; otherwise, false. </returns>
        /// <remarks>
        /// Unlike <see cref="IsAssetValid"/>, this never loads the asset, so it is safe to call from GUI callbacks.
        /// Pair it with <see cref="ScheduleAssetValidation"/> to fill the cache outside the GUI pass.
        /// </remarks>
        internal static bool TryGetCachedAssetResult(string assetPath, out bool isValid)
        {
            if (s_AssetResults.TryGetValue(assetPath, out isValid))
            {
                return true;
            }

            isValid = true;
            return false;
        }

        /// <summary>
        /// Queues a stored asset for validation on the next Editor update instead of validating it immediately.
        /// </summary>
        /// <param name="assetPath"> The asset path to validate. </param>
        /// <remarks>
        /// Validating a scene or prefab loads its contents into a preview scene. Doing that inside a GUI callback
        /// starts other rendering in the middle of the GUI pass, which Unity reports as unbalanced GUI clips and
        /// graphics-frame assertions. The Project window repaints once the queue finds an invalid asset.
        /// </remarks>
        internal static void ScheduleAssetValidation(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !s_PendingAssetPaths.Add(assetPath) || s_IsAssetValidationScheduled)
            {
                return;
            }

            s_IsAssetValidationScheduled = true;
            EditorApplication.delayCall += ValidatePendingAssets;
        }

        /// <summary>
        /// Validates every queued asset, caches the results, and repaints the Project window when one is invalid.
        /// </summary>
        internal static void ValidatePendingAssets()
        {
            EditorApplication.delayCall -= ValidatePendingAssets;
            s_IsAssetValidationScheduled = false;
            if (s_PendingAssetPaths.Count == 0)
            {
                return;
            }

            var assetPaths = new List<string>(s_PendingAssetPaths);
            s_PendingAssetPaths.Clear();

            var hasInvalidAsset = false;
            foreach (var assetPath in assetPaths)
            {
                var isValid = ValidateAsset(assetPath);
                s_AssetResults[assetPath] = isValid;
                hasInvalidAsset |= !isValid;
            }

            // Valid assets draw nothing, so only an invalid result changes what the Project window shows.
            if (hasInvalidAsset)
            {
                EditorApplication.RepaintProjectWindow();
            }
        }

        /// <summary>
        /// Discards every cached result.
        /// </summary>
        internal static void Clear()
        {
            s_ObjectResults.Clear();
            s_AssetResults.Clear();
            ResultsInvalidated?.Invoke();
        }

        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            var resultsInvalidated = false;
            for (var i = 0; i < stream.length; i++)
            {
                switch (stream.GetEventType(i))
                {
                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                        stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var propertyChange);
                        InvalidateObject(EditorEntityIdCompatibility.GetEntityId(propertyChange));
                        break;
                    case ObjectChangeKind.ChangeAssetObjectProperties:
                        stream.GetChangeAssetObjectPropertiesEvent(i, out var assetChange);
                        InvalidateAsset(assetChange.guid.ToString());
                        InvalidateObject(EditorEntityIdCompatibility.GetEntityId(assetChange));
                        break;
                    case ObjectChangeKind.CreateAssetObject:
                        stream.GetCreateAssetObjectEvent(i, out var createdAsset);
                        InvalidateAsset(createdAsset.guid.ToString());
                        break;
                    case ObjectChangeKind.DestroyAssetObject:
                        stream.GetDestroyAssetObjectEvent(i, out var destroyedAsset);
                        InvalidateAsset(destroyedAsset.guid.ToString());
                        break;
                    case ObjectChangeKind.ChangeScene:
                        stream.GetChangeSceneEvent(i, out var sceneChange);
                        ClearSceneObjectResults(sceneChange.scene);
                        break;
                    case ObjectChangeKind.ChangeGameObjectParent:
                        stream.GetChangeGameObjectParentEvent(i, out var parentChange);
                        ClearSceneObjectResults(parentChange.previousScene);
                        ClearSceneObjectResults(parentChange.newScene);
                        break;
                    case ObjectChangeKind.CreateGameObjectHierarchy:
                        stream.GetCreateGameObjectHierarchyEvent(i, out var createdHierarchy);
                        ClearSceneObjectResults(createdHierarchy.scene);
                        break;
                    case ObjectChangeKind.DestroyGameObjectHierarchy:
                        stream.GetDestroyGameObjectHierarchyEvent(i, out var destroyedHierarchy);
                        ClearSceneObjectResults(destroyedHierarchy.scene);
                        break;
                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                        stream.GetChangeGameObjectStructureHierarchyEvent(i, out var hierarchyChange);
                        ClearSceneObjectResults(hierarchyChange.scene);
                        break;
                    case ObjectChangeKind.ChangeGameObjectStructure:
                        stream.GetChangeGameObjectStructureEvent(i, out var structureChange);
                        ClearSceneObjectResults(structureChange.scene);
                        break;
                    case ObjectChangeKind.ChangeChildrenOrder:
                        stream.GetChangeChildrenOrderEvent(i, out var childrenChange);
                        ClearSceneObjectResults(childrenChange.scene);
                        break;
                    case ObjectChangeKind.ChangeRootOrder:
                        stream.GetChangeRootOrderEvent(i, out var rootChange);
                        ClearSceneObjectResults(rootChange.scene);
                        break;
                    case ObjectChangeKind.UpdatePrefabInstances:
                        stream.GetUpdatePrefabInstancesEvent(i, out var prefabChange);
                        ClearSceneObjectResults(prefabChange.scene);
                        break;
                    default:
                        continue;
                }

                resultsInvalidated = true;
            }

            if (resultsInvalidated)
            {
                ResultsInvalidated?.Invoke();
            }
        }

        private static void InvalidateObject(EntityId entityId)
        {
            var target = EditorUtility.EntityIdToObject(entityId);
            if (target == null)
            {
                return;
            }

            s_ObjectResults.Remove(target);
            if (target is Component component)
            {
                s_ObjectResults.Remove(component.gameObject);
            }

            var assetPath = AssetDatabase.GetAssetPath(target);
            if (!string.IsNullOrEmpty(assetPath))
            {
                InvalidateAssetPath(assetPath);
            }
        }

        private static void InvalidateAsset(string guid)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
            {
                s_AssetResults.Clear();
                return;
            }

            InvalidateAssetPath(assetPath);
        }

        private static void InvalidateAssetPath(string assetPath)
        {
            s_AssetResults.Remove(assetPath);

            var objectsToRemove = new List<UnityEngine.Object>();
            foreach (var cachedObject in s_ObjectResults.Keys)
            {
                if (cachedObject == null ||
                    string.Equals(AssetDatabase.GetAssetPath(cachedObject), assetPath, StringComparison.Ordinal))
                {
                    objectsToRemove.Add(cachedObject);
                }
            }

            foreach (var cachedObject in objectsToRemove)
            {
                s_ObjectResults.Remove(cachedObject);
            }
        }

        private static void ClearSceneObjectResults(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            var objectsToRemove = new List<UnityEngine.Object>();
            foreach (var cachedObject in s_ObjectResults.Keys)
            {
                var gameObject = cachedObject switch
                {
                    GameObject target => target,
                    Component component => component.gameObject,
                    _ => null
                };

                if (cachedObject == null || gameObject != null && gameObject.scene == scene)
                {
                    objectsToRemove.Add(cachedObject);
                }
            }

            foreach (var cachedObject in objectsToRemove)
            {
                s_ObjectResults.Remove(cachedObject);
            }
        }

        private static void ClearObjectResults()
        {
            s_ObjectResults.Clear();
            ResultsInvalidated?.Invoke();
        }

        private static void ClearProjectResults()
        {
            s_AssetResults.Clear();

            var persistentObjects = new List<UnityEngine.Object>();
            foreach (var cachedObject in s_ObjectResults.Keys)
            {
                if (cachedObject == null || EditorUtility.IsPersistent(cachedObject))
                {
                    persistentObjects.Add(cachedObject);
                }
            }

            foreach (var cachedObject in persistentObjects)
            {
                s_ObjectResults.Remove(cachedObject);
            }

            ResultsInvalidated?.Invoke();
        }

        private static bool ValidateObject(UnityEngine.Object target)
        {
            if (target is not GameObject gameObject)
            {
                return ValidationHelper.IsScriptValid(target, false);
            }

            // The Inspector shows a prefab asset through its root, but the asset is invalid when any object in it is.
            return EditorUtility.IsPersistent(gameObject)
                ? ValidationHelper.IsPrefabValidAtPath(AssetDatabase.GetAssetPath(gameObject), false)
                : ValidationHelper.IsGameObjectValid(gameObject, false);
        }

        private static bool ValidateAsset(string assetPath)
        {
            return ValidationHelper.ValidateImportedAssetAtPath(assetPath, false) != AssetValidationState.Invalid;
        }
    }
}
