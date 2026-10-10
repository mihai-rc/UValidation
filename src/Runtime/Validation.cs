using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;

namespace UValidation
{
    /// <summary>
    /// Provides methods to assert validation rules.
    /// </summary>
    /// <remarks>
    /// Wrap usage in a <c>using</c> statement and call <see cref="Report"/> before disposal to log any failures.
    /// </remarks>
    public sealed class Validation : IDisposable
    {
        private const string k_ConditionFailed = "[Validation] Condition failed!";
        private const string k_PredicateNull = "[Validation] Predicate was null!";
        private const string k_NullReference = "[Validation] Reference is null!";
        private const string k_EmptyString = "[Validation] String is null or empty!";
        private const string k_EmptyCollection = "[Validation] Collection is empty!";
        private const string k_CollectionWithNullItems = "[Validation] Collection contains null items!";
        private const string k_CollectionWithEmptyStrings = "[Validation] Collection contains null or empty string items!";
        private const string k_CustomValidationException = "[Validation] Custom validation threw an exception!";

        private const string k_ContextTag = " - Context: {0}";
        private const string k_VariableTag = " - Variable: {0}";
        private const string k_FileTag = " - File: {0}";
        private const string k_FunctionTag = " - Function: {0}";
        private const string k_LineTag = " - Line: {0}";
        private const string k_ExceptionTag = " - Exception: {0}";

        private readonly UnityEngine.Object m_Context;
        private List<string> m_Reasons;
        private StringBuilder m_ErrorBuilder;
        private string m_VariablePath;
        private bool m_Failed;
        private bool m_Disposed;

        /// <summary>
        /// Indicates whether all validation checks have passed.
        /// </summary>
        public bool Passed
        {
            get
            {
                ThrowIfDisposed();
                return !m_Failed;
            }
        }

