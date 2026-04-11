using System;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.data.model
{
	/// <summary>
	/// 带查询的语句
	/// </summary>
	public abstract class BaseSentenceWithQuery : BaseSentence
	{
		/// <inheritdoc />
		public override bool          IsParameterDependent
		{
			get => SelectQuery.IsParameterDependent;
			set => SelectQuery.IsParameterDependent = value;
		}

		private         SelectQueryClause? _selectQuery;
		//[NotNull]
		/// <inheritdoc />
		public override SelectQueryClause?  SelectQuery
		{
			get => _selectQuery ??= new SelectQueryClause();
			set => _selectQuery = value;
		}

		/// <summary>可选 WITH（CTE）子句。</summary>
		public WithClause? With { get; set; }

		/// <summary>指定查询体、语句节点类型与 CLR 类型。</summary>
		protected BaseSentenceWithQuery(SelectQueryClause? selectQuery,ClauseType clauseType,Type type) : base(clauseType, type)
        {
			_selectQuery = selectQuery;
		}


	}
}
