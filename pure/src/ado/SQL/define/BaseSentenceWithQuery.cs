using System;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.data.model
{
	/// <summary>
	/// 带查询的语句
	/// </summary>
	public abstract class BaseSentenceWithQuery : BaseSentence
	{
		public override bool          IsParameterDependent
		{
			get => SelectQuery.IsParameterDependent;
			set => SelectQuery.IsParameterDependent = value;
		}

		private         SelectQueryClause? _selectQuery;
		//[NotNull]
		public override SelectQueryClause?  SelectQuery
		{
			get => _selectQuery ??= new SelectQueryClause();
			set => _selectQuery = value;
		}

		public WithClause? With { get; set; }

		protected BaseSentenceWithQuery(SelectQueryClause? selectQuery,ClauseType clauseType,Type type) : base(clauseType, type)
        {
			_selectQuery = selectQuery;
		}


	}
}
