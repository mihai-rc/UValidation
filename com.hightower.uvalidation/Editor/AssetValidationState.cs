namespace UValidation.Editor
{
    /// <summary>
    /// Describes whether an asset can be saved or must be validated once Unity imports it.
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
        /// The contents being saved are not readable yet and must be validated after Unity imports them.
        /// </summary>
        /// <remarks>
        /// Used for assets that do not exist in the AssetDatabase yet, for prefab writes whose source is not
        /// exposed to the save callback, and for scenes saved to an unloaded path, such as using Save As to
        /// overwrite an existing scene file.
        /// </remarks>
        ValidateAfterImport
    }
}
