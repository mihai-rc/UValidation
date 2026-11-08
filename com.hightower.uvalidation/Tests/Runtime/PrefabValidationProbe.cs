using UnityEngine;

namespace UValidation.Tests
{
    public sealed class PrefabValidationProbe : MonoBehaviour
    {
        [SerializeField, NotEmpty] private string m_Value;

        /// <summary>
        /// Sets the serialized value used by prefab validation tests.
        /// </summary>
        /// <param name="value"> The value to validate. </param>
        public void SetValue(string value)
        {
            m_Value = value;
        }
    }
}
