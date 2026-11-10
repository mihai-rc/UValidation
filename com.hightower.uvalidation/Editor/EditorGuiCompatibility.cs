using System;
using UnityEditor;
using UnityEngine;

namespace UValidation.Editor
{
    /// <summary>
    /// Adapts Editor GUI geometry whose layout differs between supported Unity versions.
    /// </summary>
    internal static class EditorGuiCompatibility
    {
        /// <summary>
        /// Gets the label width Unity assigns to a reorderable collection element.
        /// </summary>
        /// <param name="elementRect"> The unmodified element rectangle supplied by Unity. </param>
        /// <param name="element"> The serialized collection element. </param>
        /// <returns> The width of the element label. </returns>
        internal static float GetCollectionElementLabelWidth(
            Rect elementRect,
            SerializedProperty element)
        {
            const float labelRatio = 0.45f;
            const float labelBaseOffset = 20.0f;
            const float nestedArrayOffset = 22.0f;
            const float shallowPropertyOffset = 7.0f;

            var labelWidth = elementRect.width * labelRatio - labelBaseOffset -
                CountArrayLevels(element.propertyPath) * nestedArrayOffset;
            if (element.depth < 2)
            {
                labelWidth += shallowPropertyOffset;
            }

            return Mathf.Clamp(labelWidth, 0.0f, elementRect.width);
        }

        private static int CountArrayLevels(string propertyPath)
        {
            const string arrayElementPath = ".Array.data";

            var count = 0;
            var startIndex = 0;
            while ((startIndex = propertyPath.IndexOf(
                       arrayElementPath,
                       startIndex,
                       StringComparison.Ordinal)) >= 0)
            {
                count++;
                startIndex += arrayElementPath.Length;
            }

            return count;
        }
    }
}
