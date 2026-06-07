using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using mooSQL.data;
using mooSQL.utils;

namespace mooSQL.linq
{
	using Common.Internal;
	using mooSQL.linq.Extensions;
	using MappingExtensions = mooSQL.linq.Extensions.MappingExtensions;

	public partial class DbFunc
	{
		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
		public partial class ExtensionAttribute : ExpressionAttribute
		{
			public Type?     BuilderType     { get; set; }
			public object?   BuilderValue    { get; set; }

			/// <summary>编译时按 <see cref="NullsPosition"/> 参数追加 NULLS FIRST/LAST（替代 OrderItemBuilder）。</summary>
			public bool      AppendNullsPositionSuffix { get; set; }

			/// <summary>
			/// Defines in which order process extensions. Items will be ordered Descending.
			/// </summary>
			public int       ChainPrecedence { get; set; }

			public ExtensionAttribute(string expression): this(string.Empty, expression)
			{
			}

			public ExtensionAttribute(string configuration, string expression) : base(configuration, expression)
			{
				ExpectExpression = true;
				ServerSideOnly   = true;
				PreferServerSide = true;
				ChainPrecedence  = -1;
			}

			public ExtensionAttribute(Type builderType): this(string.Empty, builderType)
			{
			}

			public ExtensionAttribute(string configuration, Type builderType) : this(configuration, string.Empty)
			{
				BuilderType = builderType;
			}

			public static ExtensionAttribute[] GetExtensionAttributes(Expression expression, DBInstance mapping, bool forFirstConfiguration = true)
			{
				MemberInfo? memberInfo = expression.NodeType switch
				{
					ExpressionType.MemberAccess => ((MemberExpression)expression).Member,
					ExpressionType.Call         => ((MethodCallExpression)expression).Method,
					_                           => null
				};

				if (memberInfo == null)
					return ArrayCache.Empty<ExtensionAttribute>();

				var all = memberInfo.GetAttributes<ExtensionAttribute>(inherit: true);
				if (all.Length == 0)
					return all;

				var configuration = MappingExtensions.GetDialectConfiguration(mapping);
				var primary       = PickExtensionAttributes(all, configuration);

				if (forFirstConfiguration)
					return primary;

				var primaryTokens = new HashSet<string>(
					primary.Where(a => !string.IsNullOrEmpty(a.TokenName)).Select(a => a.TokenName!));

				return all
					.Where(a => !string.IsNullOrEmpty(a.TokenName) && !primaryTokens.Contains(a.TokenName!))
					.Where(a => !string.IsNullOrEmpty(a.Configuration)
					            && (configuration == null || !string.Equals(a.Configuration, configuration, StringComparison.OrdinalIgnoreCase)))
					.ToArray();
			}

			static ExtensionAttribute[] PickExtensionAttributes(ExtensionAttribute[] attrs, string? configuration)
			{
				if (attrs.Length <= 1)
					return attrs;

				if (configuration != null)
				{
					var dialectSpecific = attrs
						.Where(a => string.Equals(a.Configuration, configuration, StringComparison.OrdinalIgnoreCase))
						.ToArray();
					if (dialectSpecific.Length > 0)
						return dialectSpecific;
				}

				var defaults = attrs.Where(a => string.IsNullOrEmpty(a.Configuration)).ToArray();
				return defaults.Length > 0 ? defaults : attrs;
			}

			public static Expression ExcludeExtensionChain(DBInstance mapping, Expression expr, out bool isQueryable)
			{
				var current = expr;
				isQueryable = false;

				while (true)
				{
					var attributes = GetExtensionAttributes(current, mapping);

					if (attributes.Length == 0)
						break;

					switch (current.NodeType)
					{
						case ExpressionType.MemberAccess :
							{
								var memberExpr = (MemberExpression)current;
								current        = memberExpr.Expression!;

								break;
							}

						case ExpressionType.Call :
							{
								var call = (MethodCallExpression) current;

								if (call.Method.IsStatic && call.Method.DeclaringType != null)
								{
									isQueryable = false;
									var firstArgType = call.Arguments[0].Type;
									if (call.Arguments.Count > 0 && typeof(IQueryableContainer).IsSameOrParentOf(firstArgType) || typeof(IEnumerable<>).IsSameOrParentOf(firstArgType))
									{
										var paramAttribute = call.Method.GetParameters()[0].GetAttribute<ExprParameterAttribute>();
										if (paramAttribute == null ||
										    paramAttribute.ParameterKind == ExprParameterKind.Default ||
										    paramAttribute.ParameterKind == ExprParameterKind.Sequence)
										{
											current     = call.Arguments[0];
											isQueryable = typeof(IQueryableContainer).IsSameOrParentOf(current.Type);
										}
										else
											return current;
									}
									else
										return current;
								}
								else
									current = call.Object!;

								break;
							}
						default:
							{
								return current;
							}
					}
				}

				return current;
			}

		}
	}
}
