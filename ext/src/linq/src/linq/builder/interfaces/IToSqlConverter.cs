using System.Linq.Expressions;

namespace mooSQL.linq.builder
{
    using mooSQL.data.model;
    using mooSQL.linq.clause;
	public interface IToSqlConverter
	{
		IExpWord ToSql(object value);
	}
}
