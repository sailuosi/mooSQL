using System;
using System.Diagnostics.CodeAnalysis;


namespace mooSQL.linq
{
	using Data;
	using DataProvider;
	using Expressions;

	using mooSQL.data;

	/// <summary>
	/// Contains extension methods for DbQuery sources.
	/// </summary>

	public static class DbQueryExtensions
	{
		#region Table Helpers

		public static IDbQuery<T> IsTemporary<T>(this IDbQuery<T> table, [SqlQueryDependent] bool isTemporary)
			where T : notnull
		{
			return ((IDbQueryMutable<T>)table).ChangeTableOptions(isTemporary
				? table.TableOptions | data.TableOptions.IsTemporary
				: table.TableOptions & ~data.TableOptions.IsTemporary);
		}

		public static IDbQuery<T> IsTemporary<T>(this IDbQuery<T> table)
			where T : notnull
		{
			return ((IDbQueryMutable<T>)table).ChangeTableOptions(table.TableOptions | data.TableOptions.IsTemporary);
		}

		public static IDbQuery<T> TableOptions<T>(this IDbQuery<T> table, [SqlQueryDependent] TableOptions options)
			where T : notnull
		{
			return ((IDbQueryMutable<T>)table).ChangeTableOptions(options);
		}

		#endregion
	}
}
