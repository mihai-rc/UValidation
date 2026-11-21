using UnityEditor;
using UnityEngine.UIElements;

namespace UValidation.Tests
{
    /// <summary>
    /// Hosts UI Toolkit content in a real editor window so tests can observe resolved layout.
    /// </summary>
    internal sealed class UiToolkitTestWindow : EditorWindow
    {
        /// <summary>
        /// Adds the content whose resolved layout is under test.
        /// </summary>
        /// <param name="content"> The content to display. </param>
        internal void Configure(VisualElement content)
        {
            rootVisualElement.Add(content);
        }
    }
}
