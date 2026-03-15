using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Expressions
{
	using Common;
	using Mapping;
    using mooSQL.data;

    public class DefaultValueExpression : Expression
	{
        public DefaultValueExpression(DBInstance? mappingSchema, Type type)
        {
            DBLive = mappingSchema;
            _type = type;
        }
        public DefaultValueExpression( Type type)
        {
            _type = type;
        }

		readonly DBInstance DBLive;
		readonly Type           _type;

		public override Type           Type      => _type;
		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override bool           CanReduce => true;

		public override Expression Reduce()
		{
			return Constant(
				DBLive == null ?
					DefaultValue.GetValue(Type) :
					DBLive.dialect.mapping.GetDefaultValue(Type),
				Type);
		}

		public override string ToString()
		{
			return $"Default({Type.Name})";
		}

		protected bool Equals(DefaultValueExpression other)
		{
			return Equals(DBLive, other.DBLive) && _type.Equals(other._type);
		}

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

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = DBLive.config?.GetHashCode() ?? 0;
				hashCode = (hashCode * 397) ^ _type.GetHashCode();
				return hashCode;
			}
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitDefaultValueExpression(this);
			return base.Accept(visitor);
		}

	}
}
