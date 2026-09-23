using System.Linq.Expressions;

namespace mooSQL.linq
{
	using mooSQL.linq.mapping;
    using mooSQL.data;

    public interface IExpressionInfo
	{
		LambdaExpression GetExpression(DBInstance mappingSchema);
	}
}
