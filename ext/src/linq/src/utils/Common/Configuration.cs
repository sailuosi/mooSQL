using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;



namespace mooSQL.linq.Common
{
	using System.Text;

	using Data;
	using Linq;

	/// <summary>
	/// Contains LINQ expression compilation options.
	/// </summary>
	public static class Compilation
	{
		private static Func<LambdaExpression,Delegate?>? _compiler;

		/// <summary>
		/// Sets LINQ expression compilation method.
		/// </summary>
		/// <param name="compiler">Method to use for expression compilation or <c>null</c> to reset compilation logic to defaults.</param>
		public static void SetExpressionCompiler(Func<LambdaExpression, Delegate?>? compiler)
		{
			_compiler = compiler;
		}

		/// <summary>
		/// Internal API.
		/// </summary>
		public static TDelegate CompileExpression<TDelegate>(this Expression<TDelegate> expression)
			where TDelegate : Delegate
		{
			return ((TDelegate?)_compiler?.Invoke(expression)) ?? expression.Compile();
		}

		/// <summary>
		/// Internal API.
		/// </summary>
		public static Delegate CompileExpression(this LambdaExpression expression)
		{
			return _compiler?.Invoke(expression) ?? expression.Compile();
		}
	}

	/// <summary>
	/// Contains global linq2db settings.
	/// </summary>
	
	public static class Configuration
	{
		/// <summary>
		/// If <c>true</c> - non-primitive and non-enum value types (structures) will be treated as scalar types (e.g. <see cref="DateTime"/>) during mapping;
		/// otherwise they will be treated the same way as classes.
		/// Default value: <c>true</c>.
		/// </summary>
		public static bool IsStructIsScalarType = true;

		/// <summary>
		/// If <c>true</c> - Enum values are stored as by calling ToString().
		/// Default value: <c>true</c>.
		/// </summary>
		public static bool UseEnumValueNameForStringColumns = true;

		/// <summary>
		/// Defines value to pass to <see cref="Task.ConfigureAwait(bool)"/> method for all linq2db internal await operations.
		/// Default value: <c>false</c>.
		/// </summary>
		public static bool ContinueOnCapturedContext;

		/// <summary>
		/// Enables mapping expression to be compatible with <see cref="CommandBehavior.SequentialAccess"/> behavior.
		/// Note that it doesn't switch linq2db to use <see cref="CommandBehavior.SequentialAccess"/> behavior for
		/// queries, so this optimization could be used for <see cref="CommandBehavior.Default"/> too.
		/// Default value: <c>false</c>.
		/// </summary>
		public static bool OptimizeForSequentialAccess;




		private static bool _useNullableTypesMetadata;
		/// <summary>
		/// Whether or not Nullable Reference Types annotations from C#
		/// are read and taken into consideration to determine if a
		/// column or association can be null.
		/// Nullable Types can be overriden with explicit CanBeNull
		/// annotations in [Column], [Association], or [Nullable].
		/// </summary>
		/// <remarks>Defaults to false.</remarks>
		public static bool UseNullableTypesMetadata
		{
			get => _useNullableTypesMetadata;
			set
			{
				// Can't change the default value of "false" on platforms where nullable metadata is unavailable.
				if (value) Mapping.Nullability.EnsureSupport();
				_useNullableTypesMetadata = value;
			}
		}

		/// <summary>
		/// Enables tracing of object materialization activity. It can significantly break performance if tracing consumer performs slow, so it is disabled by default.
		/// </summary>
		public static bool TraceMaterializationActivity { get; set; }

		public static class Data
		{
			/// <summary>
			/// Enables throwing of <see cref="ObjectDisposedException"/> when access disposed <see cref="DataConnection"/> instance.
			/// Default value: <c>true</c>.
			/// </summary>
			public static bool ThrowOnDisposed = true;

			/// <summary>
			/// Controls behavior of bulk copy timeout if <see cref="BulkCopyOptions.BulkCopyTimeout"/> is not provided.
			/// - if <c>true</c> - the current timeout on the <see cref="DataConnection"/> is used
			/// - if <c>false</c> - command timeout is infinite.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool BulkCopyUseConnectionCommandTimeout;
		}

		// N: supported in options

		/// <summary>
		/// LINQ query settings.
		/// </summary>
		
		public static class Linq
		{








			/// <summary>
			/// Specifies timeout when query will be evicted from cache since last execution of query.
			/// Default value is 1 hour.
			/// </summary>
			public static TimeSpan CacheSlidingExpiration
			{
				get => TimeSpan.FromHours(1);

			}


		}





		/// <summary>
		/// SQL generation global settings.
		/// </summary>
		
		public static class Sql
		{




#if SUPPORTS_COMPOSITE_FORMAT
			internal static CompositeFormat? AssociationAliasFormat { get; private set; } = CompositeFormat.Parse("a_{0}");
#else
			internal static string? AssociationAliasFormat { get; private set; } = "a_{0}";
#endif


		}
	}
}
