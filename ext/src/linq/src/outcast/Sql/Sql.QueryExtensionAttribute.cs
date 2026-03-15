using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq
{
	using Common;
	using Linq.Builder;
	using Common.Internal;
	using Mapping;
	using SqlQuery;
    using mooSQL.data.Mapping;
    using mooSQL.data.model;
    using mooSQL.data;

    public partial class Sql
	{
		/// <summary>
		/// Defines custom query extension builder.
		/// </summary>
		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
		public class QueryExtensionAttribute : MappingAttribute
		{
			public QueryExtensionAttribute(QueryExtensionScope scope, Type extensionBuilderType)
			{
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
			}

			public QueryExtensionAttribute(QueryExtensionScope scope, Type extensionBuilderType, params string[] extensionArguments)
			{
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
				ExtensionArguments   = extensionArguments;
			}

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, Type extensionBuilderType)
			{
				Configuration        = configuration;
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
			}

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, Type extensionBuilderType, params string[] extensionArguments)
			{
				Configuration        = configuration;
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
				ExtensionArguments   = extensionArguments;
			}

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, Type extensionBuilderType, string extensionArgument)
			{
				Configuration        = configuration;
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
				ExtensionArguments   = new [] { extensionArgument };
			}

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, Type extensionBuilderType, string extensionArgument0, string extensionArgument1)
			{
				Configuration        = configuration;
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
				ExtensionArguments   = new [] { extensionArgument0, extensionArgument1 };
			}

			public QueryExtensionScope Scope                { get; }
			/// <summary>
			/// Instance of <see cref="ISqlExtensionBuilder"/>.
			/// </summary>
			public Type?               ExtensionBuilderType { get; set; }
			public string[]?           ExtensionArguments   { get; set; }

			public virtual QueryExtension GetExtension(List<SqlQueryExtensionData> parameters)
			{
				var arguments = new Dictionary<string,IExpWord>();

				foreach (var item in parameters)
					arguments.Add(item.Name, item.SqlExpression!);

				if (ExtensionArguments is not null)
				{
					arguments.Add(".ExtensionArguments.Count",  new ValueWord(ExtensionArguments.Length));

					for (var i = 0; i < ExtensionArguments.Length; i++)
						arguments.Add($".ExtensionArguments.{i}", new ValueWord(ExtensionArguments[i]));
				}

				return new QueryExtension()
				{
					Configuration = Configuration,
					Scope         = Scope,
					BuilderType   = ExtensionBuilderType,
					Arguments     = arguments
				};
			}

			public virtual void ExtendTable(TableWord table, List<SqlQueryExtensionData> parameters)
			{
				(table.SqlQueryExtensions ??= new()).Add(GetExtension(parameters));
			}

			public virtual void ExtendJoin(List<QueryExtension> extensions, List<SqlQueryExtensionData> parameters)
			{
				extensions.Add(GetExtension(parameters));
			}

			public virtual void ExtendSubQuery(List<QueryExtension> extensions, List<SqlQueryExtensionData> parameters)
			{
				extensions.Add(GetExtension(parameters));
			}

			public virtual void ExtendQuery(List<QueryExtension> extensions, List<SqlQueryExtensionData> parameters)
			{
				extensions.Add(GetExtension(parameters));
			}

			public static QueryExtensionAttribute[] GetExtensionAttributes(Expression expression, DBInstance mapping)
			{
				MemberInfo memberInfo;

				switch (expression.NodeType)
				{
					case ExpressionType.MemberAccess : memberInfo = ((MemberExpression)    expression).Member; break;
					case ExpressionType.Call         : memberInfo = ((MethodCallExpression)expression).Method; break;
					default                          : return new QueryExtensionAttribute[] { };
				}
				return null;
				//return mapping.GetAttributes<QueryExtensionAttribute>(memberInfo.ReflectedType!, memberInfo, forFirstConfiguration: true);
			}

			public override string GetObjectID()
			{
				return $".{Configuration}.{(int)Scope}.{IdentifierBuilder.GetObjectID(ExtensionBuilderType)}.{IdentifierBuilder.GetObjectID(ExtensionArguments)}.";
			}
		}
	}
}
