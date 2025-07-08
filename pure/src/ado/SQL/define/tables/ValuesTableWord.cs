using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace mooSQL.data.model
{
	/// <summary>
	/// 直接使用值创建的来源表
	/// </summary>
	public class ValuesTableWord :Clause, ITableNode
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitValuesTable(this);
        }
        /// <summary>
        /// Source value expression.
        /// </summary>
        public ISQLNode? Source { get; }

        public List<IExpWord[]>? Rows { get; private set; }
        /// <summary>
        /// Used only during build.
        /// </summary>
        public Dictionary<MemberInfo, FieldWord>? FieldsLookup { get; set; }

        private readonly List<FieldWord> _fields = new();

        // Fields from source, used in query. Columns in rows should have same order.
        public List<FieldWord> Fields => _fields;

        public List<Func<object, IExpWord>>? ValueBuilders { get; set; }


        /// <summary>
        /// To create new instance in build context.
        /// </summary>
        /// <param name="source">Expression, that contains enumerable source.</param>
        public ValuesTableWord(ISQLNode source) : base(ClauseType.SqlValuesTable, null)
        {
			Source        = source;
			FieldsLookup  = new();

			SourceID = Interlocked.Increment(ref SelectQueryClause.SourceIDCounter);
		}

        /// <summary>
        /// Constructor for convert visitor.
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
        FieldWord ITableNode.All => _all ??= FieldWord.All(this);

        public int SourceID { get; }

		SqlTableType ITableNode.SqlTableType => SqlTableType.Values;


        IList<IExpWord> ITableNode.GetKeys(bool allIfEmpty)
        {
            return _fields.ToArray();
        }


        public bool CanBeNullable(ISQLNode nullability) => throw new NotImplementedException();

        Type SystemType => typeof(object);



        public override ClauseType NodeType => ClauseType.SqlValuesTable;

        public string Name => throw new NotImplementedException();
    }
}
