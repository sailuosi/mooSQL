using System.Linq.Expressions;

namespace mooSQL.linq.Linq
{
	using Mapping;
    using mooSQL.data;

    public interface IExpressionInfo
	{
		LambdaExpression GetExpression(DBInstance mappingSchema);
	}
}
