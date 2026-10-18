#if UNITY_6000_6_OR_NEWER
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UValidation.Editor
{
    /// <summary>
    /// Colors invalid GameObject labels in the UI Toolkit Hierarchy available in Unity 6.6 and newer.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyV2ValidationFeedback
    {
        private const string k_InvalidLabelClass = "uvalidation-invalid-hierarchy-label";
        private const string k_StyleSheetPath = "Packages/com.uvalidation.validation/Editor/HierarchyV2ValidationFeedback.uss";

        private static readonly HashSet<HierarchyView> s_BoundViews = new();

        private static StyleSheet s_StyleSheet;
        private static bool s_RefreshScheduled;

        static HierarchyV2ValidationFeedback()
        {
            HierarchyWindow.BindView += OnBindView;
            HierarchyWindow.UnbindView += OnUnbindView;
            HierarchyWindow.BindViewItem += OnBindViewItem;
            ValidationResultCache.ResultsInvalidated += ScheduleRefresh;
        }

        private static void OnBindView(HierarchyWindow window, HierarchyView view)
        {
            s_BoundViews.Add(view);

            s_StyleSheet ??= AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StyleSheetPath);
            if (s_StyleSheet != null && !view.StyleContainer.styleSheets.Contains(s_StyleSheet))
            {
                view.StyleContainer.styleSheets.Add(s_StyleSheet);
            }
        }

        private static void OnUnbindView(HierarchyWindow window, HierarchyView view)
        {
            s_BoundViews.Remove(view);
            if (s_StyleSheet != null)
            {
                view.StyleContainer.styleSheets.Remove(s_StyleSheet);
            }
        }

        private static void OnBindViewItem(
            HierarchyWindow window,
            HierarchyView view,
            HierarchyViewItem item)
        {
            RefreshItem(item);
        }

        private static void ScheduleRefresh()
        {
            if (s_RefreshScheduled)
            {
                return;
            }

            s_RefreshScheduled = true;
            EditorApplication.delayCall += RefreshVisibleItems;
        }

        private static void RefreshVisibleItems()
        {
            s_RefreshScheduled = false;
            s_BoundViews.RemoveWhere(view => view == null || view.panel == null);

            foreach (var view in s_BoundViews)
            {
                view.Query<HierarchyViewItem>().ForEach(RefreshItem);
            }
        }

        private static void RefreshItem(HierarchyViewItem item)
        {
            var isInvalid = false;
            if (item.Handler is HierarchyGameObjectHandler handler)
            {
                var gameObject = handler.GetGameObject(item.Node);
                isInvalid = gameObject != null && !ValidationResultCache.IsObjectValid(gameObject);
            }

            // Hierarchy rows are recycled, so the class must also be removed when a valid item is rebound.
            item.Name.EnableInClassList(k_InvalidLabelClass, isInvalid);
        }
    }
}
#endif
