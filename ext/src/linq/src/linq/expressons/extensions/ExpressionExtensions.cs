using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;



namespace mooSQL.linq.Expressions
{
	using Common;
	using Extensions;
	using Mapping;
    using mooSQL.utils;
    using Reflection;

	public static class ExpressionExtensions
	{
		#region GetDebugView

		private static Func<Expression,string>? _getDebugView;

		/// <summary>
		/// 获取提供的表达式的DebugView内部属性值
		/// </summary>
		/// <param name="expression">要获取DebugView的表达式</param>
		/// <returns>DebugView值</returns>
#if NET6_0_OR_GREATER
		[DynamicDependency("get_DebugView", typeof(Expression))]
#endif
		public static string GetDebugView(this Expression expression)
		{
			if (_getDebugView == null)
			{
				var p = Expression.Parameter(typeof(Expression));

				try
				{
					var l = Expression.Lambda<Func<Expression,string>>(
						ExpressionHelper.PropertyOrField(p, "DebugView"),
						p);

					_getDebugView = l.CompileExpression();
				}
				catch (ArgumentException)
				{
					_getDebugView = e => e.ToString();
				}
			}

			return _getDebugView(expression);
		}

		#endregion

		#region GetCount

		/// <summary>
		/// 返回匹配给定<paramref name="func"/>的表达式项总数
		/// </summary>
		/// <param name="expr">要计数的表达式树</param>
		/// <param name="context">表达式树访问者上下文</param>
		/// <param name="func">用于测试给定表达式是否应被计数的谓词</param>
		public static int GetCount<TContext>(this Expression expr, TContext context, Func<TContext, Expression, bool> func)
		{
			var ctx = new CountContext<TContext>(context, func);

			expr.Visit(ctx, static (context, e) =>
			{
				if (context.Func(context.Context, e))
					context.Count++;
			});

			return ctx.Count;
		}

		private sealed class CountContext<TContext>
		{
			public CountContext(TContext context, Func<TContext, Expression, bool> func)
			{
				Context = context;
				Func    = func;
			}

			public readonly TContext                         Context;
			public          int                              Count;
			public readonly Func<TContext, Expression, bool> Func;
		}

		#endregion

		#region Visit
		/// <summary>
		/// 为<paramref name="expr"/>的每个子节点调用给定的<paramref name="func"/>
		/// </summary>
		public static void Visit<TContext>(this Expression expr, TContext context, Action<TContext, Expression> func)
		{
			if (expr == null)
				return;

			new VisitActionVisitor<TContext>(context, func).Visit(expr);
		}

		/// <summary>
		/// 为<paramref name="expr"/>的每个节点调用给定的<paramref name="func"/>
		/// 如果<paramref name="func"/>返回false，则不会枚举测试表达式的子节点
		/// </summary>
		public static void Visit<TContext>(this Expression expr, TContext context, Func<TContext, Expression, bool> func)
		{
			if (expr == null || !func(context, expr))
				return;

			new VisitFuncVisitor<TContext>(context, func).Visit(expr);
		}

		#endregion

		#region Find

		/// <summary>
		/// 枚举表达式树并返回<paramref name="exprToFind"/>（如果它包含在<paramref name="expr"/>中）
		/// </summary>
		public static Expression? Find(this Expression? expr, Expression exprToFind)
		{
			return expr.Find(exprToFind, static (exprToFind, e) => e == exprToFind);
		}

		/// <summary>
		/// Enumerates the expression tree and returns the <paramref name="exprToFind"/> if it's
		/// contained within the <paramref name="expr"/>.
		/// </summary>
		public static Expression? Find(this Expression? expr, Expression exprToFind, IEqualityComparer<Expression> comparer)
		{
			return expr.Find((exprToFind, comparer), static (ctx, e) => ctx.comparer.Equals(e, ctx.exprToFind));
		}

		/// <summary>
		/// 枚举给定的<paramref name="expr"/>并返回第一个匹配给定<paramref name="func"/>的子表达式
		/// 如果没有找到表达式，则返回null
		/// </summary>
		public static Expression? Find<TContext>(this Expression? expr, TContext context, Func<TContext,Expression, bool> func)
		{
			if (expr == null)
				return expr;

			return new FindVisitor<TContext>(context, func).Find(expr);
		}

		#endregion

		#region Transform

		public static Expression Replace(this Expression expression, Expression toReplace, Expression replacedBy)
		{
			return Transform(
				expression,
				(toReplace, replacedBy),
				static (context, e) => e == context.toReplace ? context.replacedBy : e);
		}

		public static Expression Replace(this Expression expression, Expression toReplace, Expression replacedBy, IEqualityComparer<Expression> equalityComparer)
		{
			return Transform(
				expression,
				(toReplace, replacedBy, equalityComparer),
				static (context, e) => context.equalityComparer.Equals(e, context.toReplace) ? context.replacedBy : e);
		}

