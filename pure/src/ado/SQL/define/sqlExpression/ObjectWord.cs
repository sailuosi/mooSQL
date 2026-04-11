using System;
using System.Runtime.CompilerServices;

namespace mooSQL.linq.SqlQuery
{

	using mooSQL.data.model;

	/// <summary>
	/// 表示由多个 <see cref="SqlGetValue"/> 组成的对象表达式（如行构造）。
	/// </summary>
	public class ObjectWord :Clause, IExpWord
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitObjectExpression(this);
        }
        /// <summary>组成对象各属性的取值表达式。</summary>
        public readonly SqlGetValue[] _infoParameters;

		/// <summary>
		/// 使用各属性取值列表构造对象表达式。
		/// </summary>
		public ObjectWord( SqlGetValue[] infoParameters) : base(ClauseType.SqlObjectExpression, null)
        {
			//MappingSchema   = mappingSchema;
			_infoParameters = infoParameters;
		}



		/// <inheritdoc />
		public Type? SystemType => null;
		/// <inheritdoc />
		public int Precedence => PrecedenceLv.Unknown;

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion



		bool IEquatable<IExpWord>.Equals(IExpWord? other)
		{
			return Equals(other, DefaultComparer);
		}



		private bool? _canBeNull;

		/// <inheritdoc />
		public bool CanBeNull
		{
			get
			{
				if (_canBeNull.HasValue)
					return _canBeNull.Value;

				return false;
			}

			set => _canBeNull = value;
		}

		internal static Func<IExpWord,IExpWord,bool> DefaultComparer = (x, y) => true;

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(this);
		}

		/// <inheritdoc />
		public bool Equals(IExpWord? other, Func<IExpWord, IExpWord, bool> comparer)
		{
			return ReferenceEquals(this, other);
		}





#if DEBUG
		/// <summary>调试输出文本。</summary>
		public string DebugText => this.ToDebugString();
#endif
		/// <inheritdoc />
		public ClauseType NodeType => ClauseType.SqlObjectExpression;





		//public MappingSchema MappingSchema { get; }

        /// <summary>组成对象的各属性取值。</summary>
        public SqlGetValue[] InfoParameters => _infoParameters;
	}
}
