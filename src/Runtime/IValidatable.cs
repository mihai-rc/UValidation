namespace UValidation
{
    /// <summary>
    /// Marks a serializable type that needs custom validation.
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// Reports any validation failures.
        /// </summary>
        /// <param name="validation"> The validation instance that accumulates failures. </param>
        void Validate(Validation validation);
    }
}
