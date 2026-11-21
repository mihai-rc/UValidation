using UnityEngine;

namespace UValidation.Tests
{
    public sealed class ScriptableObjectValidationProbe : ScriptableObject
    {
        [SerializeField, NotEmpty] private string m_Value;

        /// <summary>
        /// Sets the serialized value used by ScriptableObject asset validation tests.
        /// </summary>
        /// <param name="value"> The value to validate. </param>
        public void SetValue(string value)
        {
            m_Value = value;
        }
    }
}
