using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// 链接起来的表表达式，每个成员之间会定义链接内容
    /// </summary>
    public class LinkedTableWord: Clause, ITableNode
    {

        /// <summary>顺序链接的表节点集合。</summary>
        public LinkedBag<ITableNode> content;

        /// <summary>初始化空链表。</summary>
        public LinkedTableWord() : base(ClauseType.SqlTable, null)
        { 
            this.content = new LinkedBag<ITableNode>();

        }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.SqlTable;

        /// <inheritdoc />
        public string Name => throw new NotImplementedException();

        /// <inheritdoc />
        public FieldWord All => throw new NotImplementedException();

        /// <inheritdoc />
        public int SourceID => throw new NotImplementedException();

        /// <inheritdoc />
        public SqlTableType SqlTableType => throw new NotImplementedException();

        /// <inheritdoc />
        public IList<IExpWord>? GetKeys(bool allIfEmpty)
        {
            throw new NotImplementedException();
        }
    }
}
