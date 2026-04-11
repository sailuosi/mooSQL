using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace mooSQL.data.model
{
    /// <summary>
    /// 将 WITH 中的 <see cref="CTEClause"/> 作为表使用的来源节点（<c>SqlCteTable</c>）。
    /// </summary>
    public class CteTableWord :TableWord, ITableNode
	{
		/// <summary>对应的公用表表达式定义。</summary>
		public CTEClause? Cte { get; set; }

		/// <inheritdoc />
		public string Name{
			get {
				return Cte.Name;
			}
		}

		/// <inheritdoc />
        public override SqlObjectName TableName
        {
            get => new SqlObjectName(Cte?.Name ?? string.Empty);
            set { }
        }

		/// <inheritdoc />
        public override ClauseType NodeType  => ClauseType.SqlCteTable;
        //public override SqlTableType     SqlTableType => SqlTableType.Cte;

		/// <summary>由 CTE 定义与实体 CLR 类型构造。</summary>
        public CteTableWord(CTEClause cte,    Type entityType)    
            : base(entityType, null, new SqlObjectName(cte.Name ?? string.Empty))
        {
            Cte = cte;
        }

		/// <summary>指定来源 ID、别名、列与 CTE 定义。</summary>
        public CteTableWord(int id, string alias, FieldWord[] fields, CTEClause cte)
            : base(id, null, alias, new(string.Empty), cte.ObjectType, null, fields, SqlTableType.Cte, null, data.TableOptions.NotSet, null)
        {
            Cte = cte;
        }

		/// <summary>仅指定来源 ID、别名与列（无 CTE 体时占位）。</summary>
        public CteTableWord(int id, string alias, FieldWord[] fields)
            : base(id, null, alias, new(string.Empty), null!, null, fields, SqlTableType.Cte, null, data.TableOptions.NotSet, null)
        {
        }


		/// <inheritdoc />
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

		/// <inheritdoc />
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
