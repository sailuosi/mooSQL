using System;
using System.Runtime.CompilerServices;

namespace mooSQL.linq.SqlQuery
{

	using mooSQL.data.model;

	public class ObjectWord :Clause, IExpWord
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitObjectExpression(this);
        }
        public readonly SqlGetValue[] _infoParameters;

		public ObjectWord( SqlGetValue[] infoParameters) : base(ClauseType.SqlObjectExpression, null)
        {
			//MappingSchema   = mappingSchema;
			_infoParameters = infoParameters;
		}



		public Type? SystemType => null;
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

		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(this);
		}

		public bool Equals(IExpWord? other, Func<IExpWord, IExpWord, bool> comparer)
		{
			return ReferenceEquals(this, other);
		}





#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public ClauseType NodeType => ClauseType.SqlObjectExpression;





		//public MappingSchema MappingSchema { get; }

        public SqlGetValue[] InfoParameters => _infoParameters;
	}
}
