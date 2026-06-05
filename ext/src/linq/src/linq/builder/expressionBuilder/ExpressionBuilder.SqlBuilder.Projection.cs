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
		#region Projection

		public Expression Project(IBuildContext context, Expression? path, List<Expression>? nextPath, int nextIndex, ProjectFlags flags, Expression body, bool strict)
		{
			MemberInfo? member = null;
			Expression? next   = null;

			if (path is MemberExpression memberExpression)
			{
				nextPath ??= new();
				nextPath.Add(memberExpression);

				if (memberExpression.Expression is MemberExpression me)
				{
					// going deeper
					return Project(context, me, nextPath, nextPath.Count - 1, flags, body, strict);
				}

				if (memberExpression.Expression!.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					// going deeper
					return Project(context, ((UnaryExpression)memberExpression.Expression).Operand, nextPath, nextPath.Count - 1, flags, body, strict);
				}

				// make path projection
				return Project(context, null, nextPath, nextPath.Count - 1, flags, body, strict);
			}

			if (path is SqlGenericParamAccessExpression accessExpression)
			{
				nextPath ??= new();
				nextPath.Add(accessExpression);

				if (accessExpression.Constructor is SqlGenericParamAccessExpression ae)
				{
					// going deeper
					return Project(context, ae, nextPath, nextPath.Count - 1, flags, body, strict);
				}

				// make path projection
				return Project(context, null, nextPath, nextPath.Count - 1, flags, body, strict);
			}

			if (path == null)
			{
				if (nextPath == null || nextIndex < 0)
				{
					if (body == null)
						throw new InvalidOperationException();

					return body;
				}

				next = nextPath[nextIndex];

				if (next is MemberExpression me)
				{
					member = me.Member;
				}
				else if (next is SqlGenericParamAccessExpression paramAccess)
				{
					if (body.NodeType == ExpressionType.New)
					{
						var newExpr = (NewExpression)body;
						if (newExpr.Constructor == paramAccess.ParameterInfo.Member && paramAccess.ParamIndex < newExpr.Arguments.Count)
						{
							return Project(context, null, nextPath, nextIndex - 1, flags,
								newExpr.Arguments[paramAccess.ParamIndex], strict);
						}
					}
					else if (body.NodeType == ExpressionType.Call)
					{
						var methodCall = (MethodCallExpression)body;
						if (methodCall.Method == paramAccess.ParameterInfo.Member && paramAccess.ParamIndex < methodCall.Arguments.Count)
						{
							return Project(context, null, nextPath, nextIndex - 1, flags,
								methodCall.Arguments[paramAccess.ParamIndex], strict);
						}
					}

					// nothing to do right now
				}
				else
				{
					throw new NotImplementedException();
				}
			}

			if (flags.HasFlag(ProjectFlags.SQL))
			{
				body = RemoveNullPropagation(body, flags.HasFlag(ProjectFlags.Keys));
			}

			if (body is SqlDefaultIfEmptyExpression defaultIfEmptyExpression)
			{
				body = defaultIfEmptyExpression.InnerExpression;
			}

			switch (body.NodeType)
			{
				case ExpressionType.Extension:
				{
					if (body is SqlPlaceholderExpression placeholder)
					{
						return placeholder;
					}

					if (member != null)
					{
						if (body is ContextRefExpression contextRef)
						{
							var objExpression   = body;
							var memberCorrected = contextRef.Type.GetMemberEx(member);
							if (memberCorrected  is null)
							{
								// inheritance handling
								if (member.DeclaringType != null &&
									contextRef.Type.IsSameOrParentOf(member.DeclaringType))
								{
									memberCorrected = member;
									objExpression   = Expression.Convert(objExpression, member.DeclaringType);
								}
								else
								{
									return next!;
								}
							}

							var ma      = Expression.MakeMemberAccess(objExpression, memberCorrected);
							var newPath = nextPath![0].Replace(next!, ma);

							return newPath;
						}

						if (body.IsNullValue())
						{
							return new DefaultValueExpression(DBLive, member.GetMemberType());
						}

						if (body is SqlGenericConstructorExpression genericConstructor)
						{
							Expression? bodyExpresion = null;
							for (int i = 0; i < genericConstructor.Assignments.Count; i++)
							{
								var assignment = genericConstructor.Assignments[i];
								if (MemberInfoEqualityComparer.Default.Equals(assignment.MemberInfo, member))
								{
									bodyExpresion = assignment.Expression;
									break;
								}
							}

							if (bodyExpresion == null)
							{
								for (int i = 0; i < genericConstructor.Parameters.Count; i++)
								{
									var parameter = genericConstructor.Parameters[i];
									if (MemberInfoEqualityComparer.Default.Equals(parameter.MemberInfo, member))
									{
										bodyExpresion = parameter.Expression;
										break;
									}
								}
							}

							if (bodyExpresion == null)
							{
								// search in base class
								for (int i = 0; i < genericConstructor.Assignments.Count; i++)
								{
									var assignment = genericConstructor.Assignments[i];
									if (assignment.MemberInfo.ReflectedType != member.ReflectedType && assignment.MemberInfo.Name == member.Name)
									{
										var mi = assignment.MemberInfo.ReflectedType!.GetMemberEx(member);
										if (mi != null && MemberInfoEqualityComparer.Default.Equals(assignment.MemberInfo, mi))
										{
											bodyExpresion = assignment.Expression;
											break;
										}
									}
								}
							}

							if (bodyExpresion is not null)
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, bodyExpresion, strict);
							}

							if (strict)
								return CreateSqlError(null, nextPath![0]);

							return new DefaultValueExpression(DBLive, nextPath![0].Type);
						}
					}

					if (next is SqlGenericParamAccessExpression paramAccessExpression)
					{

						/*
						var projected = Project(context, path, nextPath, nextIndex - 1, flags,
							paramAccessExpression);

						return projected;
						*/

						if (body is SqlGenericConstructorExpression constructorExpression)
						{
							var projected = Project(context, path, nextPath, nextIndex - 1, flags,
								constructorExpression.Parameters[paramAccessExpression.ParamIndex].Expression, strict);
							return projected;
						}

						//throw new InvalidOperationException();
					}

					return body;
				}

				case ExpressionType.MemberAccess:
				{
					if (member != null && nextPath != null)
					{
						if (nextPath[nextIndex] is MemberExpression nextMember && body.Type.IsSameOrParentOf(nextMember.Expression!.Type))
						{
							var newMember = body.Type.GetMemberEx(nextMember.Member);
							if (newMember != null)
							{
								var newMemberAccess = Expression.MakeMemberAccess(body, newMember);
								return Project(context, path, nextPath, nextIndex - 1, flags, newMemberAccess, strict);
							}
						}
					}

					break;
				}

				case ExpressionType.New:
				{
					var ne = (NewExpression)body;

					if (ne.Members != null)
					{
						if (member == null)
						{
							break;
						}

						for (var i = 0; i < ne.Members.Count; i++)
						{
							var memberLocal = ne.Members[i];

							if (MemberInfoEqualityComparer.Default.Equals(memberLocal, member))
							{
								var projected = Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);

								// set alias
								if (projected is ContextRefExpression contextRef)
								{
									contextRef.BuildContext.SetAlias(member.Name);
								}

								return projected;
							}
						}
					}
					else
					{
						var parameters = ne.Constructor!.GetParameters();

						for (var i = 0; i < parameters.Length; i++)
						{
							var parameter     = parameters[i];
							var memberByParam = SqlGenericConstructorExpression.FindMember(ne.Constructor.DeclaringType!, parameter);

							if (memberByParam != null &&
								MemberInfoEqualityComparer.Default.Equals(memberByParam, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);
							}
						}
					}

					if (member == null)
						return ne;

					if (strict)
						return CreateSqlError(null, nextPath![0]);

					return new DefaultValueExpression(DBLive, nextPath![0].Type);
				}

				case ExpressionType.MemberInit:
				{
					var mi = (MemberInitExpression)body;
					var ne = mi.NewExpression;

					if (member == null)
					{
						if (next is SqlGenericParamAccessExpression paramAccess)
						{
							if (paramAccess.ParamIndex >= ne.Arguments.Count)
								return CreateSqlError(context, nextPath![0]);

							return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[paramAccess.ParamIndex], strict);
						}

						throw new NotImplementedException($"Projecting '{next}' is not supported yet.");
					}

					if (ne.Members != null)
					{

						for (var i = 0; i < ne.Members.Count; i++)
						{
							var memberLocal = ne.Members[i];

							if (MemberInfoEqualityComparer.Default.Equals(memberLocal, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);
							}
						}
					}

					var memberInType = body.Type.GetMemberEx(member);
					if (memberInType == null)
					{
						if (member.DeclaringType?.IsSameOrParentOf(body.Type) == true)
							memberInType = member;
					}

					if (memberInType != null)
					{
						for (int index = 0; index < mi.Bindings.Count; index++)
						{
							var binding = mi.Bindings[index];
							switch (binding.BindingType)
							{
								case MemberBindingType.Assignment:
								{
									var assignment = (MemberAssignment)binding;
									if (MemberInfoEqualityComparer.Default.Equals(assignment.Member, memberInType))
									{
										return Project(context, path, nextPath, nextIndex - 1, flags,
											assignment.Expression, strict);
									}

									break;
								}
								case MemberBindingType.MemberBinding:
								{
									var memberMemberBinding = (MemberMemberBinding)binding;
									if (MemberInfoEqualityComparer.Default.Equals(memberMemberBinding.Member, memberInType))
									{
										return Project(context, path, nextPath, nextIndex - 1, flags,
											new SqlGenericConstructorExpression(
												memberMemberBinding.Member.GetMemberType(),
												memberMemberBinding.Bindings), strict);
									}

									break;
								}
								case MemberBindingType.ListBinding:
									throw new NotImplementedException();
								default:
									throw new NotImplementedException();
							}
						}

						if (ne.Constructor != null && ne.Arguments.Count > 0)
						{
							var parameters = ne.Constructor.GetParameters();
							for (int i = 0; i < ne.Arguments.Count; i++)
							{
								var parameter     = parameters[i];
								var memberByParam = SqlGenericConstructorExpression.FindMember(ne.Constructor.DeclaringType!, parameter);

								if (memberByParam != null &&
									MemberInfoEqualityComparer.Default.Equals(memberByParam, member))
								{
									return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);
								}

							}
						}
					}

					if (strict)
						return CreateSqlError(null, nextPath![0]);

					return new DefaultValueExpression(DBLive, nextPath![0].Type);

				}
				case ExpressionType.Conditional:
				{
					var cond      = (ConditionalExpression)body;
					var trueExpr  = Project(context, null, nextPath, nextIndex, flags, cond.IfTrue, strict);
					var falseExpr = Project(context, null, nextPath, nextIndex, flags, cond.IfFalse, strict);

					var trueHasError = trueExpr is SqlErrorExpression;
					var falseHasError = falseExpr is SqlErrorExpression;

					if (strict && (trueHasError || falseHasError))
					{
						if (trueHasError == falseHasError)
						{
							return trueExpr;
						}

						trueExpr  = Project(context, null, nextPath, nextIndex, flags, cond.IfTrue, false);
						falseExpr = Project(context, null, nextPath, nextIndex, flags, cond.IfFalse, false);
					}

					if (trueExpr is SqlErrorExpression || falseExpr is SqlErrorExpression)
					{
						break;
					}

					if (trueExpr.Type != falseExpr.Type)
					{
						if (trueExpr.IsNullValue())
							trueExpr = new DefaultValueExpression(DBLive, falseExpr.Type);
						else if (falseExpr.IsNullValue())
							falseExpr = new DefaultValueExpression(DBLive, trueExpr.Type);
					}

					var newExpr = (Expression)Expression.Condition(cond.Test, trueExpr, falseExpr);

					return newExpr;
				}

				case ExpressionType.Constant:
				{
					var cnt = (ConstantExpression)body;
					if (cnt.Value == null)
					{
						var expr = (path ?? next)!;

						/*
						if (expr.Type.IsValueType)
						{
							var placeholder = CreatePlaceholder(context, new SqlValue(expr.Type, null), expr);
							return placeholder;
						}
						*/

						return new DefaultValueExpression(DBLive, expr.Type);
					}

					break;

				}
				case ExpressionType.Default:
				{
					var expr = (path ?? next)!;

					/*
					if (expr.Type.IsValueType)
					{
						var placeholder = CreatePlaceholder(context, new SqlValue(expr.Type, null), expr);
						return placeholder;
					}
					*/

					return new DefaultValueExpression(DBLive, expr.Type);
				}

				/*
				case ExpressionType.Constant:
				{
					var cnt = (ConstantExpression)body;
					if (cnt.Value == null)
					{
						var expr = (path ?? next)!;

						var placeholder = CreatePlaceholder(context, new SqlValue(expr.Type, null), expr);

						return placeholder;
					}

					return body;
				}

				case ExpressionType.Default:
				{
					var placeholder = CreatePlaceholder(context, new SqlValue(body.Type, null), body);
					return placeholder;
				}
				*/

				case ExpressionType.Call:
				{
					var mc = (MethodCallExpression)body;

					if (mc.Method.IsStatic)
					{
						var parameters = mc.Method.GetParameters();

						for (var i = 0; i < parameters.Length; i++)
						{
							var parameter     = parameters[i];
							var memberByParam = SqlGenericConstructorExpression.FindMember(mc.Method.ReturnType, parameter);

							if (memberByParam != null &&
								MemberInfoEqualityComparer.Default.Equals(memberByParam, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, mc.Arguments[i], strict);
							}
						}
					}

					if (member != null)
					{
						var ma = Expression.MakeMemberAccess(mc, member);
						return Project(context, path, nextPath, nextIndex - 1, flags, ma, strict);
					}

					return mc;
				}

				case ExpressionType.TypeAs:
				{
					var unary = (UnaryExpression)body;

					var truePath = Project(context, path, nextPath, nextIndex, flags, unary.Operand, strict);

					var isPredicate = MakeIsPredicateExpression(context, Expression.TypeIs(unary.Operand, unary.Type));

					if (isPredicate is ConstantExpression constExpr)
					{
						if (constExpr.Value is true)
							return truePath;
						return new DefaultValueExpression(DBLive, truePath.Type);
					}

					var falsePath = Expression.Constant(null, truePath.Type);

					var conditional = Expression.Condition(isPredicate, truePath, falsePath);

					return conditional;
				}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					var unaryExpression = (UnaryExpression)body;

					if (unaryExpression.Operand is ContextRefExpression contextRef)
					{
						contextRef = contextRef.WithType(unaryExpression.Type);
						return Project(context, path, nextPath, nextIndex, flags, contextRef, strict);
					}

					break;
				}
			}

			return CreateSqlError(context, next!);
		}

		public Expression ParseGenericConstructor(Expression createExpression, ProjectFlags flags, EntityColumn? columnDescriptor, bool force = false)
		{
			if (createExpression.Type.IsNullable())
				return createExpression;

			if (!force && createExpression.Type.IsValueType)
				return createExpression;

			if (!force && DBLive.dialect.mapping.IsScalarType(createExpression.Type))
				return createExpression;

			if (typeof(FormattableString).IsSameOrParentOf(createExpression.Type))
				return createExpression;

			if (flags.IsSql() && IsForceParameter(createExpression, columnDescriptor))
				return createExpression;

			switch (createExpression.NodeType)
			{
				case ExpressionType.New:
				{
					return new SqlGenericConstructorExpression((NewExpression)createExpression);
				}

				case ExpressionType.MemberInit:
				{
					return new SqlGenericConstructorExpression((MemberInitExpression)createExpression);
				}

				case ExpressionType.Call:
				{
					//TODO: Do we still need Alias?
					var mc = (MethodCallExpression)createExpression;
					if (mc.IsSameGenericMethod(Methods.LinqToDB.SqlExt.Alias))
						return ParseGenericConstructor(mc.Arguments[0], flags, columnDescriptor);

					if (mc.IsQueryable())
						return mc;

					if (!mc.Method.IsStatic)
						break;

					if (mc.Method.IsSqlPropertyMethodEx() || mc.IsSqlRow() || mc.Method.DeclaringType == typeof(string))
						break;

					return new SqlGenericConstructorExpression(mc);
				}
			}

			return createExpression;
		}

		Dictionary<SqlCacheKey, Expression>                  _expressionCache    = new(SqlCacheKey.SqlCacheKeyComparer);
		Dictionary<ColumnCacheKey, SqlPlaceholderExpression> _columnCache = new(ColumnCacheKey.ColumnCacheKeyComparer);

