using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;

namespace mooSQL.linq.Expressions
{
	using Common;
	using Common.Internal;
	using Extensions;

	using Linq;
	using Mapping;
    using mooSQL.data;
    using mooSQL.utils;
    using Reflection;
    using Tools;
    /// <summary>
    /// 从行读取中取出查询结果类的表达式
    /// </summary>
	sealed class ConvertFromDataReaderExpression : Expression
	{
		public ConvertFromDataReaderExpression(Type type, int idx, IValueConverter? converter, Expression dataReaderParam, bool? canBeNull)
		{
			_type            = type;
			Converter        = converter;
			CanBeNull        = canBeNull;
			_idx             = idx;
			_dataReaderParam = dataReaderParam;
		}

		// slow mode constructor
		public ConvertFromDataReaderExpression(Type type, int idx, IValueConverter? converter, Expression dataReaderParam, DBInstance dataContext)
			: this(type, idx, converter, dataReaderParam, (bool?)null)
		{
			_slowModeDataContext = dataContext;
		}

		readonly int            _idx;
		readonly Expression     _dataReaderParam;
		readonly Type           _type;
		readonly DBInstance?  _slowModeDataContext;

		public IValueConverter? Converter { get; }
		public bool?            CanBeNull { get; }

		public override Type           Type      => _type;
		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override bool           CanReduce => true;
		public          int            Index     => _idx;

		public override Expression Reduce()
		{
			return Reduce(_slowModeDataContext, true);
		}

		public Expression Reduce(DBInstance? dataContext, bool slowMode)
		{
			if (dataContext == null)
				return _dataReaderParam;

			var columnReader = new ColumnReader(dataContext,  _type, _idx, Converter, slowMode);

			if (slowMode && Configuration.OptimizeForSequentialAccess)
				return Convert(Call(Constant(columnReader), Methods.LinqToDB.ColumnReader.GetValueSequential, _dataReaderParam, Call(_dataReaderParam, Methods.ADONet.IsDBNull, ExpressionInstances.Int32Array(_idx)), Call(Methods.LinqToDB.ColumnReader.RawValuePlaceholder)), _type);
			else
				return Convert(Call(Constant(columnReader), Methods.LinqToDB.ColumnReader.GetValue, _dataReaderParam), _type);
		}

		public Expression Reduce(DBInstance dataContext, DbDataReader dataReader)
		{

			return GetColumnReader(dataContext,  dataReader, _type, Converter, _idx, _dataReaderParam, forceNullCheck: CanBeNull == true);
		}

		public Expression Reduce(DBInstance dataContext, DbDataReader dataReader, Expression dataReaderParam)
		{
			return GetColumnReader(dataContext, dataReader, _type, Converter, _idx, dataReaderParam, forceNullCheck: CanBeNull == true);
		}

		static Expression ConvertExpressionToType(Expression current, Type toType, DBInstance DB)
		{
			var toConvertExpression = GetConvertType(DB,current.Type, toType);

			if (toConvertExpression == null)
				return current;

			current = InternalExtensions.ApplyLambdaToExpression(toConvertExpression, current);

			return current;
		}
		public static Expression GetReaderExpression(DBInstance DB,DbDataReader reader, int idx, Expression readerExpression, Type? toType)
			 
		{
			var fieldType = reader.GetFieldType(idx);
			var providerType = reader.GetProviderSpecificFieldType(idx);
			var typeName = reader.GetDataTypeName(idx);

			if (fieldType == null)
			{
				var name = reader.GetName(idx);
				throw new LinqToDBException($"Can't create '{typeName}' type or '{providerType}' specific type for {name}.");
			}

			//typeName = NormalizeTypeName(typeName);
            var getValueMethodInfo = MemberHelper.MethodOf<DbDataReader>(r => r.GetValue(0));
            return Expression.Convert(
                Expression.Call(readerExpression, getValueMethodInfo, ExpressionInstances.Int32Array(idx)),
                fieldType);
        }

		public static LambdaExpression GetConvertType(DBInstance DB, Type src, Type to) {

			var func = DB.dialect.mapping.GetValueConverter(src, to);

			var para= Expression.Parameter(src, "p");
			var call = Expression.Call(
				Expression.Constant(func),func.Method,para
				);
			var res= Expression.Lambda(call, para);
			return res;
		}

