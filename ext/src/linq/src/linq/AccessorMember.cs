using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq
{
	/// <summary>
	/// AccessorMember type.
	/// </summary>
	[DebuggerDisplay("Member: {MemberInfo.Name}")]
	public class AccessorMember
	{
		/// <summary>
		/// AccessorMember type.
		/// </summary>
		/// <summary>
		/// AccessorMember type.
		/// </summary>
		public AccessorMember(MemberInfo memberInfo)
		{
			MemberInfo = memberInfo;
		}
		/// <summary>
		/// AccessorMember type.
		/// </summary>

		public AccessorMember(MemberInfo memberInfo, ReadOnlyCollection<Expression>? arguments)
		{
			MemberInfo = memberInfo;
			Arguments  = arguments;
		}
		/// <summary>
		/// AccessorMember type.
		/// </summary>

		public AccessorMember(Expression expression)
		{
			if (expression is MethodCallExpression mc)
			{
				MemberInfo = mc.Method;
				Arguments  = mc.Arguments;
			}
			else if (expression is MemberExpression ma)
			{
				MemberInfo = ma.Member;
			}
			else
			{
				throw new InvalidOperationException($"Expression '{expression}' cannot be used in association.");
			}
		}
		/// <summary>
		/// Gets or sets MemberInfo.
		/// </summary>

		public MemberInfo MemberInfo { get; }
		/// <summary>
		/// Arguments
		/// </summary>
		/// <summary>
		/// Gets or sets Arguments.
		/// </summary>
		public ReadOnlyCollection<Expression>? Arguments { get; }
		/// <summary>
		/// Equals
		/// </summary>

		protected bool Equals(AccessorMember other)
		{
			if (!MemberInfo.Equals(other.MemberInfo))
				return false;

			if (Arguments == other.Arguments)
				return true;

			if (Arguments == null || other.Arguments == null || Arguments.Count != other.Arguments.Count)
				return false;

			for (int i = 0; i < Arguments.Count; i++)
			{
				if (!Arguments[i].Equals(other.Arguments[i]))
					return false;
			}

			return true;
		}
/// <summary>
/// Equals
/// </summary>
	

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((AccessorMember)obj);
		}
/// <summary>
/// Gets GetHashCode.
/// </summary>
	

		public override int GetHashCode()
		{
			unchecked
			{
				return (MemberInfo.GetHashCode() * 397) ^ (Arguments != null ? Arguments.GetHashCode() : 0);
			}
		}
	}
}
