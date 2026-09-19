using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;

namespace UValidation
{
    /// <summary>
    /// Provides methods to assert validation rules.
    /// </summary>
    /// <remarks>
    /// Wrap usage in <c>try / finally</c> and call <see cref="Dispose"/> in the finally
    /// (the C# <c>using</c> statement does not compose with passing the instance by <c>ref</c>
    /// to validation methods). Call <see cref="Report"/> before disposal to log any failures.
    /// Until the first failure is recorded, no allocations occur.
    /// </remarks>
    public ref struct Validation
    {
        private const string k_ConditionFailed = "[Validation] Condition failed!";
        private const string k_PredicateNull = "[Validation] Predicate was null!";
        private const string k_NullReference = "[Validation] Reference is null!";
        private const string k_EmptyString = "[Validation] String is null or empty!";
        private const string k_EmptyCollection = "[Validation] Collection is empty!";
        private const string k_CollectionWithNullItems = "[Validation] Collection contains null items!";
        private const string k_CollectionWithEmptyStrings = "[Validation] Collection contains null or empty string items!";

        private const string k_ContextTag = " - Context: {0}";
        private const string k_VariableTag = " - Variable: {0}";
        private const string k_FileTag = " - File: {0}";
        private const string k_FunctionTag = " - Function: {0}";
        private const string k_LineTag = " - Line: {0}";
        private const string k_ExceptionTag = " - Exception: {0}";

        private readonly UnityEngine.Object m_Context;
        private List<string> m_Reasons;
        private StringBuilder m_ErrorBuilder;
        private bool m_Failed;

        /// <summary>
        /// Indicates whether all validation checks have passed.
        /// </summary>
        public bool Passed => !m_Failed;

        /// <summary>
        /// Indicates whether at list one validation had failed.
        /// </summary>
        public bool Failed => m_Failed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Validation"/> struct.
        /// </summary>
        /// <param name="context"> The Unity object in the context which the validation happens. </param>
        public Validation(UnityEngine.Object context = null)
        {
            m_Context = context;
            m_Reasons = null;
            m_ErrorBuilder = null;
            m_Failed = false;
        }

        /// <summary>
        /// Reports all validation reasons if there are any.
        /// </summary>
        public void Report()
        {
            if (m_Reasons == null)
            {
                return;
            }

            foreach (var reason in m_Reasons)
            {
                if (m_Context != null)
                {
                    Debug.LogError(reason, m_Context);
                    continue;
                }

                Debug.LogError(reason);
            }
        }

        /// <summary>
        /// Releases any pooled state rented while recording failures.
        /// Safe to call multiple times and on a default-constructed instance.
        /// </summary>
        public void Dispose()
        {
            if (m_Reasons == null)
            {
                return;
            }

            ListPool<string>.Release(m_Reasons);
            m_Reasons = null;
            m_ErrorBuilder = null;
        }

        /// <summary>
        /// Checks if the specified condition is true for the given reference.
        /// If the condition is not met, the validation fails.
        /// </summary>
        /// <typeparam name="T"> The type that owns the object being validated. </typeparam>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="ownerRef" > The owner of the variable being validated. </param>
        /// <param name="assertFn"> The function that defines the validation condition. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        internal void PassIfTrue<T>(string variable, T ownerRef, Func<T, bool> assertFn, string file, string function, int line)
        {
            if (assertFn == null)
            {
                Fail(k_PredicateNull, variable, file, function, line);
                return;
            }

            bool validationPassed = false;
            string exception = null;

            try
            {
                validationPassed = assertFn.Invoke(ownerRef);
            }
            catch (Exception e)
            {
                validationPassed = false;
                exception = e.ToString();
            }

            if (!validationPassed)
            {
                Fail(k_ConditionFailed, variable, file, function, line, exception);
            }
        }

        /// <summary>
        /// Checks if the specified reference is not null.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        internal void PassIfNotNull(string variable, object variableRef, string file, string function, int line)
        {
            if (variableRef is UnityEngine.Object unityObject)
            {
                PassIfNotNull(variable, unityObject, file, function, line);
                return;
            }

            if (variableRef != null)
            {
                return;
            }

            Fail(k_NullReference, variable, file, function, line);
        }

        /// <summary>
        /// Checks if the specified reference is not null.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        internal void PassIfNotNull(string variable, UnityEngine.Object variableRef, string file, string function, int line)
        {
            if (variableRef != null)
            {
                return;
            }

            Fail(k_NullReference, variable, file, function, line);
        }

        /// <summary>
        /// Checks if the specified reference is not null.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        internal void PassIfNotNull(string variable, IEnumerable<object> variableRef, string file, string function, int line)
        {
            if (variableRef != null)
            {
                return;
            }

            Fail(k_NullReference, variable, file, function, line);
        }

        /// <summary>
        /// Checks if the specified string is not null or empty.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        internal void PassIfNotEmpty(string variable, string variableRef, string file, string function, int line)
        {
            if (!string.IsNullOrEmpty(variableRef))
            {
                return;
            }

            Fail(k_EmptyString, variable, file, function, line);
        }

        /// <summary>
        /// Checks if the specified collection is not empty.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        internal void PassIfNotEmpty(string variable, IEnumerable<object> variableRef, string file, string function, int line)
        {
            if (variableRef != null && variableRef.Any())
            {
                return;
            }

            Fail(k_EmptyCollection, variable, file, function, line);
        }

        /// <summary>
        /// Checks if the specified collection contains no null items.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        internal void PassIfNoNullElements(string variable, IEnumerable<object> variableRef, string file, string function, int line)
        {
            if (variableRef != null && variableRef.All(o => o != null))
            {
                return;
            }

            Fail(k_CollectionWithNullItems, variable, file, function, line);
        }

        /// <summary>
        /// Checks if the specified collection contains no null items.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        internal void PassIfNoNullElements(string variable, IEnumerable<UnityEngine.Object> variableRef, string file, string function, int line)
        {
            if (variableRef != null && variableRef.All(o => o != null))
            {
                return;
            }

            Fail(k_CollectionWithNullItems, variable, file, function, line);
        }

        /// <summary>
        /// Checks if the specified string collection contains no null or empty items.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        internal void PassIfNoEmptyStrings(string variable, IEnumerable<string> variableRef, string file, string function, int line)
        {
            if (variableRef != null && variableRef.All(s => !string.IsNullOrEmpty(s)))
            {
                return;
            }

            Fail(k_CollectionWithEmptyStrings, variable, file, function, line);
        }

        private void Fail(string reason, string variable, string file, string function, int line, string exception = null)
        {
            m_Reasons ??= ListPool<string>.Get();
            m_ErrorBuilder ??= new StringBuilder();
            m_ErrorBuilder.Clear();

            m_ErrorBuilder.AppendLine(reason);

            if (m_Context != null)
            {
                m_ErrorBuilder.AppendLine(string.Format(k_ContextTag, m_Context.name));
            }

            m_ErrorBuilder.AppendLine(string.Format(k_VariableTag, variable));
            m_ErrorBuilder.AppendLine(string.Format(k_FileTag, file));

            if (function != null)
            {
                m_ErrorBuilder.AppendLine(string.Format(k_FunctionTag, function));
            }

            m_ErrorBuilder.AppendLine(string.Format(k_LineTag, line));

            if (!string.IsNullOrEmpty(exception))
            {
                m_ErrorBuilder.AppendLine(string.Format(k_ExceptionTag, exception));
            }

            m_Reasons.Add(m_ErrorBuilder.ToString());
            m_Failed = true;
        }
    }
}