using System;

using mooSQL.data.model;

namespace mooSQL.linq.SqlQuery
{
	/// <summary>
	/// 表/子查询别名的占位节点，在后续解析阶段再绑定实际表名。
	/// </summary>
	public class AliasPlaceholderWord : Clause, IExpWord
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAliasPlaceholder(this);
        }
        /// <summary>单例占位符实例。</summary>
        public static readonly AliasPlaceholderWord Instance = new();

		AliasPlaceholderWord() : base(ClauseType.SqlAliasPlaceholder, null) { }

#if DEBUG
		/// <summary>调试输出文本。</summary>
		public string DebugText => this.ToDebugString();
#endif

		/// <inheritdoc />
		public ClauseType NodeType => ClauseType.SqlAliasPlaceholder;



		/// <inheritdoc />
		public bool Equals(IExpWord? other)
		{
			return other == this;
		}

		/// <inheritdoc />
		public bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			return comparer(this, other);
		}

		/// <inheritdoc />
		public bool CanBeNullable(NullabilityContext nullability) => false;
		/// <inheritdoc />
		public bool CanBeNull => false;
		/// <inheritdoc />
		public int Precedence => PrecedenceLv.Primary;
		/// <inheritdoc />
		public Type SystemType => typeof(object);
	}
}
