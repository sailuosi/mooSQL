using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;

namespace mooSQL.linq
{
    /// <summary>
    /// join语句 on部分的处理器
    /// </summary>
    public class JoinOnExpressionVisitor
    {

        private DBInstance DB;

        private WhereExpressionVisitor whereExpressionVisitor;
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="DB"></param>
        public JoinOnExpressionVisitor(DBInstance DB) { 
            this.DB = DB;

            var kit = DB.useSQL();
            var context= new FastCompileContext();
            context.initByBuilder(kit);

            this.whereExpressionVisitor = new WhereExpressionVisitor(context);
        }

        /// <summary>
        /// 将外部层的别名/参数等上下文复制到内部 WHERE 访问器的当前层，以便 JOIN ON 与主查询共享状态。
        /// </summary>
        /// <param name="layer">要复制的层上下文。</param>
        public void CopyLayer(LayerContext layer) { 
            this.whereExpressionVisitor.Context.CurrentLayer.Copy(layer);
        }

        /// <summary>
        /// 访问并获取结果
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public SQLCmd Visit(Expression expression) { 
            var tar= this.whereExpressionVisitor.Visit(expression);

            var cmd = whereExpressionVisitor.Context.TopLayer.Root.buildWhereContent();
            var paras = whereExpressionVisitor.Context.TopLayer.Root.ps;
            return new SQLCmd(cmd, paras);
        }

    }
}
