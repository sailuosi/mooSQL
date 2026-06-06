using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
	using Extensions;
	using mooSQL.linq.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using mooSQL.data;
	using mooSQL.data.model;
	using mooSQL.utils;

    partial class ClauseSqlTranslator
    {
        #region BuildProjection
		public Expression BuildProjection(IClauseContext? forContext, Expression path, ProjectFlags flags)
		{
			var currentContext = forContext;
#if DEBUG
			Expression ExecuteMake(IClauseContext context, Expression expr, ProjectFlags projectFlags)
			{
				var counter = ++_makeCounter;

				Debug.WriteLine($"({counter.ToString(CultureInfo.InvariantCulture)})ExecuteMake ({projectFlags}):");
				Debug.WriteLine($"\tCtx: {ClauseContextDebuggingHelper.GetContextInfo(currentContext)}");
				Debug.WriteLine($"\tPath: {path}");
				Debug.WriteLine($"\tExpr: {expr}");

				var result = context.BuildProjection(expr, projectFlags);

				Debug.WriteLine($"({counter.ToString(CultureInfo.InvariantCulture)})Result ({projectFlags}): {result}");
				Debug.WriteLine("");

				return result;
			}
#else
			static Expression ExecuteMake(IClauseContext context, Expression expr, ProjectFlags projectFlags)
			{
				return context.BuildProjection(expr, projectFlags);
			}
#endif

#if DEBUG
			static void DebugCacheHit(IClauseContext? context, Expression original, Expression cached, ProjectFlags projectFlags)
			{
				Debug.WriteLine($"Cache hit for: {original}, {projectFlags}");
				Debug.WriteLine($"\tResult: {cached}");

				if (!projectFlags.IsTest() && (projectFlags.IsExpression() || projectFlags.IsSql()))
				{
				}
			}
#endif

			ContextRefExpression? CalcRootContext(Expression expressionToCheck)
			{
				expressionToCheck = expressionToCheck.UnwrapConvert();

				if (expressionToCheck is ContextRefExpression contextRef)
					return contextRef;

				if (expressionToCheck is MemberExpression me)
				{
					if (me.Expression is null)
						return null;
					return CalcRootContext(me.Expression);
				}

				if (expressionToCheck is MethodCallExpression mc && mc.IsQueryable())
				{
					return CalcRootContext(mc.Arguments[0]);
				}

				return null;
			}

			// nothing to project here
			if (path.NodeType   == ExpressionType.Parameter
				|| path.NodeType == ExpressionType.Lambda
				|| path.NodeType == ExpressionType.Extension && path is SqlPlaceholderExpression or SqlGenericConstructorExpression)
			{
				return path;
			}

			if ((flags & (ProjectFlags.Root | ProjectFlags.AggregationRoot | ProjectFlags.AssociationRoot | ProjectFlags.ExtractProjection | ProjectFlags.Table)) == 0)
			{
				// try to find already converted to SQL
				var sqlKey = new SqlCacheKey(path, null, null, forContext?.SelectQuery, flags.SqlFlag());
				if (_cachedSql.TryGetValue(sqlKey, out var cachedSql) && cachedSql is SqlPlaceholderExpression)
				{
					return cachedSql;
				}
			}

			var shouldCache = !flags.IsTest() && null != path.Find(1, (_, e) => e is ContextRefExpression);

			var key = new SqlCacheKey(path, null, null, forContext?.SelectQuery, flags);

			Expression? expression;

			if (shouldCache && _expressionCache.TryGetValue(key, out expression) && expression.Type == path.Type && expression is not SqlErrorExpression)
			{
				if (!ExpressionEqualityComparer.Instance.Equals(path, expression))
				{
#if DEBUG
					DebugCacheHit(currentContext, path, expression, flags);
#endif
					return expression;
				}
			}

			var doNotProcess = false;
			expression = null;

			ContextRefExpression? rootContext = null;

			if (path is MemberExpression { Expression: not null } memberExpression)
			{
				var declaringType = memberExpression.Member.DeclaringType;
				if (declaringType != null && declaringType != memberExpression.Expression.Type)
				{
					memberExpression = memberExpression.Update(SequenceHelper.EnsureType(memberExpression.Expression, declaringType));
					return BuildProjection(currentContext, memberExpression, flags);
				}

				if (memberExpression.Member.IsNullableValueMember())
				{
					var corrected = BuildProjection(currentContext, memberExpression.Expression, flags);
					if (corrected.Type != path.Type)
					{
						corrected = Expression.Convert(corrected, path.Type);
					}
					return BuildProjection(currentContext, corrected, flags);
				}

				if (memberExpression.Expression.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					var unary = (UnaryExpression)memberExpression.Expression;
					if (unary.Operand is ContextRefExpression contextRef)
					{
						memberExpression = memberExpression.Update(contextRef.WithType(memberExpression.Expression.Type));
						return BuildProjection(currentContext, memberExpression, flags);
					}
				}

				rootContext = CalcRootContext(memberExpression.Expression);

				if (rootContext != null)
				{
					currentContext = rootContext.BuildContext;

					// SetOperationContext can know how to process such path without preparing

					var corrected = ExecuteMake(rootContext.BuildContext, path, flags);

					if (!ExpressionEqualityComparer.Instance.Equals(corrected, path) &&
						corrected is not DefaultValueExpression && corrected is not SqlErrorExpression)
					{
						var newCorrected = BuildProjection(rootContext.BuildContext, corrected, flags);

						if (newCorrected is SqlErrorExpression sqlError)
						{
							if (sqlError.IsCritical)
								return sqlError;
							newCorrected = corrected;
						}

						if (newCorrected is SqlPlaceholderExpression placeholder)
						{
							newCorrected = placeholder.WithTrackingPath(path);
						}

						if (ExpressionEqualityComparer.Instance.Equals(corrected, newCorrected))
							return corrected;

						return BuildProjection(rootContext.BuildContext, newCorrected, flags);
					}
				}

				var root = BuildProjection(currentContext, memberExpression.Expression, flags.RootFlag());

				// Association may cause such situation
				if (root is SqlErrorExpression rootError)
				{
					return rootError.WithType(path.Type);
				}

				if (root is MethodCallExpression mce && mce.IsQueryable() && currentContext != null)
				{
					var subqueryExpression = TryGetSubQueryExpression(currentContext, root, null, flags, out var isSequence, out var corrected);
					if (subqueryExpression != null)
					{
						root = subqueryExpression;
						if (subqueryExpression.Type != root.Type)
						{
							root = SqlAdjustTypeExpression.AdjustType(root, root.Type, DBLive);
						}
					}
					else if (isSequence)
					{
						if (corrected != null)
						{
							// Failed to build sequence, but we transformed First/FirstOrDefault.
							return memberExpression.Update(corrected);
						}

						// Failed to build sequence. Do not continue.
						return memberExpression;
					}
					
				}

				var newPath = memberExpression;
				if (!ReferenceEquals(root, memberExpression.Expression))
				{
					newPath = memberExpression.Update(SequenceHelper.EnsureType(root, memberExpression.Expression.Type));
				}

				path = newPath;

				if (!flags.IsTraverse() && IsAssociation(newPath, out _))
				{
					if (root is ContextRefExpression contextRef)
					{
						expression = TryCreateAssociation(newPath, contextRef, currentContext, flags);
						if (expression is SqlErrorExpression)
							return expression;
					}
				}

				rootContext = CalcRootContext(root);
			}
			else if (path is MethodCallExpression mc)
			{
				if (mc.Method.IsSqlPropertyMethodEx())
				{
					var memberInfo   = MemberHelper.GetMemberInfo(mc);
					var memberAccess = Expression.MakeMemberAccess(mc.Arguments[0], memberInfo);
					return BuildProjection(currentContext, memberAccess, flags);
				}

				if (mc.Method.Name == nameof(DbFunc.Alias) && mc.Method.DeclaringType == typeof(DbFunc))
				{
					var translated = BuildProjection(currentContext, mc.Arguments[0], flags);
					if (ReferenceEquals(mc.Arguments[0], translated))
					{
						translated = mc;
					}
					else if (translated is SqlPlaceholderExpression placeholder)
					{
						translated = placeholder.WithAlias(mc.Arguments[1].EvaluateExpression() as string);
					}
					else
					{
						if (!flags.IsRoot())
						{
							var args = mc.Arguments.ToArray();
							args[0]    = translated;
							translated = mc.Update(mc.Object, args);
						}
					}

					return translated;
				}

				if (IsAssociation(mc, out _))
				{
					var arguments = mc.Arguments;
					if (arguments.Count == 0)
						throw new InvalidOperationException("Association methods should have at least one parameter");

					var firstArgument = mc.Method.IsStatic ? arguments[0] : mc.Object!;

					if (firstArgument == null)
						throw new InvalidOperationException();

					var rootArgument = BuildProjection(currentContext, firstArgument, flags.RootFlag());

					if (!ReferenceEquals(rootArgument, firstArgument))
					{
						if (mc.Method.IsStatic)
						{
							var argumentsArray = arguments.ToArray();
							argumentsArray[0] = rootArgument;

							mc = mc.Update(mc.Object, argumentsArray);
						}
						else
						{
							mc = mc.Update(rootArgument, mc.Arguments);
						}
					}

					if (rootArgument is ContextRefExpression contextRef)
					{
						expression  = TryCreateAssociation(mc, contextRef, currentContext, flags);
						rootContext = expression as ContextRefExpression;
					}
				}
			}
			else if (path is ContextRefExpression contextRef)
			{
				rootContext = contextRef;
			}
			else if (path is SqlGenericParamAccessExpression paramAccessExpression)
			{
				var root = paramAccessExpression.Constructor;
				while (root is SqlGenericParamAccessExpression pa)
				{
					root = pa.Constructor;
				}

				if (root is ContextRefExpression contextRefExpression)
				{
					rootContext = contextRefExpression;
				}
			}
			else if (path.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			{
				var unary      = (UnaryExpression)path;

				expression   = BuildProjection(currentContext, unary.Operand, flags);
				if (!flags.IsTable() && expression.Type != path.Type)
				{
					expression = Expression.MakeUnary(path.NodeType, expression, unary.Type, unary.Method);
				}
				doNotProcess = true;
			}
			else if (path.NodeType == ExpressionType.TypeAs && currentContext != null)
			{
				var unary     = (UnaryExpression)path;
				var testExpr  = MakeIsPredicateExpression(currentContext, Expression.TypeIs(unary.Operand, unary.Type));
				var trueCase  = Expression.Convert(unary.Operand, unary.Type);
				var falseCase = new DefaultValueExpression(DBLive, unary.Type);

				if (testExpr is ConstantExpression constExpr)
				{
					if (constExpr.Value is true)
						expression  = trueCase;
					else
						expression = falseCase;
				}
				else
				{
					doNotProcess = true;
					expression   = Expression.Condition(testExpr, trueCase, falseCase);
				}
			}
			else if (path.NodeType == ExpressionType.TypeIs && currentContext != null)
			{
				var typeBinary = (TypeBinaryExpression)path;
				expression = MakeIsPredicateExpression(currentContext, typeBinary);
				doNotProcess = true;
			}

			if (expression == null)
			{
				if (rootContext != null)
				{
					currentContext = rootContext.BuildContext;
					expression     = ExecuteMake(currentContext, path, flags);
				}
				else
					expression = path;
			}

			if (!doNotProcess)
			{
				if (!ExpressionEqualityComparer.Instance.Equals(expression, path))
				{
					// Do recursive again
					var convertedAgain = BuildProjection(currentContext, expression, flags);
					if (convertedAgain is not SqlErrorExpression)
						expression = convertedAgain;
				}
				else
				{
					var handled = false;

					if (flags.IsExpression() && path.NodeType == ExpressionType.NewArrayInit)
					{
						expression = path;
						handled    = true;
					}

					if (!handled && (flags.IsSql() || flags.IsExpression()))
					{
						// Handling subqueries
						//

						var ctx = rootContext?.BuildContext ?? currentContext;
						if (ctx != null)
						{
							var subqueryExpression = TryGetSubQueryExpression(ctx, path, null, flags, out var isSequence, out var corrected);
							if (subqueryExpression != null)
							{
								if (subqueryExpression is SqlErrorExpression)
								{
									expression = subqueryExpression;
								}
								else
								{
									expression = BuildProjection(ctx, subqueryExpression, flags);
									if (expression.Type != path.Type)
									{
										expression = SqlAdjustTypeExpression.AdjustType(expression, path.Type, DBLive);
									}
								}

								handled = true;
							}
							else if (isSequence)
							{
								if (corrected != null)
								{
									// Failed to build sequence, but we transformed First/FirstOrDefault.
									expression = corrected;
								}

								handled = true;
							}
						}
					}

					if (!handled && flags.HasFlag(ProjectFlags.Expression) && CanBeCompiled(path, true))
					{
						expression = path;
						handled    = true;
					}

				}
			}

			if (expression is SqlPlaceholderExpression placeholderExpression)
			{
				expression = placeholderExpression.WithTrackingPath(path);
			}

			if (shouldCache)
			{
				_expressionCache[key] = expression;

				if (!flags.HasFlag(ProjectFlags.Test))
				{
					if ((flags.HasFlag(ProjectFlags.SQL) ||
					     flags.HasFlag(ProjectFlags.Keys)) && expression is SqlPlaceholderExpression)
					{
						var anotherKey = new SqlCacheKey(path, null, null, forContext?.SelectQuery, ProjectFlags.Expression);
						_expressionCache[anotherKey] = expression;

						if (flags.HasFlag(ProjectFlags.Keys))
						{
							anotherKey                   = new SqlCacheKey(path, null, null, forContext?.SelectQuery, ProjectFlags.Expression | ProjectFlags.Keys);
							_expressionCache[anotherKey] = expression;
						}
					}
				}
			}

			return expression;
		}
        #endregion
    }
}
