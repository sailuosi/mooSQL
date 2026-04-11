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
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitBoxTable(this);
        }

        private LinkBox<ITableNode, JoinKind, JoinOnWord> _content;

        /// <summary>初始化根级表连接图。</summary>
        public BoxTable() : base(ClauseType.SqlTable, null)
        {
            _content = new LinkBox<ITableNode, JoinKind, JoinOnWord>();
            _content.isTop = true;
            _content.isNot = false;
            _content.isBox = true;
            _content.root= _content;
            _content.focus = _content;

        }

        /// <summary>内部连接图（表与 JOIN 边）。</summary>
        public LinkBox<ITableNode, JoinKind, JoinOnWord> Content
        {
            get { 
                return _content;
            }
        }

        /// <inheritdoc />
        public ClauseType  NodeType => ClauseType.SqlTable;

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

        /// <summary>将表以派生别名形式加入连接图。</summary>
        public ITableNode Join(JoinKind joinType, ITableNode table, string asName, JoinOnWord onCondition) {

            var item = new DerivatedTableWord(table, asName);
            _content.add(item, joinType, onCondition);
            return item;
        }
    }
}
