#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace mooSQL.data.taos
{
    /// <summary>
    /// Arguments type.
    /// </summary>
    /// <summary>
    /// Arguments type.
    /// </summary>
    public static class Arguments
    {
        private const string DurationPattern = "([-+]?)([0-9]+(\\\\.[0-9]*)?[a-z]+)+|inf|-inf";
        /// <summary>
        /// CheckNonEmptyString
        /// </summary>

        public static void CheckNonEmptyString(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Expecting a non-empty string for " + name);
            }
        }
        /// <summary>
        /// CheckDuration
        /// </summary>

        public static void CheckDuration(string value, string name)
        {
            if (string.IsNullOrEmpty(value) || !Regex.Match(value, "([-+]?)([0-9]+(\\\\.[0-9]*)?[a-z]+)+|inf|-inf").Success)
            {
                throw new ArgumentException("Expecting a duration string for " + name + ". But got: " + value);
            }
        }
        /// <summary>
        /// CheckPositiveNumber
        /// </summary>

        public static void CheckPositiveNumber(int number, string name)
        {
            if (number <= 0)
            {
                throw new ArgumentException("Expecting a positive number for " + name);
            }
        }
        /// <summary>
        /// CheckNotNegativeNumber
        /// </summary>

        public static void CheckNotNegativeNumber(int number, string name)
        {
            if (number < 0)
            {
                throw new ArgumentException("Expecting a positive or zero number for " + name);
            }
        }
        /// <summary>
        /// CheckNotNull
        /// </summary>

        public static void CheckNotNull(object obj, string name)
        {
            if (obj == null)
            {
                throw new NullReferenceException("Expecting a not null reference for " + name);
            }
        }
        /// <summary>
        /// CheckNotEmpty&lt;T&gt;
        /// </summary>
        /// <summary>
        /// CheckNotEmpty&lt;T&gt;
        /// </summary>
        public static void CheckNotEmpty<T>(IEnumerable<T> obj, string name)
        {
            if (obj == null || obj.Count()==0)
            {
                throw new ArgumentException("Expecting a  empyt  for " + name);
            }
        }
    }
}
#endif
