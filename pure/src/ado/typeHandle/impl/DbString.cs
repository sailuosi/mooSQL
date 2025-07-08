using System;
using System.Data;

namespace mooSQL.data
{
    /// <summary>
    /// This class represents a SQL string, it can be used if you need to denote your parameter is a Char vs VarChar vs nVarChar vs nChar
    /// </summary>
    public sealed class DbString : ICustomQueryParameter
    {
        /// <summary>
        /// Default value for IsAnsi.
        /// </summary>
        public static bool IsAnsiDefault { get; set; }

        /// <summary>
        /// A value to set the default value of strings
        /// going through Dapper. Default is 4000, any value larger than this
        /// field will not have the default value applied.
        /// </summary>
        public const int DefaultLength = 4000;

        /// <summary>
        /// Create a new DbString
        /// </summary>
        public DbString()
        {
            Length = -1;
            IsAnsi = IsAnsiDefault;
        }

        /// <summary>
        /// Create a new DbString
        /// </summary>
        public DbString(string value, int length = -1)
        {
            Value = value;
            Length = length;
            IsAnsi = IsAnsiDefault;
        }

        /// <summary>
        /// Ansi vs Unicode 
        /// </summary>
        public bool IsAnsi { get; set; }
        /// <summary>
        /// Fixed length 
        /// </summary>
        public bool IsFixedLength { get; set; }
        /// <summary>
        /// Length of the string -1 for max
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// The value of the string
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets a string representation of this DbString.
        /// </summary>
        public override string ToString() => Value is null
            ? $"Dapper.DbString (Value: null, Length: {Length}, IsAnsi: {IsAnsi}, IsFixedLength: {IsFixedLength})"
            : $"Dapper.DbString (Value: '{Value}', Length: {Length}, IsAnsi: {IsAnsi}, IsFixedLength: {IsFixedLength})";


    }
}
