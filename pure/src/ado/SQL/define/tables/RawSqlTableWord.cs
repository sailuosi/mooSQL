using mooSQL.data;
using System;
using System.Linq;

namespace mooSQL.data.model
{


    /// <summary>
    /// 由原始 SQL 片段与参数构成的表来源（<c>SqlRawSqlTable</c>），常用于内联视图或方言特定片段。
    /// </summary>
    public class RawSqlTableWord : TableWord
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitRawSqlTable(this);
        }
		/// <summary>括号内嵌入的 SQL 文本。</summary>
		public string SQL { get; }

		/// <summary>与 SQL 中占位符对应的参数表达式。</summary>
		public IExpWord[] Parameters { get; private set; }

		/// <summary>绑定实体元数据、SQL 文本与参数列表。</summary>
		public RawSqlTableWord(EntityInfo endtityDescriptor,string sql,IExpWord[] parameters)
            : base(endtityDescriptor)
        {
			SQL        = sql        ?? throw new ArgumentNullException(nameof(sql));
			Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

			foreach (var value in parameters)
				if (value == null) throw new ArgumentNullException(nameof(parameters));
		}


		/// <summary>复制表结构并替换参数列表。</summary>
		public RawSqlTableWord(RawSqlTableWord table, IExpWord[] parameters)
			: base(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;

			SequenceAttributes = table.SequenceAttributes;

			AddRange(table.Fields.Select(f => new FieldWord(f)));

			SQL                = table.SQL;
			Parameters         = parameters;
		}


		/// <inheritdoc />
		public override ClauseType NodeType  => ClauseType.SqlRawSqlTable;

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.AppendLine("(")
				.Append(SQL)
				.Append(')')
				.AppendLine();

			return writer;
		}

		#region IQueryElement Members

		/// <summary>调试/展示用的完整 SQL 文本。</summary>
		public string SqlText => this.ToDebugString();

		#endregion
	}
}
