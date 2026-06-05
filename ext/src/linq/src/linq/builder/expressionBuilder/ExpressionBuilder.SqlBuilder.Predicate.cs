using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace mooSQL.linq.Linq.Builder
{
	using Common;
	using Common.Internal;
	using Data;
	using Extensions;
	using Translation;
	using mooSQL.linq.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using DataProvider;
	using mooSQL.data.model;
	using mooSQL.data;
	using mooSQL.data.model.affirms;
    using mooSQL.data.Mapping;
    using mooSQL.utils;
    using mooSQL.data.mapping;

    partial class ExpressionBuilder
	{
        #region Predicate Converter

        IAffirmWord? ConvertPredicate(IBuildContext? context, Expression expression, ProjectFlags flags, out SqlErrorExpression? error)
		{
			error = null;

            IAffirmWord? CheckExpression(Expression expr, ref SqlErrorExpression? resultError)
			{
				if (expr is SqlPlaceholderExpression { Sql: SearchConditionWord sc })
					return sc;

				resultError = SqlErrorExpression.EnsureError(context, expr);

				return null;
			}

            IExpWord IsCaseSensitive(MethodCallExpression mc)
			{
				if (mc.Arguments.Count <= 1)
					return new ValueWord(typeof(bool?), null);

				if (!typeof(StringComparison).IsSameOrParentOf(mc.Arguments[1].Type))
					return new ValueWord(typeof(bool?), null);

				var arg = mc.Arguments[1];

				if (arg.NodeType == ExpressionType.Constant || arg.NodeType == ExpressionType.Default)
				{
					var comparison = (StringComparison)(EvaluateExpression(arg) ?? throw new InvalidOperationException());
					return new ValueWord(comparison is StringComparison.CurrentCulture
										           or StringComparison.InvariantCulture
										           or StringComparison.Ordinal);
				}

				var variable   = Expression.Variable(typeof(StringComparison), "c");
				var assignment = Expression.Assign(variable, arg);
				var expr       = (Expression)Expression.Equal(variable, Expression.Constant(StringComparison.CurrentCulture));
				expr = Expression.OrElse(expr, Expression.Equal(variable, Expression.Constant(StringComparison.InvariantCulture)));
				expr = Expression.OrElse(expr, Expression.Equal(variable, Expression.Constant(StringComparison.Ordinal)));
				expr = Expression.Block(new[] { variable }, assignment, expr);

				var parameter = ParametersContext.BuildParameter(context, expr, columnDescriptor : null, forceConstant : true)!;
				parameter.SqlParameter.IsQueryParameter = false;

				return parameter.SqlParameter;
			}

			if (CanBeCompiled(expression, false))
			{
				var param = _parametersContext.BuildParameter(context, expression, null, buildParameterType: ParametersContext.BuildParameterType.Bool);
				if (param != null)
				{
					return new Expr(param.SqlParameter);
				}
			}

			switch (expression.NodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				{
					var e = (BinaryExpression)expression;

					var left  = RemoveNullPropagation(context!, e.Left, flags, false);
					var right = RemoveNullPropagation(context!, e.Right, flags, false);

					var newExpr = e.Update(left, e.Conversion, right);

					left  = newExpr.Left;
					right = newExpr.Right;

					return CheckExpression(ConvertCompareExpression(context, newExpr.NodeType, left, right, flags, newExpr), ref error);
				}

				case ExpressionType.Call:
				{
					var e = (MethodCallExpression)expression;

                        IAffirmWord? predicate = null;

					if (e.Method.Name          == nameof(Sql.Alias) && e.Object == null && e.Arguments.Count == 2 &&
						e.Method.DeclaringType == typeof(Sql))
					{
						predicate = ConvertPredicate(context, e.Arguments[0], flags, out error);
						return predicate;
					}

					if (e.Method.Name == "Equals" && e.Object != null && e.Arguments.Count == 1)
						return CheckExpression(ConvertCompareExpression(context, ExpressionType.Equal, e.Object, e.Arguments[0], flags), ref error);

					if (e.Method.DeclaringType == typeof(string))
					{
						switch (e.Method.Name)
						{
							case "Contains"   : predicate = CreateStringPredicate(context, e, mooSQL.data.model.affirms.SearchString.SearchKind.Contains,   IsCaseSensitive(e), flags); break;
							case "StartsWith" : predicate = CreateStringPredicate(context, e, mooSQL.data.model.affirms.SearchString.SearchKind.StartsWith, IsCaseSensitive(e), flags); break;
							case "EndsWith"   : predicate = CreateStringPredicate(context, e, mooSQL.data.model.affirms.SearchString.SearchKind.EndsWith,   IsCaseSensitive(e), flags); break;
						}
					}
					else if (e.Method.Name == "Contains")
					{
						if (e.Method.DeclaringType  == typeof(Enumerable) ||
						    (e.Method.DeclaringType == typeof(Queryable) && e.Arguments.Count == 2 && CanBeCompiled(e.Arguments[0], false)) ||
							typeof(IList).IsSameOrParentOf(e.Method.DeclaringType!) ||
							typeof(ICollection<>).IsSameOrParentOf(e.Method.DeclaringType!) ||
							typeof(IReadOnlyCollection<>).IsSameOrParentOf(e.Method.DeclaringType!))
						{
							predicate = ConvertInPredicate(context!, e);
						}
					}
					else if (e.Method.Name == "ContainsValue" && typeof(Dictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!))
					{
						var args = e.Method.DeclaringType!.GetGenericArguments(typeof(Dictionary<,>))!;
						var minf = EnumerableMethods
								.First(static m => m.Name == "Contains" && m.GetParameters().Length == 2)
								.MakeGenericMethod(args[1]);

						var expr = Expression.Call(
								minf,
								ExpressionHelper.PropertyOrField(e.Object!, "Values"),
								e.Arguments[0]);

						predicate = ConvertInPredicate(context!, expr);
					}
					else if (e.Method.Name == "ContainsKey" &&
						(typeof(IDictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!) ||
						 typeof(IReadOnlyDictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!)))
					{
						var type = typeof(IDictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!) ? typeof(IDictionary<,>) : typeof(IReadOnlyDictionary<,>);
						var args = e.Method.DeclaringType!.GetGenericArguments(type)!;
						var minf = EnumerableMethods
								.First(static m => m.Name == "Contains" && m.GetParameters().Length == 2)
								.MakeGenericMethod(args[0]);

						var expr = Expression.Call(
								minf,
								ExpressionHelper.PropertyOrField(e.Object!, "Keys"),
								e.Arguments[0]);

						predicate = ConvertInPredicate(context!, expr);
					}

#if NETFRAMEWORK
					else if (e.Method == ReflectionHelper.Functions.String.Like11) predicate = ConvertLikePredicate(context!, e, flags);
					else if (e.Method == ReflectionHelper.Functions.String.Like12) predicate = ConvertLikePredicate(context!, e, flags);
#endif
					else if (e.Method == ReflectionHelper.Functions.String.Like21) predicate = ConvertLikePredicate(context!, e, flags);
					else if (e.Method == ReflectionHelper.Functions.String.Like22) predicate = ConvertLikePredicate(context!, e, flags);

					if (predicate != null)
						return predicate;

					var attr = e.Method.GetExpressionAttribute(DBLive);

					if (attr != null && attr.GetIsPredicate(expression))
						break;

					var processed = MakeExpression(context, expression, flags);
					if (!ReferenceEquals(processed, expression))
					{
						return ConvertPredicate(context, processed, flags, out error);
					}

					break;
				}

				case ExpressionType.Conditional:
					return new ExprExpr(
                            ConvertToSql(context, expression),
                            AffirmWord.Operator.Equal,
							new ValueWord(true), null);

				case ExpressionType.TypeIs:
				{
					var e   = (TypeBinaryExpression)expression;
					var contextRef = GetRootContext(context, e.Expression, false);

					if (contextRef != null && SequenceHelper.GetTableContext(contextRef.BuildContext) != null)
						return MakeIsPredicate(contextRef.BuildContext, e, flags, out error);

					break;
				}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					var e = (UnaryExpression)expression;

					if (e.Type == typeof(bool) && e.Operand.Type == typeof(SqlBoolean))
						return ConvertPredicate(context, e.Operand, flags, out error);

					break;
				}
			}

			if (!TryConvertToSql(context, expression, flags, null, out var ex, out error))
				return null;

			if (ExpressionWord.NeedsEqual(ex))
			{
				var descriptor = QueryHelper.GetColumnDescriptor(ex);

				if (ex is ColumnWord col)
					ex = NullabilityWord.ApplyNullability(ex, NullabilityContext.GetContext(col.Parent));

				if (TryConvertToSql(context, ExpressionInstances.True, flags, descriptor, out var trueValue, out _)
				    && TryConvertToSql(context, ExpressionInstances.False, flags, descriptor, out var falseValue, out _)
				    && trueValue.NodeType == ClauseType.SqlValue
				    && falseValue.NodeType == ClauseType.SqlValue)
				{
					return new IsTrue(ex, trueValue, falseValue, DBLive.dialect.Option.CompareNullsAsValues ? false : null, false);
				}

				return new ExprExpr(ex, AffirmWord.Operator.Equal, new ValueWord(true),
					CompareNullsAsValues ? true : null);
			}

			if (ex is IAffirmWord expPredicate)
				return expPredicate;

			return new mooSQL.data.model.affirms.Expr(ex);
		}


        #region ConvertEnumConversion

        IAffirmWord? ConvertEnumConversion(IBuildContext context, Expression left, AffirmWord.Operator op, Expression right)
		{
			Expression value;
			Expression operand;

			if (left is MemberExpression)
			{
				operand = left;
				value   = right;
			}
			else if (left.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked && ((UnaryExpression)left).Operand is MemberExpression)
			{
				operand = ((UnaryExpression)left).Operand;
				value   = right;
			}
			else if (right is MemberExpression)
			{
				operand = right;
				value   = left;
			}
			else if (right.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked && ((UnaryExpression)right).Operand is MemberExpression)
			{
				operand = ((UnaryExpression)right).Operand;
				value   = left;
			}
			else if (left.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			{
				operand = ((UnaryExpression)left).Operand;
				value   = right;
			}
			else
			{
				operand = ((UnaryExpression)right).Operand;
				value = left;
			}

			var type = operand.Type;

			if (!type.UnwrapNullable().IsEnum)
				return null;

			var dic = new Dictionary<object, object?>();

			//var mapValues = MappingSchema.GetMapValues(type);

			//if (mapValues != null)
			//	foreach (var mv in mapValues)
			//		if (!dic.ContainsKey(mv.OrigValue))
			//			dic.Add(mv.OrigValue, mv.MapValues[0].Value);

			switch (value.NodeType)
			{
				case ExpressionType.Constant:
				{
					var name = Enum.GetName(type, ((ConstantExpression)value).Value!);

					// ReSharper disable ConditionIsAlwaysTrueOrFalse
					// ReSharper disable HeuristicUnreachableCode
					if (name == null)
						return null;
					// ReSharper restore HeuristicUnreachableCode
					// ReSharper restore ConditionIsAlwaysTrueOrFalse

					var origValue = Enum.Parse(type, name, false);

					if (!dic.TryGetValue(origValue, out var mapValue))
						mapValue = origValue;

                        IExpWord l, r;

                        ValueWord sqlvalue=null;
					//var ce = MappingSchema.GetConverter(new DbDataType(type), new DbDataType(typeof(DataParameter)), false, ConversionType.Common);

					//if (ce != null)
					//{
					//	sqlvalue = new ValueWord(ce.ConvertValueToParameter(origValue).Value!);
					//}
					//else
					//{
					//	// TODO: pass column type to type mapValue=null cases?
					//	sqlvalue = DBLive.GetSqlValue(type, mapValue, null);
					//}

					if (left.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
					{
						l = ConvertToSql(context, operand);
						r = sqlvalue;
					}
					else
					{
						r = ConvertToSql(context, operand);
						l = sqlvalue;
					}

					return new ExprExpr(l, op, r, true);
				}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					value = ((UnaryExpression)value).Operand;

					var cd = SuggestColumnDescriptor(context, operand, value, ProjectFlags.SQL);

					var l = ConvertToSql(context, operand, columnDescriptor: cd);
					var r = ConvertToSql(context, value, columnDescriptor: cd);

					return new ExprExpr(l, op, r, true);
				}
			}

			return null;
		}

		#endregion

		#region ConvertObjectComparison

		static Expression? ConstructMemberPath(MemberInfo[] memberPath, Expression ob, bool throwOnError)
		{
			Expression result = ob;
			foreach (var memberInfo in memberPath)
			{
				if (memberInfo.DeclaringType!.IsAssignableFrom(result.Type))
				{
					result = Expression.MakeMemberAccess(result, memberInfo);
				}
			}

			if (ReferenceEquals(result, ob) && throwOnError)
				throw new LinqToDBException($"Type {result.Type.Name} does not have member {memberPath.Last().Name}.");

			return result;
		}

		#endregion

		#region Parameters

		public static DbDataType GetMemberDataType(DBInstance mappingSchema, MemberInfo member)
		{
			var typeResult = new DbDataType(member.GetMemberType());


			var col = mappingSchema.client.EntityCash.getEntityInfo(member.ReflectedType!);

			var mem = col?.GetColumn(member);


			var dataType = mem?.DataType;

			if (dataType != null)
				typeResult = typeResult.WithDataType(dataType.Value);

			//var dbType = mem?. ;
			//if (dbType != null)
			//	typeResult = typeResult.WithDbType(dbType);

			if (mem != null && mem.Length != null)
				typeResult = typeResult.WithLength(mem.Length);

			return typeResult;
		}

		private sealed class GetDataTypeContext
		{
			public GetDataTypeContext(DbDataType baseType, DBInstance mappingSchema)
			{
				DataType      = baseType.DataType;
				DbType        = baseType.DbType;
				Length        = baseType.Length;
				Precision     = baseType.Precision;
				Scale         = baseType.Scale;

				MappingSchema = mappingSchema;
			}

			public DataFam      DataType;
			public string?       DbType;
			public int?          Length;
			public int?          Precision;
			public int?          Scale;

			public DBInstance MappingSchema { get; }

		}

		static DbDataType GetDataType(IExpWord expr, DbDataType baseType, DBInstance mappingSchema)
		{
			var ctx = new GetDataTypeContext(baseType, mappingSchema);

			expr.Find(ctx, static (context, e) =>
			{
				switch (e.NodeType)
				{
					case ClauseType.SqlField:
					{
						var fld = (FieldWord)e;
						context.DataType     = fld.Type.DataType;
						context.DbType       = fld.Type.DbType;
						context.Length       = fld.Type.Length;
						context.Precision    = fld.Type.Precision;
						context.Scale        = fld.Type.Scale;
						return true;
					}
					case ClauseType.SqlParameter:
					{
						var type             = ((ParameterWord)e).Type;
						context.DataType     = type.DataType;
						context.DbType       = type.DbType;
						context.Length       = type.Length;
						context.Precision    = type.Precision;
						context.Scale        = type.Scale;
						return true;
					}
					case ClauseType.SqlDataType:
					{
						var type             = ((DataTypeWord)e).Type;
						context.DataType     = type.DataType;
						context.DbType       = type.DbType;
						context.Length       = type.Length;
						context.Precision    = type.Precision;
						context.Scale        = type.Scale;
						return true;
					}
					case ClauseType.SqlValue:
					{
						var valueType        = ((ValueWord)e).ValueType;
						context.DataType     = valueType.DataType;
						context.DbType       = valueType.DbType;
						context.Length       = valueType.Length;
						context.Precision    = valueType.Precision;
						context.Scale        = valueType.Scale;
						return true;
					}
					default:
					{
						if (e is IExpWord expr)
						{
							var type = QueryHelper.GetDbDataType(expr,context.MappingSchema);
							context.DataType  = type.DataType;
							context.DbType    = type.DbType;
							context.Length    = type.Length;
							context.Precision = type.Precision;
							context.Scale     = type.Scale;
							return true;
						}
						return false;
					}
				}
			});

			return new DbDataType(
				baseType.SystemType,
				ctx.DataType == DataFam.Undefined ? baseType.DataType : ctx.DataType,
				string.IsNullOrEmpty(ctx.DbType)   ? baseType.DbType   : ctx.DbType,
				ctx.Length     ?? baseType.Length,
				ctx.Precision  ?? baseType.Precision,
				ctx.Scale      ?? baseType.Scale
			);
		}

		#endregion

		#region ConvertInPredicate

		void BuildObjectGetters(SqlGenericConstructorExpression generic, ParameterExpression rootParam, Expression root, List<SqlGetValue> getters)
		{
			for (int i = 0; i < generic.Assignments.Count; i++)
			{
				var assignment = generic.Assignments[i];

				if (assignment.Expression is SqlGenericConstructorExpression subGeneric)
				{
					BuildObjectGetters(subGeneric, rootParam, Expression.MakeMemberAccess(root, assignment.MemberInfo), getters);
				}
				else if (assignment.Expression is SqlPlaceholderExpression placeholder)
				{
					var access = Expression.MakeMemberAccess(root, assignment.MemberInfo);
					var body   = Expression.Convert(access, typeof(object));

					var lambda = Expression.Lambda<Func<object, object>>(body, rootParam);

					getters.Add(new SqlGetValue(placeholder.Sql, placeholder.Type, null, lambda.Compile()));
				}
			}
		}

		private IAffirmWord? ConvertInPredicate(IBuildContext context, MethodCallExpression expression)
		{
			var e        = expression;
			var argIndex = e.Object != null ? 0 : 1;
			var arr      = e.Object ?? e.Arguments[0];
			var arg      = e.Arguments[argIndex];

            IExpWord? expr = null;

			var builtExpr = BuildSqlExpression(context, arg, ProjectFlags.SQL | ProjectFlags.Keys, null);

			if (builtExpr is SqlPlaceholderExpression placeholder)
			{
				expr = placeholder.Sql;
			}
			else if (SequenceHelper.UnwrapDefaultIfEmpty(builtExpr) is SqlGenericConstructorExpression constructor)
			{
				var objParam = Expression.Parameter(typeof(object));

				var getters = new List<SqlGetValue>();
				BuildObjectGetters(constructor, objParam, Expression.Convert(objParam, constructor.ObjectType),
					getters);

				expr = new ObjectWord( getters.ToArray());
			}

			if (expr == null)
				return null;

			var columnDescriptor = QueryHelper.GetColumnDescriptor(expr);

			switch (arr.NodeType)
			{
				case ExpressionType.NewArrayInit :
					{
						var newArr = (NewArrayExpression)arr;

						if (newArr.Expressions.Count == 0)
							return AffirmWord.False;

						var exprs  = new IExpWord[newArr.Expressions.Count];

						for (var i = 0; i < newArr.Expressions.Count; i++)
							exprs[i] = ConvertToSql(context, newArr.Expressions[i], columnDescriptor: columnDescriptor);

						return new data.model.affirms.InList(expr, DBLive.dialect.Option.CompareNullsAsValues ? false : null, false, exprs);
					}

				default :

					if (CanBeCompiled(arr, false))
					{
						var p = ParametersContext.BuildParameter(context, arr, columnDescriptor, forceConstant : false,
							buildParameterType : ParametersContext.BuildParameterType.InPredicate)!.SqlParameter;
						p.IsQueryParameter = false;
						return new InList(expr, DBLive.dialect.Option.CompareNullsAsValues ? false : null, false, p);
					}

					break;
			}

			return null;
		}

		#endregion

		#region ColumnDescriptor Helpers

		public EntityColumn? SuggestColumnDescriptor(IBuildContext? context, Expression expr, ProjectFlags flags)
		{
			expr = expr.Unwrap();

			var converted = ConvertToSqlExpr(context, expr, flags.SqlFlag());
			if (converted is not SqlPlaceholderExpression placeholderTest)
				return null;

			//var descriptor = QueryHelper.GetColumnDescriptor(placeholderTest.Sql);
			//return descriptor;
			return null;
		}

		public EntityColumn? SuggestColumnDescriptor(IBuildContext? context, Expression expr1, Expression expr2, ProjectFlags flags)
		{
			return SuggestColumnDescriptor(context, expr1, flags) ?? SuggestColumnDescriptor(context, expr2, flags);
		}

		public EntityColumn? SuggestColumnDescriptor(IBuildContext? context, ReadOnlyCollection<Expression> expressions, ProjectFlags flags)
		{
			foreach (var expr in expressions)
			{
				var descriptor = SuggestColumnDescriptor(context, expr, flags);
				if (descriptor != null)
					return descriptor;
			}

			return null;
		}

        #endregion

        #region LIKE predicate

        IAffirmWord? CreateStringPredicate(IBuildContext? context, MethodCallExpression expression, mooSQL.data.model.affirms.SearchString.SearchKind kind, IExpWord caseSensitive, ProjectFlags flags)
		{
			var e = expression;

			if (e.Object == null)
				return null;

			var descriptor = SuggestColumnDescriptor(context, e.Object, e.Arguments[0], flags);

			if (!TryConvertToSql(context, e.Object, flags, columnDescriptor : descriptor, sqlExpression : out var o, error : out _))
				return null;

			if (!TryConvertToSql(context, e.Arguments[0], flags, columnDescriptor : descriptor, sqlExpression : out var a, error : out _))
				return null;

			return new mooSQL.data.model.affirms.SearchString(o, false, a, kind, caseSensitive);
		}

        IAffirmWord ConvertLikePredicate(IBuildContext context, MethodCallExpression expression, ProjectFlags flags)
		{
			var e  = expression;

			var descriptor = SuggestColumnDescriptor(context, e.Arguments, flags);

			var a1 = ConvertToSql(context, e.Arguments[0], unwrap: false, columnDescriptor: descriptor);
			var a2 = ConvertToSql(context, e.Arguments[1], unwrap: false, columnDescriptor: descriptor);

            IExpWord? a3 = null;

			if (e.Arguments.Count == 3)
				a3 = ConvertToSql(context, e.Arguments[2], unwrap: false, columnDescriptor: descriptor);

			return new mooSQL.data.model.affirms.Like(a1, false, a2, a3);
		}

		#endregion

		#region MakeIsPredicate

		public IAffirmWord MakeIsPredicate(TableBuilder.TableContext table, Type typeOperand)
		{
			if (typeOperand == table.ObjectType)
			{
				var all = true;
				foreach (var m in table.InheritanceMapping)
				{
					if (m.Type == typeOperand)
					{
						all = false;
						break;
					}
				}

				if (all)
					return AffirmWord.True;
			}

			return MakeIsPredicate(table, table, table.InheritanceMapping, typeOperand, static (table, name) => table.SqlTable.FindFieldByMemberName(name) ?? throw new LinqException($"Field {name} not found in table {table.SqlTable}"));
		}

		public IAffirmWord MakeIsPredicate<TContext>(
			TContext                              getSqlContext,
			IBuildContext                         context,
			IReadOnlyList<EntiyInherit>     inheritanceMapping,
			Type                                  toType,
			Func<TContext,string, IExpWord> getSql)
		{
			static IAffirmWord CorrectNullability(ExprExpr exprExpr)
			{
				if (exprExpr.Expr2 is ValueWord { Value: null })
				{
					exprExpr.Expr1 = NullabilityWord.ApplyNullability(exprExpr.Expr1, true);
				}
				else if (exprExpr.Expr1 is ValueWord { Value: null })
				{
					exprExpr.Expr2 = NullabilityWord.ApplyNullability(exprExpr.Expr2, true);
				}

				return exprExpr;
			}

			var mapping = new List<EntiyInherit>(inheritanceMapping.Count);
			foreach (var m in inheritanceMapping)
				if (m.Type == toType && !m.IsDefault)
					mapping.Add(m);

			switch (mapping.Count)
			{
				case 0 :
					{
						var cond = new SearchConditionWord();

						var found = false;
						foreach (var m in inheritanceMapping)
						{
							if (m.Type == toType)
							{
								found = true;
								break;
							}
						}

						if (found)
						{
							foreach (var m in inheritanceMapping.Where(static m => !m.IsDefault))
							{
								//cond.Predicates.Add(
								//	CorrectNullability(
								//		new ExprExpr(
								//			getSql(getSqlContext, m.DiscriminatorName),
								//			AffirmWord.Operator.NotEqual,
								//			DBLive.GetSqlValue(m.Entity.MemberType, m.Code, m.Discriminator.GetDbDataType(true)),
								//			DBLive.dialect.Option.CompareNullsAsValues ? true : null)
								//	)
								//);
							}
						}
						else
						{
							var sc = new SearchConditionWord(true);
							foreach (var m in inheritanceMapping)
							{
								if (toType.IsSameOrParentOf(m.Type))
								{
									//sc.Predicates.Add(
									//	CorrectNullability(
									//		new ExprExpr(
									//			getSql(getSqlContext, m.DiscriminatorName),
									//			AffirmWord.Operator.Equal,
         //                                       DBLive.GetSqlValue(m.Discriminator.MemberType, m.Code, m.Discriminator.GetDbDataType(true)),
									//			DBLive.dialect.Option.CompareNullsAsValues ? true : null)
									//	)
									//);
								}
							}

							cond.Add(sc);
						}

						return cond;
					}

				//case 1 :
				//{
				//	//var discriminatorSql = getSql(getSqlContext, mapping[0].Type);
				//	var sqlValue = null;
				//			//DBLive.GetSqlValue(mapping[0].Discriminator.MemberType, mapping[0].Code, mapping[0].Discriminator.GetDbDataType(true));

				//	return CorrectNullability(
				//		new ExprExpr(
				//			discriminatorSql,
				//			AffirmWord.Operator.Equal,
				//			sqlValue,
				//			DBLive.dialect.Option.CompareNullsAsValues ? true : null)
				//	);
				//}
				//default:
				//	{
				//		var cond = new SearchConditionWord(true);

				//		foreach (var m in mapping)
				//		{
				//			cond.Predicates.Add(
				//				new ExprExpr(
				//					getSql(getSqlContext, m.DiscriminatorName),
    //                                AffirmWord.Operator.Equal,
    //                                DBLive.GetSqlValue(m.Discriminator.MemberType, m.Code, m.Discriminator.GetDbDataType(true)),
    //                                DBLive.dialect.Option.CompareNullsAsValues ? true : null));
				//		}

				//		return cond;
				//	}
			}
			return null;
		}

        IAffirmWord? MakeIsPredicate(IBuildContext context, TypeBinaryExpression expression, ProjectFlags flags, out SqlErrorExpression? error)
		{
			var predicateExpr = MakeIsPredicateExpression(context, expression);

			return ConvertPredicate(context, predicateExpr, flags, out error);
		}

		Expression MakeIsPredicateExpression(IBuildContext context, TypeBinaryExpression expression)
		{
			var typeOperand = expression.TypeOperand;
			var table       = new TableBuilder.TableContext(this, DBLive, new BuildInfo((IBuildContext?)null, ExpressionInstances.UntypedNull, new SelectQueryClause()), null);

			if (typeOperand == table.ObjectType)
			{
				var all = true;
				foreach (var m in table.InheritanceMapping)
				{
					if (m.Type == typeOperand)
					{
						all = false;
						break;
					}
				}

				if (all)
					return Expression.Constant(true);
			}

			//var mapping = new List<(InheritanceMapping m, int i)>(table.InheritanceMapping.Count);

			//for (var i = 0; i < table.InheritanceMapping.Count; i++)
			//{
			//	var m = table.InheritanceMapping[i];
			//	if (typeOperand.IsAssignableFrom(m.Type) && !m.IsDefault)
			//		mapping.Add((m, i));
			//}

			//var isEqual = true;

			//if (mapping.Count == 0)
			//{
			//	for (var i = 0; i < table.InheritanceMapping.Count; i++)
			//	{
			//		var m = table.InheritanceMapping[i];
			//		if (!m.IsDefault)
			//			mapping.Add((m, i));
			//	}

			//	isEqual = false;
			//}

			Expression? expr = null;

			//foreach (var m in mapping)
			//{
			//	var field = table.SqlTable.FindFieldByMemberName(table.InheritanceMapping[m.i].DiscriminatorName) ?? throw new LinqException($"Field {table.InheritanceMapping[m.i].DiscriminatorName} not found in table {table.SqlTable}");
			//	var ttype = field.ColumnDescriptor.UnderType;
			//	var obj   = expression.Expression;

			//	if (obj.Type != ttype)
			//		obj = Expression.Convert(expression.Expression, ttype);

			//	var memberInfo = ttype.GetMemberEx(field.ColumnDescriptor.PropertyInfo) ?? throw new InvalidOperationException();

			//	var left = Expression.MakeMemberAccess(obj, memberInfo);
			//	var code = m.m.Code;

			//	if (code == null)
			//		code = left.Type.GetDefaultValue();
			//	else if (left.Type != code.GetType())
			//		code = context.Builder.DBLive.dialect.mapping.ChangeTypeTo(code, left.Type);

			//	Expression right = Expression.Constant(code, left.Type);

			//	//var e = isEqual ? Expression.Equal(left, right) : Expression.NotEqual(left, right);

			//	//if (!isEqual)
			//	//	expr = expr != null ? Expression.AndAlso(expr, e) : e;
			//	//else
			//	//	expr = expr != null ? Expression.OrElse(expr, e) : e;
			//}

			return expr!;
		}

		#endregion

		#endregion
	}
}
