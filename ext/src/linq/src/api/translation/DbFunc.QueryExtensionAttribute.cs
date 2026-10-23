using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq
{
	using mooSQL.linq.utils;
	using mooSQL.linq.builder;
	using mooSQL.linq.mapping;
	using mooSQL.linq.clause;
    using mooSQL.data.Mapping;
    using mooSQL.data.model;
    using mooSQL.data;

    public partial class DbFunc
	{
		/// <summary>
		/// Defines custom query extension builder.
		/// </summary>
		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
		public class QueryExtensionAttribute : MappingAttribute
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.QueryExtensionAttribute"/> class.
			/// </summary>
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.QueryExtensionAttribute"/> class.
			/// </summary>
			public QueryExtensionAttribute(QueryExtensionScope scope, Type extensionBuilderType)
			{
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.QueryExtensionAttribute"/> class.
			/// </summary>

			public QueryExtensionAttribute(QueryExtensionScope scope, Type extensionBuilderType, params string[] extensionArguments)
			{
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
				ExtensionArguments   = extensionArguments;
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.QueryExtensionAttribute"/> class.
			/// </summary>

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, Type extensionBuilderType)
			{
				Configuration        = configuration;
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.QueryExtensionAttribute"/> class.
			/// </summary>

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, Type extensionBuilderType, params string[] extensionArguments)
			{
				Configuration        = configuration;
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
				ExtensionArguments   = extensionArguments;
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.QueryExtensionAttribute"/> class.
			/// </summary>

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, Type extensionBuilderType, string extensionArgument)
			{
				Configuration        = configuration;
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
				ExtensionArguments   = new [] { extensionArgument };
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.QueryExtensionAttribute"/> class.
			/// </summary>

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, Type extensionBuilderType, string extensionArgument0, string extensionArgument1)
			{
				Configuration        = configuration;
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
				ExtensionArguments   = new [] { extensionArgument0, extensionArgument1 };
			}
			/// <summary>
			/// Gets or sets Scope.
			/// </summary>

			public QueryExtensionScope Scope                { get; }
			/// <summary>
			/// 扩展构建器类型。
			/// </summary>
			public Type?               ExtensionBuilderType { get; set; }
			/// <summary>
			/// 扩展参数名数组。
			/// </summary>
			public string[]?           ExtensionArguments   { get; set; }
			/// <summary>
			/// Gets GetExtension.
			/// </summary>

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
			/// <summary>
			/// ExtendTable
			/// </summary>

			public virtual void ExtendTable(TableWord table, List<SqlQueryExtensionData> parameters)
			{
				(table.SqlQueryExtensions ??= new()).Add(GetExtension(parameters));
			}
			/// <summary>
			/// ExtendJoin
			/// </summary>

			public virtual void ExtendJoin(List<QueryExtension> extensions, List<SqlQueryExtensionData> parameters)
			{
				extensions.Add(GetExtension(parameters));
			}
			/// <summary>
			/// ExtendSubQuery
			/// </summary>

			public virtual void ExtendSubQuery(List<QueryExtension> extensions, List<SqlQueryExtensionData> parameters)
			{
				extensions.Add(GetExtension(parameters));
			}
			/// <summary>
			/// ExtendQuery
			/// </summary>

			public virtual void ExtendQuery(List<QueryExtension> extensions, List<SqlQueryExtensionData> parameters)
			{
				extensions.Add(GetExtension(parameters));
			}
/// <summary>
/// Gets GetObjectID.
/// </summary>
	

			public override string GetObjectID()
			{
				return $".{Configuration}.{(int)Scope}.{IdentifierBuilder.GetObjectID(ExtensionBuilderType)}.{IdentifierBuilder.GetObjectID(ExtensionArguments)}.";
			}
		}
	}
}