#if DEBUG
		int _makeCounter;
#endif

		/// <summary>
		/// 缓存创建的表达式
		/// </summary>
		/// <param name="forContext"></param>
		/// <param name="path"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public Expression MakeExpression(IBuildContext? forContext, Expression path, ProjectFlags flags)
		{
			//迁移走
			throw new NotImplementedException();
			//return expression;
		}

		public bool IsSimpleForCompilation(IBuildContext context, Expression expr)
		{
			if (CanBeConstant(expr))
				return true;
			var sqlExpr = ConvertToSqlExpr(context, expr, ProjectFlags.SQL | ProjectFlags.Test);
			return sqlExpr is SqlPlaceholderExpression || ExpressionEqualityComparer.Instance.Equals(sqlExpr, expr);
		}

		public SqlPlaceholderExpression MakeColumn(SelectQueryClause? parentQuery, SqlPlaceholderExpression sqlPlaceholder, bool asNew = false)
		{
			if (parentQuery == sqlPlaceholder.SelectQuery)
				throw new InvalidOperationException();

			var placeholderType = sqlPlaceholder.Type;
			if (placeholderType.IsNullable())
				placeholderType = placeholderType.UnwrapNullableType();

			if (sqlPlaceholder.SelectQuery == null)
				throw new InvalidOperationException($"Placeholder with path '{sqlPlaceholder.Path}' and SQL '{sqlPlaceholder.Sql}' has no SelectQuery defined.");

			var key = new ColumnCacheKey(sqlPlaceholder.Path, placeholderType, sqlPlaceholder.SelectQuery, parentQuery);

			if (!asNew && _columnCache.TryGetValue(key, out var placeholder))
			{
				return placeholder.WithType(sqlPlaceholder.Type);
			}

			var alias = sqlPlaceholder.Alias;

			if (string.IsNullOrEmpty(alias))
			{
				if (sqlPlaceholder.TrackingPath is MemberExpression tme)
					alias = tme.Member.Name;
				else if (sqlPlaceholder.Path is MemberExpression me)
					alias = me.Member.Name;
			}

			/*

			// Left here for simplifying debugging

			var findStr = "Ref(TableContext[ID:1](13)(T: 14)::ElementTest).Id";
			if (sqlPlaceholder.Path.ToString().Contains(findStr))
			{
				var found = _columnCache.Keys.FirstOrDefault(c => c.Expression?.ToString().Contains(findStr) == true);
				if (found.Expression != null)
				{
					if (_columnCache.TryGetValue(found, out var current))
					{
						var fh = ExpressionEqualityComparer.Instance.GetHashCode(found.Expression);
						var kh = ExpressionEqualityComparer.Instance.GetHashCode(key.Expression);

						var foundHash = ColumnCacheKey.ColumnCacheKeyComparer.GetHashCode(found);
						var KeyHash   = ColumnCacheKey.ColumnCacheKeyComparer.GetHashCode(key);
					}
				}
			}

			*/

			var sql    = sqlPlaceholder.Sql;
			var idx    = sqlPlaceholder.SelectQuery.Select.AddNew(sql);
			var column = sqlPlaceholder.SelectQuery.Select.Columns[idx];

			if (!string.IsNullOrEmpty(alias))
			{
				column.RawAlias = alias;
			}

			placeholder = CreatePlaceholder(parentQuery, column, sqlPlaceholder.Path, sqlPlaceholder.ConvertType, alias, idx, trackingPath: sqlPlaceholder.TrackingPath);

			_columnCache[key] = placeholder;

			return placeholder;
		}

		#endregion
	}
}
