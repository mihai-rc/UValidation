using UnityEditor;

namespace UValidation.Editor
{
    /// <summary>
    /// A custom property drawer for fields marked with the <see cref="IsValidAttribute"/>.
    /// Displays a warning when the field cannot be recursively validated.
    /// </summary>
    [CustomPropertyDrawer(typeof(IsValidAttribute))]
    public class IsValidFieldDrawer : ValidationFieldDrawer
    {
        private const string k_AttributeName = nameof(IsValidAttribute);
        private const string k_AllowedTypeDescription = "custom serializable classes, [SerializeReference] references, or arrays/lists of those";
        private const string k_ViolationMessage = "Nested values must satisfy their validation rules.";

        /// <inheritdoc />
        protected override string AttributeName => k_AttributeName;

        /// <inheritdoc />
        protected override string AllowedTypeDescription => k_AllowedTypeDescription;

        /// <inheritdoc />
        protected override string ViolationMessage => k_ViolationMessage;

        /// <inheritdoc />
        protected override bool IsUsageValid(SerializedProperty property)
        {
            return IsValidTargetUtility.Classify(fieldInfo) != IsValidTargetKind.Unsupported;
        }

        /// <inheritdoc />
        protected override bool IsPropertyValid(SerializedProperty property)
        {
            return true;
        }
    }
}