		public static Expression Replace(this Expression expression, IReadOnlyDictionary<Expression, Expression> replaceMap)
		{
			return Transform(
				expression,
				replaceMap,
				static (map, e) => map.TryGetValue(e, out var newExpression) ? newExpression : e);
		}

		/// <summary>
		/// 返回<paramref name="lambda"/>的主体，但将其第一个参数替换为<paramref name="exprToReplaceParameter"/>表达式
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter)
		{
			return Transform(
				lambda.Body,
				(parameter: lambda.Parameters[0], exprToReplaceParameter),
				static (context, e) => e == context.parameter ? context.exprToReplaceParameter : e);
		}

		/// <summary>
		/// 返回<paramref name="lambda"/>的主体，但将其前两个参数替换为给定的替换表达式
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter1, Expression exprToReplaceParameter2)
		{
			return Transform(
				lambda.Body,
				(parameters: lambda.Parameters, exprToReplaceParameter1, exprToReplaceParameter2),
				static (context, e) =>
					e == context.parameters[0] ? context.exprToReplaceParameter1 :
					e == context.parameters[1] ? context.exprToReplaceParameter2 : e);
		}

		/// <summary>
		/// 返回<paramref name="lambda"/>的主体，但将其前三个参数替换为给定的替换表达式
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter1, Expression exprToReplaceParameter2, Expression exprToReplaceParameter3)
		{
			return Transform(
				lambda.Body,
				(parameters: lambda.Parameters, exprToReplaceParameter1, exprToReplaceParameter2, exprToReplaceParameter3),
				static (context, e) =>
					e.NodeType != ExpressionType.Parameter ? e                               :
					e == context.parameters[0]             ? context.exprToReplaceParameter1 :
					e == context.parameters[1]             ? context.exprToReplaceParameter2 :
					e == context.parameters[2]             ? context.exprToReplaceParameter3 : e);
		}

		/// <summary>
		/// 返回<paramref name="lambda"/>的主体，但将所有参数替换为给定的替换表达式
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, params Expression[] replacement)
		{
			return Transform(lambda.Body, e =>
			{
				if (e.NodeType == ExpressionType.Parameter)
				{
					var idx = lambda.Parameters.IndexOf((ParameterExpression)e);
					if (idx >= 0 && idx < replacement.Length)
						return replacement[idx];
				}

				return e;
			});
		}

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces all parameters
		/// with the given replace expressions.
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, ReadOnlyCollection<Expression> replacement)
		{
			return Transform(lambda.Body, e =>
			{
				if (e.NodeType == ExpressionType.Parameter)
				{
					var idx = lambda.Parameters.IndexOf((ParameterExpression)e);
					if (idx >= 0 && idx < replacement.Count)
						return replacement[idx];
				}

				return e;
			});
		}

		/// <summary>
		/// 枚举表达式 <paramref name="expr"/> 并用给定的方法 <paramref name="func"/>进行替换
		/// </summary>
		/// <returns>The modified expression.</returns>
		[return: NotNullIfNotNull(nameof(expr))]
		public static Expression? Transform<TContext>(this Expression? expr, TContext context,                          Func<TContext, Expression, Expression> func)
		{
			if (expr == null)
				return null;

			return new TransformVisitor<TContext>(context, func).Transform(expr);
		}

		/// <summary>
		/// 枚举<paramref name="expr"/>的表达式树，并可能用给定<paramref name="func"/>的返回值替换表达式
		/// </summary>
		/// <returns>修改后的表达式</returns>
		[return: NotNullIfNotNull(nameof(expr))]
		public static Expression? Transform(this Expression? expr,                          Func<Expression, Expression> func)
		{
			if (expr == null)
				return null;

			return new TransformVisitor<Func<Expression, Expression>>(func, static (f, e) => f(e)).Transform(expr);
		}

		#endregion

		#region Transform2

		[return: NotNullIfNotNull(nameof(expr))]
		public static Expression? Transform<TContext>(this Expression? expr, TContext context, Func<TContext, Expression, TransformInfo> func)
		{
			if (expr == null)
				return null;

			return new TransformInfoVisitor<TContext>(context, func).Transform(expr);
		}

		[return: NotNullIfNotNull(nameof(expr))]
		public static Expression? Transform(this Expression? expr, Func<Expression, TransformInfo> func)
		{
			if (expr == null)
				return null;

			return new TransformInfoVisitor<Func<Expression, TransformInfo>>(func, static (f, e) => f(e)).Transform(expr);
		}

		#endregion

		public static Expression GetMemberGetter(MemberInfo mi, Expression obj)
		{
			if (mi is DynamicColumnInfo)
			{
				return Expression.Call(
					Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(mi.GetMemberType()),
					obj,
					Expression.Constant(mi.Name));
			}
			else
				return Expression.MakeMemberAccess(obj, mi);
		}
	}
}
