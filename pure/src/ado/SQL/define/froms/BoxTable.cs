using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data.model
{
    /// <summary>
    /// 表盒子，存放一组表
    /// </summary>
    public class BoxTable:Clause,ITableNode
    {
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitBoxTable(this);
        }

        private LinkBox<ITableNode, JoinKind, JoinOnWord> _content;

        public BoxTable() : base(ClauseType.SqlTable, null)
        {
            _content = new LinkBox<ITableNode, JoinKind, JoinOnWord>();
            _content.isTop = true;
            _content.isNot = false;
            _content.isBox = true;
            _content.root= _content;
            _content.focus = _content;

        }

        public LinkBox<ITableNode, JoinKind, JoinOnWord> Content
        {
            get { 
                return _content;
            }
        }

        public ClauseType  NodeType => ClauseType.SqlTable;

        public string Name => throw new NotImplementedException();

        public FieldWord All => throw new NotImplementedException();

        public int SourceID => throw new NotImplementedException();

        public SqlTableType SqlTableType => throw new NotImplementedException();

        public IList<IExpWord>? GetKeys(bool allIfEmpty)
        {
            throw new NotImplementedException();
        }

        public ITableNode Join(JoinKind joinType, ITableNode table, string asName, JoinOnWord onCondition) {

            var item = new DerivatedTableWord(table, asName);
            _content.add(item, joinType, onCondition);
            return item;
        }
    }
}
