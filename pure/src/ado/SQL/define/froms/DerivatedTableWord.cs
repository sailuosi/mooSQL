using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// 派生表，即 as name 的表
    /// </summary>
    public class DerivatedTableWord :Clause, ITableNode
    {

        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitDerivatedTable(this);
        }

        public override ClauseType NodeType => ClauseType.SqlTable;

        private string _asName;
        /// <summary>
        /// 别名
        /// </summary>
        public string Name{
                get{ return _asName; } set{ _asName = value; }
        }

        public FieldWord All => throw new NotImplementedException();

        public int SourceID => throw new NotImplementedException();

        public SqlTableType SqlTableType => throw new NotImplementedException();

        public ITableNode src;

        public DerivatedTableWord(ITableNode src, string asName) : base(ClauseType.SqlTable, null)
        {
            _asName = asName;
            this.src = src;
        }

        public IList<IExpWord>? GetKeys(bool allIfEmpty)
        {
            throw new NotImplementedException();
        }
    }
}
