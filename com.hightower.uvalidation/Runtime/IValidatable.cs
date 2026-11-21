namespace UValidation
{
    /// <summary>
    /// Marks a serializable type that needs custom validation.
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// Performs the validation and reports any failures.
        /// </summary>
        /// <param name="validation"> The validation instance that accumulates failures. </param>
        /// <remarks>
        /// UValidation invokes this method in the Unity Editor when it validates the implementing object.
        /// Exceptions thrown while UValidation invokes this method are recorded as validation failures.
        /// </remarks>
        void Validate(Validation validation);
    }
}
