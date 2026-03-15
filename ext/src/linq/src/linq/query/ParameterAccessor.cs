using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{


	using System.Linq.Expressions;
    using mooSQL.data;
    using mooSQL.data.model;

	/// <summary>
	/// 参数访问器
	/// </summary>
	sealed class ParameterAccessor
	{
		public ParameterAccessor(
			Func<Expression, DBInstance?, object?[]?, object?> valueAccessor,
			Func<object?, object?>? itemAccessor,
			Func<Expression, object?, DBInstance?, object?[]?, DbDataType> dbDataTypeAccessor,
            ParameterWord sqlParameter)
		{
			ValueAccessor = valueAccessor;
			ItemAccessor = itemAccessor;
			DbDataTypeAccessor = dbDataTypeAccessor;
			SqlParameter = sqlParameter;
		}

		public readonly Func<Expression, DBInstance?,object?[]?,object?>             ValueAccessor;
		public readonly Func<object?,object?>?                                        ItemAccessor;
		public readonly Func<Expression,object?, DBInstance?,object?[]?,DbDataType>  DbDataTypeAccessor;
		public readonly ParameterWord                                                  SqlParameter;
#if DEBUG
		public Expression<Func<Expression,DBInstance?,object?[]?,object?>>? AccessorExpr;
		public Expression<Func<object?,object?>>?                             ItemAccessorExpr;
#endif
	}
}
