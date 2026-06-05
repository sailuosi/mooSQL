using System;
using System.Linq.Expressions;
using mooSQL.data.Mapping;

namespace mooSQL.linq.Mapping
{
	/// <summary>
	/// Defines relation between tables or views for LINQ association navigation.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public sealed class AssociationAttribute : MappingAttribute
	{
		/// <summary>Comma-separated association key members on this side.</summary>
		public string? ThisKey { get; set; }

		/// <summary>Comma-separated association key members on the other side.</summary>
		public string? OtherKey { get; set; }

		/// <summary>Static property or method name returning predicate expression.</summary>
		public string? ExpressionPredicate { get; set; }

		/// <summary>Inline predicate expression.</summary>
		public Expression? Predicate { get; set; }

		/// <summary>Static property or method name returning custom query expression.</summary>
		public string? QueryExpressionMethod { get; set; }

		/// <summary>Inline custom query expression.</summary>
		public Expression? QueryExpression { get; set; }

		/// <summary>Storage member for LoadWith.</summary>
		public string? Storage { get; set; }

		public string? AssociationSetterExpressionMethod { get; set; }

		public Expression? AssociationSetterExpression { get; set; }

		internal bool? ConfiguredCanBeNull;

		/// <summary>When true, association generates outer join semantics.</summary>
		public bool CanBeNull
		{
			get => ConfiguredCanBeNull ?? true;
			set => ConfiguredCanBeNull = value;
		}

		public string? AliasName { get; set; }

		public string[] GetThisKeys() => AssociationDescriptor.ParseKeys(ThisKey);

		public string[] GetOtherKeys() => AssociationDescriptor.ParseKeys(OtherKey);

		public override string GetObjectID()
		{
			return $".{Configuration}.{ThisKey}.{OtherKey}.{ExpressionPredicate}.{QueryExpressionMethod}.{Storage}.{AssociationSetterExpressionMethod}.{(CanBeNull ? 1 : 0)}.{AliasName}.";
		}
	}
}
