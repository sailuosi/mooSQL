using System;
using System.Diagnostics.CodeAnalysis;


namespace mooSQL.linq
{
	using Data;
	using DataProvider;
	using Expressions;

	using mooSQL.data;

	/// <summary>
	/// Contains extension methods for LINQ queries.
	/// </summary>

	public static class TableExtensions
	{
		#region Table Helpers

		/// <summary>
		/// Overrides IsTemporary flag for the current table. This call will have effect only for databases that support
		/// temporary tables.
		/// <para>Supported by: DB2, Oracle, PostgreSQL, Informix, SQL Server, Sybase ASE.</para>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="isTemporary">If true, the current tables will handled as a temporary table.</param>
		/// <returns>Table-like query source with new owner/schema name.</returns>
		
		
		public static ITable<T> IsTemporary<T>(this ITable<T> table, [SqlQueryDependent] bool isTemporary)
			where T : notnull
		{
			return ((ITableMutable<T>)table).ChangeTableOptions(isTemporary
				? table.TableOptions | data.TableOptions.IsTemporary
				: table.TableOptions & ~data.TableOptions.IsTemporary);
		}

		/// <summary>
		/// Overrides IsTemporary flag for the current table. This call will have effect only for databases that support
		/// temporary tables.
		/// <para>Supported by: DB2, Oracle, PostgreSQL, Informix, SQL Server, Sybase ASE.</para>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <returns>Table-like query source with new owner/schema name.</returns>
		
		
		public static ITable<T> IsTemporary<T>(this ITable<T> table)
			where T : notnull
		{
			return ((ITableMutable<T>)table).ChangeTableOptions(table.TableOptions | data.TableOptions.IsTemporary);
		}

		/// <summary>
		/// Overrides TableOptions value for the current table. This call will have effect only for databases that support
		/// the options.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="options"><see cref="TableOptions"/> value.</param>
		/// <returns>Table-like query source with new owner/schema name.</returns>
		
		
		public static ITable<T> TableOptions<T>(this ITable<T> table, [SqlQueryDependent] TableOptions options)
			where T : notnull
		{
			return ((ITableMutable<T>)table).ChangeTableOptions(options);
		}



		#endregion




	}
}