            static Expression GetColumnReader(
			DBInstance dataContext, DbDataReader dataReader, Type type, IValueConverter? converter, int idx, Expression dataReaderExpr, bool forceNullCheck)
		{
			var toType = type.UnwrapNullable();

			Expression ex;
			Type? mapType = null;

			if (toType.IsEnum)
				mapType = ConvertBuilder.GetDefaultMappingFromEnumType(dataContext, toType);

			if (converter != null)
			{
				var expectedProvType = converter.FromProviderExpression.Parameters[0].Type;
				ex = GetReaderExpression(dataContext,dataReader, idx, dataReaderExpr, expectedProvType);
			}
			else
			{
				ex = GetReaderExpression(dataContext,dataReader, idx, dataReaderExpr, mapType?.UnwrapNullable() ?? toType);
			}

			if (ex.NodeType == ExpressionType.Lambda)
			{
				var l = (LambdaExpression)ex;

				switch (l.Parameters.Count)
				{
					case 1 : ex = l.GetBody(dataReaderExpr);                                 break;
					case 2 : ex = l.GetBody(dataReaderExpr, ExpressionInstances.Int32(idx)); break;
				}
			}

			if (converter != null)
			{
				// we have to prepare read expression to conversion
				//
				var expectedType = converter.FromProviderExpression.Parameters[0].Type;

				if (expectedType != ex.Type)
					ex = ConvertExpressionToType(ex, expectedType, dataContext);

				if (converter.HandlesNulls)
				{
					ex = Condition(
						Call(dataReaderExpr, Methods.ADONet.IsDBNull, ExpressionInstances.Int32Array(idx)),
						Constant(dataContext.dialect.mapping.GetDefaultValue(expectedType), expectedType),
						ex);
				}

				ex = InternalExtensions.ApplyLambdaToExpression(converter.FromProviderExpression, ex);
				if (toType != ex.Type && toType.IsAssignableFrom(ex.Type))
				{
					ex = Convert(ex, toType);
				}
			}
			else if (toType.IsEnum)
			{
				if (mapType != ex.Type)
				{
					// Use only defined convert
					var econv =
						GetConvertType(dataContext,ex.Type, type ) ;

					ex = InternalExtensions.ApplyLambdaToExpression(econv, ex);
				}
			}

			if (ex.Type != type)
				ex = ConvertExpressionToType(ex, type, dataContext)!;

			// Try to search postprocessing converter TType -> TType
			//
			ex = ConvertExpressionToType(ex, ex.Type, dataContext)!;

			// Add check null expression.
			// If converter handles nulls, do not provide IsNull check
			// Note: some providers may return wrong IsDBNullAllowed, so we enforce null check in slow mode. E.g.:
			// Microsoft.Data.SQLite
			// Oracle (group by columns)
			// MySql.Data and some other providers enforce null check in IsDBNullAllowed implementation
			if (converter?.HandlesNulls != true && (forceNullCheck || (dataContext.dialect.mapping.GetTypeNullable(type))))
			{
				ex = Condition(
					Call(dataReaderExpr, Methods.ADONet.IsDBNull, ExpressionInstances.Int32Array(idx)),
					Constant(dataContext.dialect.mapping.GetDefaultValue(type), type),
					ex);
			}

			return ex;
		}

		

		internal sealed class ColumnReader
		{
			public ColumnReader(DBInstance dataContext, Type columnType, int columnIndex, IValueConverter? converter, bool slowMode)
			{
				_dataContext   = dataContext;
				ColumnType     = columnType;
				ColumnIndex    = columnIndex;
				_converter     = converter;
				_slowMode      = slowMode;
			}

			/// <summary>
			/// This method is used as placeholder, which will be replaced with raw value variable.
			/// </summary>
			/// <returns></returns>
			public static object? RawValuePlaceholder() => throw new InvalidOperationException("Raw value placeholder replacement failed");

            /*
			 * 我们可以为相同的列使用不同的ColumnType类型的列阅读器，这将导致不同的阅读器表达式。
			 * 为了使它在顺序模式下工作，我们应该只执行一次从reader读取的实际列值，然后在所有类型的reader表达式中使用它。
			 * 为此，我们添加了额外的方法来读取原始值，然后将其传递给GetValueSequential。我们需要额外的方法，因为我们不能在字段中存储原始值：ColumnReader实例可以从多个线程中使用，所以它不能有状态。出于同样的原因，将mapper表达式中的ColumnReader实例数量减少到单个列的一个没有多大意义。如果我们看到它的好处，可以稍后再做，但坦率地说，优化慢模式阅读器没有意义
			 *
			 * 限制与非慢映射器相同：列映射表达式应该使用相同的读取器方法来获取列值。这个限制在GetRawValueSequential方法中强制执行。
			 */
            public object? GetValueSequential(DbDataReader dataReader, bool isNull, object? rawValue)
			{
				var fromType = dataReader.GetFieldType(ColumnIndex);

