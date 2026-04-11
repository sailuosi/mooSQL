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

        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitDerivatedTable(this);
        }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.SqlTable;

        private string _asName;
        /// <summary>
        /// 别名
        /// </summary>
        public string Name{
                get{ return _asName; } set{ _asName = value; }
        }

        /// <inheritdoc />
        public FieldWord All => throw new NotImplementedException();

        /// <inheritdoc />
        public int SourceID => throw new NotImplementedException();

        /// <inheritdoc />
        public SqlTableType SqlTableType => throw new NotImplementedException();

        /// <summary>被包一层别名的底层表或查询。</summary>
        public ITableNode src;

        /// <summary>为 <paramref name="src"/> 指定别名 <paramref name="asName"/>。</summary>
        public DerivatedTableWord(ITableNode src, string asName) : base(ClauseType.SqlTable, null)
        {
            _asName = asName;
            this.src = src;
        }

        /// <inheritdoc />
        public IList<IExpWord>? GetKeys(bool allIfEmpty)
        {
            throw new NotImplementedException();
        }
    }
}
