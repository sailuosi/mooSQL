using System;
using System.Globalization;

namespace mooSQL.linq.utils
{
	/// <summary>
	/// 表示原始 SQL 查询字符串；用于与插值 SQL 重载区分。
	/// </summary>
	public readonly struct RawSqlString
	{
		/// <summary>
		///     Implicitly converts a <see cref="string" /> to a <see cref="RawSqlString" />
		/// </summary>
		/// <param name="s"> The string. </param>
		public static implicit operator RawSqlString(string s) => new RawSqlString(s);

		/// <summary>
		///     Implicitly converts a <see cref="FormattableString" /> to a <see cref="RawSqlString" />
		/// </summary>
		/// <param name="fs"> The string format. </param>
		public static implicit operator RawSqlString(FormattableString fs) => default;

		/// <summary>
		///     Constructs a <see cref="RawSqlString" /> from a <see cref="string" />
		/// </summary>
		/// <param name="s"> The string. </param>
		public RawSqlString(string s) => Format = s;

		/// <summary>
		///     The string format.
		/// </summary>
		public string Format { get; }
	}
}
