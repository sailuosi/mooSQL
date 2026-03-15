using mooSQL.data;
using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Expressions
{
	/// <summary>
	/// 包含预先创建好的常用常量表达式。
	/// </summary>
	public static class ExpressionInstances
	{
		public static readonly Expression NullIDataContext = Expression.Constant(null, typeof(DBInstance));
		public static readonly Expression HashMultiplier   = Expression.Constant(-1521134295);
		public static readonly Expression EmptyTypes       = Expression.Constant(Type.EmptyTypes);
		
		public static readonly ConstantExpression True        = Expression.Constant(true);
		public static readonly ConstantExpression False       = Expression.Constant(false);
		public static readonly ConstantExpression UntypedNull = Expression.Constant(null);

		public static readonly ConstantExpression Constant0  = Expression.Constant(0);
		public static readonly ConstantExpression Constant1  = Expression.Constant(1);

		// those constants used by linq2db but not covered by _int32Constants array
		public static readonly ConstantExpression Constant26  = Expression.Constant(26);
		public static readonly ConstantExpression Constant29  = Expression.Constant(29);

		static readonly Expression[] _int32Constants =
		new Expression[] {
			Constant0,
			Constant1,
			Expression.Constant(2),
			Expression.Constant(3),
			Expression.Constant(4),
			Expression.Constant(5),
			Expression.Constant(6),
			Expression.Constant(7),
			Expression.Constant(8),
			Expression.Constant(9),
			Expression.Constant(10)
		};

		static readonly Expression[][] _int32Length1Arrays =
		new Expression[][] {
			new Expression[]{_int32Constants[0] },
            new Expression[]{_int32Constants[1] },
            new Expression[]{_int32Constants[2] },
            new Expression[]{_int32Constants[3] },
            new Expression[]{_int32Constants[4] },
            new Expression[]{_int32Constants[5] },
            new Expression[]{_int32Constants[6] },
            new Expression[]{_int32Constants[7] },
            new Expression[]{_int32Constants[8] },
            new Expression[]{_int32Constants[9] },
            new Expression[]{_int32Constants[10] }
		};

		// integer constants with 0+ values used a lot for indexes (e.g. in array access and data reader expressions)
		internal static Expression Int32(int value)
		{
			return value >= 0 && value < _int32Constants.Length
				? _int32Constants[value]
				: Expression.Constant(value);
		}

		internal static Expression[] Int32Array(int value)
		{
			return value >= 0 && value < _int32Constants.Length
				? _int32Length1Arrays[value]
				: new Expression[] { Expression.Constant(value) };
		}

		internal static ConstantExpression Boolean(bool value) => value ? True : False;
	}
}
