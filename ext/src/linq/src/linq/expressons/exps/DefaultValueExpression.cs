using System;
using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	using mooSQL.linq.utils;
	using mooSQL.linq.mapping;
    using mooSQL.data;
    /// <summary>
    /// DefaultValueExpression type.
    /// </summary>

    public class DefaultValueExpression : Expression
	{
        /// <summary>
        /// DefaultValueExpression type.
        /// </summary>
        /// <summary>
        /// DefaultValueExpression type.
        /// </summary>
        public DefaultValueExpression(DBInstance? mappingSchema, Type type)
        {
            DBLive = mappingSchema;
            _type = type;
        }
        /// <summary>
        /// DefaultValueExpression type.
        /// </summary>
        /// <summary>
        /// DefaultValueExpression type.
        /// </summary>
        public DefaultValueExpression( Type type)
        {
            _type = type;
        }

		readonly DBInstance DBLive;
		readonly Type           _type;
/// <summary>
/// Type
/// </summary>
	

		public override Type           Type      => _type;
	/// <summary>
	/// NodeType
	/// </summary>
	
		public override ExpressionType NodeType  => ExpressionType.Extension;
	/// <summary>
	/// CanReduce
	/// </summary>
	
		public override bool           CanReduce => true;
/// <summary>
/// Reduce
/// </summary>
	

		public override Expression Reduce()
		{
			return Constant(
				DBLive == null ?
					DefaultValue.GetValue(Type) :
					DBLive.dialect.mapping.GetDefaultValue(Type),
				Type);
		}
/// <summary>
/// ToString
/// </summary>
	

		public override string ToString()
		{
			return $"Default({Type.Name})";
		}
		/// <summary>
		/// Equals
		/// </summary>

		protected bool Equals(DefaultValueExpression other)
		{
			return Equals(DBLive, other.DBLive) && _type.Equals(other._type);
		}
/// <summary>
/// Equals
/// </summary>
	

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((DefaultValueExpression)obj);
		}
/// <summary>
/// Gets GetHashCode.
/// </summary>
	

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = DBLive.config?.GetHashCode() ?? 0;
				hashCode = (hashCode * 397) ^ _type.GetHashCode();
				return hashCode;
			}
		}
/// <summary>
/// Accept
/// </summary>
	

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitDefaultValueExpression(this);
			return base.Accept(visitor);
		}

	}
}
