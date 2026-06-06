using System;

namespace mooSQL.linq;

/// <summary>Ext LINQ Extension 链函数族（Analytic Over、StringAgg、Convert、Row、GroupBy）。</summary>
public static partial class SooFunctionExtension
{
	public interface ISqlExtension
	{
	}

	public static ISqlExtension? Ext => null;

	public enum AggregateModifier
	{
		None,
		Distinct,
		All,
	}

	public enum From
	{
		None,
		First,
		Last
	}

	public enum Nulls
	{
		None,
		Respect,
		Ignore
	}

	public enum NullsPosition
	{
		None,
		First,
		Last
	}

	/// <summary>Alias for <see cref="DbFunc.ExtensionAttribute"/> on Extension-chain APIs.</summary>
	[Serializable]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
	public class ExtensionAttribute : DbFunc.ExtensionAttribute
	{
		public ExtensionAttribute(string expression) : base(expression)
		{
		}

		public ExtensionAttribute(string configuration, string expression) : base(configuration, expression)
		{
		}

		public ExtensionAttribute(Type builderType) : base(builderType)
		{
		}

		public ExtensionAttribute(string configuration, Type builderType) : base(configuration, builderType)
		{
		}
	}
}
