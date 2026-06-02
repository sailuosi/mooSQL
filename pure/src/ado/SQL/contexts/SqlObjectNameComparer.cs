using System.Collections.Generic;



namespace mooSQL.data.model
{
	/// <summary>
	/// SQL 对象名比较器（用于排序/去重）。
	/// </summary>
	public sealed class SqlObjectNameComparer : IComparer<SqlObjectName>
	{
		/// <summary>
		/// 单例比较器实例。
		/// </summary>
		public static readonly IComparer<SqlObjectName> Instance = new SqlObjectNameComparer();

		private SqlObjectNameComparer()
		{
		}

		int IComparer<SqlObjectName>.Compare(SqlObjectName x, SqlObjectName y)
		{
			var res = string.CompareOrdinal(x.Server, y.Server);
			if (res != 0) return res;
			res = string.CompareOrdinal(x.Database, y.Database);
			if (res != 0) return res;
			res = string.CompareOrdinal(x.Schema, y.Schema);
			if (res != 0) return res;
			res = string.CompareOrdinal(x.Package, y.Package);
			if (res != 0) return res;
			return string.CompareOrdinal(x.Name, y.Name);
		}
	}
}