				if (!_slowColumnConverters.TryGetValue(fromType, out var func))
				{
					var dataReaderParameter = Parameter(typeof(DbDataReader));
					var isNullParameter     = Parameter(typeof(bool));
					var rawValueParameter   = Parameter(typeof(object));
					var dataReaderExpr      = Convert(dataReaderParameter, dataReader.GetType());

					var expr = GetColumnReader(_dataContext,  dataReader, ColumnType, _converter, ColumnIndex, dataReaderExpr, _slowMode);
					expr     = SequentialAccessHelper.OptimizeColumnReaderForSequentialAccess(expr, isNullParameter, rawValueParameter, ColumnIndex);

					var lex  = Lambda<Func<bool, object?, object?>>(
						expr.Type == typeof(object) ? expr : Convert(expr, typeof(object)),
						isNullParameter,
						rawValueParameter);

					_slowColumnConverters[fromType] = func = lex.CompileExpression();
				}

				try
				{
					return func(isNull, rawValue);
				}
				catch (Exception ex)
				{
					var name = dataReader.GetName(ColumnIndex);
					throw new Exception(
							$"Mapping of column '{name}' value failed, see inner exception for details", ex);
				}
			}

			public object GetRawValueSequential(DbDataReader dataReader, Type[] forTypes)
			{
				var fromType = dataReader.GetFieldType(ColumnIndex);

				if (!_slowRawReaders.TryGetValue(fromType, out var func))
				{
					var dataReaderParameter = Parameter(typeof(DbDataReader));
					var dataReaderExpr      = Convert(dataReaderParameter, dataReader.GetType());

					MethodCallExpression rawExpr = null!;
					foreach (var type in forTypes)
					{
						var expr           = GetColumnReader(_dataContext,  dataReader, type, _converter, ColumnIndex, dataReaderExpr, _slowMode);
						var currentRawExpr = SequentialAccessHelper.ExtractRawValueReader(expr, ColumnIndex);

						if (rawExpr == null)
							rawExpr = currentRawExpr;
						else if (rawExpr.Method != currentRawExpr.Method)
							throw new Exception(
								$"Different data reader methods used for same column: '{rawExpr.Method.DeclaringType?.Name}.{rawExpr.Method.Name}' vs '{currentRawExpr.Method.DeclaringType?.Name}.{currentRawExpr.Method.Name}'");

					}

					var lex  = Lambda<Func<DbDataReader, object>>(
						rawExpr.Type == typeof(object) ? rawExpr : Convert(rawExpr, typeof(object)),
						dataReaderParameter);

					_slowRawReaders[fromType] = func = lex.CompileExpression();
				}

				return func(dataReader);
			}

			public object? GetValue(DbDataReader dataReader)
			{
				var fromType = dataReader.GetFieldType(ColumnIndex);

				if (!_columnConverters.TryGetValue(fromType, out var func))
				{
					var parameter      = Parameter(typeof(DbDataReader));
					var dataReaderExpr = Convert(parameter, dataReader.GetType());

					var expr = GetColumnReader(_dataContext,  dataReader, ColumnType, _converter, ColumnIndex, dataReaderExpr, _slowMode);

					var lex  = Lambda<Func<DbDataReader, object>>(
						expr.Type == typeof(object) ? expr : Convert(expr, typeof(object)),
						parameter);

					_columnConverters[fromType] = func = lex.CompileExpression();
				}

				try
				{
					return func(dataReader);
				}
				catch (Exception ex)
				{
					var name = dataReader.GetName(ColumnIndex);
					throw new Exception(
							$"Mapping of column '{name}' value failed, see inner exception for details", ex);
				}
			}

			readonly ConcurrentDictionary<Type, Func<DbDataReader, object?>>  _columnConverters     = new ();
			readonly ConcurrentDictionary<Type, Func<bool, object?, object?>> _slowColumnConverters = new ();
			readonly ConcurrentDictionary<Type, Func<DbDataReader, object>>   _slowRawReaders       = new ();

			readonly DBInstance     _dataContext;

			readonly IValueConverter? _converter;
			readonly bool             _slowMode;

			public int  ColumnIndex { get; }
			public Type ColumnType  { get; }
		}

		public override string ToString()
		{
			var result = $"ConvertFromDataReaderExpression<{_type.Name}>({_idx})";
			if (CanBeNull == true || Type.IsNullable())
				result += "?";
			return result;
		}

		public ConvertFromDataReaderExpression MakeNullable()
		{
			if (!Type.IsReferType())
			{
				var type = ReflectionExtensions.WrapNullable(Type);
				return new ConvertFromDataReaderExpression(type, _idx, Converter, _dataReaderParam, true);
			}

			return this;
		}

		public ConvertFromDataReaderExpression MakeNotNullable()
		{
			if (Type.IsNullable())
			{
				var type = Type.GetGenericArguments()[0];
				return new ConvertFromDataReaderExpression(type, _idx, Converter, _dataReaderParam, (bool?)null);
			}

			return this;
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitConvertFromDataReaderExpression(this);
			return base.Accept(visitor);
		}

	}
}
