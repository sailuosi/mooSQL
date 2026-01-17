using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;
using mooSQL.data.clip;
using mooSQL.linq;

namespace mooSQL.data.clip
{
    internal class ClipJoinVisitor
    {
        private DBInstance DB;

        private ClipConditionVisitor whereExpressionVisitor;

        private SQLClip clip;
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="DB"></param>
        public ClipJoinVisitor(DBInstance DB, SQLClip clip)
        {
            this.DB = DB;

            var kit = DB.useSQL();
            var context = new FastCompileContext();
            context.initByBuilder(kit);

            this.whereExpressionVisitor = new ClipConditionVisitor(context,clip);
            this.clip = clip;
        }

        public void CopyLayer(LayerContext layer)
        {
            this.whereExpressionVisitor.Context.CurrentLayer.Copy(layer);
        }

        /// <summary>
        /// 访问并获取结果
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public ClipJoinData Visit(Expression expression)
        {
            var tar = this.whereExpressionVisitor.Visit(expression);

            var cmd = whereExpressionVisitor.Context.TopLayer.Root.buildWhereContent();
            var paras = whereExpressionVisitor.Context.TopLayer.Root.ps;
            return new ClipJoinData()
            {
                onSQL = cmd,
                paras = paras,
                ParsedTables = whereExpressionVisitor.ParsedTables
            };
        }
    }

    internal class ClipJoinData {
        public string onSQL { get; set; }

        public Paras paras { get; set; }

        public List<ClipTable> ParsedTables { get; set; }

        public ClipTable JoinBy { get; set; }

        public string JoinType { get; set; }
    
    }
}
