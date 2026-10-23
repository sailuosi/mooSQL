using System;

namespace mooSQL.linq;

/// <summary>Ext LINQ Extension 链函数族（Analytic Over、StringAgg、Convert、Row、GroupBy）。</summary>
public static partial class SooFunctionExtension
{
	/// <summary>
	/// ISqlExtension
	/// </summary>
	/// <summary>
	/// ISqlExtension
	/// </summary>
	public interface ISqlExtension
	{
	}
	/// <summary>
	/// Ext
	/// </summary>

	public static ISqlExtension? Ext => null;
	/// <summary>
	/// AggregateModifier
	/// </summary>

	public enum AggregateModifier
	{
		/// <summary>
		/// None
		/// </summary>
		/// <summary>
		/// None
		/// </summary>
		None,
		/// <summary>
		/// Distinct
		/// </summary>
		/// <summary>
		/// Distinct
		/// </summary>
		Distinct,
		/// <summary>
		/// All
		/// </summary>
		/// <summary>
		/// All
		/// </summary>
		All,
	}
	/// <summary>
	/// From
	/// </summary>

	public enum From
	{
		/// <summary>
		/// None
		/// </summary>
		/// <summary>
		/// None
		/// </summary>
		None,
		/// <summary>
		/// First
		/// </summary>
		/// <summary>
		/// First
		/// </summary>
		First,
		/// <summary>
		/// Last
		/// </summary>
		/// <summary>
		/// Last
		/// </summary>
		Last
	}
	/// <summary>
	/// Nulls
	/// </summary>

	public enum Nulls
	{
		/// <summary>
		/// None
		/// </summary>
		/// <summary>
		/// None
		/// </summary>
		None,
		/// <summary>
		/// Respect
		/// </summary>
		/// <summary>
		/// Respect
		/// </summary>
		Respect,
		/// <summary>
		/// Ignore
		/// </summary>
		/// <summary>
		/// Ignore
		/// </summary>
		Ignore
	}
	/// <summary>
	/// NullsPosition
	/// </summary>

	public enum NullsPosition
	{
		/// <summary>
		/// None
		/// </summary>
		/// <summary>
		/// None
		/// </summary>
		None,
		/// <summary>
		/// First
		/// </summary>
		/// <summary>
		/// First
		/// </summary>
		First,
		/// <summary>
		/// Last
		/// </summary>
		/// <summary>
		/// Last
		/// </summary>
		Last
	}

	/// <summary>Alias for <see cref="DbFunc.ExtensionAttribute"/> on Extension-chain APIs.</summary>
	[Serializable]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
	public class ExtensionAttribute : DbFunc.ExtensionAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SooFunctionExtension"/> class.
		/// </summary>
		/// <summary>
		/// Initializes a new instance of the <see cref="SooFunctionExtension"/> class.
		/// </summary>
		public ExtensionAttribute(string expression) : base(expression)
		{
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="SooFunctionExtension"/> class.
		/// </summary>

		public ExtensionAttribute(string configuration, string expression) : base(configuration, expression)
		{
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="SooFunctionExtension"/> class.
		/// </summary>

		public ExtensionAttribute(Type builderType) : base(builderType)
		{
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="SooFunctionExtension"/> class.
		/// </summary>

		public ExtensionAttribute(string configuration, Type builderType) : base(configuration, builderType)
		{
		}
	}
}
