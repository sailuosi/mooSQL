using System;
using System.Linq.Expressions;

namespace mooSQL.linq
{
	using mooSQL.linq.expressions;
	using mooSQL.linq.builder;
    using mooSQL.data.model;
    using mooSQL.linq.clause;
	public partial class DbFunc
	{
		/// <summary>
		/// SqlIDType
		/// </summary>
		/// <summary>
		/// SqlIDType
		/// </summary>
		public enum SqlIDType
		{
			/// <summary>
			/// TableAlias
			/// </summary>
			/// <summary>
			/// TableAlias
			/// </summary>
			TableAlias,
			/// <summary>
			/// TableName
			/// </summary>
			/// <summary>
			/// TableName
			/// </summary>
			TableName,
			/// <summary>
			/// TableSpec
			/// </summary>
			/// <summary>
			/// TableSpec
			/// </summary>
			TableSpec
		}
		/// <summary>
		/// SqlID
		/// </summary>

		public readonly struct SqlID : IToSqlConverter, IEquatable<SqlID>
		{
			/// <summary>
			/// Type
			/// </summary>
			/// <summary>
			/// Gets or sets Type.
			/// </summary>
			public SqlIDType Type { get; }
			/// <summary>
			/// ID
			/// </summary>
			/// <summary>
			/// Gets or sets ID.
			/// </summary>
			public string    ID   { get; }
			/// <summary>
			/// 初始化 <see cref="SqlID"/> 实例。
			/// </summary>
			public SqlID(SqlIDType type, string id)
			{
				ID   = id;
				Type = type;
			}
/// <summary>
/// ToString
/// </summary>
	

			public override string ToString()
			{
				return $"{Type}:{ID}";
			}
/// <summary>
/// Equals
/// </summary>
	

			public override bool Equals(object? obj)
			{
				return obj is SqlID other && Equals(other);
			}
			/// <summary>
			/// Equals
			/// </summary>

			public bool Equals(SqlID other)
			{
				return Type == other.Type && ID == other.ID;
			}
/// <summary>
/// Gets GetHashCode.
/// </summary>
	

			public override int GetHashCode()
			{
				return (int)Type | (ID.GetHashCode() >> 3);
			}
			/// <summary>
			/// ToSql
			/// </summary>

			public IExpWord ToSql(object value)
			{
				return new ValueWord(typeof(SqlID), value);
			}
			/// <summary>
			/// Parse
			/// </summary>

			public static SqlID Parse(string value)
			{
				var idx = value.IndexOf(':');

				if (idx == -1)
					throw new InvalidOperationException($"Cannot parse '{value}' to SqlID.");

				var type = value.Substring(0, idx);
				var id   = value.Substring(idx + 1);

#pragma warning disable CA2263 // Prefer generic overload when type is known
				return new ((SqlIDType)Enum.Parse(typeof(SqlIDType), type), id);
#pragma warning restore CA2263 // Prefer generic overload when type is known
			}
		}
		/// <summary>
		/// TableAlias
		/// </summary>

		public static SqlID TableAlias(string id)
		{
			return new(SqlIDType.TableAlias, id);
		}
		/// <summary>
		/// TableName
		/// </summary>

		public static SqlID TableName(string id)
		{
			return new(SqlIDType.TableName, id);
		}
		/// <summary>
		/// TableSpec
		/// </summary>

		public static SqlID TableSpec(string id)
		{
			return new(SqlIDType.TableSpec, id);
		}
	}
}
