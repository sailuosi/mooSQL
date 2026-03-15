using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
    using mooSQL.data.model;
    using SqlQuery;

	public interface IToSqlConverter
	{
		IExpWord ToSql(object value);
	}
}
