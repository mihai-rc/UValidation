using UnityEngine;

namespace UValidation.Showcase
{
    /// <summary>
    /// Displays instructions for navigating the dictionary showcase.
    /// </summary>
    public sealed class DictionaryShowcaseOverview : MonoBehaviour
    {
        [SerializeField, TextArea(12, 24)]
        private string m_Instructions =
            "Unity 6.6 serializes Dictionary<TKey, TValue> fields marked with [SerializeField].\n\n" +
            "Select each numbered GameObject to inspect valid and intentionally invalid dictionary " +
            "examples. UValidation treats dictionary values as collection elements; Unity owns duplicate " +
            "and null-key diagnostics.\n\n" +
            "Dictionary validation messages stay at field level so Unity's native two-column editor and " +
            "key diagnostics remain unchanged.\n\n" +
            "Use UValidation > Rebuild Dictionary Showcase to restore all examples.";
    }
}
