namespace UValidation.Editor
{
    /// <summary>
    /// Describes whether an asset can be saved or still needs validation after creation.
    /// </summary>
    internal enum AssetValidationState
    {
        /// <summary>
        /// The asset is valid or is not handled by UValidation.
        /// </summary>
        Valid,

        /// <summary>
        /// The asset contains invalid data or could not be read safely.
        /// </summary>
        Invalid,

        /// <summary>
        /// The asset does not exist in the AssetDatabase yet and must be validated after import.
        /// </summary>
        PendingCreation
    }
}
