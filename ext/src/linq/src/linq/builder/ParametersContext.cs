using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
	using Common;
	using Data;
	using Extensions;
	using mooSQL.linq.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using mooSQL.data.model;
	using mooSQL.data;
    using mooSQL.linq.ext;
    using mooSQL.utils;
    using mooSQL.data.mapping;

    sealed class ParametersContext
	{
		readonly object?[]? _parameterValues;

		public ParametersContext(Expression parametersExpression, object?[]? parameterValues, ExpressionTreeOptimizationContext optimizationContext, DBInstance dataContext)
		{
			_parameterValues     = parameterValues;
			ParametersExpression = parametersExpression;
			OptimizationContext  = optimizationContext;
			DBLive          = dataContext;

			_expressionAccessors = parametersExpression.GetExpressionAccessors(ExpressionBuilder.ExpressionParam);
		}

		public Expression                        ParametersExpression { get; }
		public ExpressionTreeOptimizationContext OptimizationContext  { get; }
		public DBInstance DBLive {  get; }


		static readonly ParameterExpression ItemParameter = Expression.Parameter(typeof(object));

		static readonly ParameterExpression[] AccessorParameters =
		{
			ExpressionBuilder.ExpressionParam,
			ExpressionConstants.DataContextParam,
			ExpressionBuilder.ParametersParam
		};

		static ParameterExpression[] DbTypeAccessorParameters =
		{
			ExpressionBuilder.ExpressionParam,
			ItemParameter,
			ExpressionConstants.DataContextParam,
			ExpressionBuilder.ParametersParam
		};

		public readonly List<ParameterAccessor>           CurrentSqlParameters = new();
		readonly        Dictionary<Expression,Expression> _expressionAccessors;

		#region Build Parameter

		internal List<(Expression Expression, EntityColumn? Column, ParameterAccessor Accessor)>? _parameters;

		internal List<(Func<Expression, DBInstance?, object?[]?, object?> main, Func<Expression, DBInstance?, object?[]?, object?> substituted)>? _parametersDuplicateCheck;

        internal Dictionary<Expression, (Expression used, DBInstance DB, Func<DBInstance, Expression> accessorFunc)>? _dynamicDBAccessors;
        internal void RegisterDuplicateParameter(Expression expression, Func<Expression, DBInstance?, object?[]?, object?> mainAccessor, Func<Expression, DBInstance?, object?[]?, object?> substitutedAccessor)
		{
			_parametersDuplicateCheck ??= new();

			_parametersDuplicateCheck.Add((mainAccessor, substitutedAccessor));
		}


        public Expression RegisterDynamicExpressionAccessor(Expression forExpression, DBInstance DB, Func< DBInstance, Expression> accessorFunc)
        {
            var result = accessorFunc(DB);

            _dynamicDBAccessors ??= new(ExpressionEqualityComparer.Instance);

            if (!_dynamicDBAccessors.ContainsKey(forExpression))
                _dynamicDBAccessors.Add(forExpression, (result, DB, accessorFunc));

            return result;
        }

        internal void AddCurrentSqlParameter(ParameterAccessor parameterAccessor)
		{
			var idx = CurrentSqlParameters.Count;
			CurrentSqlParameters.Add(parameterAccessor);
			parameterAccessor.SqlParameter.AccessorId = idx;
		}

		internal enum BuildParameterType
		{
			Default,
			Bool,
			InPredicate
		}

		public ParameterAccessor? BuildParameter(
			IBuildContext?     context,
			Expression         expr,
            EntityColumn?  columnDescriptor,
			bool               forceConstant           = false,
			bool               doNotCheckCompatibility = false,
			bool               forceNew                = false,
			string?            alias                   = null,
			BuildParameterType buildParameterType      = BuildParameterType.Default)
		{
			string? name = alias;

			var newExpr = ReplaceParameter(context?.Builder.DBLive, expr, columnDescriptor, forceConstant, nm => name = nm);

			var newAccessor = PrepareConvertersAndCreateParameter(newExpr, expr, name, columnDescriptor, doNotCheckCompatibility, buildParameterType);
			if (newAccessor == null)
				return null;

			// do replacing again for registering parameterized constants
			ApplyAccessors(expr, true);

			if (!forceNew && !ReferenceEquals(newExpr.ValueExpression, expr))
			{
				// check that expression is not just compilable expression
				var hasAccessors = HasAccessors(expr);

				// we can find duplicates in this case
				if (hasAccessors)
				{
					var found = newAccessor;

					if (_parameters != null)
					{
						foreach (var (paramExpr, column, accessor) in _parameters)
						{
							// build
							if (!accessor.SqlParameter.Type.Equals(newAccessor.SqlParameter.Type))
								continue;

							// we cannot merge parameters if they have defined ValueConverter
							if (column != null && columnDescriptor != null)
							{
								if (!ReferenceEquals(column, columnDescriptor))
								{
									//if (column.ValueConverter != null || columnDescriptor.ValueConverter != null)
									//	continue;
								}
							}
							else if (!ReferenceEquals(column, columnDescriptor))
								continue;

							if (ReferenceEquals(paramExpr, expr))
							{
								// Its is the same already created parameter
								return accessor;
							}

							if (paramExpr.EqualsTo(expr, OptimizationContext.GetSimpleEqualsToContext(true)))
							{
								found = accessor;
								break;
							}
						}
					}

					// We already have registered parameter for the same expression
					//
					if (!ReferenceEquals(found, newAccessor))
					{
						// registers duplicate parameter check for expression cache
						RegisterDuplicateParameter(expr, found.ValueAccessor, newAccessor.ValueAccessor);
						return found;
					}
				}
			}

			(_parameters ??= new()).Add((expr, columnDescriptor, newAccessor));
			AddCurrentSqlParameter(newAccessor);

			return newAccessor;
		}

		static bool HasDbMapping(DBInstance mappingSchema, Type testedType, out LambdaExpression? convertExpr)
		{
            convertExpr = null;
            //if (mappingSchema.IsScalarType(testedType))
            //{
            //	convertExpr = null;
            //	return true;
            //}

            //convertExpr = mappingSchema.GetConvertExpression(testedType, typeof(DataParameter), false, false);

            //if (convertExpr != null)
            //	return true;

            //if (testedType == typeof(object))
            //	return false;

            //var dataType = mappingSchema.GetDataType(testedType);
            //if (dataType.Type.DataType != DataType.Undefined)
            //	return true;

            var notNullable = testedType.UnwrapNullable();

			if (notNullable != testedType)
				return HasDbMapping(mappingSchema, notNullable, out convertExpr);

			// TODO: Workaround, wee need good TypeMapping approach
			if (testedType.IsArray)
			{
				convertExpr = null;
				return HasDbMapping(mappingSchema, testedType.GetElementType()!, out _);
			}

			//if (!testedType.IsEnum)
			//	return false;

			//var defaultMapping = Converter.GetDefaultMappingFromEnumType(mappingSchema, testedType);
			//if (defaultMapping != null && defaultMapping != testedType)
			//	return HasDbMapping(mappingSchema, defaultMapping, out convertExpr);

			//var enumDefault = null; //mappingSchema.GetDefaultFromEnumType(testedType);
			//if (enumDefault != null && enumDefault != testedType)
			//	return HasDbMapping(mappingSchema, enumDefault, out convertExpr);

			return false;
		}

		ParameterAccessor? PrepareConvertersAndCreateParameter(ValueTypeExpression newExpr, Expression valueExpression, string? name, EntityColumn? columnDescriptor, bool doNotCheckCompatibility, BuildParameterType buildParameterType)
		{
			if (valueExpression.Type == typeof(void))
				return null;

			Type? elementType     = null;
			var   isParameterList = buildParameterType == BuildParameterType.InPredicate;

			if (isParameterList)
			{
				elementType = newExpr.ValueExpression.Type.GetItemType()
					?? newExpr.ValueExpression.UnwrapConvert().Type.GetItemType()
					?? columnDescriptor?.UnderType
					?? typeof(object);
			}

			var originalAccessor = newExpr.ValueExpression;
			var valueType        = elementType ?? newExpr.ValueExpression.Type;
			var valueGetter      = isParameterList ? ItemParameter : newExpr.ValueExpression;

			if (!newExpr.IsDataParameter)
			{
				if (columnDescriptor != null && originalAccessor is not BinaryExpression)
				{
					newExpr.DataType = columnDescriptor.DbType;

					if (valueType != columnDescriptor.UnderType)
					{
						var memberType = columnDescriptor.UnderType;
						var noConvert  = valueGetter.UnwrapConvert();

						if (noConvert.Type != typeof(object))
							valueGetter = noConvert;
						else if (!isParameterList && valueGetter.Type != valueExpression.Type)
							valueGetter = Expression.Convert(noConvert, valueExpression.Type);
						else if (valueGetter.Type == typeof(object))
							valueGetter = Expression.Convert(noConvert, elementType != null && elementType != typeof(object) ? elementType : memberType);

						if (valueGetter.Type != memberType
							&& !(valueGetter.Type.IsNullable() && valueGetter.Type.UnwrapNullable() == memberType.UnwrapNullable()))
						{
							if (memberType.IsValueType ||
								!memberType.IsSameOrParentOf(valueGetter.Type))
							{
								var convertLambda = TypeConverterUtil.GetConvertType(DBLive,valueGetter.Type, memberType);
								if (convertLambda != null)
								{
									valueGetter = InternalExtensions.ApplyLambdaToExpression(convertLambda, valueGetter);
								}
							}

							if (valueGetter.Type.IsNullable() && valueGetter.Type.UnwrapNullable() != memberType)
							{
								var convertLambda = TypeConverterUtil.GetConvertType(DBLive,valueGetter.Type, memberType);
								valueGetter = InternalExtensions.ApplyLambdaToExpression(convertLambda, valueGetter);
							}

							if (valueGetter.Type != memberType)
							{
								valueGetter = Expression.Convert(valueGetter, memberType);
							}
						}
					}
					else if (valueType != valueGetter.Type)
					{
						valueGetter = Expression.Convert(valueGetter, valueType);
					}

					//valueGetter = columnDescriptor.ApplyConversions(valueGetter, newExpr.DataType, true);

					newExpr.DbDataTypeExpression = Expression.Constant(newExpr.DataType);

					if (name == null)
					{
						if (columnDescriptor.PropertyName.Contains("."))
							name = columnDescriptor.PropertyName;
						else
							name = columnDescriptor.PropertyName;

					}
				}
				else
				{
					if (buildParameterType == BuildParameterType.Bool)
					{
						// right now, do nothing
					}
					else
					{
						// Try GetConvertExpression<.., DataParameter>() first.
						//
						if (valueGetter.Type != typeof(DataParameter))
						{
							LambdaExpression? convertExpr = null;
							if (buildParameterType == BuildParameterType.Default
								&& !HasDbMapping(DBLive, valueGetter.Type, out convertExpr))
							{
								if (!doNotCheckCompatibility)
									return null;
							}

							valueGetter = InternalExtensions.ApplyLambdaToExpression(convertExpr, valueGetter);
								//: ColumnDescriptor.ApplyConversions(MappingSchema, valueGetter, newExpr.DataType, null, true);
						}
						else
						{
							//valueGetter = ColumnDescriptor.ApplyConversions(MappingSchema, valueGetter, newExpr.DataType, null, true);
						}
					}
				}
			}

			if (typeof(DataParameter).IsSameOrParentOf(valueGetter.Type))
			{
				newExpr.DbDataTypeExpression = Expression.Property(valueGetter, Methods.LinqToDB.DataParameter.DbDataType);

				if (columnDescriptor != null)
				{
					var dbDataType = columnDescriptor.DbType;//.GetDbDataType(false);
					newExpr.DbDataTypeExpression = Expression.Call(Expression.Constant(dbDataType),
						LinqExtensions.WithSetValuesMethodInfo, newExpr.DbDataTypeExpression);
				}

				valueGetter = Expression.Property(valueGetter, Methods.LinqToDB.DataParameter.Value);
			}

			if (!isParameterList)
				newExpr.ValueExpression = valueGetter;

			name ??= columnDescriptor?.PropertyName;

			var p = CreateParameterAccessor(
				DBLive, newExpr.ValueExpression, isParameterList ? valueGetter : null, newExpr.DbDataTypeExpression, valueExpression, ParametersExpression, name);

			return p;
		}

		sealed class ValueTypeExpression
		{
			public Expression ValueExpression      = null!;
			public Expression DbDataTypeExpression = null!;

			public bool       IsDataParameter;
			public DbDataType DataType;
		}

		public Expression ApplyAccessors(Expression expression, bool register)
		{
			var result = expression.Transform(
				(1, paramContext : this, register),
				static (context, expr) =>
				{
					// TODO: !!! Code should be synched with ReplaceParameter !!!
					if (expr.NodeType == ExpressionType.ArrayIndex && ((BinaryExpression)expr).Left == ExpressionBuilder.ParametersParam)
					{
						return new TransformInfo(expr, true);
					}

					if (expr.NodeType == ExpressionType.Constant && context.paramContext.GetAccessorExpression(expr, out var accessor, context.register))
					{
						if (accessor.Type != expr.Type)
							accessor = Expression.Convert(accessor, expr.Type);

						return new TransformInfo(accessor);
					}

					return new TransformInfo(expr);
				});

			return result;
		}

		public bool HasAccessors(Expression expression)
		{
			var result = false;
			expression.Transform(
				(1, paramContext : this),
				(context, expr) =>
				{
					// TODO: !!! Code should be synched with ReplaceParameter !!!
					if (expr.NodeType == ExpressionType.ArrayIndex && ((BinaryExpression)expr).Left == ExpressionBuilder.ParametersParam)
					{
						return new TransformInfo(expr, true);
					}

					if (expr.NodeType == ExpressionType.Constant && context.paramContext.GetAccessorExpression(expr, out var _, false))
					{
						result = true;
					}

					return new TransformInfo(expr, result);
				});

			return result;
		}

		ValueTypeExpression ReplaceParameter(DBInstance mappingSchema, Expression expression, EntityColumn? columnDescriptor, bool forceConstant, Action<string>? setName)
		{
			var result = new ValueTypeExpression
			{
				DataType             = columnDescriptor?.DbType ?? new DbDataType(expression.Type),
				DbDataTypeExpression = Expression.Constant(mappingSchema.dialect.mapping.GetDbDataType(expression.UnwrapConvertToObject().Type), typeof(DbDataType)),
			};

			var unwrapped = expression.Unwrap();
			if (unwrapped.NodeType == ExpressionType.MemberAccess)
			{
				var ma = (MemberExpression)unwrapped;
				setName?.Invoke(ma.Member.Name);
			}

			result.ValueExpression = expression.Transform(
				(forceConstant, columnDescriptor, (expression as MemberExpression)?.Member, result, setName, paramContext: this),
				static (context, expr) =>
				{
					if (expr.NodeType == ExpressionType.ArrayIndex && ((BinaryExpression)expr).Left == ExpressionBuilder.ParametersParam)
					{
						return new TransformInfo(expr, true);
					}

					if (expr.NodeType == ExpressionType.Constant)
					{
						var exprType = expr.Type;
						if (context.paramContext.GetAccessorExpression(expr, out var val, false))
						{
							var constantValue = ((ConstantExpression)expr).Value;

							expr = Expression.Convert(val, exprType);

							if (constantValue is DataParameter dataParameter)
							{
								context.result.IsDataParameter = true;
								context.result.DataType        = dataParameter.DbDataType;
								var dataParamExpr = Expression.Convert(expr, typeof(DataParameter));
								context.result.DbDataTypeExpression = Expression.Property(dataParamExpr, nameof(DataParameter.DbDataType));

								expr = Expression.Property(dataParamExpr, nameof(DataParameter.Value));
							}
							else if (context.Member != null)
							{
								if (context.columnDescriptor == null)
								{
									var mt = ExpressionBuilder.GetMemberDataType(context.paramContext.DBLive, context.Member);

									if (mt.DataType != DataFam.Undefined)
									{
										context.result.DataType             = context.result.DataType.WithDataType(mt.DataType);
										context.result.DbDataTypeExpression = Expression.Constant(mt);
									}

									if (mt.DbType != null)
									{
										context.result.DataType             = context.result.DataType.WithDbType(mt.DbType);
										context.result.DbDataTypeExpression = Expression.Constant(mt);
									}

									if (mt.Length != null)
									{
										context.result.DataType             = context.result.DataType.WithLength(mt.Length);
										context.result.DbDataTypeExpression = Expression.Constant(mt);
									}
								}

								context.setName?.Invoke(context.Member.Name);
							}
						}
					}

					return new TransformInfo(expr);
				});

			return result;
		}

		#endregion

		internal static ParameterAccessor CreateParameterAccessor(
			DBInstance         dataContext,
			Expression           accessorExpression,
			Expression?          itemAccessorExpression,
			Expression           dbDataTypeAccessorExpression,
			Expression           expression,
			Expression?          parametersExpression,
			string?              name)
		{
			// Extracting name for parameter
			//
			if (name == null && expression.Type == typeof(DataParameter))
			{
				var dp = expression.EvaluateExpression<DataParameter>();
				if (dp != null && !string.IsNullOrEmpty(dp.Name))
					name = dp.Name;
			}

			name ??= "p";

			// see #820
			accessorExpression           = CorrectAccessorExpression(accessorExpression, dataContext);
			dbDataTypeAccessorExpression = CorrectAccessorExpression(dbDataTypeAccessorExpression, dataContext);

			var mapper = Expression.Lambda<Func<Expression, DBInstance?,object?[]?,object?>>(
				Expression.Convert(accessorExpression, typeof(object)),
				AccessorParameters);

			// TODO: it make sense to use cache for item converter
			var itemMapper = itemAccessorExpression == null
				? null
				: Expression.Lambda<Func<object?,object?>>(
					Expression.Convert(itemAccessorExpression, typeof(object)),
					ItemParameter);

			var dbDataTypeAccessor = Expression.Lambda<Func<Expression,object?, DBInstance?,object?[]?,DbDataType>>(
				Expression.Convert(dbDataTypeAccessorExpression, typeof(DbDataType)),
				DbTypeAccessorParameters);

			var dataTypeAccessor = dbDataTypeAccessor.CompileExpression();

			var parameterType = itemAccessorExpression != null
				? new DbDataType(itemAccessorExpression.Type)
				: parametersExpression == null
					? new DbDataType(accessorExpression.Type)
					: dataTypeAccessor(parametersExpression, null, dataContext, null);

			return new ParameterAccessor(
					mapper.CompileExpression<Func<Expression, DBInstance, object[], object>>(),
					itemMapper?.CompileExpression<Func<object, object>>(),
					dataTypeAccessor,
					new ParameterWord(parameterType, name, null)
					{
						IsQueryParameter =true
					}
				)
#if DEBUG
				{
					AccessorExpr     = mapper,
					ItemAccessorExpr = itemMapper
				}
#endif
				;
		}

		static Expression CorrectAccessorExpression(Expression accessorExpression, DBInstance dataContext)
		{
			// see #820
			accessorExpression = accessorExpression.Transform(dataContext, static (context, e) =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.MemberAccess:
					{
						var ma = (MemberExpression) e;

						if (ma.Member.IsNullableValueMember())
						{
							return Expression.Condition(
								Expression.Equal(ma.Expression!, Expression.Constant(null, ma.Expression!.Type)),
								Expression.Default(e.Type),
								e);
						}

						return e;
					}
					case ExpressionType.Convert       :
					case ExpressionType.ConvertChecked:
					{
						var ce = (UnaryExpression) e;
						if (ce.Operand.Type.IsNullable() && !ce.Type.IsNullable())
						{
							return Expression.Condition(
								Expression.Equal(ce.Operand, Expression.Constant(null, ce.Operand.Type)),
								Expression.Default(e.Type),
								e);
						}

						return e;
					}

					case ExpressionType.Extension:
					{
						if (e is SqlQueryRootExpression root)
						{
							var newExpr = (Expression)ExpressionConstants.DataContextParam;
							if (newExpr.Type != e.Type)
								newExpr = Expression.Convert(newExpr, e.Type);
							return newExpr;
						}

						return e;
					}
					default:
						return e;
				}
			})!;

			return accessorExpression;
		}

		List<Expression>? _parameterized;

		public List<Expression>?                   GetParameterized()  => _parameterized;



		public bool CanBeConstant(Expression expr)
		{
			if (_parameterized != null && _parameterized.Contains(expr))
				return false;
			return true;
		}

		public void AddAsParameterized(Expression expression)
		{
			_parameterized ??= new ();
			if (!_parameterized.Contains(expression))
				_parameterized.Add(expression);
		}

		public bool GetAccessorExpression(Expression expression, [NotNullWhen(true)] out Expression? accessor, bool register)
		{
			if (_expressionAccessors.TryGetValue(expression, out accessor))
			{
				if (register)
					AddAsParameterized(expression);

				return true;
			}

			accessor = null;
			return false;
		}
	}
}
