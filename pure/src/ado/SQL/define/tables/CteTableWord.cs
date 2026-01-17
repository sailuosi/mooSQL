using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace mooSQL.data.model
{
    /// <summary>
    /// 使用CTE作为表来源,SqlCteTable
    /// </summary>
    public class CteTableWord :TableWord, ITableNode
	{
		public CTEClause? Cte { get; set; }

		public string Name{
			get {
				return Cte.Name;
			}
		}

        public override SqlObjectName TableName
        {
            get => new SqlObjectName(Cte?.Name ?? string.Empty);
            set { }
        }

        public override ClauseType NodeType  => ClauseType.SqlCteTable;
        //public override SqlTableType     SqlTableType => SqlTableType.Cte;

        public CteTableWord(CTEClause cte,    Type entityType)    
            : base(entityType, null, new SqlObjectName(cte.Name ?? string.Empty))
        {
            Cte = cte;
        }

        public CteTableWord(int id, string alias, FieldWord[] fields, CTEClause cte)
            : base(id, null, alias, new(string.Empty), cte.ObjectType, null, fields, SqlTableType.Cte, null, data.TableOptions.NotSet, null)
        {
            Cte = cte;
        }

        public CteTableWord(int id, string alias, FieldWord[] fields)
            : base(id, null, alias, new(string.Empty), null!, null, fields, SqlTableType.Cte, null, data.TableOptions.NotSet, null)
        {
        }


        public  IElementWriter ToString(IElementWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.Append("CteTable(")
				.AppendElement(Cte)
				.Append('[').Append(Name).Append(']')
				.Append(')');

			return writer;
		}

        public override IList<IExpWord>? GetKeys(bool allIfEmpty)
        {
            if (Cte?.Body == null)
                return null;

            var cteKeys = Cte.Body.GetKeys(allIfEmpty);

            if (!(cteKeys?.Count > 0))
                return cteKeys;

            var hasInvalid = false;
            IList<IExpWord> projected = Cte.Body.Select.Columns.content.Select((c, idx) =>
            {
                var found = cteKeys.FirstOrDefault(k => ReferenceEquals(c, k));
                if (found != null)
                {
                    var field = Cte.Fields[idx];

                    var foundField = Fields.FirstOrDefault(f => f.Name == field.Name);
                    if (foundField == null)
                        hasInvalid = true;
                    return (foundField as IExpWord)!;
                }

                hasInvalid = true;
                return null!;
            }).ToList();

            if (hasInvalid)
                return null;

            return projected;
        }
    }
}