        /// <summary>
        /// Indicates whether at least one validation has failed.
        /// </summary>
        public bool Failed
        {
            get
            {
                ThrowIfDisposed();
                return m_Failed;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Validation"/> class.
        /// </summary>
        /// <param name="context"> The Unity object in the context which the validation happens. </param>
        public Validation(UnityEngine.Object context = null)
        {
            m_Context = context;
            m_Reasons = null;
            m_ErrorBuilder = null;
            m_VariablePath = null;
            m_Failed = false;
            m_Disposed = false;
        }

        /// <summary>
        /// Reports all validation reasons if there are any.
        /// </summary>
        public void Report()
        {
            ThrowIfDisposed();

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
        /// Safe to call multiple times.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            m_Disposed = true;

            if (m_Reasons != null)
            {
                ListPool<string>.Release(m_Reasons);
                m_Reasons = null;
            }

            m_ErrorBuilder = null;
            m_VariablePath = null;
        }

        /// <summary>
        /// Prefixes failures produced by custom validation with their location in the serialized object graph.
        /// </summary>
        /// <param name="variablePath"> The path of the object currently being validated. </param>
        /// <returns> A scope that restores the previous path when disposed. </returns>
        internal VariablePathScope PushVariablePath(string variablePath)
        {
            ThrowIfDisposed();

            var previousPath = m_VariablePath;
            m_VariablePath = QualifyVariable(variablePath);
            return new VariablePathScope(this, previousPath);
        }

        /// <summary>
        /// Records an exception thrown by a custom validator as a validation failure.
        /// </summary>
        /// <param name="variable"> The object path whose custom validation failed. </param>
        /// <param name="exception"> The exception thrown by the custom validator. </param>
        internal void RecordCustomValidationException(string variable, Exception exception)
        {
            ThrowIfDisposed();

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Fail(
                k_CustomValidationException,
                variable,
                null,
                nameof(IValidatable.Validate),
                0,
                exception.ToString());
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
        /// <returns> The validation instance for continued fluent chaining. </returns>
        public Validation IsTrue<T>(
            string variable,
            T ownerRef,
            Func<T, bool> assertFn,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            ThrowIfDisposed();

            if (assertFn == null)
            {
                Fail(k_PredicateNull, variable, file, function, line);
                return this;
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

            return this;
        }

        /// <summary>
        /// Checks if the specified reference is not null.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        /// <returns> The validation instance for continued fluent chaining. </returns>
        public Validation IsNotNull(
            string variable,
            object variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            ThrowIfDisposed();

            if (variableRef is UnityEngine.Object unityObject)
            {
                return IsNotNull(variable, unityObject, file, function, line);
            }

            if (variableRef != null)
            {
                return this;
            }

            Fail(k_NullReference, variable, file, function, line);
            return this;
        }

        /// <summary>
        /// Checks if the specified reference is not null.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        /// <returns> The validation instance for continued fluent chaining. </returns>
        public Validation IsNotNull(
            string variable,
            UnityEngine.Object variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            ThrowIfDisposed();

            if (variableRef != null)
            {
                return this;
            }

            Fail(k_NullReference, variable, file, function, line);
            return this;
        }

        /// <summary>
        /// Checks if the specified reference is not null.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        /// <returns> The validation instance for continued fluent chaining. </returns>
        public Validation IsNotNull(
            string variable,
            IEnumerable<object> variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            ThrowIfDisposed();

            if (variableRef != null)
            {
                return this;
            }

            Fail(k_NullReference, variable, file, function, line);
            return this;
        }

        /// <summary>
        /// Checks if the specified string is not null or empty.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        /// <returns> The validation instance for continued fluent chaining. </returns>
        public Validation IsNotEmpty(
            string variable,
            string variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            ThrowIfDisposed();

            if (!string.IsNullOrEmpty(variableRef))
            {
                return this;
            }

            Fail(k_EmptyString, variable, file, function, line);
            return this;
        }

        /// <summary>
        /// Checks if the specified collection is not empty.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        /// <returns> The validation instance for continued fluent chaining. </returns>
        public Validation IsNotEmpty(
            string variable,
            IEnumerable<object> variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            ThrowIfDisposed();

            if (variableRef != null && variableRef.Any())
            {
                return this;
            }

            Fail(k_EmptyCollection, variable, file, function, line);
            return this;
        }

        /// <summary>
        /// Checks if the specified collection contains no null items.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        /// <returns> The validation instance for continued fluent chaining. </returns>
        public Validation HasNoNulls(
            string variable,
            IEnumerable<object> variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            ThrowIfDisposed();

            if (variableRef != null && variableRef.All(o => o != null))
            {
                return this;
            }

            Fail(k_CollectionWithNullItems, variable, file, function, line);
            return this;
        }

        /// <summary>
        /// Checks if the specified collection contains no null items, honoring Unity's
        /// fake-null semantics for destroyed <see cref="UnityEngine.Object"/> elements.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        /// <returns> The validation instance for continued fluent chaining. </returns>
        public Validation HasNoNulls(
            string variable,
            IEnumerable<UnityEngine.Object> variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            ThrowIfDisposed();

            if (variableRef != null && variableRef.All(o => o != null))
            {
                return this;
            }

            Fail(k_CollectionWithNullItems, variable, file, function, line);
            return this;
        }

        /// <summary>
        /// Checks if the specified string collection contains no null or empty items.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        /// <returns> The validation instance for continued fluent chaining. </returns>
        public Validation HasNoEmpties(
            string variable,
            IEnumerable<string> variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            ThrowIfDisposed();

            if (variableRef != null && variableRef.All(s => !string.IsNullOrEmpty(s)))
            {
                return this;
            }

            Fail(k_CollectionWithEmptyStrings, variable, file, function, line);
            return this;
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

            m_ErrorBuilder.AppendLine(string.Format(k_VariableTag, QualifyVariable(variable)));

            if (!string.IsNullOrEmpty(file))
            {
                m_ErrorBuilder.AppendLine(string.Format(k_FileTag, file));
            }

            if (!string.IsNullOrEmpty(function))
            {
                m_ErrorBuilder.AppendLine(string.Format(k_FunctionTag, function));
            }

            if (line > 0)
            {
                m_ErrorBuilder.AppendLine(string.Format(k_LineTag, line));
            }

            if (!string.IsNullOrEmpty(exception))
            {
                m_ErrorBuilder.AppendLine(string.Format(k_ExceptionTag, exception));
            }

            m_Reasons.Add(m_ErrorBuilder.ToString());
            m_Failed = true;
        }

        private string QualifyVariable(string variable)
        {
            if (string.IsNullOrEmpty(m_VariablePath))
            {
                return variable;
            }

            return string.IsNullOrEmpty(variable)
                ? m_VariablePath
                : $"{m_VariablePath}.{variable}";
        }

        private void ThrowIfDisposed()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(nameof(Validation));
            }
        }

        /// <summary>
        /// Restores a validation variable path after nested custom validation completes.
        /// </summary>
        internal readonly struct VariablePathScope : IDisposable
        {
            private readonly Validation m_Validation;
            private readonly string m_PreviousPath;

            /// <summary>
            /// Initializes a new path-restoration scope.
            /// </summary>
            /// <param name="validation"> The validation whose path should be restored. </param>
            /// <param name="previousPath"> The path to restore. </param>
            internal VariablePathScope(Validation validation, string previousPath)
            {
                m_Validation = validation;
                m_PreviousPath = previousPath;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                if (m_Validation != null)
                {
                    m_Validation.m_VariablePath = m_PreviousPath;
                }
            }
        }
    }
}
