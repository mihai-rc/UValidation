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
        void Validate(ref Validation validation);
    }
}
