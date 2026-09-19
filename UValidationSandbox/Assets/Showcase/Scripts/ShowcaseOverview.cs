using UnityEngine;

namespace UValidation.Showcase
{
    /// <summary>
    /// Displays instructions for navigating the validation showcase.
    /// </summary>
    public sealed class ShowcaseOverview : MonoBehaviour
    {
        [SerializeField, TextArea(12, 24)]
        private string m_Instructions =
            "Select each numbered GameObject to inspect one validation attribute.\n\n" +
            "Each component contains supported passing values, supported failing values, and deliberate " +
            "misuse examples. Yellow HelpBoxes indicate unsupported attribute placement; red HelpBoxes " +
            "indicate invalid values.\n\n" +
            "The scene is intentionally invalid. Saving it while HighTower > Enable Validation is enabled " +
            "should be blocked.\n\n" +
            "Use HighTower > UValidation > Rebuild Showcase Scene to restore all examples.";
    }
}
