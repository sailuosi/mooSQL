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

        public LinkedBag<ITableNode> content;

        public LinkedTableWord() : base(ClauseType.SqlTable, null)
        { 
            this.content = new LinkedBag<ITableNode>();

        }

        public override ClauseType NodeType => ClauseType.SqlTable;

        public string Name => throw new NotImplementedException();

        public FieldWord All => throw new NotImplementedException();

        public int SourceID => throw new NotImplementedException();

        public SqlTableType SqlTableType => throw new NotImplementedException();

        public IList<IExpWord>? GetKeys(bool allIfEmpty)
        {
            throw new NotImplementedException();
        }
    }
}
