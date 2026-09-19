using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UValidation
{
    public static class ValidationExtensions
    {
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
        public static ref Validation IsTrue<T>(
            this ref Validation validation, 
            string variable, 
            T ownerRef, 
            Func<T, bool> assertFn,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            validation.PassIfTrue(variable, ownerRef, assertFn, file, function, line);
            return ref validation;
        }

        /// <summary>
        /// Checks if the specified reference is not null.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        public static ref Validation IsNotNull(
            this ref Validation validation, 
            string variable, 
            object variableRef, 
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            validation.PassIfNotNull(variable, variableRef, file, function, line);
            return ref validation;
        }

        /// <summary>
        /// Checks if the specified reference is not null.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        public static ref Validation IsNotNull(
            this ref Validation validation, 
            string variable, 
            UnityEngine.Object variableRef, 
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            validation.PassIfNotNull(variable, variableRef, file, function, line);
            return ref validation;
        }

        /// <summary>
        /// Checks if the specified reference is not null.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        public static ref Validation IsNotNull(
            this ref Validation validation, 
            string variable, 
            IEnumerable<object> variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            validation.PassIfNotNull(variable, variableRef, file, function, line);
            return ref validation;
        }

        /// <summary>
        /// Checks if the specified string is not null or empty.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        public static ref Validation IsNotEmpty(
            this ref Validation validation, 
            string variable, 
            string variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            validation.PassIfNotEmpty(variable, variableRef, file, function, line);
            return ref validation;
        }

        /// <summary>
        /// Checks if the specified collection is not empty.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        public static ref Validation IsNotEmpty(
            this ref Validation validation, 
            string variable, 
            IEnumerable<object> variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            validation.PassIfNotEmpty(variable, variableRef, file, function, line);
            return ref validation;
        }

        /// <summary>
        /// Checks if the specified collection contains no null items.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        public static ref Validation HasNoNulls(
            this ref Validation validation,
            string variable,
            IEnumerable<object> variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            validation.PassIfNoNullElements(variable, variableRef, file, function, line);
            return ref validation;
        }

        /// <summary>
        /// Checks if the specified string collection contains no null or empty items.
        /// </summary>
        /// <param name="variable"> The name of the variable being validated. </param>
        /// <param name="variableRef"> The reference being validated. </param>
        /// <param name="file"> The path to the file in which the validation is performed. </param>
        /// <param name="function"> The name of the function that performs the validation. </param>
        /// <param name="line"> The line at which the validation was performed. </param>
        public static ref Validation HasNoEmpties(
            this ref Validation validation,
            string variable,
            IEnumerable<string> variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            validation.PassIfNoEmptyStrings(variable, variableRef, file, function, line);
            return ref validation;
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
        public static ref Validation HasNoNulls(
            this ref Validation validation,
            string variable,
            IEnumerable<UnityEngine.Object> variableRef,
            [CallerFilePath] string file = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int line = 0)
        {
            validation.PassIfNoNullElements(variable, variableRef, file, function, line);
            return ref validation;
        }
    }
}