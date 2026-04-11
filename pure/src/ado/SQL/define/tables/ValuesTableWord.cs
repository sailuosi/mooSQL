using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace mooSQL.data.model
{
	/// <summary>
	/// <c>VALUES (…), (…)</c> 行集来源表：由表达式或显式行列构造。
	/// </summary>
	public class ValuesTableWord :Clause, ITableNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitValuesTable(this);
        }
        /// <summary>生成行数据的源表达式（如序列投影），可为空表示仅静态行。</summary>
        public ISQLNode? Source { get; }

		/// <summary>各行单元格表达式；列顺序须与 <see cref="Fields"/> 一致。</summary>
        public List<IExpWord[]>? Rows { get; private set; }
        /// <summary>
        /// 构建阶段使用的成员到列字段映射。
        /// </summary>
        public Dictionary<MemberInfo, FieldWord>? FieldsLookup { get; set; }

        private readonly List<FieldWord> _fields = new();

        /// <summary>查询中使用的列字段（与各行 <see cref="Rows"/> 顺序对齐）。</summary>
        public List<FieldWord> Fields => _fields;

		/// <summary>将运行时对象转为单元格表达式的构建器（与枚举源配合）。</summary>
        public List<Func<object, IExpWord>>? ValueBuilders { get; set; }


        /// <summary>
        /// 在构建上下文中由源表达式创建实例并分配 <see cref="SourceID"/>。
        /// </summary>
        /// <param name="source">包含可枚举数据源的逻辑节点。</param>
        public ValuesTableWord(ISQLNode source) : base(ClauseType.SqlValuesTable, null)
        {
			Source        = source;
			FieldsLookup  = new();

			SourceID = Interlocked.Increment(ref SelectQueryClause.SourceIDCounter);
		}

        /// <summary>
        /// 供转换访问器使用的构造：显式指定源、值构建器、列与行。
        /// </summary>
        public ValuesTableWord(ISQLNode? source, List<Func<object, IExpWord>>? valueBuilders, IEnumerable<FieldWord> fields, List<IExpWord[]>? rows)
            : base(ClauseType.SqlValuesTable, null)
        {
			Source        = source;
			ValueBuilders = valueBuilders;
			Rows          = rows;

			foreach (var field in fields)
			{
				if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");
				_fields.Add(field);
				field.Table = this;
			}

			SourceID = Interlocked.Increment(ref SelectQueryClause.SourceIDCounter);
		}

		/// <summary>由列、可选成员映射与行数据构造。</summary>
        public ValuesTableWord(FieldWord[] fields, MemberInfo?[]? members, List<IExpWord[]> rows) : base(ClauseType.SqlValuesTable, null)
        {
            Rows = rows;
            FieldsLookup = new();

            foreach (var field in fields)
            {
                if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");
                _fields.Add(field);
                field.Table = this;
            }

            if (members != null)
            {
                for (var index = 0; index < fields.Length; index++)
                {
                    var member = members[index];
                    if (member != null)
                    {
                        var field = fields[index];
                        FieldsLookup.Add(member, field);
                    }
                }
            }

            SourceID = Interlocked.Increment(ref SelectQueryClause.SourceIDCounter);
        }



        private FieldWord? _all;
		/// <inheritdoc />
        FieldWord ITableNode.All => _all ??= FieldWord.All(this);

		/// <inheritdoc />
        public int SourceID { get; }

		/// <inheritdoc />
		SqlTableType ITableNode.SqlTableType => SqlTableType.Values;


		/// <inheritdoc />
        IList<IExpWord> ITableNode.GetKeys(bool allIfEmpty)
        {
            return _fields.ToArray();
        }


		/// <inheritdoc />
        public bool CanBeNullable(ISQLNode nullability) => throw new NotImplementedException();

		/// <summary>行集表在 CLR 中视为 <see cref="object"/> 集合。</summary>
        Type SystemType => typeof(object);



		/// <inheritdoc />
        public override ClauseType NodeType => ClauseType.SqlValuesTable;

		/// <inheritdoc />
        public string Name => throw new NotImplementedException();
    }
}
