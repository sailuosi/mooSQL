using mooSQL.data;
using System;
using System.Linq;

namespace mooSQL.data.model
{


    /// <summary>
    /// 直接用SQL配置的一个来源表 SqlRawSqlTable
    /// </summary>
    public class RawSqlTableWord : TableWord
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitRawSqlTable(this);
        }
		public string SQL { get; }

		public IExpWord[] Parameters { get; private set; }

		public RawSqlTableWord(EntityInfo endtityDescriptor,string sql,IExpWord[] parameters)
            : base(endtityDescriptor)
        {
			SQL        = sql        ?? throw new ArgumentNullException(nameof(sql));
			Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

			foreach (var value in parameters)
				if (value == null) throw new ArgumentNullException(nameof(parameters));
		}


		public RawSqlTableWord(RawSqlTableWord table, IExpWord[] parameters)
			: base(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;

			SequenceAttributes = table.SequenceAttributes;

			AddRange(table.Fields.Select(f => new FieldWord(f)));

			SQL                = table.SQL;
			Parameters         = parameters;
		}


		public override ClauseType NodeType  => ClauseType.SqlRawSqlTable;

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

		public string SqlText => this.ToDebugString();

		#endregion
	}
}
