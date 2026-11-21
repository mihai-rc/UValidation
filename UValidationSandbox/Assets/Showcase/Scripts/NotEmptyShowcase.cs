using System.Collections.Generic;
using UnityEngine;
using UValidation;

namespace UValidation.Showcase
{
    /// <summary>
    /// Shows supported, failing, and unsupported uses of <see cref="NotEmptyAttribute"/>.
    /// </summary>
    public sealed class NotEmptyShowcase : MonoBehaviour
    {
        [Header("Strings - pass and fail")]
        [SerializeField, NotEmpty] private string m_PopulatedString = "Configured";
        [SerializeField, NotEmpty] private string m_EmptyString = "";

        [Header("Arrays - pass and fail")]
        [SerializeField, NotEmpty] private GameObject[] m_PopulatedObjects = { null };
        [SerializeField, NotEmpty] private GameObject[] m_EmptyObjects = { };

        [Header("Lists - pass and fail")]
        [SerializeField, NotEmpty] private List<int> m_PopulatedValues = new() { 1 };
        [SerializeField, NotEmpty] private List<int> m_EmptyValues = new();

        [Header("Misuse - Inspector warning and runtime no-op")]
        [SerializeField, NotEmpty] private int m_ScalarMisuse;
    }
}
